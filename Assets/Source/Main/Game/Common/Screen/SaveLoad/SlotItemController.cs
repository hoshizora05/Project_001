using UnityEngine;
using UnityEngine.UI;
using System;
using SaveSystem;

public class SlotItemController : MonoBehaviour
{
    [SerializeField] private Text slotLabel;
    [SerializeField] private Text dateLabel;
    [SerializeField] private Image thumbnailImage;
    [SerializeField] private Button slotButton;

    private Action onClickCallback;

    public void SetSlotInfo(int slotNumber, System.DateTime lastSaveDate, string playerName, string thumbnailData)
    {
        slotLabel.text = $"[{slotNumber + 1}] {lastSaveDate:yyyy/MM/dd HH:mm} {playerName}";

        // If you store a base64 thumbnail (thumbnailData), you can convert to Texture2D using:
        var tex = SaveLoadManager.Instance.GetThumbnailTexture(thumbnailData);
        if (tex != null)
        {
            // Convert to Sprite or just assign to the UI using something like:
            thumbnailImage.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), Vector2.zero);
        }

        dateLabel.gameObject.SetActive(true);
        dateLabel.text = lastSaveDate.ToString("yyyy/MM/dd HH:mm");
    }

    public void SetEmptySlot(int slotNumber)
    {
        slotLabel.text = $"[{slotNumber + 1}] ---EMPTY---";
        dateLabel.gameObject.SetActive(false);
        thumbnailImage.sprite = null; // or some placeholder
    }

    public void SetClickAction(Action callback)
    {
        onClickCallback = callback;
        slotButton.onClick.RemoveAllListeners();
        slotButton.onClick.AddListener(() => onClickCallback?.Invoke());
    }
}
