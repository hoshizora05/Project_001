using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace SceneManagement
{
    /// <summary>
    /// Scene manager that handles scene transitions and maintains scene history.
    /// </summary>
    public class SceneManager : MonoBehaviour
    {
        [SerializeField] private Transform _sceneContainer;
        [SerializeField] public GameObject _loadingScreenPrefab;
        [SerializeField] private int _sceneHistoryCapacity = 10;

        private SceneFactory _sceneFactory;
        public LoadingScreen _loadingScreen;
        private ITransitionEffect _transitionEffect;
        private Stack<SceneHistoryEntry> _sceneHistory;
        private MonoBehaviour _currentScene;
        
        private bool _isTransitioning;

        /// <summary>
        /// Event that is triggered when a scene transition starts.
        /// </summary>
        public event Action<Type> OnSceneTransitionStarted;

        /// <summary>
        /// Event that is triggered when a scene transition completes.
        /// </summary>
        public event Action<Type> OnSceneTransitionCompleted;

        /// <summary>
        /// Gets the current scene.
        /// </summary>
        public MonoBehaviour CurrentScene => _currentScene;

        /// <summary>
        /// Indicates whether a scene transition is in progress.
        /// </summary>
        public bool IsTransitioning => _isTransitioning;

        /// <summary>
        /// Gets the transition effect being used.
        /// </summary>
        public ITransitionEffect TransitionEffect => _transitionEffect;

        private static SceneManager _instance;

        /// <summary>
        /// Gets the singleton instance of the SceneManager.
        /// </summary>
        public static SceneManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("SceneManager");
                    _instance = go.AddComponent<SceneManager>();
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

            if (_sceneContainer == null)
            {
                var containerGo = new GameObject("SceneContainer");
                containerGo.transform.SetParent(transform);
                _sceneContainer = containerGo.transform;
            }

            _sceneFactory = new SceneFactory(_sceneContainer);
            _sceneHistory = new Stack<SceneHistoryEntry>(_sceneHistoryCapacity);

            InitializeLoadingScreen();
            InitializeTransitionEffect();
        }

        private void InitializeLoadingScreen()
        {
            if (_loadingScreenPrefab != null)
            {
                var loadingScreenInstance = Instantiate(_loadingScreenPrefab, transform);
                _loadingScreen = loadingScreenInstance.GetComponent<LoadingScreen>();

                if (_loadingScreen == null)
                {
                    Debug.LogError("Loading screen prefab does not have a LoadingScreen component.");
                    Destroy(loadingScreenInstance);
                }
            }
            else
            {
                Debug.LogWarning("No loading screen prefab assigned. Loading screen functionality will be disabled.");
            }
        }

        private void InitializeTransitionEffect()
        {
            if (_loadingScreen != null)
            {
                // Use the loading screen's canvas group for transitions
                var canvasGroup = _loadingScreen.GetComponent<CanvasGroup>();
                if (canvasGroup != null)
                {
                    _transitionEffect = new FadeTransitionEffect(canvasGroup);
                    return;
                }
            }

            // Fallback to no transition effect
            _transitionEffect = new NoTransitionEffect();
        }

        /// <summary>
        /// Sets a custom transition effect.
        /// </summary>
        /// <param name="effect">The transition effect to use.</param>
        public void SetTransitionEffect(ITransitionEffect effect)
        {
            _transitionEffect = effect ?? new NoTransitionEffect();
        }

        /// <summary>
        /// Registers a scene prefab.
        /// </summary>
        /// <typeparam name="TScene">The type of scene.</typeparam>
        /// <param name="prefab">The scene prefab.</param>
        public void RegisterScene<TScene>(GameObject prefab) where TScene : MonoBehaviour
        {
            _sceneFactory.RegisterScenePrefab<TScene>(prefab);
        }

        /// <summary>
        /// Shows a scene without parameters.
        /// </summary>
        /// <typeparam name="TScene">The type of scene to show.</typeparam>
        /// <param name="addToHistory">Indicates whether to add the current scene to history.</param>
        /// <returns>An awaitable task that returns the scene instance.</returns>
        public Task<TScene> ShowScene<TScene>(bool addToHistory = true) where TScene : Scene
        {
            return ShowScene<TScene, EmptySceneParams>(null, addToHistory);
        }

        /// <summary>
        /// Shows a scene with parameters.
        /// </summary>
        /// <typeparam name="TScene">The type of scene to show.</typeparam>
        /// <typeparam name="TParams">The type of parameters for the scene.</typeparam>
        /// <param name="parameters">The parameters for the scene.</param>
        /// <param name="addToHistory">Indicates whether to add the current scene to history.</param>
        /// <returns>An awaitable task that returns the scene instance.</returns>
        public async Task<TScene> ShowScene<TScene, TParams>(TParams parameters, bool addToHistory = true) 
            where TScene : Scene<TParams> 
            where TParams : class, new()
        {
            if (_isTransitioning)
            {
                Debug.LogWarning("Scene transition already in progress. Ignoring request.");
                return null;
            }

            _isTransitioning = true;
            var sceneType = typeof(TScene);
            OnSceneTransitionStarted?.Invoke(sceneType);

            try
            {
                // Show loading screen if available
                if (_loadingScreen != null)
                {
                    await _loadingScreen.Show();
                }
                else
                {
                    await _transitionEffect.PlayExitingEffect();
                }

                // Hide current scene if exists
                if (_currentScene != null)
                {
                    if (_currentScene is Scene scene)
                    {
                        if (addToHistory)
                        {
                            // Add current scene to history
                            _sceneHistory.Push(new SceneHistoryEntry(_currentScene.GetType(), scene));
                            if (_sceneHistory.Count > _sceneHistoryCapacity)
                            {
                                // Remove oldest entry by creating a new stack with only the newest entries
                                var tempArray = _sceneHistory.ToArray();
                                _sceneHistory.Clear();
                                
                                // Push all except the last one (oldest) back to the stack
                                for (int i = 0; i < tempArray.Length - 1; i++)
                                {
                                    _sceneHistory.Push(tempArray[i]);
                                }
                            }
                        }

                        await scene.Hide();
                    }
                }

                // Create and show the new scene
                TScene newScene = await _sceneFactory.GetOrCreateSceneInstance<TScene>();
                newScene.Initialize(parameters);
                await newScene.Show();

                _currentScene = newScene;

                // Hide loading screen if available
                if (_loadingScreen != null)
                {
                    await _loadingScreen.Hide();
                }
                else
                {
                    await _transitionEffect.PlayEnteringEffect();
                }

                OnSceneTransitionCompleted?.Invoke(sceneType);
                return newScene;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error during scene transition to {sceneType.Name}: {ex.Message}");
                if (_loadingScreen != null && _loadingScreen.IsVisible)
                {
                    await _loadingScreen.Hide();
                }
                throw;
            }
            finally
            {
                _isTransitioning = false;
            }
        }

        /// <summary>
        /// Goes back to the previous scene.
        /// </summary>
        /// <returns>True if successfully went back, false if there's no previous scene.</returns>
        public async Task<bool> GoBack()
        {
            if (_sceneHistory.Count == 0)
            {
                Debug.Log("No previous scene in history.");
                return false;
            }

            if (_isTransitioning)
            {
                Debug.LogWarning("Scene transition already in progress. Ignoring request.");
                return false;
            }

            var historyEntry = _sceneHistory.Pop();
            var sceneType = historyEntry.SceneType;
            var sceneBehavior = historyEntry.SceneBehavior;

            _isTransitioning = true;
            OnSceneTransitionStarted?.Invoke(sceneType);

            try
            {
                // Show loading screen if available
                if (_loadingScreen != null)
                {
                    await _loadingScreen.Show();
                }
                else
                {
                    await _transitionEffect.PlayExitingEffect();
                }

                // Hide current scene if exists
                if (_currentScene != null)
                {
                    if (_currentScene is Scene scene)
                    {
                        await scene.Hide();
                    }
                }

                // Show the previous scene
                await sceneBehavior.Show();
                _currentScene = sceneBehavior as MonoBehaviour;

                // Hide loading screen if available
                if (_loadingScreen != null)
                {
                    await _loadingScreen.Hide();
                }
                else
                {
                    await _transitionEffect.PlayEnteringEffect();
                }

                OnSceneTransitionCompleted?.Invoke(sceneType);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error during back navigation to {sceneType.Name}: {ex.Message}");
                if (_loadingScreen != null && _loadingScreen.IsVisible)
                {
                    await _loadingScreen.Hide();
                }
                return false;
            }
            finally
            {
                _isTransitioning = false;
            }
        }

        /// <summary>
        /// Clears the scene history.
        /// </summary>
        public void ClearHistory()
        {
            _sceneHistory.Clear();
        }

        /// <summary>
        /// Updates the loading progress.
        /// </summary>
        /// <param name="progress">The progress value (0-1).</param>
        public void UpdateLoadingProgress(float progress)
        {
            if (_loadingScreen != null)
            {
                _loadingScreen.UpdateProgress(progress);
            }
        }

        /// <summary>
        /// Destroys all scene instances and clears history.
        /// </summary>
        public void DestroyAllScenes()
        {
            _sceneFactory.ClearAllSceneInstances();
            _sceneHistory.Clear();
            _currentScene = null;
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        /// <summary>
        /// An entry in the scene history.
        /// </summary>
        private struct SceneHistoryEntry
        {
            public Type SceneType { get; }
            public Scene SceneBehavior { get; }

            public SceneHistoryEntry(Type sceneType, Scene sceneBehavior)
            {
                SceneType = sceneType;
                SceneBehavior = sceneBehavior;
            }
        }
    }
}