using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class FadeTransition : MonoBehaviour
{
    public static FadeTransition Instance { get; private set; }

    [Header("Fade Image")]
    public Image fadeImage;

    private float fadeDuration = 0.5f;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
    }

    void Start()
    {
        // Ensure the overlay starts fully transparent
        fadeImage.color = new Color(0f, 0f, 0f, 0f);
    }

    public void FadeOut(Action onComplete)
    {
        StartCoroutine(FadeRoutine(0f, 1f, onComplete));
    }

    public void FadeIn()
    {
        StartCoroutine(FadeRoutine(1f, 0f, null));
    }

    IEnumerator FadeRoutine(float from, float to, Action onComplete)
    {
        float elapsed = 0f;
        fadeImage.color = new Color(0f, 0f, 0f, from);

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float alpha = Mathf.Lerp(from, to, elapsed / fadeDuration);
            fadeImage.color = new Color(0f, 0f, 0f, alpha);
            yield return null;
        }

        fadeImage.color = new Color(0f, 0f, 0f, to);
        onComplete?.Invoke();
    }
}
