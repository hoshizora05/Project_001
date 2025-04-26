using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using CharacterSystem;
using InformationManagementUI;

/// <summary>
/// Displays character relationship information on the status screen
/// using the Relationship Network System.
/// </summary>
public class RelationshipStatusUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform relationshipContentParent;
    [SerializeField] private GameObject relationshipEntryPrefab;
    [SerializeField] private TMP_Dropdown sortDropdown;
    [SerializeField] private TMP_Dropdown filterDropdown;
    [SerializeField] private Button refreshButton;

    [Header("Settings")]
    [SerializeField] private string playerID = "player_1";
    [SerializeField] private int maxDisplayCount = 10;
    [SerializeField] private Color positiveColor = new Color(0.2f, 0.8f, 0.2f);
    [SerializeField] private Color neutralColor = new Color(0.7f, 0.7f, 0.7f);
    [SerializeField] private Color negativeColor = new Color(0.8f, 0.2f, 0.2f);

    // Reference to the Relationship Network API
    private RelationshipNetworkAPI networkAPI;

    // Cached relationship data
    private List<RelationshipData> relationships = new List<RelationshipData>();
    private List<EntityData> characters = new List<EntityData>();

    private enum SortOption
    {
        StrengthDescending,
        StrengthAscending,
        TrustDescending,
        TrustAscending,
        FamiliarityDescending,
        FamiliarityAscending,
        Alphabetical
    }

    private SortOption currentSortOption = SortOption.StrengthDescending;
    private string currentFilterOption = "All";

    private void Awake()
    {
        // Get reference to the relationship network API
        networkAPI = RelationshipNetworkAPI.Instance;

        // Setup UI controls
        if (sortDropdown != null)
        {
            sortDropdown.ClearOptions();
            sortDropdown.AddOptions(new List<string> {
                "Strength ↓", "Strength ↑",
                "Trust ↓", "Trust ↑",
                "Familiarity ↓", "Familiarity ↑",
                "Name A-Z"
            });
            sortDropdown.onValueChanged.AddListener(OnSortOptionChanged);
        }

        if (filterDropdown != null)
        {
            filterDropdown.ClearOptions();
            List<string> filterOptions = new List<string> { "All", "Friend", "Family", "Romantic", "Rival", "Enemy", "Acquaintance", "Business" };
            filterDropdown.AddOptions(filterOptions);
            filterDropdown.onValueChanged.AddListener(OnFilterOptionChanged);
        }

        if (refreshButton != null)
        {
            refreshButton.onClick.AddListener(RefreshRelationships);
        }
    }

    private void OnEnable()
    {
        // Subscribe to relationship events
        if (networkAPI != null)
        {
            // Register for relationship change events if the system supports it
            var eventHandler = GetComponent<RelationshipNetworkUIEvents>();
            if (eventHandler == null)
            {
                eventHandler = gameObject.AddComponent<RelationshipNetworkUIEvents>();
            }

            eventHandler.onRelationshipChanged.AddListener(OnRelationshipChanged);
        }

        // Load relationships when panel becomes visible
        RefreshRelationships();
    }

    private void OnDisable()
    {
        // Unsubscribe from events when panel is hidden
        var eventHandler = GetComponent<RelationshipNetworkUIEvents>();
        if (eventHandler != null)
        {
            eventHandler.onRelationshipChanged.RemoveListener(OnRelationshipChanged);
        }
    }

    /// <summary>
    /// Refreshes the relationship display with latest data
    /// </summary>
    public void RefreshRelationships()
    {
        if (networkAPI == null)
        {
            Debug.LogError("RelationshipNetworkAPI reference is null. Make sure the system is initialized.");
            return;
        }

        // Clear existing list
        ClearRelationshipEntries();

        // Get all relationships for the player
        LoadPlayerRelationships();

        // Apply current sorting and filtering
        ApplySortingAndFiltering();

        // Display relationships
        PopulateRelationshipEntries();
    }

    private void ClearRelationshipEntries()
    {
        if (relationshipContentParent == null)
            return;

        // Remove all existing entries
        foreach (Transform child in relationshipContentParent)
        {
            Destroy(child.gameObject);
        }
    }

    private void LoadPlayerRelationships()
    {
        // Clear existing data
        relationships.Clear();
        characters.Clear();

        // Get all entities from the relationship network API
        var allEntities = networkAPI.GetAllEntities();
        if (allEntities == null || allEntities.Count == 0)
        {
            Debug.LogWarning("No entities found in the relationship network.");
            return;
        }

        // Check if player exists
        if (!allEntities.Any(c => c.id == playerID))
        {
            Debug.LogWarning($"Player character with ID {playerID} not found in the relationship network.");
            return;
        }

        // Get relationships for player
        foreach (var entity in allEntities)
        {
            // Skip the player character itself
            if (entity.id == playerID)
                continue;

            // Get relationship status from player to this character
            RelationshipData relationship = networkAPI.GetRelationshipStatus(playerID, entity.id);

            if (relationship != null)
            {
                relationships.Add(relationship);
                characters.Add(entity);
            }
        }
    }

    private void ApplySortingAndFiltering()
    {
        // First apply filters
        if (currentFilterOption != "All")
        {
            // Create temporary lists
            var filteredRelationships = new List<RelationshipData>();
            var filteredCharacters = new List<EntityData>();

            for (int i = 0; i < relationships.Count; i++)
            {
                if (relationships[i].type == currentFilterOption)
                {
                    filteredRelationships.Add(relationships[i]);
                    filteredCharacters.Add(characters[i]);
                }
            }

            // Replace with filtered lists
            relationships = filteredRelationships;
            characters = filteredCharacters;
        }

        // Apply sorting
        List<int> indices = Enumerable.Range(0, relationships.Count).ToList();

        // Sort indices based on sort option
        switch (currentSortOption)
        {
            case SortOption.StrengthDescending:
                indices.Sort((a, b) => relationships[b].strength.CompareTo(relationships[a].strength));
                break;
            case SortOption.StrengthAscending:
                indices.Sort((a, b) => relationships[a].strength.CompareTo(relationships[b].strength));
                break;
            case SortOption.TrustDescending:
                indices.Sort((a, b) => relationships[b].trust.CompareTo(relationships[a].trust));
                break;
            case SortOption.TrustAscending:
                indices.Sort((a, b) => relationships[a].trust.CompareTo(relationships[b].trust));
                break;
            case SortOption.FamiliarityDescending:
                indices.Sort((a, b) => relationships[b].familiarity.CompareTo(relationships[a].familiarity));
                break;
            case SortOption.FamiliarityAscending:
                indices.Sort((a, b) => relationships[a].familiarity.CompareTo(relationships[b].familiarity));
                break;
            case SortOption.Alphabetical:
                indices.Sort((a, b) => characters[a].id.CompareTo(characters[b].id));
                break;
        }

        // Use sorted indices to reorder our lists
        relationships = indices.Select(i => relationships[i]).ToList();
        characters = indices.Select(i => characters[i]).ToList();

        // Limit to max display count if needed
        if (relationships.Count > maxDisplayCount)
        {
            relationships = relationships.Take(maxDisplayCount).ToList();
            characters = characters.Take(maxDisplayCount).ToList();
        }
    }

    private void PopulateRelationshipEntries()
    {
        if (relationshipEntryPrefab == null || relationshipContentParent == null)
        {
            Debug.LogError("Relationship entry prefab or content parent not assigned.");
            return;
        }

        // Create an entry for each relationship
        for (int i = 0; i < relationships.Count; i++)
        {
            var relationship = relationships[i];
            var character = characters[i];

            // Instantiate the entry prefab
            GameObject entryObject = Instantiate(relationshipEntryPrefab, relationshipContentParent);
            RelationshipEntryUI entryUI = entryObject.GetComponent<RelationshipEntryUI>();

            if (entryUI != null)
            {
                // Populate entry data
                entryUI.SetData(character, relationship, GetRelationshipColor(relationship.strength));
            }
            else
            {
                // Manual setup if RelationshipEntryUI component doesn't exist
                SetupEntryManually(entryObject, character, relationship);
            }
        }
    }

    private void SetupEntryManually(GameObject entryObject, EntityData character, RelationshipData relationship)
    {
        // Find UI elements by name if the custom component doesn't exist
        Transform nameText = entryObject.transform.Find("NameText");
        Transform relationshipText = entryObject.transform.Find("RelationshipText");
        Transform strengthSlider = entryObject.transform.Find("StrengthSlider");
        Transform trustSlider = entryObject.transform.Find("TrustSlider");
        Transform familiaritySlider = entryObject.transform.Find("FamiliaritySlider");
        Transform portraitImage = entryObject.transform.Find("PortraitImage");

        // Set character name
        if (nameText != null && nameText.TryGetComponent<TMP_Text>(out var nameTMP))
        {
            nameTMP.text = character.id; // Using ID as name since EntityData doesn't have a name property
        }

        // Set relationship type
        if (relationshipText != null && relationshipText.TryGetComponent<TMP_Text>(out var relationshipTMP))
        {
            relationshipTMP.text = relationship.type;
            relationshipTMP.color = GetRelationshipColor(relationship.strength);
        }

        // Set strength slider
        if (strengthSlider != null && strengthSlider.TryGetComponent<Slider>(out var strengthSliderComponent))
        {
            // Convert from -100 to 100 range to 0 to 1 range
            float normalizedStrength = (relationship.strength + 100f) / 200f;
            strengthSliderComponent.value = normalizedStrength;

            // Update color if there's a fill area
            Transform fill = strengthSlider.Find("Fill Area/Fill");
            if (fill != null && fill.TryGetComponent<Image>(out var fillImage))
            {
                fillImage.color = GetRelationshipColor(relationship.strength);
            }
        }

        // Set trust slider
        if (trustSlider != null && trustSlider.TryGetComponent<Slider>(out var trustSliderComponent))
        {
            trustSliderComponent.value = relationship.trust / 100f;
        }

        // Set familiarity slider
        if (familiaritySlider != null && familiaritySlider.TryGetComponent<Slider>(out var familiaritySliderComponent))
        {
            familiaritySliderComponent.value = relationship.familiarity / 100f;
        }

        // We don't have a portrait system in EntityData, so we'll skip portrait setting
        // If you need portraits, you would need to implement your own portrait management system
    }

    private Color GetRelationshipColor(float strength)
    {
        // Determine color based on relationship strength
        if (strength > 20f)
            return positiveColor;
        else if (strength < -20f)
            return negativeColor;
        else
            return neutralColor;
    }

    private void OnRelationshipChanged(string sourceId, string targetId, float strength)
    {
        // Check if this affects the player
        if (sourceId == playerID || targetId == playerID)
        {
            // Refresh the relationship display
            RefreshRelationships();
        }
    }

    private void OnSortOptionChanged(int optionIndex)
    {
        currentSortOption = (SortOption)optionIndex;
        RefreshRelationships();
    }

    private void OnFilterOptionChanged(int optionIndex)
    {
        currentFilterOption = filterDropdown.options[optionIndex].text;
        RefreshRelationships();
    }
}

