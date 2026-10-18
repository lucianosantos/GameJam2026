using UnityEngine;
using UnityEngine.UI;

namespace GameJam2026
{
    // Sample an oversized elliptical vignette so no rectangular seams or solid outer edge are visible.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class ScreenWarningOverlay : MaskableGraphic
    {
        public ScreenWarningOverlay() { useLegacyMeshGeneration = false; }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            Rect r = GetPixelAdjustedRect();
            const int columns = 64, rows = 40;
            for (int y = 0; y <= rows; y++)
            {
                float v = (float)y / rows;
                for (int x = 0; x <= columns; x++)
                {
                    float u = (float)x / columns;
                    float radius = new Vector2(u * 2f - 1f, v * 2f - 1f).magnitude;
                    // Full color is reached outside even the screen corners (radius sqrt(2)).
                    float fade = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.58f, 1.65f, radius));
                    Color tint = color;
                    tint.a *= fade;
                    mesh.AddVert(new Vector2(Mathf.Lerp(r.xMin, r.xMax, u),
                        Mathf.Lerp(r.yMin, r.yMax, v)), tint, Vector2.zero);
                }
            }
            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < columns; x++)
                {
                    int start = y * (columns + 1) + x;
                    mesh.AddTriangle(start, start + columns + 1, start + 1);
                    mesh.AddTriangle(start + 1, start + columns + 1, start + columns + 2);
                }
            }
        }
    }
}
