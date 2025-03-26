using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace CharacterSystem
{
    /// <summary>
    /// Component for visualizing character state in the Unity Editor
    /// </summary>
    public class CharacterStateVisualizer : MonoBehaviour
    {
        [SerializeField] private string characterId;
        [SerializeField] private Text nameText;
        [SerializeField] private Text dominantDesireText;
        [SerializeField] private Text moodText;
        [SerializeField] private Image moodColorImage;
        [SerializeField] private RectTransform desiresContainer;
        [SerializeField] private RectTransform emotionsContainer;
        [SerializeField] private Slider desireSliderPrefab;
        [SerializeField] private Slider emotionSliderPrefab;
        
        private CharacterManager.Character character;
        
        private void Start()
        {
            // Get character ID from this object if not specified
            if (string.IsNullOrEmpty(characterId))
            {
                var identifier = GetComponent<CharacterIdentifier>();
                if (identifier != null)
                {
                    characterId = identifier.characterId;
                }
            }
            
            // Get the character
            character = CharacterManager.Instance.GetCharacter(characterId);
            
            if (character == null)
            {
                Debug.LogError($"Character with ID {characterId} not found!");
                return;
            }
            
            // Set up the UI
            SetupUI();
        }
        
        private void Update()
        {
            if (character == null)
                return;
                
            // Update UI with current values
            UpdateUI();
        }
        
        /// <summary>
        /// Set up the UI elements
        /// </summary>
        private void SetupUI()
        {
            if (nameText != null)
            {
                nameText.text = character.baseInfo.name;
            }
            
            // Create sliders for each desire
            if (desiresContainer != null && desireSliderPrefab != null)
            {
                foreach (var desire in character.desires.desireTypes)
                {
                    var sliderObj = Instantiate(desireSliderPrefab, desiresContainer);
                    var sliderLabel = sliderObj.GetComponentInChildren<Text>();
                    
                    if (sliderLabel != null)
                    {
                        sliderLabel.text = desire.type;
                    }
                    
                    // Tag the slider with the desire type
                    sliderObj.gameObject.name = desire.type;
                }
            }
            
            // Create sliders for each emotion
            if (emotionsContainer != null && emotionSliderPrefab != null)
            {
                foreach (var emotion in character.mentalState.emotionalStates)
                {
                    var sliderObj = Instantiate(emotionSliderPrefab, emotionsContainer);
                    var sliderLabel = sliderObj.GetComponentInChildren<Text>();
                    
                    if (sliderLabel != null)
                    {
                        sliderLabel.text = emotion.type;
                    }
                    
                    // Tag the slider with the emotion type
                    sliderObj.gameObject.name = emotion.type;
                }
            }
        }
        
        /// <summary>
        /// Update the UI with current values
        /// </summary>
        private void UpdateUI()
        {
            // Update dominant desire text
            if (dominantDesireText != null)
            {
                dominantDesireText.text = $"Dominant Desire: {character.desires.dominantDesire}";
            }
            
            // Update mood text
            if (moodText != null)
            {
                moodText.text = $"Mood: {character.mentalState.currentMood}";
            }
            
            // Update mood color
            if (moodColorImage != null)
            {
                moodColorImage.color = GetMoodColor(character.mentalState.currentMood);
            }
            
            // Update desire sliders
            if (desiresContainer != null)
            {
                foreach (var desire in character.desires.desireTypes)
                {
                    var sliderObj = desiresContainer.Find(desire.type);
                    
                    if (sliderObj != null)
                    {
                        var slider = sliderObj.GetComponent<Slider>();
                        if (slider != null)
                        {
                            slider.value = desire.currentValue / 100f;
                        }
                    }
                }
            }
            
            // Update emotion sliders
            if (emotionsContainer != null)
            {
                foreach (var emotion in character.mentalState.emotionalStates)
                {
                    var sliderObj = emotionsContainer.Find(emotion.type);
                    
                    if (sliderObj != null)
                    {
                        var slider = sliderObj.GetComponent<Slider>();
                        if (slider != null)
                        {
                            // Convert from -100 to 100 range to 0 to 1 range
                            slider.value = (emotion.currentValue + 100f) / 200f;
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Get a color representing a mood
        /// </summary>
        private Color GetMoodColor(string mood)
        {
            switch (mood.ToLower())
            {
                case "happy":
                case "joyful":
                case "excited":
                    return new Color(1f, 0.8f, 0.2f); // Bright yellow
                    
                case "sad":
                case "depressed":
                case "melancholy":
                    return new Color(0.3f, 0.3f, 0.8f); // Blue
                    
                case "angry":
                case "furious":
                case "outraged":
                    return new Color(0.8f, 0.2f, 0.2f); // Red
                    
                case "afraid":
                case "anxious":
                case "nervous":
                    return new Color(0.8f, 0.6f, 0.8f); // Purple
                    
                case "neutral":
                case "calm":
                    return new Color(0.7f, 0.7f, 0.7f); // Gray
                    
                default:
                    return Color.white;
            }
        }
        
        /// <summary>
        /// Generate a debug report of the character's state
        /// </summary>
        public string GenerateDebugReport()
        {
            if (character == null)
                return "Character not found!";
                
            var sb = new StringBuilder();
            
            sb.AppendLine($"=== Character Report: {character.baseInfo.name} ===");
            sb.AppendLine();
            
            sb.AppendLine("--- Basic Info ---");
            sb.AppendLine($"ID: {character.baseInfo.characterId}");
            sb.AppendLine($"Age: {character.baseInfo.age}");
            sb.AppendLine($"Gender: {character.baseInfo.gender}");
            sb.AppendLine($"Occupation: {character.baseInfo.occupation}");
            sb.AppendLine($"Personality Type: {character.baseInfo.personalityType}");
            sb.AppendLine($"Relationship Status: {character.baseInfo.relationshipStatus}");
            sb.AppendLine();
            
            sb.AppendLine("--- Desires ---");
            sb.AppendLine($"Dominant Desire: {character.desires.dominantDesire}");
            foreach (var desire in character.desires.desireTypes)
            {
                string status = desire.currentValue < desire.threshold.low ? "UNSATISFIED" : 
                                (desire.currentValue > desire.threshold.high ? "SATISFIED" : "Neutral");
                                
                sb.AppendLine($"{desire.type}: {desire.currentValue:F1} [{status}] (Decay: {desire.decayRate:F1}/update)");
            }
            sb.AppendLine();
            
            sb.AppendLine("--- Mental State ---");
            sb.AppendLine($"Current Mood: {character.mentalState.currentMood}");
            sb.AppendLine($"Stress Level: {character.mentalState.stressLevel:F1}");
            sb.AppendLine("Emotions:");
            foreach (var emotion in character.mentalState.emotionalStates)
            {
                sb.AppendLine($"  {emotion.type}: {emotion.currentValue:F1} (Volatility: {emotion.volatility:F1}, Decay: {emotion.decayRate:F1}/update)");
            }
            sb.AppendLine("Active Mood Modifiers:");
            foreach (var modifier in character.mentalState.moodModifiers)
            {
                sb.AppendLine($"  {modifier.source} ({modifier.remainingTime} updates remaining)");
            }
            sb.AppendLine();
            
            sb.AppendLine("--- Complex Emotions ---");
            sb.AppendLine("Internal Conflicts:");
            foreach (var conflict in character.complexEmotions.internalConflicts)
            {
                sb.AppendLine($"  {conflict.type}: {conflict.values.firstValue:F1} vs {conflict.values.secondValue:F1} (Dominant: {conflict.dominantSide})");
            }
            sb.AppendLine("Personal Values:");
            foreach (var value in character.complexEmotions.personalValues)
            {
                sb.AppendLine($"  {value.valueType}: {value.importance:F1}");
            }
            sb.AppendLine("Emotional Memories:");
            foreach (var memory in character.complexEmotions.emotionalMemory)
            {
                sb.AppendLine($"  {memory.eventId} (Intensity: {memory.intensity:F1})");
            }
            
            return sb.ToString();
        }
    }
}