using UnityEngine;

public class PlayerSpawn : MonoBehaviour
{
    public GameObject playerObject; // ← Drag your player here in the Inspector
    public static Vector2 spawnPoint;

    void Awake()
    {
        spawnPoint = transform.position;

        if (playerObject == null)
        {
            Debug.LogWarning("Player object not assigned in PlayerSpawn! Assign it in the Inspector.");
        }
    }

    public static void UpdateSpawnPoint(Vector2 newPoint)
    {
        spawnPoint = newPoint;
    }

    public void RespawnPlayer()
    {
        if (playerObject != null)
        {
            playerObject.transform.position = spawnPoint;

            var controller = playerObject.GetComponent<PlayerControllerMain>();
            var manager = playerObject.GetComponent<GameManager>();
            if (controller != null)
            {
                controller.ResetAfterRespawn();
                manager.OnPlayerRespawn();
            }
        }
        else
        {
            Debug.LogWarning("playerObject is null in PlayerSpawn — make sure it’s assigned.");
        }
    }
}