/// <summary>
/// Component for individual relationship entry UIs.
/// Attach this to your relationship entry prefab.
/// </summary>
public class RelationshipEntryUI : MonoBehaviour
{
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text relationshipTypeText;
    [SerializeField] private Slider strengthSlider;
    [SerializeField] private Slider trustSlider;
    [SerializeField] private Slider familiaritySlider;
    [SerializeField] private Image portraitImage;
    [SerializeField] private Button detailsButton;

    [SerializeField] private TMP_Text strengthValueText;
    [SerializeField] private TMP_Text trustValueText;
    [SerializeField] private TMP_Text familiarityValueText;

    private string characterId;

    private void Awake()
    {
        if (detailsButton != null)
        {
            detailsButton.onClick.AddListener(OnDetailsButtonClicked);
        }
    }

    private void OnDestroy()
    {
        if (detailsButton != null)
        {
            detailsButton.onClick.RemoveListener(OnDetailsButtonClicked);
        }
    }

    public void SetData(EntityData character, RelationshipData relationship, Color relationshipColor)
    {
        this.characterId = character.id;

        // Set name
        if (nameText != null)
        {
            nameText.text = character.id; // Using ID as name
        }

        // Set relationship type
        if (relationshipTypeText != null)
        {
            relationshipTypeText.text = relationship.type;
            relationshipTypeText.color = relationshipColor;
        }

        // Set strength
        if (strengthSlider != null)
        {
            // Convert from -100 to 100 range to 0 to 1 range
            float normalizedStrength = (relationship.strength + 100f) / 200f;
            strengthSlider.value = normalizedStrength;

            // Update color if there's a fill image
            Image fillImage = strengthSlider.fillRect?.GetComponent<Image>();
            if (fillImage != null)
            {
                fillImage.color = relationshipColor;
            }

            // Update text value if available
            if (strengthValueText != null)
            {
                strengthValueText.text = relationship.strength.ToString("F0");
                strengthValueText.color = relationshipColor;
            }
        }

        // Set trust
        if (trustSlider != null)
        {
            trustSlider.value = relationship.trust / 100f;

            if (trustValueText != null)
            {
                trustValueText.text = relationship.trust.ToString("F0");
            }
        }

        // Set familiarity
        if (familiaritySlider != null)
        {
            familiaritySlider.value = relationship.familiarity / 100f;

            if (familiarityValueText != null)
            {
                familiarityValueText.text = relationship.familiarity.ToString("F0");
            }
        }

        // Skip portrait setting as EntityData doesn't have portrait info
    }

