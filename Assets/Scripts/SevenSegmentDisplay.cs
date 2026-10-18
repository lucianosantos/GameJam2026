using UnityEngine;
using UnityEngine.UI;

namespace GameJam2026
{
    // Vector geometry keeps the LED digits sharp without an external font or texture.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class SevenSegmentDisplay : MaskableGraphic
    {
        private static readonly int[] Masks = { 0x3f, 0x06, 0x5b, 0x4f, 0x66, 0x6d, 0x7d, 0x07, 0x7f, 0x6f };
        private int seconds = 600;

        public SevenSegmentDisplay()
        {
            useLegacyMeshGeneration = false;
        }

        public void SetSeconds(int value)
        {
            value = Mathf.Clamp(value, 0, 5999);
            if (seconds == value) return;
            seconds = value;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            Rect r = GetPixelAdjustedRect();
            float scale = Mathf.Min(r.width / 244f, r.height / 80f);
            Vector2 origin = r.center - new Vector2(122, 40) * scale;
            int minutes = seconds / 60;
            int[] digits = { minutes / 10, minutes % 10, seconds / 10 % 6, seconds % 10 };
            float[] offsets = { 0, 58, 132, 190 };
            for (int d = 0; d < 4; d++)
            {
                Vector2 p = origin + new Vector2(offsets[d], 0) * scale;
                for (int s = 0; s < 7; s++)
                {
                    bool on = (Masks[digits[d]] & (1 << s)) != 0;
                    Color tint = on ? color : new Color(color.r * .12f, color.g * .12f, color.b * .12f, color.a);
                    Vector2 center;
                    bool horizontal = s == 0 || s == 3 || s == 6;
                    switch (s)
                    {
                        case 0: center = new Vector2(24, 75); break;
                        case 1: center = new Vector2(45, 57); break;
                        case 2: center = new Vector2(45, 23); break;
                        case 3: center = new Vector2(24, 5); break;
                        case 4: center = new Vector2(3, 23); break;
                        case 5: center = new Vector2(3, 57); break;
                        default: center = new Vector2(24, 40); break;
                    }
                    Segment(mesh, p + center * scale, horizontal ? 39 * scale : 29 * scale,
                        7 * scale, horizontal, tint);
                }
            }
            Segment(mesh, origin + new Vector2(118, 24) * scale, 6 * scale, 6 * scale, true, color);
            Segment(mesh, origin + new Vector2(118, 56) * scale, 6 * scale, 6 * scale, true, color);
        }

        private static void Segment(VertexHelper mesh, Vector2 center, float length, float thickness,
            bool horizontal, Color tint)
        {
            int start = mesh.currentVertCount;
            float half = length * .5f, bevel = thickness * .5f;
            Vector2[] points = {
                new Vector2(-half, 0), new Vector2(-half + bevel, bevel),
                new Vector2(half - bevel, bevel), new Vector2(half, 0),
                new Vector2(half - bevel, -bevel), new Vector2(-half + bevel, -bevel)
            };
            foreach (Vector2 point in points)
                mesh.AddVert(center + (horizontal ? point : new Vector2(-point.y, point.x)), tint, Vector2.zero);
            for (int i = 1; i < 5; i++) mesh.AddTriangle(start, start + i, start + i + 1);
        }
    }
}
