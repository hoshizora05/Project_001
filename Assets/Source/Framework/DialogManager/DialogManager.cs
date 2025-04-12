using UnityEngine;
using System;
using System.Collections.Generic;

namespace DialogSystem
{
    /// <summary>
    /// Manages creation, queuing, and display of dialogs.
    /// It can spawn any type of dialog as long as you register the prefab for that Data type.
    /// </summary>
    public class DialogManager : MonoBehaviour
    {
        public static DialogManager Instance { get; private set; }

        // A dictionary mapping from a Data type to a prefab for that type.
        // e.g. { typeof(OkDialogData) -> OkDialogUIController prefab, typeof(YesNoDialogData) -> YesNoDialogUIController prefab, ... }
        private Dictionary<Type, BaseDialogUIController> dialogPrefabs = new Dictionary<Type, BaseDialogUIController>();

        // Manage multiple dialogs in a queue
        private Queue<BaseDialogData> dialogQueue = new Queue<BaseDialogData>();

        // Currently displayed dialog
        private BaseDialogUIController currentDialog = null;

        private void Awake()
        {
            // Basic Singleton
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }
            // Optionally, keep this across scenes:
            // DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// Registers a prefab for a specific data type T.
        /// That prefab must have a component deriving from BaseDialogUIController.
        /// 
        /// Example usage in Awake or Start:
        ///  RegisterDialogPrefab<OkDialogData>(okDialogPrefab);
        ///  RegisterDialogPrefab<YesNoDialogData>(yesNoDialogPrefab);
        /// </summary>
        public void RegisterDialogPrefab<T>(BaseDialogUIController prefab) where T : BaseDialogData
        {
            if (prefab == null)
            {
                Debug.LogError("Attempted to register a null prefab for type " + typeof(T).Name);
                return;
            }

            dialogPrefabs[typeof(T)] = prefab;
        }

        /// <summary>
        /// Show a dialog of any type. If another is open, it is queued.
        /// </summary>
        public void ShowDialog(BaseDialogData data)
        {
            if (currentDialog != null)
            {
                // Enqueue if a dialog is already displayed
                dialogQueue.Enqueue(data);
            }
            else
            {
                // Otherwise, show immediately
                CreateDialog(data);
            }
        }

        /// <summary>
        /// Actually instantiate the dialog prefab for this data type, and call Initialize.
        /// </summary>
        private void CreateDialog(BaseDialogData data)
        {
            var dataType = data.GetType();
            if (!dialogPrefabs.ContainsKey(dataType))
            {
                Debug.LogError($"No dialog prefab registered for type: {dataType.Name}");
                return;
            }

            var prefab = dialogPrefabs[dataType];
            currentDialog = Instantiate(prefab, transform);

            // Pass an inline callback to be fired when the dialog closes
            currentDialog.InitializeDialog(data, OnDialogClosed);
        }

        /// <summary>
        /// Called when a dialog closes. If another is queued, show the next one.
        /// </summary>
        private void OnDialogClosed()
        {
            currentDialog = null;
            if (dialogQueue.Count > 0)
            {
                var nextData = dialogQueue.Dequeue();
                CreateDialog(nextData);
            }
        }

        #region Convenient Wrapper Methods (Optional)
        /// <summary>
        /// A convenience wrapper to show an Ok dialog.
        /// You could place this in a separate class if you prefer.
        /// </summary>
        public void ShowOkDialog(string title, string message, Action onOk = null)
        {
            ShowDialog(new OkDialogData(title, message, onOk));
        }

        /// <summary>
        /// A convenience wrapper to show a Yes/No dialog.
        /// </summary>
        public void ShowYesNoDialog(string title, string message, Action onYes, Action onNo)
        {
            ShowDialog(new YesNoDialogData(title, message, onYes, onNo));
        }
        #endregion
    }
}