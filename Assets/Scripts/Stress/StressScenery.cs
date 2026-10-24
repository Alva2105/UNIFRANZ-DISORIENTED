using UnityEngine;

// Cambia el escenario según el nivel de estrés con un fundido. Las variantes se
// apilan sobre la versión normal: la de Estrés se ve desde el nivel 1 y la de
// Ansiedad desde el nivel 2.
public class StressScenery : MonoBehaviour
{
    [Tooltip("Variante de Estrés (nivel 2 del arte).")]
    [SerializeField] private SpriteRenderer stressLayer;

    [Tooltip("Variante de Ansiedad (nivel 3 del arte).")]
    [SerializeField] private SpriteRenderer anxietyLayer;

    [Tooltip("Segundos que dura el fundido entre variantes.")]
    [SerializeField] private float fadeDuration = 1.5f;

    private float stressTarget;
    private float anxietyTarget;

    private void Start()
    {
        SetAlpha(stressLayer, 0f);
        SetAlpha(anxietyLayer, 0f);
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
        stressTarget = level >= StressLevel.Stress ? 1f : 0f;
        anxietyTarget = level >= StressLevel.Anxiety ? 1f : 0f;
    }

    private void Update()
    {
        float step = Time.deltaTime / Mathf.Max(fadeDuration, 0.01f);
        Fade(stressLayer, stressTarget, step);
        Fade(anxietyLayer, anxietyTarget, step);
    }

    private static void Fade(SpriteRenderer layer, float target, float step)
    {
        if (layer != null)
        {
            SetAlpha(layer, Mathf.MoveTowards(layer.color.a, target, step));
        }
    }

    private static void SetAlpha(SpriteRenderer layer, float alpha)
    {
        if (layer == null)
        {
            return;
        }
        Color c = layer.color;
        c.a = alpha;
        layer.color = c;
    }
}
