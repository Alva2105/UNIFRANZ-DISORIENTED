using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Menú de pausa (laptop): Escape lo abre y lo cierra. Detiene el tiempo del juego
// y ofrece reanudar, abrir la Configuración o volver al menú principal.
public class PauseMenu : MonoBehaviour
{
    // --- Referencias de UI ---
    [Tooltip("Raíz del menú (capa oscura + laptops).")]
    [SerializeField] private GameObject root;

    [Tooltip("Laptop de pausa con sus botones.")]
    [SerializeField] private GameObject pausePage;

    [SerializeField] private SettingsPanel settings;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button menuButton;

    [SerializeField] private string menuScene = "MainMenu";

    public static bool IsPaused { get; private set; }

    private PlayerControls controls;

    private void Awake()
    {
        controls = new PlayerControls();
        resumeButton.onClick.AddListener(Resume);
        settingsButton.onClick.AddListener(OpenSettings);
        menuButton.onClick.AddListener(GoToMenu);
        settings.Closed += OnSettingsClosed;
        root.SetActive(false);
        IsPaused = false;
    }

    private void OnEnable()
    {
        controls.Player.Pause.Enable();
    }

    private void OnDisable()
    {
        controls.Player.Pause.Disable();
    }

    private void OnDestroy()
    {
        controls.Dispose();
        IsPaused = false;
    }

    private void Update()
    {
        if (!controls.Player.Pause.WasPressedThisFrame())
        {
            return;
        }

        if (!IsPaused)
        {
            // No se pausa durante un diálogo ni con el nivel terminado
            bool levelOver = LevelFlow.Instance != null && LevelFlow.Instance.IsOver;
            if (!DialogueBox.IsOpen && !levelOver)
            {
                Pause();
            }
        }
        else if (settings.gameObject.activeSelf)
        {
            settings.Close();
        }
        else
        {
            Resume();
        }
    }

    public void Pause()
    {
        IsPaused = true;
        Time.timeScale = 0f;
        root.SetActive(true);
        pausePage.SetActive(true);
        settings.gameObject.SetActive(false);
        EventSystem.current?.SetSelectedGameObject(resumeButton.gameObject);
    }

    public void Resume()
    {
        IsPaused = false;
        Time.timeScale = 1f;
        root.SetActive(false);
    }

    private void OpenSettings()
    {
        pausePage.SetActive(false);
        settings.gameObject.SetActive(true);
    }

    private void OnSettingsClosed()
    {
        pausePage.SetActive(true);
        EventSystem.current?.SetSelectedGameObject(settingsButton.gameObject);
    }

    private void GoToMenu()
    {
        Time.timeScale = 1f;
        IsPaused = false;
        SceneManager.LoadScene(menuScene);
    }
}
