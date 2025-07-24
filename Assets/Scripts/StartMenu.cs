using UnityEngine;
using UnityEngine.UI;

public class StartMenu : MonoBehaviour
{
    public GameObject startMenuUI;
    public GameObject player;

    void Start()
    {
        Time.timeScale = 0f;
        startMenuUI.SetActive(true);
        if (player != null)
            player.GetComponent<PlayerControllerMain>().enabled = false;
    }

    public void PlayGame()
    {
        Time.timeScale = 1f;
        startMenuUI.SetActive(false);
        if (player != null)
            player.GetComponent<PlayerControllerMain>().enabled = true;
    }

    public void QuitGame()
    {
        Application.Quit();
        Debug.Log("Quit Game");
    }
}