    public void SetData(CharacterNode characterNode, RelationshipData relationship, Color relationshipColor)
    {
        this.characterId = characterNode.CharacterID;

        // Set name
        if (nameText != null)
        {
            nameText.text = characterNode.Name;
        }

        // Set relationship type
        if (relationshipTypeText != null)
        {
            relationshipTypeText.text = relationship.type;
            relationshipTypeText.color = relationshipColor;
        }

        // Set strength
        if (strengthSlider != null)
        {
            // Convert from -100 to 100 range to 0 to 1 range
            float normalizedStrength = (relationship.strength + 100f) / 200f;
            strengthSlider.value = normalizedStrength;

            // Update color if there's a fill image
            Image fillImage = strengthSlider.fillRect?.GetComponent<Image>();
            if (fillImage != null)
            {
                fillImage.color = relationshipColor;
            }

            // Update text value if available
            if (strengthValueText != null)
            {
                strengthValueText.text = relationship.strength.ToString("F0");
                strengthValueText.color = relationshipColor;
            }
        }

        // Set trust
        if (trustSlider != null)
        {
            trustSlider.value = relationship.trust / 100f;

            if (trustValueText != null)
            {
                trustValueText.text = relationship.trust.ToString("F0");
            }
        }

        // Set familiarity
        if (familiaritySlider != null)
        {
            familiaritySlider.value = relationship.familiarity / 100f;

            if (familiarityValueText != null)
            {
                familiarityValueText.text = relationship.familiarity.ToString("F0");
            }
        }

        // Set portrait if available
        if (portraitImage != null && characterNode.Portrait != null)
        {
            portraitImage.sprite = characterNode.Portrait;
        }
    }

    private void OnDetailsButtonClicked()
    {
        // Open detailed relationship view for this character
        // This could trigger a UI event to show a more detailed panel
        // or navigate to a character detail screen

        // Example of using an information management system to show detailed relationship
        if (string.IsNullOrEmpty(characterId))
            return;

        // Check if the Information Management UI system is available
        var infoUISystem = FindFirstObjectByType<InformationManagementUISystem>();
        if (infoUISystem != null)
        {
            // Show relationship diagram centered on this character
            infoUISystem.ShowRelationshipsForCharacter(characterId);
        }
        else
        {
            Debug.Log($"Showing detailed relationship view for character: {characterId}");
            // Implement your own detail view logic here
        }
    }
}