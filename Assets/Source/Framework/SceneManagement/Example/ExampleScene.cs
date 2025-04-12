using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace SceneManagement.Example
{
    /// <summary>
    /// Example parameters for the example scene.
    /// </summary>
    public class ExampleSceneParams
    {
        public string Title { get; set; }
        public int Score { get; set; }
    }

    /// <summary>
    /// Example scene that demonstrates how to use the scene management framework.
    /// </summary>
    public class ExampleScene : Scene<ExampleSceneParams>
    {
        [SerializeField] private Text _titleText;
        [SerializeField] private Text _scoreText;
        [SerializeField] private Button _backButton;
        [SerializeField] private Button _nextSceneButton;

        protected override void OnInitialize()
        {
            base.OnInitialize();
            
            if (_titleText != null)
            {
                _titleText.text = Parameters.Title ?? "Example Scene";
            }

            if (_scoreText != null)
            {
                _scoreText.text = $"Score: {Parameters.Score}";
            }

            if (_backButton != null)
            {
                _backButton.onClick.RemoveAllListeners();
                _backButton.onClick.AddListener(OnBackButtonClicked);
            }

            if (_nextSceneButton != null)
            {
                _nextSceneButton.onClick.RemoveAllListeners();
                _nextSceneButton.onClick.AddListener(OnNextSceneButtonClicked);
            }
        }

        protected override async Task OnShow()
        {
            Debug.Log($"ExampleScene OnShow: {Parameters.Title}");
            
            // Add any animation or initialization logic here
            await Task.Delay(500); // Simulate some loading time
        }

        protected override async Task OnHide()
        {
            Debug.Log($"ExampleScene OnHide: {Parameters.Title}");
            
            // Add any cleanup or animation logic here
            await Task.Delay(500); // Simulate some unloading time
        }

        protected override async Task OnFinalize()
        {
            Debug.Log($"ExampleScene OnFinalize: {Parameters.Title}");
            
            // Add any final cleanup logic here
            await Task.CompletedTask;
        }

        private async void OnBackButtonClicked()
        {
            await SceneManager.Instance.GoBack();
        }

        private async void OnNextSceneButtonClicked()
        {
            // Show next scene with parameters
            await SceneManager.Instance.ShowScene<ExampleScene, ExampleSceneParams>(
                new ExampleSceneParams
                {
                    Title = "Next Scene",
                    Score = Parameters.Score + 10
                });
        }
    }
}