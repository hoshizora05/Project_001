using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.SceneManagement;
using UnityEngine.AddressableAssets.ResourceLocators;
using Debug = UnityEngine.Debug;

namespace AddressableManagementSystem
{
    /// <summary>
    /// Manages resource loading and unloading using Unity's Addressables system.
    /// Provides a robust service layer for asset management with performance monitoring,
    /// memory optimization, and simplified API for game systems.
    /// </summary>
    public class AddressableResourceManager : MonoBehaviour
    {
        #region Singleton

        private static AddressableResourceManager _instance;

        /// <summary>
        /// Singleton instance of the AddressableResourceManager
        /// </summary>
        public static AddressableResourceManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject go = new GameObject("[AddressableResourceManager]");
                    _instance = go.AddComponent<AddressableResourceManager>();
                    DontDestroyOnLoad(go);
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

            _ = Initialize();
        }

        #endregion

        #region Inspector Properties

        [Header("Memory Management")]
        [Tooltip("Maximum memory usage in MB before auto-unloading unused assets")]
        [SerializeField] private float _memoryBudgetMB = 512f;

        [Tooltip("Auto-unload assets not accessed for this many seconds")]
        [SerializeField] private float _assetTimeoutSeconds = 180f;

        [Header("Loading Settings")]
        [Tooltip("Maximum concurrent asset loading operations")]
        [SerializeField] private int _maxConcurrentOperations = 5;

        [Tooltip("Interval between memory usage checks")]
        [SerializeField] private float _memoryCheckInterval = 30f;

        //[Tooltip("Whether to automatically initialize Addressables on startup")]
        //[SerializeField] private bool _autoInitialize = true;

        [Header("Debugging")]
        [Tooltip("Enable verbose logging of all asset operations")]
        [SerializeField] private bool _verboseLogging = false;

        [Tooltip("Log memory usage statistics during memory checks")]
        [SerializeField] private bool _logMemoryUsage = false;

        #endregion

        #region Fields and Properties

        // Reference tracking
        private Dictionary<string, AsyncOperationHandle> _activeOperations = new Dictionary<string, AsyncOperationHandle>();
        private Dictionary<string, int> _referenceCounters = new Dictionary<string, int>();
        private Dictionary<string, float> _lastAccessTimes = new Dictionary<string, float>();
        private Dictionary<string, object> _loadedAssets = new Dictionary<string, object>();

        // Performance monitoring
        private Dictionary<string, float> _loadTimings = new Dictionary<string, float>();
        private Dictionary<string, List<float>> _loadTimeHistory = new Dictionary<string, List<float>>();
        private Dictionary<string, LoadPriority> _assetPriorities = new Dictionary<string, LoadPriority>();

        // Loading queue
        private Queue<LoadRequest> _loadQueue = new Queue<LoadRequest>();
        private int _activeLoadOperations = 0;
        private bool _isProcessingQueue = false;

        // Initialization tracking
        private bool _isInitialized = false;
        private bool _isInitializing = false;
        public bool IsInitialized => _isInitialized;

        //// Memory tracking
        //private float _lastMemoryCheck = 0f;

        #endregion

        #region Classes and Enums

        /// <summary>
        /// Defines the priority of a load operation.
        /// </summary>
        public enum LoadPriority
        {
            /// <summary>
            /// Low priority - loaded after normal and high priority requests.
            /// </summary>
            Low = 0,

            /// <summary>
            /// Normal priority - standard loading priority.
            /// </summary>
            Normal = 1,

            /// <summary>
            /// High priority - loaded before normal and low priority requests.
            /// </summary>
            High = 2
        }

        /// <summary>
        /// Base class for load requests to track loading info.
        /// </summary>
        private class LoadRequest
        {
            public string Key { get; set; }
            public LoadPriority Priority { get; set; }
            public float StartTime { get; set; }
        }

        /// <summary>
        /// Generic load request with typed callback.
        /// </summary>
        /// <typeparam name="T">Type of asset being loaded</typeparam>
        private class LoadRequest<T> : LoadRequest
        {
            public Action<T> Callback { get; set; }
        }

        /// <summary>
        /// Contains memory usage information for the Addressables system.
        /// </summary>
        public class MemoryUsageInfo
        {
            /// <summary>
            /// Current total memory usage in MB.
            /// </summary>
            public float TotalMemoryUsageMB { get; set; }

            /// <summary>
            /// Configured memory budget in MB.
            /// </summary>
            public float MemoryBudgetMB { get; set; }

            /// <summary>
            /// Percentage of memory budget used.
            /// </summary>
            public float MemoryUsagePercent => (TotalMemoryUsageMB / MemoryBudgetMB) * 100f;

            /// <summary>
            /// Number of active operations.
            /// </summary>
            public int ActiveOperationsCount { get; set; }

            /// <summary>
            /// Number of loaded assets.
            /// </summary>
            public int LoadedAssetsCount { get; set; }
        }

        /// <summary>
        /// Contains loading statistics for an asset.
        /// </summary>
        public class AssetLoadingStats
        {
            /// <summary>
            /// Asset key.
            /// </summary>
            public string Key { get; set; }

            /// <summary>
            /// Whether the asset is currently loaded.
            /// </summary>
            public bool IsLoaded { get; set; }

