using UnityEngine;

namespace LastDay.Core
{
    /// <summary>
    /// Scales a SpriteRenderer so the full sprite is visible in the camera view
    /// without stretching. Preserves aspect ratio and fits within the view.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class BackgroundFitCamera : MonoBehaviour
    {
        [Tooltip("Camera to fit against. If null, uses UnityEngine.Camera.main.")]
        [SerializeField] private UnityEngine.Camera targetCamera;

        private void Start()
        {
            FitToCamera();
        }

        /// <summary>Recompute scale to fit the camera view. Call after camera rect or ortho size changes.</summary>
        public void FitToCamera()
        {
            var cam = targetCamera != null ? targetCamera : UnityEngine.Camera.main;
            var sr = GetComponent<SpriteRenderer>();
            if (cam == null || sr == null || sr.sprite == null) return;

            float viewHeight = 2f * cam.orthographicSize;
            float viewWidth = viewHeight * cam.aspect;

            Sprite sp = sr.sprite;
            float spriteWorldW = sp.rect.width / sp.pixelsPerUnit;
            float spriteWorldH = sp.rect.height / sp.pixelsPerUnit;

            float scaleX = viewWidth / spriteWorldW;
            float scaleY = viewHeight / spriteWorldH;
            float scale = Mathf.Min(scaleX, scaleY);

            transform.localScale = new Vector3(scale, scale, 1f);
        }
    }
}
