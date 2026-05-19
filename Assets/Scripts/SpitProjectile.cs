using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class SpitProjectile : MonoBehaviour
{
    [Header("Settings")]
    public float lifetime = 5f;
    public float damage = 10f;

    [Header("Splash")]
    public GameObject splashPrefab;

    // Set by SpitShooter
    [HideInInspector] 
    public Vector2 launchVelocity;

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

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponent<SpitProjectile>() != null) return;

        EnemyEntity enemy = other.GetComponent<EnemyEntity>();
        enemy.TakeDamage(damage);
        
        Instantiate(splashPrefab, transform.position, Quaternion.identity);
        Destroy(gameObject);
    }
}