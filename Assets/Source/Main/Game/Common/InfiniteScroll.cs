// InfiniteScroll.cs (Enhanced with OnItemCreated event)
// NOTE: This file replaces / extends the previous generic InfiniteScroll<T>
// so that external systems (e.g. UIController) can hook into item instantiation.
// ---------------------------------------------------------------
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;

// Required interface for item prefabs
public interface IInfiniteScrollItem<T>
{
    void Setup(T itemData, int index, Action<int> onClickCallback);
}

[RequireComponent(typeof(ScrollRect))]
public class InfiniteScroll<T> : MonoBehaviour
{
    #region Inspector
    [Header("References")]
    [SerializeField] private RectTransform content;
    [SerializeField] private GameObject slotItemPrefab;

    [Header("Behaviour")]
    [SerializeField] private float itemHeight = 100f;
    [SerializeField, Tooltip("Number of extra rows rendered above / below the viewport")] private int renderBuffer = 3;
    #endregion

    #region Events
    /// <summary>
    /// Fired the FIRST time a pooled item is instantiated.
    /// <para>Arguments: (instantiated MonoBehaviour component, dataIndex or -1 if unknown yet)</para>
    /// You can use this to inject additional dependencies (e.g. controller references)
    /// that only need to happen once per prefab instance.
    /// </summary>
    public Action<MonoBehaviour, int> OnItemCreated;

    /// <summary>
    /// Fired when an item GameObject is clicked (index passed from the item)
    /// </summary>
    public Action<int> OnItemClicked;
    #endregion

    #region Private fields
    private IList<T> dataList;
    private readonly List<MonoBehaviour> itemsPool = new();
    private readonly HashSet<MonoBehaviour> initialisedItems = new();

    private ScrollRect scrollRect;
    private float previousScrollPos;
    private Coroutine updateCoroutine;

    private int firstVisibleIndex = -1;
    private int lastVisibleIndex = -1;
    #endregion

    #region Unity
    private void Awake()
    {
        scrollRect = GetComponent<ScrollRect>();
        if (!content) content = scrollRect.content;

        if (slotItemPrefab == null)
        {
            Debug.LogError("[InfiniteScroll] SlotItemPrefab is not assigned!");
            enabled = false;
            return;
        }
        if (slotItemPrefab.GetComponent<IInfiniteScrollItem<T>>() == null)
        {
            Debug.LogError($"[InfiniteScroll] Prefab {slotItemPrefab.name} must implement IInfiniteScrollItem<{typeof(T).Name}>.");
        }

        scrollRect.onValueChanged.AddListener(_ => ScheduleUpdate(false));
    }
    #endregion

    #region Public API
    public void Initialize(IList<T> data, float prefabHeight)
    {
        StopUpdateCoroutine();

        // Flush old pool
        foreach (var itm in itemsPool)
        {
            if (itm != null) Destroy(itm.gameObject);
        }
        itemsPool.Clear();
        initialisedItems.Clear();

        dataList = data;
        itemHeight = prefabHeight;

        // Resize content
        content.sizeDelta = dataList == null || dataList.Count == 0
            ? new Vector2(content.sizeDelta.x, 0)
            : new Vector2(content.sizeDelta.x, CalculateContentHeight());

        scrollRect.verticalNormalizedPosition = 1f;
        previousScrollPos = 1f;
        firstVisibleIndex = lastVisibleIndex = -1;

        updateCoroutine = StartCoroutine(UpdateVisibleItemsCoroutine(true));
    }

    /// <summary>
    /// Force refresh of currently visible items.
    /// </summary>
    public void RefreshVisibleItems() => ScheduleUpdate(true);
    #endregion

    #region Internal helpers
    private void ScheduleUpdate(bool force)
    {
        if (updateCoroutine == null)
            updateCoroutine = StartCoroutine(UpdateVisibleItemsCoroutine(force));
    }

    private float CalculateContentHeight()
    {
        float total = dataList.Count * itemHeight;
        if (content.TryGetComponent(out VerticalLayoutGroup vlg))
        {
            total += (dataList.Count - 1) * vlg.spacing + vlg.padding.top + vlg.padding.bottom;
        }
        return total;
    }

