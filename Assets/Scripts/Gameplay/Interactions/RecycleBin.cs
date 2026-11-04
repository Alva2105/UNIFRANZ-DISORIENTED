using UnityEngine;

// Contenedor de reciclaje: recibe la basura que lleva el jugador. Al juntar la
// cantidad pedida completa su objetivo del Checklist (si ya se juntó antes de que el
// objetivo esté activo, se completa en cuanto le toque).
public class RecycleBin : Interactable
{
    [SerializeField] private string objectiveId = "reciclaje";
    [SerializeField] private int required = 5;
    [SerializeField] private AudioClip dropClip;

    private int deposited;

    private void Start()
    {
        if (ObjectiveTracker.Instance != null)
        {
            ObjectiveTracker.Instance.Changed += TryComplete;
        }
    }

    private void OnDestroy()
    {
        if (ObjectiveTracker.Instance != null)
        {
            ObjectiveTracker.Instance.Changed -= TryComplete;
        }
    }

    public override void Interact()
    {
        int dropped = TrashPickup.DropAll();
        if (dropped == 0)
        {
            DialogueBox.Instance.Show(speaker, new[]
            {
                $"Contenedor de reciclaje. Llevas {deposited} de {required}.",
                "(Busca basura tirada por el pasillo.)"
            }, null);
            return;
        }

        deposited += dropped;
        AudioManager.PlaySfx(dropClip);
        string line = deposited >= required
            ? "¡Listo! El pasillo quedó limpio. Te sientes mejor."
            : $"Botas {dropped} al contenedor. Llevas {deposited} de {required}.";
        DialogueBox.Instance.Show(speaker, new[] { line }, () =>
        {
            if (StressSystem.Instance != null)
            {
                StressSystem.Instance.Add(stressChange * dropped);
            }
            TryComplete();
        });
    }

    private void TryComplete()
    {
        if (deposited >= required && ObjectiveTracker.Instance != null
            && ObjectiveTracker.Instance.GetState(objectiveId) == ObjectiveState.Current)
        {
            ObjectiveTracker.Instance.Complete(objectiveId);
        }
    }
}
