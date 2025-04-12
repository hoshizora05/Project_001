using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SceneManagement;
using System.Threading.Tasks;

public class TitleScene : Scene
{
    [Header("îwåi")]
    [SerializeField] private Image _backgroundImage;

    [Header("É^ÉCÉgÉãÉçÉS")]
    [SerializeField] private Image _titleLogo;

    [Header("É{É^ÉìåQ")]
    [SerializeField] private Button _newGameButton;
    [SerializeField] private Button _continueButton;
    [SerializeField] private Button _settingsButton;
    [SerializeField] private Button _galleryButton;
    [SerializeField] private Button _achievementsButton;

    [Header("ÇªÇÃëºUI")]
    [SerializeField] private Image _circleLogo;
    [SerializeField] private TMP_Text _versionText;

    protected override void OnInitialize()
    {
        _newGameButton.onClick.AddListener(OnNewGameClicked);
        _continueButton.onClick.AddListener(OnContinueClicked);
        _settingsButton.onClick.AddListener(OnSettingsClicked);
        _galleryButton.onClick.AddListener(OnGalleryClicked);
        _achievementsButton.onClick.AddListener(OnAchievementsClicked);
    }

    protected override async Task OnShow()
    {
        _versionText.text = $"ver {Application.version}";
        await base.OnShow();
    }

    protected override async Task OnHide()
    {
        await base.OnHide();
    }

    protected override async Task OnFinalize()
    {
        _newGameButton.onClick.RemoveAllListeners();
        _continueButton.onClick.RemoveAllListeners();
        _settingsButton.onClick.RemoveAllListeners();
        _galleryButton.onClick.RemoveAllListeners();
        _achievementsButton.onClick.RemoveAllListeners();

        await base.OnFinalize();
    }

    private async void OnNewGameClicked()
    {
        await SceneManager.Instance.ShowScene<TitleScene>();
        //await SceneManager.Instance.ShowScene<NewGameScene>();
    }

    private async void OnContinueClicked()
    {
        await SceneManager.Instance.ShowScene<TitleScene>();
        //await SceneManager.Instance.ShowScene<ContinueScene>();
    }

    private async void OnSettingsClicked()
    {
        await SceneManager.Instance.ShowScene<TitleScene>();
        //await SceneManager.Instance.ShowScene<SettingsScene>();
    }

    private async void OnGalleryClicked()
    {
        await SceneManager.Instance.ShowScene<TitleScene>();
        //await SceneManager.Instance.ShowScene<GalleryScene>();
    }

    private async void OnAchievementsClicked()
    {
        await SceneManager.Instance.ShowScene<TitleScene>();
        //await SceneManager.Instance.ShowScene<AchievementsScene>();
    }
}
