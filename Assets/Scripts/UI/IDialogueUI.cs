namespace LastDay.UI
{
    /// <summary>
    /// Interface for dialogue UI implementations (standard panel or interview-style floating).
    /// Allows NPCController and interactables to work with either variant.
    /// </summary>
    public interface IDialogueUI
    {
        void OpenForNPC(string npcId, string npcName);
        void OpenForObject(string objectId, string memoryId, string displayName);
        void OpenForPhone();
        void ShowMonologue(string text);
        void Close();
    }
}
