using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Prueba automática del juego completo: entra en modo Play y lo juega con un teclado
// simulado (Input System), igual que un jugador. Recorre el menú, la elección de
// personaje y el selector de hitos, juega los 5 hitos de principio a fin (diálogos,
// minijuegos, gradas, molinetes, reciclaje) y prueba la pausa, la derrota y el
// selector. Guarda un informe y capturas de pantalla en la carpeta indicada.
// En modo batch (sin -quit ni -nographics):
//   Unity -batchmode -projectPath . -executeMethod GamePlaytest.Run -playtestOut <carpeta>
public static class GamePlaytest
{
    private const float Speed = 3f;                 // el juego corre más rápido durante la prueba
    private const double MaxMinutes = 40;

    private static readonly string[] Hitos = { "Hito1", "Hito2", "Hito3", "Hito4", "Hito5" };

    private static string outDir;
    private static readonly List<string> report = new List<string>();
    private static readonly List<string> errors = new List<string>();
    private static int failures;
    private static readonly Stack<IEnumerator> stack = new Stack<IEnumerator>();
    private static int lastFrame = -1;
    private static double started;
    private static Keyboard keyboard;
    private static readonly HashSet<Key> held = new HashSet<Key>();
    private static readonly Dictionary<string, float> maxStress = new Dictionary<string, float>();
    private static EnterPlayModeOptions previousOptions;
    private static bool previousOptionsEnabled;

    // ================================================================
    // Arranque y cierre
    // ================================================================

    public static void Run()
    {
        string[] args = Environment.GetCommandLineArgs();
        int i = Array.IndexOf(args, "-playtestOut");
        outDir = i >= 0 && i + 1 < args.Length ? args[i + 1] : Path.Combine(Path.GetTempPath(), "playtest");
        Directory.CreateDirectory(outDir);

        // Sin recarga de dominio para que esta clase siga viva en Play
        previousOptionsEnabled = EditorSettings.enterPlayModeOptionsEnabled;
        previousOptions = EditorSettings.enterPlayModeOptions;
        EditorSettings.enterPlayModeOptionsEnabled = true;
        EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;

        EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity", OpenSceneMode.Single);
        Application.logMessageReceived += OnLog;
        EditorApplication.playModeStateChanged += OnPlayMode;
        started = EditorApplication.timeSinceStartup;
        EditorApplication.EnterPlaymode();
    }

    private static void OnPlayMode(PlayModeStateChange change)
    {
        if (change == PlayModeStateChange.EnteredPlayMode)
        {
            stack.Clear();
            stack.Push(Main());
            EditorApplication.update += Step;
        }
    }

    private static void Finish()
    {
        EditorApplication.update -= Step;
        Application.logMessageReceived -= OnLog;
        report.Add("");
        report.Add("ESTRÉS MÁXIMO POR ESCENA:");
        foreach (var pair in maxStress)
        {
            report.Add($"  {pair.Key}: {Mathf.RoundToInt(pair.Value * 100f)}%");
        }
        report.Add("");
        report.Add($"ERRORES Y EXCEPCIONES EN CONSOLA: {errors.Count}");
        report.AddRange(errors.Distinct().Take(60).Select(e => "  " + e));
        report.Add("");
        report.Add(failures == 0 && errors.Count == 0 ? "RESULTADO: TODO OK" : $"RESULTADO: {failures} FALLAS, {errors.Count} ERRORES");
        File.WriteAllLines(Path.Combine(outDir, "informe.txt"), report);

        EditorSettings.enterPlayModeOptions = previousOptions;
        EditorSettings.enterPlayModeOptionsEnabled = previousOptionsEnabled;
        EditorApplication.playModeStateChanged -= OnPlayMode;
        EditorApplication.ExitPlaymode();
        EditorApplication.delayCall += () => EditorApplication.Exit(failures == 0 && errors.Count == 0 ? 0 : 1);
    }

