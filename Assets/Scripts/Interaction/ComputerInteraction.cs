using UnityEngine;
using UnityEngine.UI;
using TMPro;
using LastDay.Core;
using LastDay.UI;

namespace LastDay.Interaction
{
    /// <summary>
    /// The computer on Robert's desk. Displays sequential security questions that gate
    /// the MAID document. Each correct answer reveals the next mystery and advances the
    /// narrative state for Martha and David's LLM prompts.
    /// </summary>
    public class ComputerInteraction : InteractableObject2D
    {
        [Header("Computer UI Panel")]
        [SerializeField] private GameObject computerOverlay;
        [SerializeField] private GameObject computerPanel;
        [SerializeField] private RectTransform computerWindowRect;
        [SerializeField] private TMP_Text questionLabelText;
        [SerializeField] private TMP_Text questionText;
        [SerializeField] private TMP_Text feedbackText;
        [SerializeField] private TMP_InputField answerInputField;
        [SerializeField] private Button submitButton;
        [SerializeField] private Button closeButton;

        [Header("Final Prompt UI")]
        [SerializeField] private GameObject finalPromptPanel;
        [SerializeField] private TMP_Text finalPromptLabelText;
        [SerializeField] private TMP_Text finalPromptText;
        [SerializeField] private Button signButton;
        [SerializeField] private Button tearButton;

        [Header("Monologue Hints")]
        [SerializeField] private string[] monologueHints = new string[]
        {
            "Emergency contact for the '98 K2 Expedition. I remember the mountain. I remember the storm. I remember the rope.",
            "Beneficiary for offshore account 4014. There was money going somewhere every month. I told myself I didn't know where.",
            "Date of my proudest moment. The guitar is in the corner. I should look at the guitar."
        };

        // Question number labels shown in the header bar
        private static readonly string[] QuestionLabels = new string[]
        {
            "SECURITY CHECK  1 / 3",
            "SECURITY CHECK  2 / 3",
            "SECURITY CHECK  3 / 3",
        };

        // Question body text shown in the main panel area
        private static readonly string[] Questions = new string[]
        {
            "Emergency Contact for the '98 K2 Expedition.",
            "Beneficiary Name for Offshore Account 4014.",
            "Date of Your Proudest Moment.",
        };

        // Accepted answers — case-insensitive, trimmed. Multiple forms accepted for Q3.
        private static readonly string[][] Answers = new string[][]
        {
            new[] { "arthur" },
            new[] { "lily" },
            new[] { "10th anniversary", "10th", "tenth anniversary", "our 10th anniversary" }
        };

        private int currentQuestionIndex = 0;
        private bool allAnswered = false;
        private float outsideClickEnabledAtTime = 0f;

        protected override void Start()
        {
            base.Start();

            if (computerOverlay != null)
                computerOverlay.SetActive(false);

            if (computerPanel != null)
                computerPanel.SetActive(false);

            if (finalPromptPanel != null)
                finalPromptPanel.SetActive(false);

            if (submitButton != null)
                submitButton.onClick.AddListener(OnSubmitClicked);

            if (closeButton != null)
                closeButton.onClick.AddListener(ClosePanel);

            if (answerInputField != null)
                answerInputField.onSubmit.AddListener(_ => OnSubmitClicked());

            if (signButton != null)
                signButton.onClick.AddListener(OnSignClicked);

            if (tearButton != null)
                tearButton.onClick.AddListener(OnTearClicked);

            // Sync to any saved progress (e.g. after scene reload in testing).
            // activeSecurityQuestion is 1-indexed (1 = Q1 shown), currentQuestionIndex is 0-indexed.
            if (EventManager.Instance != null)
            {
                int active = EventManager.Instance.activeSecurityQuestion;
                currentQuestionIndex = Mathf.Clamp(active > 0 ? active - 1 : 0, 0, Questions.Length);
                allAnswered = EventManager.Instance.documentUnlocked;
            }

            GameEvents.OnAllQuestionsAnswered += HandleAllQuestionsAnswered;
        }

        void OnDestroy()
        {
            GameEvents.OnAllQuestionsAnswered -= HandleAllQuestionsAnswered;
        }

        void Update()
        {
            // Old computer-screen behavior: click outside the window to exit.
            if (!IsComputerOpen()) return;
            if (!Input.GetMouseButtonDown(0)) return;
            if (Time.unscaledTime < outsideClickEnabledAtTime) return;

            // If no window rect is wired yet (older scene), don't auto-close immediately.
            if (computerWindowRect == null) return;

            // Ignore clicks on any UI element inside the window.
            if (RectTransformUtility.RectangleContainsScreenPoint(computerWindowRect, Input.mousePosition, null))
                return;

            // If there is a final prompt open, don't close from outside click.
            if (finalPromptPanel != null && finalPromptPanel.activeSelf)
                return;

            ClosePanel();
        }

        /// <summary>
        /// Called by InteractableObject2D when the player clicks the computer.
        /// </summary>
        public override void OnInteract()
        {
            if (allAnswered)
            {
                ShowFinalPrompt();
                return;
            }

            OpenPanel();
        }

        private void OpenPanel()
        {
            if (computerPanel == null) return;

            if (computerOverlay != null)
                computerOverlay.SetActive(true);
            computerPanel.SetActive(true);
            DisplayCurrentQuestion();
            outsideClickEnabledAtTime = Time.unscaledTime + 0.12f;
            GameEvents.ComputerOpen();

            if (GameStateMachine.Instance != null)
                GameStateMachine.Instance.ChangeState(GameState.InDialogue);

            // Monologue hints intentionally disabled — Robert's inner voice is no longer shown.
        }

