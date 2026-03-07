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
    /// Cinematic letterbox dialogue UI — two black bars (top NPC subtitle, bottom player input).
    /// Implements IDialogueUI so all existing interaction scripts work unchanged.
    /// Assign serialized fields via the editor patch or manually in the Inspector.
    /// </summary>
    public class CinematicDialogueUI : MonoBehaviour, IDialogueUI
    {
        private enum DialogueState
        {
            Closed, Waiting, NPCResponding, PlayerTurn, Monologue, PhoneCall, ActionDescription
        }

        // ── Top bar ────────────────────────────────────────────────────────
        [Header("Top Bar")]
        [SerializeField] private CanvasGroup topBar;
        [SerializeField] private TMP_Text speakerNameText;
        [SerializeField] private TMP_Text subtitleText;

        // ── Bottom bar ─────────────────────────────────────────────────────
        [Header("Bottom Bar")]
        [SerializeField] private CanvasGroup bottomBar;
        [SerializeField] private TMP_Text monologueText;
        [SerializeField] private TMP_InputField inputField;
        [SerializeField] private Button sendButton;

        // ── Skip ">" button ────────────────────────────────────────────────
        [Header("Skip Button")]
        [SerializeField] private Button skipButton;

        // ── Fonts & Colors ─────────────────────────────────────────────────
        [Header("Fonts & Colors")]
        [SerializeField] private TMP_FontAsset subtitleFont;
        [SerializeField] private TMP_FontAsset speakerNameFont;
        [SerializeField] private TMP_FontAsset inputFont;
        [SerializeField] private TMP_FontAsset actionFont;
        [SerializeField] private Color subtitleColor = Color.white;
        [SerializeField] private Color speakerNameColor = new Color(1f, 0.85f, 0.6f, 1f);
        [SerializeField] private Color inputTextColor = Color.white;
        [SerializeField] private Color actionTextColor = new Color(0.78f, 0.78f, 0.78f, 1f);
        [SerializeField, Range(0f, 1f)] private float dimmedAlpha = 0.35f;

        // ── Timing ─────────────────────────────────────────────────────────
        [Header("Timing")]
        [SerializeField] private float typewriterSpeed = 28f;
        [Tooltip("Max visible lines in the top bar before text chunks with '...'. Set to 2 or 3.")]
        [SerializeField, Range(1, 5)] private int maxLinesPerChunk = 3;
        [SerializeField] private float continuationPauseSeconds = 0.7f;
        [SerializeField] private float actionDisplaySeconds = 2.5f;

        // ── Layout ─────────────────────────────────────────────────────────
        [Header("Layout")]
        [SerializeField, Range(0f, 0.3f)] private float topBarHeightRatio = 0.12f;
        [SerializeField, Range(0f, 0.3f)] private float bottomBarHeightRatio = 0.18f;
        [SerializeField, Range(0f, 1f)] private float topBarAlpha = 0.82f;
        [SerializeField, Range(0f, 1f)] private float bottomBarAlpha = 0.88f;

        // ── Waiting Cues ───────────────────────────────────────────────────
        [Header("Waiting Cues")]
        [SerializeField] private string[] waitingCues = new string[]
        {
            "Martha sighs.",
            "Martha: ...",
            "Martha ponders.",
            "Martha stares out in thought.",
            "Martha looks wonderingly into Robert's eyes."
        };

        // ── Private state ──────────────────────────────────────────────────
        private DialogueState state = DialogueState.Closed;
        private string currentCharacter = "martha";
        private string currentObjectId;
        private string currentMemoryId;
        private bool skipRequested;
        private Coroutine activeCoroutine;
        private bool barsHiddenForComputer;
        // Small delay so the click that *opens* dialogue doesn't immediately close it
        private float closeClickEnabledAtTime = 0f;

        // ──────────────────────────────────────────────────────────────────

        void Awake()
        {
            DialogueSession.Current = this;
            ApplyFontsAndColors();
            ApplyStateVisibility();
        }

        void OnEnable()
        {
            GameEvents.OnComputerOpen  += OnComputerOpened;
            GameEvents.OnComputerClose += OnComputerClosed;
        }

        void OnDisable()
        {
            GameEvents.OnComputerOpen  -= OnComputerOpened;
            GameEvents.OnComputerClose -= OnComputerClosed;
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
                inputField.onSubmit.AddListener(_ => OnSendClicked());
            if (skipButton != null)
                skipButton.onClick.AddListener(OnSkipClicked);
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape) && state != DialogueState.Closed)
            {
                Close();
                return;
            }

            // Click outside both bars to close (not during transient states)
            if (state != DialogueState.Closed
                && state != DialogueState.Waiting
                && state != DialogueState.ActionDescription
                && Input.GetMouseButtonDown(0)
                && Time.unscaledTime >= closeClickEnabledAtTime)
            {
                CheckClickOutside();
            }
        }

        private void CheckClickOutside()
        {
            bool onTop    = IsPointOnBar(topBar);
            bool onBottom = IsPointOnBar(bottomBar);
            if (!onTop && !onBottom)
                Close();
        }

        private bool IsPointOnBar(CanvasGroup bar)
        {
            if (bar == null || !bar.gameObject.activeSelf) return false;
            var rt = bar.GetComponent<RectTransform>();
            if (rt == null) return false;
            return RectTransformUtility.RectangleContainsScreenPoint(rt, Input.mousePosition, null);
        }

        // ── IDialogueUI ────────────────────────────────────────────────────

        public void OpenForNPC(string npcId, string npcName)
        {
            currentObjectId = null;
            currentMemoryId = null;
            currentCharacter = npcId;
            closeClickEnabledAtTime = Time.unscaledTime + 0.2f;

            if (LocalLLMManager.Instance != null)
                LocalLLMManager.Instance.SetCharacter(npcId);
            if (GameStateMachine.Instance != null)
                GameStateMachine.Instance.ChangeState(GameState.InDialogue);

            string greeting = npcId == "david"
                ? CharacterPrompts.GetObjectOpeningLine("phone", "david")
                : "Is everything alright? You have that look.";

            SetState(DialogueState.NPCResponding);
            ShowNPCResponse(npcName, greeting);
        }

        public void OpenForObject(string objectId, string memoryId, string displayName)
        {
            currentObjectId = objectId;
            currentMemoryId = memoryId;
            currentCharacter = "martha";
            closeClickEnabledAtTime = Time.unscaledTime + 0.2f;

            if (LocalLLMManager.Instance != null)
                LocalLLMManager.Instance.SetCharacter("martha");
            if (GameStateMachine.Instance != null)
                GameStateMachine.Instance.ChangeState(GameState.InDialogue);

            int activeQuestion = EventManager.Instance != null
                ? EventManager.Instance.activeSecurityQuestion : 0;

            if (memoryId == "guitar" && activeQuestion == 3
                && EventManager.Instance != null
                && !EventManager.Instance.marthaGuitarBreakdown)
            {
                ShowMonologue("There's a massive crack down the back of the neck. It's broken.");
            }

            string greeting = CharacterPrompts.GetObjectOpeningLine(memoryId, "martha", activeQuestion);
            SetState(DialogueState.NPCResponding);
            ShowNPCResponse("Martha", greeting);
        }

        public void OpenForPhone()
        {
            currentCharacter = "david";
            closeClickEnabledAtTime = Time.unscaledTime + 0.2f;

            if (LocalLLMManager.Instance != null)
                LocalLLMManager.Instance.SetCharacter("david");
            if (GameStateMachine.Instance != null)
                GameStateMachine.Instance.ChangeState(GameState.PhoneCall);

            int activeQuestion = EventManager.Instance != null
                ? EventManager.Instance.activeSecurityQuestion : 0;
            string greeting = activeQuestion > 0
                ? CharacterPrompts.GetObjectOpeningLine("phone", "david", activeQuestion)
                : "Hey, old friend. Thought I'd give you a call today. How are you holding up?";

            SetState(DialogueState.PhoneCall);
            ShowNPCResponse("David", greeting);
        }

        public void ShowMonologue(string text)
        {
            StopActive();
            SetState(DialogueState.Monologue);
            SetBarActive(topBar, false);
            SetBarActive(bottomBar, true);
            if (bottomBar != null) bottomBar.alpha = bottomBarAlpha;
            SetBottomContent(showInput: false);
            SetSkipVisible(true);

            activeCoroutine = StartCoroutine(TypewriterBottomBar(text, afterDone: () =>
            {
                activeCoroutine = StartCoroutine(HideBottomAfterDelay(actionDisplaySeconds));
            }));
        }

        public void ShowAction(string text)
        {
            StopActive();
            SetState(DialogueState.ActionDescription);
            SetBarActive(topBar, true);
            if (topBar != null) topBar.alpha = topBarAlpha;
            SetBarActive(bottomBar, false);

            if (speakerNameText != null) speakerNameText.text = "";
            if (subtitleText != null)
            {
                subtitleText.fontStyle = FontStyles.Italic;
                subtitleText.color = actionTextColor;
                if (actionFont != null) subtitleText.font = actionFont;
                subtitleText.text = $"[ {text} ]";
                subtitleText.maxVisibleCharacters = int.MaxValue;
            }
            SetSkipVisible(false);
            activeCoroutine = StartCoroutine(AutoCloseTopBarAfter(actionDisplaySeconds));
        }

        public void Close()
        {
            StopActive();
            if (LocalLLMManager.Instance != null)
                LocalLLMManager.Instance.ClearHistory();
            if (GameStateMachine.Instance != null)
            {
                var s = GameStateMachine.Instance.CurrentState;
                if (s == GameState.InDialogue || s == GameState.PhoneCall)
                    GameStateMachine.Instance.ChangeState(GameState.Playing);
            }
            SetState(DialogueState.Closed);
            ApplyStateVisibility();
        }

        // ── Computer bar suppression ───────────────────────────────────────

        private void OnComputerOpened()
        {
            barsHiddenForComputer = true;
            if (topBar != null)    topBar.gameObject.SetActive(false);
            if (bottomBar != null) bottomBar.gameObject.SetActive(false);
        }

        private void OnComputerClosed()
        {
            barsHiddenForComputer = false;
            ApplyStateVisibility();
        }

        // ── NPC response flow ──────────────────────────────────────────────

        private void ShowNPCResponse(string speakerName, string text)
        {
            StopActive();

            SetBarActive(topBar, true);
            if (topBar != null) topBar.alpha = topBarAlpha;
            SetBarActive(bottomBar, false);
            SetSkipVisible(true);

            if (speakerNameText != null)
            {
                speakerNameText.text = speakerName;
                speakerNameText.fontStyle = FontStyles.Normal;
                speakerNameText.color = speakerNameColor;
                if (speakerNameFont != null) speakerNameText.font = speakerNameFont;
            }

            if (subtitleText != null)
            {
                subtitleText.fontStyle = FontStyles.Normal;
                subtitleText.color = subtitleColor;
                if (subtitleFont != null) subtitleText.font = subtitleFont;
            }

            var chunks = SplitIntoChunks(text);
            activeCoroutine = StartCoroutine(PlayChunks(chunks, afterDone: TransitionToPlayerTurn));
        }

        private void TransitionToPlayerTurn()
        {
            SetState(DialogueState.PlayerTurn);
            SetSkipVisible(false);

            if (topBar != null) topBar.alpha = dimmedAlpha;

            SetBarActive(bottomBar, true);
            if (bottomBar != null) bottomBar.alpha = bottomBarAlpha;
            SetBottomContent(showInput: true);

            if (inputField != null)
            {
                inputField.text = "";
                inputField.interactable = true;
                if (sendButton != null) sendButton.interactable = true;
                StartCoroutine(FocusInputNextFrame());
            }
        }

        // ── Submit player input ────────────────────────────────────────────

        private void OnSendClicked() => SubmitInput();
        private void OnSkipClicked() => skipRequested = true;

        private async void SubmitInput()
        {
            if (inputField == null) return;
            string playerText = inputField.text.Trim();
            if (string.IsNullOrEmpty(playerText)) return;

            inputField.text = "";
            inputField.interactable = false;
            if (sendButton != null) sendButton.interactable = false;

            // Guitar breakdown detection
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

            // Show waiting cue in top bar
            ShowWaitingCue();

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
            {
                response = "...";
            }

            string speakerName = currentCharacter == "david" ? "David" : "Martha";
            // Preserve PhoneCall state across the response
            if (state != DialogueState.PhoneCall)
                SetState(DialogueState.NPCResponding);
            ShowNPCResponse(speakerName, response);
            GameEvents.ReceiveDialogue(currentCharacter, response);
        }

        private void ShowWaitingCue()
        {
            StopActive();
            SetState(DialogueState.Waiting);
            SetBarActive(topBar, true);
            if (topBar != null) topBar.alpha = topBarAlpha;
            SetBarActive(bottomBar, false);
            SetSkipVisible(false);

            if (subtitleText != null && waitingCues != null && waitingCues.Length > 0)
            {
                int idx = Random.Range(0, waitingCues.Length);
                string speakerName = currentCharacter == "david" ? "David" : "Martha";
                string cue = waitingCues[idx].Replace("Martha", speakerName);
                subtitleText.text = $"[ {cue} ]";
                subtitleText.fontStyle = FontStyles.Italic;
                subtitleText.color = actionTextColor;
                subtitleText.maxVisibleCharacters = int.MaxValue;
                if (actionFont != null) subtitleText.font = actionFont;
            }

            if (speakerNameText != null)
            {
                string speakerName = currentCharacter == "david" ? "David" : "Martha";
                speakerNameText.text = speakerName;
            }
        }

        // ── 2-line overflow chunking ───────────────────────────────────────

        private List<string> SplitIntoChunks(string text)
        {
            var chunks = new List<string>();
            if (subtitleText == null || string.IsNullOrEmpty(text))
            {
                chunks.Add(text ?? "");
                return chunks;
            }

            string remaining = text.Trim();
            int safetyLimit = 20;

            while (!string.IsNullOrEmpty(remaining) && safetyLimit-- > 0)
            {
                subtitleText.text = remaining;
                subtitleText.maxVisibleCharacters = int.MaxValue;
                subtitleText.ForceMeshUpdate(true, true);

                if (subtitleText.textInfo.lineCount <= maxLinesPerChunk)
                {
                    chunks.Add(remaining);
                    break;
                }

                // Split at the end of the last allowed line (0-indexed)
                var lineInfo = subtitleText.textInfo.lineInfo;
                int splitIndex = lineInfo[maxLinesPerChunk - 1].lastCharacterIndex + 1;

                // Walk back to a word boundary
                int walkBack = splitIndex;
                while (walkBack > 1 && remaining[walkBack - 1] != ' ')
                    walkBack--;
                if (walkBack > 1) splitIndex = walkBack;

                string chunk = remaining.Substring(0, splitIndex).TrimEnd();
                remaining = remaining.Substring(splitIndex).TrimStart();
                if (!string.IsNullOrEmpty(chunk))
                    chunks.Add(chunk);
                else
                    break;
            }

            return chunks;
        }

        // ── Typewriter coroutines ──────────────────────────────────────────

        private IEnumerator PlayChunks(List<string> chunks, System.Action afterDone)
        {
            for (int c = 0; c < chunks.Count; c++)
            {
                bool hasMore = c < chunks.Count - 1;
                string prefix  = c > 0    ? "..."  : "";
                string suffix  = hasMore  ? "..."  : "";
                string display = prefix + chunks[c] + suffix;

                if (subtitleText != null)
                {
                    subtitleText.text = display;
                    subtitleText.maxVisibleCharacters = prefix.Length;
                    subtitleText.ForceMeshUpdate();
                }

                for (int i = prefix.Length; i <= display.Length; i++)
                {
                    if (skipRequested)
                    {
                        if (subtitleText != null)
                            subtitleText.maxVisibleCharacters = display.Length;
                        break;
                    }
                    if (subtitleText != null)
                        subtitleText.maxVisibleCharacters = i;
                    yield return new WaitForSeconds(1f / typewriterSpeed);
                }

                skipRequested = false;

                if (hasMore)
                    yield return new WaitForSeconds(continuationPauseSeconds);
            }

            activeCoroutine = null;
            afterDone?.Invoke();
        }

        private IEnumerator TypewriterBottomBar(string text, System.Action afterDone)
        {
            if (monologueText == null) { afterDone?.Invoke(); yield break; }

            monologueText.text = text;
            monologueText.maxVisibleCharacters = 0;
            monologueText.ForceMeshUpdate();

            for (int i = 0; i <= text.Length; i++)
            {
                if (skipRequested)
                {
                    monologueText.maxVisibleCharacters = text.Length;
                    break;
                }
                monologueText.maxVisibleCharacters = i;
                yield return new WaitForSeconds(1f / typewriterSpeed);
            }

            skipRequested = false;
            activeCoroutine = null;
            afterDone?.Invoke();
        }

        private IEnumerator AutoCloseTopBarAfter(float delay)
        {
            yield return new WaitForSeconds(delay);
            SetState(DialogueState.Closed);
            ApplyStateVisibility();
            activeCoroutine = null;
        }

        private IEnumerator HideBottomAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            SetState(DialogueState.Closed);
            ApplyStateVisibility();
            activeCoroutine = null;
        }

        private IEnumerator FocusInputNextFrame()
        {
            yield return null;
            if (inputField != null) inputField.ActivateInputField();
        }

        // ── State & visibility helpers ─────────────────────────────────────

        private void SetState(DialogueState newState)
        {
            state = newState;
        }

        private void ApplyStateVisibility()
        {
            if (barsHiddenForComputer)
            {
                if (topBar != null)    topBar.gameObject.SetActive(false);
                if (bottomBar != null) bottomBar.gameObject.SetActive(false);
                return;
            }

            switch (state)
            {
                case DialogueState.Closed:
                    SetBarActive(topBar, false);
                    SetBarActive(bottomBar, false);
                    break;

                case DialogueState.Waiting:
                case DialogueState.NPCResponding:
                case DialogueState.PhoneCall:
                case DialogueState.ActionDescription:
                    SetBarActive(topBar, true);
                    if (topBar != null) topBar.alpha = topBarAlpha;
                    SetBarActive(bottomBar, false);
                    break;

                case DialogueState.PlayerTurn:
                    SetBarActive(topBar, true);
                    if (topBar != null) topBar.alpha = dimmedAlpha;
                    SetBarActive(bottomBar, true);
                    if (bottomBar != null) bottomBar.alpha = bottomBarAlpha;
                    break;

                case DialogueState.Monologue:
                    SetBarActive(topBar, false);
                    SetBarActive(bottomBar, true);
                    if (bottomBar != null) bottomBar.alpha = bottomBarAlpha;
                    break;
            }
        }

        private void SetBarActive(CanvasGroup bar, bool active)
        {
            if (bar == null) return;
            bar.gameObject.SetActive(active);
        }

        private void SetBottomContent(bool showInput)
        {
            if (inputField != null)  inputField.gameObject.SetActive(showInput);
            if (sendButton != null)  sendButton.gameObject.SetActive(showInput);
            if (monologueText != null) monologueText.gameObject.SetActive(!showInput);
        }

        private void SetSkipVisible(bool visible)
        {
            if (skipButton != null) skipButton.gameObject.SetActive(visible);
        }

        private void StopActive()
        {
            if (activeCoroutine != null)
            {
                StopCoroutine(activeCoroutine);
                activeCoroutine = null;
            }
            skipRequested = false;
        }

        // ── Font / colour application ──────────────────────────────────────

        private void ApplyFontsAndColors()
        {
            if (subtitleText != null)
            {
                subtitleText.color = subtitleColor;
                if (subtitleFont != null) subtitleText.font = subtitleFont;
            }
            if (speakerNameText != null)
            {
                speakerNameText.color = speakerNameColor;
                if (speakerNameFont != null) speakerNameText.font = speakerNameFont;
            }
            if (inputField != null)
            {
                if (inputField.textComponent != null)
                {
                    inputField.textComponent.color = inputTextColor;
                    if (inputFont != null) inputField.textComponent.font = inputFont;
                }
            }
        }
    }
}
