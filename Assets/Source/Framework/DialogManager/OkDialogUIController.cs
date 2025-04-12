using UnityEngine;
using UnityEngine.UI;
namespace DialogSystem
{
    /// <summary>
    /// Renders an OK dialog (single button).
    /// </summary>
    public class OkDialogUIController : BaseDialogUIController
    {
        [Header("UI References")]
        [SerializeField] private Text titleText = null;
        [SerializeField] private Text messageText = null;
        [SerializeField] private Button okButton = null;

        private System.Action onClose;

        // Implementation required by BaseDialogUIController
        public override void InitializeDialog(BaseDialogData data, System.Action onDialogClosed)
        {
            onClose = onDialogClosed;

            // Cast data to our specialized OkDialogData
            var okData = data as OkDialogData;
            if (okData == null)
            {
                Debug.LogError("OkDialogUIController received data that is not OkDialogData!");
                CloseDialog(onClose);
                return;
            }

            // Populate UI
            if (titleText) titleText.text = okData.Title;
            if (messageText) messageText.text = okData.Message;

            // Hook up OK button
            okButton.onClick.AddListener(() =>
            {
                okData.OnOk?.Invoke();
                CloseDialog(onClose);
            });
        }
    }
}