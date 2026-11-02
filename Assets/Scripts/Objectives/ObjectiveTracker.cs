using System;
using UnityEngine;

// Estado de un objetivo respecto al avance del jugador
public enum ObjectiveState
{
    Pending,   // Todavía no le toca (faltan objetivos anteriores)
    Current,   // Es el objetivo activo
    Done       // Ya se completó
}

// Checklist de Inducción de un hito: objetivos en orden que se completan uno tras
// otro. Al completar el último se cierra el hito con LevelFlow (pantalla de victoria).
public class ObjectiveTracker : MonoBehaviour
{
    [Serializable]
    public class Objective
    {
        [Tooltip("Identificador que usan los interactuables para completarlo.")]
        public string id;

        [Tooltip("Texto que se muestra en el HUD.")]
        public string description;
    }

    public static ObjectiveTracker Instance { get; private set; }

    [Tooltip("Nombre del hito que muestra el HUD, p. ej. \"HITO 1\".")]
    [SerializeField] private string milestoneTitle = "HITO 1";

    [SerializeField] private Objective[] objectives;

    // --- Sonidos ---
    [SerializeField] private AudioClip objectiveCompletedClip;
    [SerializeField] private AudioClip milestoneCompletedClip;

    private int currentIndex;

    // Avisa cada vez que cambia el objetivo activo (lo escucha el HUD)
    public event Action Changed;

    public string MilestoneTitle => milestoneTitle;
    public int CompletedCount => currentIndex;
    public int TotalCount => objectives.Length;
    public bool AllDone => currentIndex >= objectives.Length;
    public string CurrentDescription => AllDone ? "Completado" : objectives[currentIndex].description;

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

    public ObjectiveState GetState(string id)
    {
        int index = Array.FindIndex(objectives, o => o.id == id);
        if (index < 0)
        {
            Debug.LogWarning("[ObjectiveTracker] No existe el objetivo " + id);
            return ObjectiveState.Done;
        }
        return index < currentIndex ? ObjectiveState.Done
            : index == currentIndex ? ObjectiveState.Current
            : ObjectiveState.Pending;
    }

    // Completa el objetivo si es el activo; devuelve false si no le tocaba
    public bool Complete(string id)
    {
        if (GetState(id) != ObjectiveState.Current)
        {
            return false;
        }

        currentIndex++;
        Changed?.Invoke();

        if (AllDone)
        {
            AudioManager.PlaySfx(milestoneCompletedClip);
            if (LevelFlow.Instance != null)
            {
                LevelFlow.Instance.CompleteMilestone();
            }
        }
        else
        {
            AudioManager.PlaySfx(objectiveCompletedClip);
        }
        return true;
    }
}
