### How to Set It Up in Unity

1. **Create Prefabs for Each Dialog Type**  
   - For `OkDialog`, create a Canvas-based prefab with a root panel, attach the `OkDialogUIController` script, and wire up the references:
     - A Text (titleText)  
     - A Text (messageText)  
     - A Button (okButton)  
   - For `YesNoDialog`, do similarly. Attach `YesNoDialogUIController`, set references for title, message, yesButton, noButton, etc.

2. **Register Prefabs in the Scene**  
   - In your scene, create an empty `GameObject` named `DialogManager` (or similar).  
   - Attach the `DialogManager` script.  
   - In an `Awake` or `Start` method (or from another script), call:
     ```csharp
     // Suppose you drag references in the Inspector:
     //   public BaseDialogUIController okDialogPrefab;
     //   public BaseDialogUIController yesNoDialogPrefab;
     private void Awake()
     {
         DialogManager.Instance.RegisterDialogPrefab<OkDialogData>(okDialogPrefab);
         DialogManager.Instance.RegisterDialogPrefab<YesNoDialogData>(yesNoDialogPrefab);
         // Register further custom dialogs here...
     }
     ```
   - Make sure the Manager instance is in the scene, or you can do the registration from its own inspector references.

3. **Show Dialogs**  
   - From anywhere in your code (e.g., a button click), simply call:
     ```csharp
     // Single-OK style
     DialogManager.Instance.ShowOkDialog(
         "Warning", 
         "This is a dangerous button! Proceed?", 
         () => { Debug.Log("User pressed OK"); }
     );

     // Yes/No style
     DialogManager.Instance.ShowYesNoDialog(
         "Confirm Action",
         "Do you really want to continue?",
         () => { Debug.Log("Yes pressed"); },
         () => { Debug.Log("No pressed"); }
     );
     ```
   - If a dialog is already on screen, subsequent calls are queued and will appear after the current dialog closes.

4. **Add New Dialog Types**  
   - If you want to create a brand-new type of dialog with different fields, do the following:
     1. **Create a new `MyCustomDialogData : BaseDialogData`** to hold the additional data (images, special text, numeric fields, etc.).  
     2. **Create a matching `MyCustomDialogUIController : BaseDialogUIController`** to handle layout and user interactions.  
     3. **Make a new prefab** in Unity that uses `MyCustomDialogUIController`.  
     4. **Register** that prefab with `DialogManager.RegisterDialogPrefab<MyCustomDialogData>(...)`.  
     5. **Call** `DialogManager.Instance.ShowDialog(new MyCustomDialogData(...))` to display it.

This architecture keeps your **manager** generic and easily expandable, while each **dialog type** (data + UI) lives in its own file/prefab. You can quickly add additional specialized dialogs to suit your game’s needs.