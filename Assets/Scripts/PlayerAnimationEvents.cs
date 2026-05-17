using System.Collections;
using UnityEngine;

/// <summary>
/// Handles player death and spawn animations, freezing the player during each.
/// Sits between HealthSystem (which detects death) and GameManager (which handles
/// lives and respawn) so both can be paused while animations play.
///
/// SETUP:
///   1. Add this script to the frog player alongside PlayerController.
///   2. Make sure your Animator has bool parameters: "IsDying" and "IsSpawning".
///   3. In HealthSystem's Die(), call GetComponent<PlayerAnimationEvents>().TriggerDeath()
///      instead of GameManager.Instance.LoseLife() directly.
///   4. In GameManager.RespawnPlayer(), call playerAnimEvents.TriggerSpawn()
///      after Respawn() repositions the player.
///   5. In GameManager.TriggerGameOver(), call TriggerDeath(isGameOver: true)
///      so it plays the animation but skips calling LoseLife at the end.
/// </summary>
[RequireComponent(typeof(Animator))]
public class PlayerAnimationEvents : MonoBehaviour
{
    // ── Private refs ──────────────────────────────────────────────────────────
    private Animator         animator;
    private Rigidbody2D      rb;
    private PlayerController playerController;
    private SpitShooter      spitShooter;

    void Awake()
    {
        animator         = GetComponent<Animator>();
        rb               = GetComponent<Rigidbody2D>();
        playerController = GetComponent<PlayerController>();
        spitShooter      = GetComponent<SpitShooter>();
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Freezes the player, plays the death animation, then either calls
    /// GameManager.LoseLife() (normal death) or just shows the game over panel
    /// (when isGameOver is true).
    /// </summary>
    public void TriggerDeath(bool isGameOver = false)
    {
        StartCoroutine(DeathRoutine(isGameOver));
    }

    /// <summary>
    /// Plays the spawn animation then re-enables player control.
    /// Call this from GameManager after the player has been repositioned.
    /// </summary>
    public void TriggerSpawn()
    {
        StartCoroutine(SpawnRoutine());
    }

    // ── Routines ──────────────────────────────────────────────────────────────

    IEnumerator DeathRoutine(bool isGameOver)
    {
        // Freeze the player
        SetPlayerFrozen(true);

        // Trigger death animation
        animator.SetBool("IsDying", true);
        Debug.Log($"PlayerAnimationEvents: IsDying set to true. Clip length: {GetClipLength("Death")}s");

        // Wait for the clip to finish
        yield return new WaitForSeconds(GetClipLength("Death"));

        animator.SetBool("IsDying", false);

        // Hand off to GameManager
        if (GameManager.Instance != null)
        {
            if (isGameOver)
                GameManager.Instance.ShowGameOverPanel();
            else
                GameManager.Instance.LoseLife();
        }
    }

    IEnumerator SpawnRoutine()
    {
        // Make sure player stays frozen during spawn animation
        SetPlayerFrozen(true);

        animator.SetBool("IsSpawning", true);

        yield return new WaitForSeconds(GetClipLength("Spawn"));

        animator.SetBool("IsSpawning", false);

        // Unfreeze now that spawn animation is done
        SetPlayerFrozen(false);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Freezes or unfreezes player input and physics.
    /// </summary>
    void SetPlayerFrozen(bool frozen)
    {
        if (playerController != null) playerController.enabled = !frozen;
        if (spitShooter      != null) spitShooter.enabled      = !frozen;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.bodyType       = frozen ? RigidbodyType2D.Kinematic : RigidbodyType2D.Dynamic;
        }
    }

    /// <summary>
    /// Finds the length of the first animation clip that matches the given
    /// animator parameter name. Falls back to 1f if no match is found.
    /// </summary>
    float GetClipLength(string paramName)
    {
        // Normalize the param name to match clip naming conventions
        // e.g. "IsDying" -> looks for a clip with "Die" or "Death" or "IsDying" in its name
        foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips)
        {
            if (clip.name.Contains(paramName) ||
                paramName.Contains(clip.name))
                return clip.length;
        }

        Debug.LogWarning($"PlayerAnimationEvents: Could not find clip matching '{paramName}'. Defaulting to 1 second.");
        return 1f;
    }
}