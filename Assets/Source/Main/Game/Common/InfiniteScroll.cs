// InfiniteScroll.cs (汎用化・リフレッシュ機能付き改造版)
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;

// アイテムプレハブが実装すべきインターフェース (オプション)
public interface IInfiniteScrollItem<T>
{
    void Setup(T itemData, int index, Action<int> onClickCallback); // データ設定とコールバック登録
}

[RequireComponent(typeof(ScrollRect))]
public class InfiniteScroll<T> : MonoBehaviour // ジェネリクス<T>を使用
{
    [Header("References")]
    [SerializeField] private RectTransform content;
    [SerializeField] private GameObject slotItemPrefab; // アイテムプレハブ (IInfiniteScrollItem<T>を実装したコンポーネントを持つこと)

    public Action<int> OnItemClicked; // 外部からのクリックイベント購読用

    private IList<T> dataList; // データソース (ジェネリック型)
    private float itemHeight = 100f;
    private int renderBuffer = 3; // ビューポート上下に見えない範囲で余分に生成/保持するアイテム数

    private List<MonoBehaviour> itemsPool = new List<MonoBehaviour>(); // プール (型をMonoBehaviourに)
    private ScrollRect scrollRect;
    private float previousScrollPos = 0f;
    private Coroutine updateCoroutine = null;

    private int firstVisibleIndex = -1;
    private int lastVisibleIndex = -1;

    private void Awake()
    {
        scrollRect = GetComponent<ScrollRect>();
        if (!content) content = scrollRect.content;
        scrollRect.onValueChanged.AddListener(OnScrollValueChanged);

        // プレハブの有効性を確認
        if (slotItemPrefab == null)
        {
            Debug.LogError("[InfiniteScroll] SlotItemPrefab is not assigned!");
            enabled = false;
            return;
        }
        if (slotItemPrefab.GetComponent<IInfiniteScrollItem<T>>() == null) // インターフェース実装確認
        {
            Debug.LogError($"[InfiniteScroll] Prefab {slotItemPrefab.name} must have a component implementing IInfiniteScrollItem<{typeof(T).Name}>.");
            // 代替案: 特定のメソッド名(例: Setup)をリフレクションで探すか、規約とする
            // enabled = false;
            // return;
        }
    }

    public void Initialize(IList<T> data, float prefabHeight)
    {
        StopUpdateCoroutine(); // 既存のコルーチン停止
        // 古いアイテムを削除 (プールをクリア)
        foreach (var item in itemsPool)
        {
            if (item != null && item.gameObject != null) Destroy(item.gameObject);
        }
        itemsPool.Clear();

        this.dataList = data;
        this.itemHeight = prefabHeight;

        if (dataList == null || dataList.Count == 0)
        {
            // データがない場合はContentを空にする
            LayoutRebuilder.ForceRebuildLayoutImmediate(content); // レイアウトを即時更新
            content.sizeDelta = new Vector2(content.sizeDelta.x, 0);
            return;
        }

        // Contentの高さを計算 (LayoutGroupを考慮)
        float totalHeight = CalculateContentHeight();
        content.sizeDelta = new Vector2(content.sizeDelta.x, totalHeight);


        // 初期スクロール位置を一番上に
        scrollRect.verticalNormalizedPosition = 1f;
        previousScrollPos = 1f;
        firstVisibleIndex = -1; // インデックスをリセット
        lastVisibleIndex = -1;

        // 更新コルーチンを開始
        updateCoroutine = StartCoroutine(UpdateVisibleItemsCoroutine(true));
    }

    private float CalculateContentHeight()
    {
        float totalHeight = dataList.Count * itemHeight;
        VerticalLayoutGroup layoutGroup = content.GetComponent<VerticalLayoutGroup>();
        if (layoutGroup != null)
        {
            if (dataList.Count > 0)
            {
                totalHeight += (dataList.Count - 1) * layoutGroup.spacing;
            }
            totalHeight += layoutGroup.padding.top + layoutGroup.padding.bottom;
        }
        return totalHeight;
    }

    private void OnScrollValueChanged(Vector2 normalizedPos)
    {
        // 頻繁な更新を防ぐため、コルーチンで処理
        if (updateCoroutine == null)
        {
            updateCoroutine = StartCoroutine(UpdateVisibleItemsCoroutine(false));
        }
    }

