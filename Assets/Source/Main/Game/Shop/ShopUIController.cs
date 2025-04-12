// ShopUIController.cs (完全版)
using UnityEngine;
using UnityEngine.UI; // UIコンポーネント使用のため
using TMPro; // TextMeshPro使用のため
using System.Collections; // コルーチン使用のため
using System.Collections.Generic; // List, Dictionary使用のため
using System.Linq; // Linqメソッド使用のため
using ResourceManagement; // 提供されたシステムのコア名前空間
using System; // Action, Tupleなど (Tupleは現在未使用)

/// <summary>
/// ショップのUI全体の制御を担当するコントローラークラス。
/// 購入タブと売却タブの切り替え、アイテムリストの表示（InfiniteScrollを使用）、
/// プレイヤーの通貨表示、購入/売却アクションの処理とフィードバックを行う。
/// ResourceManagementSystem を介して CurrencyManager および InventoryManager と連携する。
/// </summary>
public class ShopUIController : MonoBehaviour
{
    #region Inspector Variables

    [Header("System References")]
    [Tooltip("シーン内のShopSystemコンポーネントへの参照")]
    [SerializeField] private ShopSystem shopSystem;

    [Header("UI References")]
    [Tooltip("購入タブを選択するボタン")]
    [SerializeField] private Button purchaseTabButton;
    [Tooltip("売却タブを選択するボタン")]
    [SerializeField] private Button sellTabButton;

    [Header("Scroll View Settings (Separate or Shared)")]
    [Tooltip("購入アイテムリストを表示するInfiniteScrollコンポーネント（ShopItemData用）")]
    [SerializeField] private InfiniteScroll<ShopItemData> purchaseScroll;
    [Tooltip("売却アイテムリストを表示するInfiniteScrollコンポーネント（object用 - 匿名型{Item, Quantity}を想定）。購入リストと共有する場合は、こちらのみ設定し、Purchase ScrollはNoneにする")]
    [SerializeField] private InfiniteScroll<object> sellScroll; // 型は object

    [Header("Player Info & Feedback")]
    [Tooltip("プレイヤーの所持金を表示するTextMeshPro Text")]
    [SerializeField] private TextMeshProUGUI playerCurrencyText;
    [Tooltip("購入/売却の結果などのフィードバックメッセージを表示するTextMeshPro Text")]
    [SerializeField] private TextMeshProUGUI feedbackText;

    [Header("Shop Settings")]
    [Tooltip("ショップで使用する主要通貨タイプ")]
    [SerializeField] private CurrencyType currencyType = CurrencyType.StandardCurrency; // ResourceManagementSystem.csで定義されたEnum
    [Tooltip("アイテムプレハブの高さ（InfiniteScrollの計算に使用）")]
    [SerializeField] private float itemPrefabHeight = 100f;
    [Tooltip("フィードバックメッセージを表示する時間（秒）")]
    [SerializeField] private float feedbackDisplayTime = 3f;

    #endregion

    #region Private Variables

    // 各種リソース管理システムへのインターフェース参照
    private ICurrencySystem currencySystem;
    private IInventorySystem inventorySystem;

    // 売却可能アイテムリスト（Itemオブジェクトと数量を格納）
    private readonly List<object> playerSellableItems = new List<object>(); // object型で匿名型を保持

    // フィードバック表示用コルーチン参照
    private Coroutine feedbackCoroutine;

    // 現在アクティブなタブの状態
    private bool isPurchaseTabActive = true;

    // Scroll Viewが購入用と売却用で別々かどうかのフラグ
    private bool useSeparateScrollViews = false;

    #endregion

    #region Unity Lifecycle Methods

