using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameJam2026
{
    public sealed class SceneNavigation : MonoBehaviour
    {
        public void QuitToMenu() => SceneManager.LoadScene("MainMenu");
    }
}
