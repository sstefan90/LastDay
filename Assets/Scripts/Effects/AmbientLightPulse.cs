using UnityEngine;

namespace LastDay.Effects
{
    /// <summary>
    /// Simulates a warm ambient light source (e.g. afternoon window light) by
    /// procedurally generating a soft radial gradient sprite at runtime and gently
    /// pulsing its alpha — no art asset required.
    ///
    /// Suggested placement: child of the Environment GameObject, positioned near
    /// the window area in the scene. The gradient covers a configurable world-space
    /// area. Because it is a SpriteRenderer in world space it renders below the
    /// Screen Space - Overlay Canvas automatically.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class AmbientLightPulse : MonoBehaviour
    {
        [Header("Color & Opacity")]
        [SerializeField] private Color warmColor       = new Color(1f, 0.82f, 0.45f, 1f);
        [SerializeField, Range(0f, 0.3f)] private float minAlpha = 0.04f;
        [SerializeField, Range(0f, 0.3f)] private float maxAlpha = 0.10f;

        [Header("Animation")]
        [SerializeField, Range(0.02f, 1f)] private float pulseSpeed       = 0.15f;
        [SerializeField, Range(0f, 6.28f)] private float phaseOffset      = 0f;

        [Header("World Scale")]
        [SerializeField] private Vector2 overlayWorldSize = new Vector2(8f, 6f);

        [Header("Texture Resolution")]
        [SerializeField, Range(32, 256)] private int textureSize = 128;

        [Header("Sorting")]
        [SerializeField] private string sortingLayerName = "Default";
        [SerializeField] private int    sortingOrder     = 2;

        private SpriteRenderer _sr;
        private float _baseAlpha;

        void Awake()
        {
            _sr = GetComponent<SpriteRenderer>();
            _sr.sortingLayerName = sortingLayerName;
            _sr.sortingOrder     = sortingOrder;
            BuildGradientSprite();
        }

        void Start()
        {
            // Randomise phase so multiple instances don't pulse in sync
            phaseOffset = Random.Range(0f, Mathf.PI * 2f);
        }

        void Update()
        {
            float t     = Mathf.Sin(Time.time * pulseSpeed * Mathf.PI * 2f + phaseOffset) * 0.5f + 0.5f;
            float alpha = Mathf.Lerp(minAlpha, maxAlpha, t);

            Color c = _sr.color;
            c.a      = alpha;
            _sr.color = c;
        }

        private void BuildGradientSprite()
        {
            int res = textureSize;
            var tex = new Texture2D(res, res, TextureFormat.RGBA32, false);
            tex.wrapMode   = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;

            float cx = res * 0.5f;
            float cy = res * 0.5f;
            float r  = res * 0.5f;

            for (int y = 0; y < res; y++)
            {
                for (int x = 0; x < res; x++)
                {
                    float dist    = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                    float t       = Mathf.Clamp01(dist / r);
                    // Soft falloff: 1 at centre, 0 at edge (smoothstep-ish)
                    float alpha   = Mathf.Pow(1f - t, 2.2f);
                    tex.SetPixel(x, y, new Color(warmColor.r, warmColor.g, warmColor.b, alpha));
                }
            }
            tex.Apply();

            var rect   = new Rect(0, 0, res, res);
            var pivot  = new Vector2(0.5f, 0.5f);
            // pixels-per-unit chosen so the sprite matches overlayWorldSize
            float ppu  = res / Mathf.Max(overlayWorldSize.x, overlayWorldSize.y);
            var sprite = Sprite.Create(tex, rect, pivot, ppu);

            _sr.sprite = sprite;
            // Start fully transparent; Update() will animate from there
            _sr.color  = new Color(warmColor.r, warmColor.g, warmColor.b, 0f);

            // Match the exact aspect ratio requested
            float scaleX = overlayWorldSize.x / overlayWorldSize.y;
            transform.localScale = new Vector3(scaleX, 1f, 1f);
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            if (_sr == null) _sr = GetComponent<SpriteRenderer>();
            if (_sr != null && Application.isPlaying) BuildGradientSprite();
        }
#endif
    }
}
