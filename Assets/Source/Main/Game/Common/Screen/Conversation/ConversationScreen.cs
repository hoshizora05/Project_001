using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using CharacterSystem;

/// <summary>
/// Example conversation screen supporting conversationId, typewriter text, 
/// touch-based input to advance, skip, and a log feature.
/// </summary>
public class ConversationScreen : BaseScreen
{
    [Header("UI References - Background & Character")]
    [SerializeField] private Image backgroundImage;         // Your background UI Image
    [SerializeField] private Image characterPortrait;       // Center character image
    [SerializeField] private Sprite defaultPortrait;

    [Header("Dialogue UI")]
    [SerializeField] private TextMeshProUGUI dialogueText;  // The main text window
    [SerializeField] private GameObject textWindow;         // Container for the text
    [SerializeField] private TextMeshProUGUI speakerNameText;

    [Header("Choice Section")]
    [SerializeField] private GameObject choicesPanel;
    [SerializeField] private Button[] choiceButtons;        // Typically 3~4

    [Header("Log Feature")]
    [SerializeField] private GameObject logPanel;           // A panel to display the log
    [SerializeField] private TextMeshProUGUI logText;       // Text element in the log panel

    [Header("Typing Settings")]
    [Tooltip("Interval (seconds) between characters when typing.")]
    [SerializeField] private float typingSpeed = 0.03f;
    [Tooltip("If the player taps skip, instantly show the full text.")]
    [SerializeField] private bool allowSkipToEndOfLine = true;

    [Header("Systems")]
    [SerializeField] private InteractionSystem interactionSystem;
    [SerializeField] private TouchManager touchManager;

    private float typingSpeedMultiplier = 1.0f;
    private float textWindowAlpha = 1.0f;

    // References to the player & NPC
    private ICharacter player;
    private ICharacter npc;

    // Currently active DialogueContext
    private DialogueContext currentContext;
    // Current dialogue options from the DialogueSystem
    private DialogueOptions currentOptions;

    // Are we actively conversing?
    private bool conversationActive = false;

    // Typewriter-related
    private Coroutine typewriterCoroutine;
    private bool isTextFullyDisplayed = false;  // Has the current line finished typing?

    // We store the lines shown so far, for the log
    private List<string> conversationLog = new List<string>();

    // ------------------------------------------------------------------------
    // Initialization

