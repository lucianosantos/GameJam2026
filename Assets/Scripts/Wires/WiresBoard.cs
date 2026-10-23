using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace GameJam2026.Wires
{
    [ExecuteAlways]
    public sealed class WiresBoard : MonoBehaviour, IPointerDownHandler, IPointerUpHandler,
        IInitializePotentialDragHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Header("Scene references")]
        [SerializeField] private Transform surface;
        [SerializeField] private WiresSocket[] sockets;
        [SerializeField] private WiresCable[] cables;
        [Tooltip("Static scenery paths used only to keep movable cables above the fixed wires.")]
        [SerializeField] private WiresCableObstacle[] sceneryCables;
        [SerializeField] private WiresRules rules;
        [SerializeField] private CameraController cameraController;
        [SerializeField] private Transform cameraTarget;
        [SerializeField] private Camera interactionCamera;
        [Header("Interaction (panel local units)")]
        [SerializeField, Min(0.001f)] private float snapTolerance = 0.045f;
        [SerializeField, Min(0f)] private float dragThresholdPixels = 4f;
        [Tooltip("Touch grab radius in pixels at a 540-pixel-high gameplay viewport.")]
        [SerializeField, Min(0f)] private float touchGrabRadius = 32f;
        [SerializeField] private float looseTipDepth = -0.025f;
        [SerializeField, Min(0.001f)] private float looseEndClearance = 0.034f;
        [Header("Completion")]
        [SerializeField] private UnityEvent onCompleted = new UnityEvent();

        private WiresPuzzle puzzle;
        private int heldEndpoint = -1;
        private Vector2 pressPosition;
        private Vector3 grabOffset;
        private bool dragging;
        private int? heldPointer;
        private bool completionSent;
        private bool geometryDirty = true;
        private Vector3[] lastEndpoints;
        private Matrix4x4 lastSurfaceMatrix;
        private Matrix4x4[] lastSceneryMatrices;
        public bool IsCompleted => puzzle != null && puzzle.IsCompleted;
        public UnityEvent OnCompleted => onCompleted;
        public bool CanInteract => Application.isPlaying && isActiveAndEnabled && puzzle != null &&
            !IsCompleted && cameraController != null && cameraController.IsFocusedOn(cameraTarget);

        private void OnEnable()
        {
            geometryDirty = true;
            if (Application.isPlaying)
            {
                Initialize();
                if (cameraController != null) cameraController.ViewChanged += RefreshInput;
                RefreshInput();
            }
        }

        private void Initialize()
        {
            if (puzzle != null) return; // Disabling the component or leaving the panel preserves state.
            try
            {
                if (surface == null || rules == null || interactionCamera == null || cameraController == null ||
                    cameraTarget == null || sockets == null || sockets.Length != 8 || cables == null || cables.Length != 4)
                    throw new InvalidOperationException("Assign surface, rules, camera, target, eight sockets and four cables.");
                string[] ports = new string[sockets.Length];
                for (int i = 0; i < sockets.Length; i++)
                {
                    if (sockets[i] == null) throw new InvalidOperationException("Missing socket.");
                    ports[i] = sockets[i].PortId;
                }
                foreach (var cable in cables)
                    if (cable == null || !cable.IsConfigured) throw new InvalidOperationException("Missing cable references.");
                puzzle = new WiresPuzzle(ports, rules.CopySolution());
            }
            catch (Exception error) { Debug.LogError("Wires: " + error.Message, this); }
        }

        private void RefreshInput()
        {
            if (!CanInteract) CancelDrag();
            if (cables == null) return;
            foreach (var cable in cables) if (cable != null) cable.SetInputEnabled(CanInteract);
        }

        public void OnPointerDown(PointerEventData data)
        {
            if (!CanInteract || heldPointer.HasValue || data.button != PointerEventData.InputButton.Left ||
                !interactionCamera.pixelRect.Contains(data.position) || CameraClickArea.IsBlockedByScreenUI(data.position)) return;
            PickEndpoint(data);
            if (heldEndpoint >= 0) heldPointer = data.pointerId;
        }

        public void OnInitializePotentialDrag(PointerEventData data) => data.useDragThreshold = false;
        public void OnBeginDrag(PointerEventData data) => OnDrag(data);

        public void OnDrag(PointerEventData data)
        {
            if (data.button != PointerEventData.InputButton.Left || heldPointer != data.pointerId) return;
            if (!CanInteract) { CancelDrag(); return; }
            MovePointer(data.position);
        }

        private void MovePointer(Vector2 cursor)
        {
            if (!interactionCamera.pixelRect.Contains(cursor)) return;
            Ray ray = interactionCamera.ScreenPointToRay(cursor);
            if (!dragging && Vector2.Distance(cursor, pressPosition) >= dragThresholdPixels)
            {
                dragging = true;
                puzzle.Disconnect(heldEndpoint);
            }
            if (dragging && PointOnSurface(ray, out Vector3 position))
            {
                position += grabOffset;
                position.z = looseTipDepth;
                End(heldEndpoint).position = surface.TransformPoint(position);
            }
        }

        public void OnPointerUp(PointerEventData data)
        {
            if (data.button != PointerEventData.InputButton.Left || heldPointer != data.pointerId) return;
            if (!CanInteract || !interactionCamera.pixelRect.Contains(data.position) ||
                CameraClickArea.IsBlockedByScreenUI(data.position) || IsCanceledTouch(data))
            { CancelDrag(); return; }
            MovePointer(data.position);
            ReleaseEndpoint();
        }

        private static bool IsCanceledTouch(PointerEventData data)
        {
            if (!(data is UnityEngine.InputSystem.UI.ExtendedPointerEventData extended) ||
                !(extended.device is UnityEngine.InputSystem.Touchscreen screen)) return false;
            foreach (var touch in screen.touches)
                if (touch.touchId.ReadValue() == extended.touchId &&
                    touch.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Canceled) return true;
            return false;
        }

        public void OnEndDrag(PointerEventData data)
        {
            // The EventSystem sends PointerUp before EndDrag. An unmatched end only cancels.
            if (data.button == PointerEventData.InputButton.Left && heldPointer == data.pointerId) CancelDrag();
        }

        private Transform End(int endpoint) => cables[endpoint / 2].End(endpoint % 2);

        private bool PointOnSurface(Ray ray, out Vector3 point)
        {
            var plane = new Plane(surface.TransformDirection(Vector3.back), surface.TransformPoint(new Vector3(0, 0, looseTipDepth)));
            if (plane.Raycast(ray, out float distance))
            { point = surface.InverseTransformPoint(ray.GetPoint(distance)); return true; }
            point = default; return false;
        }

        public bool TryGetEndpointHit(Ray ray, out RaycastHit closestHit)
        {
            closestHit = default;
            if (!CanInteract) return false;
            float closest = float.PositiveInfinity;
            for (int i = 0; i < cables.Length * 2; i++)
            {
                Collider collider = cables[i / 2].GrabCollider(i % 2);
                if (collider.enabled && collider.Raycast(ray, out RaycastHit hit, closest))
                { closest = hit.distance; closestHit = hit; }
            }
            return closestHit.collider != null;
        }

        public bool TryGetEndpointHit(PointerEventData data, out RaycastHit hit)
        {
            Ray ray = interactionCamera.ScreenPointToRay(data.position);
            // Exact hits retain priority, including when two plugs overlap.
            if (TryGetEndpointHit(ray, out hit)) return true;
            if (!CanInteract || !interactionCamera.pixelRect.Contains(data.position) ||
                !(data is UnityEngine.InputSystem.UI.ExtendedPointerEventData extended) ||
                !(extended.device is UnityEngine.InputSystem.Touchscreen)) return false;

            float radius = touchGrabRadius * interactionCamera.pixelRect.height / 540f;
            float nearest = radius * radius;
            for (int i = 0; i < cables.Length * 2; i++)
            {
                Collider collider = cables[i / 2].GrabCollider(i % 2);
                if (!collider.enabled) continue;
                Vector3 screen = interactionCamera.WorldToScreenPoint(collider.bounds.center);
                if (screen.z <= 0f || !interactionCamera.pixelRect.Contains((Vector2)screen)) continue;
                float distance = ((Vector2)screen - data.position).sqrMagnitude;
                if (distance > nearest) continue;
                Ray centerRay = interactionCamera.ScreenPointToRay(screen);
                if (!collider.Raycast(centerRay, out var candidate, Mathf.Infinity)) continue;
                nearest = distance;
                hit = candidate;
            }
            return hit.collider != null;
        }

        private void PickEndpoint(PointerEventData data)
        {
            heldEndpoint = -1;
            if (!TryGetEndpointHit(data, out var hit)) return;
            for (int i = 0; i < cables.Length * 2; i++)
                if (cables[i / 2].GrabCollider(i % 2) == hit.collider) { heldEndpoint = i; break; }
            if (heldEndpoint < 0) return;
            Vector2 cursor = data.position;
            Ray ray = interactionCamera.ScreenPointToRay(cursor);
            pressPosition = cursor; dragging = false;
            grabOffset = PointOnSurface(ray, out Vector3 point) ? surface.InverseTransformPoint(End(heldEndpoint).position) - point : Vector3.zero;
            grabOffset.z = 0;
        }

        private void ReleaseEndpoint()
        {
            if (dragging)
            {
                Vector3 end = surface.InverseTransformPoint(End(heldEndpoint).position);
                for (int i = 0; i < sockets.Length; i++)
                {
                    if (puzzle.IsOccupied(i)) continue;
                    Vector3 socket = surface.InverseTransformPoint(sockets[i].Position);
                    if (Vector2.Distance(end, socket) > snapTolerance) continue;
                    if (puzzle.TryConnect(heldEndpoint, i)) End(heldEndpoint).position = sockets[i].Position;
                    break;
                }
                if (puzzle.GetSocket(heldEndpoint) < 0) PlaceLooseTipAboveOtherPlugs();
            }
            CancelDrag();
            if (IsCompleted && !completionSent)
            {
                completionSent = true;
                RefreshInput();
                SoundManager.Play(GameSound.PuzzleCompleted);
                onCompleted.Invoke();
            }
        }

        private void PlaceLooseTipAboveOtherPlugs()
        {
            // A rejected drop remains loose. Rest it above another plug rather than burying it,
            // so the player can pick it up again even after dropping exactly on an occupied hole.
            Vector3 position = surface.InverseTransformPoint(End(heldEndpoint).position);
            for (int i = 0; i < cables.Length * 2; i++)
            {
                if (i == heldEndpoint) continue;
                Vector3 other = surface.InverseTransformPoint(End(i).position);
                if (Vector2.Distance(position, other) < looseEndClearance)
                    position.z = Mathf.Min(position.z, other.z - looseEndClearance);
            }
            if (sceneryCables != null) foreach (var fixedCable in sceneryCables)
            {
                if (fixedCable == null) continue;
                Vector3[] path = fixedCable.Sample(surface);
                if (path == null || path.Length == 0) continue;
                foreach (int index in new[] { 0, path.Length - 1 })
                    if (Vector2.Distance(position, path[index]) < looseEndClearance)
                        position.z = Mathf.Min(position.z, path[index].z - looseEndClearance);
            }
            End(heldEndpoint).position = surface.TransformPoint(position);
        }

        // Cancel without snapping: camera transitions and focus loss never complete a move by themselves.
        private void CancelDrag() { heldEndpoint = -1; heldPointer = null; dragging = false; }
        private void OnApplicationFocus(bool focus) { if (!focus) CancelDrag(); }
        private void OnApplicationPause(bool paused) { if (paused) CancelDrag(); }
        private void OnDisable()
        {
            if (cameraController != null) cameraController.ViewChanged -= RefreshInput;
            CancelDrag();
            if (cables != null) foreach (var cable in cables) if (cable != null) cable.SetInputEnabled(false);
        }

        private void LateUpdate() => RefreshCableGeometry();
        public void InvalidateGeometry() => geometryDirty = true;
        private void OnValidate() => InvalidateGeometry();

        public void RefreshCableGeometry()
        {
            if (surface == null || cables == null) return;
            foreach (var cable in cables) if (cable == null || !cable.IsConfigured) return;
            if (lastEndpoints == null || lastEndpoints.Length != cables.Length * 2)
            { lastEndpoints = new Vector3[cables.Length * 2]; geometryDirty = true; }
            bool changed = geometryDirty || lastSurfaceMatrix != surface.localToWorldMatrix;
            int sceneryCount = sceneryCables == null ? 0 : sceneryCables.Length;
            if (lastSceneryMatrices == null || lastSceneryMatrices.Length != sceneryCount)
            { lastSceneryMatrices = new Matrix4x4[sceneryCount]; changed = true; }
            for (int i = 0; i < sceneryCount; i++)
            {
                if (sceneryCables[i] == null) continue;
                var matrix = sceneryCables[i].transform.localToWorldMatrix;
                if (lastSceneryMatrices[i] != matrix) changed = true;
                lastSceneryMatrices[i] = matrix;
            }
            for (int i = 0; i < cables.Length * 2; i++)
            {
                Vector3 position = End(i).position;
                if (lastEndpoints[i] != position || !cables[i / 2].HasGeometry) changed = true;
                lastEndpoints[i] = position;
            }
            if (!changed) return;
            geometryDirty = false; lastSurfaceMatrix = surface.localToWorldMatrix;
            foreach (var cable in cables) cable.Sample(surface);
            var sceneryPaths = new Vector3[sceneryCount][];
            for (int i = 0; i < sceneryCount; i++)
                if (sceneryCables[i] != null) sceneryPaths[i] = sceneryCables[i].Sample(surface);
            // Stable stacking avoids oscillation when two wires cross. No rope physics required.
            for (int i = 0; i < cables.Length; i++)
            {
                for (int j = 0; j < sceneryCount; j++)
                    if (sceneryPaths[j] != null) cables[i].LiftAbove(sceneryPaths[j], sceneryCables[j].Radius);
                for (int j = 0; j < i; j++) cables[i].LiftAbove(cables[j]);
                cables[i].ApplyLift();
                cables[i].DrawTube(surface);
            }
        }
    }
}
