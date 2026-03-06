using UnityEngine;
using LastDay.Core;

namespace LastDay.Interaction
{
    /// <summary>
    /// The euthanasia document. Locked until enough memories are triggered.
    /// Opens the decision panel (Sign / Tear) when unlocked.
    /// </summary>
    public class DocumentInteraction : InteractableObject2D
    {
        [Header("Document Settings")]
        [SerializeField] private string lockedMessage = "Not yet... I'm not ready to look at that.";

        private bool isUnlocked;

        void OnEnable()
        {
            GameEvents.OnDocumentUnlocked += HandleDocumentUnlocked;
        }

        void OnDisable()
        {
            GameEvents.OnDocumentUnlocked -= HandleDocumentUnlocked;
        }

        private void HandleDocumentUnlocked()
        {
            isUnlocked = true;
            Debug.Log("[Document] Document is now unlocked.");
        }

        public override void OnInteract()
        {
            if (!isUnlocked)
            {
                ShowLockedMessage();
                return;
            }

            if (EventManager.Instance != null)
            {
                EventManager.Instance.PublishEvent(new GameEvent(
                    "interact", ObjectId, MemoryId
                ));
            }

            GameStateMachine.Instance.ChangeState(GameState.Decision);

            var decisionUI = FindObjectOfType<LastDay.UI.DecisionUI>();
            if (decisionUI != null)
                decisionUI.Show();

            Debug.Log("[Document] Decision panel opened.");
        }

        private void ShowLockedMessage()
        {
            Debug.Log($"[Document] Locked: {lockedMessage}");

            if (LastDay.UI.DialogueSession.Current != null)
                LastDay.UI.DialogueSession.Current.ShowMonologue(lockedMessage);
        }
    }
}
