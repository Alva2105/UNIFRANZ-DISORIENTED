using System;
using UnityEngine;

// Estado de un objetivo respecto al avance del jugador
public enum ObjectiveState
{
    Pending,   // Todavía no le toca (faltan objetivos anteriores)
    Current,   // Es un objetivo activo (en un bloque libre puede haber varios)
    Done       // Ya se completó
}

// Checklist de Inducción de un hito: objetivos en orden que se completan uno tras
// otro. Los objetivos seguidos marcados como "anyOrder" forman un bloque libre: se
// activan juntos y se pueden completar en cualquier orden. Al completar el último se
// cierra el hito con LevelFlow (pantalla de victoria).
public class ObjectiveTracker : MonoBehaviour
{
    [Serializable]
    public class Objective
    {
        [Tooltip("Identificador que usan los interactuables para completarlo.")]
        public string id;

        [Tooltip("Texto que se muestra en el HUD.")]
        public string description;

        [Tooltip("Junto con los objetivos vecinos marcados igual, se puede hacer en cualquier orden.")]
        public bool anyOrder;
    }

    public static ObjectiveTracker Instance { get; private set; }

    [Tooltip("Nombre del hito que muestra el HUD, p. ej. \"HITO 1\".")]
    [SerializeField] private string milestoneTitle = "HITO 1";

    [SerializeField] private Objective[] objectives;

    // --- Sonidos ---
    [SerializeField] private AudioClip objectiveCompletedClip;
    [SerializeField] private AudioClip milestoneCompletedClip;

    // Primer objetivo sin completar y objetivos ya hechos
    private int currentIndex;
    private bool[] done;

    // Avisa cada vez que cambia el objetivo activo (lo escucha el HUD)
    public event Action Changed;

    public string MilestoneTitle => milestoneTitle;
    public int CompletedCount => Array.FindAll(done, d => d).Length;
    public int TotalCount => objectives.Length;
    public bool AllDone => currentIndex >= objectives.Length;
    public string CurrentDescription => AllDone ? "Completado" : DescribeCurrent();

    private void Awake()
    {
        Instance = this;
        done = new bool[objectives.Length];
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
        if (done[index])
        {
            return ObjectiveState.Done;
        }
        return index == currentIndex || index < GroupEnd(currentIndex) ? ObjectiveState.Current : ObjectiveState.Pending;
    }

    // Completa el objetivo si es el activo; devuelve false si no le tocaba
    public bool Complete(string id)
    {
        if (GetState(id) != ObjectiveState.Current)
        {
            return false;
        }

        done[Array.FindIndex(objectives, o => o.id == id)] = true;
        while (currentIndex < objectives.Length && done[currentIndex])
        {
            currentIndex++;
        }
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

    // Índice siguiente al bloque libre que empieza en start (start + 1 si no es un bloque)
    private int GroupEnd(int start)
    {
        if (start >= objectives.Length || !objectives[start].anyOrder)
        {
            return start + 1;
        }
        int end = start;
        while (end < objectives.Length && objectives[end].anyOrder)
        {
            end++;
        }
        return end;
    }

    // En un bloque libre se listan todos los pendientes: "Pendientes: A, B, C"
    private string DescribeCurrent()
    {
        int end = GroupEnd(currentIndex);
        if (end - currentIndex <= 1)
        {
            return objectives[currentIndex].description;
        }
        var pending = new System.Collections.Generic.List<string>();
        for (int i = currentIndex; i < end; i++)
        {
            if (!done[i])
            {
                pending.Add(objectives[i].description);
            }
        }
        return pending.Count == 1 ? pending[0] : "Pendientes: " + string.Join(", ", pending);
    }
}
