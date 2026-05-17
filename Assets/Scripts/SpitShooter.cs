using UnityEngine;

/// <summary>
/// Attach this to the frog player GameObject alongside PlayerController.
///
/// SETUP:
///   1. Add this script to the frog.
///   2. Drag your SpitProjectile prefab into the spitPrefab field.
///   3. Drag the TongueHook component into the tongueHook field
///      (same reference as in PlayerController).
///   4. Tweak mouthOffset to match the frog's mouth position in local space.
///   5. Adjust spitSpeed, cooldown, and damage to taste.
/// </summary>
public class SpitShooter : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The spit projectile prefab (must have SpitProjectile + Rigidbody2D + Collider2D).")]
    public GameObject spitPrefab;

    [Tooltip("Same TongueHook reference used by PlayerController.")]
    public TongueHook tongueHook;

    [Header("Shoot Settings")]
    [Tooltip("Launch speed of the spit ball (units/sec). Gravity will curve it after launch.")]
    public float spitSpeed = 15f;

    [Tooltip("Seconds between shots.")]
    public float cooldown = 0.4f;

    [Tooltip("Damage dealt on hit. Passed to SpitProjectile at spawn time.")]
    public float damage = 10f;

    [Header("Spawn Offset")]
    [Tooltip("Local-space offset from the frog's centre where spit spawns. " +
             "Positive X = right; adjust Y to match the mouth height.")]
    public Vector2 mouthOffset = new Vector2(0.3f, 0.1f);

    // ── Private ───────────────────────────────────────────────────────────────

    private float lastShotTime = -999f;

    void Update()
    {
        if (Time.timeScale == 0f) return;
        if (!Input.GetMouseButtonDown(0)) return;   // LMB only

        // Blocked while tongue is active
        if (tongueHook != null && (tongueHook.IsHooking || tongueHook.IsTravelling)) return;

        // Cooldown check
        if (Time.time - lastShotTime < cooldown) return;

        Shoot();
    }

    void Shoot()
    {
        if (spitPrefab == null)
        {
            Debug.LogWarning("SpitShooter: spitPrefab is not assigned.");
            return;
        }

        lastShotTime = Time.time;

        // ── Spawn position ────────────────────────────────────────────────────
        // Flip X offset to match the sprite's facing direction.
        float facingSign = Mathf.Sign(transform.localScale.x);
        Vector2 spawnOffset = new Vector2(mouthOffset.x * facingSign, mouthOffset.y);
        Vector2 spawnPos    = (Vector2)transform.position + spawnOffset;

        // ── Aim toward mouse ──────────────────────────────────────────────────
        Vector2 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 direction  = (mouseWorld - spawnPos).normalized;

        // ── Instantiate & launch ──────────────────────────────────────────────
        GameObject spitObj  = Instantiate(spitPrefab, spawnPos, Quaternion.identity);
        SpitProjectile spit = spitObj.GetComponent<SpitProjectile>();

        if (spit != null)
        {
            spit.damage         = damage;         // override prefab default with shooter value
            spit.launchVelocity = direction * spitSpeed;

            // Make the spit harmless to the frog that fired it.
            // Ignores all colliders on this GameObject (body, ground check, etc.)
            Collider2D spitCollider = spitObj.GetComponent<Collider2D>();
            if (spitCollider != null)
            {
                foreach (Collider2D playerCol in GetComponentsInChildren<Collider2D>())
                    Physics2D.IgnoreCollision(spitCollider, playerCol);
            }
        }
    }
}