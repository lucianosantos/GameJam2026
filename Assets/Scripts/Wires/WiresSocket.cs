using UnityEngine;

namespace GameJam2026.Wires
{
    public sealed class WiresSocket : MonoBehaviour
    {
        [SerializeField] private string portId;
        [SerializeField] private Transform anchor;
        public string PortId => portId;
        public Vector3 Position => anchor != null ? anchor.position : transform.position;
    }
}
