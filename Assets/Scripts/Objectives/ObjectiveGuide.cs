using System.Collections.Generic;
using TMPro;
using UnityEngine;

// Guía hacia el objetivo: una flecha que flota sobre la persona u objeto con quien hay
// que interactuar y un texto en pantalla que indica hacia dónde caminar. Si el objetivo
// está en otro piso, guía hacia las gradas o el ascensor que llevan hacia él. En un
// bloque libre señala el pendiente más cercano. Se oculta en diálogos, minijuegos y al
// terminar el nivel.
public class ObjectiveGuide : MonoBehaviour
{
    [SerializeField] private ObjectiveTracker tracker;
    [SerializeField] private Transform player;
    [SerializeField] private SpriteRenderer marker;
    [SerializeField] private TMP_Text hintText;

    [SerializeField] private float markerHeight = 0.9f;
    [SerializeField] private float bobAmplitude = 0.15f;
    [SerializeField] private float bobSpeed = 4f;

    private FloorLabel[] floors;
    private FloorPortal[] portals;
    private ObjectiveInteractable[] objectives;
    private RecycleBin[] bins;
    private TrashPickup[] trash;

    private void Start()
    {
        floors = FindObjectsByType<FloorLabel>(FindObjectsSortMode.None);
        portals = FindObjectsByType<FloorPortal>(FindObjectsSortMode.None);
        objectives = FindObjectsByType<ObjectiveInteractable>(FindObjectsSortMode.None);
        bins = FindObjectsByType<RecycleBin>(FindObjectsSortMode.None);
        trash = FindObjectsByType<TrashPickup>(FindObjectsSortMode.None);
    }

    private void LateUpdate()
    {
        bool busy = tracker.AllDone || DialogueBox.IsOpen || MinigameBase.AnyOpen || BreathingMinigame.IsOpen
            || PauseMenu.IsPaused || (LevelFlow.Instance != null && LevelFlow.Instance.IsOver);
        string goal = "";
        Interactable target = busy ? null : FindTarget(out goal);
        if (target == null)
        {
            marker.enabled = false;
            ShowHint("");
            return;
        }
        goal = target is FloorPortal portal ? "Usa: " + portal.Prompt : goal;

        float dx = target.transform.position.x - player.position.x;
        bool close = Mathf.Abs(dx) <= target.Range;
        marker.enabled = !close;
        float bob = Mathf.Sin(Time.unscaledTime * bobSpeed) * bobAmplitude;
        marker.transform.position = target.PromptPosition + Vector3.up * (markerHeight + bob);

        ShowHint(close ? $"{goal}: presiona {ControlPlatform.InteractHint}"
            : dx < 0f ? $"<<  {goal}" : $"{goal}  >>");
    }

    // La pista va en una franja (su objeto padre) que se oculta cuando no hay nada que decir
    private void ShowHint(string text)
    {
        hintText.text = text;
        GameObject bar = hintText.transform.parent.gameObject;
        if (bar.activeSelf != (text.Length > 0))
        {
            bar.SetActive(text.Length > 0);
        }
    }

    // Interactuable al que hay que ir ahora: el del objetivo en este piso, o la grada
    // o ascensor que acerca al piso del objetivo
    private Interactable FindTarget(out string goal)
    {
        goal = "";
        FloorLabel here = FloorLabel.At(floors, player.position.x);
        Interactable best = null;
        int bestHops = int.MaxValue;
        float bestDistance = float.MaxValue;
        foreach (string id in tracker.CurrentIds)
        {
            foreach (Interactable candidate in TargetsFor(id))
            {
                FloorLabel there = FloorLabel.At(floors, candidate.transform.position.x);
                int stepHops = 0;
                Interactable step = there == here ? candidate : NextPortal(here, there, out stepHops);
                if (step == null)
                {
                    continue;
                }
                float distance = Mathf.Abs(step.transform.position.x - player.position.x);
                if (stepHops < bestHops || (stepHops == bestHops && distance < bestDistance))
                {
                    best = step;
                    bestHops = stepHops;
                    bestDistance = distance;
                    goal = Short(tracker.DescriptionOf(id));
                }
            }
        }
        return best;
    }

    private IEnumerable<Interactable> TargetsFor(string id)
    {
        foreach (RecycleBin bin in bins)
        {
            if (bin.ObjectiveId != id)
            {
                continue;
            }
            // Reciclaje: primero la basura que queda, después el contenedor
            bool anyLeft = false;
            foreach (TrashPickup item in trash)
            {
                if (item != null && item.isActiveAndEnabled)
                {
                    anyLeft = true;
                    yield return item;
                }
            }
            if (!anyLeft)
            {
                yield return bin;
            }
        }
        foreach (ObjectiveInteractable item in objectives)
        {
            if (item != null && item.isActiveAndEnabled && item.ObjectiveId == id)
            {
                yield return item;
            }
        }
    }

    // Primer portal del camino más corto entre dos pisos (búsqueda en anchura)
    private FloorPortal NextPortal(FloorLabel from, FloorLabel to, out int hops)
    {
        hops = 0;
        if (from == null || to == null)
        {
            return null;
        }
        var first = new Dictionary<FloorLabel, FloorPortal> { [from] = null };
        var depth = new Dictionary<FloorLabel, int> { [from] = 0 };
        var queue = new Queue<FloorLabel>();
        queue.Enqueue(from);
        while (queue.Count > 0)
        {
            FloorLabel floor = queue.Dequeue();
            if (floor == to)
            {
                hops = depth[floor];
                return first[floor];
            }
            foreach (FloorPortal portal in portals)
            {
                if (portal.Destination == null || FloorLabel.At(floors, portal.transform.position.x) != floor)
                {
                    continue;
                }
                FloorLabel next = FloorLabel.At(floors, portal.Destination.position.x);
                if (next != null && !first.ContainsKey(next))
                {
                    first[next] = first[floor] ?? portal;
                    depth[next] = depth[floor] + 1;
                    queue.Enqueue(next);
                }
            }
        }
        return null;
    }

    // Las descripciones largas se acortan para que la pista quepa en pantalla
    private static string Short(string description)
    {
        return description.Length > 42 ? description.Substring(0, 40) + "..." : description;
    }
}
