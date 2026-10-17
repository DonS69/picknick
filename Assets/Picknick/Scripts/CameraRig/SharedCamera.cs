using System.Collections.Generic;
using UnityEngine;

namespace Picknick.CameraRig
{
    /// <summary>
    /// One camera for everyone: follows the centre of all targets and zooms out
    /// so every creature stays on screen.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class SharedCamera : MonoBehaviour
    {
        public readonly List<Transform> Targets = new List<Transform>();

        public float padding = 2f;
        public float minSize = 5f;
        public float maxSize = 14f;
        public float smoothTime = 0.25f;

        Camera cam;
        Vector3 moveVelocity;
        float zoomVelocity;

        void Awake()
        {
            cam = GetComponent<Camera>();
            cam.orthographic = true;
        }

        void LateUpdate()
        {
            if (Targets.Count == 0)
                return;

            var bounds = new Bounds(Targets[0].position, Vector3.zero);
            foreach (Transform t in Targets)
                if (t != null) bounds.Encapsulate(t.position);

            Vector3 goal = new Vector3(bounds.center.x, bounds.center.y, transform.position.z);
            transform.position = Vector3.SmoothDamp(transform.position, goal, ref moveVelocity, smoothTime);

            float sizeForHeight = bounds.extents.y + padding;
            float sizeForWidth = (bounds.extents.x + padding) / cam.aspect;
            float targetSize = Mathf.Clamp(Mathf.Max(sizeForHeight, sizeForWidth), minSize, maxSize);
            cam.orthographicSize = Mathf.SmoothDamp(cam.orthographicSize, targetSize, ref zoomVelocity, smoothTime);
        }
    }
}
