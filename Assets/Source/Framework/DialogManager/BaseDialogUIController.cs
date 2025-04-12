using UnityEngine;
namespace DialogSystem
{
    /// <summary>
    /// Abstract base for all UI controllers that render a dialog.
    /// </summary>
    public abstract class BaseDialogUIController : MonoBehaviour
    {
        // Called by DialogManager after instantiation to feed data in.
        public abstract void InitializeDialog(BaseDialogData data, System.Action onDialogClosed);

        /// <summary>
        /// Must be called from derived classes when the dialog is to close.
        /// This ensures the manager knows the dialog is done.
        /// </summary>
        protected void CloseDialog(System.Action onDialogClosed)
        {
            // Notify manager first, so it can queue next, etc.
            onDialogClosed?.Invoke();
            // Then destroy this dialog gameobject
            Destroy(gameObject);
        }
    }
}