    private IEnumerator UpdateVisibleItemsCoroutine(bool forceUpdate)
    {
        //yield return null; // 1フレーム待機してLayout計算などを待つ
        // 負荷軽減のため、毎フレームではなく少し待つ場合
        yield return new WaitForSeconds(0.05f);


        if (dataList == null || dataList.Count == 0)
        {
            updateCoroutine = null;
            yield break;
        }

        float currentNormPos = scrollRect.verticalNormalizedPosition;

        // スクロール変化が小さい場合は更新しない (forceUpdateを除く)
        if (!forceUpdate && Mathf.Abs(currentNormPos - previousScrollPos) < 0.001f && firstVisibleIndex != -1)
        {
            updateCoroutine = null;
            yield break;
        }

        previousScrollPos = currentNormPos;

        float contentHeight = content.rect.height;
        float viewportHeight = scrollRect.viewport.rect.height;

        // スクロール可能な領域の高さ
        float scrollAreaHeight = contentHeight - viewportHeight;
        if (scrollAreaHeight <= 0) scrollAreaHeight = 0; // スクロール不要の場合

        // 現在のビューポートの上端のY座標 (Contentのローカル座標系, 上が0)
        float viewportTopY = (1f - currentNormPos) * scrollAreaHeight;
        // 現在のビューポートの下端のY座標
        float viewportBottomY = viewportTopY + viewportHeight;

        // 表示すべきアイテムのインデックス範囲を計算
        int newFirstVisibleIndex = Mathf.Max(0, Mathf.FloorToInt(viewportTopY / itemHeight) - renderBuffer);
        int newLastVisibleIndex = Mathf.Min(dataList.Count - 1, Mathf.CeilToInt(viewportBottomY / itemHeight) - 1 + renderBuffer); // CeilToIntなので-1する

        // 表示範囲が変わっていない場合は終了 (forceUpdateを除く)
        if (!forceUpdate && newFirstVisibleIndex == firstVisibleIndex && newLastVisibleIndex == lastVisibleIndex)
        {
            updateCoroutine = null;
            yield break;
        }

        //Debug.Log($"Updating Visible Items: {newFirstVisibleIndex} to {newLastVisibleIndex}");

        // --- アイテムの再利用と更新 ---
        HashSet<int> itemsToShow = new HashSet<int>();
        for (int i = newFirstVisibleIndex; i <= newLastVisibleIndex; i++)
        {
            itemsToShow.Add(i);
        }

        List<MonoBehaviour> itemsToRemove = new List<MonoBehaviour>();
        Dictionary<int, MonoBehaviour> activeItems = new Dictionary<int, MonoBehaviour>(); // index -> item map

        // 既存のアイテムをチェック
        foreach (MonoBehaviour item in itemsPool)
        {
            if (item == null || !item.gameObject.activeSelf) continue; // 無効なアイテムは無視

            // IInfiniteScrollItem<T>を実装しているか、特定のコンポーネントを取得
            // ここでは ShopItemUI を直接参照する代わりにインターフェースを使う
            var scrollItem = item.GetComponent<IInfiniteScrollItem<T>>();
            int currentIndex = -1;
            // もしインデックスをコンポーネント内に保持しているなら取得する
            // 例: currentIndex = scrollItem.GetCurrentIndex();
            // これがない場合、アイテムとインデックスの関連付けが必要

            // 仮にGameObject名からインデックスを復元 (非推奨な方法)
            if (!int.TryParse(item.gameObject.name.Replace("Item_", ""), out currentIndex))
            {
                // インデックスが不明なアイテムは扱いが難しい
                // itemsToRemove.Add(item);
                // continue;
                // この例ではGetCurrentIndexが実装されていると仮定するか、
                // 別の方法(例: Dictionaryで管理)でインデックスを追跡する
                Debug.LogWarning($"Could not determine index for item {item.gameObject.name}");
                continue; // インデックス不明なアイテムはスキップ
            }


            if (itemsToShow.Contains(currentIndex))
            {
                // このアイテムは表示範囲内なので維持
                activeItems.Add(currentIndex, item);
                itemsToShow.Remove(currentIndex); // 表示済みとしてマーク
            }
            else
            {
                // このアイテムは表示範囲外なのでプールに戻す（非アクティブ化）
                itemsToRemove.Add(item);
            }
        }

        // 不要になったアイテムを非アクティブ化
        foreach (var itemToRemove in itemsToRemove)
        {
            if (itemToRemove != null) itemToRemove.gameObject.SetActive(false);
        }

        // 新しく表示範囲に入ったアイテムを生成または再アクティブ化
        foreach (int indexToShow in itemsToShow)
        {
            MonoBehaviour newItem = GetOrCreateItem();
            UpdateItem(newItem, indexToShow);
            activeItems.Add(indexToShow, newItem); // アクティブアイテムリストに追加
        }

        // 保持しているインデックスを更新
        firstVisibleIndex = newFirstVisibleIndex;
        lastVisibleIndex = newLastVisibleIndex;

        updateCoroutine = null; // コルーチン終了
    }

