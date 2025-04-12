// InfiniteScroll.cs (�ėp���E���t���b�V���@�\�t��������)
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;

// �A�C�e���v���n�u���������ׂ��C���^�[�t�F�[�X (�I�v�V����)
public interface IInfiniteScrollItem<T>
{
    void Setup(T itemData, int index, Action<int> onClickCallback); // �f�[�^�ݒ�ƃR�[���o�b�N�o�^
}

[RequireComponent(typeof(ScrollRect))]
public class InfiniteScroll<T> : MonoBehaviour // �W�F�l���N�X<T>���g�p
{
    [Header("References")]
    [SerializeField] private RectTransform content;
    [SerializeField] private GameObject slotItemPrefab; // �A�C�e���v���n�u (IInfiniteScrollItem<T>�����������R���|�[�l���g��������)

    public Action<int> OnItemClicked; // �O������̃N���b�N�C�x���g�w�Ǘp

    private IList<T> dataList; // �f�[�^�\�[�X (�W�F�l���b�N�^)
    private float itemHeight = 100f;
    private int renderBuffer = 3; // �r���[�|�[�g�㉺�Ɍ����Ȃ��͈͂ŗ]���ɐ���/�ێ�����A�C�e����

    private List<MonoBehaviour> itemsPool = new List<MonoBehaviour>(); // �v�[�� (�^��MonoBehaviour��)
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

