using UnityEngine;
using UnityEngine.UI;

public class VoidMeterSwitcher : MonoBehaviour
{
    public PlayerControllerMain player; // drag your player object here
    public Image voidImage;             // drag the VoidMeterState image here

    [Header("Void Sprites")]
    public Sprite empty;
    public Sprite low;
    public Sprite half;
    public Sprite high;
    public Sprite full;

    void Update()
    {
        if (player == null || voidImage == null) return;

        float voidPercent = (float)player.currentVoidEnergy / player.maxVoidEnergy;

        if (voidPercent >= 0.95f)
        {
            voidImage.sprite = full;
        }
        else if (voidPercent >= 0.7f)
        {
            voidImage.sprite = high;
        }
        else if (voidPercent >= 0.4f)
        {
            voidImage.sprite = half;
        }
        else if (voidPercent > 0f)
        {
            voidImage.sprite = low;
        }
        else
        {
            voidImage.sprite = empty;
        }
    }
}