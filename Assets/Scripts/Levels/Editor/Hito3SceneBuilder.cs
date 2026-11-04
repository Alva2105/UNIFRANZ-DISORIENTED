using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using H = Hito1SceneBuilder;
using H2 = Hito2SceneBuilder;

// Arma la escena del Hito 3 (Mitad del semestre) según el plan de niveles: el Piso 1
// (laboratorios de Fisiología e Histología y Sala de Simulación) y el Piso 5 (Biblioteca
// y Aula Dinámica) de la sede La Paz, unidos por las gradas. El ascensor siempre está
// lleno. Checklist de 7 objetivos con un bloque central que se hace en cualquier orden,
// reloj de cierre de la biblioteca y cuatro minijuegos. Reutiliza las utilidades de los
// generadores del Hito 1 y del Hito 2.
// Menú: Tools > UNIFRANZ-DISORIENTED. En modo batch:
//   Unity -batchmode -quit -projectPath . -executeMethod Hito3SceneBuilder.Build
public static class Hito3SceneBuilder
{
    private const string SourceScenePath = "Assets/Scenes/Piso2.unity";
    private const string ScenePath = "Assets/Scenes/Hito3.unity";
    private const string MenuScenePath = "Assets/Scenes/MainMenu.unity";
    private const string Hito1ScenePath = "Assets/Scenes/Hito1.unity";
    private const string Hito2ScenePath = "Assets/Scenes/Hito2.unity";

    private static readonly string[] Piso1Blocks = { "pasillo", "fisiologia", "histologia", "simulacion" };
    private static readonly string[] Piso5Blocks = { "biblioteca", "aula_dinamica" };
    private static readonly string[] NewSheets = { "DocenteLab", "Bibliotecaria", "Camila" };

    // El Piso 5 se arma lejos en X para que nada de un piso alcance al otro
    private const float Piso5OffsetX = 300f;

    private static readonly Color Orange = new Color(0.976f, 0.435f, 0.075f);
    private static readonly Color TextLight = new Color(0.92f, 0.92f, 0.95f);
    private static readonly Color BoxColor = new Color(0.16f, 0.18f, 0.3f);

    // Objetos del Piso 2 que no se usan en este hito
    private static readonly string[] RemovedObjects =
        { "Puerta_204", "Puerta_205", "Puerta_206", "Puerta_207", "Mural", "Escaleras" };

