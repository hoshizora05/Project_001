// =============================
// InventoryItemUI.cs
// =============================
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using ResourceManagement;

/// <summary>
/// A single inventory slot representation for <see cref="InfiniteScroll{T}"/>.
/// Displays the item icon, name, quantity stack and exposes Use / Drop buttons.
/// </summary>
public class InventoryItemUI : MonoBehaviour, IInfiniteScrollItem<InventoryItemUI.InventorySlotData>
{
    #region nested
    /// <summary>
    /// Light‑weight data structure passed from <see cref="InventoryUIController"/> to the scroll list.
    /// We decouple the huge <see cref="InventoryContainer"/> and Item objects so that the UI list can remain
    /// completely POCO‑friendly and serialisation‑safe.
    /// </summary>
    [Serializable]
    public struct InventorySlotData
    {
        public string containerId;
        public int slotIndex;
        public string itemId;
        public string displayName;
        public int quantity;
        public Sprite icon;
        public bool isEquipped;
        public bool isLocked;
    }
    #endregion

    #region inspector
    [Header("UI References")]
    [SerializeField] private Image _iconImage;
    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private TextMeshProUGUI _quantityText;
    [SerializeField] private Button _useButton;
    [SerializeField] private Button _dropButton;
    [SerializeField] private Image _equippedMarkImage;
    [SerializeField] private Color _equippedColor = Color.yellow;
    #endregion

    private InventorySlotData _data;
    private Action<int> _onClickCallback;
    private InventoryUIController _controller;
    private int _currentIndex;

    private void Awake()
    {
        if (_useButton) _useButton.onClick.AddListener(OnUseClicked);
        if (_dropButton) _dropButton.onClick.AddListener(OnDropClicked);
    }

    /// <summary>
    /// Called by <see cref="InfiniteScroll{T}"/>.
    /// </summary>
    public void Setup(InventorySlotData data, int index, Action<int> clickCallback)
    {
        _data = data;
        _currentIndex = index;
        _onClickCallback = clickCallback;
        RefreshVisuals();
    }

    /// <summary>
    /// Called by <see cref="InventoryUIController"/> right after the prefab is created so the list item
    /// has a back reference to the controller without relying on <c>FindObjectOfType</c>.
    /// </summary>
    public void SetController(InventoryUIController controller) => _controller = controller;

    private void RefreshVisuals()
    {
        if (_iconImage)
        {
            _iconImage.sprite = _data.icon;
            _iconImage.enabled = _data.icon;
        }
        if (_nameText) _nameText.text = _data.displayName;
        if (_quantityText) _quantityText.text = _data.quantity > 1 ? $"x{_data.quantity}" : string.Empty;
        if (_equippedMarkImage)
        {
            _equippedMarkImage.enabled = _data.isEquipped;
            _equippedMarkImage.color = _equippedColor;
        }
        if (_useButton) _useButton.interactable = !_data.isLocked;
        if (_dropButton) _dropButton.interactable = !_data.isLocked;
    }

    private void OnUseClicked()
    {
        _controller?.HandleUseRequest(_data);
    }

    private void OnDropClicked()
    {
        _controller?.HandleDropRequest(_data);
    }
}