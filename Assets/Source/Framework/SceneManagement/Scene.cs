using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;

namespace SceneManagement
{
    /// <summary>
    /// Base class for all scenes in the application.
    /// </summary>
    /// <typeparam name="TParams">The type of parameters this scene accepts.</typeparam>
    public abstract class Scene<TParams> : MonoBehaviour where TParams : class, new()
    {
        /// <summary>
        /// The parameters passed to this scene.
        /// </summary>
        protected TParams Parameters { get; private set; }

        /// <summary>
        /// Indicates whether the scene has been initialized.
        /// </summary>
        public bool IsInitialized { get; private set; }

        /// <summary>
        /// Indicates whether the scene is currently active.
        /// </summary>
        public bool IsActive { get; private set; }

        /// <summary>
        /// Initializes the scene with the specified parameters.
        /// </summary>
        /// <param name="parameters">The parameters to initialize the scene with.</param>
        public void Initialize(TParams parameters = null)
        {
            if (IsInitialized)
            {
                Debug.LogWarning($"Scene {GetType().Name} has already been initialized.");
                return;
            }

            Parameters = parameters ?? new TParams();
            IsInitialized = true;
            OnInitialize();
        }

        /// <summary>
        /// Shows the scene.
        /// </summary>
        /// <returns>An awaitable task.</returns>
        public async Task Show()
        {
            if (!IsInitialized)
            {
                Debug.LogError($"Cannot show Scene {GetType().Name} before it is initialized.");
                return;
            }

            if (IsActive)
            {
                Debug.LogWarning($"Scene {GetType().Name} is already active.");
                return;
            }

            gameObject.SetActive(true);
            IsActive = true;
            await OnShow();
        }

        /// <summary>
        /// Hides the scene.
        /// </summary>
        /// <returns>An awaitable task.</returns>
        public async Task Hide()
        {
            if (!IsActive)
            {
                Debug.LogWarning($"Scene {GetType().Name} is already hidden.");
                return;
            }

            await OnHide();
            IsActive = false;
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Finalizes the scene.
        /// </summary>
        /// <returns>An awaitable task.</returns>
        public async Task Finalize()
        {
            if (!IsInitialized)
            {
                Debug.LogWarning($"Scene {GetType().Name} is not initialized.");
                return;
            }

            if (IsActive)
            {
                await Hide();
            }

            await OnFinalize();
            IsInitialized = false;
            Parameters = null;
        }

        /// <summary>
        /// Called when the scene is initialized.
        /// </summary>
        protected virtual void OnInitialize() { }

        /// <summary>
        /// Called when the scene is shown.
        /// </summary>
        /// <returns>An awaitable task.</returns>
        protected virtual Task OnShow() => Task.CompletedTask;

        /// <summary>
        /// Called when the scene is hidden.
        /// </summary>
        /// <returns>An awaitable task.</returns>
        protected virtual Task OnHide() => Task.CompletedTask;

        /// <summary>
        /// Called when the scene is finalized.
        /// </summary>
        /// <returns>An awaitable task.</returns>
        protected virtual Task OnFinalize() => Task.CompletedTask;
    }

    /// <summary>
    /// Base class for scenes that don't require parameters.
    /// </summary>
    public abstract class Scene : Scene<EmptySceneParams>
    {
    }

    /// <summary>
    /// Empty parameters class for scenes that don't require parameters.
    /// </summary>
    public class EmptySceneParams
    {
    }
}