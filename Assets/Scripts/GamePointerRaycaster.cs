using System.Collections.Generic;
using GameJam2026.Wires;
using UnityEngine;
using UnityEngine.EventSystems;

namespace GameJam2026
{
    // Use the EventSystem for mouse and touch while retaining the game's selection rules.
    [DisallowMultipleComponent, RequireComponent(typeof(CameraController))]
    public sealed class GamePointerRaycaster : PhysicsRaycaster
    {
        // World-space canvases are panel art; screen-overlay UI keeps its higher priority.
        public override int sortOrderPriority => int.MinValue + 1;
        private CameraController controller;
        private WiresBoard wires;

        public override void Raycast(PointerEventData data, List<RaycastResult> results)
        {
            var camera = eventCamera;
            if (camera == null || !camera.pixelRect.Contains(data.position)) return;
            if (controller == null) controller = GetComponent<CameraController>();
            if (controller.IsGameOver || controller.ViewState == CameraViewState.Transitioning) return;
            Ray ray = camera.ScreenPointToRay(data.position);
            if (controller.ViewState == CameraViewState.Overview)
            {
                var finish = ShipCompletionButton.FindAtScreenPoint(camera, data.position);
                if (finish != null)
                {
                    if (finish.GetComponent<Collider>().Raycast(ray, out var hit, Mathf.Infinity)) Add(hit, data, results);
                    return;
                }
                var area = CameraClickArea.FindAtScreenPoint(camera, data.position);
                if (area == null) return;
                RaycastHit closest = default;
                float distance = float.PositiveInfinity;
                foreach (var collider in area.GetComponents<Collider>())
                    if (collider.enabled && collider.Raycast(ray, out var hit, distance))
                    { closest = hit; distance = hit.distance; }
                if (closest.collider != null) Add(closest, data, results);
                return;
            }
            if (wires == null) wires = FindFirstObjectByType<WiresBoard>();
            if (wires != null && wires.CanInteract)
            {
                // Keep the same endpoint-only selection as the original cable input.
                if (wires.TryGetEndpointHit(ray, out var hit)) Add(hit, data, results);
                return;
            }
            base.Raycast(data, results);
        }

        private void Add(RaycastHit hit, PointerEventData data, List<RaycastResult> results)
        {
            if ((finalEventMask & (1 << hit.collider.gameObject.layer)) == 0) return;
            results.Add(new RaycastResult
            {
                gameObject = hit.collider.gameObject,
                module = this,
                distance = hit.distance,
                worldPosition = hit.point,
                worldNormal = hit.normal,
                screenPosition = data.position,
                index = results.Count
            });
        }
    }
}