    private IEnumerator UpdateVisibleItemsCoroutine(bool forceUpdate)
    {
        // small delay to gather scroll changes
        yield return null;

        if (dataList == null || dataList.Count == 0) { updateCoroutine = null; yield break; }

        float normPos = scrollRect.verticalNormalizedPosition;
        if (!forceUpdate && Mathf.Abs(normPos - previousScrollPos) < 0.001f && firstVisibleIndex != -1)
        { updateCoroutine = null; yield break; }
        previousScrollPos = normPos;

        float contentH = content.rect.height;
        float viewportH = scrollRect.viewport.rect.height;
        float scrollable = Mathf.Max(0f, contentH - viewportH);

        float viewportTop = (1f - normPos) * scrollable;
        float viewportBottom = viewportTop + viewportH;

        int newFirst = Mathf.Max(0, Mathf.FloorToInt(viewportTop / itemHeight) - renderBuffer);
        int newLast = Mathf.Min(dataList.Count - 1, Mathf.CeilToInt(viewportBottom / itemHeight) - 1 + renderBuffer);

        if (!forceUpdate && newFirst == firstVisibleIndex && newLast == lastVisibleIndex)
        { updateCoroutine = null; yield break; }

        HashSet<int> want = new();
        for (int i = newFirst; i <= newLast; i++) want.Add(i);

        // Reactivate / deactivate
        List<MonoBehaviour> toDisable = new();
        Dictionary<int, MonoBehaviour> active = new();

        foreach (var itm in itemsPool)
        {
            if (itm == null) continue;
            if (!itm.gameObject.activeSelf) continue;

            if (!int.TryParse(itm.gameObject.name.Replace("Item_", ""), out int idx)) { continue; }

            if (want.Contains(idx)) { active[idx] = itm; want.Remove(idx); }
            else { toDisable.Add(itm); }
        }

        foreach (var d in toDisable) d.gameObject.SetActive(false);

        foreach (int idx in want)
        {
            var itm = GetOrCreateItem();
            UpdateItem(itm, idx);
            active[idx] = itm;
        }

        firstVisibleIndex = newFirst;
        lastVisibleIndex = newLast;
        updateCoroutine = null;
    }

    private MonoBehaviour GetOrCreateItem()
    {
        foreach (var itm in itemsPool)
        {
            if (!itm.gameObject.activeSelf)
            {
                itm.gameObject.SetActive(true);
                return itm;
            }
        }

        GameObject go = Instantiate(slotItemPrefab, content);
        var comp = go.GetComponent<MonoBehaviour>();
        itemsPool.Add(comp);

        // Fire creation event (index unknown yet: -1)
        OnItemCreated?.Invoke(comp, -1);
        return comp;
    }

    private void UpdateItem(MonoBehaviour item, int idx)
    {
        if (item == null) return;
        item.gameObject.name = $"Item_{idx}";

        // position if not using layout group
        if (!content.TryGetComponent<VerticalLayoutGroup>(out _))
        {
            if (item.TryGetComponent(out RectTransform rt))
            {
                rt.anchoredPosition = new Vector2(0f, -(idx * itemHeight) - itemHeight * 0.5f);
            }
        }

        if (item.TryGetComponent(out IInfiniteScrollItem<T> scrollItem))
        {
            scrollItem.Setup(dataList[idx], idx, HandleItemClick);
        }

        // First?time initialisation callback (with valid index)
        if (!initialisedItems.Contains(item))
        {
            initialisedItems.Add(item);
            OnItemCreated?.Invoke(item, idx);
        }
    }

    private void HandleItemClick(int idx) => OnItemClicked?.Invoke(idx);

    private void StopUpdateCoroutine()
    {
        if (updateCoroutine != null)
        {
            StopCoroutine(updateCoroutine);
            updateCoroutine = null;
        }
    }

    private void OnDisable() => StopUpdateCoroutine();

    #endregion
}