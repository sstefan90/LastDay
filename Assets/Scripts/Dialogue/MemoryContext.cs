using UnityEngine;
using System.Collections.Generic;
using LastDay.Data;

namespace LastDay.Dialogue
{
    /// <summary>
    /// Manages memory story data and builds context strings for the LLM.
    /// </summary>
    public class MemoryContext : MonoBehaviour
    {
        public static MemoryContext Instance { get; private set; }

        [Header("Memory Data")]
        [SerializeField] private List<MemoryData> allMemories = new List<MemoryData>();

        private Dictionary<string, MemoryData> memoryLookup = new Dictionary<string, MemoryData>();

        void Awake()
        {
            Instance = this;

            foreach (var memory in allMemories)
            {
                if (memory != null && !memoryLookup.ContainsKey(memory.memoryId))
                    memoryLookup[memory.memoryId] = memory;
            }
        }

        public MemoryData GetMemory(string memoryId)
        {
            memoryLookup.TryGetValue(memoryId, out MemoryData data);
            return data;
        }

        /// <summary>
        /// Returns memory IDs that have been triggered, for CharacterPrompts to embed
        /// as narrative context within the system prompt.
        /// NOTE: Do not inject the raw output of this into an LLM prompt — use
        /// CharacterPrompts.GetMarthaPrompt(triggeredMemoryIds) instead.
        /// </summary>
        public List<string> GetTriggeredMemoryIds(List<string> triggeredMemoryIds)
        {
            var valid = new List<string>();
            if (triggeredMemoryIds == null) return valid;
            foreach (string id in triggeredMemoryIds)
            {
                if (memoryLookup.ContainsKey(id))
                    valid.Add(id);
            }
            return valid;
        }

        /// <summary>
        /// Get the full story for a specific memory (for internal monologue or narration).
        /// </summary>
        public string GetFullStory(string memoryId)
        {
            if (memoryLookup.TryGetValue(memoryId, out MemoryData data))
                return data.fullStory;
            return null;
        }
    }
}
