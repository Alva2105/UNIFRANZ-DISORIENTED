using UnityEngine;
using UnityEngine.Events;

// Interactuable ligado a un objetivo del Checklist (NPC, tótem, docente). Su
// diálogo depende del avance:
//   - Pendiente: todavía no le toca (linesPending, suele subir un poco el estrés).
//   - Activo: usa las líneas normales de Interactable y al cerrar completa el objetivo.
//   - Hecho: un recordatorio corto (linesDone), sin efecto en el estrés.
public class ObjectiveInteractable : Interactable
{
    [Header("Objetivo")]
    [Tooltip("Id del objetivo de ObjectiveTracker que completa este interactuable.")]
    [SerializeField] private string objectiveId;

    [Tooltip("Lo que dice si el jugador llega antes de tiempo.")]
    [SerializeField, TextArea(2, 4)] private string[] linesPending;

    [Tooltip("Estrés que suma llegar antes de tiempo (desorientación).")]
    [SerializeField] private float stressPending = 2f;

    [Tooltip("Lo que dice cuando el objetivo ya está hecho.")]
    [SerializeField, TextArea(2, 4)] private string[] linesDone;

    [Header("Aspecto al completarse (opcional)")]
    [SerializeField] private SpriteRenderer visual;
    [SerializeField] private Sprite doneSprite;

    [Header("Sonidos")]
    [SerializeField] private AudioClip interactClip;
    [SerializeField] private AudioClip pendingClip;

    [Tooltip("Se dispara al cerrar el diálogo cuando el objetivo ya estaba hecho (p. ej. repetir un minijuego).")]
    [SerializeField] private UnityEvent onDoneFinished = new UnityEvent();

    public override void Interact()
    {
        ObjectiveTracker tracker = ObjectiveTracker.Instance;
        ObjectiveState state = tracker != null ? tracker.GetState(objectiveId) : ObjectiveState.Current;

        switch (state)
        {
            case ObjectiveState.Current:
                AudioManager.PlaySfx(interactClip);
                DialogueBox.Instance.Show(speaker, lines, CompleteObjective);
                break;

            case ObjectiveState.Pending:
                AudioManager.PlaySfx(pendingClip);
                DialogueBox.Instance.Show(speaker, linesPending, () => AddStress(stressPending));
                break;

            default:
                AudioManager.PlaySfx(interactClip);
                DialogueBox.Instance.Show(speaker, linesDone, onDoneFinished.Invoke);
                break;
        }
    }

    private void CompleteObjective()
    {
        // Primero el efecto del Interactable (estrés y onFinished) y luego el objetivo,
        // así la victoria del último objetivo llega con el estrés ya actualizado
        Finish();
        if (visual != null && doneSprite != null)
        {
            visual.sprite = doneSprite;
        }
        if (ObjectiveTracker.Instance != null)
        {
            ObjectiveTracker.Instance.Complete(objectiveId);
        }
    }

    private static void AddStress(float amount)
    {
        if (StressSystem.Instance != null)
        {
            StressSystem.Instance.Add(amount);
        }
    }
}
