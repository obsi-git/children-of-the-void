using UnityEngine;

public class PlayerSpawn : MonoBehaviour
{
    public static Vector2 spawnPoint;
    private static GameObject player;

    void Awake()
    {
        spawnPoint = transform.position;
    }

    public static void UpdateSpawnPoint(Vector2 newpoint)
    {
        spawnPoint = newpoint;
    }

    public static void RespawnPlayer()
    {
        if (player != null)
        {
            player.transform.position = spawnPoint;

            var controller = player.GetComponent<PlayerControllerMain>();
            controller.ResetAfterRespawn();
        }
    }
}