using TMPro;
using UnityEngine;

// Muestra en el HUD el objetivo activo del hito y cuántos van completados,
// p. ej. "HITO 1  Habla con la Guía de Bienestar  (1/4)".
public class ObjectiveHUD : MonoBehaviour
{
    [SerializeField] private ObjectiveTracker tracker;
    [SerializeField] private TMP_Text label;

    // Tamaño mínimo de letra, relativo al original, al achicar un texto largo
    private const float MinFontScale = 0.55f;

    private void Start()
    {
        // En un bloque libre se listan todos los pendientes y el texto puede ser más ancho
        // que la franja: la letra se achica solo lo necesario para que quepa
        label.fontSizeMax = label.fontSize;
        label.fontSizeMin = Mathf.Floor(label.fontSize * MinFontScale);
        label.enableAutoSizing = true;

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
