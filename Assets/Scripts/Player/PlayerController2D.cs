using UnityEngine;
using LastDay.Interaction;
using LastDay.NPC;

namespace LastDay.Player
{
    /// <summary>
    /// Static scene: no movement or pathfinding. Provides immediate point-and-click
    /// interaction (interactables and NPC dialogue) when called from ClickToMoveHandler.
    /// </summary>
    public class PlayerController2D : MonoBehaviour
    {
        public static PlayerController2D Instance { get; private set; }

        [Header("Components (optional, for static characters)")]
        public CharacterAnimator characterAnimator;
        public CharacterIdleMovement idleMovement;

        public bool IsMoving => false;

        void Awake()
        {
            Instance = this;
        }

        /// <summary>Immediately interact with the object (no movement).</summary>
        public void MoveToAndInteract(InteractableObject2D target)
        {
            if (target == null) return;
            if (characterAnimator != null)
                characterAnimator.FacePosition(target.transform.position);
            target.OnInteract();
        }

        /// <summary>Immediately open dialogue with the NPC (no movement).</summary>
        public void MoveToAndTalk(NPCController npc)
        {
            if (npc == null) return;
            if (characterAnimator != null)
                characterAnimator.FacePosition(npc.transform.position);
            npc.OnPlayerInteract();
        }

        /// <summary>No-op in static scene (no pathfinding).</summary>
        public void MoveTo(Vector2 destination, System.Action onComplete = null)
        {
            onComplete?.Invoke();
        }

        public void StopMoving() { }
        public void ForceStop() { }
    }
}
