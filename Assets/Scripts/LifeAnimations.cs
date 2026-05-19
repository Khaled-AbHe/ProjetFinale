using System.Collections;
using UnityEngine;

// source: https://www.youtube.com/watch?v=odStG_LfPMQ 
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PlayerController))]
[RequireComponent(typeof(SpitShooter))]
public class LifeAnimations : MonoBehaviour
{
    private Animator animator;
    private Rigidbody2D rb;
    private PlayerController playerController;
    private SpitShooter spitShooter;

    void Awake()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        playerController = GetComponent<PlayerController>();
        spitShooter = GetComponent<SpitShooter>();
    }

    public void TriggerDeath(bool isGameOver = false)
    {
        StartCoroutine(Death(isGameOver));
    }

    public void TriggerSpawn()
    {
        StartCoroutine(Respawn());
    }

    IEnumerator Death(bool isGameOver)
    {
        SetPlayerController(false);

        animator.SetBool("IsDying", true);
        yield return new WaitForSeconds(GetClipLength("Death"));
        animator.SetBool("IsDying", false);

        if (isGameOver)
        {
            GameManager.Instance.ShowGameOverPanel();
        }
        else
        {
            GameManager.Instance.LoseLife();
        }
    }

    IEnumerator Respawn()
    {
        SetPlayerController(false);

        animator.SetBool("IsSpawning", true);
        yield return new WaitForSeconds(GetClipLength("Spawn"));
        animator.SetBool("IsSpawning", false);

        SetPlayerController(true);
    }

    // Helpers
    void SetPlayerController(bool state)
    {
        playerController.enabled = state;
        spitShooter.enabled = state;

        rb.linearVelocity = Vector2.zero;
        rb.bodyType = !state ? RigidbodyType2D.Kinematic : RigidbodyType2D.Dynamic;
    }

    float GetClipLength(string paramName)
    {
        foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips)
        {
            if (clip.name.Contains(paramName) || paramName.Contains(clip.name))
            {
                return clip.length;
            }
        }

        return 1f; // default
    }
}