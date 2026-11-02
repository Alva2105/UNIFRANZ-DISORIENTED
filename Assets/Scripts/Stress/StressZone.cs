using UnityEngine;

// Zona de aglomeración (GDD): mientras el jugador la cruza, el estrés sube más rápido.
// Se define como un tramo horizontal del pasillo, sin físicas.
public class StressZone : MonoBehaviour
{
    [SerializeField] private Transform player;

    [Tooltip("Inicio y fin de la zona en el eje X (unidades de mundo).")]
    [SerializeField] private float minX;
    [SerializeField] private float maxX;

    [Tooltip("Estrés extra por segundo dentro de la zona.")]
    [SerializeField] private float stressPerSecond = 2.5f;

    public bool PlayerInside => player != null && player.position.x >= minX && player.position.x <= maxX;

    private void Update()
    {
        StressSystem stress = StressSystem.Instance;
        // Frozen cubre diálogos, minijuegos y el final del nivel
        if (stress == null || stress.Frozen || !PlayerInside)
        {
            return;
        }
        stress.Add(stressPerSecond * Time.deltaTime);
    }
}
