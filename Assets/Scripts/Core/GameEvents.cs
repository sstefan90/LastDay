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
    }
}
