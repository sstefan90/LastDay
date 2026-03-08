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
    [DefaultExecutionOrder(-50)] // run before LLM [-1] so contextSize can be set before server starts
    public class LocalLLMManager : MonoBehaviour
    {
        public static LocalLLMManager Instance { get; private set; }

        [Header("LLM Settings")]
        [SerializeField] private int maxTokens = 100;
        [SerializeField] private float temperature = 0.5f;
        // Each model instance gets its own independent 4096-token context window.
        [SerializeField] private int contextSize = 4096;

        [Header("State")]
        public bool isInitialized;
        public string currentCharacter = "martha";
        public bool useLLM = true;

#if LLMUNITY_AVAILABLE
        [Header("LLMUnity — Characters")]
        [SerializeField] private LLMAgent marthaCharacter;
        [SerializeField] private LLMAgent davidCharacter;

        [Header("LLMUnity — Models")]
        [Tooltip("Martha's LLM server (Llama 3 8B). Lives on the LocalLLMManager GameObject.")]
        [SerializeField] private LLM marthaLLM;
        [Tooltip("David's LLM server (Phi-3 Mini). Assigned by DavidModelSetup — run LastDay > Setup: David Model.")]
        [SerializeField] private LLM davidLLM;

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

#if LLMUNITY_AVAILABLE
            // Resolve model path BEFORE LLM.Awake() (execution order -1) starts the llama.cpp server.
            // The scene-baked _model path is a developer-machine absolute path that won't exist on
            // other machines. We override it here at execution order -50 so the server starts with
            // the correct path (either local Models/ or persistentDataPath/Models/ after download).
            if (marthaLLM == null) marthaLLM = GetComponent<LLM>();

            string resolvedModel = ModelDownloader.GetPathForFilename("llama3-8b-instruct.gguf");
            bool   modelOnDisk   = !string.IsNullOrEmpty(resolvedModel)
                                   && System.IO.File.Exists(resolvedModel);

            if (marthaLLM != null)
            {
                marthaLLM.contextSize = contextSize;
                if (modelOnDisk)
                {
                    marthaLLM.model = resolvedModel;
                    Debug.Log($"[LLM] Martha model path set in Awake: {resolvedModel}");
                }
                else
                {
                    Debug.LogWarning($"[LLM] Model not found at resolve time ({resolvedModel}). " +
                                     "Ensure LoadingScene has run and the model has been downloaded.");
                }
            }
            if (davidLLM != null)
            {
                davidLLM.contextSize = contextSize;
                if (modelOnDisk) davidLLM.model = resolvedModel;
            }
            else
            {
                Debug.LogWarning("[LLM] davidLLM not assigned — David shares Martha's LLM server. " +
                                 "Run LastDay > Setup: David Model to separate them.");
            }
#endif
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
                // Martha → Llama 3 8B (path from ModelDownloader or caller)
                string marthaPath = !string.IsNullOrEmpty(modelPath)
                    ? modelPath
                    : ModelDownloader.GetPathForFilename("llama3-8b-instruct.gguf");
                if (marthaCharacter.llm != null && !string.IsNullOrEmpty(marthaPath))
                {
                    marthaCharacter.llm.model = marthaPath;
                    Debug.Log($"[LLM] Martha model: {marthaPath}");
                }

                // David → Phi-3 Mini (separate server if davidLLM is assigned)
                if (davidCharacter != null && davidLLM != null)
                {
                    string davidPath = ModelDownloader.GetPathForFilename("llama3-8b-instruct.gguf");
                    if (!string.IsNullOrEmpty(davidPath))
                    {
                        davidLLM.model = davidPath;
                        Debug.Log($"[LLM] David model (Llama 3 8B): {davidPath}");
                    }
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
                    var marthaWarmup = marthaCharacter.Warmup();
                    if (await System.Threading.Tasks.Task.WhenAny(marthaWarmup, System.Threading.Tasks.Task.Delay(System.TimeSpan.FromSeconds(20))) != marthaWarmup)
                        throw new System.TimeoutException("Martha Warmup timed out after 20 s.");
                    await marthaWarmup;
                    Debug.Log("[LLM] Martha warmed up.");

                    if (davidCharacter != null)
                    {
                        var davidWarmup = davidCharacter.Warmup();
                        if (await System.Threading.Tasks.Task.WhenAny(davidWarmup, System.Threading.Tasks.Task.Delay(System.TimeSpan.FromSeconds(20))) != davidWarmup)
                            throw new System.TimeoutException("David Warmup timed out after 20 s.");
                        await davidWarmup;
                        Debug.Log("[LLM] David warmed up.");
                    }

                    isInitialized = true;
                    Debug.Log("[LLM] Initialized with LLMUnity. Both characters warmed up.");
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

            // Route David to the correct single-secret prompt state based on what the player just asked.
            // This must happen before the resistance check so the right activeSecurityQuestion is used.
            if (currentCharacter == "david" && EventManager.Instance != null)
            {
                string lower = playerInput.ToLower();
                int detected = 0;
                if (ContainsAny(lower, "rope", "arthur", "expedition", "k2", "mountain", "leader", "emergency", "contact"))
                    detected = 1;
                else if (ContainsAny(lower, "money", "account", "offshore", "sarah", "lily", "payment", "fund"))
                    detected = 2;
                else if (ContainsAny(lower, "guitar", "music", "playing", "song", "instrument"))
                    detected = 3;

                if (detected > 0)
                    EventManager.Instance.activeSecurityQuestion = detected;
            }

            // After the first exchange with David, mark resistance as used so he delivers the full truth on the second push
            if (currentCharacter == "david" && GetTurnCount("david") >= 2
                && EventManager.Instance != null)
            {
                int aq = EventManager.Instance.activeSecurityQuestion;
                if (!EventManager.Instance.HasDavidResisted(aq))
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
            int activeQuestion = EventManager.Instance != null ? EventManager.Instance.activeSecurityQuestion : 0;
            bool davidResisted = EventManager.Instance != null && EventManager.Instance.HasDavidResisted(activeQuestion);

            string prompt = currentCharacter == "david"
                ? CharacterPrompts.GetDavidPrompt(memories ?? new List<string>(), activeQuestion, davidResisted)
                : CharacterPrompts.GetMarthaPrompt(memories ?? new List<string>());

            // Only reassign if the prompt has actually changed — avoids invalidating the KV cache
            // on exchanges where the memory state is identical (speeds up subsequent turns).
            if (ActiveCharacter.systemPrompt != prompt)
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
            "This is exchange #", "Vary your tone", "Do not repeat",
            "<role>", "<voice>", "<rules>", "<context>", "<secret>", "<aware>", "<turn_count>",
            "(As Martha)", "(As David)", "(Note:", "(Martha", "(David"
        };

        private string StripScriptArtifacts(string response)
        {
            var ignoreCase = System.Text.RegularExpressions.RegexOptions.IgnoreCase;

            response = System.Text.RegularExpressions.Regex.Replace(
                response, @"^\s*\[?Martha\]?\s*:", "", ignoreCase);
            response = System.Text.RegularExpressions.Regex.Replace(
                response, @"^\s*\[?David\]?\s*:", "", ignoreCase);

            // Strip third-person NPC self-narration: "Martha leans...", "David pauses..." etc.
            response = System.Text.RegularExpressions.Regex.Replace(
                response, @"\bMartha\s+\w+s\b[^.!?]*[.!?]?", "", ignoreCase);
            response = System.Text.RegularExpressions.Regex.Replace(
                response, @"\bDavid\s+\w+s\b[^.!?]*[.!?]?", "", ignoreCase);

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
            string lowerInput  = input.ToLower();
            int activeQuestion = EventManager.Instance != null ? EventManager.Instance.activeSecurityQuestion : 0;
            bool davidResisted = EventManager.Instance != null && EventManager.Instance.HasDavidResisted(activeQuestion);

            if (character == "david")
            {
                if (ContainsAny(lowerInput, "rope", "arthur", "expedition", "k2", "mountain", "leader", "who was"))
                {
                    if (!davidResisted)
                        return "You really want to open that box? Right now? Today of all days?";
                    return "His name was Arthur. And you know what you did. I was on the radio. I heard him screaming.";
                }
                if (ContainsAny(lowerInput, "money", "account", "investment", "offshore", "fund"))
                {
                    if (!davidResisted)
                        return "I was hoping you wouldn't ask me about this. Are you sure? This one is going to change things.";
                    return "Sarah. The child support. Twenty-five years. Her name is Lily.";
                }
                if (ContainsAny(lowerInput, "guitar", "anniversary", "song", "music", "playing"))
                    return "The guitar? Honestly, I have no idea. You just stopped one day. I asked about it once and you changed the subject.";
                if (ContainsAny(lowerInput, "help", "advice", "decision", "document", "sign"))
                    return "I can't tell you what to do. But I know you — you've never been one to run from a hard thing. Not until now.";
                return "I'm here, pal. Whatever you need to say.";
            }

            // Martha stubs
            if (ContainsAny(lowerInput, "crack", "broken", "smash", "why is it", "neck", "why did you"))
                return "You came home drunk. You had that look. I sat on the floor until morning, picking up pieces of the neck.";

            if (ContainsAny(lowerInput, "guitar", "anniversary", "song", "playing", "music"))
                return "Our tenth anniversary. You stayed up all night writing me a song. The kitchen at sunrise, still in your dress shirt. I've never forgotten a single note.";

            if (ContainsAny(lowerInput, "mountain", "rope", "expedition", "k2", "arthur", "leader"))
                return "The storm was terrible. The rope gave way. That is all I know. David was on the radio that day — he might remember more than I do.";

            if (ContainsAny(lowerInput, "money", "account", "investment", "offshore"))
                return "I've had my suspicions for years. Never had the heart to push. David always understood your business better than I did.";

            if (ContainsAny(lowerInput, "photo", "wedding", "children", "child", "family", "baby"))
                return "Just the two of us. After everything we went through trying to change that... I told myself it was enough. Some days I even believed it.";

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
