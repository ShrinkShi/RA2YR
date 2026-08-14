using System;
using UnityEngine;

namespace RA2YR.UnityIntegration
{
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
        public UnityIsometricCameraAdapter(UnityCameraAdapterPolicy policy = null) { this.policy = policy ?? new UnityCameraAdapterPolicy(); zoom = 10f; }
        public Vector2 LogicalPan => logicalPan;
        public float Zoom => zoom;
        public void Pan(Vector2 delta) { logicalPan += delta; }
        public void SetZoom(float value) { if (value < policy.MinZoom || value > policy.MaxZoom) throw new ArgumentOutOfRangeException(nameof(value)); zoom = value; }
        public void Apply(Camera camera)
        {
            if (camera == null) throw new ArgumentNullException(nameof(camera));
            camera.orthographic = true; camera.orthographicSize = zoom; camera.transform.position = new Vector3(logicalPan.x, zoom, logicalPan.y); camera.transform.rotation = Quaternion.Euler(45f, 0f, 0f);
        }
    }
}