    private static void OnLog(string message, string stack, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
        {
            string where = SceneManager.GetActiveScene().name;
            errors.Add($"[{where}] {type}: {message.Split('\n')[0]} | {stack.Split('\n').FirstOrDefault()}");
        }
    }

    // Avanza la corrutina principal una vez por cuadro de juego
    private static void Step()
    {
        if (EditorApplication.timeSinceStartup - started > MaxMinutes * 60)
        {
            Check(false, "Tiempo máximo de la prueba agotado");
            Finish();
            return;
        }
        if (!EditorApplication.isPlaying || Time.frameCount == lastFrame)
        {
            return;
        }
        lastFrame = Time.frameCount;
        TrackStress();
        try
        {
            while (stack.Count > 0)
            {
                IEnumerator top = stack.Peek();
                if (!top.MoveNext())
                {
                    stack.Pop();
                    continue;
                }
                if (top.Current is IEnumerator nested)
                {
                    stack.Push(nested);
                    continue;
                }
                return;                                   // null: esperar al siguiente cuadro
            }
            Finish();
        }
        catch (Exception e)
        {
            Check(false, "Excepción en la prueba: " + e);
            Finish();
        }
    }

    private static void TrackStress()
    {
        if (StressSystem.Instance == null)
        {
            return;
        }
        string scene = SceneManager.GetActiveScene().name;
        maxStress.TryGetValue(scene, out float max);
        maxStress[scene] = Mathf.Max(max, StressSystem.Instance.Normalized);
    }

    // ================================================================
    // Recorrido principal
    // ================================================================

    private static IEnumerator Main()
    {
        keyboard = InputSystem.AddDevice<Keyboard>("TecladoPrueba");
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        report.Add("PRUEBA AUTOMÁTICA DE UNIFRANZ-DISORIENTED  " + DateTime.Now);
        report.Add("");

        // --- Menú: Jugar empieza en el Hito 1 ---
        Section("MENÚ PRINCIPAL");
        yield return Wait(1f);
        MainMenuController menu = UnityEngine.Object.FindAnyObjectByType<MainMenuController>();
        Check(menu != null, "El menú principal carga");
        Check(Get(menu, "levelSelectButton") is Button b && b.gameObject.activeInHierarchy, "El menú tiene el botón Seleccionar nivel");
        yield return Capture("00_menu");
        menu.Play();
        yield return Wait(0.5f);
        CharacterSelectPanel characters = UnityEngine.Object.FindAnyObjectByType<CharacterSelectPanel>();
        Check(characters != null && characters.gameObject.activeInHierarchy, "Jugar abre Elige tu personaje");
        yield return Capture("01_personaje");
        Array cards = (Array)Get(characters, "cards");
        ((Button)Get(cards.GetValue(0), "button")).onClick.Invoke();

        // --- Los 5 hitos seguidos, pasando con CONTINUAR ---
        for (int h = 0; h < Hitos.Length; h++)
        {
            yield return WaitScene(Hitos[h]);
            if (h == 0)
            {
                Check(GameSettings.SelectedCharacter == 0, "Jugar empieza en el Hito 1 con el personaje elegido");
            }
            yield return PlayLevel(Hitos[h], h == Hitos.Length - 1);
        }

        // --- Seleccionar nivel: hito -> personaje -> carga ese hito ---
        Section("SELECCIONAR NIVEL");
        for (int h = 0; h < Hitos.Length; h++)
        {
            SceneManager.LoadScene("MainMenu");
            yield return WaitScene("MainMenu");
            menu = UnityEngine.Object.FindAnyObjectByType<MainMenuController>();
            menu.OpenLevelSelect();
            yield return Wait(0.3f);
            LevelSelectPanel levels = UnityEngine.Object.FindAnyObjectByType<LevelSelectPanel>();
            Check(levels != null && levels.gameObject.activeInHierarchy, "Seleccionar nivel abre Elige el hito");
            Array levelList = (Array)Get(levels, "levels");
            if (h == 0)
            {
                Check(levelList.Length == 5, "El selector tiene 5 hitos");
                yield return Capture("02_selector_hitos");
                // Escape cierra el selector y vuelve al menú
                yield return Press(Key.Escape);
                yield return Wait(0.3f);
                Check(!levels.gameObject.activeInHierarchy, "Escape en el selector vuelve al menú");
                menu.OpenLevelSelect();
                yield return Wait(0.3f);
            }
            ((Button)Get(levelList.GetValue(h), "button")).onClick.Invoke();
            yield return Wait(0.3f);
            characters = UnityEngine.Object.FindAnyObjectByType<CharacterSelectPanel>();
            Check(characters != null && !levels.gameObject.activeInHierarchy, $"Elegir el hito {h + 1} abre Elige tu personaje");
            if (h == 0)
            {
                // Atrás en el personaje vuelve al selector de hitos
                characters.Close();
                yield return Wait(0.3f);
                Check(levels.gameObject.activeInHierarchy, "Atrás en Elige tu personaje vuelve a Elige el hito");
                ((Button)Get(levelList.GetValue(h), "button")).onClick.Invoke();
                yield return Wait(0.3f);
            }
            cards = (Array)Get(characters, "cards");
            ((Button)Get(cards.GetValue(1), "button")).onClick.Invoke();
            yield return WaitScene(Hitos[h]);
            Check(SceneManager.GetActiveScene().name == Hitos[h] && GameSettings.SelectedCharacter == 1,
                $"Seleccionar nivel {h + 1} carga {Hitos[h]} con el personaje 2");
            if (h == 2)
            {
                yield return Capture("90_personaje2_" + Hitos[h]);
            }
        }

        // --- Derrota por estrés máximo y reintentar (en el Hito 5 cargado) ---
        Section("DERROTA Y REINTENTO");
        yield return Wait(1f);
        StressSystem.Instance.Add(1000f);
        yield return Wait(0.5f);
        LevelFlow flow = LevelFlow.Instance;
        Check(((GameObject)Get(flow, "losePanel")).activeSelf, "Con el estrés al máximo aparece la pantalla de crisis");
        yield return Capture("91_derrota");
        ((Button)Get(flow, "retryButton")).onClick.Invoke();
        yield return WaitScene(Hitos[4]);
        yield return Wait(0.5f);
        Check(StressSystem.Instance.Stress < 5f, "Reintentar vuelve a cargar el nivel con el estrés en cero");
    }

