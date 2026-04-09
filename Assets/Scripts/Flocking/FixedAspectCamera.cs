using UnityEngine;

namespace Flocking
{
    [RequireComponent(typeof(Camera))]
    public sealed class FixedAspectCamera : MonoBehaviour
    {
        [SerializeField] private float targetWidth = 16f;
        [SerializeField] private float targetHeight = 9f;

        private Camera cachedCamera;
        private int lastScreenWidth = -1;
        private int lastScreenHeight = -1;

        private void OnEnable()
        {
            cachedCamera = GetComponent<Camera>();
            ApplyViewport();
        }

        private void LateUpdate()
        {
            if (Screen.width != lastScreenWidth || Screen.height != lastScreenHeight)
            {
                ApplyViewport();
            }
        }

        private void OnDisable()
        {
            if (cachedCamera != null)
            {
                cachedCamera.rect = new Rect(0f, 0f, 1f, 1f);
            }
        }

        private void ApplyViewport()
        {
            if (cachedCamera == null || targetHeight <= 0f)
            {
                return;
            }

            lastScreenWidth = Screen.width;
            lastScreenHeight = Screen.height;

            float screenAspect = Screen.width / (float)Screen.height;
            float targetAspect = targetWidth / targetHeight;

            if (Mathf.Approximately(screenAspect, targetAspect))
            {
                cachedCamera.rect = new Rect(0f, 0f, 1f, 1f);
                return;
            }

            if (screenAspect < targetAspect)
            {
                float normalizedHeight = screenAspect / targetAspect;
                float yOffset = (1f - normalizedHeight) * 0.5f;
                cachedCamera.rect = new Rect(0f, yOffset, 1f, normalizedHeight);
                return;
            }

            float normalizedWidth = targetAspect / screenAspect;
            float xOffset = (1f - normalizedWidth) * 0.5f;
            cachedCamera.rect = new Rect(xOffset, 0f, normalizedWidth, 1f);
        }
    }
}
