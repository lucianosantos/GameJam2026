
using UnityEngine;
using UnityEngine.InputSystem;

public class CameraClickArea : MonoBehaviour
{
    [SerializeField] private Transform targetPosition;
    [SerializeField] private CameraController cameraController;

    private void Update()
    {
        if (Mouse.current == null)
            return;

        if (!Mouse.current.leftButton.wasPressedThisFrame)
            return;

        Camera cam = Camera.main;

        if (cam == null)
            return;

        Ray ray = cam.ScreenPointToRay(
            Mouse.current.position.ReadValue()
        );

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            // Verifica se clicamos no collider desta área
            if (hit.collider.gameObject == gameObject)
            {
                cameraController.MoveTo(targetPosition);
            }
        }
    }
}