    // ================================================================
    // Un hito completo
    // ================================================================

    private static IEnumerator PlayLevel(string scene, bool last)
    {
        Section(scene.ToUpper());
        Time.timeScale = Speed;
        yield return Wait(1f);
        ObjectiveTracker tracker = ObjectiveTracker.Instance;
        Check(tracker != null, $"{scene}: hay Checklist");
        if (tracker == null)
        {
            yield break;
        }
        string[] ids = ((Array)Get(tracker, "objectives")).Cast<object>().Select(o => (string)Get(o, "id")).ToArray();
        report.Add($"  Objetivos: {string.Join(", ", ids)}");
        yield return Capture(scene + "_00_inicio");

        // Caminar con el teclado
        Transform player = UnityEngine.Object.FindAnyObjectByType<PlayerMovement>().transform;
        float x0 = player.position.x;
        held.Add(Key.RightArrow);
        SendKeys();
        yield return Wait(0.6f);
        held.Remove(Key.RightArrow);
        SendKeys();
        Check(player.position.x > x0 + 0.2f, $"{scene}: el jugador camina con las flechas");

        // Pausa con Escape
        yield return Press(Key.Escape);
        yield return null;
        Check(PauseMenu.IsPaused, $"{scene}: Escape pausa el juego");
        yield return Capture(scene + "_01_pausa");
        yield return Press(Key.Escape);
        yield return null;
        Check(!PauseMenu.IsPaused, $"{scene}: Escape quita la pausa");
        Time.timeScale = Speed;

        // Gradas y ascensores: cada uno lleva a su destino
        foreach (FloorPortal portal in UnityEngine.Object.FindObjectsByType<FloorPortal>(FindObjectsSortMode.None))
        {
            Transform destination = (Transform)Get(portal, "destination");
            yield return GoTo(portal.transform.position.x);
            yield return Press(Key.E);
            yield return Wait(1.5f);
            Check(Mathf.Abs(player.position.x - destination.position.x) < 0.5f, $"{scene}: '{portal.Prompt}' lleva a su destino");
        }

        // Un NPC con objetivo pendiente responde sin abrir minijuegos
        ObjectiveInteractable pending = Objectives().FirstOrDefault(o => tracker.GetState((string)Get(o, "objectiveId")) == ObjectiveState.Pending);
        if (pending != null)
        {
            yield return Talk(pending);
            Check(!MinigameBase.AnyOpen && !BreathingMinigame.IsOpen && tracker.CompletedCount == 0,
                $"{scene}: '{pending.name}' antes de tiempo solo da su aviso");
        }

        // Conversaciones opcionales (extras, puntos de descanso)
        foreach (Interactable extra in Interactable.Active.ToArray().Where(IsExtra))
        {
            yield return Talk(extra);
        }

        // Checklist
        int shots = 0;
        var attempts = new Dictionary<string, int>();
        while (!tracker.AllDone)
        {
            string id = ids.First(o => tracker.GetState(o) == ObjectiveState.Current);
            attempts.TryGetValue(id, out int tries);
            attempts[id] = tries + 1;
            if (tries >= 3)
            {
                Check(false, $"{scene}: no se pudo completar '{id}' en 3 intentos (se completa a la fuerza para seguir)");
                tracker.Complete(id);
                continue;
            }
            yield return DoObjective(id, () => Capture($"{scene}_{++shots:00}_{id}"));
            if (tracker.GetState(id) == ObjectiveState.Done)
            {
                report.Add($"  OK  {id} ({tracker.CompletedCount}/{tracker.TotalCount})  estrés {Mathf.RoundToInt(StressSystem.Instance.Normalized * 100f)}%");
            }
            if (((GameObject)Get(LevelFlow.Instance, "losePanel")).activeSelf)
            {
                Check(false, $"{scene}: crisis de ansiedad durante la partida en '{id}' (se reintenta)");
                yield break;
            }
        }

        // Victoria
        yield return Wait(1.5f);
        LevelFlow flow = LevelFlow.Instance;
        Check(flow.IsOver && ((GameObject)Get(flow, "winPanel")).activeSelf, $"{scene}: al completar el Checklist aparece la victoria");
        yield return Capture(scene + "_99_victoria");
        Button next = (Button)Get(flow, "winNextButton");
        string nextScene = (string)Get(flow, "nextScene");
        if (last)
        {
            Check(string.IsNullOrEmpty(nextScene), $"{scene}: es el último hito (sin CONTINUAR)");
            TMP_Text message = FindDeep(((GameObject)Get(flow, "winPanel")).transform, "Mensaje")?.GetComponent<TMP_Text>();
            Check(message != null && message.text.Contains("Promedio"), $"{scene}: la pantalla final muestra el resumen del semestre");
            if (message != null)
            {
                report.Add("  Resumen: " + message.text.Replace("\n", " | "));
            }
        }
        else
        {
            Check(next != null && next.gameObject.activeInHierarchy && !string.IsNullOrEmpty(nextScene), $"{scene}: hay botón CONTINUAR a {nextScene}");
            next.onClick.Invoke();
        }
        Time.timeScale = 1f;
    }

