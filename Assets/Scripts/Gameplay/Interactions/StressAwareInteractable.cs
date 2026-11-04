using UnityEngine;

// NPC que cambia lo que dice según el estrés del jugador: con Ansiedad sus frases se
// vuelven inquietantes (así se siente el mundo cuando uno está desbordado).
public class StressAwareInteractable : Interactable
{
    [Tooltip("Lo que dice cuando el jugador está en Ansiedad.")]
    [SerializeField, TextArea(2, 4)] private string[] linesAnxious;

    public override void Interact()
    {
        bool anxious = StressSystem.Instance != null && StressSystem.Instance.Level == StressLevel.Anxiety;
        DialogueBox.Instance.Show(speaker, anxious && linesAnxious.Length > 0 ? linesAnxious : lines, Finish);
    }
}
