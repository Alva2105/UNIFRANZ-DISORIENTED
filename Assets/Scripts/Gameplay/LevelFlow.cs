using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Inicio y final del nivel: completar el hito muestra la pantalla de victoria y
// llegar al estrés máximo muestra la crisis de ansiedad con opción de reintentar.
public class LevelFlow : MonoBehaviour
{
    public static LevelFlow Instance { get; private set; }

    [SerializeField] private PlayerMovement player;
    [SerializeField] private string menuScene = "MainMenu";

    // --- Pantallas finales ---
    [SerializeField] private GameObject winPanel;
    [SerializeField] private Button winMenuButton;
    [SerializeField] private GameObject losePanel;
    [SerializeField] private Button retryButton;
    [SerializeField] private Button loseMenuButton;

    public bool IsOver { get; private set; }

    private void Awake()
    {
        Instance = this;
        winPanel.SetActive(false);
        losePanel.SetActive(false);
        winMenuButton.onClick.AddListener(GoToMenu);
        loseMenuButton.onClick.AddListener(GoToMenu);
        retryButton.onClick.AddListener(Retry);
    }

    private void Start()
    {
        Time.timeScale = 1f;
        if (StressSystem.Instance != null)
        {
            StressSystem.Instance.Maxed += OnStressMaxed;
        }
    }

    private void OnDestroy()
    {
        if (StressSystem.Instance != null)
        {
            StressSystem.Instance.Maxed -= OnStressMaxed;
        }
        if (Instance == this)
        {
            Instance = null;
        }
    }

    // Se conecta al onFinished del interactuable que cierra el hito
    public void CompleteMilestone()
    {
        End(winPanel, winMenuButton);
    }

    private void OnStressMaxed()
    {
        End(losePanel, retryButton);
    }

    private void End(GameObject panel, Button selected)
    {
        if (IsOver)
        {
            return;
        }
        IsOver = true;
        player.enabled = false;
        if (StressSystem.Instance != null)
        {
            StressSystem.Instance.Frozen = true;
        }
        panel.SetActive(true);
        EventSystem.current?.SetSelectedGameObject(selected.gameObject);
    }

    private void Retry()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void GoToMenu()
    {
        SceneManager.LoadScene(menuScene);
    }
}