    private void Awake()
    {
        // Optional: automatically register for touch events
        if (touchManager != null)
        {
            touchManager.OnTouchEnded += OnTouchEnded;
        }

        // Hide log by default
        if (logPanel != null)
        {
            logPanel.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        // Unregister event
        if (touchManager != null)
        {
            touchManager.OnTouchEnded -= OnTouchEnded;
        }
    }

    // ------------------------------------------------------------------------
    // Public API

    /// <summary>
    /// Start a conversation (topic-based or fallback).
    /// This calls the default DialogueSystem.InitiateDialogue (no conversationId).
    /// </summary>
    public void StartConversation(ICharacter playerCharacter, ICharacter npcCharacter)
    {
        if (interactionSystem == null)
        {
            Debug.LogError("ConversationScreen: No InteractionSystem assigned!");
            return;
        }

        // Store references
        player = playerCharacter;
        npc = npcCharacter;

        // Initiate the standard conversation
        interactionSystem.DialogueSystem.InitiateDialogue(player, npc);

        // Build context 
        currentContext = new DialogueContext
        {
            Location = npc.CurrentLocation,
            TimeOfDay = TimeOfDay.Afternoon,
            PreviousTopic = DialogueTopic.Greeting,
            NpcCurrentEmotion = npc.CurrentEmotionalState
        };

        conversationActive = true;
        gameObject.SetActive(true);

        ShowNextDialogue();
    }

    /// <summary>
    /// Start a conversation by conversationId. 
    /// This calls DialogueSystem.InitiateDialogue with the specified ID.
    /// </summary>
    public void StartConversation(ICharacter playerCharacter, ICharacter npcCharacter, string conversationId)
    {
        if (interactionSystem == null)
        {
            Debug.LogError("ConversationScreen: No InteractionSystem assigned!");
            return;
        }

        // Store references
        player = playerCharacter;
        npc = npcCharacter;

        // Initiate the conversation by ID
        interactionSystem.DialogueSystem.InitiateDialogue(player, npc, conversationId);

        // Build a context
        currentContext = new DialogueContext
        {
            Location = npc.CurrentLocation,
            TimeOfDay = TimeOfDay.Afternoon,
            PreviousTopic = DialogueTopic.Special, // Or anything
            NpcCurrentEmotion = npc.CurrentEmotionalState
        };

        conversationActive = true;
        gameObject.SetActive(true);

        ShowNextDialogue();
    }

    /// <summary>
    /// Ends the conversation, hides UI, etc.
    /// </summary>
    public void EndConversation()
    {
        conversationActive = false;
        interactionSystem.DialogueSystem.EndDialogue(player, npc);
        gameObject.SetActive(false);
    }

    // ------------------------------------------------------------------------
    // Internal Logic

    /// <summary>
    /// Fetches the next dialogue options from the DialogueSystem, updates the UI.
    /// </summary>
    private void ShowNextDialogue()
    {
        // Request new dialogue options from DialogueSystem
        currentOptions = interactionSystem.DialogueSystem.GetDialogueOptions(player, npc, currentContext);
        if (currentOptions == null)
        {
            // End if no options
            EndConversation();
            return;
        }

        // Update the UI with the new line
        UpdateConversationUI(currentOptions);
    }

    /// <summary>
    /// Updates the entire conversation screen with the new NPC line & choices.
    /// </summary>
    private void UpdateConversationUI(DialogueOptions options)
    {
        if (!conversationActive) return;

        // 1) Stop any existing typewriter
        if (typewriterCoroutine != null)
        {
            StopCoroutine(typewriterCoroutine);
        }

        // 2) Start typing the NPC's line
        if (dialogueText != null)
        {
            dialogueText.text = "";
            isTextFullyDisplayed = false;

            // Typewriter
            typewriterCoroutine = StartCoroutine(TypeTextCoroutine(options.NpcDialogueText));
        }

        // 3) Update speaker name (for demonstration, we show NPC name)
        if (speakerNameText != null)
        {
            speakerNameText.text = npc.Name;
        }

        // 4) Set portrait based on emotion, or default
        if (characterPortrait != null)
        {
            // If you want to set different sprites by emotion, do it here
            // For now, using default
            characterPortrait.sprite = defaultPortrait;
        }

        // 5) Update choice buttons
        UpdateChoiceButtons(options.Choices);

        // 6) Optionally log this line
        if (!string.IsNullOrEmpty(options.NpcDialogueText))
        {
            conversationLog.Add($"{npc.Name}: {options.NpcDialogueText}");
        }
    }

    /// <summary>
    /// Sets up the choice buttons. If there are no choices, the panel is hidden.
    /// </summary>
    private void UpdateChoiceButtons(List<DialogueChoice> choices)
    {
        if (choicesPanel != null)
        {
            choicesPanel.SetActive(choices != null && choices.Count > 0);
        }

        if (choiceButtons == null) return;

        // Hide all buttons
        foreach (var btn in choiceButtons)
        {
            btn.gameObject.SetActive(false);
        }

        if (choices == null) return;

        for (int i = 0; i < choices.Count && i < choiceButtons.Length; i++)
        {
            var button = choiceButtons[i];
            var choiceData = choices[i];

            button.gameObject.SetActive(true);

            // Set text
            TextMeshProUGUI btnText = button.GetComponentInChildren<TextMeshProUGUI>();
            if (btnText != null)
            {
                btnText.text = choiceData.ChoiceText;
            }

            // Remove old listeners
            button.onClick.RemoveAllListeners();
            // Add new
            button.onClick.AddListener(() =>
            {
                OnChoiceSelected(choiceData);
            });
        }
    }

    /// <summary>
    /// Called when a choice is selected by the player.
    /// We pass it to the DialogueSystem, then fetch the next line.
    /// </summary>
    private void OnChoiceSelected(DialogueChoice choice)
    {
        // Immediately log the player's choice
        conversationLog.Add($"<Player>: {choice.ChoiceText}");

        // Process via DialogueSystem
        interactionSystem.DialogueSystem.ProcessDialogueChoice(choice, player, npc);

        // Update context
        currentContext.PreviousTopic = choice.Topic;
        currentContext.NpcCurrentEmotion = npc.CurrentEmotionalState;

        // Get next
        ShowNextDialogue();
    }

    // ------------------------------------------------------------------------
    // Typewriter Coroutine

    /// <summary>
    /// Typewriter effect for the given text, one character at a time.
    /// If the user taps (TouchEnded) while typing, we can skip to end (if enabled).
    /// </summary>
    private IEnumerator TypeTextCoroutine(string fullText)
    {
        dialogueText.text = "";

        float adjustedSpeed = typingSpeed;
        if (typingSpeedMultiplier > 0f)
        {
            adjustedSpeed = typingSpeed * (1.01f - typingSpeedMultiplier); // 0.0=遅い, 1.0=速い
            adjustedSpeed = Mathf.Clamp(adjustedSpeed, 0.001f, 1f); // 安全範囲
        }

        for (int i = 0; i < fullText.Length; i++)
        {
            dialogueText.text += fullText[i];
            yield return new WaitForSeconds(adjustedSpeed);

            if (isTextFullyDisplayed) // If skip was triggered
            {
                break;
            }
        }

        // If we were interrupted (skip), show entire text
        if (!isTextFullyDisplayed)
        {
            dialogueText.text = fullText;
        }

        isTextFullyDisplayed = true;
        typewriterCoroutine = null;
    }

    // ------------------------------------------------------------------------
    // Touch / Input Handling

    /// <summary>
    /// Called by TouchManager when a tap ends. 
    /// We'll interpret that as "advance" or "skip" input.
    /// </summary>
    private void OnTouchEnded(TouchInfo touchInfo)
    {
        // If the typewriter is still going, skip to the end of this line
        if (!isTextFullyDisplayed && allowSkipToEndOfLine)
        {
            isTextFullyDisplayed = true;
            return;
        }

        // Otherwise, if the line is fully displayed and there are no visible choices,
        // we can automatically proceed to the next. Or do nothing if choices exist
        if (currentOptions != null && (currentOptions.Choices == null || currentOptions.Choices.Count == 0))
        {
            ShowNextDialogue();
        }
        else
        {
            // If we do have choices, the user must pick one 
            // (unless you want a "no choices" fallback).
        }
    }

    // ------------------------------------------------------------------------
    // Skip All (Optional) 
    // E.g. a button to skip the entire conversation quickly.

    /// <summary>
    /// Instantly reveals the current line. If there's no next line (i.e., no choices),
    /// we move on until we find lines or the conversation ends.
    /// This might let the user skip the entire conversation quickly.
    /// </summary>
    public void OnSkipAllClicked()
    {
        // If we are mid-type, force it to show
        if (!isTextFullyDisplayed)
        {
            isTextFullyDisplayed = true;
        }
        else
        {
            // If the line is done, and no choices remain, skip to next line
            // in a loop. This depends on your design logic 
            // (maybe you show everything automatically?).
            // For simplicity, let's just do ShowNextDialogue once:
            ShowNextDialogue();
        }
    }

    // ------------------------------------------------------------------------
    // Log Feature

    /// <summary>
    /// Toggles the log panel on/off.
    /// </summary>
    public void ToggleLog()
    {
        if (logPanel == null || logText == null) return;

        bool isActive = logPanel.activeSelf;
        logPanel.SetActive(!isActive);

        // If showing now, refresh the text
        if (!isActive)
        {
            // Build a single string from the log list
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            foreach (var line in conversationLog)
            {
                sb.AppendLine(line);
            }
            logText.text = sb.ToString();
        }
    }
    /// <summary>
    /// タイピング速度倍率を変更（0〜1）
    /// </summary>
    public void SetTypingSpeedMultiplier(float value)
    {
        typingSpeedMultiplier = Mathf.Clamp01(value);
    }

    /// <summary>
    /// テキストウィンドウの透過度を変更（0〜1）
    /// </summary>
    public void SetTextWindowAlpha(float value)
    {
        textWindowAlpha = Mathf.Clamp01(value);

        if (textWindow != null)
        {
            var image = textWindow.GetComponent<Image>();
            if (image != null)
            {
                Color c = image.color;
                c.a = textWindowAlpha;
                image.color = c;
            }

            var cg = textWindow.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                cg.alpha = textWindowAlpha;
            }
        }
    }
}
