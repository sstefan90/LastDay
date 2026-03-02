using UnityEngine;
using System.Collections;
using LastDay.Utilities;

namespace LastDay.Camera
{
    /// <summary>
    /// 2D orthographic camera controller.
    /// Supports smooth zoom to objects (e.g. document interaction) and reset.
    /// </summary>
    public class CameraController2D : Singleton<CameraController2D>
    {
        [Header("Settings")]
        [SerializeField] private float defaultSize = 5f;
        [SerializeField] private float zoomedSize = 2f;
        [SerializeField] private float zoomDuration = 0.5f;

        private UnityEngine.Camera cam;
        private Vector3 defaultPosition;
        private Coroutine activeTransition;

        protected override void Awake()
        {
            base.Awake();
            cam = GetComponent<UnityEngine.Camera>();
            if (cam == null)
                cam = UnityEngine.Camera.main;
            defaultPosition = transform.position;
        }

        public void ZoomToTarget(Transform target)
        {
            if (cam == null) return;

            Vector3 targetPos = new Vector3(target.position.x, target.position.y, defaultPosition.z);

            if (activeTransition != null)
                StopCoroutine(activeTransition);

            activeTransition = StartCoroutine(TransitionRoutine(targetPos, zoomedSize));
        }

        public void ResetZoom()
        {
            if (cam == null) return;

            if (activeTransition != null)
                StopCoroutine(activeTransition);

            activeTransition = StartCoroutine(TransitionRoutine(defaultPosition, defaultSize));
        }

        private IEnumerator TransitionRoutine(Vector3 targetPos, float targetSize)
        {
            Vector3 startPos = transform.position;
            float startSize = cam.orthographicSize;
            float elapsed = 0f;

            while (elapsed < zoomDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / zoomDuration);
                transform.position = Vector3.Lerp(startPos, targetPos, t);
                cam.orthographicSize = Mathf.Lerp(startSize, targetSize, t);
                yield return null;
            }

            transform.position = targetPos;
            cam.orthographicSize = targetSize;
            activeTransition = null;
        }
    }
}
