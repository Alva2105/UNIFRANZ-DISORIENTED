using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Arma la escena del Hito 1 (Planta Baja / Recepción) según el GDD:
// introducción y orientación con estrés bajo. Parte de una copia de Piso2 para
// reutilizar su HUD, diálogos, pausa, cámara y pantallas finales, y le cambia el
// contenido: Checklist de 8 objetivos, NPCs, Cajas, molinete, aglomeración,
// minijuego de respiración, tótem, dispensador y audio.
//
// El escenario es la Planta Baja y el Mezzanine reales de la sede La Paz (según el
// tour virtual), unidos por las gradas de Recepción.
// Sus utilidades (NPCs, interactuables, sprites, audio y campos serializados) son
// internal: las reutilizan los generadores de los demás hitos.
// Menú: Tools > UNIFRANZ-DISORIENTED. En modo batch:
//   Unity -batchmode -quit -projectPath . -executeMethod Hito1SceneBuilder.Build
public static class Hito1SceneBuilder
{
    // --- Rutas ---
    private const string SourceScenePath = "Assets/Scenes/Piso2.unity";
    private const string ScenePath = "Assets/Scenes/Hito1.unity";
    private const string MenuScenePath = "Assets/Scenes/MainMenu.unity";
    private const string NextScene = "Hito2";
    internal const string NpcPath = "Assets/Sprites/Personajes/NPCs/";
    internal const string PropsPath = "Assets/Sprites/Escenario/Objetos/";
    internal const string SfxPath = "Assets/Audio/SFX/";
    internal const string MusicPath = "Assets/Audio/Music/musica_recepcion_lobby.wav";
    private const string ButtonsPath = "Assets/Sprites/UI/Menus/Botones/boton_vacio_";
    internal const string FontPath = "Assets/art/UI/Fonts/PressStart2P SDF.asset";

    // Misma escala y resolución que los protagonistas (64 px por unidad, x3.2)
    internal const float CharacterScale = 3.2f;
    private const int PixelsPerUnit = 64;

    // Objetos del Piso 2 que no se usan en la Planta Baja
    private static readonly string[] RemovedObjects =
        { "Puerta_204", "Puerta_205", "Puerta_206", "Puerta_207", "Mural", "Escaleras" };

