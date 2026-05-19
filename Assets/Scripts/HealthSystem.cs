using System.Collections;
using UnityEngine;

[RequireComponent(typeof(LifeAnimations))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
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
        spriteRenderer = GetComponent<SpriteRenderer>();
        ResetHealth();
    }

    public void TakeDamage(float amount)
    {
        if (isInvincible || currentHealth <= 0)
        {
            return;
        }

        currentHealth -= amount;
        // makes sure it doesnt go below 0 
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);

        OnDamaged?.Invoke(currentHealth);

        GameManager.Instance.UpdateHealthUI(currentHealth, maxHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            StartCoroutine(InvincibilityFrames());
        }
    }

    public void ResetHealth()
    {
        currentHealth = maxHealth;
        GameManager.Instance.UpdateHealthUI(currentHealth, maxHealth);
    }

    private void Die()
    {
        OnDeath?.Invoke();

        LifeAnimations lifeAnimations = GetComponent<LifeAnimations>();
        lifeAnimations.TriggerDeath();
    }

    private IEnumerator InvincibilityFrames()
    {
        isInvincible = true;

        float elapsed = 0f;

        while (elapsed < invincibilityDuration)
        {
            spriteRenderer.enabled = !spriteRenderer.enabled;
            yield return new WaitForSeconds(flashInterval);
            elapsed += flashInterval;
        }

        spriteRenderer.enabled = true;
        isInvincible = false;
    }
}