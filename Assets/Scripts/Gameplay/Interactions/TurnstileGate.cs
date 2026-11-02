using UnityEngine;

// Fila de molinetes de ingreso: bloquea el paso con un collider hasta que se completa
// su objetivo (pasar el carnet). Entonces se abre: quita el bloqueo y cambia el arte de
// cada capa de la fila (la que queda detrás del jugador y la que queda delante).
public class TurnstileGate : MonoBehaviour
{
    [Tooltip("Objetivo del Checklist que abre los molinetes.")]
    [SerializeField] private string objectiveId = "molinete";

    [SerializeField] private Collider2D blocker;

    [Tooltip("Capas de la fila y su arte abierto, en el mismo orden.")]
    [SerializeField] private SpriteRenderer[] visuals;
    [SerializeField] private Sprite[] openSprites;

    private void Start()
    {
        if (ObjectiveTracker.Instance != null)
        {
            ObjectiveTracker.Instance.Changed += Refresh;
        }
        Refresh();
    }

    private void OnDestroy()
    {
        if (ObjectiveTracker.Instance != null)
        {
            ObjectiveTracker.Instance.Changed -= Refresh;
        }
    }

    private void Refresh()
    {
        bool open = ObjectiveTracker.Instance == null
            || ObjectiveTracker.Instance.GetState(objectiveId) == ObjectiveState.Done;
        blocker.enabled = !open;
        if (!open)
        {
            return;
        }
        for (int i = 0; i < visuals.Length && i < openSprites.Length; i++)
        {
            visuals[i].sprite = openSprites[i];
        }
    }
}
