using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace GameJam2026.Wires
{
    [ExecuteAlways]
    public sealed class WiresBoard : MonoBehaviour
    {
        [Header("Scene references")]
        [SerializeField] private Transform surface;
        [SerializeField] private WiresSocket[] sockets;
        [SerializeField] private WiresCable[] cables;
        [SerializeField] private WiresRules rules;
        [SerializeField] private CameraController cameraController;
        [SerializeField] private Transform cameraTarget;
        [SerializeField] private Camera interactionCamera;
        [Header("Interaction (panel local units)")]
        [SerializeField, Min(0.001f)] private float snapTolerance = 0.025f;
        [SerializeField, Min(0f)] private float dragThresholdPixels = 4f;
        [SerializeField] private float looseTipDepth = -0.025f;
        [SerializeField, Min(0.001f)] private float looseEndClearance = 0.034f;
        [Header("Completion")]
        [SerializeField] private UnityEvent onCompleted = new UnityEvent();

        private WiresPuzzle puzzle;
        private int heldEndpoint = -1;
        private Vector2 pressPosition;
        private Vector3 grabOffset;
        private bool dragging;
        private bool completionSent;
        private bool geometryDirty = true;
        private Vector3[] lastEndpoints;
        private Matrix4x4 lastSurfaceMatrix;
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

        private void Update()
        {
            if (!Application.isPlaying) return;
            if (!CanInteract || Mouse.current == null) { CancelDrag(); return; }
            Vector2 cursor = Mouse.current.position.ReadValue();
            Ray ray = interactionCamera.ScreenPointToRay(cursor);
            if (Mouse.current.leftButton.wasPressedThisFrame) PickEndpoint(ray, cursor);
            if (heldEndpoint < 0) return;
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
            if (Mouse.current.leftButton.wasReleasedThisFrame) ReleaseEndpoint();
            else if (!Mouse.current.leftButton.isPressed) CancelDrag();
        }

        private Transform End(int endpoint) => cables[endpoint / 2].End(endpoint % 2);

        private bool PointOnSurface(Ray ray, out Vector3 point)
        {
            var plane = new Plane(surface.TransformDirection(Vector3.back), surface.TransformPoint(new Vector3(0, 0, looseTipDepth)));
            if (plane.Raycast(ray, out float distance))
            { point = surface.InverseTransformPoint(ray.GetPoint(distance)); return true; }
            point = default; return false;
        }

        private void PickEndpoint(Ray ray, Vector2 cursor)
        {
            float closest = float.PositiveInfinity;
            heldEndpoint = -1;
            for (int i = 0; i < cables.Length * 2; i++)
            {
                Collider collider = cables[i / 2].GrabCollider(i % 2);
                if (collider.enabled && collider.Raycast(ray, out RaycastHit hit, closest))
                { closest = hit.distance; heldEndpoint = i; }
            }
            if (heldEndpoint < 0) return;
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
            End(heldEndpoint).position = surface.TransformPoint(position);
        }

        // Cancel without snapping: camera transitions and focus loss never complete a move by themselves.
        private void CancelDrag() { heldEndpoint = -1; dragging = false; }
        private void OnApplicationFocus(bool focus) { if (!focus) CancelDrag(); }
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
            for (int i = 0; i < cables.Length * 2; i++)
            {
                Vector3 position = End(i).position;
                if (lastEndpoints[i] != position || !cables[i / 2].HasGeometry) changed = true;
                lastEndpoints[i] = position;
            }
            if (!changed) return;
            geometryDirty = false; lastSurfaceMatrix = surface.localToWorldMatrix;
            foreach (var cable in cables) cable.Sample(surface);
            // Stable stacking avoids oscillation when two wires cross. No rope physics required.
            for (int i = 0; i < cables.Length; i++)
            {
                for (int j = 0; j < i; j++) cables[i].LiftAbove(cables[j]);
                cables[i].ApplyLift();
                cables[i].DrawTube(surface);
            }
        }
    }
}
