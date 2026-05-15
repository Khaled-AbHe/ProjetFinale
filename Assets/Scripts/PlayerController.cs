using UnityEngine;

/// <summary>
/// Hybrid 2D player controller built to work alongside the grapple hook.
///
/// Movement philosophy:
///   GROUNDED          — direct velocity control, full movement, tongue doesn't interfere.
///   AIR (no tongue)   — AddForce horizontally, physics drives vertical.
///   AIR (tongue, 0–0.5s after jump) — full air control via AddForce, transition window.
///   AIR (tongue, after window) — rigidbody left completely alone, pendulum physics take over.
///
/// Requires: Rigidbody2D, Collider2D on the player GameObject.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed       = 5f;
    public float jumpForce       = 10f;

    [Header("Air Control")]
    [Tooltip("Horizontal force applied in the air when the tongue is not active.")]
    public float airForce        = 40f;

    [Tooltip("Maximum horizontal speed the player can reach via air control.")]
    public float maxAirSpeed     = 5f;

    [Tooltip("How long after jumping (while tongue is active) the player keeps full air control.")]
    public float swingWindowDuration = 0.5f;

    [Header("Ground Detection")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayer;

    [Header("References")]
    [Tooltip("Drag the GrappleHook component here.")]
    public GrappleHook grappleHook;

    // ── Private ──────────────────────────────────────────────────────────────
    private Rigidbody2D rb;
    private Animator    animator;
    private Vector3     originalScale;

    private bool  isGrounded;
    private float horizontalInput;

    // Tracks the moment the player jumped while tongue was active
    private float swingWindowTimer = 0f;
    private bool  inSwingWindow    = false;

    // ── Unity Lifecycle ──────────────────────────────────────────────────────

    void Start()
    {
        rb            = GetComponent<Rigidbody2D>();
        animator      = GetComponent<Animator>();
        originalScale = transform.localScale;
    }

    void Update()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");

        bool tongueActive = IsTongueActive();

        // ── Jump ─────────────────────────────────────────────────────────────
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);

            // If the tongue is active when we jump, start the control window
            if (tongueActive)
            {
                inSwingWindow    = true;
                swingWindowTimer = swingWindowDuration;
            }
        }

        // ── Swing window countdown ────────────────────────────────────────────
        if (inSwingWindow)
        {
            swingWindowTimer -= Time.deltaTime;
            if (swingWindowTimer <= 0f)
                inSwingWindow = false;
        }

        // Reset window if player lands
        if (isGrounded)
            inSwingWindow = false;

        // ── Sprite flip ───────────────────────────────────────────────────────
        if (horizontalInput != 0)
        {
            transform.localScale = new Vector3(
                Mathf.Abs(originalScale.x) * Mathf.Sign(horizontalInput),
                originalScale.y,
                originalScale.z
            );
        }

        // ── Animations ────────────────────────────────────────────────────────
        if (animator != null)
        {
            animator.SetFloat("Speed",      Mathf.Abs(horizontalInput));
            animator.SetBool("IsGrounded",  isGrounded);
            animator.SetBool("IsGrappling", tongueActive);
        }
    }

    void FixedUpdate()
    {
        // Ground check
        if (groundCheck != null)
            isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

        bool tongueActive   = IsTongueActive();
        bool pendulumActive = tongueActive && !isGrounded && !inSwingWindow;

        if (isGrounded)
        {
            // ── Grounded: direct velocity control ────────────────────────────
            // Full responsiveness on the ground regardless of tongue state.
            rb.linearVelocity = new Vector2(horizontalInput * moveSpeed, rb.linearVelocity.y);
        }
        else if (pendulumActive)
        {
            // ── Swinging: hands off the rigidbody completely ──────────────────
            // The DistanceJoint2D drives all movement. Any velocity override here
            // would fight the joint and break the pendulum feel.
            // Do nothing.
        }
        else
        {
            // ── Air (no tongue, or inside swing window): force-based control ──
            // AddForce so we don't stomp physics velocity, just nudge horizontally.
            // Clamped so the player can't accelerate indefinitely.
            float currentHorizontal = rb.linearVelocity.x;
            bool  underMaxSpeed     = Mathf.Abs(currentHorizontal) < maxAirSpeed
                                      || Mathf.Sign(currentHorizontal) != Mathf.Sign(horizontalInput);

            if (horizontalInput != 0 && underMaxSpeed)
                rb.AddForce(new Vector2(horizontalInput * airForce, 0f), ForceMode2D.Force);
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns true if the tongue is either travelling or attached.
    /// Both states require the pendulum to be left alone once the window expires.
    /// </summary>
    bool IsTongueActive()
    {
        return grappleHook != null &&
               (grappleHook.IsGrappling || grappleHook.IsTravelling);
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}