    [MenuItem("Tools/UNIFRANZ-DISORIENTED/Generar Hito 3 (Mitad del semestre)")]
    public static void Build()
    {
        PrepareSprites();

        // Copia de Piso2 conservando el .meta de Hito3 si ya existe (mismo GUID)
        if (File.Exists(ScenePath))
        {
            File.Copy(SourceScenePath, ScenePath, true);
            AssetDatabase.ImportAsset(ScenePath, ImportAssetOptions.ForceUpdate);
        }
        else if (!AssetDatabase.CopyAsset(SourceScenePath, ScenePath))
        {
            Debug.LogError("[Hito3SceneBuilder] No se pudo copiar " + SourceScenePath);
            return;
        }
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        foreach (string name in RemovedObjects)
        {
            GameObject old = H.FindRoot(scene, name);
            if (old != null)
            {
                Object.DestroyImmediate(old);
            }
        }

        GameObject systems = H.FindRoot(scene, "Sistemas");
        GameObject player = H.FindRoot(scene, "Jugador");
        Transform ui = H.FindRoot(scene, "HUD").transform;
        float floorY = player.transform.position.y - 0.05f;

        // --- Escenario: Piso 1 y Piso 5 lado a lado en X, unidos por las gradas ---
        Object.DestroyImmediate(H.FindRoot(scene, "Escenario"));
        H.Floor p1 = H.BuildFloor("Piso1", Piso1Blocks, 0f, floorY);
        H.Floor p5 = H.BuildFloor("Piso5", Piso5Blocks, Piso5OffsetX, floorY);

        PolygonCollider2D p1Bounds = H.FindRoot(scene, "LimitesCamara").GetComponent<PolygonCollider2D>();
        H.SetBounds(p1Bounds, p1);
        PolygonCollider2D p5Bounds = new GameObject("LimitesCamara_Piso5").AddComponent<PolygonCollider2D>();
        p5Bounds.isTrigger = true;
        H.SetBounds(p5Bounds, p5);
        GameObject vcamGo = H.FindRoot(scene, "CinemachineCamera");
        CinemachineCamera vcam = vcamGo.GetComponent<CinemachineCamera>();
        CinemachineConfiner2D confiner = vcamGo.GetComponent<CinemachineConfiner2D>();
        confiner.BoundingShape2D = p1Bounds;

        // El jugador sale del ascensor del Piso 1
        player.transform.position = new Vector3(p1.X(0, 0.66f), floorY + 0.05f, 0f);

        // --- Estrés: mitad del semestre, sube más que en el Hito 2 ---
        StressSystem stress = systems.GetComponent<StressSystem>();
        H.SetFloat(stress, "passiveRate", 0.5f);
        H.SetFloat(stress, "sprintRate", 1.6f);

        // --- Audio ---
        AudioManager audio = systems.AddComponent<AudioManager>();
        H.Set(audio, "music", AssetDatabase.LoadAssetAtPath<AudioClip>(H.MusicPath));
        FootstepAudio footsteps = player.AddComponent<FootstepAudio>();
        H.SetArray(footsteps, "steps", Enumerable.Range(0, 5).Select(i => H.Sfx("footstep_concrete_00" + i)).ToArray());
        HeartbeatAudio heartbeat = systems.AddComponent<HeartbeatAudio>();
        H.Set(heartbeat, "lub", H.Sfx("impactSoft_heavy_000"));
        H.Set(heartbeat, "dub", H.Sfx("impactSoft_heavy_001"));

        // --- Checklist del Hito 3: los objetivos 2 a 5 se hacen en cualquier orden ---
        ObjectiveTracker tracker = systems.AddComponent<ObjectiveTracker>();
        H.SetString(tracker, "milestoneTitle", "HITO 3");
        H.SetObjectives(tracker, new[]
        {
            ("rol", "Arma tu rol de parciales en los casilleros"),
            ("laboratorio", "Fisiología"),
            ("biblioteca", "Biblioteca (Piso 5)"),
            ("simulacion", "Simulación"),
            ("pendientes", "Aula Dinámica (Piso 5)"),
            ("informe", "Entrega tu informe en Histología"),
            ("salida", "Termina el día: toma el ascensor")
        });
        SetAnyOrder(tracker, "laboratorio", "biblioteca", "simulacion", "pendientes");
        H.Set(tracker, "objectiveCompletedClip", H.Sfx("confirmation_002"));
        H.Set(tracker, "milestoneCompletedClip", H.Sfx("confirmation_004"));
        TMP_Text objectiveText = H2.SetupObjectiveHud(ui, tracker);
        // El bloque libre lista varios lugares a la vez: el texto se achica si no entra
        objectiveText.fontSizeMax = objectiveText.fontSize;
        objectiveText.fontSizeMin = 12f;
        objectiveText.enableAutoSizing = true;

        // --- Reloj: la biblioteca cierra y el informe tiene hora de entrega ---
        TextMeshProUGUI clock = H.CreateLabel(ui, "RelojClase", AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(H.FontPath),
            "", 20f, TextLight, 0f);
        var clockRect = clock.rectTransform;
        clockRect.anchorMin = clockRect.anchorMax = clockRect.pivot = new Vector2(0f, 1f);
        clockRect.anchoredPosition = ((RectTransform)objectiveText.transform).anchoredPosition + new Vector2(0f, -48f);
        clockRect.sizeDelta = new Vector2(900f, 34f);
        clock.alignment = TextAlignmentOptions.TopLeft;
        clockRect.SetSiblingIndex(objectiveText.transform.GetSiblingIndex() + 1);
        ObjectiveDeadline deadline = systems.AddComponent<ObjectiveDeadline>();
        H.Set(deadline, "tracker", tracker);
        H.Set(deadline, "clockText", clock);
        H.Set(deadline, "lateClip", H.Sfx("error_004"));
        H2.SetDeadlines(deadline, new[]
        {
            ("biblioteca", 90f, 12f, "La biblioteca cierra en"),
            ("informe", 35f, 12f, "La entrega del informe cierra en")
        });

        // --- Pantalla de victoria (último hito hecho por ahora: solo volver al menú) ---
        Transform win = H.FindDeep(ui, "Victoria");
        H.FindDeep(win, "Titulo").GetComponent<TMP_Text>().text = "HITO 3 COMPLETADO";
        H.FindDeep(win, "Mensaje").GetComponent<TMP_Text>().text = "Pasaste la mitad del semestre. Se vienen los exámenes parciales.";
        LevelFlow flow = systems.GetComponent<LevelFlow>();
        H.SetString(flow, "nextScene", "");

        // --- Minijuegos y fundido de las gradas ---
        ScheduleMinigame exams = CreateExamScheduleMinigame(ui, systems, player);
        LabSequenceMinigame lab = CreateLabMinigame(ui, systems, player);
        BookSearchMinigame books = CreateBookMinigame(ui, systems, player);
        PriorityMinigame priorities = CreatePriorityMinigame(ui, systems, player);
        BreathingMinigame breathing = H.CreateBreathingMinigame(ui, systems, player, "RESPIRA CON CAMILA");
        H.SetString(breathing, "objectiveId", "simulacion");
        H.SetFloat(breathing, "inhaleSeconds", 3f);
        H.SetFloat(breathing, "exhaleSeconds", 3f);
        H.SetFloat(breathing, "maxRelief", 20f);
        CanvasGroup fade = H.CreateFade(ui);

        // --- Contenido ---
        // Piso 1: pasillo (casilleros, ascensor, gradas) -> Fisiología -> Histología -> Simulación.
        // Piso 5: Biblioteca (gradas, estantes, mostrador) -> Aula Dinámica.
        Transform content = new GameObject("Hito3_Contenido").transform;
        Transform t = player.transform;
        PlayerMovement movement = player.GetComponent<PlayerMovement>();

        // Pasillo del Piso 1
        ObjectiveInteractable notice = H2.CreateObject(content, "AvisoParciales", p1.X(0, 0.16f), floorY, "Revisar",
            "Aviso en los casilleros", "rol", 0f,
            new[]
            {
                "Un aviso pegado en los casilleros: \"ROL DE EXÁMENES PARCIALES\".",
                "Cada materia toma su parcial solo ciertos días. Organiza tu semana de exámenes."
            },
            new string[0], 0f,
            new[] { "Tu rol de parciales ya está anotado en tu agenda." });
        H.SetBool(notice, "completesObjective", false);          // lo completa el minijuego
        UnityEditor.Events.UnityEventTools.AddPersistentListener(H.GetEvent(notice, typeof(Interactable), "onFinished"), exams.Begin);

        H.CreateCrowd(content, p1.X(0, 0.30f), p1.X(0, 0.44f), floorY, t);   // fila del ascensor

        H2.CreateObject(content, "Ascensor", p1.X(0, 0.54f), floorY, "Usar ascensor", "", "salida", -8f,
            new[]
            {
                "Por fin el ascensor está vacío.",
                "Bajas pensando en todo lo que lograste hoy. La mitad del semestre ya es tuya."
            },
            new[]
            {
                "El ascensor está lleno otra vez... y la fila no avanza.",
                "Mejor usa las gradas. (Todavía te quedan pendientes.)"
            }, 3f,
            new[] { "Ya terminaste por hoy." });

        // Gradas Piso 1 <-> Piso 5
        Transform p5Arrival = H.CreatePoint(content, "Llegada_Piso5", p5.X(0, 0.34f), floorY);
        Transform p1Arrival = H.CreatePoint(content, "Llegada_Piso1", p1.X(0, 0.78f), floorY);
        H.CreatePortal(content, "Gradas_Subir", p1.X(0, 0.88f), floorY, "Subir al Piso 5",
            p5Arrival, p5Bounds, movement, vcam, confiner, fade);
        H.CreatePortal(content, "Gradas_Bajar", p5.X(0, 0.14f), floorY, "Bajar al Piso 1",
            p1Arrival, p1Bounds, movement, vcam, confiner, fade);

        // Laboratorio de Fisiología
        ObjectiveInteractable teacher = H.CreateNpc(content, "DocenteLab", p1.X(1, 0.36f), floorY, t,
            "Dra. Paredes - Docente de laboratorio", "Hablar", "laboratorio", 0f,
            new[]
            {
                "Llegas justo para la práctica. Soy la Dra. Paredes.",
                "Hoy preparamos soluciones: el orden de los reactivos es lo más importante.",
                "Te muestro la secuencia una vez y tú la repites. Concéntrate."
            },
            new[] { "Primero organiza tu rol de parciales; el aviso está en los casilleros." }, 2f,
            new[] { "Buen trabajo. No olvides entregar tu informe en Histología." });
        H.SetBool(teacher, "completesObjective", false);
        UnityEditor.Events.UnityEventTools.AddPersistentListener(H.GetEvent(teacher, typeof(Interactable), "onFinished"), lab.Begin);

        // Laboratorio de Histología: entrega del informe
        H.CreateNpc(content, "Kevin", p1.X(2, 0.36f), floorY, t,
            "Kevin - Ayudante de Histología", "Hablar", "informe", -10f,
            new[]
            {
                "¡Hey! ¿Vienes a entregar tu informe? Justo a tiempo.",
                "Práctica, bibliografía y todo en orden... se nota que te organizaste.",
                "Recibido. Ahora sí, descansa: te lo ganaste."
            },
            new[]
            {
                "Para el informe necesitas la práctica, la bibliografía de la biblioteca...",
                "...y tener tus pendientes en orden. Termina eso y vuelve."
            }, 3f,
            new[] { "Tu informe ya está entregado. ¡Nos vemos en los parciales!" });

        // Sala de Simulación: Camila entra en pánico en su primer caso simulado
        ObjectiveInteractable camila = H.CreateNpc(content, "Camila", p1.X(3, 0.50f), floorY, t,
            "Camila - Compañera", "Hablar", "simulacion", 0f,
            new[]
            {
                "N-no puedo... el maniquí \"se descompensó\" y me quedé en blanco.",
                "Siento que el corazón se me sale. ¿Y si me pasa en un caso real?",
                "(Le propones respirar juntas como te enseñó Lucía.)"
            },
            new[] { "Ahora no puedo hablar... (Primero revisa tu rol de parciales.)" }, 2f,
            new[] { "Gracias. Ya puedo volver a intentarlo. Respirar sí funciona." },
            patrol: 0.8f);                                       // camina nerviosa
        H.SetBool(camila, "completesObjective", false);
        UnityEditor.Events.UnityEventTools.AddPersistentListener(H.GetEvent(camila, typeof(Interactable), "onFinished"), breathing.Begin);

        // Biblioteca (Piso 5)
        ObjectiveInteractable librarian = H.CreateNpc(content, "Bibliotecaria", p5.X(0, 0.62f), floorY, t,
            "Sra. Elena - Bibliotecaria", "Hablar", "biblioteca", 0f,
            new[]
            {
                "Bienvenid@ a la biblioteca. ¿Buscas bibliografía para tu informe?",
                "Te doy los códigos y tú los buscas en el estante. Fíjate bien: se parecen mucho.",
                "Y apúrate, que cerramos pronto."
            },
            new[] { "Hola. Primero organiza tu rol de parciales y luego vienes por tus libros." }, 2f,
            new[] { "Devuelve los libros a tiempo, ¿eh? ¡Suerte con tu informe!" });
        H.SetBool(librarian, "completesObjective", false);
        UnityEditor.Events.UnityEventTools.AddPersistentListener(H.GetEvent(librarian, typeof(Interactable), "onFinished"), books.Begin);

        H.CreateExtra(content, "Valeria", p5.X(0, 0.42f), floorY, t, false, "Valeria - Estudiante", -3f, new[]
        {
            "Shh... estoy estudiando para el parcial de Anatomía.",
            "Aquí arriba hay silencio y buena vista. Me ayuda a concentrarme."
        });

        // Aula Dinámica: pizarra para ordenar pendientes
        ObjectiveInteractable board = H2.CreateObject(content, "Pizarra", p5.X(1, 0.49f), floorY, "Planificar",
            "Pizarra del Aula Dinámica", "pendientes", 0f,
            new[]
            {
                "Una pizarra libre. Buen lugar para ordenar todo lo que tienes encima.",
                "Escribes tus pendientes... son más de los que pensabas."
            },
            new[] { "Una pizarra libre. (Primero revisa tu rol de parciales.)" }, 1f,
            new[] { "Tu plan de la semana sigue en la pizarra." });
        H.SetBool(board, "completesObjective", false);
        UnityEditor.Events.UnityEventTools.AddPersistentListener(H.GetEvent(board, typeof(Interactable), "onFinished"), priorities.Begin);

        H.CreateExtra(content, "Companero", p5.X(1, 0.24f), floorY, t, false, "Diego - Compañero", -4f, new[]
        {
            "¡Mitad de semestre! Siento que todo vence la misma semana.",
            "Yo anoto todo en la pizarra y hago primero lo que vence antes.",
            "Así por lo menos sé por dónde empezar."
        });
        H.CreateRestPoint(content, p5.X(1, 0.95f), floorY);

        EditorSceneManager.SaveScene(scene);
        UpdateBuildSettings();
        AssetDatabase.SaveAssets();
        Debug.Log("[Hito3SceneBuilder] Listo: " + ScenePath);
    }

