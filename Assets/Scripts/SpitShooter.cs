using UnityEngine;
using UnityEngine.InputSystem;

// source: https://www.youtube.com/watch?v=zYN1LTMdFYg
public class SpitShooter : MonoBehaviour
{
    [Header("References")]
    public GameObject spitPrefab;
    public TongueHook tongueHook;

    [Header("Shoot Settings")]
    public float spitSpeed = 15f;
    public float cooldown = 0.4f;
    public float damage = 10f;

    private float lastShotTime = -999f;

    void Update()
    {
        if (Time.timeScale == 0f) return;
        if (!Input.GetMouseButtonDown(0)) return;

        // Blocked while tongue is active
        if (tongueHook != null && (tongueHook.IsHooking || tongueHook.IsTravelling)) return;

        // Cooldown check
        if (Time.time - lastShotTime < cooldown) return;

        Shoot();
    }

    void Shoot()
    {
        if (spitPrefab == null) return;

        lastShotTime = Time.time;

        // Aim toward mouse
        Vector2 worldPosition = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        Vector2 direction  = (worldPosition - (Vector2) transform.position).normalized;

        // creates the bullet
        GameObject spitObj = Instantiate(spitPrefab, (Vector2) transform.position, Quaternion.identity);

        // Gets the SpitProjectile script 
        SpitProjectile spit = spitObj.GetComponent<SpitProjectile>();

        if (spit != null)
        {
            spit.damage = damage;
            spit.launchVelocity = direction * spitSpeed;

            // Gets the bullet collider
            Collider2D spitCollider = spitObj.GetComponent<Collider2D>();
            
            // Makes it ignore the player's collider
            foreach (Collider2D playerCollider in GetComponentsInChildren<Collider2D>())
            {
                Physics2D.IgnoreCollision(spitCollider, playerCollider);
            }
        }
    }
}