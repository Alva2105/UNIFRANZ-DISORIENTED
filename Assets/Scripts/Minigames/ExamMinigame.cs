using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Minijuego "Examen final": preguntas de opción múltiple con tiempo (izquierda/derecha
// para elegir, Interactuar para responder). El estrés acorta el tiempo de cada pregunta
// y, en Ansiedad, las opciones se mezclan de lugar. Se aprueba con 51 sobre 100; si se
// reprueba, el objetivo no se completa y se puede volver a rendir con el docente.
public class ExamMinigame : MinigameBase
{
    [Serializable]
    public class Question
    {
        [TextArea(1, 3)] public string prompt;
        public string[] options = new string[3];
        public int correct;
    }

    [Header("Examen")]
    [SerializeField] private string subject = "Examen final";
    [SerializeField] private Question[] questions;
    [SerializeField] private float secondsPerQuestion = 10f;
    [Tooltip("Parte del tiempo que se pierde con el estrés al máximo.")]
    [SerializeField, Range(0f, 0.8f)] private float stressTimeLoss = 0.4f;
    [SerializeField] private float shuffleEvery = 2.5f;
    [SerializeField] private int passGrade = 51;

    [SerializeField] private TMP_Text promptText;
    [SerializeField] private Image[] optionBoxes;
    [SerializeField] private TMP_Text[] optionTexts;
    [SerializeField] private Image timerFill;
    [SerializeField] private TMP_Text feedbackText;
    [SerializeField] private TMP_Text progressText;

    [SerializeField] private float stressPerMiss = 3f;
    [SerializeField] private float reliefPass = -15f;

    [SerializeField] private Color boxColor = new Color(0.16f, 0.18f, 0.3f);
    [SerializeField] private Color cursorColor = new Color(0.98f, 0.44f, 0.08f);
    [SerializeField] private Color rightColor = new Color(0.2f, 0.6f, 0.35f);
    [SerializeField] private Color wrongColor = new Color(0.75f, 0.2f, 0.2f);

    // Resultado para el resumen del semestre
    public string Subject => subject;
    public int BestGrade { get; private set; }
    public int Attempts { get; private set; }
    public bool Passed => BestGrade >= passGrade;

    private string[] shown;
    private int shownCorrect;

    protected override IEnumerator Play(Action<float> result)
    {
        Attempts++;
        int hits = 0;
        string key = ControlPlatform.InteractHint;

        for (int q = 0; q < questions.Length; q++)
        {
            Question question = questions[q];
            shown = (string[])question.options.Clone();
            shownCorrect = question.correct;
            progressText.text = $"{subject}   Pregunta {q + 1}/{questions.Length}";
            promptText.text = question.prompt;
            feedbackText.text = $"< > elige   {key} responde";
            int cursor = 1;
            ShowOptions();
            Paint(cursor, -1, false);

            // Con más estrés, menos tiempo para pensar
            float stress = StressSystem.Instance != null ? StressSystem.Instance.Normalized : 0f;
            float limit = secondsPerQuestion * (1f - stressTimeLoss * stress);
            float nextShuffle = shuffleEvery;
            int chosen = -1;
            for (float t = 0f; t < limit; t += Time.deltaTime)
            {
                timerFill.rectTransform.localScale = new Vector3(1f - t / limit, 1f, 1f);
                if (StressSystem.Instance != null && StressSystem.Instance.Level == StressLevel.Anxiety && t >= nextShuffle)
                {
                    nextShuffle += shuffleEvery;
                    Shuffle();
                    ShowOptions();
                    feedbackText.text = "¡Las opciones se mezclan! Respira y vuelve a leer.";
                }
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

            if (chosen == shownCorrect)
            {
                hits++;
                AudioManager.PlaySfx(goodClip);
                feedbackText.text = "Respuesta correcta.";
            }
            else
            {
                AudioManager.PlaySfx(badClip);
                feedbackText.text = chosen < 0 ? "Se acabó el tiempo de esa pregunta..." : "Respuesta incorrecta.";
                if (StressSystem.Instance != null)
                {
                    StressSystem.Instance.Add(stressPerMiss);
                }
            }
            Paint(chosen, shownCorrect, true);
            yield return new WaitForSeconds(1f);
        }

        int grade = Mathf.RoundToInt(100f * hits / questions.Length);
        BestGrade = Mathf.Max(BestGrade, grade);
        failed = grade < passGrade;
        progressText.text = $"{subject}   Nota: {grade}/100";
        promptText.text = failed ? "Reprobaste este examen." : "¡Aprobaste!";
        feedbackText.text = failed ? "Respira y vuelve a hablar con el docente para rendirlo otra vez."
            : grade >= 85 ? "Excelente nota. Todo el esfuerzo valió la pena." : "Lo lograste. Un examen menos.";
        result(failed ? 0f : reliefPass);
    }

    // Intercambia dos opciones de lugar (la correcta se mueve con su texto)
    private void Shuffle()
    {
        int a = UnityEngine.Random.Range(0, shown.Length);
        int b = (a + UnityEngine.Random.Range(1, shown.Length)) % shown.Length;
        (shown[a], shown[b]) = (shown[b], shown[a]);
        shownCorrect = shownCorrect == a ? b : shownCorrect == b ? a : shownCorrect;
    }

    private void ShowOptions()
    {
        for (int i = 0; i < optionTexts.Length; i++)
        {
            optionTexts[i].text = shown[i];
        }
    }

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