            /// <summary>
            /// Current reference count for the asset.
            /// </summary>
            public int ReferenceCount { get; set; }

            /// <summary>
            /// Time taken for the last load operation in seconds.
            /// </summary>
            public float LastLoadTime { get; set; }

            /// <summary>
            /// History of load times for this asset.
            /// </summary>
            public List<float> LoadTimeHistory { get; set; }

            /// <summary>
            /// Average load time for this asset in seconds.
            /// </summary>
            public float AverageLoadTime => LoadTimeHistory.Count > 0 ? LoadTimeHistory.Average() : 0f;

            /// <summary>
            /// Last assigned loading priority for this asset.
            /// </summary>
            public LoadPriority Priority { get; set; }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Initializes the Addressable system and prepares for asset loading.
        /// </summary>
        /// <returns>Task that completes when initialization is finished</returns>
        public async Task<bool> Initialize()
        {
            if (_isInitialized)
                return true;

            if (_isInitializing)
            {
                Log("Initialization already in progress");

                // Wait for initialization to complete
                while (_isInitializing)
                    await Task.Delay(100);

                return _isInitialized;
            }

            _isInitializing = true;
            Log("Initializing Addressable Resource Manager");

            try
            {
                // Initialize Addressables
                var initOperation = Addressables.InitializeAsync();
                await initOperation.Task;

                if (initOperation.Status == AsyncOperationStatus.Failed)
                {
                    LogError("Failed to initialize Addressables: " + initOperation.OperationException);
                    _isInitializing = false;
                    return false;
                }

                _isInitialized = true;
                Log("Addressable Resource Manager initialized successfully");

                StartCoroutine(MemoryMonitoringRoutine());

                return true;
            }
            catch (Exception e)
            {
                LogError("Error initializing Addressables: " + e.Message);
                _isInitialized = false;
                return false;
            }
            finally
            {
                _isInitializing = false;
            }
        }

        /// <summary>
        /// Loads an asset asynchronously with the specified key and type.
        /// </summary>
        /// <typeparam name="T">Type of asset to load</typeparam>
        /// <param name="key">Addressable key for the asset</param>
        /// <param name="priority">Priority of the load operation</param>
        /// <param name="callback">Optional callback when loading completes</param>
        /// <returns>AsyncOperationHandle for the loading operation</returns>
        public AsyncOperationHandle<T> LoadAssetAsync<T>(string key, LoadPriority priority = LoadPriority.Normal, Action<T> callback = null)
        {
            if (!_isInitialized && !_isInitializing)
            {
                LogWarning("Attempting to load asset before initialization. Auto-initializing...");
                _ = Initialize();
            }

            if (_loadedAssets.TryGetValue(key, out object asset) && asset is T cachedAsset)
            {
                Log($"Asset already loaded: {key}, returning cached instance");

                // Update reference counter and last access time
                IncrementReferenceCounter(key);
                _lastAccessTimes[key] = Time.realtimeSinceStartup;

                // Create a completed operation handle
                var completedHandle = Addressables.ResourceManager.CreateCompletedOperation(cachedAsset, null);

                // Invoke callback if provided
                callback?.Invoke(cachedAsset);

                return completedHandle;
            }

            // Check if we already have an active operation for this key
            if (_activeOperations.TryGetValue(key, out AsyncOperationHandle existingOperation))
            {
                Log($"Operation already in progress for: {key}, returning existing handle");

                // Increment reference counter
                IncrementReferenceCounter(key);

                // Add callback to the existing operation if provided
                if (callback != null)
                {
                    existingOperation.Completed += (op) =>
                    {
                        if (op.Status == AsyncOperationStatus.Succeeded)
                        {
                            var result = (T)op.Result;
                            callback(result);
                        }
                    };
                }

                return existingOperation.Convert<T>();
            }

            // Create a new load request and add it to the queue
            var request = new LoadRequest<T>
            {
                Key = key,
                Priority = priority,
                Callback = callback,
                StartTime = Time.realtimeSinceStartup
            };

            _assetPriorities[key] = priority;
            IncrementReferenceCounter(key);

            _loadQueue.Enqueue(request);

            if (!_isProcessingQueue)
                StartCoroutine(ProcessLoadQueue());

            // Return a placeholder handle that will be properly linked when the asset loads
            return Addressables.ResourceManager.CreateCompletedOperation<T>(default, null);
        }