    // ================================================================
    // Minijuegos
    // ================================================================

    private static ScheduleMinigame CreateExamScheduleMinigame(Transform ui, GameObject systems, GameObject player)
    {
        RectTransform panel = H2.CreatePanel(ui, "MinijuegoRolParciales", "ROL DE PARCIALES");
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(H.FontPath);
        TextMeshProUGUI card = H.CreateLabel(panel, "Materia", font, "", 26f, Orange, 250f);
        card.rectTransform.sizeDelta = new Vector2(1600f, 100f);
        Object[] cells = CreateBoxes(panel, "Dia_", 5, 300f, new Vector2(270f, 230f), 10f, 18f, out Object[] texts);
        TextMeshProUGUI feedback = H.CreateLabel(panel, "Mensaje", font, "", 20f, TextLight, -200f);
        TextMeshProUGUI progress = H.CreateLabel(panel, "Progreso", font, "", 18f, Orange, -270f);

        ScheduleMinigame game = systems.AddComponent<ScheduleMinigame>();
        H2.ConfigureMinigame(game, panel.gameObject, player, "rol");
        H.SetArray(game, "dayCells", cells);
        H.SetArray(game, "dayTexts", texts);
        H.Set(game, "cardText", card);
        H.Set(game, "feedbackText", feedback);
        H.Set(game, "progressText", progress);
        // Las más restringidas van primero: así ninguna elección deja el rol sin solución
        H2.SetSubjects(game, new[]
        {
            ("Parcial de Bioquímica", "Bioquímica", new[] { 3 }),
            ("Parcial de Fisiología", "Fisiología", new[] { 1, 3 }),
            ("Parcial de Anatomía", "Anatomía", new[] { 0, 1 }),
            ("Parcial de Histología", "Histología", new[] { 2, 4 }),
            ("Parcial de Inglés Técnico", "Inglés", new[] { 2, 4 })
        });
        return game;
    }

