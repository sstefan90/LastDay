using UnityEngine;
using System.Collections.Generic;
using LastDay.Utilities;

namespace LastDay.Core
{
    [System.Serializable]
    public struct GameEvent
    {
        public string eventType;
        public string objectId;
        public string memoryId;
        public float timestamp;

        public GameEvent(string eventType, string objectId, string memoryId)
        {
            this.eventType = eventType;
            this.objectId = objectId;
            this.memoryId = memoryId;
            this.timestamp = Time.time;
        }
    }

    public class EventManager : Singleton<EventManager>
    {
        [Header("Game Progress")]
        public List<string> triggeredMemories = new List<string>();
        public bool hasAskedForHelp;
        public bool documentUnlocked;
        public bool phoneHasRung;

        [Header("Security Questions")]
        // 0 = no question active yet, 1-3 = which mystery the player is currently on
        public int activeSecurityQuestion = 0;

        [Header("David Resistance")]
        // Tracks whether David has already pushed back on each mystery (keyed by activeQuestion 1-3)
        private HashSet<int> davidResistanceUsed = new HashSet<int>();

        public bool HasDavidResisted(int questionIndex) => davidResistanceUsed.Contains(questionIndex);
        public void MarkDavidResisted(int questionIndex) => davidResistanceUsed.Add(questionIndex);

        private List<GameEvent> eventHistory = new List<GameEvent>();

        protected override void Awake()
        {
            base.Awake();
        }

        private bool subscribedToStateChanges;

        void Start()
        {
            SubscribeToStateChanges();
        }

        void OnEnable()
        {
            SubscribeToStateChanges();
        }

        void OnDisable()
        {
            if (subscribedToStateChanges && GameStateMachine.Instance != null)
            {
                GameStateMachine.Instance.OnStateChanged -= OnGameStateChanged;
                subscribedToStateChanges = false;
            }
        }

        private void SubscribeToStateChanges()
        {
            if (subscribedToStateChanges) return;
            if (GameStateMachine.Instance == null) return;

            GameStateMachine.Instance.OnStateChanged += OnGameStateChanged;
            subscribedToStateChanges = true;
        }

        private void OnGameStateChanged(GameState oldState, GameState newState)
        {
            GameEvents.ChangeGameState(oldState, newState);
        }

        public void PublishEvent(GameEvent evt)
        {
            eventHistory.Add(evt);
            Debug.Log($"[Event] {evt.eventType}: obj={evt.objectId}, memory={evt.memoryId}");

            switch (evt.eventType)
            {
                case "gaze_complete":
                    HandleGazeComplete(evt);
                    break;
                case "interact":
                    HandleInteract(evt);
                    break;
            }
        }

        private void HandleGazeComplete(GameEvent evt)
        {
            if (!string.IsNullOrEmpty(evt.memoryId) && !triggeredMemories.Contains(evt.memoryId))
            {
                triggeredMemories.Add(evt.memoryId);
                GameEvents.TriggerMemory(evt.memoryId);
                Debug.Log($"[Event] Memory triggered: {evt.memoryId} (total: {triggeredMemories.Count})");
            }

            GameEvents.CompleteGaze(evt.objectId);
        }

        private void HandleInteract(GameEvent evt)
        {
            GameEvents.InteractWithObject(evt.objectId);
        }

        // Called by ComputerInteraction when all three security questions are answered.
        public void OnAllSecurityQuestionsAnswered()
        {
            documentUnlocked = true;
            GameEvents.UnlockDocument();
            GameEvents.AllQuestionsAnswered();
            Debug.Log("[Event] All security questions answered — document unlocked.");
        }

        // Called by ComputerInteraction when a question is first SHOWN (not when answered).
        // Sets activeSecurityQuestion so Martha/David LLM prompts shift immediately, and rings
        // the phone as soon as Q1 is displayed — the player needs David to find the truth.
        public void OnSecurityQuestionStarted(int questionIndex)
        {
            int newActive = questionIndex + 1;
            if (newActive <= activeSecurityQuestion) return; // already at or past this question

            activeSecurityQuestion = newActive;
            Debug.Log($"[Event] Security question {questionIndex} started. Active question now: {activeSecurityQuestion}");

            CheckPhoneTrigger();
        }

        // Called by ComputerInteraction each time a question is correctly answered.
        public void OnSecurityQuestionAnswered(int questionIndex)
        {
            GameEvents.SecurityQuestionAnswered(questionIndex);
            Debug.Log($"[Event] Security question {questionIndex} answered.");
        }

        private void CheckPhoneTrigger()
        {
            if (!phoneHasRung && activeSecurityQuestion >= 1)
            {
                phoneHasRung = true;
                GameEvents.RingPhone();
                Debug.Log("[Event] Phone triggered — player needs David.");
            }
        }

        public List<GameEvent> GetRecentEvents(int count = 10)
        {
            int start = Mathf.Max(0, eventHistory.Count - count);
            return eventHistory.GetRange(start, eventHistory.Count - start);
        }

        public void TriggerNPCComment(string memoryId)
        {
            Debug.Log($"[Event] NPC comment triggered for memory: {memoryId}");
        }
    }
}
