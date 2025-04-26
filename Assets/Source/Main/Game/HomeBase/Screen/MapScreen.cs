using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// MapScreen.cs
/// 地域選択 → スポット選択 → スポットの詳細表示 → 次画面へ遷移 の流れを管理するクラス。
/// 継承：BaseScreen
/// InfiniteScroll<T> を利用し、ジェネリックに地域/スポット一覧を管理。
/// </summary>
public class MapScreen : BaseScreen
{
    [Header("マップデータ (ScriptableObject)")]
    [SerializeField] private MapData mapData;

    [Header("InfiniteScroll 参照")]
    // ※ ヒエラルキー上に配置してある InfiniteScroll<RegionData>, InfiniteScroll<SpotData> コンポーネントを
    //    インスペクターでアタッチしておいてください。
    [SerializeField] private InfiniteScroll<RegionData> regionScroll;
    [SerializeField] private InfiniteScroll<SpotData> spotScroll;

    [Header("ItemPrefab の高さ (InfiniteScrollに渡す)")]
    [SerializeField] private float regionItemPrefabHeight = 100f;
    [SerializeField] private float spotItemPrefabHeight = 100f;

    [Header("地域詳細表示UI")]
    [SerializeField] private Image regionImage;
    [SerializeField] private Text regionTitle;

    [Header("スポット詳細表示UI")]
    [SerializeField] private Image spotImage;
    [SerializeField] private Text spotTitle;
    [SerializeField] private Text spotDescription;

    [Header("戻るボタン")]
    [SerializeField] private Button backButton;

    // 選択状態の地域・スポット
    private int selectedRegionIndex = -1;
    private int selectedSpotIndex = -1;

    /// <summary>
    /// ShowScreen: 画面の初期表示時に呼ばれる。
    /// </summary>
    public override void OnShow()
    {
        base.OnShow();

        // 戻るボタンのリスナーを設定
        if (backButton != null)
        {
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(OnBackButtonClicked);
        }

        // 初期状態は「地域選択モード」
        SwitchToRegionSelection();
    }

    /// <summary>
    /// 地域選択状態に切り替える。
    /// </summary>
    private void SwitchToRegionSelection()
    {
        // スクロール切り替え
        regionScroll.gameObject.SetActive(true);
        spotScroll.gameObject.SetActive(false);

        // 戻るボタン非表示（地域一覧に戻る必要がないため）
        if (backButton != null)
        {
            backButton.gameObject.SetActive(false);
        }

        // スポット表示をクリア
        ClearSpotDisplay();

        // 地域一覧をInfiniteScrollで初期化
        InitializeRegionScroll();
    }

    /// <summary>
    /// スポット選択状態に切り替える。
    /// </summary>
    private void SwitchToSpotSelection()
    {
        regionScroll.gameObject.SetActive(false);
        spotScroll.gameObject.SetActive(true);

        // 戻るボタンを表示
        if (backButton != null)
        {
            backButton.gameObject.SetActive(true);
        }

        // スポット表示クリア
        ClearSpotDisplay();

        // 選択中の地域が正しい範囲内なら、スポット一覧を初期化
        if (selectedRegionIndex >= 0 && selectedRegionIndex < mapData.Regions.Count)
        {
            RegionData region = mapData.Regions[selectedRegionIndex];
            InitializeSpotScroll(region);
        }
    }

    /// <summary>
    /// 地域一覧をInfiniteScrollで表示初期化する。
    /// </summary>
    private void InitializeRegionScroll()
    {
        if (mapData == null || mapData.Regions == null)
        {
            Debug.LogError("[MapScreen] MapData or Regions is not assigned.");
            return;
        }

        // クリックコールバックを設定 (毎回再設定)
        regionScroll.OnItemClicked = OnRegionSelected;
        // データを初期化してスクロールに反映
        regionScroll.Initialize(mapData.Regions, regionItemPrefabHeight);

        // 一覧を一番上までスクロールし、表示更新
        regionScroll.RefreshVisibleItems();
    }

    /// <summary>
    /// スポット一覧をInfiniteScrollで表示初期化する。
    /// </summary>
    private void InitializeSpotScroll(RegionData regionData)
    {
        if (regionData == null || regionData.spots == null)
        {
            Debug.LogError("[MapScreen] RegionData or spots is null.");
            return;
        }

        spotScroll.OnItemClicked = OnSpotSelected;
        spotScroll.Initialize(regionData.spots, spotItemPrefabHeight);

        spotScroll.RefreshVisibleItems();
    }

    /// <summary>
    /// 地域がクリックされた際の処理。
    /// </summary>
    private void OnRegionSelected(int index)
    {
        if (mapData == null || mapData.Regions == null) return;
        if (index < 0 || index >= mapData.Regions.Count) return;

        selectedRegionIndex = index;
        RegionData region = mapData.Regions[index];

        // 地域詳細情報を表示
        regionTitle.text = region.regionName;
        regionImage.sprite = region.regionIcon;

        // 地域選択後 → スポット選択画面へ
        SwitchToSpotSelection();
    }

    /// <summary>
    /// スポットがクリックされた際の処理。
    /// </summary>
    private void OnSpotSelected(int index)
    {
        if (selectedRegionIndex < 0 || selectedRegionIndex >= mapData.Regions.Count) return;
        var region = mapData.Regions[selectedRegionIndex];
        if (index < 0 || index >= region.spots.Count) return;

        selectedSpotIndex = index;
        SpotData spot = region.spots[index];

        // スポット詳細情報を表示
        spotTitle.text = spot.spotName;
        spotImage.sprite = spot.spotIcon;
        spotDescription.text = spot.description;

        // 選択後、次の画面へ
        ProceedToNextScreen();
    }

    /// <summary>
    /// スポット表示をクリアする。
    /// </summary>
    private void ClearSpotDisplay()
    {
        spotTitle.text = string.Empty;
        spotImage.sprite = null;
        spotDescription.text = string.Empty;
    }

    /// <summary>
    /// 次の画面に遷移させる処理。
    /// </summary>
    private void ProceedToNextScreen()
    {
        Debug.Log($"[MapScreen] Spot selected: {spotTitle.text}  →  次の画面へ遷移する処理を実装してください。");
        // TODO: 実際のシーン遷移や画面切り替えを実装
    }

    /// <summary>
    /// 戻るボタンが押された時。
    /// スポット選択状態から地域選択状態へ戻る。
    /// </summary>
    private void OnBackButtonClicked()
    {
        SwitchToRegionSelection();
    }
}


/// <summary>
/// マップ情報全体を管理するScriptableObject。
/// Unity上で生成し、エディタから地域・スポットを登録する。
/// </summary>
[CreateAssetMenu(fileName = "MapData", menuName = "Game/MapData", order = 1)]
public class MapData : ScriptableObject
{
    public List<RegionData> Regions = new List<RegionData>();
}

/// <summary>
/// 地域ごとの情報を持つクラス。
/// </summary>
[System.Serializable]
public class RegionData
{
    public string regionName;
    public Sprite regionIcon;
    public List<SpotData> spots = new List<SpotData>();
}

/// <summary>
/// スポットの情報を持つクラス。
/// </summary>
[System.Serializable]
public class SpotData
{
    public string spotName;
    public Sprite spotIcon;
    [TextArea]
    public string description;
}
