using UnityEngine;

// Audio del nivel: la música de fondo en bucle y una fuente para los efectos.
// Respeta los volúmenes de Configuración (GameSettings) y, según el GDD, la música
// cambia con el estado del protagonista: con Estrés y Ansiedad se vuelve más grave
// y lenta, y baja un poco de volumen para dejar oír los latidos.
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Música")]
    [SerializeField] private AudioClip music;
    [Range(0f, 1f)]
    [SerializeField] private float musicVolume = 0.45f;

    [Header("Efectos")]
    [Range(0f, 1f)]
    [SerializeField] private float effectsVolume = 0.9f;

    [Header("Reacción al estrés (tono y volumen de la música)")]
    [SerializeField] private float stressPitch = 0.94f;
    [SerializeField] private float anxietyPitch = 0.85f;
    [SerializeField] private float anxietyVolume = 0.6f;
    [SerializeField] private float transitionSpeed = 0.5f;

    private AudioSource musicSource;
    private AudioSource effectsSource;
    private float targetPitch = 1f;
    private float targetVolumeScale = 1f;
    private float volumeScale = 1f;

    private void Awake()
    {
        Instance = this;

        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.clip = music;
        musicSource.loop = true;
        musicSource.playOnAwake = false;

        effectsSource = gameObject.AddComponent<AudioSource>();
        effectsSource.playOnAwake = false;

        GameSettings.Changed += ApplyVolumes;
        ApplyVolumes();
    }

    private void Start()
    {
        if (music != null)
        {
            musicSource.Play();
        }
        if (StressSystem.Instance != null)
        {
            StressSystem.Instance.LevelChanged += OnStressLevelChanged;
        }
    }

    private void OnDestroy()
    {
        GameSettings.Changed -= ApplyVolumes;
        if (StressSystem.Instance != null)
        {
            StressSystem.Instance.LevelChanged -= OnStressLevelChanged;
        }
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Update()
    {
        // Sin escala de tiempo: la transición sigue aunque el juego esté en pausa
        float step = transitionSpeed * Time.unscaledDeltaTime;
        musicSource.pitch = Mathf.MoveTowards(musicSource.pitch, targetPitch, step);
        float scale = Mathf.MoveTowards(volumeScale, targetVolumeScale, step);
        if (!Mathf.Approximately(scale, volumeScale))
        {
            volumeScale = scale;
            ApplyVolumes();
        }
    }

    // Reproduce un efecto si hay un AudioManager en la escena (si no, no hace nada)
    public static void PlaySfx(AudioClip clip, float volume = 1f)
    {
        if (Instance == null || clip == null)
        {
            return;
        }
        Instance.effectsSource.PlayOneShot(clip, volume);
    }

    // Volumen final de un efecto propio (p. ej. pasos con su propia fuente)
    public static float EffectsVolume => GameSettings.EffectsVolume * (Instance != null ? Instance.effectsVolume : 1f);

    private void ApplyVolumes()
    {
        musicSource.volume = GameSettings.MusicVolume * musicVolume * volumeScale;
        effectsSource.volume = GameSettings.EffectsVolume * effectsVolume;
    }

    private void OnStressLevelChanged(StressLevel level)
    {
        targetPitch = level == StressLevel.Anxiety ? anxietyPitch
            : level == StressLevel.Stress ? stressPitch
            : 1f;
        targetVolumeScale = level == StressLevel.Anxiety ? anxietyVolume : 1f;
    }
}
