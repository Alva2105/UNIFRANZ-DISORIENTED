using System;
using UnityEngine;

// Sistema central de Estrés/Ansiedad. El estrés sube solo con el tiempo
// (desorientación), más rápido al correr, y los eventos del juego lo suben o
// bajan con Add. Al cruzar los umbrales cambia el nivel y avisa a quien escuche
// (HUD, escenario, audio). Al llegar al máximo dispara Maxed.
public class StressSystem : MonoBehaviour
{
    public static StressSystem Instance { get; private set; }

    // --- Ritmo de subida ---
    [Header("Subida pasiva")]
    [Tooltip("Estrés que se acumula por segundo caminando o quieto.")]
    [SerializeField] private float passiveRate = 1f;

    [Tooltip("Estrés por segundo mientras el jugador corre.")]
    [SerializeField] private float sprintRate = 2.5f;

    // --- Umbrales ---
    [Header("Umbrales")]
    [SerializeField] private float maxStress = 100f;

    [Tooltip("A partir de este valor el nivel pasa a Estrés.")]
    [SerializeField] private float stressThreshold = 35f;

    [Tooltip("A partir de este valor el nivel pasa a Ansiedad.")]
    [SerializeField] private float anxietyThreshold = 70f;

    // --- Referencias ---
    [Tooltip("Jugador, para saber si está corriendo.")]
    [SerializeField] private PlayerMovement player;

    // --- Estado ---
    public float Stress { get; private set; }
    public float Normalized => Stress / maxStress;
    public StressLevel Level { get; private set; }

    // Mientras sea true el estrés no sube solo (diálogos, menús, final del nivel)
    public bool Frozen { get; set; }

    public event Action<StressLevel> LevelChanged;
    public event Action Maxed;

    private bool maxed;

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Update()
    {
        if (Frozen)
        {
            return;
        }

        bool sprinting = player != null && player.IsSprinting;
        Add((sprinting ? sprintRate : passiveRate) * Time.deltaTime);
    }

    // Suma (o resta, con un valor negativo) estrés y actualiza el nivel
    public void Add(float amount)
    {
        if (maxed)
        {
            return;
        }

        Stress = Mathf.Clamp(Stress + amount, 0f, maxStress);
        UpdateLevel();

        if (Stress >= maxStress)
        {
            maxed = true;
            Maxed?.Invoke();
        }
    }

    private void UpdateLevel()
    {
        StressLevel level = Stress >= anxietyThreshold ? StressLevel.Anxiety
            : Stress >= stressThreshold ? StressLevel.Stress
            : StressLevel.Normal;

        if (level != Level)
        {
            Level = level;
            LevelChanged?.Invoke(level);
        }
    }
}
