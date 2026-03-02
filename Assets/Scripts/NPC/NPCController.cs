using UnityEngine;
using LastDay.Player;
using LastDay.UI;

namespace LastDay.NPC
{
    /// <summary>
    /// Controls NPC idle behavior, facing logic, and player interaction.
    /// Martha stands in place, faces the player when nearby, and can be clicked to start dialogue.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(Collider2D))]
    public class NPCController : MonoBehaviour
    {
        [Header("NPC Identity")]
        [SerializeField] private string npcId = "martha";
        [SerializeField] private string displayName = "Martha";

        [Header("Facing")]
        [SerializeField] private bool facePlayer = true;
        [SerializeField] private float facePlayerDistance = 3f;

        [Header("Components")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private CharacterAnimator characterAnimator;
        [SerializeField] private SubtleIdleMovement idleMovement;

        public string NpcId => npcId;
        public string DisplayName => displayName;

        void Awake()
        {
            if (spriteRenderer == null)
                spriteRenderer = GetComponent<SpriteRenderer>();
        }

        void Update()
        {
            if (!facePlayer) return;

            var player = PlayerController2D.Instance;
            if (player == null) return;

            float distance = Vector2.Distance(transform.position, player.transform.position);
            if (distance <= facePlayerDistance && characterAnimator != null)
            {
                characterAnimator.FacePosition(player.transform.position);
            }
        }

        /// <summary>
        /// Called when the player walks up to this NPC and interacts.
        /// Opens the dialogue UI for conversation.
        /// </summary>
        public void OnPlayerInteract()
        {
            if (characterAnimator != null)
            {
                var player = PlayerController2D.Instance;
                if (player != null)
                    characterAnimator.FacePosition(player.transform.position);
            }

            if (DialogueUI.Instance != null)
                DialogueUI.Instance.OpenForNPC(npcId, displayName);
            else
                Debug.LogWarning($"[NPC] DialogueUI.Instance is null, cannot open dialogue for {displayName}");
        }
    }
}