    [MenuItem("Tools/UNIFRANZ-DISORIENTED/Generar Hito 1 (Planta Baja)")]
    public static void Build()
    {
        PrepareSprites();

        // Copia limpia de Piso2 en cada ejecución
        // Si la escena ya existe se sobrescribe su contenido y se conserva su .meta (mismo
        // GUID), así regenerarla no cambia referencias ni ensucia los commits
        if (File.Exists(ScenePath))
        {
            File.Copy(SourceScenePath, ScenePath, true);
            AssetDatabase.ImportAsset(ScenePath, ImportAssetOptions.ForceUpdate);
        }
        else if (!AssetDatabase.CopyAsset(SourceScenePath, ScenePath))
        {
            Debug.LogError("[Hito1SceneBuilder] No se pudo copiar " + SourceScenePath);
            return;
        }
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        foreach (string name in RemovedObjects)
        {
            GameObject old = FindRoot(scene, name);
            if (old != null)
            {
                Object.DestroyImmediate(old);
            }
        }

        GameObject systems = FindRoot(scene, "Sistemas");
        GameObject player = FindRoot(scene, "Jugador");
        Transform ui = FindRoot(scene, "HUD").transform;
        float floorY = player.transform.position.y - 0.05f;

        // --- Escenario real de la sede La Paz (reemplaza el pasillo del Piso 2) ---
        // Planta Baja y Mezzanine se arman lado a lado en X; las gradas pasan de uno a otro.
        Object.DestroyImmediate(FindRoot(scene, "Escenario"));
        Floor pb = BuildFloor("PlantaBaja", PlantaBajaBlocks, 0f, floorY);
        Floor mz = BuildFloor("Mezzanine", MezzanineBlocks, MezzanineOffsetX, floorY);

        PolygonCollider2D pbBounds = FindRoot(scene, "LimitesCamara").GetComponent<PolygonCollider2D>();
        SetBounds(pbBounds, pb);
        PolygonCollider2D mzBounds = new GameObject("LimitesCamara_Mezzanine").AddComponent<PolygonCollider2D>();
        mzBounds.isTrigger = true;
        SetBounds(mzBounds, mz);
        GameObject vcamGo = FindRoot(scene, "CinemachineCamera");
        CinemachineCamera vcam = vcamGo.GetComponent<CinemachineCamera>();
        CinemachineConfiner2D confiner = vcamGo.GetComponent<CinemachineConfiner2D>();
        confiner.BoundingShape2D = pbBounds;

        // El jugador entra por la puerta principal del Hall
        player.transform.position = new Vector3(pb.X(0, 0.42f), floorY + 0.05f, 0f);

        // --- Estrés bajo para el primer hito ---
        StressSystem stress = systems.GetComponent<StressSystem>();
        SetFloat(stress, "passiveRate", 0.25f);
        SetFloat(stress, "sprintRate", 1f);

        // --- Audio: música, pasos y latidos ---
        AudioManager audio = systems.AddComponent<AudioManager>();
        Set(audio, "music", AssetDatabase.LoadAssetAtPath<AudioClip>(MusicPath));
        FootstepAudio footsteps = player.AddComponent<FootstepAudio>();
        SetArray(footsteps, "steps", Enumerable.Range(0, 5).Select(i => Sfx("footstep_concrete_00" + i)).ToArray());
        HeartbeatAudio heartbeat = systems.AddComponent<HeartbeatAudio>();
        Set(heartbeat, "lub", Sfx("impactSoft_heavy_000"));
        Set(heartbeat, "dub", Sfx("impactSoft_heavy_001"));

        // --- Checklist de Inducción ---
        ObjectiveTracker tracker = systems.AddComponent<ObjectiveTracker>();
        SetString(tracker, "milestoneTitle", "HITO 1");
        SetObjectives(tracker, new[]
        {
            ("guia", "Habla con la Guía de Bienestar en Recepción"),
            ("respiracion", "Practica la respiración con Lucía"),
            ("cartelera", "Busca tu código en Admisiones"),
            ("carnet", "Recoge tu carnet en Cajas (Mezzanine)"),
            ("molinete", "Pasa el molinete con tu carnet"),
            ("totem", "Activa el Tótem de la Cafetería"),
            ("companero", "Ayuda a Diego en el Hall"),
            ("docente", "Registra tu asistencia en el Auditorio")
        });
        Set(tracker, "objectiveCompletedClip", Sfx("confirmation_002"));
        Set(tracker, "milestoneCompletedClip", Sfx("confirmation_004"));

        TMP_Text objectiveText = FindDeep(ui, "Objetivo").GetComponent<TMP_Text>();
        objectiveText.richText = true;
        // Los fondos de la sede son claros: el objetivo va sobre una franja oscura y más ancha
        var objectiveRect = (RectTransform)objectiveText.transform;
        objectiveRect.sizeDelta = new Vector2(1700f, objectiveRect.sizeDelta.y);
        objectiveText.textWrappingMode = TextWrappingModes.NoWrap;
        var barGo = new GameObject("FondoObjetivo", typeof(RectTransform));
        barGo.transform.SetParent(ui, false);
        barGo.transform.SetSiblingIndex(objectiveText.transform.GetSiblingIndex());
        var bar = (RectTransform)barGo.transform;
        bar.anchorMin = bar.anchorMax = bar.pivot = new Vector2(0f, 1f);
        bar.anchoredPosition = objectiveRect.anchoredPosition + new Vector2(-16f, 12f);
        bar.sizeDelta = new Vector2(1730f, 50f);
        Image barImage = barGo.AddComponent<Image>();
        barImage.color = new Color(0.06f, 0.07f, 0.16f, 0.8f);
        barImage.raycastTarget = false;
        ObjectiveHUD hud = objectiveText.gameObject.AddComponent<ObjectiveHUD>();
        Set(hud, "tracker", tracker);
        Set(hud, "label", objectiveText);

        // --- Pantalla de victoria con paso al siguiente hito ---
        Transform win = FindDeep(ui, "Victoria");
        FindDeep(win, "Titulo").GetComponent<TMP_Text>().text = "HITO 1 COMPLETADO";
        FindDeep(win, "Mensaje").GetComponent<TMP_Text>().text =
            "Orientación lista. Tu clase te espera en el Piso 2.";
        Button next = CreateNextButton(win);
        LevelFlow flow = systems.GetComponent<LevelFlow>();
        SetString(flow, "nextScene", NextScene);
        Set(flow, "winNextButton", next);

        // --- Diálogos un poco más pausados: es el hito de introducción y se lee con calma ---
        SetFloat(FindDeep(ui, "CajaDialogo").GetComponent<DialogueBox>(), "charsPerSecond", 32f);

        // --- Minijuego de respiración y fundido para las gradas (debajo del menú de pausa) ---
        BreathingMinigame breathing = CreateBreathingMinigame(ui, systems, player);
        CanvasGroup fade = CreateFade(ui);

        // --- Contenido del nivel ---
        // Planta Baja: Hall -> Recepción (gradas al Mezzanine) -> Admisiones -> molinete ->
        // Cafetería (aglomeración, dispensador, tótem) -> Game Room -> Auditorio.
        // Mezzanine: ARCA y Cajas. Diego espera en el Hall: hay que volver por él.
        Transform content = new GameObject("Hito1_Contenido").transform;
        Transform t = player.transform;
        PlayerMovement movement = player.GetComponent<PlayerMovement>();

        // Hall Principal
        CreateNpc(content, "Companero", pb.X(0, 0.82f), floorY, t,
            "Diego - Compañero", "Hablar", "companero", -10f,
            new[]
            {
                "¡Volviste! ¿Ya sabes dónde queda Bienestar Estudiantil?",
                "(Le cuentas lo que viste en el tótem: Bienestar está en el piso superior.)",
                "¡Gracias! Ya me siento menos perdido. Nos vemos en clases."
            },
            new[]
            {
                "Oye... ¿tú también eres nuev@? Estoy perdido.",
                "Me dijeron que busque Bienestar Estudiantil, pero no sé dónde queda.",
                "(No sabes. Dicen que el tótem de la Cafetería, pasando el molinete, muestra todo el bloque.)"
            }, 3f,
            new[] { "¡Gracias otra vez! Voy a subir a Bienestar." },
            patrol: 1.2f);                                       // camina nervioso de un lado a otro

        // Recepción
        ObjectiveInteractable lucia = CreateNpc(content, "GuiaBienestar", pb.X(1, 0.32f), floorY, t,
            "Lucía - Guía de Bienestar", "Hablar", "guia", -10f,
            new[]
            {
                "¡Bienvenid@ a UNIFRANZ! Soy Lucía, de Bienestar Estudiantil.",
                "Hoy es tu primer día: completarás la Ruta de Inducción de la sede.",
                "Tu checklist está arriba en la pantalla. Síguelo paso a paso.",
                "Si te sientes abrumad@, fíjate en tu medidor de estrés, arriba a la izquierda.",
                "Antes de empezar, te enseño a respirar para calmarte. ¡Hagámoslo juntos!"
            },
            new string[0], 0f,
            new[] { "Si te estresas, vuelve conmigo y respiramos otra vez. ¡Vamos!" });
        // Al terminar su diálogo empieza el minijuego; después se puede repetir con ella
        UnityEditor.Events.UnityEventTools.AddPersistentListener(GetEvent(lucia, typeof(Interactable), "onFinished"), breathing.Begin);
        UnityEditor.Events.UnityEventTools.AddPersistentListener(GetEvent(lucia, typeof(ObjectiveInteractable), "onDoneFinished"), breathing.Begin);

        // Gradas de Recepción <-> Mezzanine
        Transform mzArrival = CreatePoint(content, "Llegada_Mezzanine", mz.X(0, 0.36f), floorY);
        Transform pbArrival = CreatePoint(content, "Llegada_PlantaBaja", pb.X(1, 0.80f), floorY);
        CreatePortal(content, "Gradas_Subir", pb.X(1, 0.86f), floorY, "Subir al Mezzanine",
            mzArrival, mzBounds, movement, vcam, confiner, fade);
        CreatePortal(content, "Gradas_Bajar", mz.X(0, 0.20f), floorY, "Bajar a Planta Baja",
            pbArrival, pbBounds, movement, vcam, confiner, fade);

        // Admisiones: cartelera de matriculados y molinete hacia el campus
        CreateCartelera(content, pb.X(2, 0.54f), floorY);
        CreateMolinete(content, pb, 2, floorY);

        // Cafetería: aglomeración, dispensador y tótem
        CreateCrowd(content, pb.X(3, 0.06f), pb.X(3, 0.58f), floorY, t);
        CreateRestPoint(content, pb.X(3, 0.66f), floorY);
        CreateTotem(content, pb.X(3, 0.86f), floorY);

        // Game Room
        CreateExtra(content, "Kevin", pb.X(4, 0.56f), floorY, t, false, "Kevin - Estudiante de 8vo semestre", -3f, new[]
        {
            "¡Hey, nuev@! Bienvenid@ al Game Room. Aquí se descansa entre clases.",
            "Un consejo: el tótem de la Cafetería muestra todo el bloque. Úsalo siempre.",
            "Y si ves mucha gente junta, respira. A todos nos pasó al principio."
        });

        // Auditorio: charla de bienvenida con el docente
        CreateNpc(content, "Docente", pb.X(5, 0.66f), floorY, t,
            "Lic. Ramírez - Docente", "Hablar", "docente", 0f,
            new[]
            {
                "Buenos días. Soy el Lic. Ramírez; la charla de bienvenida es aquí, en el Auditorio.",
                "Veo tu carnet provisional y tu checklist completo. Muy bien.",
                "Además ayudaste a un compañero. Eso también es parte de la vida universitaria.",
                "Firmo tu lista de asistencia. Bienvenid@ oficialmente a UNIFRANZ.",
                "Tu primera clase es en el Piso 2. ¡Nos vemos allá!"
            },
            new[]
            {
                "Buenos días. Antes de firmar tu asistencia, completa tu Checklist de Inducción.",
                "Revisa tu objetivo, arriba en la pantalla."
            }, 4f,
            new[] { "Ya firmé tu lista. ¡Nos vemos en el Piso 2!" });

        // Mezzanine: fila y ventanilla de Cajas
        CreateExtra(content, "Valeria", mz.X(1, 0.14f), floorY, t, false, "Valeria - Estudiante", -2f, new[]
        {
            "Esta fila de Cajas no avanza nunca...",
            "Ah, ¿también vienes por tu carnet? Te piden tu código de estudiante.",
            "Está en la cartelera de matriculados de Admisiones, en la Planta Baja."
        });
        CreateCajas(content, mz.X(1, 0.56f), floorY, t);                 // dentro de la Caja 2

        EditorSceneManager.SaveScene(scene);
        UpdateBuildSettings();
        PointMenuToHito1();
        AssetDatabase.SaveAssets();
        Debug.Log("[Hito1SceneBuilder] Listo: " + ScenePath);
    }

