using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static bool isPaused = false;
    public GameObject pauseMenuUI;
    public PlayerSpawn playerSpawn;
    public BossController boss;


    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused) Resume();
            else Pause();
        }
    }

    public void Resume()
    {
        pauseMenuUI.SetActive(false);
        Time.timeScale = 1.0f;
        isPaused = false;
    }

    public void Pause()
    {
        pauseMenuUI.SetActive(true);
        Time.timeScale = 0;
        isPaused = true;
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    public void ReloadLastCheckpoint()
    {
        Resume();
        playerSpawn.RespawnPlayer();
        OnPlayerRespawn();
    }
    public void OnPlayerRespawn()
    {
        if (boss != null)
        {
            boss.RespawnBoss();
        }
    }
}