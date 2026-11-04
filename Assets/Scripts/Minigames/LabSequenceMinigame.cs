using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Minijuego "Práctica de laboratorio": la docente muestra en qué orden se agregan los
// reactivos (los tubos se iluminan uno por uno) y el jugador repite la secuencia con
// izquierda/derecha e Interactuar. Cada ronda la secuencia es más larga. Un error sube
// el estrés y repite la misma ronda; terminar sin errores calma.
public class LabSequenceMinigame : MinigameBase
{
    [Header("Laboratorio")]
    [Tooltip("Nombre de cada tubo, en el mismo orden que las casillas.")]
    [SerializeField] private string[] reagentNames = { "Azul", "Rojo", "Verde", "Amarillo" };
    [SerializeField] private Image[] tubes;
    [SerializeField] private TMP_Text[] tubeTexts;
    [SerializeField] private TMP_Text promptText;
    [SerializeField] private TMP_Text feedbackText;
    [SerializeField] private TMP_Text progressText;

    [Tooltip("Largo de la secuencia en cada ronda.")]
    [SerializeField] private int[] roundLengths = { 3, 4, 5 };
    [SerializeField] private float showSeconds = 0.7f;
    [SerializeField] private int maxRetries = 2;

    [SerializeField] private float stressPerError = 3f;
    [SerializeField] private float reliefPerfect = -10f;

    [SerializeField] private Color[] tubeColors =
    {
        new Color(0.25f, 0.5f, 0.95f), new Color(0.9f, 0.25f, 0.25f),
        new Color(0.3f, 0.75f, 0.35f), new Color(0.95f, 0.8f, 0.2f)
    };
    [SerializeField] private Color dimColor = new Color(0.16f, 0.18f, 0.3f);
    [SerializeField] private Color cursorColor = new Color(0.98f, 0.44f, 0.08f);

    protected override IEnumerator Play(Action<float> result)
    {
        string key = ControlPlatform.InteractHint;
        int errors = 0;
        for (int i = 0; i < tubeTexts.Length; i++)
        {
            tubeTexts[i].text = reagentNames[i];
        }

        for (int round = 0; round < roundLengths.Length; round++)
        {
            int[] sequence = new int[roundLengths[round]];
            for (int i = 0; i < sequence.Length; i++)
            {
                sequence[i] = UnityEngine.Random.Range(0, tubes.Length);
            }

            bool passed = false;
            for (int attempt = 0; attempt <= maxRetries && !passed; attempt++)
            {
                progressText.text = $"Ronda {round + 1}/{roundLengths.Length}";
                promptText.text = "Observa el orden de los reactivos...";
                feedbackText.text = "";
                Paint(-1, -1);
                yield return new WaitForSeconds(0.8f);

                // La docente muestra la secuencia
                foreach (int tube in sequence)
                {
                    Paint(-1, tube);
                    AudioManager.PlaySfx(moveClip, 0.6f);
                    yield return new WaitForSeconds(showSeconds);
                    Paint(-1, -1);
                    yield return new WaitForSeconds(0.2f);
                }

                // El jugador la repite
                promptText.text = "Ahora agrega los reactivos en el mismo orden";
                feedbackText.text = $"< > elige el tubo   {key} agrega";
                int cursor = 0;
                int step = 0;
                Paint(cursor, -1);
                while (step < sequence.Length)
                {
                    int move = HorizontalPressed();
                    if (move != 0)
                    {
                        cursor = (cursor + move + tubes.Length) % tubes.Length;
                        Paint(cursor, -1);
                    }
                    if (ConfirmPressed())
                    {
                        if (cursor != sequence[step])
                        {
                            break;
                        }
                        step++;
                        AudioManager.PlaySfx(goodClip, 0.6f);
                        feedbackText.text = $"Reactivo {step}/{sequence.Length}";
                        Paint(cursor, cursor);
                    }
                    yield return null;
                }

                passed = step == sequence.Length;
                if (passed)
                {
                    feedbackText.text = "¡La mezcla salió perfecta!";
                }
                else
                {
                    errors++;
                    AudioManager.PlaySfx(badClip);
                    feedbackText.text = "¡Ese no era! La mezcla cambió de color... otra vez.";
                    if (StressSystem.Instance != null)
                    {
                        StressSystem.Instance.Add(stressPerError);
                    }
                }
                yield return new WaitForSeconds(1.2f);
            }
        }

        Paint(-1, -1);
        promptText.text = errors == 0 ? "¡Práctica impecable!" : "Práctica terminada.";
        progressText.text = errors == 0 ? "Sin errores" : $"Errores: {errors}";
        feedbackText.text = errors == 0 ? "La docente te felicita. Te sientes capaz."
            : "Equivocarse en el laboratorio también es aprender.";
        result(errors == 0 ? reliefPerfect : reliefPerfect * 0.3f);
    }

    // El tubo encendido muestra su color; el cursor se marca en naranja y el resto, apagado
    private void Paint(int cursor, int lit)
    {
        for (int i = 0; i < tubes.Length; i++)
        {
            tubes[i].color = i == lit ? tubeColors[i] : i == cursor ? cursorColor : dimColor;
        }
    }
}
