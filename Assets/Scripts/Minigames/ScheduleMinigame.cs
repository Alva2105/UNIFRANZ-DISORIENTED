using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Minijuego "Arma tu horario": cada materia solo puede ir en ciertos días. Con
// izquierda/derecha se elige el día y con Interactuar se coloca la materia. Un día
// ocupado o no permitido es un error (sube un poco el estrés). Al terminar el horario,
// sin errores baja el estrés; con errores, baja menos o sube.
public class ScheduleMinigame : MinigameBase
{
    [Serializable]
    public class Subject
    {
        public string name;
        [Tooltip("Nombre corto que se escribe en la grilla.")]
        public string shortName;
        [Tooltip("Días permitidos: 0 = Lunes ... 4 = Viernes.")]
        public int[] days;
    }

    private static readonly string[] DayNames = { "Lunes", "Martes", "Miércoles", "Jueves", "Viernes" };

    [Header("Horario")]
    [SerializeField] private Subject[] subjects;
    [SerializeField] private Image[] dayCells;
    [SerializeField] private TMP_Text[] dayTexts;
    [SerializeField] private TMP_Text cardText;
    [SerializeField] private TMP_Text feedbackText;
    [SerializeField] private TMP_Text progressText;

    [SerializeField] private float stressPerError = 2f;
    [SerializeField] private float reliefPerfect = -10f;

    [SerializeField] private Color freeColor = new Color(0.16f, 0.18f, 0.3f);
    [SerializeField] private Color cursorColor = new Color(0.98f, 0.44f, 0.08f);
    [SerializeField] private Color filledColor = new Color(0.2f, 0.5f, 0.35f);

    private string[] placed;
    private int cursor;

    protected override IEnumerator Play(Action<float> result)
    {
        placed = new string[DayNames.Length];
        cursor = 0;
        int errors = 0;
        string key = ControlPlatform.InteractHint;
        feedbackText.text = $"< > elige el día   {key} coloca la materia";

        for (int s = 0; s < subjects.Length; s++)
        {
            Subject subject = subjects[s];
            progressText.text = $"Materia {s + 1}/{subjects.Length}";
            cardText.text = $"{subject.name}\n<size=70%>Solo: {DaysText(subject.days)}</size>";
            Redraw();

            while (true)
            {
                int step = HorizontalPressed();
                if (step != 0)
                {
                    cursor = (cursor + step + DayNames.Length) % DayNames.Length;
                    Redraw();
                }
                if (ConfirmPressed())
                {
                    if (placed[cursor] != null)
                    {
                        feedbackText.text = $"El {DayNames[cursor]} ya tienes {placed[cursor]}.";
                    }
                    else if (Array.IndexOf(subject.days, cursor) < 0)
                    {
                        feedbackText.text = $"{subject.shortName} no se dicta el {DayNames[cursor]}.";
                    }
                    else
                    {
                        placed[cursor] = subject.shortName;
                        AudioManager.PlaySfx(goodClip);
                        feedbackText.text = $"{subject.shortName} el {DayNames[cursor]}. ¡Bien!";
                        break;
                    }
                    errors++;
                    AudioManager.PlaySfx(badClip);
                    // Frozen solo detiene la subida pasiva; Add sigue funcionando
                    if (StressSystem.Instance != null)
                    {
                        StressSystem.Instance.Add(stressPerError);
                    }
                }
                yield return null;
            }
            yield return null;
        }

        Redraw();
        cardText.text = "¡Horario listo!";
        progressText.text = errors == 0 ? "Sin errores" : $"Errores: {errors}";
        feedbackText.text = errors == 0 ? "Te organizaste muy bien. Te sientes más tranquil@."
            : "Te costó un poco, pero ya tienes tu semana armada.";
        result(errors == 0 ? reliefPerfect : reliefPerfect * 0.5f);
    }

    private void Redraw()
    {
        for (int i = 0; i < dayCells.Length; i++)
        {
            dayCells[i].color = i == cursor ? cursorColor : placed[i] != null ? filledColor : freeColor;
            dayTexts[i].text = $"{DayNames[i]}\n\n{placed[i] ?? "-"}";
        }
    }

    private static string DaysText(int[] days)
    {
        return string.Join(" o ", Array.ConvertAll(days, d => DayNames[d]));
    }
}