    /// <summary>
    /// 初期化処理。必要なシステムの参照を取得し、イベントを購読、UIを初期状態にする。
    /// </summary>
    private void Start()
    {
        // ResourceManagementSystem のインスタンスとサブシステムを取得
        if (!FetchSubsystems())
        {
            // 必要なシステムが見つからない場合はエラーを表示して停止
            enabled = false;
            return;
        }

        // ShopSystemの初期化 (依存するサブシステムを渡す)
        if (shopSystem == null)
        {
            ShowFeedback("エラー: ShopSystem が設定されていません。", true);
            enabled = false;
            return;
        }
        shopSystem.InitializeShop(ResourceManagementSystem.Instance); // ResourceManagementSystemインスタンスを渡す

        // Scroll View の設定を確認
        useSeparateScrollViews = (purchaseScroll != null && sellScroll != null && purchaseScroll != sellScroll);
        if (!useSeparateScrollViews && sellScroll == null)
        {
            ShowFeedback("エラー: 表示用のScroll Viewが設定されていません。(Sell Scroll)", true);
            enabled = false;
            return;
        }
        // ShopItemUIにコントローラー参照を渡す必要があれば、ここでプレハブから取得して設定するなどの処理を追加

        // タブ切り替えボタンのリスナーを設定
        SetupTabButtons();

        // 初期タブを表示（購入タブ）
        SwitchTab(true);

        // 各種イベントを購読
        SubscribeToEvents();

        // 初期UI状態を更新
        UpdatePlayerCurrencyUI();
        ClearFeedback();
    }

    /// <summary>
    /// オブジェクトが有効になった時にイベントを購読し、UIを最新の状態に更新。
    /// </summary>
    private void OnEnable()
    {
        // Startが完了しているか確認 (万が一のため)
        if (currencySystem == null || inventorySystem == null)
        {
            if (!FetchSubsystems()) return; // 再度取得試行
        }

        SubscribeToEvents();
        UpdatePlayerCurrencyUI();
        // 再有効化時にリストの内容が変わっている可能性があるためリフレッシュ
        RefreshCurrentListVisuals();
    }

    /// <summary>
    /// オブジェクトが無効になった時にイベント購読を解除し、メモリリークを防ぐ。
    /// </summary>
    private void OnDisable()
    {
        UnsubscribeFromEvents();
    }

    /// <summary>
    /// オブジェクトが破棄される時にイベント購読を解除。
    /// </summary>
    private void OnDestroy()
    {
        UnsubscribeFromEvents();
    }

    #endregion

    #region Initialization and System Access

    /// <summary>
    /// ServiceLocator を使用して、必要なサブシステム（Currency, Inventory）への参照を取得する。
    /// </summary>
    /// <returns>すべてのシステムが取得できれば true、そうでなければ false。</returns>
    private bool FetchSubsystems()
    {
        if (ResourceManagementSystem.Instance == null)
        {
            ShowFeedback("エラー: ResourceManagementSystem が見つかりません。", true);
            return false;
        }

        currencySystem = ServiceLocator.Get<ICurrencySystem>();
        inventorySystem = ServiceLocator.Get<IInventorySystem>();

        if (currencySystem == null)
        {
            ShowFeedback("エラー: CurrencySystem (ICurrencySystem) が ServiceLocator に登録されていません。", true);
            return false;
        }
        if (inventorySystem == null)
        {
            ShowFeedback("エラー: InventorySystem (IInventorySystem) が ServiceLocator に登録されていません。", true);
            return false;
        }
        return true;
    }

    /// <summary>
    /// タブ切り替えボタンにリスナーを設定する。
    /// </summary>
    private void SetupTabButtons()
    {
        if (purchaseTabButton != null)
        {
            purchaseTabButton.onClick.RemoveAllListeners(); // 既存のリスナーをクリア
            purchaseTabButton.onClick.AddListener(() => SwitchTab(true));
        }
        else { Debug.LogWarning("Purchase Tab Button が設定されていません。", this); }

        if (sellTabButton != null)
        {
            sellTabButton.onClick.RemoveAllListeners(); // 既存のリスナーをクリア
            sellTabButton.onClick.AddListener(() => SwitchTab(false));
        }
        else { Debug.LogWarning("Sell Tab Button が設定されていません。", this); }
    }

    #endregion

