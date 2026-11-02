using UnityEngine;

// Tiñe un sprite según el nivel de estrés, igual que se oscurecen los fondos: más
// gris con Estrés y rojizo y oscuro con Ansiedad. Sirve para objetos que no tienen
// variantes dibujadas (p. ej. los molinetes, que además cambian de arte al abrirse).
public class StressTint : MonoBehaviour
{
    [SerializeField] private SpriteRenderer[] targets;
    [SerializeField] private Color stressColor = new Color(0.72f, 0.7f, 0.8f);
    [SerializeField] private Color anxietyColor = new Color(0.62f, 0.38f, 0.42f);
    [SerializeField] private float fadeDuration = 1.5f;

    private Color target = Color.white;

    private void Start()
    {
        if (StressSystem.Instance != null)
        {
            StressSystem.Instance.LevelChanged += OnLevelChanged;
            OnLevelChanged(StressSystem.Instance.Level);
        }
    }

    private void OnDestroy()
    {
        if (StressSystem.Instance != null)
        {
            StressSystem.Instance.LevelChanged -= OnLevelChanged;
        }
    }

    private void OnLevelChanged(StressLevel level)
    {
        target = level == StressLevel.Anxiety ? anxietyColor
            : level == StressLevel.Stress ? stressColor
            : Color.white;
    }

    private void Update()
    {
        float step = Time.deltaTime / Mathf.Max(fadeDuration, 0.01f);
        foreach (SpriteRenderer sprite in targets)
        {
            sprite.color = Color.Lerp(sprite.color, target, step * 3f);
        }
    }
}
