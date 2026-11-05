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
using H3 = Hito3SceneBuilder;

// Arma la escena del Hito 4 (Parciales y exposiciones) según el plan de niveles: el
// Subsuelo de la sede La Paz (pasillo, Sala de Anatomía y Anfiteatro) y, en la Planta
// Baja, el Game Room y el Auditorio, unidos por las gradas. Es el punto de máxima
// presión: el estrés sube rápido, hay tres aglomeraciones, las luces del Subsuelo
// parpadean con Ansiedad y algunos NPCs cambian sus frases. Checklist de 6 objetivos
// (un bloque en cualquier orden), reloj de la exposición y cuatro minijuegos.
// Menú: Tools > UNIFRANZ-DISORIENTED. En modo batch:
//   Unity -batchmode -quit -projectPath . -executeMethod Hito4SceneBuilder.Build
public static class Hito4SceneBuilder
{
    private const string SourceScenePath = "Assets/Scenes/Piso2.unity";
    private const string ScenePath = "Assets/Scenes/Hito4.unity";
    private const string MenuScenePath = "Assets/Scenes/MainMenu.unity";
    private const string NextScene = "Hito5";
    private static readonly string[] PreviousScenes =
        { "Assets/Scenes/Hito1.unity", "Assets/Scenes/Hito2.unity", "Assets/Scenes/Hito3.unity" };

    private static readonly string[] SubsueloBlocks = { "pasillo", "anatomia", "anfiteatro" };
    // De la Planta Baja solo se usan los dos últimos bloques (el arte sigue continuo)
    private static readonly string[] PlantaBajaBlocks = { "game_room", "auditorio" };
    private const string ShadePath = "Assets/Sprites/Escenario/Subsuelo/subsuelo_sombra.png";
    private const float PlantaBajaOffsetX = 300f;

    private static readonly Color Orange = new Color(0.976f, 0.435f, 0.075f);
    private static readonly Color TextLight = new Color(0.92f, 0.92f, 0.95f);

    private static readonly string[] RemovedObjects =
        { "Puerta_204", "Puerta_205", "Puerta_206", "Puerta_207", "Mural", "Escaleras" };

