using UnityEngine;

/// <summary>
/// Attach this to your spit prefab.
///
/// SETUP:
///   1. Create a new GameObject (e.g. a small circle sprite), name it "SpitProjectile".
///   2. Add a Rigidbody2D — leave gravity scale at 1 (the arc is pure physics).
///   3. Add a CircleCollider2D — tick "Is Trigger".
///   4. Add this script.
///   5. Set the prefab's layer to "Projectile" and in Physics 2D settings,
///      disable Projectile <-> Player collision so it doesn't hit the frog.
///   6. Drag the prefab into SpitShooter's spitPrefab field.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class SpitProjectile : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Seconds before the projectile destroys itself if it hits nothing.")]
    public float lifetime = 5f;

    [Tooltip("Damage dealt to anything with a HealthSystem on hit.")]
    public float damage = 10f;

    [Header("Splash")]
    [Tooltip("SpitSplash prefab spawned at the impact point. Leave empty for no effect.")]
    public GameObject splashPrefab;

    // Set by SpitShooter immediately after Instantiate.
    [HideInInspector] public Vector2 launchVelocity;

    private Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Start()
    {
        rb.linearVelocity = launchVelocity;
        Destroy(gameObject, lifetime);
    }

    // Using OnTriggerEnter2D — make sure the collider has "Is Trigger" ticked.
    void OnTriggerEnter2D(Collider2D other)
    {
        // Ignore other projectiles
        if (other.GetComponent<SpitProjectile>() != null) return;

        // Try player health first, then enemy health
        HealthSystem health = other.GetComponent<HealthSystem>();
        if (health != null)
        {
            health.TakeDamage(damage, transform.position);
        }
        else
        {
            EnemyEntity enemy = other.GetComponent<EnemyEntity>();
            if (enemy != null)
                enemy.TakeDamage(damage, transform.position);
        }

        if (splashPrefab != null)
            Instantiate(splashPrefab, transform.position, Quaternion.identity);

        Destroy(gameObject);
    }
}