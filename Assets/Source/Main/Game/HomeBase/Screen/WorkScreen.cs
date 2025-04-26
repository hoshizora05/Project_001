using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using LifeResourceSystem;
using CharacterSystem;
using PlayerProgression;
using ResourceManagement;
using InformationManagementUI;
using SocialActivity;
using TMPro;

/// <summary>
/// WorkScreen - 仕事選択画面
/// HomeScreen.cs を参考にフレームワークへの参照を正しく行う実装例
/// </summary>
public class WorkScreen : BaseScreen
{
    [Header("UI参照")]
    [SerializeField] private Slider staminaSlider;
    [SerializeField] private TextMeshProUGUI staminaPercentText;
    [SerializeField] private TextMeshProUGUI playerMoneyText;       // プレイヤー所持金表示
    [SerializeField] private Transform jobListParent;    // 仕事ボタン配置用の親オブジェクト
    [SerializeField] private GameObject jobButtonPrefab; // 仕事ボタンのプレハブ
    [SerializeField] private TextMeshProUGUI jobDetailText;         // 選択した仕事の詳細UI表示

    [Header("データ参照")]
    [Tooltip("選択可能な仕事のデータ(ScriptableObject)リスト")]
    [SerializeField] private List<JobData> jobDataList;

    // 現在選択中の仕事
    private JobData selectedJob = null;

    private SocialActivitySystem socialActivitySystem;
    private LifeResourceManager lifeResourceManager;
    private RelationshipNetwork relationshipManager;
    private PlayerProgressionManager playerManager;
    private ResourceManagementSystem resourceManager;
    private LifeResourceSystem.EventBusReference eventBus;
    private ICurrencySystem currencySystem;
    private IInventorySystem inventoryManager;
    private CharacterAdapter characterAdapter;

    protected override void Awake()
    {
        socialActivitySystem = SocialActivitySystem.Instance;
        lifeResourceManager = LifeResourceManager.Instance;
        relationshipManager = RelationshipNetwork.Instance;
        playerManager = PlayerProgressionManager.Instance;
        resourceManager = ResourceManagementSystem.Instance;
        eventBus = LifeResourceSystem.ServiceLocator.Get<LifeResourceSystem.EventBusReference>();
        currencySystem = ResourceManagement.ServiceLocator.Get<ICurrencySystem>();
        inventoryManager = ResourceManagement.ServiceLocator.Get<IInventorySystem>();
    }

    private void Start()
    {
        // UI初期化
        RefreshUI();
        GenerateJobButtons();
        ClearJobDetails();


        UpdateStaminaDisplay();
    }

    protected override void OnEnable()
    {
        // Subscribe to events
        SubscribeToEvents();
    }

    protected override void OnDisable()
    {
        // Unsubscribe from events
        UnsubscribeFromEvents();
    }

    private void Initialize(CharacterAdapter characterAdapter)
    {
        this.characterAdapter = characterAdapter;
    }

    private void SubscribeToEvents()
    {
        if (eventBus != null)
        {
            // Subscribe to resource events
            lifeResourceManager.SubscribeToResourceEvents(OnResourceEvent);
        }
        else
        {
            Debug.LogError("Event Bus is not available. Some features may not work properly.");
        }
    }

    private void UnsubscribeFromEvents()
    {
        if (eventBus != null)
        {
            // Unsubscribe from resource events
            lifeResourceManager.UnsubscribeFromResourceEvents(OnResourceEvent);
        }
    }

    private void OnResourceEvent(LifeResourceSystem.ResourceEvent resourceEvent)
    {
        switch (resourceEvent.type)
        {

            case LifeResourceSystem.ResourceEvent.ResourceEventType.EnergyChanged:
                UpdateStaminaDisplay();
                break;
        }
    }

    private void UpdateStaminaDisplay()
    {
        if (staminaSlider != null && staminaPercentText != null && resourceManager != null)
        {
            EnergyState energyState = lifeResourceManager.GetResourceState().energy;
            float fatigue = 1f - (energyState.currentEnergy / energyState.maxEnergy);

            staminaSlider.value = fatigue;
            staminaPercentText.text = $"{fatigue * 100:F0}%";

            // Update color based on fatigue level
            Color fatigueColor = Color.green;
            if (fatigue > 0.7f)
                fatigueColor = Color.red;
            else if (fatigue > 0.4f)
                fatigueColor = Color.yellow;

            staminaSlider.fillRect.GetComponent<Image>().color = fatigueColor;
        }
    }

    /// <summary>
    /// UI上に仕事一覧のボタンを生成する
    /// </summary>
    private void GenerateJobButtons()
    {
        // 既存ボタンを全消去
        foreach (Transform child in jobListParent)
        {
            Destroy(child.gameObject);
        }

        // jobDataList の各要素についてボタンを生成
        foreach (var jobData in jobDataList)
        {
            var jobButtonObj = Instantiate(jobButtonPrefab, jobListParent);

            var buttonText = jobButtonObj.GetComponentInChildren<Text>();
            if (buttonText != null)
            {
                buttonText.text = jobData.jobName;
            }

            // ボタンを押したときに OnSelectJob を呼ぶ
            var button = jobButtonObj.GetComponent<Button>();
            if (button != null)
            {
                var capturedJobData = jobData; // ローカルコピーをキャプチャ
                button.onClick.AddListener(() => OnSelectJob(capturedJobData));
            }
        }
    }

    /// <summary>
    /// ボタン押下時に選択された仕事をUIに反映
    /// </summary>
    private void OnSelectJob(JobData jobData)
    {
        selectedJob = jobData;
        DisplayJobDetails(jobData);
    }