    // ================================================================
    // Pisos
    // ================================================================

    // Bloques de cada piso en orden (arte en Assets/Sprites/Escenario/<Piso>/)
    private static readonly string[] PlantaBajaBlocks = { "hall", "recepcion", "admisiones", "cafeteria", "game_room", "auditorio" };
    private static readonly string[] MezzanineBlocks = { "arca", "cajas" };

    // El Mezzanine se arma lejos en X para que nada de un piso alcance al otro
    private const float MezzanineOffsetX = 300f;
    private const float BlockHeight = 10f;

    // Posición de los bloques de un piso armado
    private class Floor
    {
        public float[] Left;
        public float[] Width;
        public float Start => Left[0];
        public float End => Left[Left.Length - 1] + Width[Width.Length - 1];

        // Coordenada x de un punto del bloque (fracción 0..1 de su ancho)
        public float X(int block, float fraction) => Left[block] + fraction * Width[block];
    }

    // Arma los bloques de un piso uno tras otro (el primero centrado en centerX),
    // cada uno con sus variantes de estrés y ansiedad, más el suelo y las paredes
    private static Floor BuildFloor(string floorName, string[] blocks, float centerX, float floorY)
    {
        var root = new GameObject("Escenario_" + floorName).transform;
        var floor = new Floor { Left = new float[blocks.Length], Width = new float[blocks.Length] };
        float cursor = 0f;
        for (int i = 0; i < blocks.Length; i++)
        {
            string basePath = "Assets/Sprites/Escenario/" + floorName + "/" + floorName.ToLower() + "_" + blocks[i];
            Sprite normal = LoadSprite(basePath + ".png");
            float scale = BlockHeight / normal.bounds.size.y;
            float width = normal.bounds.size.x * scale;
            if (i == 0)
            {
                cursor = centerX - width * 0.5f;
            }
            floor.Left[i] = cursor;
            floor.Width[i] = width;

            var block = new GameObject(blocks[i]);
            block.transform.SetParent(root);
            block.transform.position = new Vector3(cursor + width * 0.5f, 0f, 0f);
            CreateBackground("Normal", block.transform, normal, scale, -30, 1f);
            SpriteRenderer stressLayer = CreateBackground("Estres", block.transform, LoadSprite(basePath + "_estres.png"), scale, -29, 0f);
            SpriteRenderer anxietyLayer = CreateBackground("Ansiedad", block.transform, LoadSprite(basePath + "_ansiedad.png"), scale, -28, 0f);
            StressScenery scenery = block.AddComponent<StressScenery>();
            Set(scenery, "stressLayer", stressLayer);
            Set(scenery, "anxietyLayer", anxietyLayer);

            // Capa de frente opcional: se dibuja delante de los NPCs (orden 6), así un NPC
            // puede quedar detrás de un vidrio o un mostrador del fondo
            if (File.Exists(basePath + "_frente.png"))
            {
                var front = new GameObject("Frente");
                front.transform.SetParent(block.transform, false);
                CreateBackground("Normal", front.transform, LoadSprite(basePath + "_frente.png"), scale, 6, 1f);
                SpriteRenderer frontStress = CreateBackground("Estres", front.transform, LoadSprite(basePath + "_frente_estres.png"), scale, 7, 0f);
                SpriteRenderer frontAnxiety = CreateBackground("Ansiedad", front.transform, LoadSprite(basePath + "_frente_ansiedad.png"), scale, 8, 0f);
                StressScenery frontScenery = front.AddComponent<StressScenery>();
                Set(frontScenery, "stressLayer", frontStress);
                Set(frontScenery, "anxietyLayer", frontAnxiety);
            }
            cursor += width;
        }

        CreateBox("Suelo", root, new Vector2((floor.Start + floor.End) * 0.5f, floorY - 0.5f), new Vector2(floor.End - floor.Start + 2f, 1f));
        CreateBox("Pared_Izquierda", root, new Vector2(floor.Start - 0.5f, 0f), new Vector2(1f, 20f));
        CreateBox("Pared_Derecha", root, new Vector2(floor.End + 0.5f, 0f), new Vector2(1f, 20f));
        return floor;
    }

