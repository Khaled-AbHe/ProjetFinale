using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float jumpForce = 10f;

    [Header("Air Control")]
    public float airForce = 40f;

    [Tooltip("Maximum horizontal speed the player can reach via air control.")]
    public float maxAirSpeed = 5f;

    [Tooltip("How long after jumping (while tongue is active) the player keeps full air control.")]
    public float swingWindowDuration = 0.5f;

    [Header("Ground Detection")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayer;

    [Header("References")]
    public TongueHook tongueHook;

    private Rigidbody2D rb;
    private Animator animator;
    private Vector3 originalScale;

    private bool  isGrounded;
    private float horizontalInput;

    private float swingWindowTimer = 0f;
    private bool  inSwingWindow = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        originalScale = transform.localScale;
    }

    void Update()
    {
        if (Time.timeScale == 0f) return;
        horizontalInput = Input.GetAxisRaw("Horizontal");

        bool tongueActive = IsTongueActive();

        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);

            // If the tongue is active when we jump, start the control window
            if (tongueActive)
            {
                inSwingWindow = true;
                swingWindowTimer = swingWindowDuration;
            }
        }

        if (inSwingWindow)
        {
            swingWindowTimer -= Time.deltaTime;
            if (swingWindowTimer <= 0f) inSwingWindow = false;
        }

        if (isGrounded) inSwingWindow = false;

        // Flip
        if (horizontalInput != 0)
        {
            transform.localScale = new Vector3(
                Mathf.Abs(originalScale.x) * Mathf.Sign(horizontalInput),
                originalScale.y,
                originalScale.z
            );
        }

        animator.SetFloat("Speed", Mathf.Abs(horizontalInput));
        animator.SetBool("IsGrounded", isGrounded);
        animator.SetBool("IsHooking", tongueActive);
    }

    void FixedUpdate()
    {
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        tongueHook.NotifyGrounded(isGrounded);

        bool tongueActive = IsTongueActive();
        bool pendulumActive = tongueActive && !isGrounded && !inSwingWindow;

        if (isGrounded) // normal movement
        {
            rb.linearVelocity = new Vector2(horizontalInput * moveSpeed, rb.linearVelocity.y);
        }
        else if (pendulumActive)
        {
            // do nothing
        }
        else // while airbone
        {
            float currentHorizontal = rb.linearVelocity.x;
            bool  underMaxSpeed = Mathf.Abs(currentHorizontal) < maxAirSpeed || Mathf.Sign(currentHorizontal) != Mathf.Sign(horizontalInput);

            if (horizontalInput != 0 && underMaxSpeed) rb.AddForce(new Vector2(horizontalInput * airForce, 0f), ForceMode2D.Force);
        }
    }

    // Helpers
    bool IsTongueActive()
    {
        return tongueHook.IsHooking || tongueHook.IsTravelling;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
}