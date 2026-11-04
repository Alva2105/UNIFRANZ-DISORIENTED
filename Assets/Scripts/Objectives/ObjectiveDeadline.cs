using System;
using TMPro;
using UnityEngine;

// Reloj de clase: algunos objetivos tienen tiempo límite desde que pasan a ser el
// objetivo activo (p. ej. llegar al aula antes de que empiece la clase). El tiempo se
// muestra en el HUD y se detiene durante diálogos, minijuegos y la pausa. Si se acaba,
// no se pierde el nivel: sube el estrés y el aviso cambia a "llegas tarde".
public class ObjectiveDeadline : MonoBehaviour
{
    [Serializable]
    public class Deadline
    {
        public string objectiveId;
        public float seconds = 30f;
        [Tooltip("Estrés que suma llegar tarde.")]
        public float lateStress = 12f;
        public string label = "La clase empieza en";
    }

    [SerializeField] private ObjectiveTracker tracker;
    [SerializeField] private TMP_Text clockText;
    [SerializeField] private Deadline[] deadlines;
    [SerializeField] private AudioClip lateClip;

    [SerializeField] private Color normalColor = new Color(0.92f, 0.92f, 0.95f);
    [SerializeField] private Color urgentColor = new Color(0.98f, 0.35f, 0.25f);

    private Deadline current;
    private float remaining;
    private bool late;

    private void Start()
    {
        tracker.Changed += OnObjectiveChanged;
        OnObjectiveChanged();
    }

    private void OnDestroy()
    {
        if (tracker != null)
        {
            tracker.Changed -= OnObjectiveChanged;
        }
    }

    private void OnObjectiveChanged()
    {
        current = Array.Find(deadlines, d => tracker.GetState(d.objectiveId) == ObjectiveState.Current);
        late = false;
        remaining = current != null ? current.seconds : 0f;
        clockText.gameObject.SetActive(current != null);
        Refresh();
    }

    private void Update()
    {
        if (current == null || late)
        {
            return;
        }
        // Frozen cubre diálogos, minijuegos y el final del nivel; timeScale 0 cubre la pausa
        if (StressSystem.Instance != null && StressSystem.Instance.Frozen)
        {
            return;
        }

        remaining -= Time.deltaTime;
        if (remaining <= 0f)
        {
            late = true;
            remaining = 0f;
            AudioManager.PlaySfx(lateClip);
            if (StressSystem.Instance != null)
            {
                StressSystem.Instance.Add(current.lateStress);
            }
        }
        Refresh();
    }

    private void Refresh()
    {
        if (current == null)
        {
            return;
        }
        if (late)
        {
            clockText.text = "¡Vas tarde! Apúrate";
            clockText.color = urgentColor;
            return;
        }
        int seconds = Mathf.CeilToInt(remaining);
        clockText.text = $"{current.label} {seconds / 60}:{seconds % 60:00}";
        clockText.color = remaining <= 10f ? urgentColor : normalColor;
    }
}