    private static SpriteRenderer CreateBackground(string name, Transform parent, Sprite sprite, float scale, int order, float alpha)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localScale = new Vector3(scale, scale, 1f);
        go.transform.localPosition = -sprite.bounds.center * scale;
        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = order;
        renderer.color = new Color(1f, 1f, 1f, alpha);
        return renderer;
    }

    private static void CreateBox(string name, Transform parent, Vector2 position, Vector2 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent);
        go.transform.position = position;
        go.AddComponent<BoxCollider2D>().size = size;
    }

    // Límites de cámara de un piso. Más altos que la vista para que el confiner no se
    // reduzca a un punto; el margen va arriba porque el jugador camina abajo.
    private static void SetBounds(PolygonCollider2D bounds, Floor floor)
    {
        float half = BlockHeight * 0.5f;
        bounds.points = new[]
        {
            new Vector2(floor.Start, -half), new Vector2(floor.End, -half),
            new Vector2(floor.End, half + 1f), new Vector2(floor.Start, half + 1f)
        };
    }

    private static Transform CreatePoint(Transform parent, string name, float x, float floorY)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent);
        go.transform.position = new Vector3(x, floorY + 0.05f, 0f);
        return go.transform;
    }

    // Gradas: interactuable que lleva al jugador al otro piso con un fundido
    private static void CreatePortal(Transform parent, string name, float x, float floorY, string prompt, Transform destination,
        PolygonCollider2D bounds, PlayerMovement player, CinemachineCamera vcam, CinemachineConfiner2D confiner, CanvasGroup fade)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent);
        go.transform.position = new Vector3(x, floorY, 0f);
        FloorPortal portal = go.AddComponent<FloorPortal>();
        ConfigureInteractable(portal, prompt, "", new string[0], 0f);
        Set(portal, "destination", destination);
        Set(portal, "destinationBounds", bounds);
        Set(portal, "player", player);
        Set(portal, "vcam", vcam);
        Set(portal, "confiner", confiner);
        Set(portal, "fade", fade);
        Set(portal, "travelClip", Sfx("footstep_concrete_002"));
    }

    // Capa negra a pantalla completa para el fundido de las gradas
    private static CanvasGroup CreateFade(Transform ui)
    {
        var go = new GameObject("Fundido", typeof(RectTransform));
        go.transform.SetParent(ui, false);
        var rect = (RectTransform)go.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        Image image = go.AddComponent<Image>();
        image.color = Color.black;
        image.raycastTarget = false;
        CanvasGroup group = go.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        group.blocksRaycasts = false;
        Transform pause = FindDeep(ui, "MenuPausa");
        if (pause != null)
        {
            rect.SetSiblingIndex(pause.GetSiblingIndex());
        }
        return group;
    }

    // ================================================================
    // Contenido
    // ================================================================

    // NPC con animación de respirar. lookAtPlayer: se gira hacia el jugador;
    // si no, mira a la derecha o a la izquierda (flip) todo el tiempo.
    // patrol > 0: camina esa distancia a cada lado de su posición
    internal static GameObject CreatePerson(Transform parent, string sheet, float x, float floorY, Transform player,
        bool lookAtPlayer = true, bool flip = false, int order = 5, float patrol = 0f)
    {
        Sprite[] frames = SheetFrames(NpcPath + sheet + "_Respirar.png");
        GameObject npc = CreateVisual(parent, sheet, frames[0], x, floorY, out SpriteRenderer renderer, order);
        renderer.flipX = flip;

        NpcAnimation animation = npc.AddComponent<NpcAnimation>();
        Set(animation, "sprite", renderer);
        SetArray(animation, "idleFrames", frames);
        SetArray(animation, "walkFrames", SheetFrames(NpcPath + sheet + "_Caminar.png"));
        Set(animation, "player", lookAtPlayer ? player : null);
        SetFloat(animation, "patrolDistance", patrol);
        return npc;
    }

    internal static ObjectiveInteractable CreateNpc(Transform parent, string sheet, float x, float floorY, Transform player,
        string speaker, string prompt, string objectiveId, float stressChange,
        string[] lines, string[] linesPending, float stressPending, string[] linesDone, float patrol = 0f)
    {
        GameObject npc = CreatePerson(parent, sheet, x, floorY, player, patrol: patrol);
        ObjectiveInteractable interactable = npc.AddComponent<ObjectiveInteractable>();
        ConfigureObjective(interactable, prompt, speaker, objectiveId, stressChange, lines, linesPending, stressPending, linesDone);
        return interactable;
    }

    internal static void ConfigureObjective(ObjectiveInteractable item, string prompt, string speaker, string objectiveId,
        float stressChange, string[] lines, string[] linesPending, float stressPending, string[] linesDone)
    {
        ConfigureInteractable(item, prompt, speaker, lines, stressChange);
        SetString(item, "objectiveId", objectiveId);
        SetStrings(item, "linesPending", linesPending);
        SetFloat(item, "stressPending", stressPending);
        SetStrings(item, "linesDone", linesDone);
        Set(item, "interactClip", Sfx("select_002"));
        Set(item, "pendingClip", Sfx("error_004"));
    }

    // NPC opcional: solo conversa (no es parte del Checklist)
    internal static void CreateExtra(Transform parent, string sheet, float x, float floorY, Transform player, bool flip,
        string speaker, float stressChange, string[] lines)
    {
        GameObject npc = CreatePerson(parent, sheet, x, floorY, player, true, flip);
        Interactable item = npc.AddComponent<Interactable>();
        ConfigureInteractable(item, "Hablar", speaker, lines, stressChange);
    }

    // La cajera atiende DENTRO de la Caja 2: se sube detrás del mostrador (la capa de
    // frente del fondo, con el vidrio y el mostrador, la tapa de la cintura para abajo)
    private const float CashierRise = 1.8f;

    private static void CreateCajas(Transform parent, float x, float floorY, Transform player)
    {
        GameObject cajera = CreatePerson(parent, "Cajera", x, floorY + CashierRise, player, true, false, 4);
        ObjectiveInteractable item = cajera.AddComponent<ObjectiveInteractable>();
        ConfigureObjective(item, "Hablar", "Sra. Rosa - Cajas", "carnet", -3f,
            new[]
            {
                "Buenos días, mi vida. ¿Tu código de estudiante?",
                "(Le dices el código que viste en la cartelera: 2026-0417.)",
                "Perfecto. Aquí tienes tu carnet provisional. ¡Que no se te pierda!",
                "Con él puedes pasar el molinete, al final de Admisiones en la Planta Baja."
            },
            new[]
            {
                "Buenos días. Para tu carnet necesito tu código de estudiante.",
                "Lo encuentras en la cartelera de matriculados de Admisiones, en la Planta Baja."
            }, 2f,
            new[] { "Ese carnet te sirve hasta que llegue el definitivo." });
        // El aviso [E] queda sobre la ventanilla y no fuera de la pantalla
        SetVector(item, "promptOffset", new Vector2(0f, 3.5f));
    }

    // Cartelera de matriculados: es parte del arte del pasillo, aquí solo va el interactuable
    private static void CreateCartelera(Transform parent, float x, float floorY)
    {
        var go = new GameObject("CarteleraMatriculados");
        go.transform.SetParent(parent);
        go.transform.position = new Vector3(x, floorY, 0f);
        ObjectiveInteractable item = go.AddComponent<ObjectiveInteractable>();
        ConfigureObjective(item, "Revisar", "Cartelera de matriculados", "cartelera", -2f,
            new[]
            {
                "Listas de matriculados de primer semestre... buscas tu nombre.",
                "¡Ahí está! Código de estudiante: 2026-0417.",
                "Ahora sube al Mezzanine por las gradas de Recepción y recoge tu carnet en Cajas."
            },
            new[]
            {
                "Una cartelera llena de listas, horarios y avisos.",
                "(Todavía no sabes qué buscar aquí. Sigue tu checklist.)"
            }, 2f,
            new[] { "Tu código es 2026-0417. Ya lo tienes anotado." });
    }

    // Fila de molinetes sobre la junta diagonal entre Admisiones (madera) y la Cafetería:
    // separa las dos áreas y bloquea el paso hasta pasar el carnet. El arte va en dos
    // capas a la escala del fondo: la parte detrás del jugador y la parte delante de él.
    private const string TurnstilePath = "Assets/Sprites/Escenario/PlantaBaja/molinetes_";

    private static void CreateMolinete(Transform parent, Floor pb, int block, float floorY)
    {
        var root = new GameObject("FilaMolinetes");
        root.transform.SetParent(parent);
        root.transform.position = new Vector3(pb.X(block, 0.5f), 0f, 0f);

        Sprite backClosed = LoadSprite(TurnstilePath + "atras_cerrado.png");
        float scale = BlockHeight / backClosed.bounds.size.y;
        SpriteRenderer back = CreateBackground("Atras", root.transform, backClosed, scale, 5, 1f);
        SpriteRenderer front = CreateBackground("Adelante", root.transform, LoadSprite(TurnstilePath + "adelante_cerrado.png"), scale, 12, 1f);

        // Interactuable y bloqueo en la punta de los brazos, a la altura de los pies del jugador
        var reader = new GameObject("Lector");
        reader.transform.SetParent(root.transform);
        reader.transform.position = new Vector3(pb.X(block, 0.875f), floorY, 0f);
        ObjectiveInteractable item = reader.AddComponent<ObjectiveInteractable>();
        ConfigureObjective(item, "Pasar carnet", "", "molinete", -2f,
            new[]
            {
                "Pasas tu carnet provisional por el lector... ¡beep!",
                "La luz se pone verde y el molinete gira. ¡Ya puedes pasar!"
            },
            new[] { "El molinete no gira. Necesitas tu carnet de estudiante para pasar." }, 3f,
            new[] { "Los molinetes ya están abiertos." });
        Set(item, "interactClip", Sfx("confirmation_002"));
        SetFloat(item, "range", 2.5f);

        var blockerGo = new GameObject("Bloqueo");
        blockerGo.transform.SetParent(root.transform);
        blockerGo.transform.position = new Vector3(pb.X(block, 0.885f), 5f, 0f);
        BoxCollider2D blocker = blockerGo.AddComponent<BoxCollider2D>();
        blocker.size = new Vector2(0.4f, 20f);

        TurnstileGate gate = root.AddComponent<TurnstileGate>();
        Set(gate, "blocker", blocker);
        SetArray(gate, "visuals", new Object[] { back, front });
        SetArray(gate, "openSprites", new Object[]
        {
            LoadSprite(TurnstilePath + "atras_abierto.png"), LoadSprite(TurnstilePath + "adelante_abierto.png")
        });
        StressTint tint = root.AddComponent<StressTint>();
        SetArray(tint, "targets", new Object[] { back, front });
    }

    // Aglomeración de estudiantes conversando entre ellos; uno responde si le hablas
    internal static void CreateCrowd(Transform parent, float start, float end, float floorY, Transform player)
    {
        var zoneGo = new GameObject("ZonaAglomeracion");
        zoneGo.transform.SetParent(parent);
        StressZone zone = zoneGo.AddComponent<StressZone>();
        Set(zone, "player", player);
        SetFloat(zone, "minX", start);
        SetFloat(zone, "maxX", end);
        SetFloat(zone, "stressPerSecond", 2.5f);

        float w = end - start;
        CreatePerson(parent, "Marco", start + w * 0.25f, floorY, player, false, false, 6, 1.5f);  // se abre paso entre la gente
        CreatePerson(parent, "Sofia", start + w * 0.45f, floorY, player, false, true, 4);
        GameObject andres = CreatePerson(parent, "Andres", start + w * 0.75f, floorY, player, true, false, 6);
        Interactable item = andres.AddComponent<Interactable>();
        ConfigureInteractable(item, "Hablar", "Andrés - Estudiante", new[]
        {
            "¡Uf, cuánta gente! Siempre es así la primera semana.",
            "Mejor no te quedes parado aquí en medio, que te aplastan."
        }, 3f);
    }

    // Panel del minijuego: capa oscura, círculo que respira e instrucciones
    private static BreathingMinigame CreateBreathingMinigame(Transform ui, GameObject systems, GameObject player)
    {
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        var panelGo = new GameObject("MinijuegoRespiracion", typeof(RectTransform));
        panelGo.transform.SetParent(ui, false);
        var panel = (RectTransform)panelGo.transform;
        panel.anchorMin = Vector2.zero;
        panel.anchorMax = Vector2.one;
        panel.offsetMin = panel.offsetMax = Vector2.zero;
        panelGo.AddComponent<Image>().color = new Color(0.04f, 0.05f, 0.12f, 0.82f);

        // Debajo del menú de pausa para que la pausa quede encima
        Transform pause = FindDeep(ui, "MenuPausa");
        if (pause != null)
        {
            panel.SetSiblingIndex(pause.GetSiblingIndex());
        }

        var circleGo = new GameObject("Circulo", typeof(RectTransform));
        circleGo.transform.SetParent(panel, false);
        var circle = (RectTransform)circleGo.transform;
        circle.anchorMin = circle.anchorMax = circle.pivot = new Vector2(0.5f, 0.5f);
        circle.anchoredPosition = new Vector2(0f, 30f);
        circle.sizeDelta = new Vector2(420f, 420f);
        Image circleImage = circleGo.AddComponent<Image>();
        circleImage.sprite = LoadSprite("Assets/Sprites/UI/Minijuegos/circulo_respiracion.png");
        circleImage.raycastTarget = false;

        CreateLabel(panel, "Titulo", font, "RESPIRA CON LUCÍA", 40f, new Color(0.976f, 0.435f, 0.075f), 420f);
        TextMeshProUGUI instruction = CreateLabel(panel, "Instruccion", font, "", 24f, new Color(0.92f, 0.92f, 0.95f), -300f);
        instruction.rectTransform.sizeDelta = new Vector2(1600f, 90f);
        instruction.lineSpacing = 25f;
        TextMeshProUGUI progress = CreateLabel(panel, "Progreso", font, "", 20f, new Color(0.976f, 0.435f, 0.075f), -380f);

        BreathingMinigame minigame = systems.AddComponent<BreathingMinigame>();
        Set(minigame, "panel", panelGo);
        Set(minigame, "circle", circle);
        Set(minigame, "circleImage", circleImage);
        Set(minigame, "instruction", instruction);
        Set(minigame, "progress", progress);
        Set(minigame, "player", player.GetComponent<PlayerMovement>());
        Set(minigame, "interactor", player.GetComponent<PlayerInteractor>());
        Set(minigame, "phaseClip", Sfx("select_002"));
        Set(minigame, "finishClip", Sfx("confirmation_004"));
        panelGo.SetActive(false);
        return minigame;
    }

    internal static TextMeshProUGUI CreateLabel(Transform parent, string name, TMP_FontAsset font, string text, float size,
        Color color, float y)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rect = (RectTransform)go.transform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, y);
        rect.sizeDelta = new Vector2(1600f, 60f);
        TextMeshProUGUI label = go.AddComponent<TextMeshProUGUI>();
        label.font = font;
        label.text = text;
        label.fontSize = size;
        label.color = color;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
        return label;
    }

    // Evento UnityEvent privado de un componente (para conectar listeners persistentes)
    internal static UnityEngine.Events.UnityEvent GetEvent(Object target, System.Type type, string field)
    {
        return (UnityEngine.Events.UnityEvent)type
            .GetField(field, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .GetValue(target);
    }

    private static void CreateTotem(Transform parent, float x, float floorY)
    {
        Sprite off = AssetDatabase.LoadAssetAtPath<Sprite>(PropsPath + "totem_orientacion_apagado.png");
        Sprite on = AssetDatabase.LoadAssetAtPath<Sprite>(PropsPath + "totem_orientacion_activo.png");
        GameObject totem = CreateVisual(parent, "TotemOrientacion", off, x, floorY, out SpriteRenderer renderer);

        ObjectiveInteractable interactable = totem.AddComponent<ObjectiveInteractable>();
        ConfigureInteractable(interactable, "Activar", "Tótem de Orientación", new[]
        {
            "Tocas la pantalla. El mapa del Bloque A se enciende.",
            "Subsuelo: Sala de Anatomía. Planta Baja: Recepción, Admisiones, Cafetería y Auditorio. Mezzanine: Cajas.",
            "Piso 1 y 2: laboratorios y aulas. Piso 5: Biblioteca. Piso superior: Bienestar Estudiantil.",
            "El tótem marca tu ruta. Ahora sabes cómo moverte por el bloque."
        }, -5f);
        SetString(interactable, "objectiveId", "totem");
        SetStrings(interactable, "linesPending", new[]
        {
            "La pantalla está apagada y no sabes cómo usarla.",
            "(Primero termina lo anterior de tu checklist.)"
        });
        SetFloat(interactable, "stressPending", 2f);
        SetStrings(interactable, "linesDone", new[] { "El mapa sigue encendido. Bienestar está en el piso superior." });
        Set(interactable, "visual", renderer);
        Set(interactable, "doneSprite", on);
        Set(interactable, "interactClip", Sfx("bong_001"));
        Set(interactable, "pendingClip", Sfx("error_004"));
    }

    internal static void CreateRestPoint(Transform parent, float x, float floorY)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(PropsPath + "dispensador_agua.png");
        GameObject dispenser = CreateVisual(parent, "DispensadorAgua", sprite, x, floorY, out _);

        RestPoint rest = dispenser.AddComponent<RestPoint>();
        ConfigureInteractable(rest, "Beber agua", "", new[]
        {
            "Tomas un vaso de agua fría y respiras hondo.",
            "Te sientes un poco más tranquil@."
        }, -15f);
        SetFloat(rest, "cooldown", 25f);
        SetStrings(rest, "linesCooldown", new[] { "Acabas de tomar agua. Mejor sigue con tu ruta." });
        Set(rest, "useClip", Sfx("drop_002"));
    }

    // Objeto con su sprite en un hijo escalado como los personajes (pivote en los pies)
    internal static GameObject CreateVisual(Transform parent, string name, Sprite sprite, float x, float floorY,
        out SpriteRenderer renderer, int order = 5)
    {
        var root = new GameObject(name);
        root.transform.SetParent(parent);
        root.transform.position = new Vector3(x, floorY, 0f);

        var visual = new GameObject("Sprite");
        visual.transform.SetParent(root.transform, false);
        visual.transform.localScale = Vector3.one * CharacterScale;
        renderer = visual.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        // Detrás del jugador (10) y delante del escenario
        renderer.sortingOrder = order;
        return root;
    }

    internal static void ConfigureInteractable(Interactable item, string prompt, string speaker, string[] lines, float stressChange)
    {
        SetString(item, "prompt", prompt);
        SetString(item, "speaker", speaker);
        SetStrings(item, "lines", lines);
        SetFloat(item, "stressChange", stressChange);
    }

    // Botón "CONTINUAR" sobre el de volver al menú, con el arte de los botones del juego
    private static Button CreateNextButton(Transform winPanel)
    {
        RectTransform menu = (RectTransform)FindDeep(winPanel, "Btn_VolverMenu");
        var go = new GameObject("Btn_Continuar", typeof(RectTransform));
        go.transform.SetParent(winPanel, false);
        var rect = (RectTransform)go.transform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = menu.sizeDelta;
        rect.anchoredPosition = menu.anchoredPosition;
        menu.anchoredPosition -= new Vector2(0f, 130f);

        Image image = go.AddComponent<Image>();
        image.sprite = LoadSprite(ButtonsPath + "normal.png");
        Button button = go.AddComponent<Button>();
        button.transition = Selectable.Transition.SpriteSwap;
        button.spriteState = new SpriteState
        {
            highlightedSprite = LoadSprite(ButtonsPath + "hover.png"),
            pressedSprite = LoadSprite(ButtonsPath + "presionado.png"),
            selectedSprite = LoadSprite(ButtonsPath + "seleccionado.png")
        };

        var labelGo = new GameObject("Texto", typeof(RectTransform));
        labelGo.transform.SetParent(go.transform, false);
        var labelRect = (RectTransform)labelGo.transform;
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;
        TextMeshProUGUI label = labelGo.AddComponent<TextMeshProUGUI>();
        label.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        label.text = "CONTINUAR";
        label.fontSize = 22f;
        label.color = new Color(0.92f, 0.92f, 0.95f);
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
        return button;
    }

    // ================================================================
    // Escenas
    // ================================================================

    // Orden: menú, Hito 1, Piso 2 y luego el resto que ya hubiera
    private static void UpdateBuildSettings()
    {
        var scenes = new List<EditorBuildSettingsScene>
        {
            new EditorBuildSettingsScene(MenuScenePath, true),
            new EditorBuildSettingsScene(ScenePath, true),
            new EditorBuildSettingsScene(SourceScenePath, true)
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

    // Al elegir personaje en el menú, el juego empieza en el Hito 1
    private static void PointMenuToHito1()
    {
        Scene menu = EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
        CharacterSelectPanel select = menu.GetRootGameObjects()
            .Select(r => r.GetComponentInChildren<CharacterSelectPanel>(true))
            .FirstOrDefault(c => c != null);
        if (select == null)
        {
            Debug.LogError("[Hito1SceneBuilder] No se encontró CharacterSelectPanel en el menú");
            return;
        }
        SetString(select, "gameScene", Path.GetFileNameWithoutExtension(ScenePath));
        EditorSceneManager.SaveScene(menu);
    }

    // ================================================================
    // Sprites
    // ================================================================

    // Importa los sprites de NPCs y objetos como pixel art (64 px/unidad, pivote en los pies)
    internal static void PrepareSprites()
    {
        string[] sheets = { "GuiaBienestar", "Companero", "Docente", "Cajera", "Valeria", "Kevin", "Marco", "Sofia", "Andres" };
        foreach (string sheet in sheets)
        {
            ImportPixelArt(NpcPath + sheet + "_Respirar.png", 8);
            ImportPixelArt(NpcPath + sheet + "_Caminar.png", 8);
        }
        string[] props =
        {
            "totem_orientacion_apagado", "totem_orientacion_activo", "dispensador_agua",
        };
        foreach (string prop in props)
        {
            ImportPixelArt(PropsPath + prop + ".png", 1);
        }
        ImportPixelArt("Assets/Sprites/UI/Minijuegos/circulo_respiracion.png", 1);
        foreach (string part in new[] { "atras_cerrado", "atras_abierto", "adelante_cerrado", "adelante_abierto" })
        {
            ImportBackground(TurnstilePath + part + ".png");
        }

        // Fondos de los pisos: un sprite centrado por imagen, sin compresión
        foreach (var (floorName, blocks) in new[] { ("PlantaBaja", PlantaBajaBlocks), ("Mezzanine", MezzanineBlocks) })
        {
            foreach (string block in blocks)
            {
                foreach (string suffix in new[] { "", "_estres", "_ansiedad" })
                {
                    string basePath = "Assets/Sprites/Escenario/" + floorName + "/" + floorName.ToLower() + "_" + block;
                    ImportBackground(basePath + suffix + ".png");
                    // Capa de frente opcional (p. ej. vidrio y mostrador de la Caja 2)
                    if (File.Exists(basePath + "_frente" + suffix + ".png"))
                    {
                        ImportBackground(basePath + "_frente" + suffix + ".png");
                    }
                }
            }
        }
    }

    private static void ImportBackground(string path)
    {
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        if (importer == null)
        {
            Debug.LogError("[Hito1SceneBuilder] Falta el fondo " + path);
            return;
        }
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 100;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.maxTextureSize = 2048;
        importer.SaveAndReimport();
    }

    internal static void ImportPixelArt(string path, int frames)
    {
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        if (importer == null)
        {
            Debug.LogError("[Hito1SceneBuilder] Falta la imagen " + path);
            return;
        }
        importer.textureType = TextureImporterType.Sprite;
        importer.spritePixelsPerUnit = PixelsPerUnit;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;

        if (frames == 1)
        {
            importer.spriteImportMode = SpriteImportMode.Single;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.BottomCenter;
            settings.spritePivot = new Vector2(0.5f, 0f);
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
            return;
        }

        // Hoja de varios cuadros: el corte se escribe por el data provider del paquete 2D Sprite
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.SaveAndReimport();
        var factory = new SpriteDataProviderFactories();
        factory.Init();
        ISpriteEditorDataProvider provider = factory.GetSpriteEditorDataProviderFromObject(importer);
        provider.InitSpriteEditorDataProvider();
        provider.GetDataProvider<ITextureDataProvider>().GetTextureActualWidthAndHeight(out int textureWidth, out int textureHeight);
        int width = textureWidth / frames;
        string baseName = Path.GetFileNameWithoutExtension(path);
        // Se reutilizan los IDs de un corte anterior para que el .meta no cambie en cada ejecución
        SpriteRect[] previous = provider.GetSpriteRects();
        provider.SetSpriteRects(Enumerable.Range(0, frames).Select(i => new SpriteRect
        {
            name = baseName + "_" + i,
            rect = new Rect(i * width, 0, width, textureHeight),
            alignment = SpriteAlignment.BottomCenter,
            pivot = new Vector2(0.5f, 0f),
            spriteID = previous.FirstOrDefault(r => r.name == baseName + "_" + i)?.spriteID ?? GUID.Generate()
        }).ToArray());
        provider.Apply();
        importer.SaveAndReimport();
    }

    internal static Sprite[] SheetFrames(string path)
    {
        return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>()
            .OrderBy(s => int.Parse(s.name.Substring(s.name.LastIndexOf('_') + 1))).ToArray();
    }

    internal static Sprite LoadSprite(string path)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null)
        {
            Debug.LogError("[Hito1SceneBuilder] Falta el sprite " + path);
        }
        return sprite;
    }

    internal static AudioClip Sfx(string name)
    {
        AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(SfxPath + name + ".ogg");
        if (clip == null)
        {
            Debug.LogError("[Hito1SceneBuilder] Falta el sonido " + name);
        }
        return clip;
    }

    // ================================================================
    // Utilidades
    // ================================================================

    internal static GameObject FindRoot(Scene scene, string name)
    {
        return scene.GetRootGameObjects().FirstOrDefault(r => r.name == name);
    }

    internal static Transform FindDeep(Transform parent, string name)
    {
        return parent.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == name);
    }

    internal static void SetObjectives(ObjectiveTracker tracker, (string id, string description)[] objectives)
    {
        var so = new SerializedObject(tracker);
        SerializedProperty array = so.FindProperty("objectives");
        array.arraySize = objectives.Length;
        for (int i = 0; i < objectives.Length; i++)
        {
            SerializedProperty item = array.GetArrayElementAtIndex(i);
            item.FindPropertyRelative("id").stringValue = objectives[i].id;
            item.FindPropertyRelative("description").stringValue = objectives[i].description;
        }
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // Asignan campos privados [SerializeField] desde el Editor
    internal static SerializedProperty Property(SerializedObject so, Object target, string field)
    {
        SerializedProperty property = so.FindProperty(field);
        if (property == null)
        {
            Debug.LogError($"[Hito1SceneBuilder] {target.GetType().Name} no tiene el campo {field}");
        }
        return property;
    }

    internal static void Set(Object target, string field, Object value)
    {
        var so = new SerializedObject(target);
        SerializedProperty property = Property(so, target, field);
        if (property != null)
        {
            property.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    internal static void SetFloat(Object target, string field, float value)
    {
        var so = new SerializedObject(target);
        SerializedProperty property = Property(so, target, field);
        if (property != null)
        {
            property.floatValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    internal static void SetVector(Object target, string field, Vector2 value)
    {
        var so = new SerializedObject(target);
        SerializedProperty property = Property(so, target, field);
        if (property != null)
        {
            property.vector2Value = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    internal static void SetBool(Object target, string field, bool value)
    {
        var so = new SerializedObject(target);
        SerializedProperty property = Property(so, target, field);
        if (property != null)
        {
            property.boolValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    internal static void SetString(Object target, string field, string value)
    {
        var so = new SerializedObject(target);
        SerializedProperty property = Property(so, target, field);
        if (property != null)
        {
            property.stringValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    internal static void SetStrings(Object target, string field, string[] values)
    {
        var so = new SerializedObject(target);
        SerializedProperty array = Property(so, target, field);
        if (array == null)
        {
            return;
        }
        array.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++)
        {
            array.GetArrayElementAtIndex(i).stringValue = values[i];
        }
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    internal static void SetArray(Object target, string field, Object[] values)
    {
        var so = new SerializedObject(target);
        SerializedProperty array = Property(so, target, field);
        if (array == null)
        {
            return;
        }
        array.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++)
        {
            array.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
