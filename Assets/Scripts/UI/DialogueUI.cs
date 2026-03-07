using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using LastDay.Core;
using LastDay.Dialogue;

namespace LastDay.UI
{
    public class DialogueUI : MonoBehaviour, IDialogueUI
    {
        public static DialogueUI Instance { get; private set; }

        [Header("Panel")]
        [SerializeField] private GameObject dialoguePanel;

        [Header("Character Display")]
        [SerializeField] private Image characterPortrait;
        [SerializeField] private TMP_Text characterNameText;
        [SerializeField] private TMP_Text dialogueText;

        [Header("Player Input")]
        [SerializeField] private TMP_InputField inputField;
        [SerializeField] private Button sendButton;
        [SerializeField] private Button closeButton;

        [Header("Thinking Indicator")]
        [SerializeField] private GameObject thinkingIndicator;

        [Header("Monologue")]
        [SerializeField] private GameObject monologuePanel;
        [SerializeField] private TMP_Text monologueText;

        [Header("Typewriter")]
        [SerializeField] private float typewriterSpeed = 0.03f;

        [Header("Character Portraits")]
        [SerializeField] private Sprite marthaPortrait;
        [SerializeField] private Sprite davidPortrait;

        private string currentObjectId;
        private string currentMemoryId;
        private string currentCharacter = "martha";
        private bool isTyping;
        private Coroutine typewriterCoroutine;

        void Awake()
        {
            Instance = this;
            DialogueSession.Current = this;

            if (dialoguePanel != null)
                dialoguePanel.SetActive(false);
            if (monologuePanel != null)
                monologuePanel.SetActive(false);
            if (thinkingIndicator != null)
                thinkingIndicator.SetActive(false);
        }

        void OnDestroy()
        {
            if (ReferenceEquals(DialogueSession.Current, this))
                DialogueSession.Current = null;
            if (ReferenceEquals(Instance, this))
                Instance = null;
        }

        void Start()
        {
            if (sendButton != null)
                sendButton.onClick.AddListener(OnSendClicked);

            if (closeButton != null)
                closeButton.onClick.AddListener(Close);

            if (inputField != null)
                inputField.onSubmit.AddListener(OnInputSubmit);
        }

        void Update()
        {
            if (dialoguePanel != null && dialoguePanel.activeSelf && Input.GetKeyDown(KeyCode.Escape))
                Close();
        }

        /// <summary>
        /// Open dialogue panel for a memory object interaction with Martha.
        /// </summary>
        public void OpenForObject(string objectId, string memoryId, string displayName)
        {
            currentObjectId = objectId;
            currentMemoryId = memoryId;
            currentCharacter = "martha";

            SetupPanel("Martha", marthaPortrait);

            if (LocalLLMManager.Instance != null)
                LocalLLMManager.Instance.SetCharacter("martha");

            if (GameStateMachine.Instance != null)
                GameStateMachine.Instance.ChangeState(GameState.InDialogue);

            int activeQuestion = EventManager.Instance != null ? EventManager.Instance.activeSecurityQuestion : 0;

            // Guitar monologue: show the crack as a visual clue when Q3 is the active mystery
            if (memoryId == "guitar" && activeQuestion == 3
                && EventManager.Instance != null
                && !EventManager.Instance.marthaGuitarBreakdown)
            {
                ShowMonologue("There's a massive crack down the back of the neck. It's broken.");
            }

            string greeting = CharacterPrompts.GetObjectOpeningLine(memoryId, "martha", activeQuestion);
            ShowResponse(greeting);
        }

        /// <summary>
        /// Open dialogue panel for directly talking to Martha (clicked on her).
        /// </summary>
        public void OpenForNPC(string npcId, string npcName)
        {
            currentObjectId = null;
            currentMemoryId = null;
            currentCharacter = npcId;

            Sprite portrait = npcId == "david" ? davidPortrait : marthaPortrait;
            SetupPanel(npcName, portrait);

            if (LocalLLMManager.Instance != null)
                LocalLLMManager.Instance.SetCharacter(npcId);

            if (GameStateMachine.Instance != null)
                GameStateMachine.Instance.ChangeState(GameState.InDialogue);

            // Direct NPC click — use a natural, character-specific greeting
            string greeting = npcId == "david"
                ? CharacterPrompts.GetObjectOpeningLine("phone", "david")
                : "Is everything alright? You have that look.";
            ShowResponse(greeting);
        }

        /// <summary>
        /// Open dialogue panel for a phone call with David.
        /// </summary>
        public void OpenForPhone()
        {
            currentCharacter = "david";

            SetupPanel("David (Phone)", davidPortrait);

            if (LocalLLMManager.Instance != null)
                LocalLLMManager.Instance.SetCharacter("david");

            if (GameStateMachine.Instance != null)
                GameStateMachine.Instance.ChangeState(GameState.PhoneCall);

            int activeQuestion = EventManager.Instance != null ? EventManager.Instance.activeSecurityQuestion : 0;
            string greeting = activeQuestion > 0
                ? CharacterPrompts.GetObjectOpeningLine("phone", "david", activeQuestion)
                : "Hey, old friend. Thought I'd give you a call today. How are you holding up?";
            ShowResponse(greeting);
        }