    private static bool IsExtra(Interactable item)
    {
        Type type = item.GetType();
        return type == typeof(Interactable) || type == typeof(StressAwareInteractable) || type == typeof(RestPoint);
    }

    private static IEnumerable<ObjectiveInteractable> Objectives()
    {
        return UnityEngine.Object.FindObjectsByType<ObjectiveInteractable>(FindObjectsSortMode.None);
    }

    private static IEnumerator DoObjective(string id, Func<IEnumerator> capture)
    {
        // Reciclaje: juntar toda la basura y llevarla al contenedor
        RecycleBin bin = UnityEngine.Object.FindObjectsByType<RecycleBin>(FindObjectsSortMode.None)
            .FirstOrDefault(b => (string)Get(b, "objectiveId") == id);
        if (bin != null)
        {
            foreach (TrashPickup trash in UnityEngine.Object.FindObjectsByType<TrashPickup>(FindObjectsSortMode.None))
            {
                yield return Talk(trash);
            }
            yield return Talk(bin);
            yield break;
        }

        ObjectiveInteractable target = Objectives().FirstOrDefault(o => (string)Get(o, "objectiveId") == id);
        if (target == null)
        {
            Check(false, $"No hay interactuable para el objetivo '{id}'");
            yield break;
        }
        yield return Talk(target);

        // Minijuego que abre el diálogo
        MinigameBase game = UnityEngine.Object.FindObjectsByType<MinigameBase>(FindObjectsSortMode.None).FirstOrDefault(g => g.IsOpen);
        BreathingMinigame breathing = BreathingMinigame.IsOpen ? UnityEngine.Object.FindAnyObjectByType<BreathingMinigame>() : null;
        if (game != null || breathing != null)
        {
            yield return Wait(1f);
            yield return capture();
        }
        if (game != null)
        {
            report.Add($"  Minijuego {game.GetType().Name} ({id})");
            yield return Solve(game);
            while (game.IsOpen)
            {
                yield return null;
            }
        }
        else if (breathing != null)
        {
            report.Add($"  Minijuego BreathingMinigame ({id})");
            yield return SolveBreathing(breathing);
        }
        yield return Wait(0.3f);
    }

