using System.Text;
using TMPro;
using UnityEngine;

// Resumen del semestre en la pantalla final del último hito: notas de los exámenes,
// cuántos se aprobaron a la primera y el estrés máximo del día. Se escribe en el
// mensaje de la pantalla de victoria justo cuando se completa el Checklist.
public class SemesterSummary : MonoBehaviour
{
    [SerializeField] private ObjectiveTracker tracker;
    [SerializeField] private ExamMinigame[] exams;
    [SerializeField] private TMP_Text message;

    [SerializeField, TextArea(1, 3)] private string closing = "Terminaste tu primer semestre en UNIFRANZ.";

    private float maxStress;

    private void Start()
    {
        tracker.Changed += OnChanged;
    }

    private void OnDestroy()
    {
        if (tracker != null)
        {
            tracker.Changed -= OnChanged;
        }
    }

    private void Update()
    {
        if (StressSystem.Instance != null && !tracker.AllDone)
        {
            maxStress = Mathf.Max(maxStress, StressSystem.Instance.Normalized);
        }
    }

    private void OnChanged()
    {
        if (!tracker.AllDone)
        {
            return;
        }
        var text = new StringBuilder(closing).Append("\n\n");
        int firstTry = 0;
        float total = 0f;
        foreach (ExamMinigame exam in exams)
        {
            text.Append($"{exam.Subject}: {exam.BestGrade}/100");
            text.Append(exam.Attempts > 1 ? $" ({exam.Attempts} intentos)\n" : "\n");
            firstTry += exam.Attempts == 1 ? 1 : 0;
            total += exam.BestGrade;
        }
        if (exams.Length > 0)
        {
            text.Append($"Promedio: {Mathf.RoundToInt(total / exams.Length)}/100   Aprobados a la primera: {firstTry}/{exams.Length}\n");
        }
        text.Append($"Estrés máximo del día: {Mathf.RoundToInt(maxStress * 100f)}%");
        message.text = text.ToString();
    }
}
