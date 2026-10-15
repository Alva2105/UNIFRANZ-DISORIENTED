using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Parpadeo de luz fluorescente fallando: oscurece por instantes una imagen negra
// que cubre la pantalla. Refuerza el tono de terror psicológico del menú.
public class MenuFlicker : MonoBehaviour
{
    // --- Referencias ---
    [Tooltip("Imagen (normalmente negra) cuya transparencia se hará parpadear.")]
    [SerializeField] private Graphic target;

    // --- Parámetros del parpadeo ---
    [Tooltip("Opacidad máxima que alcanza un parpadeo.")]
    [SerializeField, Range(0f, 1f)] private float maxAlpha = 0.45f;

    [Tooltip("Segundos de espera entre ráfagas de parpadeo (mínimo y máximo).")]
    [SerializeField] private Vector2 waitRange = new Vector2(2f, 6f);

    [Tooltip("Cantidad de parpadeos por ráfaga (mínimo y máximo).")]
    [SerializeField] private Vector2Int blinkRange = new Vector2Int(1, 4);

    [Tooltip("Duración de cada parpadeo, en segundos.")]
    [SerializeField] private float blinkDuration = 0.06f;

    private Coroutine routine;

    private void Reset()
    {
        // Al agregar el componente se toma el Graphic del mismo GameObject
        target = GetComponent<Graphic>();
    }

    private void OnEnable()
    {
        SetAlpha(0f);
        routine = StartCoroutine(FlickerLoop());
    }

    private void OnDisable()
    {
        // Detener la rutina y dejar la pantalla sin oscurecer
        if (routine != null)
        {
            StopCoroutine(routine);
        }
        SetAlpha(0f);
    }

    private IEnumerator FlickerLoop()
    {
        while (true)
        {
            // Esperar un tiempo aleatorio entre ráfagas (tiempo real, ignora la pausa)
            yield return new WaitForSecondsRealtime(Random.Range(waitRange.x, waitRange.y));

            // Ráfaga de parpadeos cortos con intensidad variable
            int blinks = Random.Range(blinkRange.x, blinkRange.y + 1);
            for (int i = 0; i < blinks; i++)
            {
                SetAlpha(Random.Range(maxAlpha * 0.4f, maxAlpha));
                yield return new WaitForSecondsRealtime(blinkDuration);
                SetAlpha(0f);
                yield return new WaitForSecondsRealtime(blinkDuration * Random.Range(0.5f, 2f));
            }
        }
    }

    // Cambia solo la opacidad del Graphic, conservando su color
    private void SetAlpha(float alpha)
    {
        if (target == null)
        {
            return;
        }
        Color c = target.color;
        c.a = alpha;
        target.color = c;
    }
}
