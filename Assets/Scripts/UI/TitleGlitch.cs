using System.Collections;
using TMPro;
using UnityEngine;

// Efecto "glitch" del título: cada cierto tiempo el título tiembla y cambia de
// color por un instante, anticipando las alteraciones del sistema de Estrés/Ansiedad.
public class TitleGlitch : MonoBehaviour
{
    // --- Referencias ---
    [Tooltip("Elemento que se desplaza durante el glitch (por defecto, este mismo).")]
    [SerializeField] private RectTransform target;

    [Tooltip("Texto que cambia de color durante el glitch.")]
    [SerializeField] private TMP_Text text;

    // --- Parámetros del glitch ---
    [SerializeField] private Color glitchColor = new Color(0.85f, 0.1f, 0.1f);

    [Tooltip("Segundos de espera entre glitches (mínimo y máximo).")]
    [SerializeField] private Vector2 waitRange = new Vector2(3f, 7f);

    [Tooltip("Desplazamiento máximo en píxeles de UI.")]
    [SerializeField] private float jitter = 6f;

    [Tooltip("Cantidad de saltos por glitch.")]
    [SerializeField] private int steps = 4;

    [Tooltip("Duración de cada salto, en segundos.")]
    [SerializeField] private float stepDuration = 0.05f;

    private Vector2 basePosition;
    private Color baseColor;
    private Coroutine routine;

    private void Awake()
    {
        // Guardar posición y color originales para restaurarlos tras cada glitch
        if (target == null)
        {
            target = (RectTransform)transform;
        }
        basePosition = target.anchoredPosition;
        if (text != null)
        {
            baseColor = text.color;
        }
    }

    private void OnEnable()
    {
        routine = StartCoroutine(GlitchLoop());
    }

    private void OnDisable()
    {
        if (routine != null)
        {
            StopCoroutine(routine);
        }
        Restore();
    }

    private IEnumerator GlitchLoop()
    {
        while (true)
        {
            // Espera aleatoria en tiempo real (funciona aunque el juego esté en pausa)
            yield return new WaitForSecondsRealtime(Random.Range(waitRange.x, waitRange.y));

            // Saltos rápidos de posición alternando el color del texto
            for (int i = 0; i < steps; i++)
            {
                Vector2 offset = new Vector2(Random.Range(-jitter, jitter), Random.Range(-jitter, jitter) * 0.3f);
                target.anchoredPosition = basePosition + offset;
                if (text != null)
                {
                    text.color = i % 2 == 0 ? glitchColor : baseColor;
                }
                yield return new WaitForSecondsRealtime(stepDuration);
            }

            Restore();
        }
    }

    // Devuelve el título a su estado original
    private void Restore()
    {
        if (target != null)
        {
            target.anchoredPosition = basePosition;
        }
        if (text != null)
        {
            text.color = baseColor;
        }
    }
}