    #region Event Handling

    /// <summary>
    /// CurrencyManager と InventoryManager のイベントを購読する。
    /// </summary>
    private void SubscribeToEvents()
    {
        // イベントハンドラがnullでないこと、購読対象が存在することを確認
        if (currencySystem != null)
        {
            currencySystem.OnCurrencyChanged -= HandleCurrencyChanged; // 重複購読防止
            currencySystem.OnCurrencyChanged += HandleCurrencyChanged;
        }
        if (inventorySystem != null)
        {
            inventorySystem.OnItemAdded -= HandleInventoryChanged; // 重複購読防止
            inventorySystem.OnItemRemoved -= HandleInventoryChanged;
            inventorySystem.OnItemAdded += HandleInventoryChanged;
            inventorySystem.OnItemRemoved += HandleInventoryChanged;
        }
    }

    /// <summary>
    /// 購読していたイベントを解除する。
    /// </summary>
    private void UnsubscribeFromEvents()
    {
        // イベントハンドラがnullでないことを確認
        if (currencySystem != null)
        {
            currencySystem.OnCurrencyChanged -= HandleCurrencyChanged;
        }
        if (inventorySystem != null)
        {
            inventorySystem.OnItemAdded -= HandleInventoryChanged;
            inventorySystem.OnItemRemoved -= HandleInventoryChanged;
        }
    }

    /// <summary>
    /// 通貨量が変更された時に呼び出されるイベントハンドラ。
    /// </summary>
    private void HandleCurrencyChanged(CurrencyType type, float newAmount, float delta)
    {
        // ショップで使用している通貨タイプの場合のみUIを更新
        if (type == this.currencyType)
        {
            UpdatePlayerCurrencyUI();
            // 通貨量が変わると購入/売却の可否が変わる可能性があるため、リスト表示を更新
            RefreshCurrentListVisuals(); // Initializeではなく表示更新のみ
        }
    }

    /// <summary>
    /// インベントリアイテムが追加/削除された時に呼び出されるイベントハンドラ。
    /// </summary>
    private void HandleInventoryChanged(Item item, int quantity, InventoryContainer container)
    {
        // 売却タブが表示されている場合のみリストを更新
        if (!isPurchaseTabActive)
        {
            // データリスト自体を更新
            UpdateSellableItemsList();
            // InfiniteScrollの表示を更新 (Initializeを呼ぶとスクロール位置がリセットされる可能性あり)
            GetActiveSellScroll()?.Initialize(playerSellableItems, itemPrefabHeight); // Initializeでリスト内容を反映
            // または RefreshVisibleItems を呼ぶ（データソース変更後のRefresh実装に依存）
            // GetActiveSellScroll()?.RefreshVisibleItems();
        }
        // 購入タブが表示されている場合でも、売却可能なアイテムのボタン状態が変わる可能性があるため、
        // 表示されているアイテムの再描画をトリガーするのが望ましい
        else
        {
            RefreshCurrentListVisuals();
        }
    }
    // InventoryManagerのイベントシグネチャに合わせたオーバーロード (ItemRemoved用など)
    private void HandleInventoryChanged(Item item, int quantity) { HandleInventoryChanged(item, quantity, null); }

    #endregion

    #region Tab Switching and List Population

