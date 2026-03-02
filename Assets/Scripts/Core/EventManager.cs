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

        [Header("Settings")]
        [SerializeField] private int memoriesRequiredForDocument = 2;
        [SerializeField] private int memoriesRequiredForPhone = 2;

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

                CheckDocumentUnlock();
                CheckPhoneTrigger();
            }

            GameEvents.CompleteGaze(evt.objectId);
        }

        private void HandleInteract(GameEvent evt)
        {
            GameEvents.InteractWithObject(evt.objectId);
        }

        private void CheckDocumentUnlock()
        {
            if (!documentUnlocked && triggeredMemories.Count >= memoriesRequiredForDocument)
            {
                documentUnlocked = true;
                GameEvents.UnlockDocument();
                Debug.Log("[Event] Document unlocked!");
            }
        }

        private void CheckPhoneTrigger()
        {
            if (!phoneHasRung && triggeredMemories.Count >= memoriesRequiredForPhone)
            {
                phoneHasRung = true;
                GameEvents.RingPhone();
                Debug.Log("[Event] Phone triggered!");
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
