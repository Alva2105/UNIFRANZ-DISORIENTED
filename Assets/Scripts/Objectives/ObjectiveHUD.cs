using TMPro;
using UnityEngine;

// Muestra en el HUD el objetivo activo del hito y cuántos van completados,
// p. ej. "HITO 1  Habla con la Guía de Bienestar  (1/4)".
public class ObjectiveHUD : MonoBehaviour
{
    [SerializeField] private ObjectiveTracker tracker;
    [SerializeField] private TMP_Text label;

    private void Start()
    {
        tracker.Changed += Refresh;
        Refresh();
    }

    private void OnDestroy()
    {
        if (tracker != null)
        {
            tracker.Changed -= Refresh;
        }
    }

    private void Refresh()
    {
        string progress = $"<color=#F96F13>({tracker.CompletedCount}/{tracker.TotalCount})</color>";
        label.text = $"{tracker.MilestoneTitle}  {tracker.CurrentDescription}  {progress}";
    }
}
