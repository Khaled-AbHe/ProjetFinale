using System.Collections;
using UnityEngine;
[RequireComponent(typeof(Rigidbody2D))]

[RequireComponent(typeof(LineRenderer))]

public class TongueHook : MonoBehaviour
{
    // ── Grapple Settings ──────────────────────────────────────────────────────

    [Header("Grapple Settings")]
    public float maxLength = 10f;
    public float pullSpeed = 3f;
    public float extendSpeed = 3f;
    public float fireCooldown = 0.5f;
    public float releaseBoostMultiplier = 0.3f;
    public LayerMask grappleLayer;

    [Header("Tongue Visual")]
    public LineRenderer lineRenderer;
    public Transform tongueTip;
    public Vector2 tongueOriginOffset = Vector2.zero;

    private Rigidbody2D rb;
    private DistanceJoint2D joint;

    private bool isHooking  = false;
    private bool isTravelling = false;
    private bool isGrounded   = false;
    private bool wasGrounded  = false; // this is used to prevent constant joint creation
    private bool isTongueVisible = false;

    private Vector2 anchorPoint;
    private Vector2 travelTip;
    private Coroutine travelCoroutine;

    private const float InputCooldown = 0.1f;
    private float lastFireTime = -999f;
    private bool IsTongueActive => isHooking || isTravelling;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = 2;
        lineRenderer.enabled = false;

        tongueTip.gameObject.SetActive(false);
    }

    void Update()
    {
        if (Time.timeScale == 0f) return;
        HandleFireInput();
    }

    void FixedUpdate()
    {
        HandleGroundedTransitions();
        HandlePullExtend();
        HandleGroundedMaxLength();
    }

    void LateUpdate()
    {
        UpdateVisuals();
    }


    // Called by PlayerController
    public void NotifyGrounded(bool grounded)
    {
        isGrounded = grounded;
    }

    void HandleGroundedTransitions()
    {
        if (!isHooking)
        {
            wasGrounded = isGrounded;
            return;
        }

        if (wasGrounded && !isGrounded)
        {
            CreateJoint(); // creates join the moment the player is not grounded
        }
        else if (!wasGrounded && isGrounded)
        {
            DestroyJoint(); // destroys joint the moment the player is grounded
        }

        wasGrounded = isGrounded;
    }

    // Locks the tongue when player goes too far.
    void HandleGroundedMaxLength()
    {
        if (!isHooking || joint != null || !isGrounded) return;

        if (Vector2.Distance(transform.position, anchorPoint) >= maxLength) CreateJoint();
    }

    // Input
    void HandleFireInput()
    {
        bool holdStarted = Input.GetMouseButtonDown(1);
        bool holdEnded = Input.GetMouseButtonUp(1);

        // Fire on initial press
        if (holdStarted && !IsTongueActive)
        {
            if (Time.time - lastFireTime >= InputCooldown && Time.time - lastFireTime >= fireCooldown)
            {
                lastFireTime = Time.time;
                TryFireGrapple();
            }
        }

        // Release
        if (holdEnded && IsTongueActive)
        {
            ReleaseGrapple();
        }
    }

    void HandlePullExtend()
    {
        if (!isHooking || joint == null) return;

        if (Input.GetKey(KeyCode.W))
            joint.distance = Mathf.Max(1f, joint.distance - pullSpeed   * Time.fixedDeltaTime);
        else if (Input.GetKey(KeyCode.S))
            joint.distance = Mathf.Min(maxLength, joint.distance + extendSpeed * Time.fixedDeltaTime);
    }

    // Grapple Logic
    void TryFireGrapple()
    {
        Vector2 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 direction = (mouseWorldPos - (Vector2)transform.position).normalized;

        RaycastHit2D hit = Physics2D.Raycast(transform.position, direction, maxLength, grappleLayer);

        if (hit.collider == null) { return; }

        anchorPoint = hit.point;
        travelCoroutine = StartCoroutine(TongueTravelRoutine(anchorPoint));
    }

    IEnumerator TongueTravelRoutine(Vector2 target)
    {
        isTravelling = true;
        travelTip = (Vector2)transform.position + tongueOriginOffset;

        SetTongueVisible(true);

        while (Vector2.Distance(travelTip, target) > 0.05f)
        {
            travelTip = Vector2.MoveTowards(travelTip, target, 50f * Time.deltaTime);
            yield return null;
        }

        isTravelling = false;
        AttachTongue(target);
    }

    void AttachTongue(Vector2 anchor)
    {
        anchorPoint = anchor;
        isHooking = true;

        // Creates the joint immediately if already airborne
        if (!isGrounded) CreateJoint();
    }

    void CreateJoint()
    {
        if (joint != null) return;

        joint = gameObject.AddComponent<DistanceJoint2D>();
        joint.connectedAnchor = anchorPoint;
        joint.distance = Vector2.Distance(transform.position, anchorPoint);
        joint.maxDistanceOnly = true;
    }

    void DestroyJoint()
    {
        if (joint != null)
        {
            Destroy(joint);
            joint = null;
        }
    }

    public void ReleaseGrapple()
    {
        if (travelCoroutine != null)
        {
            StopCoroutine(travelCoroutine);
            travelCoroutine = null;
        }

        if (isHooking && joint != null && releaseBoostMultiplier > 0f)
        {
            Vector2 vel = rb.linearVelocity;
            if (vel.sqrMagnitude > 0.01f)
                rb.AddForce(vel.normalized * vel.magnitude * releaseBoostMultiplier,
                            ForceMode2D.Impulse);
        }

        DestroyJoint();

        isHooking  = false;
        isTravelling = false;

        SetTongueVisible(false);
    }

    // Visuals
    void UpdateVisuals()
    {
        if (!IsTongueActive)
        {
            // avoids spam
            if (isTongueVisible) SetTongueVisible(false);
            return;
        }

        Vector2 origin = (Vector2)transform.position + tongueOriginOffset;
        Vector2 target = isTravelling ? travelTip : anchorPoint;

        lineRenderer.SetPosition(0, origin);
        lineRenderer.SetPosition(1, target);

        if (tongueTip != null)  tongueTip.position = target;
    }

    void SetTongueVisible(bool visible)
    {
        if (isTongueVisible == visible) return;

        isTongueVisible = visible;
        lineRenderer.enabled = visible;

        tongueTip.gameObject.SetActive(visible);
    }

    public bool IsHooking => isHooking;
    public bool IsTravelling => isTravelling;
    public Vector2 AnchorPoint => anchorPoint;
}