    private static LabSequenceMinigame CreateLabMinigame(Transform ui, GameObject systems, GameObject player)
    {
        RectTransform panel = H2.CreatePanel(ui, "MinijuegoLaboratorio", "PRÁCTICA DE LABORATORIO");
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(H.FontPath);
        TextMeshProUGUI prompt = H.CreateLabel(panel, "Indicacion", font, "", 24f, TextLight, 270f);
        Object[] tubes = CreateBoxes(panel, "Tubo_", 4, 300f, new Vector2(200f, 280f), 20f, 18f, out Object[] texts);
        TextMeshProUGUI feedback = H.CreateLabel(panel, "Mensaje", font, "", 20f, TextLight, -200f);
        TextMeshProUGUI progress = H.CreateLabel(panel, "Progreso", font, "", 18f, Orange, -270f);

        LabSequenceMinigame game = systems.AddComponent<LabSequenceMinigame>();
        H2.ConfigureMinigame(game, panel.gameObject, player, "laboratorio");
        H.SetArray(game, "tubes", tubes);
        H.SetArray(game, "tubeTexts", texts);
        H.Set(game, "promptText", prompt);
        H.Set(game, "feedbackText", feedback);
        H.Set(game, "progressText", progress);
        return game;
    }

    private static BookSearchMinigame CreateBookMinigame(Transform ui, GameObject systems, GameObject player)
    {
        RectTransform panel = H2.CreatePanel(ui, "MinijuegoBiblioteca", "ENCUENTRA EL LIBRO");
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(H.FontPath);
        TextMeshProUGUI prompt = H.CreateLabel(panel, "Pedido", font, "", 24f, TextLight, 260f);
        prompt.rectTransform.sizeDelta = new Vector2(1600f, 120f);
        prompt.lineSpacing = 20f;
        Object[] spines = CreateBoxes(panel, "Lomo_", 6, 230f, new Vector2(190f, 250f), 20f, 18f, out Object[] texts);
        Image fill = CreateTimerBar(panel, -140f);
        TextMeshProUGUI feedback = H.CreateLabel(panel, "Mensaje", font, "", 20f, TextLight, -210f);
        TextMeshProUGUI progress = H.CreateLabel(panel, "Progreso", font, "", 18f, Orange, -275f);

        BookSearchMinigame game = systems.AddComponent<BookSearchMinigame>();
        H2.ConfigureMinigame(game, panel.gameObject, player, "biblioteca");
        H.SetArray(game, "spines", spines);
        H.SetArray(game, "spineTexts", texts);
        H.Set(game, "promptText", prompt);
        H.Set(game, "timerFill", fill);
        H.Set(game, "feedbackText", feedback);
        H.Set(game, "progressText", progress);
        SetRequests(game, new[]
        {
            ("Fisiología Médica", new[] { "612 GUY", "612 GAY", "621 GUY", "612 GUI", "616 GUY", "612 GYU" }, 0),
            ("Histología: Texto y Atlas", new[] { "611 ROS", "611 RSO", "611 ROS 2", "616 ROS", "611 RAS", "661 ROS" }, 2),
            ("Bioquímica Ilustrada", new[] { "572 HAR", "527 HAR", "572 HRA", "572 HAB", "572 HAR", "752 HAR" }, 4)
        });
        return game;
    }