        /// <summary>
        /// Loads a scene additively using Addressables.
        /// </summary>
        /// <param name="key">Addressable key for the scene</param>
        /// <param name="activateOnLoad">Whether to activate the scene immediately after loading</param>
        /// <param name="priority">Loading priority (higher values are loaded sooner)</param> // �� priority �p�����[�^��ǉ�
        /// <returns>AsyncOperationHandle for the scene loading operation</returns>
        public AsyncOperationHandle<SceneInstance> LoadSceneAsync(string key, bool activateOnLoad = true, int priority = 100) // �� priority ������ǉ�
        {
            if (!_isInitialized && !_isInitializing)
            {
                LogWarning("Attempting to load scene before initialization. Auto-initializing...");
                _ = Initialize(); // Initialize ��񓯊��ŊJ�n���A������҂��Ȃ�
            }

            Log($"Loading scene: {key}, activateOnLoad: {activateOnLoad}, priority: {priority}"); // �� ���O�� priority ��ǉ�

            var loadSceneMode = LoadSceneMode.Additive;
            // ������ �C���ӏ� ������
            // ActivationMode �ϐ��͕s�v�Ȃ̂ō폜
            // Addressables.LoadSceneAsync �̑�3������ activateOnLoad ��n���A��4������ priority ��n��
            var operation = Addressables.LoadSceneAsync(key, loadSceneMode, activateOnLoad, priority);
            // ������������������������

            // Track the operation - �L�[�ŒǐՂ���ꍇ�A�V�[���͏d�����ă��[�h�ł��Ȃ��_�ɒ��ӂ��K�v
            // ��ӂȃL�[�܂��̓n���h�����̂��Ǘ�������@������
            // �����ł̓L�[�ŒP���ɒǐՂ�������ɂȂ��Ă���
            _activeOperations[key] = operation; // �㏑�������\������
            IncrementReferenceCounter(key);

            // Set up completion callback to update tracking
            operation.Completed += handle =>
            {
                // �� �������̃n���h������ƎQ�ƃJ�E���g�Ǘ���������
                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    var startTime = _lastAccessTimes.GetValueOrDefault(key, Time.realtimeSinceStartup); // �J�n���Ԃ��L�^�ł��Ă��Ȃ��������ɑΏ�
                    var loadTime = Time.realtimeSinceStartup - startTime;
                    _loadTimings[key] = loadTime;

                    if (!_loadTimeHistory.ContainsKey(key))
                        _loadTimeHistory[key] = new List<float>();

                    _loadTimeHistory[key].Add(loadTime);

                    // �� ���������Ō�̃A�N�Z�X���Ԃ��X�V
                    _lastAccessTimes[key] = Time.realtimeSinceStartup;

                    Log($"Scene loaded successfully: {key} (Scene Name: {handle.Result.Scene.name}) in {loadTime:F2} seconds");

                    // �� �V�[�������[�h���ꂽ���Ƃ��������߂� _loadedAssets �� SceneInstance ��ǉ����� (�K�v�ł����)
                    _loadedAssets[key] = handle;

                }
                else
                {
                    LogError($"Failed to load scene: {key} - {handle.OperationException}");
                    DecrementReferenceCounter(key); // ���s���ɎQ�ƃJ�E���g�����炷
                    // �� ���s���ɂ��A�N�e�B�u�I�y���[�V��������폜
                    if (_activeOperations.ContainsKey(key) && _activeOperations[key].Equals(handle))
                    {
                        _activeOperations.Remove(key);
                    }
                    // �� ���s���� loadedAssets ������폜 (�����ǉ����Ă����ꍇ)
                    _loadedAssets.Remove(key);
                    // �� ���s���� lastAccessTimes ������폜 (�s�v�Ȃ�)
                    _lastAccessTimes.Remove(key);
                }

                // �� ����������A�N�e�B�u�I�y���[�V��������폜���� (����/���s��킸)
                // �������A�n���h�����̂͌�� UnloadSceneAsync �Ŏg�����߁A�L�[�Ƃ̕R�t���͕ێ�����K�v�����邩������Ȃ�
                // �����ł͊��� = �A�N�e�B�u�ȃ��[�h�����ł͂Ȃ��A�Ƃ��č폜���������
                // if (_activeOperations.ContainsKey(key) && _activeOperations[key].OperationHandle == handle.OperationHandle)
                // {
                //      _activeOperations.Remove(key);
                // }
            };

            // �� �J�n���Ԃ��L�^ (�R�[���o�b�N���ŎQ�Ƃ��邽��)
            _lastAccessTimes[key] = Time.realtimeSinceStartup;

            return operation;
        }

