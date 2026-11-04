using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Minijuego "Prioriza tus pendientes": a mitad de semestre se juntan las tareas. Hay
// que ordenarlas eligiendo siempre la más urgente de las que quedan (la que vence
// primero). Con izquierda/derecha se elige la tarjeta y con Interactuar se agenda.
// Agendar una tarea que no era la más urgente sube el estrés.
public class PriorityMinigame : MinigameBase
{
    [Serializable]
    public class Task
    {
        public string name;
        [Tooltip("Días que faltan para la entrega (menos = más urgente).")]
        public int daysLeft;
    }

    [Header("Pendientes")]
    [SerializeField] private Task[] tasks;
    [SerializeField] private Image[] cards;
    [SerializeField] private TMP_Text[] cardTexts;
    [SerializeField] private TMP_Text promptText;
    [SerializeField] private TMP_Text feedbackText;
    [SerializeField] private TMP_Text progressText;

    [SerializeField] private float stressPerError = 3f;
    [SerializeField] private float reliefPerfect = -12f;

    [SerializeField] private Color cardColor = new Color(0.16f, 0.18f, 0.3f);
    [SerializeField] private Color cursorColor = new Color(0.98f, 0.44f, 0.08f);
    [SerializeField] private Color doneColor = new Color(0.2f, 0.5f, 0.35f);

    private int[] order;
    private int cursor;

    protected override IEnumerator Play(Action<float> result)
    {
        string key = ControlPlatform.InteractHint;
        // 0 = sin agendar; si no, el puesto en que se agendó
        order = new int[tasks.Length];
        cursor = 0;
        int errors = 0;
        promptText.text = "Agenda primero lo que vence antes";
        feedbackText.text = $"< > elige la tarea   {key} agéndala";

        for (int slot = 1; slot <= tasks.Length; slot++)
        {
            progressText.text = $"Agendadas {slot - 1}/{tasks.Length}";
            Redraw();
            while (true)
            {
                int step = HorizontalPressed();
                if (step != 0)
                {
                    cursor = (cursor + step + tasks.Length) % tasks.Length;
                    Redraw();
                }
                if (ConfirmPressed())
                {
                    if (order[cursor] != 0)
                    {
                        feedbackText.text = "Esa ya está agendada.";
                    }
                    else if (tasks[cursor].daysLeft > MostUrgentLeft())
                    {
                        errors++;
                        AudioManager.PlaySfx(badClip);
                        feedbackText.text = $"Hay algo que vence antes que \"{tasks[cursor].name}\".";
                        if (StressSystem.Instance != null)
                        {
                            StressSystem.Instance.Add(stressPerError);
                        }
                    }
                    else
                    {
                        order[cursor] = slot;
                        AudioManager.PlaySfx(goodClip);
                        feedbackText.text = $"{slot}. {tasks[cursor].name}. ¡Bien!";
                        break;
                    }
                }
                yield return null;
            }
            yield return null;
        }

        Redraw();
        promptText.text = "¡Semana organizada!";
        progressText.text = errors == 0 ? "Sin errores" : $"Errores: {errors}";
        feedbackText.text = errors == 0 ? "Con un plan todo se ve más posible."
            : "Ordenar prioridades se aprende con práctica.";
        result(errors == 0 ? reliefPerfect : reliefPerfect * 0.4f);
    }

    private int MostUrgentLeft()
    {
        int best = int.MaxValue;
        for (int i = 0; i < tasks.Length; i++)
        {
            if (order[i] == 0)
            {
                best = Mathf.Min(best, tasks[i].daysLeft);
            }
        }
        return best;
    }

    private void Redraw()
    {
        for (int i = 0; i < cards.Length; i++)
        {
            cards[i].color = i == cursor ? cursorColor : order[i] != 0 ? doneColor : cardColor;
            string days = tasks[i].daysLeft == 1 ? "mañana" : $"en {tasks[i].daysLeft} días";
            string number = order[i] != 0 ? $"<size=130%>{order[i]}</size>\n" : "";
            cardTexts[i].text = $"{number}{tasks[i].name}\n<size=75%>Vence {days}</size>";
        }
    }
}