    private static PriorityMinigame CreatePriorityMinigame(Transform ui, GameObject systems, GameObject player)
    {
        RectTransform panel = H2.CreatePanel(ui, "MinijuegoPendientes", "PRIORIZA TUS PENDIENTES");
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(H.FontPath);
        TextMeshProUGUI prompt = H.CreateLabel(panel, "Indicacion", font, "", 24f, TextLight, 260f);
        Object[] cards = CreateBoxes(panel, "Tarea_", 5, 330f, new Vector2(300f, 250f), 10f, 16f, out Object[] texts);
        TextMeshProUGUI feedback = H.CreateLabel(panel, "Mensaje", font, "", 20f, TextLight, -200f);
        TextMeshProUGUI progress = H.CreateLabel(panel, "Progreso", font, "", 18f, Orange, -270f);

        PriorityMinigame game = systems.AddComponent<PriorityMinigame>();
        H2.ConfigureMinigame(game, panel.gameObject, player, "pendientes");
        H.SetArray(game, "cards", cards);
        H.SetArray(game, "cardTexts", texts);
        H.Set(game, "promptText", prompt);
        H.Set(game, "feedbackText", feedback);
        H.Set(game, "progressText", progress);
        SetTasks(game, new[]
        {
            ("Lectura de Histología", 5),
            ("Informe de laboratorio", 1),
            ("Exposición grupal", 3),
            ("Parcial de Anatomía", 2),
            ("Guía de Bioquímica", 4)
        });
        return game;
    }

