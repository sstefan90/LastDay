using System;

namespace LastDay.Core
{
    /// <summary>
    /// Centralized static event bus. Subscribe with += in OnEnable, unsubscribe with -= in OnDisable.
    /// </summary>
    public static class GameEvents
    {
        public static event Action<string> OnMemoryTriggered;
        public static event Action OnDocumentUnlocked;
        public static event Action<string, string> OnDialogueReceived;
        public static event Action OnPhoneRing;
        public static event Action<GameState, GameState> OnGameStateChanged;
        public static event Action<string> OnObjectInteracted;
        public static event Action<string> OnGazeComplete;
        public static event Action<bool> OnGameEnded;

        // Security question progression events
        /// <summary>Fired when a single security question is correctly answered. Passes the 0-based question index.</summary>
        public static event Action<int> OnSecurityQuestionAnswered;
        /// <summary>Fired when all three security questions are answered. Triggers Martha shutdown and document unlock.</summary>
        public static event Action OnAllQuestionsAnswered;
        /// <summary>Fired when Q3 (guitar) becomes active — signals that Martha's breakdown is possible.</summary>
        public static event Action OnMarthaBreakdownReady;

        public static void TriggerMemory(string memoryId) =>
            OnMemoryTriggered?.Invoke(memoryId);

        public static void UnlockDocument() =>
            OnDocumentUnlocked?.Invoke();

        public static void ReceiveDialogue(string character, string text) =>
            OnDialogueReceived?.Invoke(character, text);

        public static void RingPhone() =>
            OnPhoneRing?.Invoke();

        public static void ChangeGameState(GameState oldState, GameState newState) =>
            OnGameStateChanged?.Invoke(oldState, newState);

        public static void InteractWithObject(string objectId) =>
            OnObjectInteracted?.Invoke(objectId);

        public static void CompleteGaze(string objectId) =>
            OnGazeComplete?.Invoke(objectId);

        public static void EndGame(bool signed) =>
            OnGameEnded?.Invoke(signed);

        public static void SecurityQuestionAnswered(int questionIndex) =>
            OnSecurityQuestionAnswered?.Invoke(questionIndex);

        public static void AllQuestionsAnswered() =>
            OnAllQuestionsAnswered?.Invoke();

        public static void MarthaBreakdownReady() =>
            OnMarthaBreakdownReady?.Invoke();
    }
}
