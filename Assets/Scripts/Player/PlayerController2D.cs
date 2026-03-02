using UnityEngine;
using System.Collections.Generic;
using LastDay.Core;
using LastDay.Pathfinding;
using LastDay.Interaction;
using LastDay.NPC;

namespace LastDay.Player
{
    public class PlayerController2D : MonoBehaviour
    {
        public static PlayerController2D Instance { get; private set; }

        [Header("Movement")]
        [SerializeField] private float moveSpeed = 2f;
        [SerializeField] private float pathNodeReachDistance = 0.1f;

        [Header("Components")]
        public CharacterAnimator characterAnimator;
        public SubtleIdleMovement idleMovement;

        [Header("Pathfinding")]
        [SerializeField] private SimplePathfinder pathfinder;

        private List<Vector2> currentPath;
        private int currentPathIndex;
        private bool isMoving;
        private System.Action onReachDestination;

        public bool IsMoving => isMoving;

        void Awake()
        {
            Instance = this;
        }

        void Update()
        {
            if (!isMoving) return;

            if (currentPath == null || currentPathIndex >= currentPath.Count)
            {
                StopMoving();
                return;
            }

            Vector2 targetPos = currentPath[currentPathIndex];
            Vector2 currentPos = transform.position;
            Vector2 direction = (targetPos - currentPos).normalized;

            transform.position += (Vector3)(direction * moveSpeed * Time.deltaTime);

            if (characterAnimator != null)
                characterAnimator.SetDirection(direction);

            if (Vector2.Distance(currentPos, targetPos) < pathNodeReachDistance)
            {
                currentPathIndex++;
                if (currentPathIndex >= currentPath.Count)
                    StopMoving();
            }
        }

        public void MoveTo(Vector2 destination, System.Action onComplete = null)
        {
            if (GameStateMachine.Instance != null && !GameStateMachine.Instance.CanPlayerMove)
                return;

            if (pathfinder != null)
            {
                currentPath = pathfinder.FindPath(transform.position, destination);
            }
            else
            {
                // Fallback: direct movement if no pathfinder
                currentPath = new List<Vector2> { destination };
            }

            if (currentPath == null || currentPath.Count == 0)
            {
                Debug.Log("[Player] No path found to destination");
                return;
            }

            currentPathIndex = 0;
            isMoving = true;
            onReachDestination = onComplete;

            if (idleMovement != null)
                idleMovement.OnStartWalking();

            if (characterAnimator != null)
                characterAnimator.SetWalking(true);
        }

        public void MoveToAndInteract(InteractableObject2D target)
        {
            Vector2 interactionPoint = CalculateInteractionPoint(target.transform.position);

            MoveTo(interactionPoint, () =>
            {
                if (characterAnimator != null)
                    characterAnimator.FacePosition(target.transform.position);

                target.OnInteract();
            });
        }

        public void MoveToAndTalk(NPCController npc)
        {
            Vector2 interactionPoint = CalculateInteractionPoint(npc.transform.position);

            MoveTo(interactionPoint, () =>
            {
                if (characterAnimator != null)
                    characterAnimator.FacePosition(npc.transform.position);

                npc.OnPlayerInteract();
            });
        }

        private Vector2 CalculateInteractionPoint(Vector2 objectPos)
        {
            Vector2 direction = ((Vector2)transform.position - objectPos).normalized;
            if (direction.sqrMagnitude < 0.01f) direction = Vector2.right;
            return objectPos + direction * 0.75f;
        }

        public void StopMoving()
        {
            isMoving = false;
            currentPath = null;

            if (idleMovement != null)
                idleMovement.OnStopWalking();

            if (characterAnimator != null)
                characterAnimator.SetWalking(false);

            onReachDestination?.Invoke();
            onReachDestination = null;
        }

        public void ForceStop()
        {
            onReachDestination = null;
            StopMoving();
        }
    }
}
