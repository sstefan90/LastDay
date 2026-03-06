using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;
using LastDay.Core;
#if LLMUNITY_AVAILABLE
using LLMUnity;
#endif

namespace LastDay.Dialogue
{
    /// <summary>
    /// Manages the local LLM for generating NPC dialogue.
    /// Uses LLMUnity's LLMCharacter when available, falls back to stub responses.
    /// </summary>
    public class LocalLLMManager : MonoBehaviour
    {
        public static LocalLLMManager Instance { get; private set; }

        [Header("LLM Settings")]
        [SerializeField] private int maxTokens = 80;
        [SerializeField] private float temperature = 0.7f;

        [Header("State")]
        public bool isInitialized;
        public string currentCharacter = "martha";
        public bool useLLM = true;

#if LLMUNITY_AVAILABLE
        [Header("LLMUnity References")]
        [SerializeField] private LLMAgent marthaCharacter;
        [SerializeField] private LLMAgent davidCharacter;

        private LLMAgent ActiveCharacter =>
            currentCharacter == "david" ? davidCharacter : marthaCharacter;
#endif

        private List<ConversationEntry> conversationHistory = new List<ConversationEntry>();
        private Dictionary<string, int> turnCounts = new Dictionary<string, int>();

        [System.Serializable]
        private struct ConversationEntry
        {
            public string role;
            public string content;
        }

        private int GetTurnCount(string character)
        {
            turnCounts.TryGetValue(character, out int count);
            return count;
        }

        private void IncrementTurnCount(string character)
        {
            turnCounts.TryGetValue(character, out int count);
            turnCounts[character] = count + 1;
        }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        /// <summary>
        /// Optionally supply the model path returned by ModelDownloader.
        /// If null, the LLM component's already-configured model path is used.
        /// </summary>
        public async Task Initialize(string modelPath = null)
        {
#if LLMUNITY_AVAILABLE
            if (useLLM && marthaCharacter != null)
            {
                // Apply runtime model path if provided (from ModelDownloader)
                if (!string.IsNullOrEmpty(modelPath) && marthaCharacter.llm != null)
                {
                    marthaCharacter.llm.model = modelPath;
                    Debug.Log($"[LLM] Model path set to: {modelPath}");
                }

                marthaCharacter.systemPrompt = CharacterPrompts.GetMarthaPrompt(new List<string>());
                marthaCharacter.numPredict   = maxTokens;
                marthaCharacter.temperature  = temperature;

                if (davidCharacter != null)
                {
                    davidCharacter.systemPrompt = CharacterPrompts.GetDavidPrompt(new List<string>());
                    davidCharacter.numPredict   = maxTokens;
                    davidCharacter.temperature  = temperature;
                }

                try
                {
                    await marthaCharacter.Warmup();
                    isInitialized = true;
                    Debug.Log("[LLM] Initialized with LLMUnity. Model warmed up.");
                    return;
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"[LLM] LLMUnity warmup failed: {e.Message}. Falling back to stub.");
                }
            }
#endif
            await Task.Delay(100);
            isInitialized = true;
            Debug.Log("[LLM] Initialized in stub mode.");
        }

        public async Task<string> GenerateResponse(string playerInput, string character = null, List<string> memories = null)
        {
            if (character != null)
                currentCharacter = character;

            IncrementTurnCount(currentCharacter);
            conversationHistory.Add(new ConversationEntry { role = "user", content = playerInput });

            // After 2+ turns with David on a mystery topic, mark resistance as used
            if (currentCharacter == "david" && GetTurnCount("david") >= 2
                && EventManager.Instance != null)
            {
                int aq = EventManager.Instance.activeSecurityQuestion;
                if (aq > 0 && !EventManager.Instance.HasDavidResisted(aq))
                    EventManager.Instance.MarkDavidResisted(aq);
            }

#if LLMUNITY_AVAILABLE
            if (useLLM && ActiveCharacter != null && isInitialized)
            {
                try
                {
                    UpdatePromptWithMemories(memories);

                    string response = await ActiveCharacter.Chat(playerInput);
                    response = ValidateResponse(response);

                    conversationHistory.Add(new ConversationEntry { role = "assistant", content = response });
                    Debug.Log($"[LLM] {currentCharacter} responded: {response}");
                    return response;
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"[LLM] Generation failed: {e.Message}. Using fallback.");
                }
            }
#endif
            if (!isInitialized)
            {
                Debug.LogWarning("[LLM] Not initialized. Returning fallback.");
                return GetFallbackResponse();
            }

            await Task.Delay(Random.Range(500, 1500));

