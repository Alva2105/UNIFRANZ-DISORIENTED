using TMPro;
using UnityEngine;

// Reloj del día: el último hito tiene un tiempo total para todo el nivel (no por
// objetivo). Se detiene durante diálogos, minijuegos y la pausa. Si se acaba, no se
// pierde: el estrés empieza a subir más rápido hasta terminar el Checklist.
public class DayClock : MonoBehaviour
{
    [SerializeField] private ObjectiveTracker tracker;
    [SerializeField] private TMP_Text clockText;
    [SerializeField] private float totalSeconds = 480f;
    [SerializeField] private string label = "El día termina en";
    [Tooltip("Estrés por segundo una vez que se acabó el tiempo.")]
    [SerializeField] private float overtimeStress = 1f;
    [SerializeField] private AudioClip lateClip;

    [SerializeField] private Color normalColor = new Color(0.92f, 0.92f, 0.95f);
    [SerializeField] private Color urgentColor = new Color(0.98f, 0.35f, 0.25f);

    private float remaining;
    private bool late;

    private void Start()
    {
        remaining = totalSeconds;
        Refresh();
    }

    private void Update()
    {
        if (tracker.AllDone)
        {
            clockText.gameObject.SetActive(false);
            return;
        }
        // Frozen cubre diálogos y minijuegos; timeScale 0 cubre la pausa
        if (StressSystem.Instance != null && StressSystem.Instance.Frozen)
        {
            return;
        }

        if (late)
        {
            if (StressSystem.Instance != null)
            {
                StressSystem.Instance.Add(overtimeStress * Time.deltaTime);
            }
            return;
        }
        remaining -= Time.deltaTime;
        if (remaining <= 0f)
        {
            remaining = 0f;
            late = true;
            AudioManager.PlaySfx(lateClip);
        }
        Refresh();
    }

    private void Refresh()
    {
        if (late)
        {
            clockText.text = "¡Se acaba el día! Termina lo que falta";
            clockText.color = urgentColor;
            return;
        }
        int seconds = Mathf.CeilToInt(remaining);
        clockText.text = $"{label} {seconds / 60}:{seconds % 60:00}";
        clockText.color = remaining <= 60f ? urgentColor : normalColor;
    }
}
