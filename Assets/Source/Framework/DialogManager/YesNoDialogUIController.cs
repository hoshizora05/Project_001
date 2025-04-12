using UnityEngine;
using UnityEngine.UI;
namespace DialogSystem
{
    /// <summary>
    /// Renders a Yes/No dialog.
    /// </summary>
    public class YesNoDialogUIController : BaseDialogUIController
    {
        [Header("UI References")]
        [SerializeField] private Text titleText = null;
        [SerializeField] private Text messageText = null;
        [SerializeField] private Button yesButton = null;
        [SerializeField] private Button noButton = null;

        private System.Action onClose;

        public override void InitializeDialog(BaseDialogData data, System.Action onDialogClosed)
        {
            onClose = onDialogClosed;

            // Cast data to specialized type
            var yesNoData = data as YesNoDialogData;
            if (yesNoData == null)
            {
                Debug.LogError("YesNoDialogUIController received data that is not YesNoDialogData!");
                CloseDialog(onClose);
                return;
            }

            // Populate UI
            if (titleText) titleText.text = yesNoData.Title;
            if (messageText) messageText.text = yesNoData.Message;

            // Wire up buttons
            yesButton.onClick.AddListener(() =>
            {
                yesNoData.OnYes?.Invoke();
                CloseDialog(onClose);
            });

            noButton.onClick.AddListener(() =>
            {
                yesNoData.OnNo?.Invoke();
                CloseDialog(onClose);
            });
        }
    }
}