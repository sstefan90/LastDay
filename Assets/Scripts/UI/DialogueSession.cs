namespace LastDay.UI
{
    /// <summary>
    /// Static accessor for the active dialogue UI (standard or interview-style).
    /// Set by DialogueUI or InterviewDialogueUI in Awake.
    /// </summary>
    public static class DialogueSession
    {
        public static IDialogueUI Current { get; set; }
    }
}
