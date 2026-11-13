using System.Collections;
using UnityEngine;

// Base común de los minijuegos: abre el panel, detiene al jugador, sus interacciones y
// la subida del estrés mientras se juega, y al terminar ajusta el estrés y completa el
// objetivo del Checklist. Se juega solo con Moverse (izquierda/derecha) e Interactuar,
// así funciona igual con teclado y con los botones táctiles de Android.
public abstract class MinigameBase : MonoBehaviour
{
    // Cantidad de minijuegos abiertos (los controles táctiles se ocultan mientras haya uno)
    private static int openCount;
    public static bool AnyOpen => openCount > 0;

    [Header("Minijuego")]
    [SerializeField] protected GameObject panel;
    [SerializeField] private PlayerMovement player;
    [SerializeField] private PlayerInteractor interactor;

    [Tooltip("Objetivo del Checklist que se completa al terminar.")]
    [SerializeField] private string objectiveId;

    [Header("Instrucciones (opcional)")]
    [Tooltip("Tarjeta que explica el minijuego antes de empezar; se cierra con Interactuar.")]
    [SerializeField] private GameObject introGroup;
    [SerializeField] private TMPro.TMP_Text introText;

    [SerializeField] protected AudioClip moveClip;
    [SerializeField] protected AudioClip goodClip;
    [SerializeField] protected AudioClip badClip;
    [SerializeField] protected AudioClip finishClip;

    protected PlayerControls controls;
    public bool IsOpen { get; private set; }

    // Un minijuego puede marcarse como no superado (p. ej. un examen reprobado): entonces
    // no completa su objetivo y el jugador puede volver a intentarlo
    protected bool failed;

    private float previousMoveX;

    protected virtual void Awake()
    {
        controls = new PlayerControls();
        panel.SetActive(false);
        if (introGroup != null)
        {
            introGroup.SetActive(false);
        }
    }

    // Qué hay que hacer y con qué teclas; cada minijuego lo explica a su manera
    protected virtual string HowToPlay => "";

    protected virtual void OnDestroy()
    {
        if (IsOpen)
        {
            openCount--;
        }
        controls.Dispose();
    }

    // Se conecta a un interactuable (al terminar el diálogo de quien propone el minijuego)
    public void Begin()
    {
        if (IsOpen)
        {
            return;
        }
        StartCoroutine(Run());
    }

    private IEnumerator Run()
    {
        IsOpen = true;
        openCount++;
        SetPlaying(true);
        panel.SetActive(true);
        controls.Player.Move.Enable();
        controls.Player.Interact.Enable();
        previousMoveX = 0f;

        // Un cuadro de espera: la tecla que abrió el minijuego no cuenta como respuesta
        yield return null;

        // Instrucciones: el jugador lee y empieza cuando quiere
        if (introGroup != null && !string.IsNullOrEmpty(HowToPlay))
        {
            introGroup.SetActive(true);
            introText.text = $"{HowToPlay}\n\n<color=#F96F13>Presiona {ControlPlatform.InteractHint} para empezar</color>";
            while (!ConfirmPressed())
            {
                yield return null;
            }
            introGroup.SetActive(false);
            yield return null;
        }

        float stressChange = 0f;
        failed = false;
        yield return Play(value => stressChange = value);
        AudioManager.PlaySfx(finishClip);
        yield return new WaitForSeconds(2.5f);

        panel.SetActive(false);
        controls.Player.Move.Disable();
        controls.Player.Interact.Disable();
        SetPlaying(false);
        IsOpen = false;
        openCount--;

        if (StressSystem.Instance != null)
        {
            StressSystem.Instance.Add(stressChange);
        }
        if (!failed && ObjectiveTracker.Instance != null && !string.IsNullOrEmpty(objectiveId))
        {
            ObjectiveTracker.Instance.Complete(objectiveId);
        }
    }

    // Si el nivel termina en medio (los errores suben el estrés y puede llegar la crisis
    // de ansiedad), el minijuego se corta: no debe quedar encima de la pantalla final
    // ni volver a activar al jugador y al estrés al terminar
    private void Update()
    {
        if (IsOpen && LevelFlow.Instance != null && LevelFlow.Instance.IsOver)
        {
            StopAllCoroutines();
            panel.SetActive(false);
            controls.Player.Move.Disable();
            controls.Player.Interact.Disable();
            IsOpen = false;
            openCount--;
        }
    }

    // Juego propio de cada minijuego; al terminar informa cuánto cambia el estrés
    protected abstract IEnumerator Play(System.Action<float> result);

    // -1 o +1 el cuadro en que se presiona izquierda o derecha; 0 si no
    protected int HorizontalPressed()
    {
        // En pausa no se responde: las teclas son para el menú de pausa
        if (PauseMenu.IsPaused)
        {
            return 0;
        }
        float x = controls.Player.Move.ReadValue<Vector2>().x;
        int step = 0;
        if (x > 0.5f && previousMoveX <= 0.5f)
        {
            step = 1;
        }
        else if (x < -0.5f && previousMoveX >= -0.5f)
        {
            step = -1;
        }
        previousMoveX = x;
        if (step != 0)
        {
            AudioManager.PlaySfx(moveClip, 0.5f);
        }
        return step;
    }

    protected bool ConfirmPressed()
    {
        return !PauseMenu.IsPaused && controls.Player.Interact.WasPressedThisFrame();
    }

    private void SetPlaying(bool playing)
    {
        player.enabled = !playing;
        interactor.enabled = !playing;
        if (StressSystem.Instance != null)
        {
            StressSystem.Instance.Frozen = playing;
        }
    }
}