    // ================================================================
    // Acciones del jugador
    // ================================================================

    // Lleva al jugador a la x indicada (al piso que corresponda, como las gradas)
    private static IEnumerator GoTo(float x)
    {
        PlayerMovement player = UnityEngine.Object.FindAnyObjectByType<PlayerMovement>();
        Vector3 from = player.transform.position;
        Vector3 to = new Vector3(x, from.y, from.z);
        player.GetComponent<Rigidbody2D>().position = to;
        player.transform.position = to;
        CinemachineConfiner2D confiner = UnityEngine.Object.FindAnyObjectByType<CinemachineConfiner2D>();
        PolygonCollider2D bounds = UnityEngine.Object.FindObjectsByType<PolygonCollider2D>(FindObjectsSortMode.None)
            .Where(c => c.name.StartsWith("LimitesCamara"))
            .FirstOrDefault(c => c.bounds.min.x - 0.1f <= x && x <= c.bounds.max.x + 0.1f);
        if (confiner != null && bounds != null && confiner.BoundingShape2D != bounds)
        {
            confiner.BoundingShape2D = bounds;
            confiner.InvalidateBoundingShapeCache();
            CinemachineCamera vcam = confiner.GetComponent<CinemachineCamera>();
            vcam.OnTargetObjectWarped(player.transform, to - from);
            vcam.PreviousStateIsValid = false;
        }
        for (int i = 0; i < 4; i++)
        {
            yield return null;
        }
    }

    // Se acerca, presiona Interactuar y pasa todo el diálogo
    private static IEnumerator Talk(Interactable target)
    {
        if (target == null || !target.isActiveAndEnabled)
        {
            yield break;
        }
        yield return GoTo(target.transform.position.x);
        yield return Press(Key.E);
        for (int guard = 0; DialogueBox.IsOpen && guard < 400; guard++)
        {
            yield return Press(Key.E);
        }
        Check(!DialogueBox.IsOpen, $"El diálogo de '{target.name}' se cierra");
    }

    private static IEnumerator Press(Key key)
    {
        held.Add(key);
        SendKeys();
        yield return null;
        yield return null;
        held.Remove(key);
        SendKeys();
        yield return null;
        yield return null;
    }

