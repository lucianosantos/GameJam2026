
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;

public class CameraClickArea : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private Transform targetPosition;
    [SerializeField] private CameraController cameraController;
    public Transform TargetPosition => targetPosition;
    public bool IsAvailable => isActiveAndEnabled && cameraController != null && targetPosition != null &&
        !cameraController.IsGameOver && cameraController.ViewState == CameraViewState.Overview;
    private static RaycastHit[] approachHits = new RaycastHit[32];
    private static readonly List<RaycastResult> pointerHits = new List<RaycastResult>();

    public static bool IsBlockedByScreenUI(Vector2 screenPoint)
    {
        if (EventSystem.current == null) return false;
        var pointer = new PointerEventData(EventSystem.current) { position = screenPoint };
        pointerHits.Clear();
        EventSystem.current.RaycastAll(pointer, pointerHits);
        foreach (var hit in pointerHits)
        {
            if (!(hit.module is GraphicRaycaster graphics)) continue;
            var canvas = graphics.GetComponent<Canvas>();
            if (canvas != null && canvas.rootCanvas.renderMode != RenderMode.WorldSpace)
                return true;
        }
        return false;
    }

    public bool TryNavigateAtScreenPoint(Camera camera, Vector2 screenPoint)
    {
        if (!IsAvailable || IsBlockedByScreenUI(screenPoint) ||
            GameJam2026.ShipCompletionButton.FindAtScreenPoint(camera, screenPoint) != null ||
            FindAtScreenPoint(camera, screenPoint) != this)
            return false;
        cameraController.MoveTo(targetPosition);
        return true;
    }

    // Overview selects approach areas. Gameplay colliders belong to the focused view
    // and must not cover an approach area's click target.
    public static CameraClickArea FindAtScreenPoint(Camera camera, Vector2 screenPoint)
    {
        if (camera == null) return null;
        Ray ray = camera.ScreenPointToRay(screenPoint);
        int count;
        while ((count = Physics.RaycastNonAlloc(ray, approachHits, Mathf.Infinity,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) == approachHits.Length)
            System.Array.Resize(ref approachHits, approachHits.Length * 2);
        CameraClickArea closest = null;
        float distance = float.PositiveInfinity;
        for (int i = 0; i < count; i++)
        {
            var hit = approachHits[i];
            if (hit.distance >= distance || !hit.collider.TryGetComponent<CameraClickArea>(out var area) || !area.IsAvailable)
                continue;
            closest = area;
            distance = hit.distance;
        }
        return closest;
    }

    public bool TryGetBounds(out Bounds bounds)
    {
        bounds = default;
        bool found = false;
        if (!IsAvailable || areaColliders == null) return false;
        foreach (var area in areaColliders)
        {
            if (area == null || !area.enabled) continue;
            if (found) bounds.Encapsulate(area.bounds);
            else bounds = area.bounds;
            found = true;
        }
        return found;
    }
    private Collider[] areaColliders;
    private bool[] initialColliderStates;

    private void Awake()
    {
        areaColliders = GetComponents<Collider>();
        initialColliderStates = new bool[areaColliders.Length];
        for (int i = 0; i < areaColliders.Length; i++)
            initialColliderStates[i] = areaColliders[i].enabled;
    }

    private void OnEnable()
    {
        if (cameraController != null) cameraController.ViewChanged += RefreshColliders;
        RefreshColliders();
    }

    private void OnDisable()
    {
        if (cameraController != null) cameraController.ViewChanged -= RefreshColliders;
        for (int i = 0; i < areaColliders.Length; i++)
            if (areaColliders[i] != null) areaColliders[i].enabled = initialColliderStates[i];
    }

    private void RefreshColliders()
    {
        bool overview = cameraController != null &&
            !cameraController.IsGameOver &&
            cameraController.ViewState == CameraViewState.Overview;
        for (int i = 0; i < areaColliders.Length; i++)
            if (areaColliders[i] != null)
                areaColliders[i].enabled = overview && initialColliderStates[i];
    }

    public void OnPointerClick(PointerEventData data)
    {
        if (data.button != PointerEventData.InputButton.Left) return;
        TryNavigateAtScreenPoint(data.pressEventCamera != null ? data.pressEventCamera : Camera.main, data.position);
    }
}
