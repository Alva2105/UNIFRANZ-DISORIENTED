using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Minijuego de respiración (autorregulación del estrés, según el GDD). Un círculo
// crece (inhala) y se encoge (exhala): el jugador mantiene Interact mientras crece y
// lo suelta mientras se encoge. Cuanto mejor sigue el ritmo, más baja el estrés.
// La primera vez completa el objetivo del Checklist; después se puede repetir.
public class BreathingMinigame : MonoBehaviour
{
    public static bool IsOpen { get; private set; }

    // --- UI ---
    [SerializeField] private GameObject panel;
    [SerializeField] private RectTransform circle;
    [SerializeField] private Image circleImage;
    [SerializeField] private TMP_Text instruction;
    [SerializeField] private TMP_Text progress;

    // --- Jugador ---
    [SerializeField] private PlayerMovement player;
    [SerializeField] private PlayerInteractor interactor;

    // --- Reglas ---
    [Tooltip("Objetivo del Checklist que se completa al terminar.")]
    [SerializeField] private string objectiveId = "respiracion";
    [SerializeField] private int cycles = 5;
    [SerializeField] private float inhaleSeconds = 4f;
    [SerializeField] private float exhaleSeconds = 4f;
    [SerializeField] private float introSeconds = 4f;

    [Tooltip("Estrés que baja con un ritmo perfecto (se escala con la precisión).")]
    [SerializeField] private float maxRelief = 30f;

    [SerializeField] private float minScale = 0.35f;
    [SerializeField] private float maxScale = 1f;

    [Header("Colores y sonidos")]
    [SerializeField] private Color goodColor = new Color(0.35f, 0.85f, 0.6f);
    [SerializeField] private Color badColor = new Color(0.98f, 0.45f, 0.1f);
    [SerializeField] private AudioClip phaseClip;
    [SerializeField] private AudioClip finishClip;

    private PlayerControls controls;

    private void Awake()
    {
        controls = new PlayerControls();
        panel.SetActive(false);
        IsOpen = false;
    }

    private void OnDestroy()
    {
        controls.Dispose();
        IsOpen = false;
    }

    // Se conecta a los eventos de Lucía (al terminar su diálogo)
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
        SetPlaying(true);
        panel.SetActive(true);
        controls.Player.Interact.Enable();

        // E en Windows, botón táctil USAR en Android
        string key = ControlPlatform.InteractHint;
        instruction.text = $"Mantén {key} mientras el círculo crece (inhala)\ny suéltalo cuando se encoge (exhala)";
        progress.text = "";
        circle.localScale = Vector3.one * minScale;
        circleImage.color = goodColor;
        yield return new WaitForSeconds(introSeconds);

        float correct = 0f;
        float total = 0f;
        for (int cycle = 1; cycle <= cycles; cycle++)
        {
            progress.text = $"Respiración {cycle}/{cycles}";
            for (int phase = 0; phase < 2; phase++)
            {
                bool inhale = phase == 0;
                float duration = inhale ? inhaleSeconds : exhaleSeconds;
                instruction.text = inhale ? $"INHALA... (mantén {key})" : $"EXHALA... (suelta {key})";
                AudioManager.PlaySfx(phaseClip, 0.5f);

                for (float t = 0f; t < duration; t += Time.deltaTime)
                {
                    float k = Mathf.SmoothStep(0f, 1f, t / duration);
                    float scale = inhale ? Mathf.Lerp(minScale, maxScale, k) : Mathf.Lerp(maxScale, minScale, k);
                    circle.localScale = Vector3.one * scale;

                    bool ok = controls.Player.Interact.IsPressed() == inhale;
                    circleImage.color = ok ? goodColor : badColor;
                    if (ok)
                    {
                        correct += Time.deltaTime;
                    }
                    total += Time.deltaTime;
                    yield return null;
                }
            }
        }

        float accuracy = total > 0f ? correct / total : 0f;
        instruction.text = accuracy >= 0.7f
            ? "¡Muy bien! Te sientes mucho más tranquil@."
            : "Sigue practicando. Respirar con calma ayuda.";
        progress.text = $"Precisión: {Mathf.RoundToInt(accuracy * 100f)}%";
        AudioManager.PlaySfx(finishClip);
        yield return new WaitForSeconds(2.5f);

        panel.SetActive(false);
        controls.Player.Interact.Disable();
        SetPlaying(false);
        IsOpen = false;

        if (StressSystem.Instance != null)
        {
            StressSystem.Instance.Add(-maxRelief * accuracy);
        }
        if (ObjectiveTracker.Instance != null)
        {
            ObjectiveTracker.Instance.Complete(objectiveId);
        }
    }

    // Detiene al jugador, sus interacciones y la subida del estrés durante el minijuego
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