    // Fila de casillas centradas con un texto adentro (días, tubos, lomos, tarjetas)
    private static Object[] CreateBoxes(RectTransform panel, string prefix, int count, float spacing, Vector2 size, float y,
        float fontSize, out Object[] texts)
    {
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(H.FontPath);
        var boxes = new List<Object>();
        var labels = new List<Object>();
        for (int i = 0; i < count; i++)
        {
            RectTransform box = H2.Rect(panel, prefix + i);
            box.anchorMin = box.anchorMax = box.pivot = new Vector2(0.5f, 0.5f);
            box.anchoredPosition = new Vector2((i - (count - 1) * 0.5f) * spacing, y);
            box.sizeDelta = size;
            Image image = box.gameObject.AddComponent<Image>();
            image.color = BoxColor;
            Outline outline = box.gameObject.AddComponent<Outline>();
            outline.effectColor = Color.black;
            outline.effectDistance = new Vector2(4f, -4f);
            TextMeshProUGUI text = H.CreateLabel(box, "Texto", font, "", fontSize, TextLight, 0f);
            text.rectTransform.sizeDelta = size - new Vector2(20f, 20f);
            boxes.Add(image);
            labels.Add(text);
        }
        texts = labels.ToArray();
        return boxes.ToArray();
    }

    // Barra de tiempo que se achica escalando en X desde la izquierda
    private static Image CreateTimerBar(RectTransform panel, float y)
    {
        RectTransform bar = H2.Rect(panel, "Tiempo");
        bar.anchorMin = bar.anchorMax = bar.pivot = new Vector2(0.5f, 0.5f);
        bar.anchoredPosition = new Vector2(0f, y);
        bar.sizeDelta = new Vector2(1300f, 22f);
        bar.gameObject.AddComponent<Image>().color = new Color(0.12f, 0.12f, 0.18f);
        RectTransform fill = H2.Rect(bar, "Relleno");
        fill.anchorMin = Vector2.zero;
        fill.anchorMax = Vector2.one;
        fill.pivot = new Vector2(0f, 0.5f);
        fill.offsetMin = fill.offsetMax = Vector2.zero;
        Image fillImage = fill.gameObject.AddComponent<Image>();
        fillImage.color = Orange;
        return fillImage;
    }

