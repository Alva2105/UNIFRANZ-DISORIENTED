using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Minijuego "Encuentra el libro": la bibliotecaria da el código de un libro y en el
// estante hay varios lomos con códigos parecidos. Con izquierda/derecha se recorre el
// estante y con Interactuar se saca el libro, antes de que se acabe el tiempo. Cada
// libro correcto calma un poco; cada error o tiempo agotado sube el estrés.
public class BookSearchMinigame : MinigameBase
{
    [Serializable]
    public class Request
    {
        public string title;
        [Tooltip("Código de cada lomo del estante (uno por casilla).")]
        public string[] codes;
        public int correct;
    }

    [Header("Biblioteca")]
    [SerializeField] private Request[] requests;
    [SerializeField] private float secondsPerBook = 12f;
    [SerializeField] private Image[] spines;
    [SerializeField] private TMP_Text[] spineTexts;
    [SerializeField] private TMP_Text promptText;
    [SerializeField] private Image timerFill;
    [SerializeField] private TMP_Text feedbackText;
    [SerializeField] private TMP_Text progressText;

    [SerializeField] private float stressPerMiss = 3f;
    [SerializeField] private float reliefPerHit = -3f;

    [SerializeField] private Color spineColor = new Color(0.45f, 0.3f, 0.2f);
    [SerializeField] private Color cursorColor = new Color(0.98f, 0.44f, 0.08f);
    [SerializeField] private Color rightColor = new Color(0.2f, 0.6f, 0.35f);
    [SerializeField] private Color wrongColor = new Color(0.75f, 0.2f, 0.2f);

    protected override IEnumerator Play(Action<float> result)
    {
        int hits = 0;
        string key = ControlPlatform.InteractHint;

        for (int r = 0; r < requests.Length; r++)
        {
            Request request = requests[r];
            progressText.text = $"Libro {r + 1}/{requests.Length}   Encontrados: {hits}";
            promptText.text = $"Busca: <b>{request.title}</b>\nCódigo: <color=#F96F13>{request.codes[request.correct]}</color>";
            feedbackText.text = $"< > recorre el estante   {key} saca el libro";
            for (int i = 0; i < spineTexts.Length; i++)
            {
                spineTexts[i].text = request.codes[i];
            }
            int cursor = 0;
            Paint(cursor, -1, false);

            int chosen = -1;
            for (float t = 0f; t < secondsPerBook; t += Time.deltaTime)
            {
                timerFill.rectTransform.localScale = new Vector3(1f - t / secondsPerBook, 1f, 1f);
                int step = HorizontalPressed();
                if (step != 0)
                {
                    cursor = Mathf.Clamp(cursor + step, 0, spines.Length - 1);
                    Paint(cursor, -1, false);
                }
                if (ConfirmPressed())
                {
                    chosen = cursor;
                    break;
                }
                yield return null;
            }

            if (chosen == request.correct)
            {
                hits++;
                AudioManager.PlaySfx(goodClip);
                feedbackText.text = "¡Ese es! Lo anotas en tu ficha.";
            }
            else
            {
                AudioManager.PlaySfx(badClip);
                feedbackText.text = chosen < 0 ? "Se acabó el tiempo para ese libro..." : "Ese código no coincide.";
                if (StressSystem.Instance != null)
                {
                    StressSystem.Instance.Add(stressPerMiss);
                }
            }
            Paint(chosen, request.correct, true);
            yield return new WaitForSeconds(1.2f);
        }

        promptText.text = hits == requests.Length ? "Tienes todos los libros para tu informe." : "Conseguiste parte de la bibliografía.";
        progressText.text = $"Encontrados: {hits}/{requests.Length}";
        feedbackText.text = hits == requests.Length ? "La biblioteca ya no te parece tan grande." : "La bibliotecaria te ayuda con los que faltan.";
        result(reliefPerHit * hits);
    }

    // Mientras se busca se marca el cursor; al elegir, el correcto en verde y el error en rojo
    private void Paint(int cursor, int correct, bool reveal)
    {
        for (int i = 0; i < spines.Length; i++)
        {
            Color color = spineColor;
            if (reveal)
            {
                color = i == correct ? rightColor : i == cursor ? wrongColor : spineColor;
            }
            else if (i == cursor)
            {
                color = cursorColor;
            }
            spines[i].color = color;
        }
    }
}