    private static void SendKeys()
    {
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(held.ToArray()));
    }

    private static void Hold(Key key, bool down)
    {
        if (down ? held.Add(key) : held.Remove(key))
        {
            SendKeys();
        }
    }

    private static IEnumerator Wait(float seconds)
    {
        for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
        {
            yield return null;
        }
    }

    private static IEnumerator WaitScene(string scene)
    {
        for (int guard = 0; guard < 2000 && SceneManager.GetActiveScene().name != scene; guard++)
        {
            yield return null;
        }
        Check(SceneManager.GetActiveScene().name == scene, $"Carga la escena {scene}");
        yield return Wait(0.5f);
    }

    // ================================================================
    // Minijuegos: se juegan leyendo su estado, como lo vería el jugador
    // ================================================================

    private static IEnumerator Solve(MinigameBase game)
    {
        switch (game)
        {
            case ScheduleMinigame g: return SolveSchedule(g);
            case NotesMinigame g: return SolveChoice(g, "optionBoxes", () => CorrectNotes(g));
            case ExamMinigame g: return SolveChoice(g, "optionBoxes", () => (int)Get(g, "shownCorrect"));
            case BookSearchMinigame g: return SolveChoice(g, "spines", () => CorrectBook(g));
            case LabSequenceMinigame g: return SolveLab(g);
            case PriorityMinigame g: return SolvePriority(g);
            case MemoryMinigame g: return SolveMemory(g);
            case PresentationMinigame g: return SolvePresentation(g);
            default:
                Check(false, "Minijuego sin solucionador: " + game.GetType().Name);
                return Wait(1f);
        }
    }

    private static IEnumerator SolveBreathing(BreathingMinigame game)
    {
        TMP_Text instruction = (TMP_Text)Get(game, "instruction");
        while (BreathingMinigame.IsOpen)
        {
            Hold(Key.E, instruction.text.StartsWith("INHALA"));
            yield return null;
        }
        Hold(Key.E, false);
    }

    // Mueve un cursor circular hasta target y confirma
    private static IEnumerator MoveAndConfirm(int cursor, int target, int count, bool wrap)
    {
        int steps = wrap ? (target - cursor + count) % count : target - cursor;
        for (int i = 0; i < Mathf.Abs(steps); i++)
        {
            yield return Press(steps > 0 ? Key.RightArrow : Key.LeftArrow);
        }
        yield return Press(Key.E);
    }

    private static IEnumerator SolveSchedule(ScheduleMinigame game)
    {
        var subjects = (ScheduleMinigame.Subject[])Get(game, "subjects");
        TMP_Text progress = (TMP_Text)Get(game, "progressText");
        yield return null;
        while (game.IsOpen)
        {
            string p = progress.text;
            if (!p.StartsWith("Materia "))
            {
                yield return null;
                continue;
            }
            int s = int.Parse(p.Substring(8).Split('/')[0]) - 1;
            var placed = (string[])Get(game, "placed");
            int day = subjects[s].days.First(d => placed[d] == null);
            yield return MoveAndConfirm((int)Get(game, "cursor"), day, placed.Length, true);
            for (int i = 0; i < 6 && progress.text == p; i++)
            {
                yield return null;
            }
        }
    }

    // Opción múltiple (Apuntes, Examen, Biblioteca): el cursor se ve por su color
    private static IEnumerator SolveChoice(MinigameBase game, string boxesField, Func<int> correct)
    {
        Image[] boxes = (Image[])Get(game, boxesField);
        Color cursorColor = (Color)Get(game, "cursorColor");
        while (game.IsOpen)
        {
            int cursor = Array.FindIndex(boxes, b => Same(b.color, cursorColor));
            int answer = correct();
            if (cursor < 0 || answer < 0)
            {
                yield return null;
                continue;
            }
            if (cursor != answer)
            {
                yield return Press(cursor < answer ? Key.RightArrow : Key.LeftArrow);
                continue;
            }
            yield return Press(Key.E);
            for (int i = 0; i < 10; i++)
            {
                yield return null;
            }
        }
    }

    private static int CorrectNotes(NotesMinigame game)
    {
        string prompt = ((TMP_Text)Get(game, "promptText")).text;
        var questions = (NotesMinigame.Question[])Get(game, "questions");
        return Array.Find(questions, q => q.prompt == prompt)?.correct ?? -1;
    }

    private static int CorrectBook(BookSearchMinigame game)
    {
        string prompt = ((TMP_Text)Get(game, "promptText")).text;
        var requests = (BookSearchMinigame.Request[])Get(game, "requests");
        return Array.Find(requests, r => prompt.Contains(r.title))?.correct ?? -1;
    }

    // Laboratorio: mira la secuencia que muestra la docente y la repite
    private static IEnumerator SolveLab(LabSequenceMinigame game)
    {
        Image[] tubes = (Image[])Get(game, "tubes");
        Color[] colors = (Color[])Get(game, "tubeColors");
        TMP_Text prompt = (TMP_Text)Get(game, "promptText");
        var sequence = new List<int>();
        int lastLit = -1;
        bool watching = false;
        while (game.IsOpen)
        {
            if (prompt.text.StartsWith("Observa"))
            {
                if (!watching)
                {
                    sequence.Clear();
                    watching = true;
                }
                int lit = Array.FindIndex(tubes, t => Same(t.color, colors[Array.IndexOf(tubes, t)]));
                if (lit >= 0 && lastLit < 0)
                {
                    sequence.Add(lit);
                }
                lastLit = lit;
                yield return null;
                continue;
            }
            if (watching && prompt.text.StartsWith("Ahora"))
            {
                watching = false;
                int cursor = 0;
                foreach (int tube in sequence)
                {
                    int steps = (tube - cursor + tubes.Length) % tubes.Length;
                    for (int i = 0; i < steps; i++)
                    {
                        yield return Press(Key.RightArrow);
                    }
                    cursor = tube;
                    yield return Press(Key.E);
                }
            }
            yield return null;
        }
    }

    private static IEnumerator SolvePriority(PriorityMinigame game)
    {
        var tasks = (PriorityMinigame.Task[])Get(game, "tasks");
        yield return null;
        while (game.IsOpen)
        {
            var order = (int[])Get(game, "order");
            if (order == null || order.All(o => o != 0))
            {
                yield return null;
                continue;
            }
            int target = Enumerable.Range(0, tasks.Length).Where(i => order[i] == 0).OrderBy(i => tasks[i].daysLeft).First();
            int before = order.Count(o => o != 0);
            yield return MoveAndConfirm((int)Get(game, "cursor"), target, tasks.Length, true);
            for (int i = 0; i < 6 && ((int[])Get(game, "order")).Count(o => o != 0) == before; i++)
            {
                yield return null;
            }
        }
    }

    private static IEnumerator SolveMemory(MemoryMinigame game)
    {
        yield return null;
        while (game.IsOpen)
        {
            var matched = (bool[])Get(game, "matched");
            var shown = (bool[])Get(game, "shown");
            var pairOf = (int[])Get(game, "pairOf");
            if (matched == null || matched.All(m => m))
            {
                yield return null;
                continue;
            }
            // Carta dada vuelta que espera su par (o -1 si no hay ninguna)
            int open = Enumerable.Range(0, shown.Length).Where(i => shown[i] && !matched[i]).DefaultIfEmpty(-1).First();
            int target = open < 0
                ? Array.FindIndex(matched, m => !m)
                : Enumerable.Range(0, pairOf.Length).First(i => i != open && !matched[i] && pairOf[i] == pairOf[open]);
            yield return MoveAndConfirm((int)Get(game, "cursor"), target, matched.Length, true);
            yield return Wait(0.2f);
        }
    }

    private static IEnumerator SolvePresentation(PresentationMinigame game)
    {
        GameObject questionGroup = (GameObject)Get(game, "questionGroup");
        var questions = (PresentationMinigame.Question[])Get(game, "questions");
        Image[] boxes = (Image[])Get(game, "optionBoxes");
        Color cursorColor = (Color)Get(game, "cursorColor");
        TMP_Text prompt = (TMP_Text)Get(game, "promptText");
        while (game.IsOpen)
        {
            if (questionGroup.activeSelf)
            {
                Hold(Key.LeftArrow, false);
                Hold(Key.RightArrow, false);
                var question = Array.Find(questions, q => prompt.text.EndsWith(q.prompt));
                int cursor = Array.FindIndex(boxes, b => Same(b.color, cursorColor));
                if (question != null && cursor >= 0)
                {
                    if (cursor != question.correct)
                    {
                        yield return Press(cursor < question.correct ? Key.RightArrow : Key.LeftArrow);
                    }
                    else
                    {
                        yield return Press(Key.E);
                        yield return Wait(0.3f);
                    }
                    continue;
                }
                yield return null;
                continue;
            }
            // Mantiene la aguja en el centro
            float position = (float)Get(game, "position");
            Hold(Key.LeftArrow, position > 0.08f);
            Hold(Key.RightArrow, position < -0.08f);
            yield return null;
        }
        Hold(Key.LeftArrow, false);
        Hold(Key.RightArrow, false);
    }

    // ================================================================
    // Capturas e informe
    // ================================================================

    // Renderiza la cámara principal con el HUD (los Canvas pasan a modo cámara un momento)
    private static IEnumerator Capture(string name)
    {
        Camera camera = Camera.main;
        if (camera == null)
        {
            yield break;
        }
        var canvases = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None)
            .Where(c => c.isRootCanvas && c.renderMode == RenderMode.ScreenSpaceOverlay).ToArray();
        // Misma proporción que la pantalla del juego, así la cámara encuadra igual
        int height = Mathf.RoundToInt(1920f * Screen.height / Mathf.Max(1, Screen.width));
        var rt = new RenderTexture(1920, height, 24);
        camera.targetTexture = rt;
        // En modo cámara el HUD debe quedar delante de los sprites, como en pantalla
        int[] orders = canvases.Select(c => c.sortingOrder).ToArray();
        foreach (Canvas canvas in canvases)
        {
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = camera.nearClipPlane + 0.5f;
            canvas.sortingOrder += 1000;
        }
        yield return null;
        yield return null;
        Canvas.ForceUpdateCanvases();
        camera.Render();
        RenderTexture.active = rt;
        var texture = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
        texture.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        texture.Apply();
        RenderTexture.active = null;
        File.WriteAllBytes(Path.Combine(outDir, name + ".png"), ImageConversion.EncodeToPNG(texture));
        UnityEngine.Object.Destroy(texture);
        for (int i = 0; i < canvases.Length; i++)
        {
            canvases[i].sortingOrder = orders[i];
            canvases[i].renderMode = RenderMode.ScreenSpaceOverlay;
        }
        camera.targetTexture = null;
        rt.Release();
    }

    private static void Section(string title)
    {
        report.Add("");
        report.Add("== " + title + " ==");
    }

    private static void Check(bool ok, string what)
    {
        if (!ok)
        {
            failures++;
        }
        report.Add((ok ? "  OK    " : "  FALLA ") + what);
    }

    private static bool Same(Color a, Color b)
    {
        return Mathf.Abs(a.r - b.r) < 0.01f && Mathf.Abs(a.g - b.g) < 0.01f && Mathf.Abs(a.b - b.b) < 0.01f;
    }

    private static Transform FindDeep(Transform parent, string name)
    {
        return parent.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == name);
    }

    // Campo privado de un componente o de una clase de datos (busca en las clases base)
    private static object Get(object target, string field)
    {
        for (Type type = target.GetType(); type != null; type = type.BaseType)
        {
            FieldInfo info = type.GetField(field, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (info != null)
            {
                return info.GetValue(target);
            }
        }
        throw new MissingFieldException(target.GetType().Name, field);
    }
}
