using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class RegionScrollItem : MonoBehaviour, IInfiniteScrollItem<RegionData>
{
    [SerializeField] private Text regionNameText;
    [SerializeField] private Image regionIconImage;
    [SerializeField] private Button itemButton;

    public void Setup(RegionData itemData, int index, Action<int> onClickCallback)
    {
        regionNameText.text = itemData.regionName;
        regionIconImage.sprite = itemData.regionIcon;

        // ボタンを押したら onClickCallback(index) を呼ぶ
        if (itemButton != null)
        {
            itemButton.onClick.RemoveAllListeners();
            itemButton.onClick.AddListener(() => onClickCallback?.Invoke(index));
        }
    }
}
