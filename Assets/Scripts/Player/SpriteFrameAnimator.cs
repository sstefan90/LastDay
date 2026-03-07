using UnityEngine;

namespace LastDay.Player
{
    /// <summary>
    /// Lightweight sprite-frame animation player for sliced sprite sheets.
    /// </summary>
    public class SpriteFrameAnimator : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private SpriteRenderer spriteRenderer;

        [Header("Frames")]
        [SerializeField] private Sprite[] frames;
        [SerializeField] private float framesPerSecond = 8f;
        [SerializeField] private bool playOnEnable = true;

        private int currentFrameIndex;
        private float frameTimer;

        public void Configure(SpriteRenderer targetRenderer, Sprite[] animationFrames, float fps)
        {
            spriteRenderer = targetRenderer;
            frames = animationFrames;
            framesPerSecond = fps;
            currentFrameIndex = 0;
            frameTimer = 0f;
            ApplyFrame();
        }

        private void Awake()
        {
            if (spriteRenderer == null)
                spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        private void OnEnable()
        {
            if (!playOnEnable) return;
            currentFrameIndex = 0;
            frameTimer = 0f;
            ApplyFrame();
        }

        private void Update()
        {
            if (!playOnEnable || frames == null || frames.Length == 0 || spriteRenderer == null)
                return;

            if (framesPerSecond <= 0f)
                return;

            frameTimer += Time.deltaTime;
            float frameDuration = 1f / framesPerSecond;

            while (frameTimer >= frameDuration)
            {
                frameTimer -= frameDuration;
                currentFrameIndex = (currentFrameIndex + 1) % frames.Length;
                ApplyFrame();
            }
        }

        private void ApplyFrame()
        {
            if (spriteRenderer == null || frames == null || frames.Length == 0)
                return;

            currentFrameIndex = Mathf.Clamp(currentFrameIndex, 0, frames.Length - 1);
            spriteRenderer.sprite = frames[currentFrameIndex];
        }
    }
}
