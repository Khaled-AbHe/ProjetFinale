using UnityEngine;

public class UIButtonHandler : MonoBehaviour
{
    public void PlayGame()
    {   
        GameManager.Instance.StartGame();
    }

    public void PlayAgain() 
    {
        GameManager.Instance.RestartGame();
    }

    public void GoToMainMenu() 
    {
        GameManager.Instance.LoadScene(0);
    }

    public void TogglePause()
    {
        GameManager.Instance.TogglePause();
    }
}
