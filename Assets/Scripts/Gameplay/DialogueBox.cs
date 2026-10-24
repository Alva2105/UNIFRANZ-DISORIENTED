using System;
using TMPro;
using UnityEngine;

// Caja de diálogo inferior con efecto de máquina de escribir. Mientras está
// abierta el jugador no se mueve y el estrés no sube. Interact completa la línea
// o pasa a la siguiente; al terminar llama al callback de quien la abrió.
public class DialogueBox : MonoBehaviour
{
    public static DialogueBox Instance { get; private set; }
    public static bool IsOpen => Instance != null && Instance.panel.activeSelf;

    // Cuadro en que se cerró el último diálogo (lo usa PlayerInteractor)
    public static int ClosedFrame { get; private set; } = -1;

    // --- Referencias ---
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text speakerText;
    [SerializeField] private TMP_Text bodyText;
    [SerializeField] private PlayerMovement player;

    [Tooltip("Letras que aparecen por segundo.")]
    [SerializeField] private float charsPerSecond = 45f;

    private PlayerControls controls;
    private string[] lines;
    private int index;
    private float visibleChars;
    private int openedFrame;
    private Action onDone;

    private void Awake()
    {
        Instance = this;
        controls = new PlayerControls();
        panel.SetActive(false);
    }

    private void OnEnable()
    {
        controls.Player.Interact.Enable();
    }

    private void OnDisable()
    {
        controls.Player.Interact.Disable();
    }

    private void OnDestroy()
    {
        controls.Dispose();
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void Show(string speaker, string[] dialogueLines, Action finished)
    {
        if (dialogueLines == null || dialogueLines.Length == 0)
        {
            finished?.Invoke();
            return;
        }

        lines = dialogueLines;
        onDone = finished;
        index = 0;
        openedFrame = Time.frameCount;

        speakerText.text = speaker;
        speakerText.gameObject.SetActive(!string.IsNullOrEmpty(speaker));
        panel.SetActive(true);
        SetFrozen(true);
        StartLine();
    }

    private void Update()
    {
        if (!panel.activeSelf)
        {
            return;
        }

        int total = bodyText.textInfo.characterCount;
        if (visibleChars < total)
        {
            visibleChars += charsPerSecond * Time.unscaledDeltaTime;
            bodyText.maxVisibleCharacters = (int)visibleChars;
        }

        // La tecla que abrió el diálogo no debe saltarse la primera línea
        if (Time.frameCount == openedFrame || !controls.Player.Interact.WasPressedThisFrame())
        {
            return;
        }

        if (visibleChars < total)
        {
            visibleChars = total;
            bodyText.maxVisibleCharacters = total;
        }
        else if (++index < lines.Length)
        {
            StartLine();
        }
        else
        {
            Close();
        }
    }

    private void StartLine()
    {
        bodyText.text = lines[index];
        bodyText.ForceMeshUpdate();
        visibleChars = 0f;
        bodyText.maxVisibleCharacters = 0;
    }

    private void Close()
    {
        panel.SetActive(false);
        ClosedFrame = Time.frameCount;
        SetFrozen(false);
        Action done = onDone;
        onDone = null;
        done?.Invoke();
    }

    // Detiene al jugador y la subida del estrés mientras se lee
    private void SetFrozen(bool frozen)
    {
        if (player != null)
        {
            player.enabled = !frozen;
        }
        if (StressSystem.Instance != null)
        {
            StressSystem.Instance.Frozen = frozen;
        }
    }
}
