using System.Collections.Generic;
using UnityEngine;
using SceneManagement.Example;

namespace SceneManagement
{
    /// <summary>
    /// Initializes the scene management system.
    /// </summary>
    public class SceneManagerInitializer : MonoBehaviour
    {
        [SerializeField] private GameObject _loadingScreenPrefab;
        [SerializeField] private List<ScenePrefabMapping> _scenePrefabs = new List<ScenePrefabMapping>();
        [SerializeField] private string _initialSceneName;

        [System.Serializable]
        public class ScenePrefabMapping
        {
            public string SceneName;
            public GameObject ScenePrefab;
        }

        private Dictionary<string, GameObject> _sceneNameToPrefabMap = new Dictionary<string, GameObject>();

        private void Awake()
        {
            // Initialize the mapping dictionary
            foreach (var mapping in _scenePrefabs)
            {
                if (mapping.ScenePrefab != null)
                {
                    _sceneNameToPrefabMap[mapping.SceneName] = mapping.ScenePrefab;
                }
            }
        }

        private async void Start()
        {
            // Initialize SceneManager
            var sceneManager = SceneManager.Instance;
            
            // Set the loading screen
            if (_loadingScreenPrefab != null)
            {
                // The SceneManager will instantiate and use this prefab
                sceneManager._loadingScreenPrefab = _loadingScreenPrefab;
            }

            // Register all scenes
            RegisterScenes();

            // Show initial scene if specified
            if (!string.IsNullOrEmpty(_initialSceneName) && _sceneNameToPrefabMap.ContainsKey(_initialSceneName))
            {
                await ShowInitialScene();
            }
        }

        private void RegisterScenes()
        {
            // Register the example scene
            if (_sceneNameToPrefabMap.TryGetValue("ExampleScene", out var exampleScenePrefab))
            {
                SceneManager.Instance.RegisterScene<ExampleScene>(exampleScenePrefab);
            }

            // Register more scenes here...
            // For each scene type, check if a prefab mapping exists and register it
        }

        private async System.Threading.Tasks.Task ShowInitialScene()
        {
            if (_initialSceneName == "ExampleScene")
            {
                await SceneManager.Instance.ShowScene<ExampleScene, ExampleSceneParams>(
                    new ExampleSceneParams
                    {
                        Title = "Initial Scene",
                        Score = 0
                    });
            }
            
            // Add more initial scene types here...
        }
    }
}