    [MenuItem("Tools/UNIFRANZ-DISORIENTED/Generar Hito 4 (Parciales y exposiciones)")]
    public static void Build()
    {
        PrepareSprites();

        // Copia de Piso2 conservando el .meta de Hito4 si ya existe (mismo GUID)
        if (File.Exists(ScenePath))
        {
            File.Copy(SourceScenePath, ScenePath, true);
            AssetDatabase.ImportAsset(ScenePath, ImportAssetOptions.ForceUpdate);
        }
        else if (!AssetDatabase.CopyAsset(SourceScenePath, ScenePath))
        {
            Debug.LogError("[Hito4SceneBuilder] No se pudo copiar " + SourceScenePath);
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

        // --- Escenario: Subsuelo y Planta Baja lado a lado en X, unidos por las gradas ---
        Object.DestroyImmediate(H.FindRoot(scene, "Escenario"));
        H.Floor ss = H.BuildFloor("Subsuelo", SubsueloBlocks, 0f, floorY);
        H.Floor pb = H.BuildFloor("PlantaBaja", PlantaBajaBlocks, PlantaBajaOffsetX, floorY);
        CreateFlicker(ss, floorY);

        PolygonCollider2D ssBounds = H.FindRoot(scene, "LimitesCamara").GetComponent<PolygonCollider2D>();
        H.SetBounds(ssBounds, ss);
        PolygonCollider2D pbBounds = new GameObject("LimitesCamara_PlantaBaja").AddComponent<PolygonCollider2D>();
        pbBounds.isTrigger = true;
        H.SetBounds(pbBounds, pb);
        GameObject vcamGo = H.FindRoot(scene, "CinemachineCamera");
        CinemachineCamera vcam = vcamGo.GetComponent<CinemachineCamera>();
        CinemachineConfiner2D confiner = vcamGo.GetComponent<CinemachineConfiner2D>();
        confiner.BoundingShape2D = ssBounds;

        // El jugador baja por las gradas al Subsuelo
        player.transform.position = new Vector3(ss.X(0, 0.34f), floorY + 0.05f, 0f);

        // --- Estrés: semana de parciales, el más alto hasta ahora ---
        StressSystem stress = systems.GetComponent<StressSystem>();
        H.SetFloat(stress, "passiveRate", 0.8f);
        H.SetFloat(stress, "sprintRate", 2f);

        // --- Audio ---
        AudioManager audio = systems.AddComponent<AudioManager>();
        H.Set(audio, "music", AssetDatabase.LoadAssetAtPath<AudioClip>(H.MusicPath));
        FootstepAudio footsteps = player.AddComponent<FootstepAudio>();
        H.SetArray(footsteps, "steps", Enumerable.Range(0, 5).Select(i => H.Sfx("footstep_concrete_00" + i)).ToArray());
        HeartbeatAudio heartbeat = systems.AddComponent<HeartbeatAudio>();
        H.Set(heartbeat, "lub", H.Sfx("impactSoft_heavy_000"));
        H.Set(heartbeat, "dub", H.Sfx("impactSoft_heavy_001"));

        // --- Checklist del Hito 4: los objetivos 2 a 4 se hacen en cualquier orden ---
        ObjectiveTracker tracker = systems.AddComponent<ObjectiveTracker>();
        H.SetString(tracker, "milestoneTitle", "HITO 4");
        H.SetObjectives(tracker, new[]
        {
            ("grupo", "Reúnete con Camila, tu grupo de exposición"),
            ("memoria", "Sala de Anatomía"),
            ("repaso", "Repaso en el Anfiteatro"),
            ("crisis", "Ayuda a Diego"),
            ("exposicion", "Expón en el Auditorio (Planta Baja)"),
            ("salida", "Habla con Lucía en el Game Room")
        });
        SetAnyOrder(tracker, "memoria", "repaso", "crisis");
        H.Set(tracker, "objectiveCompletedClip", H.Sfx("confirmation_002"));
        H.Set(tracker, "milestoneCompletedClip", H.Sfx("confirmation_004"));
        TMP_Text objectiveText = H2.SetupObjectiveHud(ui, tracker);
        objectiveText.fontSizeMax = objectiveText.fontSize;
        objectiveText.fontSizeMin = 12f;
        objectiveText.enableAutoSizing = true;

        // --- Reloj de la exposición ---
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
        H2.SetDeadlines(deadline, new[] { ("exposicion", 75f, 15f, "La exposición empieza en") });

        // --- Pantalla de victoria con paso al Hito 5 ---
        Transform win = H.FindDeep(ui, "Victoria");
        H.FindDeep(win, "Titulo").GetComponent<TMP_Text>().text = "HITO 4 COMPLETADO";
        H.FindDeep(win, "Mensaje").GetComponent<TMP_Text>().text = "Sobreviviste a parciales y exposiciones. Solo faltan los exámenes finales.";
        Button next = H.CreateNextButton(win);
        LevelFlow flow = systems.GetComponent<LevelFlow>();
        H.SetString(flow, "nextScene", NextScene);
        H.Set(flow, "winNextButton", next);

        // --- Minijuegos y fundido de las gradas ---
        MemoryMinigame memory = CreateMemoryMinigame(ui, systems, player);
        NotesMinigame notes = CreateReviewMinigame(ui, systems, player);
        PresentationMinigame presentation = CreatePresentationMinigame(ui, systems, player);
        BreathingMinigame breathing = H.CreateBreathingMinigame(ui, systems, player, "RESPIRA CON DIEGO");
        H.SetString(breathing, "objectiveId", "crisis");
        H.SetFloat(breathing, "inhaleSeconds", 3.5f);
        H.SetFloat(breathing, "exhaleSeconds", 2.5f);
        H.SetFloat(breathing, "maxRelief", 20f);
        CanvasGroup fade = H.CreateFade(ui);

        // --- Contenido ---
        // Subsuelo: pasillo (gradas, Camila) -> Sala de Anatomía (Dr. Quispe) -> Anfiteatro (Kevin, Diego).
        // Planta Baja: Game Room (Lucía) -> Auditorio (exposición con el Lic. Ramírez).
        Transform content = new GameObject("Hito4_Contenido").transform;
        Transform t = player.transform;
        PlayerMovement movement = player.GetComponent<PlayerMovement>();

        // Gradas Subsuelo <-> Planta Baja
        Transform pbArrival = H.CreatePoint(content, "Llegada_PlantaBaja", pb.X(0, 0.12f), floorY);
        Transform ssArrival = H.CreatePoint(content, "Llegada_Subsuelo", ss.X(0, 0.34f), floorY);
        H.CreatePortal(content, "Gradas_Subir", ss.X(0, 0.14f), floorY, "Subir a Planta Baja",
            pbArrival, pbBounds, movement, vcam, confiner, fade);
        H.CreatePortal(content, "Gradas_Bajar", pb.X(0, 0.04f), floorY, "Bajar al Subsuelo",
            ssArrival, ssBounds, movement, vcam, confiner, fade);

        // Pasillo del Subsuelo
        H.CreateNpc(content, "Camila", ss.X(0, 0.50f), floorY, t,
            "Camila - Compañera de grupo", "Hablar", "grupo", -4f,
            new[]
            {
                "¡Llegaste! Hoy es el día: parcial práctico y nuestra exposición.",
                "Antes de exponer, repasa los huesos en la Sala de Anatomía con el Dr. Quispe.",
                "Kevin da un repaso en el Anfiteatro... y Diego anda muy nervioso por ahí.",
                "Nos vemos en el Auditorio, en la Planta Baja. ¡Tú puedes!"
            },
            new string[0], 0f,
            new[] { "¡Nos vemos en el Auditorio! Respira, ya ensayamos mucho." },
            patrol: 0.6f);
        H.CreateCrowd(content, ss.X(0, 0.62f), ss.X(0, 0.92f), floorY, t);

        // Sala de Anatomía: memoria de huesos con el Dr. Quispe
        ObjectiveInteractable quispe = H.CreateNpc(content, "DocenteAnatomia", ss.X(1, 0.50f), floorY, t,
            "Dr. Quispe - Docente de Anatomía", "Hablar", "memoria", 0f,
            new[]
            {
                "Buenos días. El parcial práctico es reconocer huesos y su ubicación.",
                "Las cartas están boca abajo: une cada hueso con su zona del cuerpo.",
                "Con calma. Si se pone nervios@, todo se le va a mezclar."
            },
            new[] { "Primero reúnase con su grupo; Camila lo busca en el pasillo." }, 3f,
            new[] { "Bien repasado. Ahora concéntrese en su exposición." });
        H.SetBool(quispe, "completesObjective", false);
        UnityEditor.Events.UnityEventTools.AddPersistentListener(H.GetEvent(quispe, typeof(Interactable), "onFinished"), memory.Begin);

        CreateMoodExtra(content, "Valeria", ss.X(1, 0.28f), floorY, t, "Valeria - Estudiante",
            new[]
            {
                "Llevo dos horas aquí repasando. Ya distingo la tibia del peroné... creo.",
                "Suerte en tu exposición. Dicen que el Lic. Ramírez pregunta mucho."
            },
            new[]
            {
                "¿Escuchaste eso? Las luces otra vez...",
                "Todos dicen que este parcial lo reprueba la mitad del curso. ¿Y si somos nosotros?"
            });
        CreateZone(content, ss.X(1, 0.62f), ss.X(1, 0.86f), t);    // estudiantes amontonados en los mesones

        // Anfiteatro: repaso con ruido y Diego en crisis
        ObjectiveInteractable kevin = H.CreateNpc(content, "Kevin", ss.X(2, 0.44f), floorY, t,
            "Kevin - Ayudante de cátedra", "Hablar", "repaso", 0f,
            new[]
            {
                "¡Repaso express antes del parcial! Hay mucho ruido, así que atent@.",
                "Voy rápido: elige la palabra correcta antes de que se acabe el tiempo."
            },
            new[] { "Primero habla con tu grupo; Camila te espera en el pasillo." }, 3f,
            new[] { "Ya repasaste todo. ¡Mucha suerte!" });
        H.SetBool(kevin, "completesObjective", false);
        UnityEditor.Events.UnityEventTools.AddPersistentListener(H.GetEvent(kevin, typeof(Interactable), "onFinished"), notes.Begin);

        ObjectiveInteractable diego = H.CreateNpc(content, "Companero", ss.X(2, 0.94f), floorY, t,
            "Diego - Compañero", "Hablar", "crisis", 0f,
            new[]
            {
                "No puedo... me tiemblan las manos. Voy a reprobar, lo sé.",
                "No me sale el aire, todo me da vueltas...",
                "(Te sientas a su lado y le propones respirar juntos, como enseñó Lucía.)"
            },
            new[] { "Ahora no... (Primero habla con Camila, en el pasillo.)" }, 3f,
            new[] { "Gracias por quedarte conmigo. Ya estoy mejor. ¡Ve a tu exposición!" },
            patrol: 0.8f);
        H.SetBool(diego, "completesObjective", false);
        UnityEditor.Events.UnityEventTools.AddPersistentListener(H.GetEvent(diego, typeof(Interactable), "onFinished"), breathing.Begin);

        // Game Room: Lucía cierra el día
        H.CreateNpc(content, "GuiaBienestar", pb.X(0, 0.56f), floorY, t,
            "Lucía - Guía de Bienestar", "Hablar", "salida", -15f,
            new[]
            {
                "¡Me contaron de tu exposición! Y de cómo ayudaste a Diego.",
                "Esta fue la semana más dura del semestre, y la superaste.",
                "Recuerda: pedir ayuda y ayudar a otros también es parte de estudiar.",
                "Descansa. Los finales llegan pronto, pero ya sabes cómo manejar el estrés."
            },
            new[] { "¡Hola! Te ves apurad@. Termina tus pendientes y luego hablamos." }, 1f,
            new[] { "Ve a descansar. ¡Te lo ganaste!" });
        H.CreateRestPoint(content, pb.X(0, 0.84f), floorY);
        CreateZone(content, pb.X(0, 0.20f), pb.X(0, 0.44f), t, "Sofia", "Andres");

        // Auditorio: exposición
        ObjectiveInteractable ramirez = H.CreateNpc(content, "Docente", pb.X(1, 0.66f), floorY, t,
            "Lic. Ramírez - Jurado", "Hablar", "exposicion", 0f,
            new[]
            {
                "Su grupo es el siguiente. El auditorio está lleno, así que hable claro.",
                "Mientras expone le haré algunas preguntas. Mantenga la calma.",
                "Adelante, cuando esté list@."
            },
            new[]
            {
                "Todavía no es su turno. Termine de prepararse.",
                "(Revisa tu objetivo, arriba en la pantalla.)"
            }, 3f,
            new[] { "Muy buena exposición. Su grupo aprobó." });
        H.SetBool(ramirez, "completesObjective", false);
        UnityEditor.Events.UnityEventTools.AddPersistentListener(H.GetEvent(ramirez, typeof(Interactable), "onFinished"), presentation.Begin);

        CreateMoodExtra(content, "Marco", pb.X(1, 0.30f), floorY, t, "Marco - Estudiante",
            new[]
            {
                "Mi grupo ya expuso. ¡Qué alivio!",
                "Tranquil@: el jurado pregunta, pero si estudiaste, sale."
            },
            new[]
            {
                "Hay como doscientas personas mirando...",
                "Al grupo anterior lo hicieron llorar. Bueno... eso dicen."
            });

        EditorSceneManager.SaveScene(scene);
        UpdateBuildSettings();
        AssetDatabase.SaveAssets();
        Debug.Log("[Hito4SceneBuilder] Listo: " + ScenePath);
    }

    // ================================================================
    // Contenido
    // ================================================================

    // NPC que no es parte del Checklist y cambia sus frases con Ansiedad
    private static void CreateMoodExtra(Transform parent, string sheet, float x, float floorY, Transform player,
        string speaker, string[] lines, string[] linesAnxious)
    {
        GameObject npc = H.CreatePerson(parent, sheet, x, floorY, player);
        StressAwareInteractable item = npc.AddComponent<StressAwareInteractable>();
        H.ConfigureInteractable(item, "Hablar", speaker, lines, -2f);
        H.SetStrings(item, "linesAnxious", linesAnxious);
    }

    // Zona de aglomeración: sube el estrés al pasar; puede tener gente de fondo
    private static void CreateZone(Transform parent, float start, float end, Transform player, params string[] people)
    {
        var zoneGo = new GameObject("ZonaAglomeracion");
        zoneGo.transform.SetParent(parent);
        StressZone zone = zoneGo.AddComponent<StressZone>();
        H.Set(zone, "player", player);
        H.SetFloat(zone, "minX", start);
        H.SetFloat(zone, "maxX", end);
        H.SetFloat(zone, "stressPerSecond", 3f);
        for (int i = 0; i < people.Length; i++)
        {
            float x = Mathf.Lerp(start, end, (i + 1f) / (people.Length + 1f));
            H.CreatePerson(parent, people[i], x, player.position.y - 0.05f, player, false, i % 2 == 1, 4 + i % 3);
        }
    }

    // Capa oscura sobre todo el Subsuelo para el parpadeo de luces en Ansiedad
    private static void CreateFlicker(H.Floor floor, float floorY)
    {
        var go = new GameObject("LucesParpadeantes");
        Sprite sprite = H.LoadSprite(ShadePath);
        go.transform.position = new Vector3((floor.Start + floor.End) * 0.5f, 0f, 0f);
        go.transform.localScale = new Vector3((floor.End - floor.Start) / sprite.bounds.size.x, 12f / sprite.bounds.size.y, 1f);
        SpriteRenderer shade = go.AddComponent<SpriteRenderer>();
        shade.sprite = sprite;
        shade.color = new Color(0f, 0f, 0f, 0f);
        shade.sortingOrder = 15;                                 // delante del jugador, debajo del HUD
        FlickerLights lights = go.AddComponent<FlickerLights>();
        H.Set(lights, "shade", shade);
    }

    // ================================================================
    // Minijuegos
    // ================================================================

    private static MemoryMinigame CreateMemoryMinigame(Transform ui, GameObject systems, GameObject player)
    {
        RectTransform panel = H2.CreatePanel(ui, "MinijuegoMemoria", "MEMORIA DE ANATOMÍA");
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(H.FontPath);
        TextMeshProUGUI prompt = H.CreateLabel(panel, "Indicacion", font, "", 24f, TextLight, 290f);
        // Dos filas de seis cartas; el cursor las recorre en orden
        Object[] top = H3.CreateBoxes(panel, "Carta_", 6, 270f, new Vector2(240f, 170f), 115f, 17f, out Object[] topTexts);
        Object[] bottom = H3.CreateBoxes(panel, "Carta_", 6, 270f, new Vector2(240f, 170f), -85f, 17f, out Object[] bottomTexts);
        for (int i = 0; i < bottom.Length; i++)
        {
            ((Image)bottom[i]).name = "Carta_" + (i + 6);
        }
        TextMeshProUGUI feedback = H.CreateLabel(panel, "Mensaje", font, "", 20f, TextLight, -230f);
        TextMeshProUGUI progress = H.CreateLabel(panel, "Progreso", font, "", 18f, Orange, -290f);

        MemoryMinigame game = systems.AddComponent<MemoryMinigame>();
        H2.ConfigureMinigame(game, panel.gameObject, player, "memoria");
        H.SetArray(game, "cards", top.Concat(bottom).ToArray());
        H.SetArray(game, "cardTexts", topTexts.Concat(bottomTexts).ToArray());
        H.Set(game, "promptText", prompt);
        H.Set(game, "feedbackText", feedback);
        H.Set(game, "progressText", progress);
        SetPairs(game, new[]
        {
            ("Fémur", "Muslo"), ("Húmero", "Brazo"), ("Tibia", "Pierna"),
            ("Cráneo", "Cabeza"), ("Esternón", "Pecho"), ("Radio", "Antebrazo")
        });
        return game;
    }

    // Apuntes rápidos más difícil (D4): menos tiempo y más preguntas
    private static NotesMinigame CreateReviewMinigame(Transform ui, GameObject systems, GameObject player)
    {
        RectTransform panel = H2.CreatePanel(ui, "MinijuegoRepaso", "REPASO CON RUIDO");
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(H.FontPath);
        TextMeshProUGUI prompt = H.CreateLabel(panel, "Dictado", font, "", 24f, TextLight, 210f);
        prompt.rectTransform.sizeDelta = new Vector2(1600f, 120f);
        prompt.lineSpacing = 20f;
        Object[] boxes = H3.CreateBoxes(panel, "Opcion_", 3, 440f, new Vector2(400f, 110f), 20f, 22f, out Object[] texts);
        Image fill = H3.CreateTimerBar(panel, -110f);
        TextMeshProUGUI feedback = H.CreateLabel(panel, "Mensaje", font, "", 20f, TextLight, -200f);
        TextMeshProUGUI progress = H.CreateLabel(panel, "Progreso", font, "", 18f, Orange, -270f);

        NotesMinigame game = systems.AddComponent<NotesMinigame>();
        H2.ConfigureMinigame(game, panel.gameObject, player, "repaso");
        H.Set(game, "promptText", prompt);
        H.SetArray(game, "optionBoxes", boxes);
        H.SetArray(game, "optionTexts", texts);
        H.Set(game, "timerFill", fill);
        H.Set(game, "feedbackText", feedback);
        H.Set(game, "progressText", progress);
        H.SetFloat(game, "secondsPerQuestion", 6f);
        H.SetFloat(game, "stressPerMiss", 4f);
        H2.SetQuestions(game, new[]
        {
            ("El hueso más largo del cuerpo humano es el ___.", new[] { "fémur", "radio", "húmero" }, 0),
            ("El corazón tiene ___ cavidades.", new[] { "dos", "cuatro", "seis" }, 1),
            ("La rótula se encuentra en la ___.", new[] { "rodilla", "cadera", "muñeca" }, 0),
            ("El músculo que separa el tórax del abdomen es el ___.", new[] { "bíceps", "trapecio", "diafragma" }, 2),
            ("Las neuronas forman parte del sistema ___.", new[] { "óseo", "nervioso", "digestivo" }, 1),
            ("El cráneo protege al ___.", new[] { "encéfalo", "hígado", "pulmón" }, 0)
        });
        return game;
    }

    private static PresentationMinigame CreatePresentationMinigame(Transform ui, GameObject systems, GameObject player)
    {
        RectTransform panel = H2.CreatePanel(ui, "MinijuegoExposicion", "EXPOSICIÓN EN EL AUDITORIO");
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(H.FontPath);
        TextMeshProUGUI prompt = H.CreateLabel(panel, "Indicacion", font, "", 24f, TextLight, 260f);
        prompt.rectTransform.sizeDelta = new Vector2(1600f, 120f);
        prompt.lineSpacing = 20f;

        // Barra de calma con la zona central y la aguja
        RectTransform bar = H2.Rect(panel, "BarraCalma");
        bar.anchorMin = bar.anchorMax = bar.pivot = new Vector2(0.5f, 0.5f);
        bar.anchoredPosition = new Vector2(0f, 120f);
        bar.sizeDelta = new Vector2(1300f, 60f);
        bar.gameObject.AddComponent<Image>().color = new Color(0.12f, 0.12f, 0.18f);
        RectTransform zone = H2.Rect(bar, "ZonaCalma");
        zone.anchorMin = zone.anchorMax = zone.pivot = new Vector2(0.5f, 0.5f);
        zone.sizeDelta = new Vector2(390f, 60f);
        Image zoneImage = zone.gameObject.AddComponent<Image>();
        zoneImage.color = new Color(0.2f, 0.6f, 0.35f, 0.8f);
        RectTransform needle = H2.Rect(bar, "Aguja");
        needle.anchorMin = needle.anchorMax = needle.pivot = new Vector2(0.5f, 0.5f);
        needle.sizeDelta = new Vector2(14f, 90f);
        needle.gameObject.AddComponent<Image>().color = Orange;

        // Preguntas del jurado (se muestran solo cuando pregunta)
        RectTransform questionGroup = H2.Rect(panel, "Preguntas");
        questionGroup.anchorMin = Vector2.zero;
        questionGroup.anchorMax = Vector2.one;
        questionGroup.offsetMin = questionGroup.offsetMax = Vector2.zero;
        Object[] boxes = H3.CreateBoxes(questionGroup, "Opcion_", 3, 440f, new Vector2(400f, 100f), -20f, 22f, out Object[] texts);
        Image fill = H3.CreateTimerBar(panel, -120f);
        TextMeshProUGUI feedback = H.CreateLabel(panel, "Mensaje", font, "", 20f, TextLight, -200f);
        TextMeshProUGUI progress = H.CreateLabel(panel, "Progreso", font, "", 18f, Orange, -270f);

        PresentationMinigame game = systems.AddComponent<PresentationMinigame>();
        H2.ConfigureMinigame(game, panel.gameObject, player, "exposicion");
        H.Set(game, "needle", needle);
        H.Set(game, "bar", bar);
        H.Set(game, "calmZone", zoneImage);
        H.Set(game, "questionGroup", questionGroup.gameObject);
        H.SetArray(game, "optionBoxes", boxes);
        H.SetArray(game, "optionTexts", texts);
        H.Set(game, "promptText", prompt);
        H.Set(game, "timerFill", fill);
        H.Set(game, "feedbackText", feedback);
        H.Set(game, "progressText", progress);
        SetPresentationQuestions(game, new[]
        {
            ("¿Cuántos huesos tiene el cuerpo humano adulto?", new[] { "206", "150", "312" }, 0),
            ("¿Qué órgano bombea la sangre?", new[] { "El pulmón", "El corazón", "El riñón" }, 1),
            ("¿Qué une un músculo con un hueso?", new[] { "Ligamento", "Cartílago", "Tendón" }, 2)
        });
        return game;
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

    private static void SetPairs(MemoryMinigame game, (string first, string second)[] items)
    {
        var so = new SerializedObject(game);
        SerializedProperty array = so.FindProperty("pairs");
        array.arraySize = items.Length;
        for (int i = 0; i < items.Length; i++)
        {
            SerializedProperty item = array.GetArrayElementAtIndex(i);
            item.FindPropertyRelative("first").stringValue = items[i].first;
            item.FindPropertyRelative("second").stringValue = items[i].second;
        }
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetPresentationQuestions(PresentationMinigame game, (string prompt, string[] options, int correct)[] items)
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

    // ================================================================
    // Sprites y escenas
    // ================================================================

    private static void PrepareSprites()
    {
        H.PrepareSprites();
        foreach (string sheet in new[] { "DocenteAnatomia", "Camila" })
        {
            H.ImportPixelArt(H.NpcPath + sheet + "_Respirar.png", 8);
            H.ImportPixelArt(H.NpcPath + sheet + "_Caminar.png", 8);
        }
        foreach (string block in SubsueloBlocks)
        {
            foreach (string suffix in new[] { "", "_estres", "_ansiedad" })
            {
                H.ImportBackground("Assets/Sprites/Escenario/Subsuelo/subsuelo_" + block + suffix + ".png");
            }
        }
        H.ImportBackground(ShadePath);
    }

    // Orden: menú, Hito 1 a Hito 4 y luego el resto que ya hubiera
    private static void UpdateBuildSettings()
    {
        var scenes = new List<EditorBuildSettingsScene> { new EditorBuildSettingsScene(MenuScenePath, true) };
        scenes.AddRange(PreviousScenes.Select(path => new EditorBuildSettingsScene(path, true)));
        scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
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
