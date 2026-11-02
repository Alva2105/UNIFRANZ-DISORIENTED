using UnityEngine;

// Punto para recuperarse (dispensador de agua): baja el estrés cada vez que se usa,
// pero necesita un tiempo de espera entre usos para que no se pueda abusar.
public class RestPoint : Interactable
{
    [Tooltip("Segundos de espera entre usos.")]
    [SerializeField] private float cooldown = 25f;

    [Tooltip("Lo que dice mientras está en espera.")]
    [SerializeField, TextArea(2, 4)] private string[] linesCooldown;

    [SerializeField] private AudioClip useClip;

    private float readyTime;

    public override void Interact()
    {
        if (Time.time < readyTime)
        {
            DialogueBox.Instance.Show(speaker, linesCooldown, null);
            return;
        }

        AudioManager.PlaySfx(useClip);
        DialogueBox.Instance.Show(speaker, lines, () =>
        {
            readyTime = Time.time + cooldown;
            if (StressSystem.Instance != null)
            {
                StressSystem.Instance.Add(stressChange);
            }
        });
    }
}
