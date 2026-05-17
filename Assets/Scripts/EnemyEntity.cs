using UnityEngine;

/// <summary>
/// Simple enemy that patrols between two points, optionally chases the player,
/// and has its own health system (instant death on reaching 0 HP).
///
/// SETUP:
///   1. Add this script to your enemy prefab.
///   2. Create two empty GameObjects in the scene as patrol boundaries,
///      assign them to pointA and pointB.
///   3. Set maxHealth, patrolSpeed, chaseSpeed, detectionRange to taste.
///   4. Assign playerLayer to your Player layer.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class EnemyEntity : MonoBehaviour
{
    [Header("Patrol Points")]
    public Transform pointA;
    public Transform pointB;

    [Header("Movement")]
    public float patrolSpeed = 2f;
    public float chaseSpeed  = 3.5f;
    public float waitTime    = 0.5f;

    [Header("Player Detection")]
    public float     detectionRange = 4f;
    public LayerMask playerLayer;
    public bool      canChase       = true;

    [Header("Health")]
    public float maxHealth     = 30f;
    public float contactDamage = 10f;

    [Header("Hit Flash")]
    public float flashDuration = 0.15f;

    [Header("Settings")]
    public int scoreValue = 1;

    // ── Private state ─────────────────────────────────────────────────────────
    private Rigidbody2D    rb;
    private Vector3        originalScale;
    private Transform      player;
    private SpriteRenderer spriteRenderer;
    private Animator       animator;

    private float currentHealth;

    // Patrol
    private Transform patrolTarget;
    private bool      isWaiting;
    private float     waitTimer;

    // ── Unity Lifecycle ───────────────────────────────────────────────────────

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 1f;
        rb.constraints  = RigidbodyConstraints2D.FreezeRotation;

        originalScale = transform.localScale;
        currentHealth = maxHealth;
        patrolTarget  = pointB;
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator       = GetComponent<Animator>();

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) player = playerObj.transform;
    }

    void FixedUpdate()
    {
        bool playerInRange = player != null &&
                             Vector2.Distance(transform.position, player.position) <= detectionRange;

        if (canChase && playerInRange)
            ChasePlayer();
        else
            Patrol();
    }

    // ── Patrol ────────────────────────────────────────────────────────────────

    void Patrol()
    {
        if (pointA == null || pointB == null) return;

        if (isWaiting)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            waitTimer -= Time.fixedDeltaTime;
            if (waitTimer <= 0f) isWaiting = false;
            if (animator != null) animator.SetBool("IsMoving", false);
            return;
        }

        float toTarget = patrolTarget.position.x - transform.position.x;

        if (Mathf.Abs(toTarget) <= 0.05f)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            if (animator != null) animator.SetBool("IsMoving", false);
            patrolTarget      = (patrolTarget == pointA) ? pointB : pointA;
            isWaiting         = true;
            waitTimer         = waitTime;
            return;
        }

        MoveToward(patrolTarget.position, patrolSpeed);
    }

    void ChasePlayer()
    {
        MoveToward(player.position, chaseSpeed);
    }

    void MoveToward(Vector3 destination, float speed)
    {
        float dir         = Mathf.Sign(destination.x - transform.position.x);
        rb.linearVelocity = new Vector2(dir * speed, rb.linearVelocity.y);

        transform.localScale = new Vector3(
            Mathf.Abs(originalScale.x) * dir,
            originalScale.y,
            originalScale.z
        );

        if (animator != null) animator.SetBool("IsMoving", true);
    }

    // ── Player Contact Damage ─────────────────────────────────────────────────

    void OnCollisionEnter2D(Collision2D collision)
    {
        HealthSystem playerHealth = collision.gameObject.GetComponent<HealthSystem>();
        if (playerHealth != null)
            playerHealth.TakeDamage(contactDamage, transform.position);
    }

    // ── Health ────────────────────────────────────────────────────────────────

    public void TakeDamage(float amount, Vector2? damageSourcePosition = null)
    {
        if (currentHealth <= 0) return;

        currentHealth -= amount;

        if (currentHealth <= 0)
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.AddScore(scoreValue);
            }

            Destroy(gameObject);
            return;
        }

        if (spriteRenderer != null)
            StartCoroutine(FlashRed());
    }

    System.Collections.IEnumerator FlashRed()
    {
        spriteRenderer.color = Color.red;
        yield return new WaitForSeconds(flashDuration);
        if (spriteRenderer != null)
            spriteRenderer.color = Color.white;
    }

    // ── Gizmos ────────────────────────────────────────────────────────────────

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        if (pointA != null && pointB != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(pointA.position, pointB.position);
            Gizmos.DrawSphere(pointA.position, 0.15f);
            Gizmos.DrawSphere(pointB.position, 0.15f);
        }
    }
}