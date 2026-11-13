using UnityEngine;

// Nombre de un piso para los avisos ("PISO 5 - Biblioteca y Aula Dinámica"). Va en el
// objeto de los límites de cámara de ese piso, que marcan qué parte del nivel es suya.
[RequireComponent(typeof(PolygonCollider2D))]
public class FloorLabel : MonoBehaviour
{
    [SerializeField] private string title = "PISO";
    [SerializeField] private string subtitle;

    public string Title => title;
    public string Subtitle => subtitle;

    private PolygonCollider2D area;

    // Se pregunta por x porque todos los pisos están a la misma altura, lado a lado
    public bool Contains(float x)
    {
        if (area == null)
        {
            area = GetComponent<PolygonCollider2D>();
        }
        Bounds bounds = area.bounds;
        return x >= bounds.min.x - 0.1f && x <= bounds.max.x + 0.1f;
    }

    public static FloorLabel At(FloorLabel[] floors, float x)
    {
        foreach (FloorLabel floor in floors)
        {
            if (floor.Contains(x))
            {
                return floor;
            }
        }
        return null;
    }
}
