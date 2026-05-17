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
    private int  score;
    private int  lives;
    private bool isPaused;
    private bool isGameOver;

    // Cached scene-object references (refreshed on each scene load)
    private HealthSystem  cachedHealthSystem;
    private PlayerRespawn cachedPlayerRespawn;
    private TongueHook             cachedTongueHook;
    private PlayerAnimationEvents  cachedAnimEvents;

    // ── Unity Lifecycle ──────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;

        // Initialize state in Awake — this only ever runs once on the
        // persistent object, so no initialized flag needed.
        lives = startingLives;
        score = 0;
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

        // Set environment text once per scene load rather than on every UI update.
        if (environmentText != null)
            environmentText.text = currentEnvironmentDescription;

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
        cachedTongueHook    = FindFirstObjectByType<TongueHook>();
        cachedAnimEvents    = FindFirstObjectByType<PlayerAnimationEvents>();
    }

    // ── Public API ───────────────────────────────────────────────────────────

    public void AddScore(int amount)
    {
        score += amount;
        UpdateUI();
    }

    public void LoseLife()
    {
        if (isGameOver) return;  // guard against double-calls before game over completes

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
        // Release tongue
        if (cachedTongueHook != null)
            cachedTongueHook.ReleaseGrapple();

        // Destroy all active spit projectiles
        foreach (SpitProjectile spit in FindObjectsByType<SpitProjectile>(FindObjectsSortMode.None))
            Destroy(spit.gameObject);

        if (cachedPlayerRespawn != null)
            cachedPlayerRespawn.Respawn();
        else
            Debug.LogWarning("GameManager: No PlayerRespawn found in scene — player won't be repositioned.");

        // Play spawn animation and re-enable player control when done
        if (cachedAnimEvents != null)
            cachedAnimEvents.TriggerSpawn();
    }

    public void TriggerGameOver()
    {
        isGameOver = true;

        if (cachedAnimEvents != null)
        {
            cachedAnimEvents.TriggerDeath(isGameOver: true);
            return; // ShowGameOverPanel() will be called by PlayerAnimationEvents after animation
        }

        ShowGameOverPanel(); // fallback if PlayerAnimationEvents is missing
    }

    public void ShowGameOverPanel()
    {
        Time.timeScale = 0f;

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
        if (pausePanel    != null) pausePanel.SetActive(false);
    }

    private void UpdateUI()
    {
        if (scoreText != null) scoreText.text = "Score: " + score;
        if (livesText != null) livesText.text = "Lives: " + lives;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}