        /// <summary>
        /// Unloads a scene loaded through Addressables using its handle.
        /// </summary>
        /// <param name="sceneHandle">AsyncOperationHandle of the loaded scene to unload</param> // �� SceneInstance �̑���� Handle �𐄏�
        /// <returns>AsyncOperationHandle for the unload operation</returns>
        // public AsyncOperationHandle<SceneInstance> UnloadSceneAsync(SceneInstance sceneInstance) // ���̃V�O�l�`��
        public AsyncOperationHandle<SceneInstance> UnloadSceneAsync(AsyncOperationHandle<SceneInstance> sceneHandle) // �� �n���h���Ŏ󂯎��V�O�l�`���ɕύX
        {
            // �� �n���h�����L�����A�������Ă��邩�Ȃǂ��`�F�b�N
            if (!sceneHandle.IsValid() || !sceneHandle.IsDone || sceneHandle.Status != AsyncOperationStatus.Succeeded)
            {
                LogWarning($"Attempting to unload an invalid or failed scene handle.");
                // ���s�����n���h����Ԃ����Anull ��Ԃ����A�G���[�𓊂��邩�Ȃǂ̐݌v���K�v
                return Addressables.ResourceManager.CreateCompletedOperation<SceneInstance>(default, "Invalid handle provided for unloading.");
            }

            SceneInstance sceneInstance = sceneHandle.Result;
            Log($"Unloading scene: {sceneInstance.Scene.name}");

            // �� UnloadSceneAsync �ɂ� SceneInstance �܂��� Handle ��n����
            var operation = Addressables.UnloadSceneAsync(sceneHandle); // �n���h���ŃA�����[�h

            // �� �A�����[�h�������̏��� (�Q�ƃJ�E���g�Ȃ�)
            operation.Completed += unloadHandle =>
            {
                if (unloadHandle.Status == AsyncOperationStatus.Succeeded)
                {
                    Log($"Scene unloaded successfully: {sceneInstance.Scene.name}");
                    // �� �V�[���ɑΉ�����L�[�������ĎQ�ƃJ�E���g�����炷�K�v������
                    // ���̂��߂ɂ́ALoadSceneAsync ���� Handle �� Key �̃}�b�s���O��ێ�����K�v������
                    // ��: private Dictionary<AsyncOperationHandle, string> _sceneHandleToKeyMap = new Dictionary<AsyncOperationHandle, string>();
                    // LoadSceneAsync �̊����R�[���o�b�N�Ń}�b�v�ɒǉ����A�����ŃL�[���擾���� DecrementReferenceCounter ���Ă�
                    // string key = FindKeyForSceneHandle(sceneHandle); // ���̂悤�ȃ��\�b�h���K�v
                    // if (!string.IsNullOrEmpty(key))
                    // {
                    //     DecrementReferenceCounter(key);
                    //     // ���̃g���b�L���O�����폜
                    //     _loadedAssets.Remove(key);
                    //     _lastAccessTimes.Remove(key);
                    //     // _sceneHandleToKeyMap.Remove(sceneHandle); // �}�b�v������폜
                    // } else {
                    //     LogWarning($"Could not find original key for unloaded scene: {sceneInstance.Scene.name}");
                    // }
                }
                else
                {
                    LogError($"Failed to unload scene: {sceneInstance.Scene.name} - {unloadHandle.OperationException}");
                }
                // �� �A�����[�h����̊�����A���̃��[�h�n���h�����������K�v�����邩�m�F
                // �ʏ�AUnloadSceneAsync ����������Γ����ŉ������邱�Ƃ��������A�h�L�������g�m�F����
                // Addressables.Release(sceneHandle); // �K�v�ł���΂����ŉ��
            };


            return operation;
        }

        // �� ReleaseAsset �� Handle ���󂯎��I�[�o�[���[�h��ǉ�����ƕ֗�
        /// <summary>
        /// Reduces the reference count for an asset using its handle, and unloads it if no longer needed.
        /// </summary>
        /// <param name="handle">AsyncOperationHandle for the asset</param>
        public void ReleaseAsset(AsyncOperationHandle handle)
        {
            // �� Handle ���� Key �������郍�W�b�N���K�v
            // ��: _activeOperations �� _loadedAssets ���t�������邩�A�ʓr�}�b�s���O������
            // string key = FindKeyForHandle(handle); // ���̂悤�ȃ��\�b�h���K�v
            // if (!string.IsNullOrEmpty(key))
            // {
            //     ReleaseAsset(key); // �L�[�ŉ����������̃��\�b�h���Ă�
            // }
            // else if (handle.IsValid()) // �L�[��������Ȃ��Ă��n���h�����L���Ȃ��������݂�
            // {
            //      LogWarning($"Could not find key for handle {handle.OperationHandle}, releasing handle directly.");
            //      Addressables.Release(handle);
            // �� ���ډ�������ꍇ�A�Q�ƃJ�E���g�Ȃǂ̊Ǘ��Ɩ������Ȃ������ӂ��K�v
            // } else {
            //      LogWarning($"Attempting to release an invalid handle.");
            // }

            // === ������ (�L�[��������Ȃ��ꍇ�A���ڃn���h�����) ===
            // ���̎����͎Q�ƃJ�E���g�ƘA�����Ȃ��\�������邽�ߒ���
            if (handle.IsValid())
            {
                // ��: �L�[������ł��Ȃ��ꍇ�ł��n���h�����������B
                // �{���̓L�[�ƕR�t���ĎQ�ƃJ�E���g���Ǘ����ׂ��B

                //LogWarning($"Releasing handle {handle.OperationHandle} directly without key mapping. Reference counting might be inaccurate.");

                Addressables.Release(handle);
            }
        }

        /// <summary>
        /// Reduces the reference count for an asset using its key, and unloads it if no longer needed.
        /// </summary>
        /// <param name="key">Addressable key for the asset</param>
        public void ReleaseAsset(string key) // �����̃L�[�ŉ�����郁�\�b�h
        {
            if (!_referenceCounters.ContainsKey(key))
            {
                LogWarning($"Attempting to release asset that isn't tracked or already released: {key}");
                return;
            }

            DecrementReferenceCounter(key);

            // If reference count reaches zero, mark asset for potential unloading or unload immediately
            if (_referenceCounters[key] <= 0)
            {
                Log($"Asset reference count reached zero: {key}. Marked for potential unload.");
                // �� �����������ꍇ:
                // if (_activeOperations.TryGetValue(key, out AsyncOperationHandle handle))
                // {
                //     Log($"Unloading asset immediately: {key}");
                //     Addressables.Release(handle);
                //     _activeOperations.Remove(key);
                //     _referenceCounters.Remove(key);
                //     _lastAccessTimes.Remove(key);
                //     _loadedAssets.Remove(key);
                // }
                // else
                // {
                //      LogWarning($"Could not find handle to release for key: {key}");
                // }

                // �� �܂��́A�^�C���A�E�g/�������Ď��ɔC����ꍇ:
                _lastAccessTimes[key] = Time.realtimeSinceStartup;
            }
        }

