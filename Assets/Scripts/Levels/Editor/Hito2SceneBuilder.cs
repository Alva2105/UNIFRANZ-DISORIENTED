using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using H = Hito1SceneBuilder;

// Arma la escena del Hito 2 (Primeras clases) según el plan de niveles: el pasillo del
// Piso 2 de Eduardo con un Checklist de 7 objetivos, reloj de clase, dos minijuegos
// (Arma tu horario y Apuntes rápidos), reciclaje en el pasillo, aglomeración en el
// cambio de hora y NPCs. Parte de una copia de Piso2 (no la modifica) y reutiliza las
// utilidades de Hito1SceneBuilder.
// Menú: Tools > UNIFRANZ-DISORIENTED. En modo batch:
//   Unity -batchmode -quit -projectPath . -executeMethod Hito2SceneBuilder.Build
public static class Hito2SceneBuilder
{
    private const string SourceScenePath = "Assets/Scenes/Piso2.unity";
    private const string ScenePath = "Assets/Scenes/Hito2.unity";
    private const string MenuScenePath = "Assets/Scenes/MainMenu.unity";
    private const string Hito1ScenePath = "Assets/Scenes/Hito1.unity";
    private const string NextScene = "Hito3";

    private static readonly Color Orange = new Color(0.976f, 0.435f, 0.075f);
    private static readonly Color TextLight = new Color(0.92f, 0.92f, 0.95f);
    private static readonly Color PanelColor = new Color(0.06f, 0.07f, 0.16f, 0.94f);

    // Interactuables del Piso 2 original que este hito reemplaza
    private static readonly string[] RemovedObjects =
        { "Puerta_204", "Puerta_205", "Puerta_206", "Puerta_207", "Mural", "Escaleras" };

