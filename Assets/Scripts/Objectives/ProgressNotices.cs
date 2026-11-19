using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

// Avisos grandes y breves en la parte alta de la pantalla para que el jugador sepa qué
// pasó y qué sigue: al empezar el hito, al completar un objetivo (y cuál sigue), al
// llegar a otro piso y la primera vez que sube el nivel de estrés. Se muestran en cola,
// uno tras otro, y no detienen el juego.
public class ProgressNotices : MonoBehaviour
{
    [SerializeField] private ObjectiveTracker tracker;
    [SerializeField] private Transform player;
    [SerializeField] private CanvasGroup group;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text bodyText;

    [SerializeField] private float showSeconds = 2.8f;
    [SerializeField] private float fadeSeconds = 0.3f;

    private readonly Queue<(string title, string body)> queue = new Queue<(string, string)>();
    private FloorLabel[] floors;
    private FloorLabel currentFloor;
    private int completed;
    private bool warnedStress;
    private bool warnedAnxiety;

    private void Start()
    {
        group.alpha = 0f;
        floors = FindObjectsByType<FloorLabel>(FindObjectsSortMode.None);
        currentFloor = FloorLabel.At(floors, player.position.x);
        tracker.Changed += OnObjectiveChanged;
        if (StressSystem.Instance != null)
        {
            StressSystem.Instance.LevelChanged += OnStressChanged;
        }
        string where = currentFloor != null ? $"\n<size=80%>Estás en: {currentFloor.Title}</size>" : "";
        queue.Enqueue((tracker.MilestoneTitle, "Objetivo: " + tracker.CurrentDescription + where));
        StartCoroutine(Show());
    }

    private void OnDestroy()
    {
        if (tracker != null)
        {
            tracker.Changed -= OnObjectiveChanged;
        }
        if (StressSystem.Instance != null)
        {
            StressSystem.Instance.LevelChanged -= OnStressChanged;
        }
    }

    private void Update()
    {
        FloorLabel floor = FloorLabel.At(floors, player.position.x);
        if (floor != null && floor != currentFloor)
        {
            currentFloor = floor;
            queue.Enqueue((floor.Title, floor.Subtitle));
        }
    }

    private void OnObjectiveChanged()
    {
        if (tracker.CompletedCount <= completed)
        {
            return;
        }
        completed = tracker.CompletedCount;
        // El último objetivo ya lo celebra la pantalla de victoria
        if (!tracker.AllDone)
        {
            queue.Enqueue(("¡OBJETIVO COMPLETADO!",
                $"{tracker.LastCompletedDescription}\n<size=80%>Siguiente: {tracker.CurrentDescription}</size>"));
        }
    }

    private void OnStressChanged(StressLevel level)
    {
        if (level == StressLevel.Stress && !warnedStress)
        {
            warnedStress = true;
            queue.Enqueue(("TU ESTRÉS SUBE",
                "Correr y perder tiempo te estresan.\n<size=80%>Bebe agua o completa objetivos para calmarte.</size>"));
        }
        else if (level == StressLevel.Anxiety && !warnedAnxiety)
        {
            warnedAnxiety = true;
            queue.Enqueue(("¡CUIDADO: ANSIEDAD!",
                "Si el medidor se llena tendrás una crisis.\n<size=80%>Busca un dispensador de agua y avanza con calma.</size>"));
        }
    }

    private IEnumerator Show()
    {
        while (true)
        {
            // Se espera a que no haya diálogo, minijuego, pausa ni pantalla final encima
            bool busy = queue.Count == 0 || DialogueBox.IsOpen || MinigameBase.AnyOpen || BreathingMinigame.IsOpen
                || PauseMenu.IsPaused || (LevelFlow.Instance != null && LevelFlow.Instance.IsOver);
            if (busy)
            {
                yield return null;
                continue;
            }
            (string title, string body) = queue.Dequeue();
            titleText.text = title;
            bodyText.text = body;
            yield return Fade(0f, 1f);
            // En pausa el aviso se queda quieto: su tiempo en pantalla no corre detrás del menú
            for (float t = 0f; t < showSeconds && !DialogueBox.IsOpen && !MinigameBase.AnyOpen; )
            {
                if (!PauseMenu.IsPaused)
                {
                    t += Time.unscaledDeltaTime;
                }
                yield return null;
            }
            yield return Fade(1f, 0f);
        }
    }

    private IEnumerator Fade(float from, float to)
    {
        for (float t = 0f; t < fadeSeconds; t += Time.unscaledDeltaTime)
        {
            group.alpha = Mathf.Lerp(from, to, t / fadeSeconds);
            yield return null;
        }
        group.alpha = to;
    }
}
