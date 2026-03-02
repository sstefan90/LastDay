using UnityEngine;
using UnityEngine.EventSystems;
using LastDay.Core;

namespace LastDay.Interaction
{
    /// <summary>
    /// Base class for all clickable/hoverable objects in the scene.
    /// Handles gaze timer, highlight, and event publishing.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class InteractableObject2D : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [Header("Object Identity")]
        [SerializeField] private string objectId;
        [SerializeField] private string displayName;
        [SerializeField] private string memoryId;

        [Header("Visuals")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private SpriteRenderer highlightRenderer;
        [SerializeField] private Color highlightColor = new Color(1f, 1f, 0.8f, 0.3f);

        [Header("Gaze/Hover")]
        [SerializeField] private float hoverTimeToTrigger = 2f;
        private float currentHoverTime;
        private bool isHovering;
        private bool hasTriggeredGaze;

        [Header("Audio")]
        [SerializeField] private AudioClip hoverSound;
        [SerializeField] private AudioClip clickSound;

        public string ObjectId => objectId;
        public string DisplayName => displayName;
        public string MemoryId => memoryId;

        void Start()
        {
            if (highlightRenderer != null)
                highlightRenderer.enabled = false;
        }

        void Update()
        {
            if (!isHovering || hasTriggeredGaze) return;

            currentHoverTime += Time.deltaTime;

            if (highlightRenderer != null)
            {
                float progress = currentHoverTime / hoverTimeToTrigger;
                highlightRenderer.color = new Color(
                    highlightColor.r,
                    highlightColor.g,
                    highlightColor.b,
                    highlightColor.a * progress
                );
            }

            if (currentHoverTime >= hoverTimeToTrigger)
                OnGazeComplete();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (GameStateMachine.Instance != null && !GameStateMachine.Instance.CanInteract) return;

            isHovering = true;

            if (highlightRenderer != null)
                highlightRenderer.enabled = true;

            var prompt = FindObjectOfType<LastDay.UI.InteractionPrompt>();
            if (prompt != null)
                prompt.Show($"Click to examine {displayName}");

            if (hoverSound != null)
                AudioSource.PlayClipAtPoint(hoverSound, transform.position, 0.5f);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            isHovering = false;
            currentHoverTime = 0f;

            if (highlightRenderer != null)
                highlightRenderer.enabled = false;

            var prompt = FindObjectOfType<LastDay.UI.InteractionPrompt>();
            if (prompt != null)
                prompt.Hide();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (GameStateMachine.Instance != null && !GameStateMachine.Instance.CanInteract) return;

            if (clickSound != null)
                AudioSource.PlayClipAtPoint(clickSound, transform.position, 0.7f);

            OnInteract();
        }

        protected virtual void OnGazeComplete()
        {
            hasTriggeredGaze = true;

            if (EventManager.Instance != null)
            {
                EventManager.Instance.PublishEvent(new GameEvent(
                    "gaze_complete", objectId, memoryId
                ));
            }

            if (Random.value > 0.5f && EventManager.Instance != null)
                EventManager.Instance.TriggerNPCComment(memoryId);
        }

        public virtual void OnInteract()
        {
            if (EventManager.Instance != null)
            {
                EventManager.Instance.PublishEvent(new GameEvent(
                    "interact", objectId, memoryId
                ));
            }

            var dialogueUI = FindObjectOfType<LastDay.UI.DialogueUI>();
            if (dialogueUI != null)
            {
                dialogueUI.OpenForObject(objectId, memoryId, displayName);
            }
        }
    }
}
