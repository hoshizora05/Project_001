using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using Newtonsoft.Json;
using PlayerProgression;
using PlayerProgression.Data;
using CharacterSystem;
using ResourceManagement;
using LifeResourceSystem;
using SocialActivity;
using ProgressionAndEventSystem;

namespace SaveSystem
{
    /// <summary>
    /// Manages the saving and loading of game data across all game systems.
    /// Integrates with the various subsystems to ensure comprehensive save files.
    /// </summary>
    public class SaveLoadManager : MonoBehaviour
    {
        #region Singleton

        private static SaveLoadManager _instance;
        public static SaveLoadManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject obj = new GameObject("SaveLoadManager");
                    _instance = obj.AddComponent<SaveLoadManager>();
                    DontDestroyOnLoad(obj);
                }
                return _instance;
            }
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
            Initialize();
        }

        #endregion

        #region Configuration

        [Header("Save Settings")]
        [SerializeField] private string _defaultSaveFileName = "save";
        [SerializeField] private string _saveFileExtension = ".sav";
        [SerializeField] private bool _useEncryption = true;
        [SerializeField] private string _encryptionKey = "CHANGE_THIS_TO_YOUR_SECRET_KEY";
        [SerializeField] private bool _useCompression = true;
        [SerializeField] private bool _createBackups = true;
        [SerializeField] private int _maxBackupCount = 5;
        [SerializeField] private bool _autoSaveOnApplicationQuit = true;
        [SerializeField] private bool _verboseLogging = false;

        [Header("Save Slots")]
        [SerializeField] private int _maxSaveSlots = 10;

        [SerializeField] private string _autosaveSlotName = "autosave";

        #endregion

        #region Private Fields

        private string _saveDirectoryPath;
        private string _backupDirectoryPath;
        private SaveData _currentSaveData;
        private int _currentSaveSlot = 0;
        private bool _isInitialized = false;
        private Dictionary<string, ISaveable> _registeredSaveables = new Dictionary<string, ISaveable>();

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets the current save data. Returns null if no save is loaded.
        /// </summary>
        public SaveData CurrentSaveData => _currentSaveData;

        /// <summary>
        /// Gets the current save slot number.
        /// </summary>
        public int CurrentSaveSlot => _currentSaveSlot;

        /// <summary>
        /// Gets whether the system is currently initialized.
        /// </summary>
        public bool IsInitialized => _isInitialized;

        #endregion

        #region Initialization

        /// <summary>
        /// Initializes the SaveLoadManager.
        /// </summary>
        private void Initialize()
        {
            if (_isInitialized)
                return;

            // Set up save directories
            _saveDirectoryPath = Path.Combine(Application.persistentDataPath, "Saves");
            _backupDirectoryPath = Path.Combine(_saveDirectoryPath, "Backups");

            // Ensure directories exist
            if (!Directory.Exists(_saveDirectoryPath))
                Directory.CreateDirectory(_saveDirectoryPath);

            if (_createBackups && !Directory.Exists(_backupDirectoryPath))
                Directory.CreateDirectory(_backupDirectoryPath);

            _isInitialized = true;
            Log("SaveLoadManager initialized");
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Creates a new save data object and sets it as current.
        /// </summary>
        /// <param name="playerName">The name of the player for this save.</param>
        /// <returns>The newly created SaveData instance.</returns>
        public SaveData CreateNewSave(string playerName = "Player")
        {
            Log($"Creating new save data for player: {playerName}");
            _currentSaveData = new SaveData
            {
                playerName = playerName,
                creationDate = DateTime.Now,
                lastSaveDate = DateTime.Now,
                saveVersion = Application.version,
                playTime = TimeSpan.Zero
            };

            return _currentSaveData;
        }

        /// <summary>
        /// Saves the current game state to the specified slot.
        /// </summary>
        /// <param name="slotNumber">The save slot to use (0-based index).</param>
        /// <param name="includeThumbnail">Whether to capture and include a thumbnail of the current game state.</param>
        /// <returns>True if save was successful, false otherwise.</returns>
        public async Task<bool> SaveGameAsync(int slotNumber = -1, bool includeThumbnail = true)
        {
            if (!_isInitialized)
            {
                Debug.LogError("SaveLoadManager is not initialized. Call Initialize first.");
                return false;
            }

            // Use current slot if not specified
            if (slotNumber < 0)
                slotNumber = _currentSaveSlot;
            else
                _currentSaveSlot = slotNumber;

            if (slotNumber >= _maxSaveSlots)
            {
                Debug.LogError($"Save slot {slotNumber} exceeds the maximum of {_maxSaveSlots - 1}");
                return false;
            }

            if (_currentSaveData == null)
            {
                Debug.LogError("No current save data exists. Create a new save first.");
                return false;
            }

            try
            {
                // Update save metadata
                _currentSaveData.lastSaveDate = DateTime.Now;

                // If a thumbnail is requested, capture one
                if (includeThumbnail)
                {
                    await Task.Delay(1); // Ensure UI is fully rendered
                    _currentSaveData.thumbnailData = await CaptureThumbnailAsync();
                }

                // Collect data from all registered saveables
                await CollectSaveDataAsync();

                // Get all subsystem save data
                await CollectSystemSaveDataAsync();

                // Serialize and save the data
                string saveFileName = GetSaveFileName(slotNumber);
                string backupFileName = null;

                // Create backup if needed
                if (_createBackups && File.Exists(saveFileName))
                {
                    backupFileName = CreateBackupFileName(slotNumber);
                    try
                    {
                        File.Copy(saveFileName, backupFileName, true);
                        CleanupOldBackups(slotNumber);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"Failed to create backup: {ex.Message}");
                    }
                }

                // Save the file
                string json = JsonConvert.SerializeObject(_currentSaveData, Formatting.Indented);

                if (_useEncryption)
                    json = EncryptString(json);

                if (_useCompression)
                    json = CompressString(json);

                await Task.Run(() => File.WriteAllText(saveFileName, json));

                Log($"Game saved successfully to slot {slotNumber}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to save game: {ex.Message}\n{ex.StackTrace}");
                return false;
            }
        }

        /// <summary>
        /// Loads a game from the specified save slot.
        /// </summary>
        /// <param name="slotNumber">The save slot to load from (0-based index).</param>
        /// <returns>True if load was successful, false otherwise.</returns>
        public async Task<bool> LoadGameAsync(int slotNumber)
        {
            if (!_isInitialized)
            {
                Debug.LogError("SaveLoadManager is not initialized. Call Initialize first.");
                return false;
            }

            if (slotNumber >= _maxSaveSlots)
            {
                Debug.LogError($"Save slot {slotNumber} exceeds the maximum of {_maxSaveSlots - 1}");
                return false;
            }

            string saveFileName = GetSaveFileName(slotNumber);

            if (!File.Exists(saveFileName))
            {
                Debug.LogError($"No save file exists in slot {slotNumber}");
                return false;
            }

            try
            {
                Log($"Loading game from slot {slotNumber}");

                // Read the save file
                string json = await Task.Run(() => File.ReadAllText(saveFileName));

                if (_useCompression)
                    json = DecompressString(json);

                if (_useEncryption)
                    json = DecryptString(json);

                // Deserialize the save data
                _currentSaveData = JsonConvert.DeserializeObject<SaveData>(json);
                _currentSaveSlot = slotNumber;

                if (_currentSaveData == null)
                {
                    Debug.LogError("Failed to deserialize save data");
                    return false;
                }

                // Apply the save data to all registered saveables
                await ApplySaveDataAsync();

                // Apply the save data to all subsystems
                await ApplySystemSaveDataAsync();

                Log($"Game loaded successfully from slot {slotNumber}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to load game: {ex.Message}\n{ex.StackTrace}");

                // Try to load from backup
                if (_createBackups)
                {
                    Debug.Log("Attempting to load from backup...");
                    return await LoadFromBackupAsync(slotNumber);
                }

                return false;
            }
        }

        /// <summary>
        /// Performs an autosave of the current game state.
        /// </summary>
        /// <returns>True if autosave was successful, false otherwise.</returns>
        public async Task<bool> AutosaveAsync()
        {
            Log("Performing autosave...");

            // 現在のプレイヤー名を保存
            string originalPlayerName = _currentSaveData.playerName;

            // オートセーブ用の名前を設定
            _currentSaveData.playerName = _autosaveSlotName;

            // オートセーブを実行
            bool result = await SaveGameAsync(GetAutosaveSlot(), false);

            // 元のプレイヤー名を復元
            _currentSaveData.playerName = originalPlayerName;

            return result;
        }

        /// <summary>
        /// Deletes a save from the specified slot.
        /// </summary>
        /// <param name="slotNumber">The save slot to delete (0-based index).</param>
        /// <returns>True if delete was successful, false otherwise.</returns>
        public bool DeleteSave(int slotNumber)
        {
            if (!_isInitialized)
            {
                Debug.LogError("SaveLoadManager is not initialized. Call Initialize first.");
                return false;
            }

            string saveFileName = GetSaveFileName(slotNumber);

            if (!File.Exists(saveFileName))
            {
                Debug.LogWarning($"No save file exists in slot {slotNumber}");
                return false;
            }

            try
            {
                File.Delete(saveFileName);

                // Delete backups as well
                if (_createBackups)
                {
                    string[] backups = Directory.GetFiles(_backupDirectoryPath, $"*slot{slotNumber}*{_saveFileExtension}");
                    foreach (string backup in backups)
                    {
                        File.Delete(backup);
                    }
                }

                Log($"Save data in slot {slotNumber} deleted successfully");

                // If we just deleted the current save, clear the current save data
                if (slotNumber == _currentSaveSlot)
                {
                    _currentSaveData = null;
                }

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to delete save: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Gets metadata for all save slots.
        /// </summary>
        /// <returns>A dictionary of save slot numbers and their metadata.</returns>
        public Dictionary<int, SaveMetadata> GetAllSaveMetadata()
        {
            Dictionary<int, SaveMetadata> result = new Dictionary<int, SaveMetadata>();

            for (int i = 0; i < _maxSaveSlots; i++)
            {
                string saveFileName = GetSaveFileName(i);

                if (File.Exists(saveFileName))
                {
                    try
                    {
                        string json = File.ReadAllText(saveFileName);

                        if (_useCompression)
                            json = DecompressString(json);

                        if (_useEncryption)
                            json = DecryptString(json);

                        // Extract only the metadata
                        SaveData saveData = JsonConvert.DeserializeObject<SaveData>(json);

                        result[i] = new SaveMetadata
                        {
                            slotNumber = i,
                            playerName = saveData.playerName,
                            creationDate = saveData.creationDate,
                            lastSaveDate = saveData.lastSaveDate,
                            saveVersion = saveData.saveVersion,
                            playTime = saveData.playTime,
                            thumbnailData = saveData.thumbnailData,
                            isAutosave = i == GetAutosaveSlot()
                        };
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"Failed to read metadata for save slot {i}: {ex.Message}");
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Registers an object that implements ISaveable to be included in save/load operations.
        /// </summary>
        /// <param name="saveable">The object implementing ISaveable.</param>
        /// <param name="saveableId">A unique identifier for this saveable.</param>
        public void RegisterSaveable(ISaveable saveable, string saveableId)
        {
            if (_registeredSaveables.ContainsKey(saveableId))
            {
                Debug.LogWarning($"A saveable with ID {saveableId} is already registered. It will be replaced.");
            }

            _registeredSaveables[saveableId] = saveable;
            Log($"Registered saveable: {saveableId}");
        }

        /// <summary>
        /// Unregisters a saveable object.
        /// </summary>
        /// <param name="saveableId">The unique identifier of the saveable to unregister.</param>
        public void UnregisterSaveable(string saveableId)
        {
            if (_registeredSaveables.ContainsKey(saveableId))
            {
                _registeredSaveables.Remove(saveableId);
                Log($"Unregistered saveable: {saveableId}");
            }
        }

        /// <summary>
        /// Checks if a save exists in the specified slot.
        /// </summary>
        /// <param name="slotNumber">The save slot to check (0-based index).</param>
        /// <returns>True if a save exists, false otherwise.</returns>
        public bool DoesSaveExist(int slotNumber)
        {
            return File.Exists(GetSaveFileName(slotNumber));
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Attempts to load from a backup if the main save is corrupted.
        /// </summary>
        /// <param name="slotNumber">The save slot to load from.</param>
        /// <returns>True if load was successful, false otherwise.</returns>
        private async Task<bool> LoadFromBackupAsync(int slotNumber)
        {
            string[] backupFiles = Directory.GetFiles(
                _backupDirectoryPath,
                $"*slot{slotNumber}*{_saveFileExtension}",
                SearchOption.TopDirectoryOnly
            );

            if (backupFiles.Length == 0)
            {
                Debug.LogError("No backup files found.");
                return false;
            }

            // Sort by creation time (newest first)
            Array.Sort(backupFiles, (a, b) => File.GetCreationTime(b).CompareTo(File.GetCreationTime(a)));

            foreach (string backupFile in backupFiles)
            {
                try
                {
                    string json = await Task.Run(() => File.ReadAllText(backupFile));

                    if (_useCompression)
                        json = DecompressString(json);

                    if (_useEncryption)
                        json = DecryptString(json);

                    _currentSaveData = JsonConvert.DeserializeObject<SaveData>(json);
                    _currentSaveSlot = slotNumber;

                    if (_currentSaveData != null)
                    {
                        // Apply the save data
                        await ApplySaveDataAsync();
                        await ApplySystemSaveDataAsync();

                        Debug.Log($"Successfully loaded from backup file: {backupFile}");
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Failed to load backup file {backupFile}: {ex.Message}");
                }
            }

            Debug.LogError("All backup files failed to load.");
            return false;
        }

        /// <summary>
        /// Collects save data from all registered saveables.
        /// </summary>
        private async Task CollectSaveDataAsync()
        {
            if (_currentSaveData.saveableObjectData == null)
                _currentSaveData.saveableObjectData = new Dictionary<string, object>();

            foreach (var kvp in _registeredSaveables)
            {
                string saveableId = kvp.Key;
                ISaveable saveable = kvp.Value;

                try
                {
                    object saveData = await saveable.GetSaveDataAsync();
                    _currentSaveData.saveableObjectData[saveableId] = saveData;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to collect save data from {saveableId}: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Applies save data to all registered saveables.
        /// </summary>
        private async Task ApplySaveDataAsync()
        {
            if (_currentSaveData.saveableObjectData == null)
                return;

            foreach (var kvp in _registeredSaveables)
            {
                string saveableId = kvp.Key;
                ISaveable saveable = kvp.Value;

                if (_currentSaveData.saveableObjectData.TryGetValue(saveableId, out object saveData))
                {
                    try
                    {
                        await saveable.LoadSaveDataAsync(saveData);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Failed to apply save data to {saveableId}: {ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// Collects save data from all major game subsystems.
        /// </summary>
        private async Task CollectSystemSaveDataAsync()
        {
            try
            {
                // Gather Player Progression System data
                if (PlayerProgressionManager.Instance != null)
                {
                    _currentSaveData.playerProgressionData = PlayerProgressionManager.Instance.GenerateSaveData();
                    Log("Collected Player Progression System data");
                }

                // Gather Relationship Network data
                if (typeof(RelationshipNetwork).Assembly.GetType("RelationshipNetwork") != null)
                {
                    var relationshipNetwork = GetInstance("RelationshipNetwork");
                    if (relationshipNetwork != null)
                    {
                        // This would need to be adapted to the actual method structure
                        _currentSaveData.relationshipNetworkData = await Task.Run(() =>
                            JsonConvert.SerializeObject(CallMethod(relationshipNetwork, "GetSerializableData")));
                        Log("Collected Relationship Network data");
                    }
                }

                // Gather Character System data
                if (typeof(CharacterManager).Assembly.GetType("CharacterManager") != null)
                {
                    var characterManager = GetInstance("CharacterManager");
                    if (characterManager != null)
                    {
                        _currentSaveData.characterSystemData = await Task.Run(() =>
                            JsonConvert.SerializeObject(CallMethod(characterManager, "GenerateSaveData")));
                        Log("Collected Character System data");
                    }
                }

                // Gather NPC Lifecycle System data
                if (typeof(NPCLifecycleSystem).Assembly.GetType("NPCLifecycleSystem") != null)
                {
                    var lifecycleSystem = GetInstance("NPCLifecycleSystem");
                    if (lifecycleSystem != null)
                    {
                        _currentSaveData.npcLifecycleData = await Task.Run(() =>
                            JsonConvert.SerializeObject(CallMethod(lifecycleSystem, "GenerateSaveData")));
                        Log("Collected NPC Lifecycle System data");
                    }
                }

                // Gather Resource Management System data
                if (typeof(ResourceManagementSystem).Assembly.GetType("ResourceManagementSystem") != null)
                {
                    var resourceSystem = GetInstance("ResourceManagementSystem");
                    if (resourceSystem != null)
                    {
                        _currentSaveData.resourceSystemData = await Task.Run(() =>
                            JsonConvert.SerializeObject(CallMethod(resourceSystem, "GenerateSaveData")));
                        Log("Collected Resource Management System data");
                    }
                }

                // Gather Life Resource System data
                if (typeof(LifeResourceManager).Assembly.GetType("LifeResourceManager") != null)
                {
                    var lifeResourceManager = GetInstance("LifeResourceManager");
                    if (lifeResourceManager != null)
                    {
                        _currentSaveData.lifeResourceData = await Task.Run(() =>
                            JsonConvert.SerializeObject(CallMethod(lifeResourceManager, "GenerateSaveData")));
                        Log("Collected Life Resource System data");
                    }
                }

                // Gather Social Activity System data
                if (typeof(SocialActivity.SocialActivitySystem).Assembly.GetType("SocialActivitySystem") != null)
                {
                    var socialSystem = GetInstance("SocialActivitySystem");
                    if (socialSystem != null)
                    {
                        _currentSaveData.socialActivityData = await Task.Run(() =>
                            JsonConvert.SerializeObject(CallMethod(socialSystem, "GenerateSaveData")));
                        Log("Collected Social Activity System data");
                    }
                }

                // Gather Progression and Event System data
                if (typeof(ProgressionAndEventSystem.ProgressionAndEventSystem).Assembly.GetType("ProgressionAndEventSystem") != null)
                {
                    var eventSystem = GetInstance("ProgressionAndEventSystem");
                    if (eventSystem != null)
                    {
                        _currentSaveData.progressionEventSystemData = await Task.Run(() =>
                            JsonConvert.SerializeObject(CallMethod(eventSystem, "GenerateSaveData")));
                        Log("Collected Progression and Event System data");
                    }
                }

                // TODO: Add any other systems here as needed

                await Task.CompletedTask; // To ensure async compatibility
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error collecting system save data: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// Applies save data to all major game subsystems.
        /// </summary>
        private async Task ApplySystemSaveDataAsync()
        {
            try
            {
                // Apply Player Progression System data
                if (_currentSaveData.playerProgressionData != null && PlayerProgressionManager.Instance != null)
                {
                    PlayerProgressionManager.Instance.RestoreFromSaveData(_currentSaveData.playerProgressionData);
                    Log("Applied Player Progression System data");
                }

                // Apply Relationship Network data
                if (!string.IsNullOrEmpty(_currentSaveData.relationshipNetworkData))
                {
                    var relationshipNetwork = GetInstance("RelationshipNetwork");
                    if (relationshipNetwork != null)
                    {
                        object data = JsonConvert.DeserializeObject(_currentSaveData.relationshipNetworkData);
                        CallMethod(relationshipNetwork, "RestoreFromSaveData", data);
                        Log("Applied Relationship Network data");
                    }
                }

                // Apply Character System data
                if (!string.IsNullOrEmpty(_currentSaveData.characterSystemData))
                {
                    var characterManager = GetInstance("CharacterManager");
                    if (characterManager != null)
                    {
                        object data = JsonConvert.DeserializeObject(_currentSaveData.characterSystemData);
                        CallMethod(characterManager, "RestoreFromSaveData", data);
                        Log("Applied Character System data");
                    }
                }

                // Apply NPC Lifecycle System data
                if (!string.IsNullOrEmpty(_currentSaveData.npcLifecycleData))
                {
                    var lifecycleSystem = GetInstance("NPCLifecycleSystem");
                    if (lifecycleSystem != null)
                    {
                        object data = JsonConvert.DeserializeObject(_currentSaveData.npcLifecycleData);
                        CallMethod(lifecycleSystem, "RestoreFromSaveData", data);
                        Log("Applied NPC Lifecycle System data");
                    }
                }

                // Apply Resource Management System data
                if (!string.IsNullOrEmpty(_currentSaveData.resourceSystemData))
                {
                    var resourceSystem = GetInstance("ResourceManagementSystem");
                    if (resourceSystem != null)
                    {
                        object data = JsonConvert.DeserializeObject(_currentSaveData.resourceSystemData);
                        CallMethod(resourceSystem, "RestoreFromSaveData", data);
                        Log("Applied Resource Management System data");
                    }
                }

                // Apply Life Resource System data
                if (!string.IsNullOrEmpty(_currentSaveData.lifeResourceData))
                {
                    var lifeResourceManager = GetInstance("LifeResourceManager");
                    if (lifeResourceManager != null)
                    {
                        object data = JsonConvert.DeserializeObject(_currentSaveData.lifeResourceData);
                        CallMethod(lifeResourceManager, "RestoreFromSaveData", data);
                        Log("Applied Life Resource System data");
                    }
                }

                // Apply Social Activity System data
                if (!string.IsNullOrEmpty(_currentSaveData.socialActivityData))
                {
                    var socialSystem = GetInstance("SocialActivitySystem");
                    if (socialSystem != null)
                    {
                        object data = JsonConvert.DeserializeObject(_currentSaveData.socialActivityData);
                        CallMethod(socialSystem, "RestoreFromSaveData", data);
                        Log("Applied Social Activity System data");
                    }
                }

                // Apply Progression and Event System data
                if (!string.IsNullOrEmpty(_currentSaveData.progressionEventSystemData))
                {
                    var eventSystem = GetInstance("ProgressionAndEventSystem");
                    if (eventSystem != null)
                    {
                        object data = JsonConvert.DeserializeObject(_currentSaveData.progressionEventSystemData);
                        CallMethod(eventSystem, "RestoreFromSaveData", data);
                        Log("Applied Progression and Event System data");
                    }
                }

                // TODO: Add any other systems here as needed

                await Task.CompletedTask; // To ensure async compatibility
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error applying system save data: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// Captures a thumbnail of the current game state.
        /// </summary>
        /// <returns>A base64 encoded string of the thumbnail image.</returns>
        private async Task<string> CaptureThumbnailAsync()
        {
            await Task.Yield(); // Allow UI to render fully

            try
            {
                // Set up the render texture
                int width = 256;
                int height = 144; // 16:9 aspect ratio
                RenderTexture renderTexture = new RenderTexture(width, height, 24);

                // Find the main camera
                Camera mainCamera = Camera.main;
                if (mainCamera == null)
                {
                    Debug.LogWarning("Main camera not found. Thumbnail capture failed.");
                    return null;
                }

                // Temporarily render to our texture
                RenderTexture prevTarget = mainCamera.targetTexture;
                mainCamera.targetTexture = renderTexture;
                mainCamera.Render();
                mainCamera.targetTexture = prevTarget;

                // Read the pixels
                RenderTexture.active = renderTexture;
                Texture2D screenshot = new Texture2D(width, height, TextureFormat.RGB24, false);
                screenshot.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                screenshot.Apply();
                RenderTexture.active = null;

                // Convert to JPG to save space
                byte[] bytes = screenshot.EncodeToJPG(75);
                string base64 = Convert.ToBase64String(bytes);

                // Cleanup
                Destroy(renderTexture);
                Destroy(screenshot);

                return base64;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error capturing thumbnail: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// サムネイルデータをTexture2Dに変換します
        /// </summary>
        /// <param name="base64Data">Base64形式のサムネイルデータ</param>
        /// <returns>変換されたTexture2D、または失敗した場合はnull</returns>
        public Texture2D GetThumbnailTexture(string base64Data)
        {
            if (string.IsNullOrEmpty(base64Data))
                return null;

            try
            {
                byte[] imageData = Convert.FromBase64String(base64Data);
                Texture2D texture = new Texture2D(2, 2); // サイズは後でリセットされます
                if (texture.LoadImage(imageData))
                {
                    return texture;
                }
                else
                {
                    Debug.LogError("サムネイル画像データのロードに失敗しました");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"サムネイルの変換に失敗しました: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Creates a filename for a backup save.
        /// </summary>
        /// <param name="slotNumber">The save slot that is being backed up.</param>
        /// <returns>The backup file path.</returns>
        private string CreateBackupFileName(int slotNumber)
        {
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            return Path.Combine(_backupDirectoryPath, $"{_defaultSaveFileName}_slot{slotNumber}_{timestamp}{_saveFileExtension}");
        }

        /// <summary>
        /// Cleans up old backup files, keeping only the most recent ones.
        /// </summary>
        /// <param name="slotNumber">The save slot to clean backups for.</param>
        private void CleanupOldBackups(int slotNumber)
        {
            if (_maxBackupCount <= 0)
                return;

            string[] backupFiles = Directory.GetFiles(
                _backupDirectoryPath,
                $"{_defaultSaveFileName}_slot{slotNumber}_*{_saveFileExtension}",
                SearchOption.TopDirectoryOnly
            );

            // Sort by creation time (oldest first)
            Array.Sort(backupFiles, (a, b) => File.GetCreationTime(a).CompareTo(File.GetCreationTime(b)));

            // Delete oldest files if we have too many
            int filesToDelete = backupFiles.Length - _maxBackupCount;
            for (int i = 0; i < filesToDelete; i++)
            {
                try
                {
                    File.Delete(backupFiles[i]);
                    Log($"Deleted old backup: {Path.GetFileName(backupFiles[i])}");
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Failed to delete old backup {backupFiles[i]}: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Gets the save file path for a specific slot.
        /// </summary>
        /// <param name="slotNumber">The save slot to get the path for.</param>
        /// <returns>The full path to the save file.</returns>
        private string GetSaveFileName(int slotNumber)
        {
            string fileName = $"{_defaultSaveFileName}_slot{slotNumber}{_saveFileExtension}";
            return Path.Combine(_saveDirectoryPath, fileName);
        }

        /// <summary>
        /// Gets the autosave slot number.
        /// </summary>
        /// <returns>The slot number to use for autosaves.</returns>
        private int GetAutosaveSlot()
        {
            // デフォルトでは最後のスロットを使用
            int autosaveSlot = _maxSaveSlots - 1;

            // メタデータを取得してオートセーブスロットを探す
            var metadata = GetAllSaveMetadata();
            foreach (var pair in metadata)
            {
                // サムネイルデータではなくセーブの名前で判断
                SaveData saveData = null;
                try
                {
                    string saveFileName = GetSaveFileName(pair.Key);
                    string json = File.ReadAllText(saveFileName);

                    if (_useCompression)
                        json = DecompressString(json);

                    if (_useEncryption)
                        json = DecryptString(json);

                    saveData = JsonConvert.DeserializeObject<SaveData>(json);
                }
                catch
                {
                    continue;
                }

                // playerNameフィールドを使用してオートセーブを識別
                if (saveData != null && saveData.playerName == _autosaveSlotName)
                {
                    autosaveSlot = pair.Key;
                    break;
                }
            }

            return autosaveSlot;
        }

        /// <summary>
        /// Encrypts a string using the configured encryption key.
        /// </summary>
        /// <param name="plainText">The string to encrypt.</param>
        /// <returns>The encrypted string.</returns>
        private string EncryptString(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                return plainText;

            // Simple XOR encryption - not secure, but adds basic obfuscation
            // In a production environment, use a proper encryption library
            byte[] data = System.Text.Encoding.UTF8.GetBytes(plainText);
            byte[] key = System.Text.Encoding.UTF8.GetBytes(_encryptionKey);

            byte[] encrypted = new byte[data.Length];
            for (int i = 0; i < data.Length; i++)
            {
                encrypted[i] = (byte)(data[i] ^ key[i % key.Length]);
            }

            return Convert.ToBase64String(encrypted);
        }

        /// <summary>
        /// Decrypts a string that was encrypted with EncryptString.
        /// </summary>
        /// <param name="encryptedText">The encrypted string.</param>
        /// <returns>The decrypted string.</returns>
        private string DecryptString(string encryptedText)
        {
            if (string.IsNullOrEmpty(encryptedText))
                return encryptedText;

            try
            {
                byte[] data = Convert.FromBase64String(encryptedText);
                byte[] key = System.Text.Encoding.UTF8.GetBytes(_encryptionKey);

                byte[] decrypted = new byte[data.Length];
                for (int i = 0; i < data.Length; i++)
                {
                    decrypted[i] = (byte)(data[i] ^ key[i % key.Length]);
                }

                return System.Text.Encoding.UTF8.GetString(decrypted);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Decryption failed: {ex.Message}");
                return encryptedText; // Return the original text if decryption fails
            }
        }

        /// <summary>
        /// Compresses a string using GZip compression.
        /// </summary>
        /// <param name="text">The string to compress.</param>
        /// <returns>The compressed string in Base64 format.</returns>
        private string CompressString(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            try
            {
                byte[] buffer = System.Text.Encoding.UTF8.GetBytes(text);
                using (var memoryStream = new MemoryStream())
                {
                    using (var gzipStream = new System.IO.Compression.GZipStream(
                        memoryStream, System.IO.Compression.CompressionMode.Compress))
                    {
                        gzipStream.Write(buffer, 0, buffer.Length);
                    }

                    memoryStream.Position = 0;
                    byte[] compressed = new byte[memoryStream.Length];
                    memoryStream.Read(compressed, 0, compressed.Length);

                    return Convert.ToBase64String(compressed);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Compression failed: {ex.Message}");
                return text; // Return the original text if compression fails
            }
        }

        /// <summary>
        /// Decompresses a string that was compressed with CompressString.
        /// </summary>
        /// <param name="compressedText">The compressed string in Base64 format.</param>
        /// <returns>The decompressed string.</returns>
        private string DecompressString(string compressedText)
        {
            if (string.IsNullOrEmpty(compressedText))
                return compressedText;

            try
            {
                byte[] buffer = Convert.FromBase64String(compressedText);
                using (var memoryStream = new MemoryStream(buffer))
                {
                    using (var gzipStream = new System.IO.Compression.GZipStream(
                        memoryStream, System.IO.Compression.CompressionMode.Decompress))
                    {
                        using (var resultStream = new MemoryStream())
                        {
                            gzipStream.CopyTo(resultStream);
                            resultStream.Position = 0;

                            using (var reader = new StreamReader(resultStream, System.Text.Encoding.UTF8))
                            {
                                return reader.ReadToEnd();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Decompression failed: {ex.Message}");
                return compressedText; // Return the original text if decompression fails
            }
        }

        /// <summary>
        /// Uses reflection to get an instance of a singleton class.
        /// </summary>
        /// <param name="typeName">The name of the type to get an instance of.</param>
        /// <returns>The instance, or null if not found.</returns>
        private object GetInstance(string typeName)
        {
            try
            {
                Type type = Type.GetType(typeName) ??
                            AppDomain.CurrentDomain.GetAssemblies()
                            .SelectMany(a => a.GetTypes())
                            .FirstOrDefault(t => t.Name == typeName);

                if (type == null)
                    return null;

                // Try to get the Instance property
                PropertyInfo instanceProperty = type.GetProperty("Instance",
                    BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);

                if (instanceProperty != null)
                    return instanceProperty.GetValue(null);

                // Try to get a static field called _instance
                FieldInfo instanceField = type.GetField("_instance",
                    BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.FlattenHierarchy);

                if (instanceField != null)
                    return instanceField.GetValue(null);

                return null;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to get instance of {typeName}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Uses reflection to call a method on an object.
        /// </summary>
        /// <param name="obj">The object to call the method on.</param>
        /// <param name="methodName">The name of the method to call.</param>
        /// <param name="parameters">Optional parameters to pass to the method.</param>
        /// <returns>The result of the method call, or null if it failed.</returns>
        private object CallMethod(object obj, string methodName, params object[] parameters)
        {
            try
            {
                Type type = obj.GetType();
                MethodInfo method = type.GetMethod(methodName);

                if (method != null)
                    return method.Invoke(obj, parameters);

                return null;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to call method {methodName}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Logs a message if verbose logging is enabled.
        /// </summary>
        /// <param name="message">The message to log.</param>
        private void Log(string message)
        {
            if (_verboseLogging)
                Debug.Log($"[SaveLoadManager] {message}");
        }

        #endregion

        #region Unity Lifecycle

        private void OnApplicationQuit()
        {
            // Auto-save on application quit if enabled
            if (_autoSaveOnApplicationQuit && _currentSaveData != null)
            {
                Debug.Log("Auto-saving on application quit...");
                SaveGameAsync(_currentSaveSlot, false).GetAwaiter().GetResult();
            }
        }

        #endregion
    }

    /// <summary>
    /// Interface for objects that need to be saved and loaded.
    /// </summary>
    public interface ISaveable
    {
        /// <summary>
        /// Gets the save data for this object.
        /// </summary>
        /// <returns>An object containing the save data.</returns>
        Task<object> GetSaveDataAsync();

        /// <summary>
        /// Loads the save data for this object.
        /// </summary>
        /// <param name="saveData">The save data to load.</param>
        Task LoadSaveDataAsync(object saveData);
    }

    /// <summary>
    /// Data structure containing all save data.
    /// </summary>
    [Serializable]
    public class SaveData
    {
        // Metadata
        public string playerName;
        public DateTime creationDate;
        public DateTime lastSaveDate;
        public string saveVersion;
        public TimeSpan playTime;
        public string thumbnailData; // Base64 encoded screenshot

        // Custom saveable data
        public Dictionary<string, object> saveableObjectData;

        // Major system data
        public PlayerProgression.ProgressionSaveData playerProgressionData;
        public string relationshipNetworkData;
        public string characterSystemData;
        public string npcLifecycleData;
        public string resourceSystemData;
        public string lifeResourceData;
        public string socialActivityData;
        public string progressionEventSystemData;

        // Additional system data can be added as needed
    }

    /// <summary>
    /// Metadata about a save file.
    /// </summary>
    [Serializable]
    public class SaveMetadata
    {
        public int slotNumber;
        public string playerName;
        public DateTime creationDate;
        public DateTime lastSaveDate;
        public string saveVersion;
        public TimeSpan playTime;
        public string thumbnailData; // Base64 encoded screenshot
        public bool isAutosave;
    }
}