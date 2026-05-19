using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Game Settings")]
    public int startingLives = 3;

    [Header("HUD")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI livesText;
    public Slider healthSlider;

    [Header("Panels")]
    public GameObject gameOverPanel;
    public GameObject pausePanel;
    public GameObject menuPanel;
    public GameObject victoryPanel;

    private int  score;
    private int  lives;
    private bool isPaused;
    private bool isGameOver;

    private HealthSystem cachedHealthSystem;
    private PlayerRespawn cachedPlayerRespawn;
    private TongueHook cachedTongueHook;
    private LifeAnimations cachedLifeAnimations;

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

        lives = startingLives;
        score = 0;
    }

    private void Update()
    {
        if (SceneManager.GetActiveScene().buildIndex == 0) return;
        if (Input.GetKeyDown(KeyCode.Escape) && !isGameOver) TogglePause();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        CacheSceneReferences();

        // Fade in from black on every scene load
        FadeTransition.Instance.FadeIn();
        if (cachedHealthSystem != null) cachedHealthSystem.ResetHealth();

        HideAllPanels();
        UpdateUI();
    }

    private void CacheSceneReferences()
    {
        cachedHealthSystem = FindFirstObjectByType<HealthSystem>();
        cachedPlayerRespawn = FindFirstObjectByType<PlayerRespawn>();
        cachedTongueHook = FindFirstObjectByType<TongueHook>();
        cachedLifeAnimations = FindFirstObjectByType<LifeAnimations>();
    }

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

    public void LoadNextLevel()
    {
        int nextIndex = SceneManager.GetActiveScene().buildIndex + 1;
        if (nextIndex >= SceneManager.sceneCountInBuildSettings)
        {
            ShowVictoryPanel();
            return;
        }

        FadeTransition.Instance.FadeOut(() =>
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(nextIndex);
        });
    }

    public void StartGame()
    {
        SceneManager.LoadScene(1);
        HideMenuPanel();
    }

    public void RestartLevel()
    {
        FadeTransition.Instance.FadeOut(() =>
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        });
    }

    public void RestartGame()
    {
        // Reset state
        isGameOver = false;
        isPaused = false;
        lives = startingLives;
        score = 0;

        FadeTransition.Instance.FadeOut(() =>
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(1); // always go back to Level 1
        });
    }

    public void LoadScene(int sceneIndex)
    {
        FadeTransition.Instance.FadeOut(() =>
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(sceneIndex);
        });
    }

    private void RespawnPlayer()
    {
        // Release tongue
        cachedTongueHook.ReleaseGrapple();

        // Destroy projectiles
        foreach (SpitProjectile spit in FindObjectsByType<SpitProjectile>(FindObjectsSortMode.None))
        {
            Destroy(spit.gameObject);
        }

        cachedPlayerRespawn.Respawn();
        cachedLifeAnimations.TriggerSpawn();
    }

    public void TriggerGameOver()
    {
        isGameOver = true;
        cachedLifeAnimations.TriggerDeath(true);
    }

    // Panels

    public void HideMenuPanel()
    {
        if (victoryPanel != null) menuPanel.SetActive(false);
    }
    
    public void ShowVictoryPanel()
    {
        Time.timeScale = 0f;
        victoryPanel.SetActive(true);
    }

    public void ShowGameOverPanel()
    {
        Time.timeScale = 0f;
        gameOverPanel.SetActive(true);
    }

    public void TogglePause()
    {
        isPaused = !isPaused;
        Time.timeScale = isPaused ? 0f : 1f;
        pausePanel.SetActive(isPaused);
    }

    private void HideAllPanels()
    {
        gameOverPanel.SetActive(false);
        pausePanel.SetActive(false);
        victoryPanel.SetActive(false);
    }

    private void UpdateUI()
    {
        scoreText.text = "Score: " + score;
        livesText.text = "Lives: " + lives;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}