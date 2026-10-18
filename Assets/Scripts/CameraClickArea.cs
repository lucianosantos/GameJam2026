
using UnityEngine;
using UnityEngine.InputSystem;

public class CameraClickArea : MonoBehaviour
{
    [SerializeField] private Transform targetPosition;
    [SerializeField] private CameraController cameraController;
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
            cameraController.ViewState == CameraViewState.Overview;
        for (int i = 0; i < areaColliders.Length; i++)
            if (areaColliders[i] != null)
                areaColliders[i].enabled = overview && initialColliderStates[i];
    }

    private void Update()
    {
        if (Mouse.current == null)
            return;

        if (!Mouse.current.leftButton.wasPressedThisFrame)
            return;

        if (cameraController == null || targetPosition == null)
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
