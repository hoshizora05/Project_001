using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;

using LifeResourceSystem;
using CharacterSystem;
using PlayerProgression;
using ResourceManagement;
using InformationManagementUI;
using SocialActivity;

public class HomeScreen : BaseScreen
{
    [Header("References")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private GameObject characterDisplayArea;
    [SerializeField] private AudioSource uiAudioSource;

    [Header("Info Panel")]
    [SerializeField] private TextMeshProUGUI currentDateTimeText;
    [SerializeField] private TextMeshProUGUI currentLocationText;
    [SerializeField] private TextMeshProUGUI moneyAmountText;
    [SerializeField] private Image moneyIcon;
    [SerializeField] private Slider fatigueSlider;
    [SerializeField] private TextMeshProUGUI fatiguePercentText;

    [Header("Action Menu")]
    [SerializeField] private Button statusButton;
    [SerializeField] private Button scheduleButton;
    [SerializeField] private Button inventoryButton;
    [SerializeField] private Button workButton;
    [SerializeField] private Button selfImprovementButton;
    [SerializeField] private Button socialMediaButton;
    [SerializeField] private Button goOutButton;

    [Header("Character Widget Panel")]
    [SerializeField] private Transform characterWidgetContainer;
    [SerializeField] private GameObject characterWidgetPrefab;
    [SerializeField] private int maxDisplayedCharacters = 5;

    [Header("Background Images")]
    [SerializeField] private Sprite morningBackground;
    [SerializeField] private Sprite afternoonBackground;
    [SerializeField] private Sprite eveningBackground;
    [SerializeField] private Sprite nightBackground;

    // References to managers
    [SerializeField] private InformationManagementUISystem infoUISystem;
    private SocialActivitySystem socialActivitySystem;
    private LifeResourceManager resourceManager;
    private RelationshipNetwork relationshipManager;
    private PlayerProgressionManager playerManager;
    private ResourceManagementSystem inventoryManager;

    // Cache for character widgets
    private Dictionary<string, CharacterWidget> activeCharacterWidgets = new Dictionary<string, CharacterWidget>();

    // System references
    private LifeResourceSystem.EventBusReference eventBus;

    private void Awake()
    {
        // Get references to all required systems
        socialActivitySystem = SocialActivitySystem.Instance;
        resourceManager = LifeResourceManager.Instance;
        relationshipManager = RelationshipNetwork.Instance;
        playerManager = PlayerProgressionManager.Instance;
        inventoryManager = ResourceManagementSystem.Instance;
        eventBus = LifeResourceSystem.ServiceLocator.Get<LifeResourceSystem.EventBusReference>();

        // Initialize UI
        InitializeUI();
    }

    private void OnEnable()
    {
        // Subscribe to events
        SubscribeToEvents();
    }

    private void OnDisable()
    {
        // Unsubscribe from events
        UnsubscribeFromEvents();
    }

    private void Start()
    {
        // Update UI with initial values
        UpdateTimeDisplay();
        UpdateLocationDisplay();
        UpdateMoneyDisplay();
        UpdateFatigueDisplay();
        UpdateBackground();
        RefreshCharacterWidgets();

        // Set up button listeners
        SetupButtonListeners();
    }

    private void InitializeUI()
    {
        // Ensure everything is properly set up
        if (backgroundImage == null)
            Debug.LogError("Background Image reference is missing!");

        if (characterWidgetPrefab == null)
            Debug.LogError("Character Widget Prefab is missing!");
    }

    private void SubscribeToEvents()
    {
        if (eventBus != null)
        {
            // Subscribe to time change events
            eventBus.Subscribe<TimeChangedEvent>(OnTimeChanged);

            // Subscribe to resource events
            resourceManager.SubscribeToResourceEvents(OnResourceEvent);

            // Subscribe to relationship events
            CharacterEventBus.Instance.Subscribe("relationship_change", OnRelationshipChanged);
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
            // Unsubscribe from time change events
            eventBus.Unsubscribe<TimeChangedEvent>(OnTimeChanged);

            // Unsubscribe from resource events
            resourceManager.UnsubscribeFromResourceEvents(OnResourceEvent);

            // Unsubscribe from relationship events
            CharacterEventBus.Instance.Unsubscribe("relationship_change", OnRelationshipChanged);
        }
    }
    private void OnRelationshipChanged(PsychologyEvent evt)
    {
        if (evt is CharacterSystem.RelationshipChangeEvent relationshipEvent)
        {
            // Update relationship widgets if the player is involved
            string playerId = playerManager.GetPlayerId();

            if (relationshipEvent.sourceEntityId == playerId || relationshipEvent.targetEntityId == playerId)
            {
                RefreshCharacterWidgets();
            }
        }
    }

    private void SetupButtonListeners()
    {
        // Set up button click listeners
        statusButton.onClick.AddListener(OnStatusButtonClicked);
        scheduleButton.onClick.AddListener(OnScheduleButtonClicked);
        inventoryButton.onClick.AddListener(OnInventoryButtonClicked);
        workButton.onClick.AddListener(OnWorkButtonClicked);
        selfImprovementButton.onClick.AddListener(OnSelfImprovementButtonClicked);
        socialMediaButton.onClick.AddListener(OnSocialMediaButtonClicked);
        goOutButton.onClick.AddListener(OnGoOutButtonClicked);
    }

    #region Event Handlers

    private void OnTimeChanged(TimeChangedEvent timeEvent)
    {
        // Update time-related displays
        UpdateTimeDisplay();
        UpdateBackground();

        // Check for scheduled events
        CheckScheduledEvents();
    }

    private void OnResourceEvent(LifeResourceSystem.ResourceEvent resourceEvent)
    {
        switch (resourceEvent.type)
        {
            case LifeResourceSystem.ResourceEvent.ResourceEventType.MoneyChanged:
                UpdateMoneyDisplay();
                break;

            case LifeResourceSystem.ResourceEvent.ResourceEventType.EnergyChanged:
                UpdateFatigueDisplay();
                break;

            case LifeResourceSystem.ResourceEvent.ResourceEventType.TimeAdvanced:
                UpdateLocationDisplay();
                break;
        }
    }

    private void OnRelationshipChanged(string sourceId, string targetId, float newStrength)
    {
        // Update relationship widgets if the player is involved
        string playerId = playerManager.GetPlayerId();

        if (sourceId == playerId || targetId == playerId)
        {
            RefreshCharacterWidgets();
        }
    }

    #endregion

    #region Display Updates

    private void UpdateTimeDisplay()
    {
        // timeManager 変数の代わりに SocialActivitySystem.TimeSystem を使用
        var timeSystem = socialActivitySystem?.TimeSystem;
        if (timeSystem != null && currentDateTimeText != null)
        {
            // TimeSystem の実装クラスである TimeManager にキャスト
            var timeManager = timeSystem as SocialActivity.TimeManager;
            if (timeManager != null)
            {
                GameDate currentDate = timeManager.GetCurrentGameDate();
                string dayOfWeek = currentDate.GetDayOfWeekName();
                string timeOfDay = currentDate.GetTimeOfDayName();

                currentDateTimeText.text = $"{dayOfWeek}・{timeOfDay}";
            }
        }
    }

    private void UpdateLocationDisplay()
    {
        if (currentLocationText != null)
        {
            string currentLocation = "自宅";  // Default to home

            // Get actual location from location system if available
            var locationSystem = SocialActivitySystem.Instance?.LocationSystem;
            if (locationSystem != null && socialActivitySystem != null)
            {
                // ここを修正 - LocationManager クラスを直接キャストして使用
                var locationManager = locationSystem as LocationManager;
                if (locationManager != null)
                {
                    // プレイヤーキャラクターの取得方法も変更
                    string playerId = playerManager.GetPlayerId();
                    // CharacterManager から対応するキャラクターを取得
                    CharacterManager.Character character = CharacterManager.Instance.GetCharacter(playerId);

                    if (character != null)
                    {
                        // アダプターを使ってSocialActivityシステムで使えるようにする
                        SocialActivity.ICharacter playerCharacter = new CharacterAdapter(character);

                        GameLocation playerLocation = locationManager.GetCharacterLocation(playerCharacter);
                        if (playerLocation != null)
                        {
                            currentLocation = playerLocation.Name;
                        }
                    }
                }
            }

            currentLocationText.text = currentLocation;
        }
    }

    private void UpdateMoneyDisplay()
    {
        if (moneyAmountText != null && resourceManager != null)
        {
            float moneyAmount = resourceManager.GetResourceState().finance.currentMoney;
            moneyAmountText.text = $"¥{moneyAmount:N0}";
        }
    }

    private void UpdateFatigueDisplay()
    {
        if (fatigueSlider != null && fatiguePercentText != null && resourceManager != null)
        {
            EnergyState energyState = resourceManager.GetResourceState().energy;
            float fatigue = 1f - (energyState.currentEnergy / energyState.maxEnergy);

            fatigueSlider.value = fatigue;
            fatiguePercentText.text = $"{fatigue * 100:F0}%";

            // Update color based on fatigue level
            Color fatigueColor = Color.green;
            if (fatigue > 0.7f)
                fatigueColor = Color.red;
            else if (fatigue > 0.4f)
                fatigueColor = Color.yellow;

            fatigueSlider.fillRect.GetComponent<Image>().color = fatigueColor;
        }
    }

    private void UpdateBackground()
    {
        // timeManager 変数の代わりに SocialActivitySystem.TimeSystem を使用
        var timeSystem = socialActivitySystem?.TimeSystem;
        if (backgroundImage != null && timeSystem != null)
        {
            // TimeSystem の実装クラスである TimeManager にキャスト
            var timeManager = timeSystem as SocialActivity.TimeManager;
            if (timeManager != null)
            {
                GameDate currentDate = timeManager.GetCurrentGameDate();
                SocialActivity.TimeOfDay currentTimeOfDay = currentDate.GetTimeOfDay();

                // Set background based on time of day
                switch (currentTimeOfDay)
                {
                    case SocialActivity.TimeOfDay.Morning:
                    case SocialActivity.TimeOfDay.EarlyMorning:
                        backgroundImage.sprite = morningBackground;
                        break;
                    case SocialActivity.TimeOfDay.Afternoon:
                        backgroundImage.sprite = afternoonBackground;
                        break;
                    case SocialActivity.TimeOfDay.Evening:
                        backgroundImage.sprite = eveningBackground;
                        break;
                    case SocialActivity.TimeOfDay.Night:
                    case SocialActivity.TimeOfDay.LateNight:
                        backgroundImage.sprite = nightBackground;
                        break;
                }
            }
        }
    }

    private void RefreshCharacterWidgets()
    {
        if (characterWidgetContainer == null || characterWidgetPrefab == null || relationshipManager == null)
            return;

        // Get player ID
        string playerId = playerManager.GetPlayerId();

        // Get recent characters the player has interacted with
        List<RelationshipData> relationships = relationshipManager.GetAllRelationships(playerId)
            .OrderByDescending(r => r.history.Count > 0 ? r.history.Last().timestamp : 0)
            .Take(maxDisplayedCharacters)
            .ToList();

        // Keep track of characters we need to add/remove
        HashSet<string> currentCharacterIds = new HashSet<string>();

        // Update or create widgets for each character
        foreach (var relationship in relationships)
        {
            string characterId = relationship.sourceId == playerId ? relationship.targetId : relationship.sourceId;
            currentCharacterIds.Add(characterId);

            // Skip if this is the player's relationship with themselves
            if (characterId == playerId) continue;

            // Get or create widget
            CharacterWidget widget;
            if (!activeCharacterWidgets.TryGetValue(characterId, out widget))
            {
                // Create new widget
                GameObject widgetObj = Instantiate(characterWidgetPrefab, characterWidgetContainer);
                widget = widgetObj.GetComponent<CharacterWidget>();
                activeCharacterWidgets.Add(characterId, widget);
            }

            // Update widget data
            widget.UpdateWidget(characterId, relationship);
        }

        // Remove any widgets for characters no longer in the top list
        List<string> charactersToRemove = activeCharacterWidgets.Keys
            .Where(charId => !currentCharacterIds.Contains(charId))
            .ToList();

        foreach (string charId in charactersToRemove)
        {
            GameObject widgetObj = activeCharacterWidgets[charId].gameObject;
            activeCharacterWidgets.Remove(charId);
            Destroy(widgetObj);
        }
    }

    private void CheckScheduledEvents()
    {
        //// Check if any NPC should appear based on scheduled events

        //// Get any scheduled activities involving the player
        //var activitySystem = SocialActivitySystem.Instance?.ActivitySystem;
        //if (activitySystem != null)
        //{
        //    ICharacter playerCharacter = playerManager.GetPlayerCharacter();
        //    List<ScheduledActivity> activities = activitySystem.GetCurrentScheduledActivities(playerCharacter);

        //    // Process any activities that should take place in the player's room
        //    foreach (var activity in activities)
        //    {
        //        // Check if this activity involves an NPC visiting the player's room
        //        if (activity.location.Name == "自宅" && activity.participants.Count > 0)
        //        {
        //            // Show the character in the room
        //            ShowCharacterInRoom(activity.participants[0], activity.activity);
        //        }
        //    }
        //}
    }

    //private void ShowCharacterInRoom(ICharacter character, Activity activity)
    //{
    //    // This would spawn the character model/sprite in the room
    //    // For now, we'll just log this
    //    Debug.Log($"Character {character.Name} appears in room for activity: {activity.Name}");

    //    // In an actual implementation, you would:
    //    // 1. Instantiate the character prefab/sprite
    //    // 2. Position it in the character display area
    //    // 3. Play any entrance animations
    //    // 4. Start the interaction sequence
    //}

    #endregion

    #region Button Click Handlers

    private void OnStatusButtonClicked()
    {
        PlayButtonSound();
        infoUISystem.ShowUI(UIViewType.StatusManagement);
    }

    private void OnScheduleButtonClicked()
    {
        PlayButtonSound();
        infoUISystem.ShowUI(UIViewType.Calendar);
    }

    private void OnInventoryButtonClicked()
    {
        PlayButtonSound();
        // Show inventory UI
        inventoryManager.GetInventorySystem().ShowInventoryUI();
    }

    private void OnWorkButtonClicked()
    {
        PlayButtonSound();
        // Open work/part-time job selection menu
        // This would typically open a sub-menu or new screen
        OpenWorkSelectionMenu();
    }

    private void OnSelfImprovementButtonClicked()
    {
        PlayButtonSound();
        // Open self-improvement activities menu (gym, classes, reading)
        OpenSelfImprovementMenu();
    }

    private void OnSocialMediaButtonClicked()
    {
        PlayButtonSound();
        // Open social media screen to check character posts
        OpenSocialMediaScreen();
    }

    private void OnGoOutButtonClicked()
    {
        PlayButtonSound();
        // Open city map for location selection
        OpenCityMapScreen();
    }

    private void PlayButtonSound()
    {
        if (uiAudioSource != null && uiAudioSource.clip != null)
        {
            uiAudioSource.Play();
        }
    }

    #endregion

    #region Navigation Methods

    private void OpenWorkSelectionMenu()
    {
        // Implementation would open work selection UI or transition to work scene
        Debug.Log("Opening work selection menu");
    }

    private void OpenSelfImprovementMenu()
    {
        // Implementation would open self-improvement activity selection UI
        Debug.Log("Opening self-improvement menu");
    }

    private void OpenSocialMediaScreen()
    {
        // Implementation would open social media UI to view character posts
        Debug.Log("Opening social media screen");
    }

    private void OpenCityMapScreen()
    {
        // Implementation would transition to city map scene for location selection
        Debug.Log("Opening city map screen");
    }

    #endregion
}