            string stubResponse = GetStubResponse(playerInput, currentCharacter);
            conversationHistory.Add(new ConversationEntry { role = "assistant", content = stubResponse });
            return stubResponse;
        }

        public void SetCharacter(string character)
        {
            if (currentCharacter != character)
            {
                currentCharacter = character;
                conversationHistory.Clear();

#if LLMUNITY_AVAILABLE
                if (ActiveCharacter != null)
                {
                    _ = ActiveCharacter.ClearHistory();
                }
#endif
                Debug.Log($"[LLM] Switched to character: {character}");
            }
        }

        public void ResetTurnCounts()
        {
            turnCounts.Clear();
        }

        public void ClearHistory()
        {
            conversationHistory.Clear();

#if LLMUNITY_AVAILABLE
            if (ActiveCharacter != null)
                _ = ActiveCharacter.ClearHistory();
#endif
        }

#if LLMUNITY_AVAILABLE
        private void UpdatePromptWithMemories(List<string> memories)
        {
            int activeQuestion   = EventManager.Instance != null ? EventManager.Instance.activeSecurityQuestion  : 0;
            bool shutdownMode    = EventManager.Instance != null && EventManager.Instance.marthaShutdownMode;
            bool guitarBreakdown = EventManager.Instance != null && EventManager.Instance.marthaGuitarBreakdown;
            bool davidResisted   = EventManager.Instance != null && EventManager.Instance.HasDavidResisted(activeQuestion);

            string prompt = currentCharacter == "david"
                ? CharacterPrompts.GetDavidPrompt(memories ?? new List<string>(), activeQuestion, davidResisted)
                : CharacterPrompts.GetMarthaPrompt(memories ?? new List<string>(), activeQuestion, shutdownMode, guitarBreakdown);

            int turns = GetTurnCount(currentCharacter);
            if (turns > 0)
                prompt += $"\n\n<turn_count>This is exchange #{turns} in this conversation. Vary your tone and phrasing. Do not repeat earlier responses.</turn_count>";

            ActiveCharacter.systemPrompt = prompt;
        }
