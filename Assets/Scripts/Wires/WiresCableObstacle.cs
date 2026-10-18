using UnityEngine;

namespace GameJam2026.Wires
{
    // Geometry data only: static scenery has no socket, grab collider or input behavior.
    public sealed class WiresCableObstacle : MonoBehaviour
    {
        [SerializeField] private Vector3[] centerline;
        [SerializeField, Min(0.001f)] private float radius = 0.006f;
        public float Radius => radius;

        public Vector3[] Sample(Transform surface)
        {
            if (centerline == null) return null;
            var points = new Vector3[centerline.Length];
            for (int i = 0; i < points.Length; i++)
                points[i] = surface.InverseTransformPoint(transform.TransformPoint(centerline[i]));
            return points;
        }

        private void OnValidate()
        {
            var board = GetComponentInParent<WiresBoard>();
            if (board != null) board.InvalidateGeometry();
        }
    }
}
