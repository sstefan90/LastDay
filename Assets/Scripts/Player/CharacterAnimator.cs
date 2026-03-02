using UnityEngine;

namespace LastDay.Player
{
    /// <summary>
    /// Manages walk/idle blend tree parameters for a character sprite.
    /// Expects an Animator with parameters: IsWalking (bool), DirectionX (float), DirectionY (float).
    /// Falls back to sprite flipping when no AnimatorController is assigned.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class CharacterAnimator : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private SpriteRenderer spriteRenderer;

        [Header("Settings")]
        [SerializeField] private bool flipSpriteForLeftRight = true;

        private Animator animator;
        private bool hasController;
        private Vector2 currentDirection = Vector2.down;

        private static readonly int IsWalking = Animator.StringToHash("IsWalking");
        private static readonly int DirectionX = Animator.StringToHash("DirectionX");
        private static readonly int DirectionY = Animator.StringToHash("DirectionY");

        public Vector2 CurrentDirection => currentDirection;

        void Awake()
        {
            animator = GetComponent<Animator>();
            if (spriteRenderer == null)
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        void Start()
        {
            hasController = animator != null
                && animator.runtimeAnimatorController != null;
        }

        public void SetWalking(bool walking)
        {
            if (hasController)
                animator.SetBool(IsWalking, walking);
        }

        public void SetDirection(Vector2 direction)
        {
            if (direction.sqrMagnitude < 0.01f) return;

            if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
            {
                if (hasController)
                {
                    animator.SetFloat(DirectionX, Mathf.Sign(direction.x));
                    animator.SetFloat(DirectionY, 0);
                }
                currentDirection = new Vector2(Mathf.Sign(direction.x), 0);

                if (flipSpriteForLeftRight && spriteRenderer != null)
                    spriteRenderer.flipX = direction.x < 0;
            }
            else
            {
                if (hasController)
                {
                    animator.SetFloat(DirectionX, 0);
                    animator.SetFloat(DirectionY, Mathf.Sign(direction.y));
                }
                currentDirection = new Vector2(0, Mathf.Sign(direction.y));
            }
        }

        public void FacePosition(Vector2 targetPosition)
        {
            Vector2 dir = (targetPosition - (Vector2)transform.position).normalized;
            SetDirection(dir);
        }
    }
}