    /// <summary>
    /// 表示するタブ（購入/売却）を切り替える。
    /// </summary>
    /// <param name="showPurchase">trueなら購入タブ、falseなら売却タブを表示。</param>
    private void SwitchTab(bool showPurchase)
    {
        isPurchaseTabActive = showPurchase;

        // タブボタンのインタラクト状態を更新（選択されているタブは非インタラクト）
        if (purchaseTabButton != null) purchaseTabButton.interactable = !showPurchase;
        if (sellTabButton != null) sellTabButton.interactable = showPurchase;

        // 対応するScroll Viewを制御
        if (useSeparateScrollViews)
        {
            purchaseScroll?.gameObject.SetActive(showPurchase);
            sellScroll?.gameObject.SetActive(!showPurchase);
            if (showPurchase)
            {
                purchaseScroll?.Initialize(shopSystem.shopItems, itemPrefabHeight);
            }
            else
            {
                UpdateSellableItemsList(); // 最新の売却可能リストを作成
                sellScroll?.Initialize(playerSellableItems, itemPrefabHeight);
            }
        }
        else // Scroll Viewを共有する場合
        {
            if (sellScroll != null) // sellScrollを共有ビューとして使用
            {
                sellScroll.gameObject.SetActive(true); // 常に表示
                if (showPurchase)
                {
                    // ShopItemDataのリストをobjectリストにキャストしてInitialize
                    sellScroll.Initialize(shopSystem.shopItems.Cast<object>().ToList(), itemPrefabHeight);
                }
                else
                {
                    UpdateSellableItemsList(); // 最新の売却可能リストを作成
                    sellScroll.Initialize(playerSellableItems, itemPrefabHeight); // objectリストをInitialize
                }
            }
        }

        // タブ切り替え時に前のフィードバックメッセージをクリア
        ClearFeedback();
    }

    /// <summary>
    /// プレイヤーのインベントリをスキャンし、ショップで売却可能なアイテムのリストを作成・更新する。
    /// </summary>
    private void UpdateSellableItemsList()
    {
        playerSellableItems.Clear(); // リストをクリア
        if (inventorySystem == null) return;

        // InventoryState を取得して全アイテムを集計
        InventoryState inventoryState = inventorySystem.GetInventoryState();
        if (inventoryState == null || inventoryState.containers == null) return;

        Dictionary<string, int> itemQuantities = new Dictionary<string, int>();
        foreach (var container in inventoryState.containers)
        {
            if (container?.slots == null) continue;
            foreach (var slot in container.slots)
            {
                if (slot?.item != null && slot.stackSize > 0)
                { string itemId = slot.item.itemId; itemQuantities[itemId] = itemQuantities.ContainsKey(itemId) ? itemQuantities[itemId] + slot.stackSize : slot.stackSize; }
            }
        }

        // 売却可能アイテムをリストに追加
        foreach (var pair in itemQuantities)
        {
            string itemId = pair.Key;
            int quantity = pair.Value;

            ShopItemData shopData = shopSystem.GetShopItemData(itemId);
            // ショップが買い取り可能（売却価格>0）なアイテムのみリストに追加
            if (shopData != null && shopData.sellPrice > 0)
            {
                Item itemDetails = inventorySystem.GetItem(itemId); // アイテム詳細を取得
                if (itemDetails != null)
                {
                    // 匿名型でItemオブジェクトと数量を格納
                    playerSellableItems.Add(new { Item = itemDetails, Quantity = quantity });
                }
                else { Debug.LogWarning($"UpdateSellableItemsList: Could not get Item details for itemId: {itemId}", this); }
            }
        }
        // 必要であればここでリストをソートする
        // playerSellableItems.Sort(...);
    }

    #endregion

    #region UI Interaction Handlers

    /// <summary>
    /// ShopItemUI の購入ボタンから呼び出される処理。
    /// </summary>
    /// <param name="itemId">購入するアイテムのID。</param>
    /// <param name="quantity">購入する数量。</param>
    public void HandlePurchaseRequest(string itemId, int quantity)
    {
        // --- 重要 ---
        // ShopSystem.BuyItem内の通貨処理がICurrencySystem.RemoveCurrencyを使うように
        // 修正されていることを確認してください。
        // --- /重要 ---
        bool success = shopSystem.BuyItem(itemId, quantity);

        if (success)
        {
            string itemName = shopSystem.GetShopItemData(itemId)?.displayName ?? itemId;
            ShowFeedback($"{itemName} を {quantity}個 購入しました。", false);
            // 在庫数が変わったため、購入リストの表示を更新
            if (isPurchaseTabActive) RefreshCurrentListVisuals(); // イベント経由ではなく即時更新が必要な場合
        }
        else
        {
            string reason = GetPurchaseFailureReason(itemId, quantity);
            ShowFeedback($"購入失敗: {reason}", true);
            // 購入可否状態が変わる可能性があるので、表示を更新
            if (isPurchaseTabActive) RefreshCurrentListVisuals();
        }
        // 通貨表示はイベントハンドラで更新される
    }