        // ... ���̃��\�b�h ...

        // �� SceneHandle �� Key �̃}�b�s���O�̂��߂̕⏕�I�ȏ������K�v�ɂȂ�\��������
        // ��:
        // private Dictionary<AsyncOperationHandle, string> _handleToKeyMap = new Dictionary<AsyncOperationHandle, string>();
        // private string FindKeyForHandle(AsyncOperationHandle handle) { ... }
        // private void MapHandleToKey(AsyncOperationHandle handle, string key) { ... }
        // LoadAssetCoroutine �� LoadSceneAsync �̊������� MapHandleToKey ���Ă�
        // ReleaseAsset(AsyncOperationHandle) �� UnloadSceneAsync �̊������� FindKeyForHandle ���Ă�

        /// <summary>
        /// Force unloads an asset regardless of reference count.
        /// Use with caution as this may cause issues if the asset is still in use.
        /// </summary>
        /// <param name="key">Addressable key for the asset</param>
        public void ForceUnloadAsset(string key)
        {
            if (_activeOperations.TryGetValue(key, out AsyncOperationHandle handle))
            {
                Log($"Force unloading asset: {key}");
                Addressables.Release(handle);

                _activeOperations.Remove(key);
                _referenceCounters.Remove(key);
                _lastAccessTimes.Remove(key);
                _loadedAssets.Remove(key);
            }
            else
            {
                LogWarning($"Attempted to force unload non-loaded asset: {key}");
            }
        }

        /// <summary>
        /// Preloads a collection of assets in the background.
        /// </summary>
        /// <typeparam name="T">Type of assets to preload</typeparam>
        /// <param name="keys">Collection of keys to preload</param>
        /// <param name="priority">Priority of the preload operations</param>
        /// <returns>Task that completes when all assets are preloaded</returns>
        public async Task PreloadAssetsAsync<T>(IEnumerable<string> keys, LoadPriority priority = LoadPriority.Low)
        {
            if (!_isInitialized && !_isInitializing)
            {
                LogWarning("Attempting to preload assets before initialization. Auto-initializing...");
                await Initialize();
            }

            Log($"Preloading {keys.Count()} assets with priority {priority}");

            var tasks = new List<Task>();

            foreach (var key in keys)
            {
                var loadTask = Task.Run(async () =>
                {
                    var handle = LoadAssetAsync<T>(key, priority);
                    await handle.Task;

                    if (handle.Status == AsyncOperationStatus.Failed)
                    {
                        LogError($"Failed to preload asset: {key} - {handle.OperationException}");
                    }
                });

                tasks.Add(loadTask);
            }

            await Task.WhenAll(tasks);
            Log($"Preloading complete for {keys.Count()} assets");
        }

        /// <summary>
        /// Gets a list of all currently loaded assets.
        /// </summary>
        /// <returns>Dictionary of asset keys and their reference counts</returns>
        public Dictionary<string, int> GetLoadedAssets()
        {
            return new Dictionary<string, int>(_referenceCounters);
        }

        /// <summary>
        /// Gets the memory usage statistics for the Addressables system.
        /// </summary>
        /// <returns>MemoryUsageInfo containing current usage data</returns>
        public MemoryUsageInfo GetMemoryUsage()
        {
            var info = new MemoryUsageInfo
            {
                TotalMemoryUsageMB = GetTotalMemoryUsage(),
                MemoryBudgetMB = _memoryBudgetMB,
                ActiveOperationsCount = _activeOperations.Count,
                LoadedAssetsCount = _loadedAssets.Count
            };

            return info;
        }

        /// <summary>
        /// Gets the loading statistics for a specific asset key.
        /// </summary>
        /// <param name="key">Asset key to get statistics for</param>
        /// <returns>AssetLoadingStats containing loading history</returns>
        public AssetLoadingStats GetAssetLoadingStats(string key)
        {
            var stats = new AssetLoadingStats
            {
                Key = key,
                IsLoaded = _loadedAssets.ContainsKey(key),
                ReferenceCount = _referenceCounters.GetValueOrDefault(key, 0),
                LastLoadTime = _loadTimings.GetValueOrDefault(key, 0),
                LoadTimeHistory = _loadTimeHistory.GetValueOrDefault(key, new List<float>()),
                Priority = _assetPriorities.GetValueOrDefault(key, LoadPriority.Normal)
            };

            return stats;
        }

        /// <summary>
        /// Sets the memory budget for the Addressables system.
        /// </summary>
        /// <param name="budgetMB">New memory budget in MB</param>
        public void SetMemoryBudget(float budgetMB)
        {
            if (budgetMB <= 0)
            {
                LogError("Memory budget must be greater than 0");
                return;
            }

            _memoryBudgetMB = budgetMB;
            Log($"Memory budget set to {_memoryBudgetMB} MB");

            // Trigger a memory check to potentially unload assets
            CheckMemoryUsage();
        }

