using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace SceneManagement
{
    /// <summary>
    /// Factory responsible for creating and managing scene instances.
    /// </summary>
    public class SceneFactory
    {
        private readonly Dictionary<Type, GameObject> _scenePrefabs = new Dictionary<Type, GameObject>();
        private readonly Dictionary<Type, MonoBehaviour> _sceneInstances = new Dictionary<Type, MonoBehaviour>();
        private readonly Transform _sceneParent;

        public SceneFactory(Transform sceneParent)
        {
            _sceneParent = sceneParent;
        }

        /// <summary>
        /// Registers a scene prefab.
        /// </summary>
        /// <typeparam name="TScene">The type of scene.</typeparam>
        /// <param name="prefab">The scene prefab.</param>
        public void RegisterScenePrefab<TScene>(GameObject prefab) where TScene : MonoBehaviour
        {
            var sceneType = typeof(TScene);
            if (_scenePrefabs.ContainsKey(sceneType))
            {
                Debug.LogWarning($"Scene prefab for {sceneType.Name} is already registered. Overwriting.");
            }

            if (!prefab.GetComponent<TScene>())
            {
                throw new ArgumentException($"Prefab does not have a component of type {sceneType.Name}");
            }

            _scenePrefabs[sceneType] = prefab;
        }

        /// <summary>
        /// Gets or creates a scene instance.
        /// </summary>
        /// <typeparam name="TScene">The type of scene.</typeparam>
        /// <returns>The scene instance.</returns>
        public Task<TScene> GetOrCreateSceneInstance<TScene>() where TScene : MonoBehaviour
        {
            var sceneType = typeof(TScene);
            
            if (_sceneInstances.TryGetValue(sceneType, out var instance))
            {
                return Task.FromResult((TScene)instance);
            }

            if (!_scenePrefabs.TryGetValue(sceneType, out var prefab))
            {
                throw new InvalidOperationException($"Scene prefab for {sceneType.Name} is not registered.");
            }

            // Instantiate the scene
            var sceneObject = UnityEngine.Object.Instantiate(prefab, _sceneParent);
            sceneObject.name = sceneType.Name;
            sceneObject.SetActive(false);

            var sceneInstance = sceneObject.GetComponent<TScene>();
            _sceneInstances[sceneType] = sceneInstance;

            return Task.FromResult(sceneInstance);
        }

        /// <summary>
        /// Destroys a scene instance.
        /// </summary>
        /// <typeparam name="TScene">The type of scene.</typeparam>
        public void DestroySceneInstance<TScene>() where TScene : MonoBehaviour
        {
            var sceneType = typeof(TScene);
            
            if (!_sceneInstances.TryGetValue(sceneType, out var instance))
            {
                Debug.LogWarning($"Scene instance for {sceneType.Name} is not created.");
                return;
            }

            UnityEngine.Object.Destroy(instance.gameObject);
            _sceneInstances.Remove(sceneType);
        }

        /// <summary>
        /// Clears all scene instances.
        /// </summary>
        public void ClearAllSceneInstances()
        {
            foreach (var instance in _sceneInstances.Values)
            {
                if (instance != null)
                {
                    UnityEngine.Object.Destroy(instance.gameObject);
                }
            }

            _sceneInstances.Clear();
        }
    }
}