    /// <summary>
    /// ShopItemUI の売却ボタンから呼び出される処理。
    /// </summary>
    /// <param name="itemId">売却するアイテムのID。</param>
    /// <param name="quantity">売却する数量。</param>
    public void HandleSellRequest(string itemId, int quantity)
    {
        // --- 重要 ---
        // ShopSystem.SellItem内の通貨処理がICurrencySystem.AddCurrencyを
        // 正しい引数で使うように修正されていることを確認してください。
        // --- /重要 ---
        bool success = shopSystem.SellItem(itemId, quantity);

        if (success)
        {
            // Item名を取得 (InventoryManagerから取る方が確実かもしれない)
            string itemName = inventorySystem?.GetItem(itemId)?.name ?? shopSystem.GetShopItemData(itemId)?.displayName ?? itemId;
            ShowFeedback($"{itemName} を {quantity}個 売却しました。", false);
            // 売却リストの更新は HandleInventoryChanged イベント経由で行われるはず
        }
        else
        {
            string reason = GetSellFailureReason(itemId, quantity);
            ShowFeedback($"売却失敗: {reason}", true);
            // 売却可否状態が変わる可能性があるので、表示を更新
            if (!isPurchaseTabActive) RefreshCurrentListVisuals();
        }
        // 通貨表示はイベントハンドラで更新される
    }

    #endregion

    #region UI Update Methods

    /// <summary>
    /// プレイヤーの所持金表示を更新する。
    /// </summary>
    private void UpdatePlayerCurrencyUI()
    {
        if (playerCurrencyText != null && currencySystem != null)
        {
            float amount = currencySystem.GetCurrencyAmount(this.currencyType);
            // 通貨記号やフォーマットは適宜調整
            playerCurrencyText.text = $"所持金: {amount:N0} G";
        }
    }

    /// <summary>
    /// 現在表示されているリストのアイテム表示のみを更新する（リスト内容自体の変更はしない）。
    /// 購入/売却ボタンの Interactable 状態更新などに使用。
    /// </summary>
    private void RefreshCurrentListVisuals()
    {
        if (isPurchaseTabActive)
        {
            GetActivePurchaseScroll()?.RefreshVisibleItems();
        }
        else
        {
            GetActiveSellScroll()?.RefreshVisibleItems();
        }
    }

    /// <summary>
    /// 画面下部などにフィードバックメッセージを表示する。
    /// </summary>
    /// <param name="message">表示するメッセージ。</param>
    /// <param name="isError">エラーメッセージか（色分け用）。</param>
    private void ShowFeedback(string message, bool isError)
    {
        if (feedbackText == null) return;

        feedbackText.text = message;
        feedbackText.color = isError ? Color.red : Color.white; // エラーなら赤、通常は白
        feedbackText.gameObject.SetActive(true);

        // 既存の非表示コルーチンがあれば停止
        if (feedbackCoroutine != null) StopCoroutine(feedbackCoroutine);
        // 一定時間後にメッセージを非表示にするコルーチンを開始
        feedbackCoroutine = StartCoroutine(HideFeedbackAfterDelay(feedbackDisplayTime));
    }

    /// <summary>
    /// フィードバックメッセージを非表示にする。
    /// </summary>
    private void ClearFeedback()
    {
        if (feedbackText != null) feedbackText.gameObject.SetActive(false);
        if (feedbackCoroutine != null)
        {
            StopCoroutine(feedbackCoroutine);
            feedbackCoroutine = null;
        }
    }