        /// <summary>
        /// Checks if an asset is currently loaded.
        /// </summary>
        /// <param name="key">Asset key to check</param>
        /// <returns>True if the asset is loaded, false otherwise</returns>
        public bool IsAssetLoaded(string key)
        {
            return _loadedAssets.ContainsKey(key);
        }

        /// <summary>
        /// Gets the loaded asset by key if it's already loaded.
        /// </summary>
        /// <typeparam name="T">Type of asset to get</typeparam>
        /// <param name="key">Asset key</param>
        /// <param name="asset">Output asset if loaded</param>
        /// <returns>True if the asset was found, false otherwise</returns>
        public bool TryGetLoadedAsset<T>(string key, out T asset)
        {
            if (_loadedAssets.TryGetValue(key, out object loadedAsset) && loadedAsset is T typedAsset)
            {
                asset = typedAsset;

                // Update last access time
                _lastAccessTimes[key] = Time.realtimeSinceStartup;

                return true;
            }

            asset = default;
            return false;
        }

        /// <summary>
        /// Gets all available asset labels in the Addressables system.
        /// </summary>
        /// <returns>IEnumerable of label strings</returns>
        public IEnumerable<string> GetAllLabels()
        {
            var labels = new HashSet<string>();

            foreach (var locator in Addressables.ResourceLocators)
            {
                foreach (var key in locator.Keys)
                {
                    if (key is string)
                        continue;

                    if (key is IKeyEvaluator)
                    {
                        // Handle label keys
                        var labelKey = key as IKeyEvaluator;
                        if (labelKey != null)
                        {
                            labels.Add(labelKey.ToString());
                        }
                    }
                }
            }

            return labels;
        }

        /// <summary>
        /// Gets all available asset keys in the Addressables system.
        /// </summary>
        /// <returns>IEnumerable of key strings</returns>
        public IEnumerable<object> GetAllKeys()
        {
            var keys = new HashSet<object>();

            foreach (var locator in Addressables.ResourceLocators)
            {
                foreach (var key in locator.Keys)
                {
                    keys.Add(key);
                }
            }

            return keys;
        }

        /// <summary>
        /// Checks if an update is available for the Addressables content.
        /// </summary>
        /// <returns>Task with boolean indicating if an update is available</returns>
        public async Task<bool> CheckForUpdatesAsync()
        {
            try
            {
                Log("Checking for Addressables content updates");

                var checkOperation = Addressables.CheckForCatalogUpdates(false);
                await checkOperation.Task;

                if (checkOperation.Status == AsyncOperationStatus.Failed)
                {
                    LogError("Failed to check for catalog updates: " + checkOperation.OperationException);
                    return false;
                }

                var catalogsToUpdate = checkOperation.Result;
                bool hasUpdate = catalogsToUpdate != null && catalogsToUpdate.Count > 0;

                Log($"Update check complete. Updates available: {hasUpdate}, Catalogs to update: {(hasUpdate ? catalogsToUpdate.Count : 0)}");

                return hasUpdate;
            }
            catch (Exception e)
            {
                LogError("Error checking for updates: " + e.Message);
                return false;
            }
        }

        /// <summary>
        /// Updates Addressables content if updates are available.
        /// </summary>
        /// <param name="progressCallback">Optional callback to report update progress</param>
        /// <returns>Task with boolean indicating if update was successful</returns>
        public async Task<bool> UpdateContentAsync(Action<float> progressCallback = null)
        {
            try
            {
                Log("Starting Addressables content update");

                // Check for updates first
                var checkOperation = Addressables.CheckForCatalogUpdates(false);
                await checkOperation.Task;

                if (checkOperation.Status == AsyncOperationStatus.Failed)
                {
                    LogError("Failed to check for catalog updates: " + checkOperation.OperationException);
                    return false;
                }

                var catalogsToUpdate = checkOperation.Result;
                if (catalogsToUpdate == null || catalogsToUpdate.Count == 0)
                {
                    Log("No updates available");
                    return true; // No updates needed is still considered successful
                }

                Log($"Found {catalogsToUpdate.Count} catalogs to update");

                // Update catalogs
                var updateOperation = Addressables.UpdateCatalogs(catalogsToUpdate);

                // Monitor progress
                while (!updateOperation.IsDone)
                {
                    progressCallback?.Invoke(updateOperation.PercentComplete);
                    await Task.Delay(100);
                }

                if (updateOperation.Status == AsyncOperationStatus.Failed)
                {
                    LogError("Failed to update catalogs: " + updateOperation.OperationException);
                    return false;
                }

                Log($"Successfully updated {catalogsToUpdate.Count} catalogs");
                return true;
            }
            catch (Exception e)
            {
                LogError("Error updating content: " + e.Message);
                return false;
            }
        }

