
// =============================
// InventoryUIController.cs
// =============================
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using ResourceManagement;
using System.Linq;

/// <summary>
/// Manages the entire inventory screen – populates an <see cref="InfiniteScroll{T}"/> with items,
/// reacts to <see cref="InventoryManager"/> events and provides context actions.
/// Inspired by the ShopUIController implementation. citeturn0file4
/// </summary>
public class InventoryUIController : MonoBehaviour
{
    #region inspector
    [Header("Scroll View")]
    [SerializeField] private InfiniteScroll<InventoryItemUI.InventorySlotData> _scroll;
    [SerializeField] private float _itemPrefabHeight = 90f;

    [Header("Details Pane")] // (optional extra UI for selected item)
    [SerializeField] private TextMeshProUGUI _selectedNameText;
    [SerializeField] private TextMeshProUGUI _selectedDescriptionText;
    [SerializeField] private Image _selectedIconImage;
    [SerializeField] private Button _equipButton;
    #endregion

    private IInventorySystem _inventorySystem;
    private Dictionary<(string containerId, int slot), InventoryItemUI.InventorySlotData> _cachedListData = new();

    private void Start()
    {
        // Locate subsystem
        _inventorySystem = ServiceLocator.Get<IInventorySystem>();
        if (_inventorySystem == null)
        {
            Debug.LogError("[InventoryUI] IInventorySystem not found – did you initialise ResourceManagementSystem first?");
            enabled = false;
            return;
        }

        SubscribeToEvents();
        RebuildItemList();
    }

    private void OnEnable() => SubscribeToEvents();
    private void OnDisable() => UnsubscribeFromEvents();

    private void SubscribeToEvents()
    {
        if (_inventorySystem == null) return;
        _inventorySystem.OnItemAdded += HandleInventoryChanged;
        _inventorySystem.OnItemRemoved += HandleInventoryChanged;
        _inventorySystem.OnItemEquipped += HandleItemEquipped;
        _inventorySystem.OnItemUnequipped += HandleItemUnequipped;
    }

    private void UnsubscribeFromEvents()
    {
        if (_inventorySystem == null) return;
        _inventorySystem.OnItemAdded -= HandleInventoryChanged;
        _inventorySystem.OnItemRemoved -= HandleInventoryChanged;
        _inventorySystem.OnItemEquipped -= HandleItemEquipped;
        _inventorySystem.OnItemUnequipped -= HandleItemUnequipped;
    }

    #region list_building
    private void RebuildItemList()
    {
        _cachedListData.Clear();

        var state = _inventorySystem.GetInventoryState(); // extension method expected
        int index = 0;
        foreach (var container in state.containers)
        {
            foreach (var slot in container.slots)
            {
                if (slot.item == null) continue;

                var data = new InventoryItemUI.InventorySlotData
                {
                    containerId = container.containerId,
                    slotIndex = slot.slotIndex,
                    itemId = slot.item.itemId,
                    displayName = slot.item.name,
                    quantity = slot.stackSize,
                    icon = null,//slot.item.iconSprite, // assuming property exists
                    isEquipped = false, //state.equipped.Any(e => e.itemId == slot.item.itemId),
                    isLocked = slot.isLocked,
                };

                _cachedListData.Add((container.containerId, slot.slotIndex), data);
            }
        }

        var list = _cachedListData.Values.ToList();
        _scroll.Initialize(list, _itemPrefabHeight);

        // Provide back‑reference from instantiated items to controller
        _scroll.OnItemCreated += (slotUI , index) =>
        {
            if (slotUI is InventoryItemUI ui)
                ui.SetController(this);
        };
    }
    #endregion

    #region inventory_event_handlers
    private void HandleInventoryChanged(Item item, int qty, InventoryContainer container)
    {
        RebuildItemList();
    }

    private void HandleItemEquipped(Item item, EquipmentSlotType slot) => RebuildItemList();
    private void HandleItemUnequipped(EquipmentSlotType slot) => RebuildItemList();
    #endregion

    #region public_callbacks_from_item_ui
    internal void HandleUseRequest(InventoryItemUI.InventorySlotData data)
    {
        _inventorySystem.UseItem(data.itemId, data.containerId);
    }

    internal void HandleDropRequest(InventoryItemUI.InventorySlotData data)
    {
        bool ok = _inventorySystem.RemoveItem(data.itemId, 1, data.containerId);
        if (!ok) Debug.LogWarning($"[InventoryUI] Failed to remove {data.itemId}");
    }
    #endregion

    #region selection_details
    /// <summary>
    /// Optional – show extra info when an item is clicked in the list.
    /// Hook this up via InfiniteScroll's click callback if desired.
    /// </summary>
    private void ShowDetails(InventoryItemUI.InventorySlotData data)
    {
        if (_selectedNameText) _selectedNameText.text = data.displayName;
        if (_selectedIconImage) _selectedIconImage.sprite = data.icon;
        // Description could be fetched from ItemDatabase
    }
    #endregion
}

// =============================
// Notes
// =============================
// • The controller mirrors the architecture of ShopUIController (event subscription, ServiceLocator access,
//   InfiniteScroll population, feedback hooks) to provide a consistent user experience. citeturn0file4
// • Inventory events are raised by InventoryManager allowing real‑time UI updates. citeturn0file0
// • For brevity, error handling and null‑checks are simplified; production code should be more defensive.
// • Split the two classes above into their own .cs files in your Unity project and assign the necessary UI
//   references in the Inspector. Attach InventoryUIController to a Canvas prefab and register the
//   InfiniteScroll component just like the shop UI.
