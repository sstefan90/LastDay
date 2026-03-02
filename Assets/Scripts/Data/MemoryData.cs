using UnityEngine;

namespace LastDay.Data
{
    /// <summary>
    /// ScriptableObject holding data for a single memory object.
    /// Create instances via Assets > Create > LastDay > Memory Data.
    /// </summary>
    [CreateAssetMenu(fileName = "NewMemory", menuName = "LastDay/Memory Data")]
    public class MemoryData : ScriptableObject
    {
        public string memoryId;
        public string objectName;
        public Sprite objectSprite;
        public Sprite glowSprite;

        [TextArea(3, 10)]
        public string shortDescription;

        [TextArea(5, 20)]
        public string fullStory;

        [TextArea(3, 10)]
        public string marthaContext;

        [TextArea(3, 10)]
        public string davidContext;
    }
}
