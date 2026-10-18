using UnityEngine;

namespace GameJam2026.LightSequence
{
    // Scene references live on the minigame, so any number of prefab copies can share them.
    public sealed class LightSequenceInput : MonoBehaviour
    {
        [SerializeField] private CameraController cameraController;
        [SerializeField] private Transform cameraTarget;

        public bool CanInteract => isActiveAndEnabled && cameraController != null &&
                                   cameraController.IsFocusedOn(cameraTarget);
    }
}
