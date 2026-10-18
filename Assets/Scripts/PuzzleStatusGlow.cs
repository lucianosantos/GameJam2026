using UnityEngine;
using UnityEngine.UI;

// Local glow keeps these indicators luminous without changing the camera's post processing.
[DisallowMultipleComponent]
public sealed class PuzzleStatusGlow : MonoBehaviour
{
    private Renderer source;
    private Transform halo;
    private Material haloMaterial;
    private Light spill;
    private RawImage screenHalo;
    private Camera viewCamera;
    private bool visible = true;

    public void SetVisible(bool value)
    {
        visible = value;
        if (halo != null) halo.gameObject.SetActive(value && source != null && source.enabled);
    }

    public void SetLightIntensity(float strength)
    {
        if (spill != null) spill.intensity = .65f * strength;
    }

    public static void Apply(Renderer target, Color color)
    {
        if (target == null) return;
        var glow = target.GetComponent<PuzzleStatusGlow>();
        if (glow == null) glow = target.gameObject.AddComponent<PuzzleStatusGlow>();
        glow.source = target;
        if (glow.halo == null) glow.CreateLamp();
        glow.haloMaterial.SetColor("_Tint", color);
        glow.spill.color = color;
        glow.PositionLamp();
    }

    public static void Apply(Image screen, bool completed)
    {
        if (screen == null) return;
        var glow = screen.GetComponent<PuzzleStatusGlow>();
        if (glow == null) glow = screen.gameObject.AddComponent<PuzzleStatusGlow>();
        if (glow.screenHalo == null)
        {
            var child = new GameObject("Completion glow", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
            child.layer = screen.gameObject.layer;
            child.transform.SetParent(screen.transform, false);
            child.transform.SetAsFirstSibling();
            glow.screenHalo = child.GetComponent<RawImage>();
            glow.screenHalo.raycastTarget = false;
            glow.haloMaterial = new Material(Resources.Load<Shader>("PuzzleStatusHalo"));
            glow.haloMaterial.SetColor("_Tint", new Color(.08f, 1f, .28f));
            glow.screenHalo.material = glow.haloMaterial;
            var rect = glow.screenHalo.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = screen.rectTransform.rect.size * .65f;
            rect.anchoredPosition = Vector2.zero;
        }
        glow.screenHalo.enabled = completed;
    }

    private void CreateLamp()
    {
        var child = new GameObject("Status light halo", typeof(MeshFilter), typeof(MeshRenderer));
        child.layer = gameObject.layer;
        child.transform.SetParent(transform, false);
        halo = child.transform;
        // A quad without a collider that could intercept puzzle clicks.
        var mesh = new Mesh { name = "Status halo quad" };
        mesh.vertices = new[] { new Vector3(-.5f,-.5f,0), new Vector3(.5f,-.5f,0), new Vector3(.5f,.5f,0), new Vector3(-.5f,.5f,0) };
        mesh.uv = new[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up };
        mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
        mesh.RecalculateBounds();
        child.GetComponent<MeshFilter>().sharedMesh = mesh;
        haloMaterial = new Material(Resources.Load<Shader>("PuzzleStatusHalo"));
        var renderer = child.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = haloMaterial;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        spill = child.AddComponent<Light>();
        spill.type = LightType.Point;
        spill.shadows = LightShadows.None;
        spill.intensity = .65f;
        spill.cullingMask = ~(1 << 2);
    }

    private void LateUpdate() { if (source != null) PositionLamp(); }

    private void PositionLamp()
    {
        if (viewCamera == null) viewCamera = Camera.main;
        if (viewCamera == null) return;
        var bounds = source.bounds;
        var towardCamera = (viewCamera.transform.position - bounds.center).normalized;
        // Move the billboard in front of the lens; normal depth testing still hides it behind scenery.
        float radius = bounds.extents.magnitude;
        halo.position = bounds.center + towardCamera * (radius + .003f);
        halo.rotation = viewCamera.transform.rotation;
        float diameter = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z) * 2.5f;
        // Compensate for the imported model's scale so the halo uses world units.
        halo.localScale = Vector3.one;
        var scale = halo.lossyScale;
        halo.localScale = new Vector3(diameter / Mathf.Abs(scale.x), diameter / Mathf.Abs(scale.y), diameter / Mathf.Abs(scale.z));
        spill.range = Mathf.Max(.15f, radius * 6f);
        halo.gameObject.SetActive(source.enabled && visible);
    }

    private void OnDestroy()
    {
        if (halo != null) Destroy(halo.GetComponent<MeshFilter>().sharedMesh);
        if (haloMaterial != null) Destroy(haloMaterial);
    }
}