    /// <summary>
    /// 選択された仕事の詳細を jobDetailText に表示
    /// </summary>
    private void DisplayJobDetails(JobData job)
    {
        if (jobDetailText == null) return;

        // ベース成功率をパーセント表示
        int baseSuccessPercent = Mathf.RoundToInt(job.baseSuccessRate * 100f);

        jobDetailText.text =
            $"<b>{job.jobName}</b>\n" +
            $"{job.jobDescription}\n\n" +
            $"・必要体力: {job.requiredStamina}\n" +
            $"・報酬金額: {job.rewardMoney}\n" +
            $"・成功時 {job.mainParameterKey}+{job.mainParameterGain}\n" +
            $"・ベース成功率: {baseSuccessPercent}%\n";
    }

    /// <summary>
    /// 「確定」ボタンから呼ばれるメソッド。仕事を実行し、成功／失敗処理を行う。
    /// </summary>
    public void OnConfirmJob()
    {
        if (selectedJob == null) return;

        // 1) 体力の取得＆消費
        float currentEnergy = lifeResourceManager.GetResourceState().energy.currentEnergy;
        float maxEnergy = lifeResourceManager.GetResourceState().energy.maxEnergy;
        currentEnergy -= selectedJob.requiredStamina;
        if (currentEnergy < 0f) currentEnergy = 0f;
        bool result = lifeResourceManager.ConsumeEnergy(selectedJob.jobName);

        // 2) 成功判定
        bool isSuccess = CalculateSuccess(selectedJob, currentEnergy);

        // 3) 成否に応じた処理
        if (isSuccess)
        {
            // 3-1) 報酬通貨
            currencySystem.AddCurrency(CurrencyType.StandardCurrency, selectedJob.rewardMoney, selectedJob.jobName, selectedJob.jobDescription);

            // 3-2) パラメータ上昇
            if (!string.IsNullOrEmpty(selectedJob.mainParameterKey))
            {
                StatValue currentStatValue = playerManager.GetStatValue(selectedJob.mainParameterKey);
                float currentValue = currentStatValue.CurrentValue;
                currentValue += selectedJob.mainParameterGain;
                var statChangeEvent = new ProgressionEvent
                {
                    type = ProgressionEvent.ProgressionEventType.StatChange,
                    parameters = new Dictionary<string, object>
                    {
                        { "statId", selectedJob.mainParameterKey },
                        { "baseValueChange", selectedJob.mainParameterGain} 
                    }
                };
                eventBus.Publish(statChangeEvent);
            }

            // 3-3) アイテム獲得
            if (!string.IsNullOrEmpty(selectedJob.rewardItemKey))
            {
                inventoryManager.AddItem(selectedJob.rewardItemKey, 1);
            }

            // 3-4) イベント発生チェック
            TryTriggerEvent(selectedJob);

            Debug.Log($"仕事 [{selectedJob.jobName}] を成功しました。");
        }
        else
        {
            Debug.Log($"仕事 [{selectedJob.jobName}] は失敗しました。");
        }

        //// 3-5) SocialActivitySystemへログ等の記録
        //socialActivitySystem.RecordWorkAttempt(selectedJob.jobName, isSuccess);

        // 4) UI再描画
        RefreshUI();
    }

    /// <summary>
    /// 成功判定を行う。体力が0の場合は自動失敗。baseSuccessRate をベースに体力やパラメータで補正。
    /// </summary>
    private bool CalculateSuccess(JobData job, float currentStamina)
    {
        // 体力0なら必ず失敗
        if (currentStamina <= 0f)
        {
            return false;
        }

        // ベース成功率
        float successRate = job.baseSuccessRate;

        // 推奨体力による補正
        if (job.recommendedStamina > 0f)
        {
            float staminaRatio = Mathf.Clamp01(currentStamina / job.recommendedStamina);
            successRate *= staminaRatio;
        }

        // メインパラメータによる補正
        if (!string.IsNullOrEmpty(job.mainParameterKey))
        {
            StatValue currentStatValue = playerManager.GetStatValue(selectedJob.mainParameterKey);
            float currentValue = currentStatValue.CurrentValue;
            successRate += (currentValue * job.mainParameterInfluence) / currentStatValue.MaxValue;
        }

        // 0～1にクランプ
        successRate = Mathf.Clamp01(successRate);

        // ランダム判定
        float randVal = Random.value;
        return (randVal <= successRate);
    }

    /// <summary>
    /// イベント発生確率チェックし、ProgressionAndEventSystemを通してイベント発火。
    /// </summary>
    private void TryTriggerEvent(JobData job)
    {
        // イベント確率が設定されている場合に発生
        if (job.eventTriggerProbability <= 0f || string.IsNullOrEmpty(job.eventID))
            return;

        float randVal = Random.value;
        if (randVal <= job.eventTriggerProbability)
        {
            eventManager.TriggerEvent(job.eventID, null); // プレイヤーを ICharacter 型で渡すなら適宜調整
            Debug.Log($"イベント [{job.eventID}] が発生しました。");
        }
    }

    /// <summary>
    /// 体力や所持金などをUIに反映
    /// </summary>
    private void RefreshUI()
    {
        // 体力表示
        if (staminaPercentText != null)
        {
            float currentEnergy = lifeResourceManager.GetResourceState().energy.currentEnergy;
            staminaPercentText.text = $"Stamina: {currentEnergy:F1}";
        }

        // 所持金表示
        if (playerMoneyText != null)
        {
            float currentMoney = currencySystem.GetCurrencyAmount(CurrencyType.StandardCurrency);
            playerMoneyText.text = $"Money: {currentMoney:F0}";
        }
    }

    /// <summary>
    /// 仕事詳細部分をリセット
    /// </summary>
    private void ClearJobDetails()
    {
        if (jobDetailText != null)
        {
            jobDetailText.text = "仕事を選択してください。";
        }
    }
}
