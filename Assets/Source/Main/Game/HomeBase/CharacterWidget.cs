using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CharacterSystem;
using AddressableManagementSystem;
using InformationManagementUI;
using LifeResourceSystem;

/// <summary>
/// CharacterWidget - Displays information about a character relationship
/// in the character information panel
/// </summary>
public class CharacterWidget : MonoBehaviour
{
    [SerializeField] private Image characterIcon;
    [SerializeField] private TextMeshProUGUI characterNameText;
    [SerializeField] private Slider relationshipSlider;
    [SerializeField] private TextMeshProUGUI recentMessageText;
    [SerializeField] private Button detailsButton;

    private string characterId;

    private void Awake()
    {
        // Set up button listener
        if (detailsButton != null)
        {
            detailsButton.onClick.AddListener(OnDetailsButtonClicked);
        }
    }

    public async void UpdateWidget(string charId, CharacterSystem.RelationshipData relationship)
    {
        this.characterId = charId;

        // Update name
        if (characterNameText != null)
        {
            // Get character name from character system
            CharacterManager.Character character = CharacterManager.Instance.GetCharacter(charId);
            if (character != null)
            {
                characterNameText.text = character.baseInfo.name;
            }
            else
            {
                characterNameText.text = charId; // Fallback to ID if name not available
            }
        }

        // Update icon
        if (characterIcon != null)
        {
            // Load character icon (assuming addressable assets)
            await AddressableResourceManager.Instance.LoadSpriteToImageAsync($"character_icon_{charId}", characterIcon);
        }

        // Update relationship slider
        if (relationshipSlider != null)
        {
            // Normalize relationship value to 0-1 range (assuming -100 to 100 range)
            float normalizedValue = (relationship.strength + 100) / 200f;
            relationshipSlider.value = normalizedValue;

            // Set color based on relationship strength
            Color sliderColor = Color.grey;
            if (relationship.strength > 75)
                sliderColor = Color.red; // Strong relationship (romantic)
            else if (relationship.strength > 50)
                sliderColor = new Color(1f, 0.64f, 0f); // Good relationship (friendship)
            else if (relationship.strength > 25)
                sliderColor = Color.yellow; // Decent relationship

            relationshipSlider.fillRect.GetComponent<Image>().color = sliderColor;
        }

        // Update recent message
        if (recentMessageText != null)
        {
            // Get recent message from messaging system (if available)
            InteractionSystem interactionSystem = ServiceLocator.Get<InteractionSystem>();
            if (interactionSystem != null)
            {
                // Get character message threads
                var messageSystem = interactionSystem.MessageSystem;
                if (messageSystem != null)
                {
                    CharacterManager.Character character = CharacterManager.Instance.GetCharacter(charId);
                    if (character != null)
                    {
                        // Adapt the character to ICharacter interface needed by the messaging system
                        // We're assuming there's an adapter class or method to convert Character to ICharacter
                        ICharacter characterInterface = new CharacterAdapter(character);
                        var threads = messageSystem.GetActiveThreads(characterInterface);
                        
                        if (threads != null && threads.Count > 0)
                        {
                            // Get the most recent thread
                            var mostRecentThread = threads.OrderByDescending(t => t.LastActivity).FirstOrDefault();
                            if (mostRecentThread != null && mostRecentThread.Messages.Count > 0)
                            {
                                // Get most recent message
                                var recentMessage = mostRecentThread.Messages.OrderByDescending(m => m.SendTime).FirstOrDefault();
                                string messageText = recentMessage?.Text ?? "No message text";
                                
                                // Truncate if too long
                                if (messageText.Length > 30)
                                {
                                    messageText = messageText.Substring(0, 27) + "...";
                                }
                                
                                recentMessageText.text = messageText;
                            }
                            else
                            {
                                recentMessageText.text = "No messages";
                            }
                        }
                        else
                        {
                            recentMessageText.text = "No message threads";
                        }
                    }
                    else
                    {
                        recentMessageText.text = "Character not found";
                    }
                }
                else
                {
                    recentMessageText.text = "Message system unavailable";
                }
            }
            else
            {
                recentMessageText.text = "...";
            }
        }
    }

    private void OnDetailsButtonClicked()
    {
        // Open detailed character relationship view
        Debug.Log($"Opening details for character: {characterId}");

        // Show character details in the information management UI
        InformationManagementUISystem uiSystem = InformationManagementUISystem.Instance;
        if (uiSystem != null)
        {
            uiSystem.ShowRelationshipsForCharacter(characterId);
        }
    }

    private void OnDestroy()
    {
        // Clean up listeners
        if (detailsButton != null)
        {
            detailsButton.onClick.RemoveListener(OnDetailsButtonClicked);
        }
    }
}