        /// <summary>
        /// Show a cinematic action description. Falls back to monologue panel on legacy UI.
        /// </summary>
        public void ShowAction(string text)
        {
            ShowMonologue($"[ {text} ]");
        }

        /// <summary>
        /// Show an internal monologue (no input field).
        /// </summary>
        public void ShowMonologue(string text)
        {
            if (monologuePanel != null)
            {
                monologuePanel.SetActive(true);
                if (monologueText != null)
                    monologueText.text = text;

                StartCoroutine(HideMonologueAfterDelay(3f));
            }
            else
            {
                Debug.Log($"[Monologue] {text}");
            }
        }

        public void Close()
        {
            if (dialoguePanel != null)
                dialoguePanel.SetActive(false);

            if (isTyping && typewriterCoroutine != null)
                StopCoroutine(typewriterCoroutine);

            isTyping = false;

            if (LocalLLMManager.Instance != null)
                LocalLLMManager.Instance.ClearHistory();

            if (GameStateMachine.Instance != null)
            {
                if (GameStateMachine.Instance.CurrentState == GameState.InDialogue
                    || GameStateMachine.Instance.CurrentState == GameState.PhoneCall)
                    GameStateMachine.Instance.ChangeState(GameState.Playing);
            }
        }

        private void SetupPanel(string characterName, Sprite portrait)
        {
            if (dialoguePanel != null)
                dialoguePanel.SetActive(true);

            if (characterNameText != null)
                characterNameText.text = characterName;

            if (characterPortrait != null)
            {
                if (portrait != null)
                {
                    characterPortrait.sprite = portrait;
                    characterPortrait.color = Color.white;
                }
                else
                {
                    characterPortrait.sprite = null;
                    characterPortrait.color = Color.clear;
                }
            }

            if (dialogueText != null)
                dialogueText.text = "";

            if (inputField != null)
            {
                inputField.text = "";
                inputField.interactable = true;
            }

            if (sendButton != null)
                sendButton.interactable = true;
        }

        private void OnSendClicked()
        {
            SubmitInput();
        }

        private void OnInputSubmit(string text)
        {
            SubmitInput();
        }

        private async void SubmitInput()
        {
            if (inputField == null) return;

            string playerText = inputField.text.Trim();
            if (string.IsNullOrEmpty(playerText)) return;

            inputField.text = "";
            inputField.interactable = false;
            if (sendButton != null)
                sendButton.interactable = false;

            if (thinkingIndicator != null)
                thinkingIndicator.SetActive(true);

            // Guitar breakdown detection — if Q3 is active and player mentions
            // the physical damage, set the breakdown flag before generating response
            if (EventManager.Instance != null
                && EventManager.Instance.activeSecurityQuestion == 3
                && !EventManager.Instance.marthaGuitarBreakdown
                && currentCharacter == "martha")
            {
                string lower = playerText.ToLower();
                if (lower.Contains("crack") || lower.Contains("smash") || lower.Contains("broken")
                    || lower.Contains("broke") || lower.Contains("shatter") || lower.Contains("damaged")
                    || lower.Contains("neck") || lower.Contains("why is it"))
                {
                    EventManager.Instance.marthaGuitarBreakdown = true;
                    GameEvents.MarthaBreakdownReady();
                }
            }

            string response;
            if (LocalLLMManager.Instance != null)
            {
                List<string> memories = EventManager.Instance != null
                    ? EventManager.Instance.triggeredMemories
                    : new List<string>();

                response = await LocalLLMManager.Instance.GenerateResponse(
                    playerText, currentCharacter, memories
                );
            }
            else
            {
                response = "...";
            }

            if (thinkingIndicator != null)
                thinkingIndicator.SetActive(false);

            ShowResponse(response);

            if (inputField != null)
            {
                inputField.interactable = true;
                StartCoroutine(RefocusInput());
            }

            if (sendButton != null)
                sendButton.interactable = true;

            GameEvents.ReceiveDialogue(currentCharacter, response);
        }

        private void ShowResponse(string text)
        {
            if (isTyping && typewriterCoroutine != null)
                StopCoroutine(typewriterCoroutine);

            typewriterCoroutine = StartCoroutine(TypewriterEffect(text));
        }

        private IEnumerator TypewriterEffect(string text)
        {
            isTyping = true;

            if (dialogueText != null)
            {
                dialogueText.text = text;
                dialogueText.maxVisibleCharacters = 0;

                for (int i = 0; i <= text.Length; i++)
                {
                    dialogueText.maxVisibleCharacters = i;
                    yield return new WaitForSeconds(typewriterSpeed);
                }
            }

            isTyping = false;
        }

        private IEnumerator RefocusInput()
        {
            yield return null;
            if (inputField != null)
                inputField.ActivateInputField();
        }

        private IEnumerator HideMonologueAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (monologuePanel != null)
                monologuePanel.SetActive(false);
        }
    }
}
