using UnityEngine;

namespace GameJam2026.Wires
{
    [CreateAssetMenu(menuName = "Minigames/Wires Rules")]
    public sealed class WiresRules : ScriptableObject
    {
        [SerializeField] private WiresPair[] solution;
        public WiresPair[] CopySolution() => solution == null ? null : (WiresPair[])solution.Clone();
    }
}
