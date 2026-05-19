using UnityEngine;

public class PlayerRespawn : MonoBehaviour
{
    [Header("Respawn")]
    public Transform spawnPoint;

    private Rigidbody2D rb;
    private HealthSystem healthSystem;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        healthSystem = GetComponent<HealthSystem>();
    }

    public void Respawn()
    {
        if (spawnPoint != null)
        {
            transform.position = spawnPoint.position;
        }

        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        healthSystem.ResetHealth();
    }
}