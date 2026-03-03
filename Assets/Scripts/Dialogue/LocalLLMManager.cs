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

        [System.Serializable]
        private struct ConversationEntry
        {
            public string role;
            public string content;
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

            conversationHistory.Add(new ConversationEntry { role = "user", content = playerInput });

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

            string prompt = currentCharacter == "david"
                ? CharacterPrompts.GetDavidPrompt(memories ?? new List<string>(), activeQuestion)
                : CharacterPrompts.GetMarthaPrompt(memories ?? new List<string>(), activeQuestion, shutdownMode, guitarBreakdown);

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

        /// <summary>
        /// Strips artifacts that appear when the LLM writes dialogue like a script.
        /// Removes: [Martha]: prefix, [Robert]: lines, [MEMORY CONTEXT] blocks, stage directions.
        /// </summary>
        private string StripScriptArtifacts(string response)
        {
            // Remove any leading "Martha:" or "[Martha]:" label
            response = System.Text.RegularExpressions.Regex.Replace(
                response, @"^\s*\[?Martha\]?\s*:", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            // Remove any leading "David:" or "[David]:" label
            response = System.Text.RegularExpressions.Regex.Replace(
                response, @"^\s*\[?David\]?\s*:", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            // Cut everything from the first [Robert]: or Robert: line onward
            int robertIdx = System.Text.RegularExpressions.Regex.Match(
                response, @"\[?Robert\]?\s*:", System.Text.RegularExpressions.RegexOptions.IgnoreCase).Index;
            if (robertIdx > 0)
                response = response.Substring(0, robertIdx);

            // Remove [MEMORY CONTEXT] blocks and everything after
            int memCtxIdx = response.IndexOf("[MEMORY CONTEXT", System.StringComparison.OrdinalIgnoreCase);
            if (memCtxIdx >= 0)
                response = response.Substring(0, memCtxIdx);

            // Remove standalone stage directions like [sighs] or *sighs* that leaked out
            response = System.Text.RegularExpressions.Regex.Replace(
                response, @"\[[^\]]{0,40}\]", "");

            return response.Trim();
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

            if (character == "david")
            {
                if (activeQuestion == 1 && (lowerInput.Contains("rope") || lowerInput.Contains("arthur") || lowerInput.Contains("expedition") || lowerInput.Contains("k2") || lowerInput.Contains("mountain")))
                    return "His name was Arthur. And you know what you did, Robert. I was on the radio. I heard him.";
                if (activeQuestion == 2 && (lowerInput.Contains("money") || lowerInput.Contains("account") || lowerInput.Contains("investment")))
                    return "Stop playing dumb. Sarah. The child support. Twenty-five years. Lily's name is Lily.";
                if (activeQuestion == 3 && (lowerInput.Contains("guitar") || lowerInput.Contains("anniversary")))
                    return "The guitar? I don't know, buddy. You just stopped playing one day. Whatever happened, that's between you and Martha.";
                if (lowerInput.Contains("help") || lowerInput.Contains("advice"))
                    return "I can't tell you what to do. But I know you — you've never been one to run from a hard thing. Not until now.";
                return "I'm here, pal. Whatever you need to say.";
            }

            // Martha stubs — narrative-aware
            if (shutdownMode)
                return "I kept the pieces, Robert. In a box in the closet. Thirty-seven years.";

            if (guitarBreakdown)
                return "You came home drunk. You had that look. I sat on the floor until morning, picking up pieces of the neck.";

            if (activeQuestion == 1 && ContainsAny(lowerInput, "rope", "expedition", "mountain", "k2"))
                return "He tried so hard, Robert. The storm was impossible. He fought to hold on. He couldn't save them. That's the truth of it.";

            if (activeQuestion == 2 && ContainsAny(lowerInput, "money", "account", "investment"))
                return "Bad investments, that's all. It was always just the two of us. You know that. We never needed anything more.";

            if (activeQuestion == 3 && ContainsAny(lowerInput, "guitar", "anniversary", "song"))
                return "It was our tenth anniversary. You stayed up all night writing it. The kitchen at sunrise, still in your dress shirt. I've never forgotten a single note.";

            if (ContainsAny(lowerInput, "photo", "wedding"))
                return "Your father's tie was too short. You were so nervous you didn't even notice. I loved you so much in that moment.";

            return "I'm here, love. Whatever you need to say.";
        }

        private static bool ContainsAny(string input, params string[] keywords)
        {
            foreach (string kw in keywords)
                if (input.Contains(kw)) return true;
            return false;
        }
    }
}
