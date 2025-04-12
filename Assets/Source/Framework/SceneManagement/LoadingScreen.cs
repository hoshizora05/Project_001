using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace SceneManagement
{
    /// <summary>
    /// Loading screen that is displayed during scene transitions.
    /// </summary>
    public class LoadingScreen : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private float _fadeDuration = 0.5f;
        [SerializeField] private Image _progressBar;
        [SerializeField] private Text _progressText;

        private ITransitionEffect _transitionEffect;

        /// <summary>
        /// Indicates whether the loading screen is currently visible.
        /// </summary>
        public bool IsVisible => _canvasGroup.alpha > 0;

        private void Awake()
        {
            if (_canvasGroup == null)
            {
                _canvasGroup = GetComponent<CanvasGroup>();
            }

            if (_canvasGroup == null)
            {
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            _transitionEffect = new FadeTransitionEffect(_canvasGroup, _fadeDuration);
            _canvasGroup.alpha = 0;
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Shows the loading screen.
        /// </summary>
        /// <returns>An awaitable task.</returns>
        public async Task Show()
        {
            gameObject.SetActive(true);
            await _transitionEffect.PlayExitingEffect();
        }

        /// <summary>
        /// Hides the loading screen.
        /// </summary>
        /// <returns>An awaitable task.</returns>
        public async Task Hide()
        {
            await _transitionEffect.PlayEnteringEffect();
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Updates the loading progress.
        /// </summary>
        /// <param name="progress">The progress value (0-1).</param>
        public void UpdateProgress(float progress)
        {
            if (_progressBar != null)
            {
                _progressBar.fillAmount = progress;
            }

            if (_progressText != null)
            {
                _progressText.text = $"{Mathf.Round(progress * 100)}%";
            }
        }
    }
}