
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System;

public enum CameraViewState { Overview, Transitioning, Minigame }

public class CameraController : MonoBehaviour
{
    [SerializeField] private Transform overview;
    [SerializeField] private float moveDuration = 0.6f;
    [SerializeField, Range(0f, 1f)] private float timeShakeDegrees = .18f;
    private float timeShake;
    private bool shakeApplied;
    private Quaternion unshakenRotation;

    public void SetTimeShake(float strength) => timeShake = Mathf.Clamp01(strength);

    private void RestoreShake()
    {
        if (!shakeApplied) return;
        transform.rotation = unshakenRotation;
        shakeApplied = false;
    }

    private void LateUpdate()
    {
        if (IsGameOver || timeShake <= 0f) return;
        unshakenRotation = transform.rotation;
        float t = (float)(Time.unscaledTimeAsDouble % 1000) * 7f;
        float amplitude = timeShakeDegrees * timeShake;
        transform.rotation = unshakenRotation * Quaternion.Euler(
            (Mathf.PerlinNoise(t, 0f) * 2f - 1f) * amplitude,
            (Mathf.PerlinNoise(0f, t) * 2f - 1f) * amplitude,
            (Mathf.PerlinNoise(t, 47f) * 2f - 1f) * amplitude * .5f);
        shakeApplied = true;
    }

    private void OnDisable() => RestoreShake();

    private Coroutine currentMovement;
    public CameraViewState ViewState { get; private set; } = CameraViewState.Overview;
    public Transform ActiveTarget { get; private set; }
    public event Action ViewChanged;
    public bool IsGameOver { get; private set; }

    public void ReturnToOverview() => MoveTo(overview);

    public bool IsFocusedOn(Transform target) => !IsGameOver && target != null &&
        ViewState == CameraViewState.Minigame && ActiveTarget == target;

    public void EndGame()
    {
        if (IsGameOver) return;
        IsGameOver = true;
        RestoreShake();
        timeShake = 0f;
        if (currentMovement != null) StopCoroutine(currentMovement);
        currentMovement = null;
        ViewChanged?.Invoke();
    }

    private void SetViewState(CameraViewState state)
    {
        ViewState = state;
        ViewChanged?.Invoke();
    }

    private void Start()
    {
        ActiveTarget = overview;
        SetViewState(CameraViewState.Overview);
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
        if (IsGameOver || target == null) return;
        if (ActiveTarget == target) return;
        RestoreShake();

        if (currentMovement != null)
            StopCoroutine(currentMovement);

        ActiveTarget = target;
        SetViewState(CameraViewState.Transitioning);
        if (moveDuration <= 0f)
        {
            transform.SetPositionAndRotation(target.position, target.rotation);
            currentMovement = null;
            SetViewState(target == overview ? CameraViewState.Overview : CameraViewState.Minigame);
            return;
        }
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
        SetViewState(target == overview ? CameraViewState.Overview : CameraViewState.Minigame);
    }

    private void Update()
    {
        // Remove the previous frame's offset before transitions calculate their next pose.
        RestoreShake();
        if (Keyboard.current != null &&
            Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            ReturnToOverview();
        }
    }
}