        /// <summary>
        /// Clears the local Addressables cache.
        /// </summary>
        /// <returns>Task with boolean indicating if cache clear was successful</returns>
        public async Task<bool> ClearCacheAsync()
        {
            try
            {
                Log("Clearing Addressables cache");

                var clearOperation = Addressables.ClearDependencyCacheAsync(Addressables.ResourceLocators.Select(l => l.Keys) , true);
                await clearOperation.Task;

                if (clearOperation.Status == AsyncOperationStatus.Failed)
                {
                    LogError("Failed to clear cache: " + clearOperation.OperationException);
                    return false;
                }

                Log("Successfully cleared Addressables cache");
                return true;
            }
            catch (Exception e)
            {
                LogError("Error clearing cache: " + e.Message);
                return false;
            }
        }

        #endregion

        #region Internal Methods

        private IEnumerator ProcessLoadQueue()
        {
            _isProcessingQueue = true;

            while (_loadQueue.Count > 0)
            {
                // Wait if we've reached the maximum concurrent operations
                if (_activeLoadOperations >= _maxConcurrentOperations)
                {
                    yield return null;
                    continue;
                }

                // Get the next request from the queue, prioritizing higher priority items
                LoadRequest nextRequest = GetNextRequest();

                if (nextRequest == null)
                {
                    yield return null;
                    continue;
                }

                _activeLoadOperations++;

                // Start the actual load operation
                StartCoroutine(LoadAssetCoroutine(nextRequest));
            }

            _isProcessingQueue = false;
        }

        private LoadRequest GetNextRequest()
        {
            if (_loadQueue.Count == 0)
                return null;

            // If we have high priority requests, process them first
            var highPriorityRequests = _loadQueue.Where(r => r.Priority == LoadPriority.High).ToList();
            if (highPriorityRequests.Count > 0)
            {
                var request = highPriorityRequests[0];
                _loadQueue = new Queue<LoadRequest>(_loadQueue.Where(r => r != request));
                return request;
            }

            // Otherwise, process the next request in queue
            return _loadQueue.Dequeue();
        }