    // ================================================================
    // Datos
    // ================================================================

    private static void SetAnyOrder(ObjectiveTracker tracker, params string[] ids)
    {
        var so = new SerializedObject(tracker);
        SerializedProperty array = so.FindProperty("objectives");
        for (int i = 0; i < array.arraySize; i++)
        {
            SerializedProperty item = array.GetArrayElementAtIndex(i);
            item.FindPropertyRelative("anyOrder").boolValue = ids.Contains(item.FindPropertyRelative("id").stringValue);
        }
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetRequests(BookSearchMinigame game, (string title, string[] codes, int correct)[] items)
    {
        var so = new SerializedObject(game);
        SerializedProperty array = so.FindProperty("requests");
        array.arraySize = items.Length;
        for (int i = 0; i < items.Length; i++)
        {
            SerializedProperty item = array.GetArrayElementAtIndex(i);
            item.FindPropertyRelative("title").stringValue = items[i].title;
            item.FindPropertyRelative("correct").intValue = items[i].correct;
            SerializedProperty codes = item.FindPropertyRelative("codes");
            codes.arraySize = items[i].codes.Length;
            for (int k = 0; k < items[i].codes.Length; k++)
            {
                codes.GetArrayElementAtIndex(k).stringValue = items[i].codes[k];
            }
        }
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetTasks(PriorityMinigame game, (string name, int daysLeft)[] items)
    {
        var so = new SerializedObject(game);
        SerializedProperty array = so.FindProperty("tasks");
        array.arraySize = items.Length;
        for (int i = 0; i < items.Length; i++)
        {
            SerializedProperty item = array.GetArrayElementAtIndex(i);
            item.FindPropertyRelative("name").stringValue = items[i].name;
            item.FindPropertyRelative("daysLeft").intValue = items[i].daysLeft;
        }
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // ================================================================
    // Sprites y escenas
    // ================================================================

    private static void PrepareSprites()
    {
        H.PrepareSprites();
        foreach (string sheet in NewSheets)
        {
            H.ImportPixelArt(H.NpcPath + sheet + "_Respirar.png", 8);
            H.ImportPixelArt(H.NpcPath + sheet + "_Caminar.png", 8);
        }
        foreach (var (floorName, blocks) in new[] { ("Piso1", Piso1Blocks), ("Piso5", Piso5Blocks) })
        {
            foreach (string block in blocks)
            {
                foreach (string suffix in new[] { "", "_estres", "_ansiedad" })
                {
                    H.ImportBackground("Assets/Sprites/Escenario/" + floorName + "/" + floorName.ToLower() + "_" + block + suffix + ".png");
                }
            }
        }
    }

    // Orden: menú, Hito 1, Hito 2, Hito 3 y luego el resto que ya hubiera
    private static void UpdateBuildSettings()
    {
        var scenes = new List<EditorBuildSettingsScene>
        {
            new EditorBuildSettingsScene(MenuScenePath, true),
            new EditorBuildSettingsScene(Hito1ScenePath, true),
            new EditorBuildSettingsScene(Hito2ScenePath, true),
            new EditorBuildSettingsScene(ScenePath, true)
        };
        foreach (EditorBuildSettingsScene existing in EditorBuildSettings.scenes)
        {
            if (scenes.All(s => s.path != existing.path))
            {
                scenes.Add(existing);
            }
        }
        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
