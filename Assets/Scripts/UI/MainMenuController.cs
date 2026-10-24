using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Acciones del menú principal: Jugar abre la elección de personaje (que carga el
// primer nivel), Opciones abre la laptop de Configuración, Créditos muestra al
// equipo y Salir cierra el juego.
public class MainMenuController : MonoBehaviour
{
    // --- Botones del menú ---
    [SerializeField] private Button playButton;
    [SerializeField] private Button optionsButton;
    [SerializeField] private Button creditsButton;
    [SerializeField] private Button quitButton;

    [Tooltip("Grupo de los botones del menú; se bloquea mientras hay un panel abierto.")]
    [SerializeField] private CanvasGroup menuGroup;

    // --- Paneles ---
    [Tooltip("Capa oscura que va detrás de los paneles.")]
    [SerializeField] private GameObject overlay;

    [SerializeField] private SettingsPanel settings;
    [SerializeField] private CharacterSelectPanel characterSelect;
    [SerializeField] private GameObject credits;
    [SerializeField] private Button creditsBackButton;

    private PlayerControls controls;

    private void Awake()
    {
        controls = new PlayerControls();
        playButton.onClick.AddListener(Play);
        optionsButton.onClick.AddListener(OpenOptions);
        creditsButton.onClick.AddListener(OpenCredits);
        quitButton.onClick.AddListener(Quit);
        creditsBackButton.onClick.AddListener(CloseCredits);
        settings.Closed += () => ClosePanels(optionsButton);
        characterSelect.Closed += () => ClosePanels(playButton);
    }

    private void Start()
    {
        // Al volver desde la pausa el tiempo podría seguir detenido
        Time.timeScale = 1f;
        overlay.SetActive(false);
        settings.gameObject.SetActive(false);
        characterSelect.gameObject.SetActive(false);
        credits.SetActive(false);
        Select(playButton);
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
    }

    // Escape cierra el panel abierto
    private void Update()
    {
        if (!controls.Player.Pause.WasPressedThisFrame())
        {
            return;
        }
        if (settings.gameObject.activeSelf)
        {
            settings.Close();
        }
        else if (credits.activeSelf)
        {
            CloseCredits();
        }
        else if (characterSelect.gameObject.activeSelf)
        {
            characterSelect.Close();
        }
    }

    public void Play()
    {
        OpenPanel();
        characterSelect.gameObject.SetActive(true);
    }

    public void OpenOptions()
    {
        OpenPanel();
        settings.gameObject.SetActive(true);
    }

    public void OpenCredits()
    {
        OpenPanel();
        credits.SetActive(true);
        Select(creditsBackButton);
    }

    private void CloseCredits()
    {
        credits.SetActive(false);
        ClosePanels(creditsButton);
    }

    public void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void OpenPanel()
    {
        menuGroup.interactable = false;
        overlay.SetActive(true);
    }

    private void ClosePanels(Button selectAfter)
    {
        overlay.SetActive(false);
        menuGroup.interactable = true;
        Select(selectAfter);
    }

    private static void Select(Button button)
    {
        EventSystem.current?.SetSelectedGameObject(button.gameObject);
    }
}
