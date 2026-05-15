using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Persistent singleton that manages score, lives, and UI.
///
/// SETUP — move your HUD and panels onto this GameObject (or its children):
///   1. Create a Canvas as a child of the GameManager GameObject.
///   2. Move your HUD elements (score, lives, health slider) and panels
///      (Game Over, Pause) under that Canvas.
///   3. The Canvas + all UI will survive scene loads automatically via
///      DontDestroyOnLoad, so references never go stale.
///   4. Either "Screen Space - Overlay" or "Screen Space - Camera" work fine
///      for the Canvas render mode — Overlay is simplest.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Game Settings")]
    public int startingLives = 3;

    // ── HUD (assign children of THIS GameObject in the Inspector) ────────────
    [Header("HUD — must be children of this GameObject")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI livesText;
    public TextMeshProUGUI environmentText;
    public Slider healthSlider;

    [Header("Panels — must be children of this GameObject")]
    public GameObject gameOverPanel;
    public GameObject pausePanel;

    [Header("Level Description")]
    [TextArea]
    public string currentEnvironmentDescription;

    // ── State ────────────────────────────────────────────────────────────────
    private int score;
    private int lives;
    private bool isPaused;
    private bool isGameOver;
    private bool initialized;

    // Cached scene-object references (refreshed on each scene load)
    private HealthSystem cachedHealthSystem;
    private PlayerRespawn cachedPlayerRespawn;
    private GrappleHook cachedGrappleHook;

    // ── Unity Lifecycle ──────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // The whole GameObject (and all its UI children) persists across scenes.
        // Because the Canvas is a child, it travels with the GameManager and
        // references assigned in the Inspector are never invalidated by a reload.
        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Start()
    {
        if (!initialized)
        {
            lives       = startingLives;
            score       = 0;
            initialized = true;
        }

        HideAllPanels();
        UpdateUI();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && !isGameOver)
            TogglePause();
    }

    // ── Scene Loading ────────────────────────────────────────────────────────

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        CacheSceneReferences();

        if (cachedHealthSystem != null)
            cachedHealthSystem.ResetHealth();

        // Always hide panels on a fresh scene load (covers the restart path)
        HideAllPanels();
        UpdateUI();
    }

    /// <summary>
    /// Finds and caches frequently-needed scene objects once per scene load.
    /// </summary>
    private void CacheSceneReferences()
    {
        cachedHealthSystem  = FindFirstObjectByType<HealthSystem>();
        cachedPlayerRespawn = FindFirstObjectByType<PlayerRespawn>();
        cachedGrappleHook   = FindFirstObjectByType<GrappleHook>();
    }

    // ── Public API ───────────────────────────────────────────────────────────

    public void AddScore(int amount)
    {
        score += amount;
        UpdateUI();
    }

    public void LoseLife()
    {
        lives--;
        UpdateUI();

        if (lives <= 0)
            TriggerGameOver();
        else
            RespawnPlayer();
    }

    public void UpdateHealthUI(float currentHealth, float maxHealth)
    {
        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value    = currentHealth;
        }
    }

    public int GetLives() => lives;

    public void RestartLevel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void RestartGame()
    {
        // Reset state BEFORE loading the scene so OnSceneLoaded sees clean values
        isGameOver     = false;
        isPaused       = false;
        lives          = startingLives;
        score          = 0;
        Time.timeScale = 1f;

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        // UpdateUI() and HideAllPanels() are called by OnSceneLoaded after load
    }

    public void LoadScene(string sceneName)
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(sceneName);
    }

    // ── Private Helpers ──────────────────────────────────────────────────────

    private void RespawnPlayer()
    {
        // if (cachedGrappleHook != null)
        //     cachedGrappleHook
        // else
        //     Debug.LogWarning("GameManager: No GrappleHook found in scene — skipping release.");

        if (cachedPlayerRespawn != null)
            cachedPlayerRespawn.Respawn();
        else
            Debug.LogWarning("GameManager: No PlayerRespawn found in scene — player won't be repositioned.");
    }

    private void TriggerGameOver()
    {
        isGameOver     = true;
        Time.timeScale = 0f;

        // gameOverPanel is a child of this persistent GameObject,
        // so this reference is always valid regardless of how many times
        // the scene has been reloaded.
        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);
        else
            Debug.LogWarning("GameManager: gameOverPanel is not assigned — drag it into the Inspector.");

        Debug.Log("Game Over");
    }

    public void TogglePause()
    {
        isPaused       = !isPaused;
        Time.timeScale = isPaused ? 0f : 1f;

        if (pausePanel != null)
            pausePanel.SetActive(isPaused);
    }

    private void HideAllPanels()
    {
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (pausePanel != null)    pausePanel.SetActive(false);
    }

    private void UpdateUI()
    {
        if (scoreText != null)       scoreText.text       = "Score: " + score;
        if (livesText != null)       livesText.text       = "Lives: " + lives;
        if (environmentText != null) environmentText.text = currentEnvironmentDescription;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}