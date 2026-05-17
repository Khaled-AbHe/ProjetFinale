using System.Collections;
using UnityEngine;

public class HealthSystem : MonoBehaviour
{
    [Header("Health Settings")]
    public float maxHealth = 100f;
    public float currentHealth;

    [Header("Damage Settings")]
    public float invincibilityDuration = 1.5f;

    [Header("Knockback")]
    public float knockbackForce = 5f;

    [Header("Visual Feedback")]
    public SpriteRenderer spriteRenderer;
    public float flashInterval = 0.1f;

    private bool isInvincible;
    private Rigidbody2D rb;

    public System.Action<float> OnDamaged;
    public System.Action OnDeath;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        ResetHealth();
    }

    public void TakeDamage(float amount, Vector2? damageSourcePosition = null)
    {
        if (isInvincible || currentHealth <= 0)
            return;

        currentHealth -= amount;

        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);

        OnDamaged?.Invoke(currentHealth);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.UpdateHealthUI(currentHealth, maxHealth);
        }

        if (damageSourcePosition.HasValue && rb != null)
        {
            Vector2 knockDir =
                ((Vector2)transform.position - damageSourcePosition.Value).normalized;

            rb.linearVelocity = Vector2.zero;

            rb.AddForce(knockDir * knockbackForce, ForceMode2D.Impulse);
        }

        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            StartCoroutine(InvincibilityFrames());
        }
    }

    public void Heal(float amount)
    {
        currentHealth += amount;

        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.UpdateHealthUI(currentHealth, maxHealth);
        }
    }

    public void ResetHealth()
    {
        currentHealth = maxHealth;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.UpdateHealthUI(currentHealth, maxHealth);
        }
    }

    public float GetCurrentHealth()
    {
        return currentHealth;
    }

    public float GetMaxHealth()
    {
        return maxHealth;
    }

    private void Die()
    {
        OnDeath?.Invoke();

        // Delegate to PlayerAnimationEvents so the death animation plays
        // before GameManager.LoseLife() is called.
        PlayerAnimationEvents animEvents = GetComponent<PlayerAnimationEvents>();
        if (animEvents != null)
            animEvents.TriggerDeath();
        else if (GameManager.Instance != null)
            GameManager.Instance.LoseLife(); // fallback if script is missing

        Debug.Log("Player died");
    }

    private IEnumerator InvincibilityFrames()
    {
        isInvincible = true;

        float elapsed = 0f;

        while (elapsed < invincibilityDuration)
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = !spriteRenderer.enabled;
            }

            yield return new WaitForSeconds(flashInterval);

            elapsed += flashInterval;
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
        }

        isInvincible = false;
    }
}