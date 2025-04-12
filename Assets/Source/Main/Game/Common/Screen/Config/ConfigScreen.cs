using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ConfigScreen : BaseScreen
{
    [Header("音量設定")]
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider seSlider;

    [Header("表示設定")]
    [SerializeField] private Slider textSpeedSlider;
    [SerializeField] private Slider windowAlphaSlider;

    [Header("ボタン")]
    [SerializeField] private Button applyButton;
    [SerializeField] private Button backButton;

    private void Start()
    {
        LoadConfig();

        applyButton.onClick.AddListener(OnApplyClicked);
        backButton.onClick.AddListener(OnBackClicked);
    }

    private void LoadConfig()
    {
    }

    private void SaveConfig()
    {
    }

    private void OnApplyClicked()
    {
        SaveConfig();
    }

    private void OnBackClicked()
    {
    }
}