    /// <summary>
    /// 指定時間後にフィードバックメッセージを非表示にするコルーチン。
    /// </summary>
    private IEnumerator HideFeedbackAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (feedbackText != null) feedbackText.gameObject.SetActive(false);
        feedbackCoroutine = null; // コルーチン完了
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// 指定されたコストの通貨をプレイヤーが所持しているか確認する。
    /// </summary>
    /// <param name="cost">必要なコスト。</param>
    /// <param name="type">確認する通貨のタイプ。</param>
    /// <returns>所持していれば true。</returns>
    public bool CanAfford(int cost, CurrencyType type)
    {
        return currencySystem != null && currencySystem.GetCurrencyAmount(type) >= cost;
    }

    /// <summary>
    /// 指定されたアイテムIDのショップでの売却価格を取得する。
    /// </summary>
    /// <param name="itemId">アイテムID。</param>
    /// <returns>売却価格。ショップが買い取らない場合は0。</returns>
    public int GetSellPrice(string itemId)
    {
        ShopItemData data = shopSystem?.GetShopItemData(itemId);
        return data?.sellPrice ?? 0; // shopSystemがnullの場合も考慮
    }

    /// <summary>
    /// 購入失敗時の理由を判定して文字列を返す（簡易版）。
    /// </summary>
    private string GetPurchaseFailureReason(string itemId, int quantity)
    {
        if (shopSystem == null) return "ショップシステムエラー。";
        ShopItemData itemData = shopSystem.GetShopItemData(itemId);
        if (itemData == null) return "アイテムデータが見つかりません。";
        if (itemData.maxStock >= 0 && itemData.currentStock < quantity) return "ショップの在庫が不足しています。";
        if (!CanAfford(itemData.buyPrice * quantity, currencyType)) return "所持金が不足しています。";
        // IInventorySystem の AddItem の事前検証 (重量/スロット上限など) が必要なら追加
        return "不明な理由。";
    }

    /// <summary>
    /// 売却失敗時の理由を判定して文字列を返す（簡易版）。
    /// </summary>
    private string GetSellFailureReason(string itemId, int quantity)
    {
        if (shopSystem == null) return "ショップシステムエラー。";
        ShopItemData itemData = shopSystem.GetShopItemData(itemId);
        if (itemData == null || itemData.sellPrice <= 0) return "このアイテムはショップでは買い取れません。";
        if (inventorySystem == null) return "インベントリシステムエラー。";
        int currentQuantity = inventorySystem.GetItemQuantity(itemId);
        if (currentQuantity < quantity) return "所持しているアイテム数が不足しています。";
        // ICurrencySystem の AddCurrency の事前検証 (通貨上限など) が必要なら追加
        return "不明な理由。";
    }

    /// <summary>
    /// 現在アクティブな購入用 InfiniteScroll コンポーネントを取得する。
    /// </summary>
    private InfiniteScroll<ShopItemData> GetActivePurchaseScroll()
    {
        if (useSeparateScrollViews) return purchaseScroll;
        // 共有ビューの場合は適切にキャストする必要があるが、
        // このメソッドは購入タブ時のみ呼ばれる想定なので、
        // sellScroll が ShopItemData を扱っていると仮定するか、別途処理が必要。
        // 現状の実装では purchaseScroll を返すのが無難。
        return purchaseScroll; // Separate のみを想定
    }

    /// <summary>
    /// 現在アクティブな売却用 InfiniteScroll コンポーネントを取得する。
    /// </summary>
    private InfiniteScroll<object> GetActiveSellScroll()
    {
        return sellScroll; // SeparateでもSharedでも sellScroll を返す
    }

    #endregion

    #region Potential Enhancements (Comments)
    // - アイテム詳細表示パネルの実装
    // - 数量指定での購入/売却機能
    // - アイテムリストのフィルタリング/ソート機能
    // - 購入/売却前の確認ダイアログ表示
    // - 効果音や視覚的なフィードバックの追加
    // - ShopSystemからのより詳細なエラー/成功情報の取得と表示
    // - ShopItemUIへのコントローラー参照のより安全な渡し方（Initialize時など）
    #endregion
}