using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SceneManagement
{
    /// <summary>
    /// Utility class for loading Unity scenes and integrating them with the scene management framework.
    /// </summary>
    public static class SceneUtility
    {
        /// <summary>
        /// Loads a Unity scene asynchronously.
        /// </summary>
        /// <param name="sceneName">The name of the scene to load.</param>
        /// <param name="loadMode">The load mode for the scene.</param>
        /// <param name="onProgressChanged">Optional callback for progress updates.</param>
        /// <returns>An awaitable task that completes when the scene is loaded.</returns>
        public static async Task LoadUnitySceneAsync(string sceneName, LoadSceneMode loadMode = LoadSceneMode.Single, Action<float> onProgressChanged = null)
        {
            var asyncOperation = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(sceneName, loadMode);
            
            if (asyncOperation == null)
            {
                throw new InvalidOperationException($"Failed to start loading scene: {sceneName}");
            }

            asyncOperation.allowSceneActivation = false;
            
            while (asyncOperation.progress < 0.9f)
            {
                onProgressChanged?.Invoke(asyncOperation.progress);
                await Task.Yield();
            }
            
            // Final progress update before activation
            onProgressChanged?.Invoke(0.9f);
            
            // Allow the scene to activate
            asyncOperation.allowSceneActivation = true;
            
            // Wait for the scene to fully load
            while (!asyncOperation.isDone)
            {
                await Task.Yield();
            }
            
            // Final progress update
            onProgressChanged?.Invoke(1.0f);
        }

        /// <summary>
        /// Loads a Unity scene asynchronously using the scene management framework's loading screen.
        /// </summary>
        /// <param name="sceneName">The name of the scene to load.</param>
        /// <param name="loadMode">The load mode for the scene.</param>
        /// <returns>An awaitable task that completes when the scene is loaded.</returns>
        public static async Task LoadUnitySceneWithLoadingScreenAsync(string sceneName, LoadSceneMode loadMode = LoadSceneMode.Single)
        {
            var sceneManager = SceneManager.Instance;
            
            try
            {
                // Show loading screen
                if (sceneManager._loadingScreen != null)
                {
                    await sceneManager._loadingScreen.Show();
                }
                else
                {
                    await sceneManager.TransitionEffect.PlayExitingEffect();
                }
                
                // Load the scene
                await LoadUnitySceneAsync(sceneName, loadMode, progress => sceneManager.UpdateLoadingProgress(progress));
                
                // Hide loading screen
                if (sceneManager._loadingScreen != null)
                {
                    await sceneManager._loadingScreen.Hide();
                }
                else
                {
                    await sceneManager.TransitionEffect.PlayEnteringEffect();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error loading Unity scene {sceneName}: {ex.Message}");
                
                // Hide loading screen in case of error
                if (sceneManager._loadingScreen != null && sceneManager._loadingScreen.IsVisible)
                {
                    await sceneManager._loadingScreen.Hide();
                }
                
                throw;
            }
        }

        /// <summary>
        /// Finds a scene object of the specified type in the current scene.
        /// </summary>
        /// <typeparam name="T">The type of scene component to find.</typeparam>
        /// <returns>The scene component if found, null otherwise.</returns>
        public static T FindSceneInActiveScene<T>() where T : MonoBehaviour
        {
            return GameObject.FindFirstObjectByType<T>();
        }

        /// <summary>
        /// Unloads a scene asynchronously.
        /// </summary>
        /// <param name="sceneName">The name of the scene to unload.</param>
        /// <returns>An awaitable task that completes when the scene is unloaded.</returns>
        public static async Task UnloadSceneAsync(string sceneName)
        {
            var asyncOperation = UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(sceneName);
            
            if (asyncOperation == null)
            {
                throw new InvalidOperationException($"Failed to start unloading scene: {sceneName}");
            }
            
            while (!asyncOperation.isDone)
            {
                await Task.Yield();
            }
        }
    }
}