        // �v���n�u�̗L�������m�F
        if (slotItemPrefab == null)
        {
            Debug.LogError("[InfiniteScroll] SlotItemPrefab is not assigned!");
            enabled = false;
            return;
        }
        if (slotItemPrefab.GetComponent<IInfiniteScrollItem<T>>() == null) // �C���^�[�t�F�[�X�����m�F
        {
            Debug.LogError($"[InfiniteScroll] Prefab {slotItemPrefab.name} must have a component implementing IInfiniteScrollItem<{typeof(T).Name}>.");
            // ��ֈ�: ����̃��\�b�h��(��: Setup)�����t���N�V�����ŒT�����A�K��Ƃ���
            // enabled = false;
            // return;
        }
    }

    public void Initialize(IList<T> data, float prefabHeight)
    {
        StopUpdateCoroutine(); // �����̃R���[�`����~
        // �Â��A�C�e�����폜 (�v�[�����N���A)
        foreach (var item in itemsPool)
        {
            if (item != null && item.gameObject != null) Destroy(item.gameObject);
        }
        itemsPool.Clear();

        this.dataList = data;
        this.itemHeight = prefabHeight;

        if (dataList == null || dataList.Count == 0)
        {
            // �f�[�^���Ȃ��ꍇ��Content����ɂ���
            LayoutRebuilder.ForceRebuildLayoutImmediate(content); // ���C�A�E�g�𑦎��X�V
            content.sizeDelta = new Vector2(content.sizeDelta.x, 0);
            return;
        }

        // Content�̍������v�Z (LayoutGroup���l��)
        float totalHeight = CalculateContentHeight();
        content.sizeDelta = new Vector2(content.sizeDelta.x, totalHeight);


        // �����X�N���[���ʒu����ԏ��
        scrollRect.verticalNormalizedPosition = 1f;
        previousScrollPos = 1f;
        firstVisibleIndex = -1; // �C���f�b�N�X�����Z�b�g
        lastVisibleIndex = -1;

        // �X�V�R���[�`�����J�n
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
        // �p�ɂȍX�V��h�����߁A�R���[�`���ŏ���
        if (updateCoroutine == null)
        {
            updateCoroutine = StartCoroutine(UpdateVisibleItemsCoroutine(false));
        }
    }

    private IEnumerator UpdateVisibleItemsCoroutine(bool forceUpdate)
    {
        //yield return null; // 1�t���[���ҋ@����Layout�v�Z�Ȃǂ�҂�
        // ���׌y���̂��߁A���t���[���ł͂Ȃ������҂ꍇ
        yield return new WaitForSeconds(0.05f);


        if (dataList == null || dataList.Count == 0)
        {
            updateCoroutine = null;
            yield break;
        }

        float currentNormPos = scrollRect.verticalNormalizedPosition;

        // �X�N���[���ω����������ꍇ�͍X�V���Ȃ� (forceUpdate������)
        if (!forceUpdate && Mathf.Abs(currentNormPos - previousScrollPos) < 0.001f && firstVisibleIndex != -1)
        {
            updateCoroutine = null;
            yield break;
        }

        previousScrollPos = currentNormPos;

        float contentHeight = content.rect.height;
        float viewportHeight = scrollRect.viewport.rect.height;

        // �X�N���[���\�ȗ̈�̍���
        float scrollAreaHeight = contentHeight - viewportHeight;
        if (scrollAreaHeight <= 0) scrollAreaHeight = 0; // �X�N���[���s�v�̏ꍇ

        // ���݂̃r���[�|�[�g�̏�[��Y���W (Content�̃��[�J�����W�n, �オ0)
        float viewportTopY = (1f - currentNormPos) * scrollAreaHeight;
        // ���݂̃r���[�|�[�g�̉��[��Y���W
        float viewportBottomY = viewportTopY + viewportHeight;

        // �\�����ׂ��A�C�e���̃C���f�b�N�X�͈͂��v�Z
        int newFirstVisibleIndex = Mathf.Max(0, Mathf.FloorToInt(viewportTopY / itemHeight) - renderBuffer);
        int newLastVisibleIndex = Mathf.Min(dataList.Count - 1, Mathf.CeilToInt(viewportBottomY / itemHeight) - 1 + renderBuffer); // CeilToInt�Ȃ̂�-1����

        // �\���͈͂��ς���Ă��Ȃ��ꍇ�͏I�� (forceUpdate������)
        if (!forceUpdate && newFirstVisibleIndex == firstVisibleIndex && newLastVisibleIndex == lastVisibleIndex)
        {
            updateCoroutine = null;
            yield break;
        }

        //Debug.Log($"Updating Visible Items: {newFirstVisibleIndex} to {newLastVisibleIndex}");

        // --- �A�C�e���̍ė��p�ƍX�V ---
        HashSet<int> itemsToShow = new HashSet<int>();
        for (int i = newFirstVisibleIndex; i <= newLastVisibleIndex; i++)
        {
            itemsToShow.Add(i);
        }

        List<MonoBehaviour> itemsToRemove = new List<MonoBehaviour>();
        Dictionary<int, MonoBehaviour> activeItems = new Dictionary<int, MonoBehaviour>(); // index -> item map

        // �����̃A�C�e�����`�F�b�N
        foreach (MonoBehaviour item in itemsPool)
        {
            if (item == null || !item.gameObject.activeSelf) continue; // �����ȃA�C�e���͖���

            // IInfiniteScrollItem<T>���������Ă��邩�A����̃R���|�[�l���g���擾
            // �����ł� ShopItemUI �𒼐ڎQ�Ƃ������ɃC���^�[�t�F�[�X���g��
            var scrollItem = item.GetComponent<IInfiniteScrollItem<T>>();
            int currentIndex = -1;
            // �����C���f�b�N�X���R���|�[�l���g���ɕێ����Ă���Ȃ�擾����
            // ��: currentIndex = scrollItem.GetCurrentIndex();
            // ���ꂪ�Ȃ��ꍇ�A�A�C�e���ƃC���f�b�N�X�̊֘A�t�����K�v

            // ����GameObject������C���f�b�N�X�𕜌� (�񐄏��ȕ��@)
            if (!int.TryParse(item.gameObject.name.Replace("Item_", ""), out currentIndex))
            {
                // �C���f�b�N�X���s���ȃA�C�e���͈��������
                // itemsToRemove.Add(item);
                // continue;
                // ���̗�ł�GetCurrentIndex����������Ă���Ɖ��肷�邩�A
                // �ʂ̕��@(��: Dictionary�ŊǗ�)�ŃC���f�b�N�X��ǐՂ���
                Debug.LogWarning($"Could not determine index for item {item.gameObject.name}");
                continue; // �C���f�b�N�X�s���ȃA�C�e���̓X�L�b�v
            }


            if (itemsToShow.Contains(currentIndex))
            {
                // ���̃A�C�e���͕\���͈͓��Ȃ̂ňێ�
                activeItems.Add(currentIndex, item);
                itemsToShow.Remove(currentIndex); // �\���ς݂Ƃ��ă}�[�N
            }
            else
            {
                // ���̃A�C�e���͕\���͈͊O�Ȃ̂Ńv�[���ɖ߂��i��A�N�e�B�u���j
                itemsToRemove.Add(item);
            }
        }

        // �s�v�ɂȂ����A�C�e�����A�N�e�B�u��
        foreach (var itemToRemove in itemsToRemove)
        {
            if (itemToRemove != null) itemToRemove.gameObject.SetActive(false);
        }

        // �V�����\���͈͂ɓ������A�C�e���𐶐��܂��͍ăA�N�e�B�u��
        foreach (int indexToShow in itemsToShow)
        {
            MonoBehaviour newItem = GetOrCreateItem();
            UpdateItem(newItem, indexToShow);
            activeItems.Add(indexToShow, newItem); // �A�N�e�B�u�A�C�e�����X�g�ɒǉ�
        }

        // �ێ����Ă���C���f�b�N�X���X�V
        firstVisibleIndex = newFirstVisibleIndex;
        lastVisibleIndex = newLastVisibleIndex;

        updateCoroutine = null; // �R���[�`���I��
    }

    // �v�[�������A�N�e�B�u�ȃA�C�e�����擾���邩�A�V�K�쐬����
    private MonoBehaviour GetOrCreateItem()
    {
        // �v�[�������A�N�e�B�u�ȃA�C�e����T��
        foreach (var item in itemsPool)
        {
            if (item != null && !item.gameObject.activeSelf)
            {
                item.gameObject.SetActive(true);
                return item;
            }
        }

        // ������Ȃ���ΐV�K�쐬
        GameObject go = Instantiate(slotItemPrefab, content);
        MonoBehaviour itemComp = go.GetComponent<MonoBehaviour>(); // ��{�N���X�Ŏ擾
        if (itemComp == null)
        {
            Debug.LogError($"Prefab {slotItemPrefab.name} does not have a MonoBehaviour component!");
            // �K�v�Ȃ�K�؂ȃR���|�[�l���g��T�����ǉ�����
            // itemComp = go.AddComponent<RectTransform>(); // �_�~�[
            return null;
        }
        itemsPool.Add(itemComp);
        return itemComp;
    }


    // �A�C�e���̓��e�ƈʒu���X�V
    private void UpdateItem(MonoBehaviour item, int dataIndex)
    {
        if (item == null || item.gameObject == null || dataIndex < 0 || dataIndex >= dataList.Count) return;

        // ���O�ŃC���f�b�N�X��ǐ� (���܂�ǂ��Ȃ����A��Ƃ���)
        item.gameObject.name = $"Item_{dataIndex}"; // �C���f�b�N�X�ǐ՗p

        // --- �ʒu�ݒ� ---
        // VerticalLayoutGroup������ꍇ�͕s�v�A�Ȃ��ꍇ�͎蓮�ݒ�
        RectTransform itemRect = item.GetComponent<RectTransform>();
        if (itemRect != null && content.GetComponent<VerticalLayoutGroup>() == null)
        {
            float yPos = -(dataIndex * itemHeight) - (itemHeight * 0.5f); // ���S��̏ꍇ
                                                                          // �A���J�[����[�̏ꍇ
                                                                          // float yPos = -dataIndex * itemHeight - content.GetComponent<VerticalLayoutGroup>()?.padding.top ?? 0;
            itemRect.anchoredPosition = new Vector2(0f, yPos);
        }

        // --- �f�[�^�ݒ� ---
        // �W�F�l���b�N�^�f�[�^�ƃC���f�b�N�X��n��
        var scrollItem = item.GetComponent<IInfiniteScrollItem<T>>();
        if (scrollItem != null)
        {
            scrollItem.Setup(dataList[dataIndex], dataIndex, HandleItemClick);
        }
        else
        {
            // �C���^�[�t�F�[�X���Ȃ��ꍇ�̑�� (����̃��\�b�h���ĂԂȂ�)
            // item.SendMessage("Setup", dataList[dataIndex], SendMessageOptions.DontRequireReceiver);
            // item.GetComponent<ShopItemUI>()?.Setup(dataList[dataIndex], dataIndex, ...) // �^���������Ă���ꍇ
            Debug.LogError($"Item {item.gameObject.name} does not implement IInfiniteScrollItem<{typeof(T).Name}> or have a compatible Setup method.");
        }

        item.gameObject.SetActive(true);
    }

    // �A�C�e�����̃N���b�N������
    private void HandleItemClick(int index)
    {
        OnItemClicked?.Invoke(index);
        //Debug.Log($"Item clicked: Index {index}");
    }

    // �\������Ă���A�C�e���������I�ɍX�V���郁�\�b�h
    public void RefreshVisibleItems()
    {
        //Debug.Log("Refreshing visible items...");
        StopUpdateCoroutine(); // �����̃R���[�`����~
        updateCoroutine = StartCoroutine(UpdateVisibleItemsCoroutine(true)); // �����X�V�t���O�𗧂ĂĎ��s
    }

    private void StopUpdateCoroutine()
    {
        if (updateCoroutine != null)
        {
            StopCoroutine(updateCoroutine);
            updateCoroutine = null;
        }
    }

    // �R���|�[�l���g�������ɂȂ�����R���[�`����~
    private void OnDisable()
    {
        StopUpdateCoroutine();
    }
}