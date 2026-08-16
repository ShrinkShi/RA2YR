using System;
using UnityEngine;

namespace RA2YR.UnityIntegration
{
    public sealed class UnityHumanPlaytestCameraController
    {
        public UnityHumanPlaytestCameraController(float panSpeed = 8f, float minZoom = 5f, float maxZoom = 24f)
        {
            if (panSpeed <= 0f || minZoom <= 0f || maxZoom < minZoom || float.IsNaN(panSpeed) || float.IsInfinity(panSpeed))
                throw new ArgumentOutOfRangeException();
            PanSpeed = panSpeed;
            MinZoom = minZoom;
            MaxZoom = maxZoom;
        }

        public float PanSpeed { get; }
        public float MinZoom { get; }
        public float MaxZoom { get; }

        public void Apply(Camera camera, float horizontal, float vertical, float wheel, float deltaTime)
        {
            if (camera == null) throw new ArgumentNullException(nameof(camera));
            if (float.IsNaN(deltaTime) || float.IsInfinity(deltaTime) || deltaTime < 0f) return;
            Vector3 forward = Vector3.ProjectOnPlane(camera.transform.forward, Vector3.up);
            if (forward.sqrMagnitude < 0.000001f) forward = Vector3.forward;
            else forward.Normalize();
            Vector3 right = Vector3.ProjectOnPlane(camera.transform.right, Vector3.up);
            if (right.sqrMagnitude < 0.000001f) right = Vector3.right;
            else right.Normalize();
            if (!float.IsNaN(horizontal) && !float.IsInfinity(horizontal) && !float.IsNaN(vertical) && !float.IsInfinity(vertical))
                camera.transform.position += (right * horizontal + forward * vertical) * (PanSpeed * deltaTime);
            if (!float.IsNaN(wheel) && !float.IsInfinity(wheel) && Mathf.Abs(wheel) > 0.0001f)
                camera.orthographicSize = Mathf.Clamp(camera.orthographicSize - wheel, MinZoom, MaxZoom);
            if (float.IsNaN(camera.orthographicSize) || float.IsInfinity(camera.orthographicSize)) camera.orthographicSize = Mathf.Clamp(MinZoom, MinZoom, MaxZoom);
            Vector3 position = camera.transform.position;
            if (float.IsNaN(position.x) || float.IsInfinity(position.x) || float.IsNaN(position.y) || float.IsInfinity(position.y) || float.IsNaN(position.z) || float.IsInfinity(position.z))
                camera.transform.position = Vector3.zero;
        }
    }

    public sealed class UnityCameraAdapterPolicy
    {
        public UnityCameraAdapterPolicy(float minZoom = 1f, float maxZoom = 10000f)
        { if (minZoom <= 0f || maxZoom < minZoom) throw new ArgumentOutOfRangeException(); MinZoom = minZoom; MaxZoom = maxZoom; }
        public float MinZoom { get; } public float MaxZoom { get; }
    }

    public sealed class UnityIsometricCameraAdapter
    {
        private readonly UnityCameraAdapterPolicy policy;
        private Vector2 logicalPan;
        private float zoom;
        public UnityIsometricCameraAdapter(UnityCameraAdapterPolicy policy = null) { this.policy = policy ?? new UnityCameraAdapterPolicy(); zoom = 10f; viewportAspect = 1f; }
        public Vector2 LogicalPan => logicalPan;
        public float Zoom => zoom;
        public float ViewportAspect => viewportAspect;
        public void Pan(Vector2 delta) { logicalPan += delta; }
        public void SetZoom(float value) { if (value < policy.MinZoom || value > policy.MaxZoom) throw new ArgumentOutOfRangeException(nameof(value)); zoom = value; }
        public void SetViewportAspect(float value) { if (value <= 0f || float.IsNaN(value) || float.IsInfinity(value)) throw new ArgumentOutOfRangeException(nameof(value)); viewportAspect = value; }
        public void Apply(Camera camera)
        {
            if (camera == null) throw new ArgumentNullException(nameof(camera));
            camera.orthographic = true; camera.orthographicSize = zoom; camera.aspect = viewportAspect; camera.transform.position = new Vector3(logicalPan.x, zoom, logicalPan.y); camera.transform.rotation = Quaternion.Euler(45f, 0f, 0f);
        }
        private float viewportAspect;
    }
}
