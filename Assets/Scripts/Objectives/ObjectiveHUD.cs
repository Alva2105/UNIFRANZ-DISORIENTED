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
    // Espacio libre que queda a la derecha del texto, antes del borde de la pantalla
    private const float RightMargin = 40f;

    private float designWidth;
    private float fittedFor = -1f;

    private void Start()
    {
        // En un bloque libre se listan todos los pendientes y el texto puede ser más ancho
        // que la franja: la letra se achica solo lo necesario para que quepa
        label.fontSizeMax = label.fontSize;
        label.fontSizeMin = Mathf.Floor(label.fontSize * MinFontScale);
        label.enableAutoSizing = true;
        designWidth = label.rectTransform.sizeDelta.x;
        FitToScreen();

        tracker.Changed += Refresh;
        Refresh();
    }

    private void LateUpdate()
    {
        FitToScreen();
    }

    // La franja mide lo mismo en todas las escenas, pensada para 16:9. En pantallas más
    // angostas (4:3, 16:10) se saldría por la derecha y el texto quedaría cortado: se
    // ajusta al ancho real del Canvas para que la letra se achique dentro de la pantalla
    private void FitToScreen()
    {
        RectTransform rect = label.rectTransform;
        if (!(rect.parent is RectTransform parent) || Mathf.Approximately(parent.rect.width, fittedFor))
        {
            return;
        }
        fittedFor = parent.rect.width;
        float available = parent.rect.width - rect.anchoredPosition.x - RightMargin;
        rect.sizeDelta = new Vector2(Mathf.Min(designWidth, available), rect.sizeDelta.y);
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
