using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Minijuego "Apuntes rápidos": el docente dicta una frase incompleta y hay que elegir,
// antes de que se acabe el tiempo, la palabra clave correcta entre tres opciones
// (izquierda/derecha para elegir, Interactuar para anotar). Cada acierto calma un poco
// y cada error o tiempo agotado sube el estrés.
public class NotesMinigame : MinigameBase
{
    [Serializable]
    public class Question
    {
        [TextArea(1, 3)] public string prompt;
        public string[] options = new string[3];
        public int correct;
    }

    [Header("Apuntes")]
    [SerializeField] private Question[] questions;
    [SerializeField] private float secondsPerQuestion = 9f;
    [SerializeField] private TMP_Text promptText;
    [SerializeField] private Image[] optionBoxes;
    [SerializeField] private TMP_Text[] optionTexts;
    [SerializeField] private Image timerFill;
    [SerializeField] private TMP_Text feedbackText;
    [SerializeField] private TMP_Text progressText;

    [SerializeField] private float stressPerMiss = 3f;
    [SerializeField] private float reliefPerHit = -2f;

    [SerializeField] private Color boxColor = new Color(0.16f, 0.18f, 0.3f);
    [SerializeField] private Color cursorColor = new Color(0.98f, 0.44f, 0.08f);
    [SerializeField] private Color rightColor = new Color(0.2f, 0.6f, 0.35f);
    [SerializeField] private Color wrongColor = new Color(0.75f, 0.2f, 0.2f);

    protected override IEnumerator Play(Action<float> result)
    {
        int hits = 0;
        string key = ControlPlatform.InteractHint;

        for (int q = 0; q < questions.Length; q++)
        {
            Question question = questions[q];
            progressText.text = $"Apunte {q + 1}/{questions.Length}   Aciertos: {hits}";
            promptText.text = question.prompt;
            feedbackText.text = $"< > elige la palabra   {key} anota";
            int cursor = 1;
            for (int i = 0; i < optionTexts.Length; i++)
            {
                optionTexts[i].text = question.options[i];
            }
            Paint(cursor, -1, false);

            int chosen = -1;
            for (float t = 0f; t < secondsPerQuestion; t += Time.deltaTime)
            {
                timerFill.rectTransform.localScale = new Vector3(1f - t / secondsPerQuestion, 1f, 1f);
                int step = HorizontalPressed();
                if (step != 0)
                {
                    cursor = Mathf.Clamp(cursor + step, 0, optionTexts.Length - 1);
                    Paint(cursor, -1, false);
                }
                if (ConfirmPressed())
                {
                    chosen = cursor;
                    break;
                }
                yield return null;
            }

            bool right = chosen == question.correct;
            if (right)
            {
                hits++;
                AudioManager.PlaySfx(goodClip);
                feedbackText.text = "¡Anotado!";
            }
            else
            {
                AudioManager.PlaySfx(badClip);
                feedbackText.text = chosen < 0 ? "No alcanzaste a anotarlo..." : "Eso no era lo que dijo el docente.";
                if (StressSystem.Instance != null)
                {
                    StressSystem.Instance.Add(stressPerMiss);
                }
            }
            Paint(chosen, question.correct, true);
            yield return new WaitForSeconds(1.2f);
        }

        promptText.text = hits >= questions.Length - 1 ? "Tus apuntes quedaron completos." : "Te faltaron algunas cosas en tus apuntes.";
        progressText.text = $"Aciertos: {hits}/{questions.Length}";
        feedbackText.text = hits >= questions.Length - 1 ? "Te sientes con más confianza." : "Puedes pedirle los apuntes a un compañero.";
        result(reliefPerHit * hits);
    }

    // Colorea las opciones: el cursor mientras se elige; al responder, la correcta en
    // verde y la elegida (si estaba mal) en rojo
    private void Paint(int cursor, int correct, bool reveal)
    {
        for (int i = 0; i < optionBoxes.Length; i++)
        {
            Color color = boxColor;
            if (reveal)
            {
                color = i == correct ? rightColor : i == cursor ? wrongColor : boxColor;
            }
            else if (i == cursor)
            {
                color = cursorColor;
            }
            optionBoxes[i].color = color;
        }
    }
}
