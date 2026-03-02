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

        public async Task Initialize()
        {
#if LLMUNITY_AVAILABLE
            if (useLLM && marthaCharacter != null)
            {
                marthaCharacter.systemPrompt = CharacterPrompts.GetMarthaPrompt(new List<string>());
                marthaCharacter.numPredict = maxTokens;
                marthaCharacter.temperature = temperature;

                if (davidCharacter != null)
                {
                    davidCharacter.systemPrompt = CharacterPrompts.GetDavidPrompt(new List<string>());
                    davidCharacter.numPredict = maxTokens;
                    davidCharacter.temperature = temperature;
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
            if (memories == null || memories.Count == 0) return;

            // CharacterPrompts already weaves memories into the persona section —
            // no separate [MEMORY CONTEXT] block that could leak into responses.
            string prompt = currentCharacter == "david"
                ? CharacterPrompts.GetDavidPrompt(memories)
                : CharacterPrompts.GetMarthaPrompt(memories);

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
            string lowerInput = input.ToLower();

            if (character == "david")
            {
                if (lowerInput.Contains("help") || lowerInput.Contains("advice"))
                    return "Look buddy, I can't tell you what to do. But I know you - you've never been one to shy away from a tough call.";
                if (lowerInput.Contains("scared") || lowerInput.Contains("afraid"))
                    return "Yeah. I'd be scared too. But you've faced worse, remember? That time on the mountain?";
                return "I'm here, pal. Whatever you need to talk about.";
            }

            if (lowerInput.Contains("photo") || lowerInput.Contains("wedding"))
                return "Oh, that photo... We were so young. You wore your father's tie, remember? It was too short. *soft laugh*";
            if (lowerInput.Contains("guitar") || lowerInput.Contains("music"))
                return "I miss hearing you play on Sunday mornings. The house felt so alive with music.";
            if (lowerInput.Contains("love"))
                return "You know I do, dear. Forty-seven years and counting. Every single day.";
            if (lowerInput.Contains("scared") || lowerInput.Contains("afraid"))
                return "I know, love. I am too. But I'm right here. I'm not going anywhere.";

            return "I'm here, dear. Whatever you need to say, I'm listening.";
        }
    }
}