    // プールから非アクティブなアイテムを取得するか、新規作成する
    private MonoBehaviour GetOrCreateItem()
    {
        // プールから非アクティブなアイテムを探す
        foreach (var item in itemsPool)
        {
            if (item != null && !item.gameObject.activeSelf)
            {
                item.gameObject.SetActive(true);
                return item;
            }
        }

        // 見つからなければ新規作成
        GameObject go = Instantiate(slotItemPrefab, content);
        MonoBehaviour itemComp = go.GetComponent<MonoBehaviour>(); // 基本クラスで取得
        if (itemComp == null)
        {
            Debug.LogError($"Prefab {slotItemPrefab.name} does not have a MonoBehaviour component!");
            // 必要なら適切なコンポーネントを探すか追加する
            // itemComp = go.AddComponent<RectTransform>(); // ダミー
            return null;
        }
        itemsPool.Add(itemComp);
        return itemComp;
    }


    // アイテムの内容と位置を更新
    private void UpdateItem(MonoBehaviour item, int dataIndex)
    {
        if (item == null || item.gameObject == null || dataIndex < 0 || dataIndex >= dataList.Count) return;

        // 名前でインデックスを追跡 (あまり良くないが、例として)
        item.gameObject.name = $"Item_{dataIndex}"; // インデックス追跡用

        // --- 位置設定 ---
        // VerticalLayoutGroupがある場合は不要、ない場合は手動設定
        RectTransform itemRect = item.GetComponent<RectTransform>();
        if (itemRect != null && content.GetComponent<VerticalLayoutGroup>() == null)
        {
            float yPos = -(dataIndex * itemHeight) - (itemHeight * 0.5f); // 中心基準の場合
                                                                          // アンカーが上端の場合
                                                                          // float yPos = -dataIndex * itemHeight - content.GetComponent<VerticalLayoutGroup>()?.padding.top ?? 0;
            itemRect.anchoredPosition = new Vector2(0f, yPos);
        }

        // --- データ設定 ---
        // ジェネリック型データとインデックスを渡す
        var scrollItem = item.GetComponent<IInfiniteScrollItem<T>>();
        if (scrollItem != null)
        {
            scrollItem.Setup(dataList[dataIndex], dataIndex, HandleItemClick);
        }
        else
        {
            // インターフェースがない場合の代替 (特定のメソッドを呼ぶなど)
            // item.SendMessage("Setup", dataList[dataIndex], SendMessageOptions.DontRequireReceiver);
            // item.GetComponent<ShopItemUI>()?.Setup(dataList[dataIndex], dataIndex, ...) // 型が分かっている場合
            Debug.LogError($"Item {item.gameObject.name} does not implement IInfiniteScrollItem<{typeof(T).Name}> or have a compatible Setup method.");
        }

        item.gameObject.SetActive(true);
    }

    // アイテム内のクリックを処理
    private void HandleItemClick(int index)
    {
        OnItemClicked?.Invoke(index);
        //Debug.Log($"Item clicked: Index {index}");
    }

    // 表示されているアイテムを強制的に更新するメソッド
    public void RefreshVisibleItems()
    {
        //Debug.Log("Refreshing visible items...");
        StopUpdateCoroutine(); // 既存のコルーチン停止
        updateCoroutine = StartCoroutine(UpdateVisibleItemsCoroutine(true)); // 強制更新フラグを立てて実行
    }

    private void StopUpdateCoroutine()
    {
        if (updateCoroutine != null)
        {
            StopCoroutine(updateCoroutine);
            updateCoroutine = null;
        }
    }

    // コンポーネントが無効になったらコルーチン停止
    private void OnDisable()
    {
        StopUpdateCoroutine();
    }
}