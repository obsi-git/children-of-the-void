using UnityEngine;
using UnityEngine.UI;

public class HealthBarSwitcher : MonoBehaviour
{
    public PlayerControllerMain player; // drag player here

    public Image healthImage; // drag the HealthBar_State image here

    [Header("Health Sprites")]
    public Sprite full;
    public Sprite seventyFive;
    public Sprite half;
    public Sprite low;
    public Sprite empty;

    void Update()
    {
        if (player == null || healthImage == null) return;

        float healthPercent = (float)player.currentHealth / player.maxHealth;

        if (healthPercent >= 0.95f)
        {
            healthImage.sprite = full;
        }
        else if (healthPercent >= 0.7f)
        {
            healthImage.sprite = seventyFive;
        }
        else if (healthPercent >= 0.4f)
        {
            healthImage.sprite = half;
        }
        else if (healthPercent > 0)
        {
            healthImage.sprite = low;
        }
        else
        {
            healthImage.sprite = empty;
        }
    }
}