        private void DisplayCurrentQuestion()
        {
            if (currentQuestionIndex >= Questions.Length)
            {
                ShowFinalPrompt();
                return;
            }

            if (questionLabelText != null)
                questionLabelText.text = QuestionLabels[currentQuestionIndex];

            if (questionText != null)
                questionText.text = Questions[currentQuestionIndex];

            if (feedbackText != null)
                feedbackText.text = "";

            if (answerInputField != null)
            {
                answerInputField.text = "";
                answerInputField.interactable = true;
                answerInputField.ActivateInputField();
            }

            if (submitButton != null)
                submitButton.interactable = true;

            // Notify EventManager that this question is now active — shifts LLM prompts
            // and rings the phone so the player can call David as soon as Q1 is shown.
            if (EventManager.Instance != null)
                EventManager.Instance.OnSecurityQuestionStarted(currentQuestionIndex);
        }

        private void OnSubmitClicked()
        {
            if (answerInputField == null) return;

            string raw = answerInputField.text.Trim().ToLowerInvariant();
            if (string.IsNullOrEmpty(raw)) return;

            if (Audio.AudioManager.Instance != null)
                Audio.AudioManager.Instance.PlaySFX("typing");

            if (IsCorrectAnswer(raw, currentQuestionIndex))
            {
                OnCorrectAnswer();
            }
            else
            {
                ShowFeedback("Incorrect. Think harder.");
                answerInputField.text = "";
                answerInputField.ActivateInputField();
            }
        }

        private static bool IsCorrectAnswer(string input, int questionIndex)
        {
            if (questionIndex < 0 || questionIndex >= Answers.Length) return false;

            foreach (string accepted in Answers[questionIndex])
            {
                if (input == accepted) return true;
            }
            return false;
        }

        private void OnCorrectAnswer()
        {
            if (feedbackText != null)
                feedbackText.text = "...";

            if (answerInputField != null)
                answerInputField.interactable = false;

            if (submitButton != null)
                submitButton.interactable = false;

            // Notify EventManager — this advances activeSecurityQuestion and may ring the phone
            if (EventManager.Instance != null)
                EventManager.Instance.OnSecurityQuestionAnswered(currentQuestionIndex);

            currentQuestionIndex++;

            if (currentQuestionIndex >= Questions.Length)
            {
                // All three answered
                if (EventManager.Instance != null)
                    EventManager.Instance.OnAllSecurityQuestionsAnswered();
            }
            else
            {
                // Brief pause then show next question
                Invoke(nameof(DisplayCurrentQuestion), 1.2f);
            }
        }

        private void ShowFeedback(string message)
        {
            if (feedbackText != null)
                feedbackText.text = message;
        }

        private void ClosePanel()
        {
            Audio.AudioManager.Instance?.StopSFX();

            if (computerPanel != null)
                computerPanel.SetActive(false);
            if (computerOverlay != null)
                computerOverlay.SetActive(false);

            GameEvents.ComputerClose();

            if (GameStateMachine.Instance != null)
            {
                if (GameStateMachine.Instance.CurrentState == GameState.InDialogue)
                    GameStateMachine.Instance.ChangeState(GameState.Playing);
            }
        }

        private void HandleAllQuestionsAnswered()
        {
            allAnswered = true;
            ClosePanel();
            // Re-open as final prompt on next click; or show immediately:
            ShowFinalPrompt();
        }

        private void ShowFinalPrompt()
        {
            if (computerPanel != null)
                computerPanel.SetActive(false);
            if (computerOverlay != null)
                computerOverlay.SetActive(true);
            GameEvents.ComputerOpen();

            if (finalPromptPanel != null)
            {
                finalPromptPanel.SetActive(true);

                if (finalPromptLabelText != null)
                    finalPromptLabelText.text = "FINAL SECURITY CHECK";

                if (finalPromptText != null)
                    finalPromptText.text = "Can you forgive yourself?";
            }
            outsideClickEnabledAtTime = Time.unscaledTime + 0.12f;

            if (GameStateMachine.Instance != null)
                GameStateMachine.Instance.ChangeState(GameState.Decision);
        }

        private void OnSignClicked()
        {
            DisableFinalButtons();

            if (FadeManager.Instance != null)
            {
                FadeManager.Instance.FadeOut(2f, () =>
                {
                    HideFinalPrompt();
                    GameManager.Instance.EndGame(signed: true);
                });
            }
            else
            {
                HideFinalPrompt();
                GameManager.Instance.EndGame(signed: true);
            }
        }

        private void OnTearClicked()
        {
            DisableFinalButtons();

            if (FadeManager.Instance != null)
            {
                FadeManager.Instance.FadeOut(2f, () =>
                {
                    HideFinalPrompt();
                    GameManager.Instance.EndGame(signed: false);
                });
            }
            else
            {
                HideFinalPrompt();
                GameManager.Instance.EndGame(signed: false);
            }
        }

        private void DisableFinalButtons()
        {
            if (signButton != null) signButton.interactable = false;
            if (tearButton != null) tearButton.interactable = false;
            Audio.AudioManager.Instance?.StopSFX();
        }

        private void HideFinalPrompt()
        {
            if (finalPromptPanel != null)
                finalPromptPanel.SetActive(false);
            if (computerOverlay != null)
                computerOverlay.SetActive(false);
            GameEvents.ComputerClose();
        }

        private bool IsComputerOpen()
        {
            bool panelOpen = computerPanel != null && computerPanel.activeSelf;
            bool finalOpen = finalPromptPanel != null && finalPromptPanel.activeSelf;
            return panelOpen || finalOpen;
        }
    }
}
