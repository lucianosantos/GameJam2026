using UnityEngine;

namespace GameJam2026.Wires
{
    [ExecuteAlways]
    public sealed class WiresCable : MonoBehaviour
    {
        [SerializeField] private Transform firstEnd;
        [SerializeField] private Transform secondEnd;
        [SerializeField] private Collider firstGrabCollider;
        [SerializeField] private Collider secondGrabCollider;
        [SerializeField] private MeshFilter tube;
        [SerializeField, Min(0.001f)] private float radius = 0.006f;
        [SerializeField, Range(16, 128)] private int segments = 64;
        [SerializeField, Range(6, 16)] private int sides = 10;
        [SerializeField, Min(0f)] private float slack = 0.025f;
        [SerializeField, Min(0f)] private float crossingClearance = 0.002f;

        private Mesh mesh;
        private Vector3[] points;
        private float[] lifts;
        private Vector3[] vertices;
        private Vector2[] uv;
        private int[] triangles;
        public float Radius => radius;
        public Transform End(int index) => index == 0 ? firstEnd : secondEnd;
        public Collider GrabCollider(int index) => index == 0 ? firstGrabCollider : secondGrabCollider;
        public bool IsConfigured => firstEnd != null && secondEnd != null && tube != null &&
                                    firstGrabCollider != null && secondGrabCollider != null;
        public bool HasGeometry => mesh != null && tube != null && tube.sharedMesh == mesh;

        public void SetInputEnabled(bool enabled)
        {
            if (firstGrabCollider != null) firstGrabCollider.enabled = enabled;
            if (secondGrabCollider != null) secondGrabCollider.enabled = enabled;
        }

        // Called in ordered passes by the board. Coordinates use the panel's XY plane; -Z is above it.
        public void Sample(Transform surface)
        {
            segments = Mathf.Clamp(segments, 16, 128);
            sides = Mathf.Clamp(sides, 6, 16);
            radius = Mathf.Max(0.001f, radius);
            if (points == null || points.Length != segments + 1)
            { points = new Vector3[segments + 1]; lifts = new float[segments + 1]; }
            Vector3 a = surface.InverseTransformPoint(firstEnd.position);
            Vector3 b = surface.InverseTransformPoint(secondEnd.position);
            Vector2 delta = b - a;
            Vector3 side = delta.sqrMagnitude > 0.000001f ?
                new Vector3(-delta.y, delta.x, 0).normalized : Vector3.right;
            float bow = Mathf.Min(slack, delta.magnitude * 0.25f + 0.012f);
            for (int i = 0; i <= segments; i++)
            {
                float t = i / (float)segments;
                points[i] = Vector3.Lerp(a, b, t) + side * (Mathf.Sin(t * Mathf.PI) * bow);
                lifts[i] = 0f;
            }
        }

        public void LiftAbove(WiresCable lower)
        {
            LiftAbove(lower.points, lower.radius);
        }

        public void LiftAbove(Vector3[] lowerPoints, float lowerRadius)
        {
            if (points == null || lowerPoints == null) return;
            float clearance = radius + lowerRadius + crossingClearance;
            for (int i = 1; i < points.Length - 1; i++)
                for (int j = 0; j < lowerPoints.Length - 1; j++)
                {
                    Vector3 a = lowerPoints[j], b = lowerPoints[j + 1];
                    Vector2 d = b - a, p = points[i] - a;
                    float t = d.sqrMagnitude < 0.0000001f ? 0 : Mathf.Clamp01(Vector2.Dot(p, d) / d.sqrMagnitude);
                    Vector3 closest = Vector3.Lerp(a, b, t);
                    float distance = Vector2.Distance(points[i], closest);
                    if (distance > clearance * 2f) continue;
                    float lift = points[i].z - closest.z + clearance;
                    if (lift <= 0) continue;
                    // Spread the local bridge, avoiding a sharp spike at the crossing.
                    for (int k = Mathf.Max(1, i - 5); k <= Mathf.Min(segments - 1, i + 5); k++)
                    {
                        float blend = 0.5f + 0.5f * Mathf.Cos((k - i) * Mathf.PI / 6f);
                        lifts[k] = Mathf.Max(lifts[k], lift * blend);
                    }
                }
        }

        public void ApplyLift()
        {
            for (int i = 1; i < points.Length - 1; i++) points[i].z -= lifts[i];
        }

        public void DrawTube(Transform surface)
        {
            int count = (segments + 1) * sides;
            if (mesh == null)
            {
                mesh = new Mesh { name = "Wires cable (dynamic)", hideFlags = HideFlags.DontSave };
                mesh.MarkDynamic();
                tube.sharedMesh = mesh;
            }
            if (vertices == null || vertices.Length != count || triangles.Length != segments * sides * 6)
            {
                vertices = new Vector3[count]; uv = new Vector2[count];
                triangles = new int[segments * sides * 6];
                int n = 0;
                for (int i = 0; i < segments; i++) for (int s = 0; s < sides; s++)
                {
                    int a = i * sides + s, b = i * sides + (s + 1) % sides, c = a + sides, d = b + sides;
                    triangles[n++] = a; triangles[n++] = b; triangles[n++] = c;
                    triangles[n++] = b; triangles[n++] = d; triangles[n++] = c;
                }
                mesh.Clear();
            }
            float length = 0;
            for (int i = 0; i <= segments; i++)
            {
                if (i > 0) length += Vector3.Distance(points[i], points[i - 1]);
                Vector3 tangent = points[Mathf.Min(i + 1, segments)] - points[Mathf.Max(i - 1, 0)];
                if (tangent.sqrMagnitude < 0.0000001f) tangent = Vector3.up;
                tangent.Normalize();
                Vector3 n = Vector3.Cross(tangent, Vector3.forward).normalized;
                if (n.sqrMagnitude < 0.1f) n = Vector3.right;
                Vector3 binormal = Vector3.Cross(tangent, n).normalized;
                for (int s = 0; s < sides; s++)
                {
                    float angle = s * Mathf.PI * 2f / sides;
                    Vector3 v = points[i] + radius * (n * Mathf.Cos(angle) + binormal * Mathf.Sin(angle));
                    vertices[i * sides + s] = tube.transform.InverseTransformPoint(surface.TransformPoint(v));
                    uv[i * sides + s] = new Vector2(s / (float)sides, length * 12f);
                }
            }
            mesh.vertices = vertices; mesh.uv = uv; mesh.triangles = triangles;
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
        }

        private void Update()
        {
            // Prefabs can be inspected on their own. The board owns the shared crossing pass.
            if (GetComponentInParent<WiresBoard>() != null || !IsConfigured) return;
            Sample(transform); DrawTube(transform);
        }

        private void OnDisable()
        {
            if (mesh == null) return;
            if (Application.isPlaying) Destroy(mesh); else DestroyImmediate(mesh);
            mesh = null;
        }

        private void OnValidate()
        {
            var board = GetComponentInParent<WiresBoard>();
            if (board != null) board.InvalidateGeometry();
        }
    }
}