    [MenuItem("Tools/UNIFRANZ-DISORIENTED/Generar Hito 2 (Primeras clases)")]
    public static void Build()
    {
        H.PrepareSprites();
        foreach (string item in new[] { "basura_botella", "basura_papel", "basura_lata" })
        {
            H.ImportPixelArt(H.PropsPath + item + ".png", 1);
        }

        // Copia de Piso2 conservando el .meta de Hito2 si ya existe (mismo GUID)
        if (File.Exists(ScenePath))
        {
            File.Copy(SourceScenePath, ScenePath, true);
            AssetDatabase.ImportAsset(ScenePath, ImportAssetOptions.ForceUpdate);
        }
        else if (!AssetDatabase.CopyAsset(SourceScenePath, ScenePath))
        {
            Debug.LogError("[Hito2SceneBuilder] No se pudo copiar " + SourceScenePath);
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
        MeasureBlocks(scene, out float[] left, out float[] width);
        float X(int block, float fraction) => left[block] + fraction * width[block];

        // El jugador llega por las escaleras desde la Planta Baja
        player.transform.position = new Vector3(X(2, 0.78f), floorY + 0.05f, 0f);

        // --- Estrés: sube más que en el Hito 1 ---
        StressSystem stress = systems.GetComponent<StressSystem>();
        H.SetFloat(stress, "passiveRate", 0.4f);
        H.SetFloat(stress, "sprintRate", 1.4f);

        // --- Audio ---
        AudioManager audio = systems.AddComponent<AudioManager>();
        H.Set(audio, "music", AssetDatabase.LoadAssetAtPath<AudioClip>(H.MusicPath));
        FootstepAudio footsteps = player.AddComponent<FootstepAudio>();
        H.SetArray(footsteps, "steps", Enumerable.Range(0, 5).Select(i => H.Sfx("footstep_concrete_00" + i)).ToArray());
        HeartbeatAudio heartbeat = systems.AddComponent<HeartbeatAudio>();
        H.Set(heartbeat, "lub", H.Sfx("impactSoft_heavy_000"));
        H.Set(heartbeat, "dub", H.Sfx("impactSoft_heavy_001"));

        // --- Checklist del Hito 2 ---
        ObjectiveTracker tracker = systems.AddComponent<ObjectiveTracker>();
        H.SetString(tracker, "milestoneTitle", "HITO 2");
        H.SetObjectives(tracker, new[]
        {
            ("diego", "Encuentra a Diego en el pasillo"),
            ("horario", "Arma tu horario en el mural"),
            ("aula207", "Llega al aula 207 antes de la clase"),
            ("apuntes", "Toma apuntes en la clase del Lic. Ramírez"),
            ("reciclaje", "Junta 5 basuras y llévalas al contenedor"),
            ("laboratorio", "Llega al Laboratorio de Sistemas (aula 206)"),
            ("salida", "Termina el día: baja por las escaleras")
        });
        H.Set(tracker, "objectiveCompletedClip", H.Sfx("confirmation_002"));
        H.Set(tracker, "milestoneCompletedClip", H.Sfx("confirmation_004"));
        TMP_Text objectiveText = SetupObjectiveHud(ui, tracker);

        // --- Reloj de clase debajo del objetivo ---
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
        SetDeadlines(deadline, new[]
        {
            ("aula207", 25f, 12f, "La clase empieza en"),
            ("laboratorio", 22f, 12f, "El laboratorio empieza en")
        });

        // --- Pantalla de victoria con paso al Hito 3 ---
        Transform win = H.FindDeep(ui, "Victoria");
        H.FindDeep(win, "Titulo").GetComponent<TMP_Text>().text = "HITO 2 COMPLETADO";
        H.FindDeep(win, "Mensaje").GetComponent<TMP_Text>().text = "Sobreviviste a tus primeras clases. Se viene la mitad del semestre.";
        Button next = H.CreateNextButton(win);
        LevelFlow flow = systems.GetComponent<LevelFlow>();
        H.SetString(flow, "nextScene", NextScene);
        H.Set(flow, "winNextButton", next);

        // --- Minijuegos ---
        ScheduleMinigame schedule = CreateScheduleMinigame(ui, systems, player);
        NotesMinigame notes = CreateNotesMinigame(ui, systems, player);

        // --- Contenido del pasillo ---
        // Bloque derecho: escaleras y Diego. Bloque central: aula 206 (laboratorio),
        // aglomeración del cambio de hora, aula 207 con el docente, mural y contenedor.
        // Bloque izquierdo: aulas 204 y 205 (equivocadas) y Valeria.
        Transform content = new GameObject("Hito2_Contenido").transform;
        Transform t = player.transform;

        H.CreateNpc(content, "Companero", X(2, 0.42f), floorY, t,
            "Diego - Compañero", "Hablar", "diego", -6f,
            new[]
            {
                "¡Llegaste! Primer día de clases de verdad... estoy nervioso.",
                "Dicen que el horario está en el mural, al lado del aula 207.",
                "Arma bien tu semana: hay materias que solo se dictan ciertos días.",
                "Y apúrate, que la primera clase empieza en cualquier momento."
            },
            new string[0], 0f,
            new[] { "El mural está al lado del aula 207. ¡Vamos!" },
            patrol: 1.2f);

        ObjectiveInteractable mural = CreateObject(content, "Mural", X(1, 0.86f), floorY, "Revisar", "Mural de horarios", "horario", 0f,
            new[]
            {
                "El mural tiene las materias del primer semestre y sus días.",
                "Toma un horario en blanco y empieza a armar tu semana."
            },
            new[] { "El mural de horarios. (Primero busca a Diego.)" }, 1f,
            new[] { "Tu horario ya está listo." });
        H.SetBool(mural, "completesObjective", false);          // lo completa el minijuego
        UnityEditor.Events.UnityEventTools.AddPersistentListener(H.GetEvent(mural, typeof(Interactable), "onFinished"), schedule.Begin);

        CreateObject(content, "Puerta_207", X(1, 0.648f), floorY, "Entrar", "Aula 207", "aula207", -4f,
            new[] { "Aula 207. Tu nombre está en la lista de la puerta.", "Llegaste a tu primera clase." },
            new[] { "Aula 207. Todavía no sabes si tienes clase aquí. Revisa tu horario." }, 2f,
            new[] { "Aula 207: Introducción a la Programación." });

        ObjectiveInteractable docente = H.CreateNpc(content, "Docente", X(1, 0.57f), floorY, t,
            "Lic. Ramírez - Docente", "Hablar", "apuntes", 0f,
            new[]
            {
                "Bienvenid@ a Introducción a la Programación.",
                "Hoy veremos lo básico. Anote las palabras clave: las preguntaré en el examen.",
                "Atent@, que no voy a repetir."
            },
            new[] { "Buenos días. La clase es en el aula 207; entre primero, por favor." }, 2f,
            new[] { "Repase sus apuntes en casa. Nos vemos la próxima clase." });
        H.SetBool(docente, "completesObjective", false);        // lo completa el minijuego
        UnityEditor.Events.UnityEventTools.AddPersistentListener(H.GetEvent(docente, typeof(Interactable), "onFinished"), notes.Begin);

        CreateBin(content, X(1, 0.958f), floorY);
        CreateTrash(content, "basura_botella", "una botella de plástico", X(2, 0.18f), floorY);
        CreateTrash(content, "basura_papel", "un papel arrugado", X(1, 0.76f), floorY);
        CreateTrash(content, "basura_lata", "una lata", X(1, 0.40f), floorY);
        CreateTrash(content, "basura_papel", "un papel arrugado", X(0, 0.55f), floorY);
        CreateTrash(content, "basura_botella", "una botella de plástico", X(0, 0.92f), floorY);

        // Aglomeración del cambio de hora entre el aula 207 y el laboratorio
        H.CreateCrowd(content, X(1, 0.22f), X(1, 0.48f), floorY, t);

        CreateObject(content, "Puerta_206", X(1, 0.133f), floorY, "Entrar", "Laboratorio de Sistemas", "laboratorio", -4f,
            new[] { "Laboratorio de Sistemas. Las computadoras ya están encendidas.", "Llegaste a tu segunda clase." },
            new[] { "Aula 206: Laboratorio de Sistemas. Tu clase aquí es más tarde." }, 1f,
            new[] { "El laboratorio de la segunda clase." });
        H.CreateExtra(content, "Kevin", X(1, 0.04f), floorY, t, false, "Kevin - Tutor del laboratorio", -4f, new[]
        {
            "¡Hey, nuev@! Soy tutor del Laboratorio de Sistemas.",
            "Un consejo: arma tu horario con tiempo y deja huecos para descansar.",
            "Y si el pasillo se llena en el cambio de hora, respira y avanza con calma."
        });

        // Bloque izquierdo: aulas equivocadas y una compañera
        CreateDoor(content, "Puerta_204", X(0, 0.15f), floorY, 6f, new[] { "Aula 204. Hay una clase a medio empezar.", "Todos te miran. No es tu aula." });
        CreateDoor(content, "Puerta_205", X(0, 0.79f), floorY, 4f, new[] { "Aula 205. Está cerrada con llave." });
        H.CreateExtra(content, "Valeria", X(0, 0.36f), floorY, t, false, "Valeria - Estudiante", -2f, new[]
        {
            "¿También estás en primer semestre? Me perdí buscando el aula 207.",
            "Gracias por avisarme que es la del medio del pasillo."
        });

        CreateObject(content, "Escaleras", X(2, 0.86f), floorY, "Bajar", "", "salida", -6f,
            new[] { "Bajas por las escaleras. Terminó tu primer día de clases." },
            new[] { "Todavía te quedan cosas por hacer hoy. Revisa tu objetivo." }, 2f,
            new[] { "Ya terminaste por hoy." });

        EditorSceneManager.SaveScene(scene);
        UpdateBuildSettings();
        PointHito1ToHito2();
        AssetDatabase.SaveAssets();
        Debug.Log("[Hito2SceneBuilder] Listo: " + ScenePath);
    }

    // ================================================================
    // Contenido
    // ================================================================

    // Interactuable de objetivo sin sprite propio (puertas, mural, escaleras del arte)
    internal static ObjectiveInteractable CreateObject(Transform parent, string name, float x, float floorY, string prompt,
        string speaker, string objectiveId, float stressChange, string[] lines, string[] pending, float stressPending, string[] done)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent);
        go.transform.position = new Vector3(x, floorY, 0f);
        ObjectiveInteractable item = go.AddComponent<ObjectiveInteractable>();
        H.ConfigureObjective(item, prompt, speaker, objectiveId, stressChange, lines, pending, stressPending, done);
        return item;
    }

    private static void CreateDoor(Transform parent, string name, float x, float floorY, float stressChange, string[] lines)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent);
        go.transform.position = new Vector3(x, floorY, 0f);
        H.ConfigureInteractable(go.AddComponent<Interactable>(), "Revisar", "", lines, stressChange);
    }

    private static void CreateTrash(Transform parent, string sprite, string itemName, float x, float floorY)
    {
        GameObject go = H.CreateVisual(parent, "Basura", AssetDatabase.LoadAssetAtPath<Sprite>(H.PropsPath + sprite + ".png"), x, floorY, out _, 6);
        TrashPickup trash = go.AddComponent<TrashPickup>();
        H.ConfigureInteractable(trash, "Recoger", "", new string[0], -1f);
        H.SetString(trash, "itemName", itemName);
        H.SetFloat(trash, "range", 1f);
        H.SetVector(trash, "promptOffset", new Vector2(0f, 1.6f));
        H.Set(trash, "pickClip", H.Sfx("select_002"));
    }

    // Contenedor de reciclaje: está dibujado en el pasillo, aquí solo va el interactuable
    private static void CreateBin(Transform parent, float x, float floorY)
    {
        var go = new GameObject("ContenedorReciclaje");
        go.transform.SetParent(parent);
        go.transform.position = new Vector3(x, floorY, 0f);
        RecycleBin bin = go.AddComponent<RecycleBin>();
        H.ConfigureInteractable(bin, "Botar basura", "Contenedor de reciclaje", new string[0], -2f);
        H.Set(bin, "dropClip", H.Sfx("drop_002"));
    }

    // ================================================================
    // Minijuegos
    // ================================================================

    private static ScheduleMinigame CreateScheduleMinigame(Transform ui, GameObject systems, GameObject player)
    {
        RectTransform panel = CreatePanel(ui, "MinijuegoHorario", "ARMA TU HORARIO");
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(H.FontPath);
        TextMeshProUGUI card = H.CreateLabel(panel, "Materia", font, "", 26f, Orange, 250f);
        card.rectTransform.sizeDelta = new Vector2(1600f, 100f);

        var cells = new List<Object>();
        var texts = new List<Object>();
        for (int i = 0; i < 5; i++)
        {
            RectTransform cell = Rect(panel, "Dia_" + i);
            cell.anchorMin = cell.anchorMax = cell.pivot = new Vector2(0.5f, 0.5f);
            cell.anchoredPosition = new Vector2((i - 2) * 300f, 10f);
            cell.sizeDelta = new Vector2(270f, 230f);
            Image image = cell.gameObject.AddComponent<Image>();
            image.color = new Color(0.16f, 0.18f, 0.3f);
            Outline outline = cell.gameObject.AddComponent<Outline>();
            outline.effectColor = Color.black;
            outline.effectDistance = new Vector2(4f, -4f);
            TextMeshProUGUI text = H.CreateLabel(cell, "Texto", font, "", 18f, TextLight, 0f);
            text.rectTransform.sizeDelta = new Vector2(250f, 210f);
            cells.Add(image);
            texts.Add(text);
        }
        TextMeshProUGUI feedback = H.CreateLabel(panel, "Mensaje", font, "", 20f, TextLight, -200f);
        TextMeshProUGUI progress = H.CreateLabel(panel, "Progreso", font, "", 18f, Orange, -270f);

        ScheduleMinigame game = systems.AddComponent<ScheduleMinigame>();
        ConfigureMinigame(game, panel.gameObject, player, "horario");
        H.SetArray(game, "dayCells", cells.ToArray());
        H.SetArray(game, "dayTexts", texts.ToArray());
        H.Set(game, "cardText", card);
        H.Set(game, "feedbackText", feedback);
        H.Set(game, "progressText", progress);
        SetSubjects(game, new[]
        {
            ("Introducción a la Programación", "Programación", new[] { 0, 2 }),
            ("Matemática I", "Matemática", new[] { 0, 1 }),
            ("Comunicación Oral y Escrita", "Comunicación", new[] { 3 }),
            ("Física I", "Física", new[] { 2, 4 })
        });
        return game;
    }

    private static NotesMinigame CreateNotesMinigame(Transform ui, GameObject systems, GameObject player)
    {
        RectTransform panel = CreatePanel(ui, "MinijuegoApuntes", "APUNTES RÁPIDOS");
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(H.FontPath);
        TextMeshProUGUI prompt = H.CreateLabel(panel, "Dictado", font, "", 24f, TextLight, 210f);
        prompt.rectTransform.sizeDelta = new Vector2(1600f, 120f);
        prompt.lineSpacing = 20f;

        var boxes = new List<Object>();
        var texts = new List<Object>();
        for (int i = 0; i < 3; i++)
        {
            RectTransform box = Rect(panel, "Opcion_" + i);
            box.anchorMin = box.anchorMax = box.pivot = new Vector2(0.5f, 0.5f);
            box.anchoredPosition = new Vector2((i - 1) * 440f, 20f);
            box.sizeDelta = new Vector2(400f, 110f);
            Image image = box.gameObject.AddComponent<Image>();
            image.color = new Color(0.16f, 0.18f, 0.3f);
            Outline outline = box.gameObject.AddComponent<Outline>();
            outline.effectColor = Color.black;
            outline.effectDistance = new Vector2(4f, -4f);
            TextMeshProUGUI text = H.CreateLabel(box, "Texto", font, "", 22f, TextLight, 0f);
            text.rectTransform.sizeDelta = new Vector2(380f, 100f);
            boxes.Add(image);
            texts.Add(text);
        }

        // Barra del tiempo de cada dictado
        RectTransform bar = Rect(panel, "Tiempo");
        bar.anchorMin = bar.anchorMax = bar.pivot = new Vector2(0.5f, 0.5f);
        bar.anchoredPosition = new Vector2(0f, -110f);
        bar.sizeDelta = new Vector2(1300f, 22f);
        bar.gameObject.AddComponent<Image>().color = new Color(0.12f, 0.12f, 0.18f);
        RectTransform fill = Rect(bar, "Relleno");
        // Se achica escalando en X desde la izquierda (pivote izquierdo)
        fill.anchorMin = Vector2.zero;
        fill.anchorMax = Vector2.one;
        fill.pivot = new Vector2(0f, 0.5f);
        fill.offsetMin = fill.offsetMax = Vector2.zero;
        Image fillImage = fill.gameObject.AddComponent<Image>();
        fillImage.color = Orange;

        TextMeshProUGUI feedback = H.CreateLabel(panel, "Mensaje", font, "", 20f, TextLight, -200f);
        TextMeshProUGUI progress = H.CreateLabel(panel, "Progreso", font, "", 18f, Orange, -270f);

        NotesMinigame game = systems.AddComponent<NotesMinigame>();
        ConfigureMinigame(game, panel.gameObject, player, "apuntes");
        H.Set(game, "promptText", prompt);
        H.SetArray(game, "optionBoxes", boxes.ToArray());
        H.SetArray(game, "optionTexts", texts.ToArray());
        H.Set(game, "timerFill", fillImage);
        H.Set(game, "feedbackText", feedback);
        H.Set(game, "progressText", progress);
        SetQuestions(game, new[]
        {
            ("Un algoritmo es una secuencia ordenada de ___ para resolver un problema.", new[] { "colores", "pasos", "cables" }, 1),
            ("Una variable sirve para guardar un ___.", new[] { "valor", "monitor", "teclado" }, 0),
            ("Si una condición es verdadera, se ejecuta el bloque ___.", new[] { "sino", "mientras", "si" }, 2),
            ("Repetir instrucciones varias veces se llama ___.", new[] { "bucle", "comentario", "archivo" }, 0),
            ("Antes de entregar un programa hay que ___ que funcione.", new[] { "imprimir", "probar", "borrar" }, 1)
        });
        return game;
    }

    internal static void ConfigureMinigame(MinigameBase game, GameObject panel, GameObject player, string objectiveId)
    {
        H.Set(game, "panel", panel);
        H.Set(game, "player", player.GetComponent<PlayerMovement>());
        H.Set(game, "interactor", player.GetComponent<PlayerInteractor>());
        H.SetString(game, "objectiveId", objectiveId);
        H.Set(game, "moveClip", H.Sfx("select_002"));
        H.Set(game, "goodClip", H.Sfx("confirmation_002"));
        H.Set(game, "badClip", H.Sfx("error_004"));
        H.Set(game, "finishClip", H.Sfx("confirmation_004"));
        panel.SetActive(false);
    }

    // Panel a pantalla completa con fondo oscuro y título, debajo del menú de pausa
    internal static RectTransform CreatePanel(Transform ui, string name, string title)
    {
        RectTransform panel = Rect(ui, name);
        panel.anchorMin = Vector2.zero;
        panel.anchorMax = Vector2.one;
        panel.offsetMin = panel.offsetMax = Vector2.zero;
        panel.gameObject.AddComponent<Image>().color = PanelColor;
        Transform pause = H.FindDeep(ui, "MenuPausa");
        if (pause != null)
        {
            panel.SetSiblingIndex(pause.GetSiblingIndex());
        }
        H.CreateLabel(panel, "Titulo", AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(H.FontPath), title, 40f, Orange, 400f);
        return panel;
    }

    internal static RectTransform Rect(Transform parent, string name)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    // ================================================================
    // HUD y datos
    // ================================================================

    // Objetivo del hito sobre una franja oscura (igual que en el Hito 1)
    internal static TMP_Text SetupObjectiveHud(Transform ui, ObjectiveTracker tracker)
    {
        TMP_Text objectiveText = H.FindDeep(ui, "Objetivo").GetComponent<TMP_Text>();
        objectiveText.richText = true;
        var objectiveRect = (RectTransform)objectiveText.transform;
        objectiveRect.sizeDelta = new Vector2(1700f, objectiveRect.sizeDelta.y);
        objectiveText.textWrappingMode = TextWrappingModes.NoWrap;
        var barGo = new GameObject("FondoObjetivo", typeof(RectTransform));
        barGo.transform.SetParent(ui, false);
        barGo.transform.SetSiblingIndex(objectiveText.transform.GetSiblingIndex());
        var bar = (RectTransform)barGo.transform;
        bar.anchorMin = bar.anchorMax = bar.pivot = new Vector2(0f, 1f);
        bar.anchoredPosition = objectiveRect.anchoredPosition + new Vector2(-16f, 12f);
        bar.sizeDelta = new Vector2(1730f, 92f);
        Image barImage = barGo.AddComponent<Image>();
        barImage.color = new Color(0.06f, 0.07f, 0.16f, 0.8f);
        barImage.raycastTarget = false;
        ObjectiveHUD hud = objectiveText.gameObject.AddComponent<ObjectiveHUD>();
        H.Set(hud, "tracker", tracker);
        H.Set(hud, "label", objectiveText);
        return objectiveText;
    }

    internal static void SetDeadlines(ObjectiveDeadline deadline, (string id, float seconds, float late, string label)[] items)
    {
        var so = new SerializedObject(deadline);
        SerializedProperty array = so.FindProperty("deadlines");
        array.arraySize = items.Length;
        for (int i = 0; i < items.Length; i++)
        {
            SerializedProperty item = array.GetArrayElementAtIndex(i);
            item.FindPropertyRelative("objectiveId").stringValue = items[i].id;
            item.FindPropertyRelative("seconds").floatValue = items[i].seconds;
            item.FindPropertyRelative("lateStress").floatValue = items[i].late;
            item.FindPropertyRelative("label").stringValue = items[i].label;
        }
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    internal static void SetSubjects(ScheduleMinigame game, (string name, string shortName, int[] days)[] items)
    {
        var so = new SerializedObject(game);
        SerializedProperty array = so.FindProperty("subjects");
        array.arraySize = items.Length;
        for (int i = 0; i < items.Length; i++)
        {
            SerializedProperty item = array.GetArrayElementAtIndex(i);
            item.FindPropertyRelative("name").stringValue = items[i].name;
            item.FindPropertyRelative("shortName").stringValue = items[i].shortName;
            SerializedProperty days = item.FindPropertyRelative("days");
            days.arraySize = items[i].days.Length;
            for (int k = 0; k < items[i].days.Length; k++)
            {
                days.GetArrayElementAtIndex(k).intValue = items[i].days[k];
            }
        }
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    internal static void SetQuestions(NotesMinigame game, (string prompt, string[] options, int correct)[] items)
    {
        var so = new SerializedObject(game);
        SerializedProperty array = so.FindProperty("questions");
        array.arraySize = items.Length;
        for (int i = 0; i < items.Length; i++)
        {
            SerializedProperty item = array.GetArrayElementAtIndex(i);
            item.FindPropertyRelative("prompt").stringValue = items[i].prompt;
            item.FindPropertyRelative("correct").intValue = items[i].correct;
            SerializedProperty options = item.FindPropertyRelative("options");
            options.arraySize = items[i].options.Length;
            for (int k = 0; k < items[i].options.Length; k++)
            {
                options.GetArrayElementAtIndex(k).stringValue = items[i].options[k];
            }
        }
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // Borde izquierdo y ancho de cada bloque del pasillo, medidos con su arte normal
    private static void MeasureBlocks(Scene scene, out float[] left, out float[] width)
    {
        Transform scenery = H.FindRoot(scene, "Escenario").transform;
        SpriteRenderer[] blocks = scenery.Cast<Transform>()
            .Select(b => b.Find("Normal"))
            .Where(n => n != null)
            .Select(n => n.GetComponent<SpriteRenderer>())
            .OrderBy(r => r.bounds.min.x)
            .ToArray();
        left = blocks.Select(r => r.bounds.min.x).ToArray();
        width = blocks.Select(r => r.bounds.size.x).ToArray();
    }

    // ================================================================
    // Escenas
    // ================================================================

    // Orden: menú, Hito 1, Hito 2 y luego el resto que ya hubiera
    private static void UpdateBuildSettings()
    {
        var scenes = new List<EditorBuildSettingsScene>
        {
            new EditorBuildSettingsScene(MenuScenePath, true),
            new EditorBuildSettingsScene(Hito1ScenePath, true),
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

    // CONTINUAR del Hito 1 lleva al Hito 2
    private static void PointHito1ToHito2()
    {
        if (!File.Exists(Hito1ScenePath))
        {
            return;
        }
        Scene hito1 = EditorSceneManager.OpenScene(Hito1ScenePath, OpenSceneMode.Single);
        LevelFlow flow = hito1.GetRootGameObjects().Select(r => r.GetComponentInChildren<LevelFlow>(true)).FirstOrDefault(f => f != null);
        if (flow != null)
        {
            H.SetString(flow, "nextScene", Path.GetFileNameWithoutExtension(ScenePath));
            EditorSceneManager.SaveScene(hito1);
        }
        TMP_Text message = hito1.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<TMP_Text>(true))
            .FirstOrDefault(x => x.name == "Mensaje" && x.text.Contains("Piso 2"));
        if (message != null)
        {
            message.text = "Orientación lista. Tus primeras clases te esperan en el Piso 2.";
            EditorUtility.SetDirty(message);
            EditorSceneManager.SaveScene(hito1);
        }
    }
}
