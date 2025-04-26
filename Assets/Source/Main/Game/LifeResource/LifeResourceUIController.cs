using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using LifeResourceSystem;

/// <summary>
/// Displays the current values of the LifeResourceSystem (time, energy, money, social credit)
/// and keeps them in sync by listening to <see cref="ResourceEvent"/>s raised by
/// <see cref="LifeResourceManager"/>.
/// Attach this component to any GameObject in your UI scene and wire the UI references in the Inspector.
/// </summary>
public class LifeResourceUIController : MonoBehaviour
{
    #region Inspector
    [Header("Time UI")] public Text dayText;       // e.g. "Day 3 (Wednesday) Y1"
    [Tooltip("24‑hour clock text (e.g. 13.5h)")] public Text hourText;

    [Header("Energy UI")] public Slider energySlider;     // fill area shows ratio
    public Text energyValueText;                            // "80/120"

    [Header("Money UI")] public Text moneyText;            // "$123.45"

    [Header("Social Credit UI")]
    [Tooltip("Parent that will hold one entry per context (child Text component)")]
    public Transform socialCreditContainer;
    [Tooltip("Prefab with a Text component used for each social‑credit context")] public GameObject socialCreditEntryPrefab;
    #endregion

    private readonly Dictionary<string, Text> _socialTexts = new();

    private LifeResourceManager _manager;

    #region Unity‑lifecycle
    private void Start()
    {
        _manager = LifeResourceManager.Instance;
        if (_manager == null)
        {
            Debug.LogError("LifeResourceManager not found in scene. UI will be disabled.");
            enabled = false;
            return;
        }

        // Initial draw
        RefreshAll(_manager.GetResourceState());

        // Subscribe to future updates
        _manager.SubscribeToResourceEvents(OnResourceEvent);
    }

    private void OnDestroy()
    {
        if (_manager != null)
        {
            _manager.UnsubscribeFromResourceEvents(OnResourceEvent);
        }
    }
    #endregion

    #region Event handling
    private void OnResourceEvent(ResourceEvent evt)
    {
        // Only fetch fresh data on relevant events to avoid unnecessary allocations
        switch (evt.type)
        {
            case ResourceEvent.ResourceEventType.TimeAdvanced:
            case ResourceEvent.ResourceEventType.DayChanged:
                UpdateTimeUI(_manager.GetResourceState().time);
                break;

            case ResourceEvent.ResourceEventType.EnergyChanged:
            case ResourceEvent.ResourceEventType.EnergyStateChanged:
                UpdateEnergyUI(_manager.GetResourceState().energy);
                break;

            case ResourceEvent.ResourceEventType.MoneyChanged:
            case ResourceEvent.ResourceEventType.TransactionProcessed:
                UpdateMoneyUI(_manager.GetResourceState().finance);
                break;

            case ResourceEvent.ResourceEventType.SocialCreditChanged:
                UpdateSocialCreditUI(_manager.GetResourceState().socialCredits);
                break;
        }
    }
    #endregion

    #region Draw helpers
    private void RefreshAll(ResourceState state)
    {
        UpdateTimeUI(state.time);
        UpdateEnergyUI(state.energy);
        UpdateMoneyUI(state.finance);
        UpdateSocialCreditUI(state.socialCredits);
    }

    private void UpdateTimeUI(TimeState time)
    {
        if (dayText != null)
            dayText.text = $"Day {time.day} ({time.dayOfWeek}) Y{time.year}";

        if (hourText != null)
            hourText.text = $"{time.hour:00.0}h";
    }

    private void UpdateEnergyUI(EnergyState energy)
    {
        if (energySlider != null)
        {
            energySlider.maxValue = energy.maxEnergy;
            energySlider.value = energy.currentEnergy;
        }

        if (energyValueText != null)
            energyValueText.text = $"{energy.currentEnergy:0}/{energy.maxEnergy:0}";
    }

    private void UpdateMoneyUI(FinancialState finance)
    {
        if (moneyText != null)
            moneyText.text = $"${finance.currentMoney:0.00}";
    }

    private void UpdateSocialCreditUI(Dictionary<string, float> credits)
    {
        if (socialCreditContainer == null || socialCreditEntryPrefab == null) return;

        foreach (var pair in credits)
        {
            if (!_socialTexts.TryGetValue(pair.Key, out var txt) || txt == null)
            {
                GameObject entry = Instantiate(socialCreditEntryPrefab, socialCreditContainer);
                txt = entry.GetComponent<Text>() ?? entry.AddComponent<Text>();
                _socialTexts[pair.Key] = txt;
            }
            txt.text = $"{pair.Key}: {pair.Value:0}";
        }
    }
    #endregion
}
