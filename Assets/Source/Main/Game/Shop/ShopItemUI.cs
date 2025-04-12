// ShopItemUI.cs (購入/売却兼用、IInfiniteScrollItem実装)
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using ResourceManagement; // 名前空間を追加

// 仮のプレイヤーインベントリアイテム情報クラス (InventoryManagerから取得するデータ形式に合わせる)
public class PlayerInventoryItemInfo
{
    public string itemId;
    public string displayName;
    public int quantity;
    public Sprite icon; // アイコン情報があれば
    // 必要に応じて Item クラスや他の情報への参照
}


// InfiniteScroll用のインターフェースを実装
// データ型としてobjectを使い、Setup内でキャストする
public class ShopItemUI : MonoBehaviour, IInfiniteScrollItem<object>
{
    [Header("UI References")]
    public TextMeshProUGUI itemNameText;
    public TextMeshProUGUI priceText; // 購入価格 or 売却価格を表示
    public Image itemIconImage;
    public TextMeshProUGUI stockOrQuantityText; // 在庫数(購入時) or 所持数(売却時)を表示
    public Button purchaseButton;
    public Button sellButton;

    [Header("Feedback Colors")]
    public Color insufficientFundsColor = Color.red;
    public Color defaultPriceColor = Color.white; // 通常の価格テキスト色

    private object currentItemData; // ShopItemData or PlayerInventoryItemInfo
    private int currentIndex;
    private Action<int> onClickCallback; // InfiniteScrollからのコールバック保持用
    private ShopUIController shopController; // ShopUIControllerへの参照

    private bool isSellMode = false; // 現在の表示モード

    void Awake()
    {
        if (purchaseButton != null) purchaseButton.onClick.AddListener(OnPurchaseButtonClick);
        if (sellButton != null) sellButton.onClick.AddListener(OnSellButtonClick);
    }

    // IInfiniteScrollItem<object> の実装
    public void Setup(object itemData, int index, Action<int> clickCallback)
    {
        // このSetupはInfiniteScrollから呼ばれるが、ShopUIControllerの参照が必要
        // InitializeでShopUIControllerを渡すか、FindObjectOfTypeなどで見つける必要がある
        // ここでは、外部からSetShopControllerが呼ばれることを期待する
        if (shopController == null)
        {
            //Debug.LogError("ShopController reference is not set in ShopItemUI!");
            // Fallback: シーンから探す (非推奨)
            shopController = FindObjectOfType<ShopUIController>();
            if (shopController == null) return; // 見つからなければ処理中断
        }

        currentItemData = itemData;
        currentIndex = index;
        onClickCallback = clickCallback; // 必要なら保持

        // データ型に応じて表示を振り分け
        if (itemData is ShopItemData shopData)
        {
            isSellMode = false;
            SetupForPurchase(shopData);
        }
        else if (itemData is PlayerInventoryItemInfo inventoryData)
        {
            isSellMode = true;
            SetupForSell(inventoryData);
        }
        else
        {
            // 不明なデータ型の場合は非表示にするなど
            gameObject.SetActive(false);
        }
    }

    // 外部から ShopUIController の参照を設定するためのメソッド
    public void SetShopController(ShopUIController controller)
    {
        this.shopController = controller;
    }


    // 購入モード用の設定
    private void SetupForPurchase(ShopItemData data)
    {
        if (itemNameText != null) itemNameText.text = data.displayName;
        if (priceText != null)
        {
            priceText.text = $"{data.buyPrice} G"; // 購入価格
                                                   // 通貨不足の場合の色変更
            bool canAfford = shopController.CanAfford(data.buyPrice, CurrencyType.StandardCurrency); // 通貨タイプ指定
            priceText.color = canAfford ? defaultPriceColor : insufficientFundsColor;
        }
        if (itemIconImage != null)
        {
            // アイコン設定 (データにあれば)
            // itemIconImage.sprite = data.iconSprite;
            itemIconImage.enabled = false; // 仮に無効
        }
        if (stockOrQuantityText != null)
        {
            if (data.maxStock < 0) stockOrQuantityText.text = "在庫: ∞";
            else stockOrQuantityText.text = $"在庫: {data.currentStock}/{data.maxStock}";
            stockOrQuantityText.enabled = true;
        }

        // ボタンの表示/非表示と有効状態
        if (purchaseButton != null)
        {
            purchaseButton.gameObject.SetActive(true);
            bool hasStock = data.maxStock < 0 || data.currentStock > 0;
            purchaseButton.interactable = shopController.CanAfford(data.buyPrice, CurrencyType.StandardCurrency) && hasStock;
        }
        if (sellButton != null) sellButton.gameObject.SetActive(false); // 売却ボタン非表示
    }

    // 売却モード用の設定
    private void SetupForSell(PlayerInventoryItemInfo data)
    {
        if (itemNameText != null) itemNameText.text = data.displayName;

        // ShopSystemからこのアイテムの売却価格を取得
        int sellPrice = shopController.GetSellPrice(data.itemId);

        if (priceText != null)
        {
            priceText.text = $"{sellPrice} G"; // 売却価格
            priceText.color = defaultPriceColor; // 売却時は通常色
        }
        if (itemIconImage != null)
        {
            // アイコン設定 (データにあれば)
            itemIconImage.sprite = data.icon;
            itemIconImage.enabled = (data.icon != null);
        }
        if (stockOrQuantityText != null)
        {
            stockOrQuantityText.text = $"所持: {data.quantity}"; // 所持数
            stockOrQuantityText.enabled = true;
        }

        // ボタンの表示/非表示と有効状態
        if (purchaseButton != null) purchaseButton.gameObject.SetActive(false); // 購入ボタン非表示
        if (sellButton != null)
        {
            sellButton.gameObject.SetActive(true);
            // 売却可能か (価格が0より大きいか、売却不可アイテムでないかなど)
            bool canSell = sellPrice > 0;
            sellButton.interactable = canSell && data.quantity > 0;
        }
    }


    private void OnPurchaseButtonClick()
    {
        if (!isSellMode && shopController != null && currentItemData is ShopItemData shopData)
        {
            shopController.HandlePurchaseRequest(shopData.itemId, 1); // 数量1で購入
            onClickCallback?.Invoke(currentIndex); // InfiniteScrollにも通知
        }
    }

    private void OnSellButtonClick()
    {
        if (isSellMode && shopController != null && currentItemData is PlayerInventoryItemInfo inventoryData)
        {
            shopController.HandleSellRequest(inventoryData.itemId, 1); // 数量1で売却
            onClickCallback?.Invoke(currentIndex); // InfiniteScrollにも通知
        }
    }

    // InfiniteScrollから呼ばれるSetupのために、GetCurrentIndexのようなメソッドが必要になる場合がある
    public int GetCurrentIndex() { return currentIndex; }
}