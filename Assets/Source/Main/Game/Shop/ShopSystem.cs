using System;
using System.Collections.Generic;
using UnityEngine;
using ResourceManagement;

/// <summary>
/// ショップで取り扱うアイテム情報。
/// 在庫管理をする場合には maxStock / currentStock を使用する。
/// 無制限に販売したい場合は maxStock = -1, currentStock = -1 などで扱う。
/// </summary>
[Serializable]
public class ShopItemData
{
    [Tooltip("アイテムの一意なID (IInventorySystemで扱うIDと対応させる)")]
    public string itemId;

    [Tooltip("UI表示用の名前など")]
    public string displayName;

    [Tooltip("購入単価")]
    public int buyPrice;

    [Tooltip("売却単価 (店が買い取る際の価格)")]
    public int sellPrice;

    [Tooltip("在庫の上限(-1なら無制限)")]
    public int maxStock = -1;

    [Tooltip("現在の在庫数(-1なら無制限)")]
    public int currentStock = -1;
}

/// <summary>
/// ショップシステム: ShopItemData のリストを元にゲーム内の売買を行う。
/// 必要なシステムは ServiceLocator 経由で取得する。
/// </summary>
public class ShopSystem : MonoBehaviour
{
    [Header("Shop Item List")]
    [Tooltip("ショップで取り扱うアイテム情報")]
    public List<ShopItemData> shopItems = new List<ShopItemData>();

    // Reference to your overall resource management system.
    private ICurrencySystem currencySystem;
    private IInventorySystem inventorySystem;
    private IResourceLogger logger;

    /// <summary>
    /// ショップシステムの初期化。
    /// 必要であれば、ここで在庫の初期化処理などを行う。
    /// </summary>
    private void Awake()
    {
        // 必要があれば、在庫数の初期設定などを行う
        // 例: currentStock が -1の場合は在庫無制限
        Debug.Log("[ShopSystem] Awake: ショップシステム初期化完了。");
    }
    public void InitializeShop(ResourceManagementSystem resourceManagementSystem)
    {
        if (resourceManagementSystem == null)
        {
            Debug.LogError("[ShopSystem] ResourceManagementSystem is null. Cannot initialize ShopSystem.");
            return;
        }

        currencySystem = ServiceLocator.Get<ICurrencySystem>();
        inventorySystem = ServiceLocator.Get<IInventorySystem>();
        logger = ServiceLocator.Get<IResourceLogger>();  // If logging is required

        // Additional setup logic here if needed
        Debug.Log("[ShopSystem] Shop system initialized successfully.");
    }

    /// <summary>
    /// アイテムを購入する。
    /// </summary>
    /// <param name="itemId">購入するアイテムID</param>
    /// <param name="quantity">購入する数量</param>
    /// <returns>購入に成功したかどうか</returns>
    public bool BuyItem(string itemId, int quantity)
    {
        if (quantity <= 0)
        {
            Debug.LogWarning("[ShopSystem] BuyItem: 購入数量が0以下です。");
            return false;
        }

        if (currencySystem == null || inventorySystem == null)
        {
            Debug.LogError("[ShopSystem] BuyItem: 必要なシステムを取得できませんでした。");
            return false;
        }

        // ショップアイテム情報を検索
        ShopItemData shopItem = shopItems.Find(item => item.itemId == itemId);
        if (shopItem == null)
        {
            Debug.LogWarning($"[ShopSystem] BuyItem: ショップに存在しないアイテムID ({itemId}) です。");
            return false;
        }

        // 在庫チェック (maxStock が -1なら無制限とみなす)
        if (shopItem.maxStock > 0 && shopItem.currentStock < quantity)
        {
            Debug.LogWarning($"[ShopSystem] BuyItem: 在庫不足のため購入不可。要求数:{quantity}, 在庫:{shopItem.currentStock}");
            return false;
        }

        // トータル価格計算
        int totalCost = shopItem.buyPrice * quantity;

        // 所持金確認
        float playerCurrency = currencySystem.GetCurrency(CurrencyType.StandardCurrency).currentAmount; // 実装に合わせて取得メソッドが違う場合修正
        if (playerCurrency < totalCost)
        {
            Debug.LogWarning($"[ShopSystem] BuyItem: 所持金不足のため購入不可。必要:{totalCost}, 所持:{playerCurrency}");
            return false;
        }

        // 通貨消費
        currencySystem.RemoveCurrency(CurrencyType.StandardCurrency, totalCost, "ShopPurchase", $"Purchased {quantity}x {itemId}");

        // インベントリに追加
        bool addResult = inventorySystem.AddItem(itemId, quantity);
        if (!addResult)
        {
            // 失敗したら通貨を戻すなどのロールバック
            currencySystem.AddCurrency(CurrencyType.StandardCurrency, totalCost, "ShopPurchaseRevert", $"Reverted purchase cost for {quantity}x {itemId}");
            Debug.LogError($"[ShopSystem] BuyItem: インベントリ追加に失敗しました。通貨をロールバックします。");
            return false;
        }

        // 在庫を減らす
        if (shopItem.maxStock > 0)
        {
            shopItem.currentStock -= quantity;
        }

        // ログ
        logger?.LogSystemMessage($"[ShopSystem] プレイヤーが {shopItem.displayName} (ID:{itemId}) を {quantity} 個購入 (合計 {totalCost} )");

        // 成功
        return true;
    }

