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
        /// Build a context string from triggered memories for injection into the LLM prompt.
        /// </summary>
        public string BuildMemoryContext(List<string> triggeredMemoryIds, string character)
        {
            if (triggeredMemoryIds == null || triggeredMemoryIds.Count == 0)
                return "";

            var lines = new List<string>();
            lines.Add("\n[MEMORY CONTEXT - These memories have been explored today:]");

            foreach (string id in triggeredMemoryIds)
            {
                if (memoryLookup.TryGetValue(id, out MemoryData data))
                {
                    string contextText = character == "david" ? data.davidContext : data.marthaContext;
                    if (!string.IsNullOrEmpty(contextText))
                    {
                        lines.Add($"- {data.objectName}: {contextText}");
                    }
                }
            }

            return string.Join("\n", lines);
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
