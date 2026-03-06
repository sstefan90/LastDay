using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using LastDay.Core;
using LastDay.Dialogue;

namespace LastDay.UI
{
    /// <summary>
    /// Interview-style dialogue: permanent input bar for Robert, floating text above Martha for her answers.
    /// Same clickable interactables and Martha; different display from the standard DialogueUI.
    /// </summary>
    public class InterviewDialogueUI : MonoBehaviour, IDialogueUI
    {
        [Header("Permanent input bar (always visible)")]
        [SerializeField] private GameObject inputBarPanel;
        [SerializeField] private TMP_Text playerLabelText;
        [SerializeField] private TMP_InputField inputField;
        [SerializeField] private Button sendButton;

        [Header("Floating dialogue above Martha")]
        [SerializeField] private FloatingDialogueDisplay floatingDisplay;

        [Header("Monologue (for David/phone, etc.)")]
        [SerializeField] private GameObject monologuePanel;
        [SerializeField] private TMP_Text monologueText;

        [Header("Thinking")]
        [SerializeField] private GameObject thinkingIndicator;

        private string currentObjectId;
        private string currentMemoryId;
        private string currentCharacter = "martha";

        void Awake()
        {
            DialogueSession.Current = this;
            if (playerLabelText != null)
                playerLabelText.text = "Robert says:";
            if (inputBarPanel != null)
                inputBarPanel.SetActive(true);
            if (monologuePanel != null)
                monologuePanel.SetActive(false);
            if (thinkingIndicator != null)
                thinkingIndicator.SetActive(false);
        }

        void OnDestroy()
        {
            if (ReferenceEquals(DialogueSession.Current, this))
                DialogueSession.Current = null;
        }

        void Start()
        {
            if (sendButton != null)
                sendButton.onClick.AddListener(OnSendClicked);
            if (inputField != null)
                inputField.onSubmit.AddListener(OnInputSubmit);
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
                Close();
        }

        public void OpenForNPC(string npcId, string npcName)
        {
            currentObjectId = null;
            currentMemoryId = null;
            currentCharacter = npcId;

            if (LocalLLMManager.Instance != null)
                LocalLLMManager.Instance.SetCharacter(npcId);
            if (GameStateMachine.Instance != null)
                GameStateMachine.Instance.ChangeState(GameState.InDialogue);

            string greeting = npcId == "david"
                ? CharacterPrompts.GetObjectOpeningLine("phone", "david")
                : "Is everything alright? You have that look.";
            ShowResponse(greeting);
        }

        public void OpenForObject(string objectId, string memoryId, string displayName)
        {
            currentObjectId = objectId;
            currentMemoryId = memoryId;
            currentCharacter = "martha";

            if (LocalLLMManager.Instance != null)
                LocalLLMManager.Instance.SetCharacter("martha");
            if (GameStateMachine.Instance != null)
                GameStateMachine.Instance.ChangeState(GameState.InDialogue);

            int activeQuestion = EventManager.Instance != null ? EventManager.Instance.activeSecurityQuestion : 0;

            if (memoryId == "guitar" && activeQuestion == 3
                && EventManager.Instance != null
                && !EventManager.Instance.marthaGuitarBreakdown)
            {
                ShowMonologue("There's a massive crack down the back of the neck. It's broken.");
            }

            string greeting = CharacterPrompts.GetObjectOpeningLine(memoryId, "martha", activeQuestion);
            ShowResponse(greeting);
        }

        public void OpenForPhone()
        {
            currentCharacter = "david";
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

        public void ShowMonologue(string text)
        {
            if (monologuePanel != null && monologueText != null)
            {
                monologuePanel.SetActive(true);
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
            if (floatingDisplay != null)
                floatingDisplay.Hide();
            if (monologuePanel != null)
                monologuePanel.SetActive(false);
            if (LocalLLMManager.Instance != null)
                LocalLLMManager.Instance.ClearHistory();
            if (GameStateMachine.Instance != null)
            {
                if (GameStateMachine.Instance.CurrentState == GameState.InDialogue
                    || GameStateMachine.Instance.CurrentState == GameState.PhoneCall)
                    GameStateMachine.Instance.ChangeState(GameState.Playing);
            }
        }

        private void ShowResponse(string text)
        {
            if (currentCharacter == "martha" && floatingDisplay != null)
                floatingDisplay.Show(text, useTypewriter: true);
            else if (monologuePanel != null && monologueText != null)
            {
                monologuePanel.SetActive(true);
                monologueText.text = text;
                StartCoroutine(HideMonologueAfterDelay(5f));
            }
        }

        private void OnSendClicked() => SubmitInput();
        private void OnInputSubmit(string _) => SubmitInput();

        private async void SubmitInput()
        {
            if (inputField == null) return;
            string playerText = inputField.text.Trim();
            if (string.IsNullOrEmpty(playerText)) return;

            inputField.text = "";
            inputField.interactable = false;
            if (sendButton != null) sendButton.interactable = false;
            if (thinkingIndicator != null) thinkingIndicator.SetActive(true);

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
                    playerText, currentCharacter, memories);
            }
            else
                response = "...";

            if (thinkingIndicator != null) thinkingIndicator.SetActive(false);
            ShowResponse(response);

            inputField.interactable = true;
            if (sendButton != null) sendButton.interactable = true;
            StartCoroutine(RefocusInput());
            GameEvents.ReceiveDialogue(currentCharacter, response);
        }

        private IEnumerator RefocusInput()
        {
            yield return null;
            if (inputField != null) inputField.ActivateInputField();
        }

        private IEnumerator HideMonologueAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (monologuePanel != null) monologuePanel.SetActive(false);
        }
    }
}