    /// <summary>
    /// アイテムを売却する。
    /// </summary>
    /// <param name="itemId">売却するアイテムID</param>
    /// <param name="quantity">売却する数量</param>
    /// <returns>売却に成功したかどうか</returns>
    public bool SellItem(string itemId, int quantity)
    {
        if (quantity <= 0)
        {
            Debug.LogWarning("[ShopSystem] SellItem: 売却数量が0以下です。");
            return false;
        }

        if (currencySystem == null || inventorySystem == null)
        {
            Debug.LogError("[ShopSystem] SellItem: 必要なシステムを取得できませんでした。");
            return false;
        }

        // ショップアイテム情報を検索
        ShopItemData shopItem = shopItems.Find(item => item.itemId == itemId);
        if (shopItem == null)
        {
            Debug.LogWarning($"[ShopSystem] SellItem: ショップに存在しないアイテムID ({itemId}) です。");
            return false;
        }

        // プレイヤーが十分にアイテムを持っているか確認
        int playerItemCount = inventorySystem.GetItemQuantity(itemId);
        if (playerItemCount < quantity)
        {
            Debug.LogWarning($"[ShopSystem] SellItem: 売却しようとするアイテム数が所持数を超えています。所持:{playerItemCount}, 要求:{quantity}");
            return false;
        }

        // 売却価格計算
        int totalGain = shopItem.sellPrice * quantity;

        // インベントリからアイテムを減らす
        bool removeResult = inventorySystem.RemoveItem(itemId, quantity);
        if (!removeResult)
        {
            Debug.LogError($"[ShopSystem] SellItem: インベントリからアイテムを削除できませんでした。");
            return false;
        }

        // 通貨を加算
        currencySystem.AddCurrency(CurrencyType.StandardCurrency,totalGain, "ShopSell", $"Sold {quantity}x {itemId}");

        // 店の在庫を増やす（在庫を管理する場合のみ）
        // maxStockが -1 で無制限なら加算しなくても良いが、店に買い取り在庫を持たせたい場合は加算
        if (shopItem.maxStock > 0)
        {
            shopItem.currentStock += quantity;
            if (shopItem.currentStock > shopItem.maxStock)
            {
                shopItem.currentStock = shopItem.maxStock;
            }
        }

        // ログ
        logger?.LogSystemMessage($"[ShopSystem] プレイヤーが {shopItem.displayName} (ID:{itemId}) を {quantity} 個売却 (獲得 {totalGain} )");

        // 成功
        return true;
    }

    /// <summary>
    /// ショップアイテムのデータを取得する補助メソッド。
    /// </summary>
    /// <param name="itemId">取得したいアイテムID</param>
    /// <returns>該当するShopItemData (なければnull)</returns>
    public ShopItemData GetShopItemData(string itemId)
    {
        return shopItems.Find(item => item.itemId == itemId);
    }

    /// <summary>
    /// ショップUIのリフレッシュなどで呼び出す想定の補助メソッド。
    /// </summary>
    public void RefreshShopUI()
    {
        // 例: ショップUIを再描画する処理など
        Debug.Log("[ShopSystem] RefreshShopUI: ショップ情報を更新しました。");
        // 実際のUI処理はプロジェクト側で実装
    }
}
