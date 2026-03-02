using UnityEngine;

namespace LastDay.Player
{
    /// <summary>
    /// Modular idle animation for any standing character.
    /// Each effect (breathing, sway, tremor) is independently toggled
    /// and configured in the Inspector — reuse on both Robert and Martha.
    ///
    /// Robert preset  : breathing ✓  sway ✓  tremor ✓
    /// NPC preset     : breathing ✓  sway ✓  tremor ✗
    /// </summary>
    public class CharacterIdleMovement : MonoBehaviour
    {
        [Header("Sprite Root")]
        [Tooltip("Transform to animate. Defaults to this GameObject if left empty.")]
        [SerializeField] private Transform spriteRoot;

        [Header("Breathing")]
        [SerializeField] private BreathingConfig breathing = BreathingConfig.Default;

        [Header("Sway")]
        [SerializeField] private SwayConfig sway = SwayConfig.Default;

        [Header("Tremor")]
        [SerializeField] private TremorConfig tremor = TremorConfig.Default;

        // ── Runtime state ─────────────────────────────────────────────────

        private Vector3 baseScale;
        private Vector3 basePosition;

        // Tremor bookkeeping
        private float nextTremorTime;
        private float tremorEndTime;
        private bool isTremoring;

        // ── Unity lifecycle ────────────────────────────────────────────────

        void Start()
        {
            if (spriteRoot == null)
                spriteRoot = transform;

            baseScale    = spriteRoot.localScale;
            basePosition = spriteRoot.localPosition;

            ScheduleNextTremor();
        }

        void Update()
        {
            if (spriteRoot == null) return;

            Vector3 scale = baseScale;
            Vector3 pos   = basePosition;

            if (breathing.enabled) ApplyBreathing(ref scale, ref pos);
            if (sway.enabled)      ApplySway(ref pos);
            if (tremor.enabled)    ApplyTremor(ref pos);

            spriteRoot.localScale    = scale;
            spriteRoot.localPosition = pos;
        }

        // ── Effects ────────────────────────────────────────────────────────

        private void ApplyBreathing(ref Vector3 scale, ref Vector3 pos)
        {
            float phase  = Time.time * breathing.speed * Mathf.PI * 2f;
            float offset = Mathf.Sin(phase) * breathing.scaleAmount;

            scale.y = baseScale.y + offset;
            pos.y   = basePosition.y + offset * 0.5f;
        }

        private void ApplySway(ref Vector3 pos)
        {
            float phase = Time.time * sway.speed * Mathf.PI * 2f;
            pos.x = basePosition.x + Mathf.Sin(phase) * sway.amount;
        }

        private void ApplyTremor(ref Vector3 pos)
        {
            if (!isTremoring && Time.time > nextTremorTime)
            {
                isTremoring  = true;
                tremorEndTime = Time.time + tremor.duration;
            }

            if (isTremoring)
            {
                // Multi-frequency tremor for an organic feel
                float t = Mathf.Sin(Time.time * 40f) * tremor.amount
                        + Mathf.Sin(Time.time * 55f) * tremor.amount * 0.5f;
                pos.x += t;

                if (Time.time > tremorEndTime)
                {
                    isTremoring = false;
                    ScheduleNextTremor();
                }
            }
        }

        private void ScheduleNextTremor()
        {
            nextTremorTime = Time.time + Random.Range(tremor.intervalMin, tremor.intervalMax);
        }

        // ── Public API (called by PlayerController2D / NPCController) ──────

        /// <summary>Freeze idle effects while the character is walking.</summary>
        public void OnStartWalking()
        {
            enabled = false;
            if (spriteRoot != null)
            {
                spriteRoot.localScale    = baseScale;
                spriteRoot.localPosition = basePosition;
            }
        }

        /// <summary>Resume idle effects when the character comes to a stop.</summary>
        public void OnStopWalking()
        {
            enabled = true;
        }

        // ── Config structs ─────────────────────────────────────────────────

        [System.Serializable]
        public struct BreathingConfig
        {
            public bool enabled;
            [Range(0f, 0.05f)] public float scaleAmount;
            [Range(0.1f, 2f)]  public float speed;

            public static BreathingConfig Default => new BreathingConfig
            {
                enabled     = true,
                scaleAmount = 0.015f,
                speed       = 0.4f
            };
        }

        [System.Serializable]
        public struct SwayConfig
        {
            public bool enabled;
            [Range(0f, 0.02f)] public float amount;
            [Range(0.05f, 1f)] public float speed;

            public static SwayConfig Default => new SwayConfig
            {
                enabled = true,
                amount  = 0.003f,
                speed   = 0.2f
            };
        }

        [System.Serializable]
        public struct TremorConfig
        {
            public bool enabled;
            [Range(0f, 0.03f)]  public float amount;
            [Range(0.1f, 1f)]   public float duration;
            [Range(1f, 10f)]    public float intervalMin;
            [Range(2f, 20f)]    public float intervalMax;

            /// <summary>Robert's default — tremor enabled.</summary>
            public static TremorConfig Default => new TremorConfig
            {
                enabled     = true,
                amount      = 0.01f,
                duration    = 0.3f,
                intervalMin = 3f,
                intervalMax = 8f
            };

            /// <summary>NPC preset — no tremor.</summary>
            public static TremorConfig None => new TremorConfig
            {
                enabled     = false,
                amount      = 0f,
                duration    = 0.3f,
                intervalMin = 5f,
                intervalMax = 10f
            };
        }
    }
}
