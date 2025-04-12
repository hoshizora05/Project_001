using System.Threading.Tasks;
using UnityEngine;

namespace SceneManagement
{
    /// <summary>
    /// Interface for scene transition effects.
    /// </summary>
    public interface ITransitionEffect
    {
        /// <summary>
        /// Plays the transition entering effect.
        /// </summary>
        /// <returns>An awaitable task.</returns>
        Task PlayEnteringEffect();

        /// <summary>
        /// Plays the transition exiting effect.
        /// </summary>
        /// <returns>An awaitable task.</returns>
        Task PlayExitingEffect();
    }

    /// <summary>
    /// A transition effect that fades the screen.
    /// </summary>
    public class FadeTransitionEffect : ITransitionEffect
    {
        private readonly CanvasGroup _canvasGroup;
        private readonly float _fadeDuration;

        /// <summary>
        /// Initializes a new instance of the <see cref="FadeTransitionEffect"/> class.
        /// </summary>
        /// <param name="canvasGroup">The canvas group to fade.</param>
        /// <param name="fadeDuration">The duration of the fade effect.</param>
        public FadeTransitionEffect(CanvasGroup canvasGroup, float fadeDuration = 0.5f)
        {
            _canvasGroup = canvasGroup;
            _fadeDuration = fadeDuration;
        }

        /// <summary>
        /// Plays the fade-in effect.
        /// </summary>
        /// <returns>An awaitable task.</returns>
        public async Task PlayEnteringEffect()
        {
            await FadeTo(0f);
        }

        /// <summary>
        /// Plays the fade-out effect.
        /// </summary>
        /// <returns>An awaitable task.</returns>
        public async Task PlayExitingEffect()
        {
            await FadeTo(1f);
        }

        private async Task FadeTo(float targetAlpha)
        {
            float startAlpha = _canvasGroup.alpha;
            float elapsedTime = 0;

            while (elapsedTime < _fadeDuration)
            {
                elapsedTime += Time.deltaTime;
                float t = Mathf.Clamp01(elapsedTime / _fadeDuration);
                _canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
                await Task.Yield();
            }

            _canvasGroup.alpha = targetAlpha;
        }
    }

    /// <summary>
    /// A transition effect that does nothing.
    /// </summary>
    public class NoTransitionEffect : ITransitionEffect
    {
        /// <summary>
        /// Plays the entering effect (does nothing).
        /// </summary>
        /// <returns>A completed task.</returns>
        public Task PlayEnteringEffect() => Task.CompletedTask;

        /// <summary>
        /// Plays the exiting effect (does nothing).
        /// </summary>
        /// <returns>A completed task.</returns>
        public Task PlayExitingEffect() => Task.CompletedTask;
    }
}