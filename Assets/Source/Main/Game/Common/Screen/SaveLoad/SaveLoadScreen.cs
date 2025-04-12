using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using DialogSystem;
using SaveSystem; // SaveLoadManager ÇÃ namespace
using System.Threading.Tasks;

/// <summary>
/// A UI controller for displaying a list of save slots using InfiniteScroll
/// and handling Save/Load/Delete via the SaveLoadManager.
/// 
/// - The 'SAVE', 'LOAD', and 'DELETE' buttons control which operation is
///   performed when a slot is clicked.
/// - Actually saving/loading/deleting is triggered by clicking on a slot.
/// - Confirmation dialogs are displayed via DialogManager before performing the action.
/// </summary>
public class SaveLoadScreen : BaseScreen
{
    [Header("UI References")]
    [SerializeField] private InfiniteScroll<string> infiniteScroll;

    [SerializeField] private Button buttonSave;
    [SerializeField] private Button buttonLoad;
    [SerializeField] private Button buttonDelete;
    [SerializeField] private Button buttonBack;

    [Tooltip("Maximum number of slots to display (e.g., 20).")]
    [SerializeField] private int maxSlots = 20;

    // Possible modes for the screen
    private enum SaveLoadMode
    {
        None,
        Save,
        Load,
        Delete
    }
    private SaveLoadMode currentMode = SaveLoadMode.None;

    // For convenience, weÅfll store the metadata for each slot index
    private Dictionary<int, SaveMetadata> slotMetadata;

    private void Start()
    {
        // Initialize the mode
        currentMode = SaveLoadMode.None;

        // Set up button listeners
        buttonSave.onClick.AddListener(() => OnClickModeButton(SaveLoadMode.Save));
        buttonLoad.onClick.AddListener(() => OnClickModeButton(SaveLoadMode.Load));
        buttonDelete.onClick.AddListener(() => OnClickModeButton(SaveLoadMode.Delete));
        buttonBack.onClick.AddListener(OnClickBack);

        // Configure the infinite scroll to handle item clicks
        infiniteScroll.OnItemClicked = OnSlotClicked;

        // Retrieve metadata and display
        RefreshSlotList();
    }

    /// <summary>
    /// Called by the mode-select buttons (SAVE / LOAD / DELETE) 
    /// to indicate which operation slot clicks should perform.
    /// </summary>
    private void OnClickModeButton(SaveLoadMode mode)
    {
        currentMode = mode;
        Debug.Log($"Current mode set to: {currentMode}");
    }

    /// <summary>
    /// Called by the BACK button.
    /// Typically you might hide this screen or return to a previous menu.
    /// </summary>
    private void OnClickBack()
    {
        Debug.Log("Back button clicked. Closing / going back.");
        this.gameObject.SetActive(false);
    }

    /// <summary>
    /// Queries the SaveLoadManager for all current metadata,
    /// then sets up the infinite scroll to display a textual list of slots.
    /// </summary>
    private void RefreshSlotList()
    {
        // 1) Get metadata from SaveLoadManager
        slotMetadata = SaveLoadManager.Instance.GetAllSaveMetadata();

        // 2) Build a textual list for each slot (0..maxSlots-1)
        List<string> slotLabels = new List<string>();
        for (int i = 0; i < maxSlots; i++)
        {
            if (slotMetadata.ContainsKey(i))
            {
                SaveMetadata meta = slotMetadata[i];
                // Format: "[slotNumber] date playerName"
                string label = $"[{i + 1}] {meta.lastSaveDate:yyyy/MM/dd HH:mm} {meta.playerName}";
                slotLabels.Add(label);
            }
            else
            {
                // empty slot
                slotLabels.Add($"[{i + 1}] ---EMPTY---");
            }
        }

        // 3) Initialize the infinite scroll with these labels
        //    itemHeight = 80f, renderCount = 5 (ó·)
        float itemHeight = 80f;
        int renderCount = 5;

        infiniteScroll.Initialize(slotLabels, itemHeight);
    }

    /// <summary>
    /// Called when a slot is clicked. 'index' is the 0-based slot index.
    /// We check currentMode and do confirm dialogs, then save/load/delete.
    /// </summary>
    private void OnSlotClicked(int index)
    {
        switch (currentMode)
        {
            case SaveLoadMode.Save:
                ConfirmSave(index);
                break;
            case SaveLoadMode.Load:
                ConfirmLoad(index);
                break;
            case SaveLoadMode.Delete:
                ConfirmDelete(index);
                break;
            default:
                Debug.LogWarning("Slot clicked, but no mode selected (SAVE, LOAD, or DELETE).");
                break;
        }
    }

    /// <summary>
    /// Show a confirmation dialog for saving to a slot, then do it if 'Yes'.
    /// </summary>
    private void ConfirmSave(int slotNumber)
    {
        string title = "Confirm Save";
        string message = $"Save to slot {slotNumber + 1}? (This will overwrite any existing data.)";

        DialogManager.Instance.ShowYesNoDialog(
            title,
            message,
            onYes: async () =>
            {
                bool success = await SaveLoadManager.Instance.SaveGameAsync(slotNumber, includeThumbnail: true);
                if (!success)
                {
                    Debug.LogError("Save failed.");
                }
                else
                {
                    // Refresh after overwriting
                    RefreshSlotList();
                }
            },
            onNo: () => { /* do nothing */ }
        );
    }

    /// <summary>
    /// Show a confirmation dialog for loading from a slot, then do it if 'Yes'.
    /// </summary>
    private void ConfirmLoad(int slotNumber)
    {
        if (!SaveLoadManager.Instance.DoesSaveExist(slotNumber))
        {
            Debug.LogWarning("No save file in this slot to load.");
            return;
        }

        string title = "Confirm Load";
        string message = $"Load from slot {slotNumber + 1}? Unsaved progress will be lost.";

        DialogManager.Instance.ShowYesNoDialog(
            title,
            message,
            onYes: async () =>
            {
                bool success = await SaveLoadManager.Instance.LoadGameAsync(slotNumber);
                if (!success)
                {
                    Debug.LogError("Load failed.");
                }
            },
            onNo: () => { /* do nothing */ }
        );
    }

    /// <summary>
    /// Show a confirmation dialog for deleting a slot, then do it if 'Yes'.
    /// </summary>
    private void ConfirmDelete(int slotNumber)
    {
        if (!SaveLoadManager.Instance.DoesSaveExist(slotNumber))
        {
            Debug.LogWarning("No save file in this slot to delete.");
            return;
        }

        string title = "Confirm Delete";
        string message = $"Delete the save in slot {slotNumber + 1}?";

        DialogManager.Instance.ShowYesNoDialog(
            title,
            message,
            onYes: () =>
            {
                bool success = SaveLoadManager.Instance.DeleteSave(slotNumber);
                if (!success)
                {
                    Debug.LogError("Delete failed.");
                }
                else
                {
                    RefreshSlotList();
                }
            },
            onNo: () => { /* do nothing */ }
        );
    }
}
