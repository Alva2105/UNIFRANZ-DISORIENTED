using UnityEngine;

// Luces que parpadean: una capa oscura sobre un piso que, en Ansiedad, se oscurece de
// golpe por momentos (como un fluorescente fallando). En los demás niveles se apaga.
public class FlickerLights : MonoBehaviour
{
    [SerializeField] private SpriteRenderer shade;

    [Tooltip("Oscuridad de fondo mientras dura la Ansiedad.")]
    [SerializeField] private float baseAlpha = 0.12f;
    [SerializeField] private float flickerAlpha = 0.55f;
    [SerializeField] private float flickerSeconds = 0.08f;
    [SerializeField] private Vector2 pauseRange = new Vector2(0.3f, 2f);

    private float timer;
    private float flickerLeft;

    private void Update()
    {
        bool anxious = StressSystem.Instance != null && StressSystem.Instance.Level == StressLevel.Anxiety;
        float alpha = 0f;
        if (anxious)
        {
            timer -= Time.deltaTime;
            flickerLeft -= Time.deltaTime;
            if (timer <= 0f)
            {
                flickerLeft = flickerSeconds;
                timer = Random.Range(pauseRange.x, pauseRange.y);
            }
            alpha = flickerLeft > 0f ? flickerAlpha : baseAlpha;
        }
        Color color = shade.color;
        color.a = anxious ? alpha : Mathf.MoveTowards(color.a, 0f, Time.deltaTime);
        shade.color = color;
    }
}
