using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Minijuego "Exposición en el Auditorio": una aguja de calma se desvía sola mientras el
// público murmura; el jugador la mantiene en la zona central moviéndose a izquierda y
// derecha. Cuanto más estrés, más fuerte se desvía, y en Ansiedad además tiembla. Cada
// cierto tiempo el docente hace una pregunta de opción múltiple. Al final, el tiempo
// que se mantuvo la calma y las respuestas definen cuánto baja el estrés.
public class PresentationMinigame : MinigameBase
{
    [Serializable]
    public class Question
    {
        [TextArea(1, 3)] public string prompt;
        public string[] options = new string[3];
        public int correct;
    }

    [Header("Exposición")]
    [SerializeField] private Question[] questions;
    [SerializeField] private float duration = 36f;
    [SerializeField] private float secondsPerQuestion = 8f;

    [Header("Aguja")]
    [SerializeField] private RectTransform needle;
    [SerializeField] private RectTransform bar;
    [SerializeField] private Image calmZone;
    [Tooltip("Mitad del ancho de la zona de calma (0..1 del recorrido de la aguja).")]
    [SerializeField] private float calmHalfWidth = 0.3f;
    [SerializeField] private float pushSpeed = 1.6f;
    [SerializeField] private float driftSpeed = 0.7f;

    [Header("UI")]
    [SerializeField] private GameObject questionGroup;
    [SerializeField] private Image[] optionBoxes;
    [SerializeField] private TMP_Text[] optionTexts;
    [SerializeField] private TMP_Text promptText;
    [SerializeField] private Image timerFill;
    [SerializeField] private TMP_Text feedbackText;
    [SerializeField] private TMP_Text progressText;

    [SerializeField] private float stressOutside = 2f;
    [SerializeField] private float stressPerMiss = 3f;
    [SerializeField] private float maxRelief = -20f;

    [SerializeField] private Color boxColor = new Color(0.16f, 0.18f, 0.3f);
    [SerializeField] private Color cursorColor = new Color(0.98f, 0.44f, 0.08f);
    [SerializeField] private Color rightColor = new Color(0.2f, 0.6f, 0.35f);
    [SerializeField] private Color wrongColor = new Color(0.75f, 0.2f, 0.2f);
    [SerializeField] private Color calmColor = new Color(0.2f, 0.6f, 0.35f, 0.8f);
    [SerializeField] private Color nervousColor = new Color(0.75f, 0.2f, 0.2f, 0.8f);

    private float position;

    protected override IEnumerator Play(Action<float> result)
    {
        position = 0f;
        float calmTime = 0f;
        int hits = 0;
        int asked = 0;
        float drift = 0f;
        float nextDrift = 0f;
        questionGroup.SetActive(false);
        float zoneWidth = bar.rect.width * calmHalfWidth;
        calmZone.rectTransform.sizeDelta = new Vector2(zoneWidth * 2f, calmZone.rectTransform.sizeDelta.y);

        for (float t = 0f; t < duration; t += Time.deltaTime)
        {
            // Preguntas repartidas a lo largo de la exposición
            if (asked < questions.Length && t >= duration * (asked + 1) / (questions.Length + 1))
            {
                bool right = false;
                yield return Ask(questions[asked], value => right = value);
                hits += right ? 1 : 0;
                asked++;
            }

            promptText.text = "Mantén la calma mientras expones";
            feedbackText.text = "< > mantén la aguja en el centro";
            timerFill.rectTransform.localScale = new Vector3(1f - t / duration, 1f, 1f);
            progressText.text = $"Calma: {Mathf.RoundToInt(100f * calmTime / Mathf.Max(t, 0.01f))}%   Preguntas: {hits}/{asked}";

            // El murmullo empuja la aguja; con más estrés empuja más fuerte
            float stress = StressSystem.Instance != null ? StressSystem.Instance.Normalized : 0f;
            nextDrift -= Time.deltaTime;
            if (nextDrift <= 0f)
            {
                drift = UnityEngine.Random.Range(-1f, 1f) * driftSpeed * (1f + stress);
                nextDrift = UnityEngine.Random.Range(0.5f, 1.2f);
            }
            float input = PauseMenu.IsPaused ? 0f : controls.Player.Move.ReadValue<Vector2>().x;
            position = Mathf.Clamp(position + (drift + input * pushSpeed) * Time.deltaTime, -1f, 1f);
            float shake = StressSystem.Instance != null && StressSystem.Instance.Level == StressLevel.Anxiety
                ? UnityEngine.Random.Range(-0.06f, 0.06f) : 0f;
            needle.anchoredPosition = new Vector2((position + shake) * bar.rect.width * 0.5f, needle.anchoredPosition.y);

            bool calm = Mathf.Abs(position) <= calmHalfWidth;
            calmZone.color = calm ? calmColor : nervousColor;
            if (calm)
            {
                calmTime += Time.deltaTime;
            }
            else if (StressSystem.Instance != null)
            {
                StressSystem.Instance.Add(stressOutside * Time.deltaTime);
            }
            yield return null;
        }

        float score = calmTime / duration * 0.7f + (questions.Length > 0 ? 0.3f * hits / questions.Length : 0.3f);
        promptText.text = score >= 0.6f ? "¡Aplausos! Tu exposición salió muy bien." : "Terminaste tu exposición.";
        feedbackText.text = score >= 0.6f ? "Hablar en público ya no te da tanto miedo." : "Los nervios se notaron, pero lo lograste.";
        progressText.text = $"Calma: {Mathf.RoundToInt(100f * calmTime / duration)}%   Preguntas: {hits}/{questions.Length}";
        result(maxRelief * score);
    }

    // Pregunta del docente: mismas reglas que Apuntes rápidos (elegir a tiempo)
    private IEnumerator Ask(Question question, Action<bool> answered)
    {
        string key = ControlPlatform.InteractHint;
        questionGroup.SetActive(true);
        promptText.text = "Pregunta del docente: " + question.prompt;
        feedbackText.text = $"< > elige la respuesta   {key} responde";
        for (int i = 0; i < optionTexts.Length; i++)
        {
            optionTexts[i].text = question.options[i];
        }
        int cursor = 1;
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
        AudioManager.PlaySfx(right ? goodClip : badClip);
        feedbackText.text = right ? "¡Buena respuesta!" : chosen < 0 ? "Te quedaste en blanco..." : "No era eso...";
        if (!right && StressSystem.Instance != null)
        {
            StressSystem.Instance.Add(stressPerMiss);
        }
        Paint(chosen, question.correct, true);
        yield return new WaitForSeconds(1.2f);
        questionGroup.SetActive(false);
        answered(right);
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
