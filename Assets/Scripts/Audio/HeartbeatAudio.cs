using UnityEngine;

// Latido del corazón como señal auditiva del estrés (GDD): no suena en estado
// Normal, suena pausado con Estrés y rápido y más fuerte con Ansiedad.
// Cada latido son dos golpes ("lub-dub").
public class HeartbeatAudio : MonoBehaviour
{
    [SerializeField] private AudioClip lub;
    [SerializeField] private AudioClip dub;

    [Tooltip("Segundos entre latidos con Estrés y con Ansiedad.")]
    [SerializeField] private float stressInterval = 1.1f;
    [SerializeField] private float anxietyInterval = 0.62f;

    [Tooltip("Separación entre los dos golpes de un latido.")]
    [SerializeField] private float dubDelay = 0.18f;

    [Range(0f, 1f)]
    [SerializeField] private float stressVolume = 0.35f;
    [Range(0f, 1f)]
    [SerializeField] private float anxietyVolume = 0.7f;

    private AudioSource source;
    private float timer;
    private float dubTimer = -1f;

    private void Awake()
    {
        source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
    }

    private void Update()
    {
        StressSystem stress = StressSystem.Instance;
        bool over = LevelFlow.Instance != null && LevelFlow.Instance.IsOver;
        if (stress == null || stress.Level == StressLevel.Normal || over)
        {
            timer = 0f;
            return;
        }

        bool anxiety = stress.Level == StressLevel.Anxiety;
        float volume = (anxiety ? anxietyVolume : stressVolume) * AudioManager.EffectsVolume;

        // Time.deltaTime: en pausa (timeScale 0) el latido se detiene
        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            timer = anxiety ? anxietyInterval : stressInterval;
            source.PlayOneShot(lub, volume);
            dubTimer = dubDelay;
        }

        if (dubTimer >= 0f)
        {
            dubTimer -= Time.deltaTime;
            if (dubTimer < 0f)
            {
                source.PlayOneShot(dub, volume * 0.8f);
            }
        }
    }
}