        private IEnumerator LoadAssetCoroutine(LoadRequest request)
        {
            string key = request.Key;

            // Update last access time to track when we started loading
            _lastAccessTimes[key] = Time.realtimeSinceStartup;

            Log($"Starting load operation for: {key} with priority {request.Priority}");

            AsyncOperationHandle handle = new AsyncOperationHandle();

            bool failedToStart = false;

            try
            {
                // Create the load operation handle based on type
                if (request is LoadRequest<GameObject>)
                {
                    handle = Addressables.LoadAssetAsync<GameObject>(key);
                }
                else if (request is LoadRequest<Texture2D>)
                {
                    handle = Addressables.LoadAssetAsync<Texture2D>(key);
                }
                else if (request is LoadRequest<AudioClip>)
                {
                    handle = Addressables.LoadAssetAsync<AudioClip>(key);
                }
                else if (request is LoadRequest<ScriptableObject>)
                {
                    handle = Addressables.LoadAssetAsync<ScriptableObject>(key);
                }
                else if (request is LoadRequest<Material>)
                {
                    handle = Addressables.LoadAssetAsync<Material>(key);
                }
                else if (request is LoadRequest<Sprite>)
                {
                    handle = Addressables.LoadAssetAsync<Sprite>(key);
                }
                else if (request is LoadRequest<TextAsset>)
                {
                    handle = Addressables.LoadAssetAsync<TextAsset>(key);
                }
                else
                {
                    // Default to object type
                    handle = Addressables.LoadAssetAsync<object>(key);
                }

                // Add to active operations
                _activeOperations[key] = handle;


            }
            catch (Exception e)
            {
                LogError($"Exception starting load for asset {key}: {e.Message}");
                DecrementReferenceCounter(key);
                failedToStart = true;
            }

            if (failedToStart)
            {
                _activeLoadOperations--;
                yield break;
            }

            yield return handle;

            try
            {
                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    float loadTime = Time.realtimeSinceStartup - _lastAccessTimes[key];
                    _loadTimings[key] = loadTime;

                    if (!_loadTimeHistory.ContainsKey(key))
                        _loadTimeHistory[key] = new List<float>();

                    _loadTimeHistory[key].Add(loadTime);
                    _loadedAssets[key] = handle.Result;
                    _lastAccessTimes[key] = Time.realtimeSinceStartup;

                    Log($"Successfully loaded asset: {key} in {loadTime:F2} seconds");

                    switch (request)
                    {
                        case LoadRequest<GameObject> gameObjectRequest when handle.Result is GameObject gameObject:
                            gameObjectRequest.Callback?.Invoke(gameObject);
                            break;
                        case LoadRequest<Texture2D> textureRequest when handle.Result is Texture2D texture:
                            textureRequest.Callback?.Invoke(texture);
                            break;
                        case LoadRequest<AudioClip> audioRequest when handle.Result is AudioClip audio:
                            audioRequest.Callback?.Invoke(audio);
                            break;
                        case LoadRequest<ScriptableObject> scriptableRequest when handle.Result is ScriptableObject scriptable:
                            scriptableRequest.Callback?.Invoke(scriptable);
                            break;
                        case LoadRequest<Material> materialRequest when handle.Result is Material material:
                            materialRequest.Callback?.Invoke(material);
                            break;
                        case LoadRequest<Sprite> spriteRequest when handle.Result is Sprite sprite:
                            spriteRequest.Callback?.Invoke(sprite);
                            break;
                        case LoadRequest<TextAsset> textRequest when handle.Result is TextAsset text:
                            textRequest.Callback?.Invoke(text);
                            break;
                    }
                }
                else
                {
                    LogError($"Failed to load asset: {key} - {handle.OperationException}");
                    DecrementReferenceCounter(key);
                    _activeOperations.Remove(key);
                }
            }
            finally
            {
                _activeLoadOperations--;
            }
        }

        private void IncrementReferenceCounter(string key)
        {
            if (!_referenceCounters.ContainsKey(key))
            {
                _referenceCounters[key] = 0;
            }

            _referenceCounters[key]++;
            Log($"Incremented reference count for {key}: {_referenceCounters[key]}");
        }

        private void DecrementReferenceCounter(string key)
        {
            if (!_referenceCounters.ContainsKey(key))
            {
                LogWarning($"Attempted to decrement reference count for untracked asset: {key}");
                return;
            }

            _referenceCounters[key]--;
            Log($"Decremented reference count for {key}: {_referenceCounters[key]}");

            if (_referenceCounters[key] <= 0)
            {
                _referenceCounters[key] = 0; // Ensure it doesn't go negative
                _lastAccessTimes[key] = Time.realtimeSinceStartup; // Mark last access time for timeout unloading
            }
        }

        private IEnumerator MemoryMonitoringRoutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(_memoryCheckInterval);
                CheckMemoryUsage();
            }
        }

        private void CheckMemoryUsage()
        {
            float currentMemoryUsage = GetTotalMemoryUsage();
            float memoryPercent = (currentMemoryUsage / _memoryBudgetMB) * 100f;

            if (_logMemoryUsage)
            {
                Log($"Memory usage: {currentMemoryUsage:F2} MB / {_memoryBudgetMB} MB ({memoryPercent:F1}%)");
                Log($"Active operations: {_activeOperations.Count}, Loaded assets: {_loadedAssets.Count}");
            }

            // Check if we need to unload assets due to memory pressure
            if (currentMemoryUsage > _memoryBudgetMB * 0.9f)
            {
                Log($"Memory usage high ({memoryPercent:F1}% of budget), unloading unused assets");
                UnloadUnusedAssets();
            }

            // Unload any assets that haven't been accessed for the timeout period
            UnloadTimedOutAssets();
        }

        private void UnloadUnusedAssets()
        {
            var assetsToUnload = new List<string>();

            // Find assets with zero reference count
            foreach (var entry in _referenceCounters)
            {
                if (entry.Value <= 0)
                {
                    assetsToUnload.Add(entry.Key);
                }
            }

            // Unload assets
            foreach (var key in assetsToUnload)
            {
                if (_activeOperations.TryGetValue(key, out AsyncOperationHandle handle))
                {
                    Log($"Unloading unused asset: {key}");
                    Addressables.Release(handle);

                    _activeOperations.Remove(key);
                    _referenceCounters.Remove(key);
                    _lastAccessTimes.Remove(key);
                    _loadedAssets.Remove(key);
                }
            }

            Log($"Unloaded {assetsToUnload.Count} unused assets");

            // Force a garbage collection to clean up memory
            System.GC.Collect();
        }

        private void UnloadTimedOutAssets()
        {
            float currentTime = Time.realtimeSinceStartup;
            var assetsToUnload = new List<string>();

            // Find assets that have timed out
            foreach (var entry in _lastAccessTimes)
            {
                string key = entry.Key;
                float lastAccessTime = entry.Value;

                // If the asset is referenced, skip it
                if (_referenceCounters.TryGetValue(key, out int count) && count > 0)
                    continue;

                float timeSinceLastAccess = currentTime - lastAccessTime;

                if (timeSinceLastAccess > _assetTimeoutSeconds)
                {
                    assetsToUnload.Add(key);
                }
            }

            // Unload assets
            foreach (var key in assetsToUnload)
            {
                if (_activeOperations.TryGetValue(key, out AsyncOperationHandle handle))
                {
                    Log($"Unloading timed out asset: {key} (not accessed for {currentTime - _lastAccessTimes[key]:F1} seconds)");
                    Addressables.Release(handle);

                    _activeOperations.Remove(key);
                    _referenceCounters.Remove(key);
                    _lastAccessTimes.Remove(key);
                    _loadedAssets.Remove(key);
                }
            }

            if (assetsToUnload.Count > 0)
            {
                Log($"Unloaded {assetsToUnload.Count} timed out assets");
            }
        }

        private float GetTotalMemoryUsage()
        {
            // This is an estimate as Unity doesn't provide direct memory tracking
            // For more accurate measurements, a custom memory profiler would be needed

            // Get current system memory usage
            float totalMemoryMB = System.GC.GetTotalMemory(false) / (1024f * 1024f);

            return totalMemoryMB;
        }

        private void Log(string message)
        {
            if (_verboseLogging)
            {
                Debug.Log($"[AddressableResourceManager] {message}");
            }
        }

        private void LogWarning(string message)
        {
            Debug.LogWarning($"[AddressableResourceManager] {message}");
        }

        private void LogError(string message)
        {
            Debug.LogError($"[AddressableResourceManager] {message}");
        }
        #endregion
    }
}