
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class CameraController : MonoBehaviour
{
    [SerializeField] private Transform overview;
    [SerializeField] private float moveDuration = 0.6f;

    private Coroutine currentMovement;

    private void Start()
    {
        // Começa na visão geral
        if (overview != null)
        {
            transform.SetPositionAndRotation(
                overview.position,
                overview.rotation
            );
        }
    }

    public void MoveTo(Transform target)
    {
        if (target == null) return;

        if (currentMovement != null)
            StopCoroutine(currentMovement);

        currentMovement = StartCoroutine(AnimateCamera(target));
    }

    private IEnumerator AnimateCamera(Transform target)
    {
        Vector3 initialPosition = transform.position;
        Quaternion initialRotation = transform.rotation;

        float elapsed = 0f;

        while (elapsed < moveDuration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / moveDuration);

            // Suaviza o início e o final
            t = t * t * (3f - 2f * t);

            transform.position = Vector3.Lerp(
                initialPosition,
                target.position,
                t
            );

            transform.rotation = Quaternion.Slerp(
                initialRotation,
                target.rotation,
                t
            );
            
            yield return null;
        }

        transform.SetPositionAndRotation(
            target.position,
            target.rotation
        );

        currentMovement = null;
    }

    private void Update()
    {
        if (Keyboard.current != null &&
            Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            MoveTo(overview);
        }
    }
}