#endif

        private string ValidateResponse(string response)
        {
            if (string.IsNullOrEmpty(response))
                return GetFallbackResponse();

            response = StripScriptArtifacts(response);

            if (response.Length < 8)
                return GetFallbackResponse();

            if (response.Length > 500)
            {
                int cutoff = response.LastIndexOf('.', 500);
                if (cutoff > 50)
                    response = response.Substring(0, cutoff + 1);
            }

            string[] forbidden = { "I'm an AI", "language model", "As an AI", "I am an AI", "as a language" };
            foreach (var pattern in forbidden)
            {
                if (response.Contains(pattern, System.StringComparison.OrdinalIgnoreCase))
                    return GetFallbackResponse();
            }

            return response.Trim();
        }

        private static readonly string[] TruncationMarkers = {
            "CONSTRAINT", "ALTERED OUTCOME", "HOW TO PLAY", "HOW TO SPEAK",
            "WHAT NOT TO DO", "OUTPUT FORMAT", "CURRENT STATE", "CORE PERSONALITY",
            "SPEECH PATTERN", "MEMORY CONTEXT", "NEW CONSTRAINT", "HIDDEN INNER",
            "<role>", "<voice>", "<rules>", "<context>", "<secret>", "<aware>"
        };

        private string StripScriptArtifacts(string response)
        {
            var ignoreCase = System.Text.RegularExpressions.RegexOptions.IgnoreCase;

            response = System.Text.RegularExpressions.Regex.Replace(
                response, @"^\s*\[?Martha\]?\s*:", "", ignoreCase);
            response = System.Text.RegularExpressions.Regex.Replace(
                response, @"^\s*\[?David\]?\s*:", "", ignoreCase);

            var robertMatch = System.Text.RegularExpressions.Regex.Match(
                response, @"\[?Robert\]?\s*:", ignoreCase);
            if (robertMatch.Success && robertMatch.Index > 0)
                response = response.Substring(0, robertMatch.Index);

            foreach (string marker in TruncationMarkers)
            {
                int idx = response.IndexOf(marker, System.StringComparison.OrdinalIgnoreCase);
                if (idx > 0) response = response.Substring(0, idx);
            }

            var sepMatch = System.Text.RegularExpressions.Regex.Match(
                response, @"[\u2501━\-=─═—]{3,}");
            if (sepMatch.Success && sepMatch.Index > 0)
                response = response.Substring(0, sepMatch.Index);

            var numberedRule = System.Text.RegularExpressions.Regex.Match(
                response, @"\n\s*\d+\.\s+(Martha|David|Robert|The character|Do not|Never|Always)", ignoreCase);
            if (numberedRule.Success && numberedRule.Index > 0)
                response = response.Substring(0, numberedRule.Index);

            response = System.Text.RegularExpressions.Regex.Replace(
                response, @"\((?:Note|Martha|Robert|David|The character|The response)[^)]{0,200}\)", "", ignoreCase);

            // Remove any XML-style tag pairs the LLM echoed back: <tag>content</tag> or bare <tag>
            response = System.Text.RegularExpressions.Regex.Replace(
                response, @"<[a-z_]+>[^<]{0,300}</[a-z_]+>", "", ignoreCase);
            response = System.Text.RegularExpressions.Regex.Replace(
                response, @"</?[a-z_]+>", "", ignoreCase);

            response = System.Text.RegularExpressions.Regex.Replace(
                response, @"\[[^\]]{0,40}\]", "");

            response = System.Text.RegularExpressions.Regex.Replace(
                response, @"\*[^*]{1,40}\*", "");

            response = response.Trim();
            response = CapSentences(response, 5);

            return response;
        }

        private static string CapSentences(string text, int maxSentences)
        {
            int count = 0;
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if ((c == '.' || c == '!' || c == '?') && i + 1 < text.Length && text[i + 1] == ' ')
                {
                    count++;
                    if (count >= maxSentences)
                        return text.Substring(0, i + 1);
                }
            }
            return text;
        }

        private string GetFallbackResponse()
        {
            string[] marthaFallbacks =
            {
                "I'm just thinking, dear. Give me a moment.",
                "Some things are hard to put into words.",
                "You know, after all these years... I still don't always know what to say.",
                "Let's just sit here together for a moment."
            };

            string[] davidFallbacks =
            {
                "Yeah, I hear you, buddy.",
                "Look, I'm not gonna pretend I have all the answers.",
                "You know me - I say what I think. And I think you already know.",
                "Whatever you decide, I've got your back. Always have."
            };

            var pool = currentCharacter == "david" ? davidFallbacks : marthaFallbacks;
            return pool[Random.Range(0, pool.Length)];
        }

        private string GetStubResponse(string input, string character)
        {
            string lowerInput    = input.ToLower();
            int activeQuestion   = EventManager.Instance != null ? EventManager.Instance.activeSecurityQuestion  : 0;
            bool shutdownMode    = EventManager.Instance != null && EventManager.Instance.marthaShutdownMode;
            bool guitarBreakdown = EventManager.Instance != null && EventManager.Instance.marthaGuitarBreakdown;
            bool davidResisted   = EventManager.Instance != null && EventManager.Instance.HasDavidResisted(activeQuestion);

            if (character == "david")
            {
                if (activeQuestion == 1 && ContainsAny(lowerInput, "rope", "arthur", "expedition", "k2", "mountain"))
                {
                    if (!davidResisted)
                        return "You really want to open that box, Robert? Right now? Today of all days?";
                    return "His name was Arthur. And you know what you did, Robert. I was on the radio. I heard him.";
                }
                if (activeQuestion == 2 && ContainsAny(lowerInput, "money", "account", "investment"))
                {
                    if (!davidResisted)
                        return "This is what you want to talk about? Money? I was hoping you wouldn't ask me about this.";
                    return "Sarah. The child support. Twenty-five years. Her name is Lily. You know that already.";
                }
                if (activeQuestion == 3 && ContainsAny(lowerInput, "guitar", "anniversary"))
                    return "The guitar? Honestly, I have no idea. You just stopped playing one day. I asked you about it once and you changed the subject.";
                if (ContainsAny(lowerInput, "help", "advice"))
                    return "I can't tell you what to do. But I know you — you've never been one to run from a hard thing. Not until now.";
                return "I'm here, pal. Whatever you need to say.";
            }

            if (shutdownMode)
                return "I kept the pieces, Robert. In a box in the closet. Thirty-seven years.";

            if (guitarBreakdown)
                return "You came home drunk. You had that look. I sat on the floor until morning, picking up pieces of the neck.";

            if (activeQuestion == 1 && ContainsAny(lowerInput, "rope", "expedition", "mountain", "k2"))
                return "The storm was terrible. He fought to hold it. That is all that matters. You know, I worry about David sometimes. Alone in that house.";

            if (activeQuestion == 2 && ContainsAny(lowerInput, "money", "account", "investment"))
                return "Bad investments, that's all. The kitchen we painted together, that awful wallpaper argument — that was our life. Maybe you should call David. He's been so alone since Margaret.";

            if (activeQuestion == 3 && ContainsAny(lowerInput, "guitar", "anniversary", "song"))
                return "Our tenth anniversary. You stayed up all night writing me a song. The kitchen at sunrise, still in your dress shirt. I've never forgotten a single note.";

            if (ContainsAny(lowerInput, "photo", "wedding"))
                return "Your father's tie was too short. You were so nervous you didn't even notice.";

            return "Your hands are doing that thing again. Are you cold, or just thinking?";
        }

        private static bool ContainsAny(string input, params string[] keywords)
        {
            foreach (string kw in keywords)
                if (input.Contains(kw)) return true;
            return false;
        }
    }
}
