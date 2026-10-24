using UnityEngine;

// HUD de ansiedad: pasa el nivel actual al parámetro "nivel" de los Animator de
// la barra y de la cara (BarraNivel.controller y CaraNivel.controller).
public class StressHUD : MonoBehaviour
{
    private static readonly int LevelParam = Animator.StringToHash("nivel");

    [SerializeField] private Animator bar;
    [SerializeField] private Animator face;

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
        if (bar != null)
        {
            bar.SetInteger(LevelParam, (int)level);
        }
        if (face != null)
        {
            face.SetInteger(LevelParam, (int)level);
        }
    }
}
