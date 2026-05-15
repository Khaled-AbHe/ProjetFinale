using UnityEngine;

/// <summary>
/// Deals damage to the player on contact.
/// Attach to any enemy or hazard GameObject with a Collider2D.
/// The player GameObject must be tagged "Player" and have a HealthSystem component.
/// </summary>
public class InstantDeath : MonoBehaviour
{
    [Header("Damage")]
    [Tooltip("Amount of damage dealt to the player on each contact.")]
    public float damageAmount = 25f;

    public void OnCollisionEnter2D(Collision2D collision)
    {
        if (!collision.gameObject.CompareTag("Player")) return;

        HealthSystem health = collision.gameObject.GetComponent<HealthSystem>();

        if (health != null)
        {
            health.TakeDamage(damageAmount, transform.position);
        }
        else
        {
            Debug.LogWarning($"InstantDeath: Player object '{collision.gameObject.name}' " +
                             "is missing a HealthSystem component.");
        }
    }
}
