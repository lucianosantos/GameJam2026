
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    [SerializeField] private GameObject creditsPanel;

    public void OpenController()
    {
        SceneManager.LoadScene("ControllerScene");
    }

    public void OpenManual()
    {
        SceneManager.LoadScene("ManualScene");
    }

    public void OpenCredits()
    {
        if (creditsPanel != null)
            creditsPanel.SetActive(true);
    }

    public void CloseCredits()
    {
        if (creditsPanel != null)
            creditsPanel.SetActive(false);
    }

    public void ExitGame()
    {
        Application.Quit();
    }
}
