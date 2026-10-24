using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Arma el juego jugable con los assets que ya hay en el repo:
//  - Nivel Piso 2: pasillo (bloques izquierdo, central y derecho con sus variantes
//    de estrés y ansiedad), el personaje elegido, puertas, mural, cámara, HUD de
//    ansiedad, diálogos, menú de pausa y pantallas de final.
//  - Menú principal: conecta los botones (Jugar, Opciones, Créditos, Salir) y arma la
//    pantalla de elección de personaje que abre Jugar.
// Menú: Tools > UNIFRANZ-DISORIENTED. En modo batch:
//   Unity -batchmode -quit -projectPath . -executeMethod GameSceneBuilder.BuildAll
public static class GameSceneBuilder
{
    // --- Rutas ---
    private const string LevelScenePath = "Assets/Scenes/Piso2.unity";
    private const string MenuScenePath = "Assets/Scenes/MainMenu.unity";
    private const string EscenarioPath = "Assets/Sprites/Escenario/Piso_2/";
    private const string PersonajesPath = "Assets/Sprites/Personajes/";
    private const string MenusPath = "Assets/Sprites/UI/Menus/";
    private const string HudPath = "Assets/Sprites/UI/HUD/Ansiedad/";
    private const string FontPath = "Assets/art/UI/Fonts/PressStart2P SDF.asset";

    // --- Medidas del nivel (unidades de mundo) ---
    // Cada bloque mide 10 de alto; su ancho sale de la proporción de su arte
    // (los laterales 1376x768, el central más ancho: 1511x768)
    private const float BlockHeight = 10f;
    // Línea de los pies: 78 % desde arriba del arte, dentro del piso en perspectiva
    private const float FloorY = BlockHeight * 0.5f - BlockHeight * 0.78f;
    private const float CharacterScale = 3.2f;

    // --- Paleta de la UI ---
    private static readonly Color Orange = new Color(0.976f, 0.435f, 0.075f);
    private static readonly Color TextLight = new Color(0.92f, 0.92f, 0.95f);
    // Opaco: con transparencia el Outline naranja se ve a través y lo tiñe
    private static readonly Color PanelColor = new Color(0.06f, 0.07f, 0.16f, 1f);

    // Personajes jugables, en el orden de la pantalla de elección
    private static readonly string[] Characters = { "Protagonista", "Personaje2" };

    private static TMP_FontAsset font;

    [MenuItem("Tools/UNIFRANZ-DISORIENTED/Generar Nivel Piso 2 y conectar Menú")]
    public static void BuildAll()
    {
        BuildLevel();
        WireMainMenu();
        UpdateBuildSettings();
        AssetDatabase.SaveAssets();
        Debug.Log("[GameSceneBuilder] Listo: " + LevelScenePath + " y " + MenuScenePath);
    }

    // ================================================================
    // Nivel Piso 2
    // ================================================================

    private static void BuildLevel()
    {
        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        SliceSheets();

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // --- Luz global para el renderer 2D de URP ---
        var lightGo = new GameObject("Global Light 2D");
        Light2D light = lightGo.AddComponent<Light2D>();
        light.lightType = Light2D.LightType.Global;
        light.intensity = 1f;

        // --- Sistema de estrés ---
        var systems = new GameObject("Sistemas");
        StressSystem stress = systems.AddComponent<StressSystem>();

        // --- Escenario: tres bloques seguidos, el primero centrado en x = 0 ---
        var scenery = new GameObject("Escenario");
        string[] blocks = { "bloque_izquierdo", "bloque_central", "bloque_derecho" };
        float[] blockLeft = new float[blocks.Length];
        float[] blockWidth = new float[blocks.Length];
        float cursor = -BlockWorldWidth(blocks[0]) * 0.5f;
        for (int i = 0; i < blocks.Length; i++)
        {
            blockWidth[i] = BlockWorldWidth(blocks[i]);
            blockLeft[i] = cursor;
            CreateBlock(blocks[i], scenery.transform, cursor + blockWidth[i] * 0.5f);
            cursor += blockWidth[i];
        }
        float left = blockLeft[0];
        float right = cursor;

        // Suelo y paredes invisibles en los extremos del pasillo
        CreateBox("Suelo", scenery.transform, new Vector2((left + right) * 0.5f, FloorY - 0.5f), new Vector2(right - left + 2f, 1f));
        CreateBox("Pared_Izquierda", scenery.transform, new Vector2(left - 0.5f, 0f), new Vector2(1f, 20f));
        CreateBox("Pared_Derecha", scenery.transform, new Vector2(right + 0.5f, 0f), new Vector2(1f, 20f));

        // --- Jugador: el personaje elegido en el menú, con sus hojas de estrés y ansiedad ---
        GameObject player = CreateCharacter("Jugador", PersonajesPath + "Protagonista/Protagonista_Respirar.png",
            "Assets/Animaciones/Protagonista/Protagonista.controller", new Vector2(-3f, FloorY + 0.05f), 10, out SpriteRenderer playerSprite, out Animator playerAnimator);
        CreatePlayerCharacter(player, playerAnimator, playerSprite);
        player.AddComponent<Rigidbody2D>();
        CapsuleCollider2D capsule = player.AddComponent<CapsuleCollider2D>();
        capsule.size = new Vector2(0.9f, 3f);
        capsule.offset = new Vector2(0f, 1.52f);
        PlayerMovement movement = player.AddComponent<PlayerMovement>();
        PlayerAnimation playerAnimation = player.AddComponent<PlayerAnimation>();
        SetField(playerAnimation, "animator", playerAnimator);
        SetField(playerAnimation, "sprite", playerSprite);
        PlayerInteractor interactor = player.AddComponent<PlayerInteractor>();
        SetField(stress, "player", movement);

        // --- Cámara con Cinemachine limitada al pasillo ---
        CreateCamera(player.transform, left, right);

        // --- UI ---
        var eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();

        Canvas canvas = CreateCanvas("HUD");
        Transform ui = canvas.transform;

        // HUD de ansiedad: cara arriba y barra debajo, en la esquina superior izquierda
        Image face = CreateImage("Cara", ui, LoadSprite(HudPath + "cara_normal.png"));
        TopLeft(face.rectTransform, new Vector2(40f, -30f), new Vector2(80f, 80f));
        face.gameObject.AddComponent<Animator>().runtimeAnimatorController =
            AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Animaciones/HUD/CaraNivel.controller");
        Image bar = CreateImage("Barra", ui, LoadSprite(HudPath + "barra_normal.png"));
        TopLeft(bar.rectTransform, new Vector2(40f, -125f), new Vector2(80f, 320f));
        bar.gameObject.AddComponent<Animator>().runtimeAnimatorController =
            AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Animaciones/HUD/BarraNivel.controller");
        StressHUD hud = canvas.gameObject.AddComponent<StressHUD>();
        SetField(hud, "bar", bar.GetComponent<Animator>());
        SetField(hud, "face", face.GetComponent<Animator>());

        // Objetivo del hito
        TextMeshProUGUI objective = CreateText("Objetivo", ui, "HITO 1  Encuentra tu aula: 207", 22f, TextLight, TextAlignmentOptions.TopLeft);
        TopLeft(objective.rectTransform, new Vector2(150f, -50f), new Vector2(1200f, 40f));
        AddShadow(objective.gameObject);

        // Aviso de interacción que sigue al objeto
        RectTransform promptRect = CreateRect("AvisoInteraccion", ui);
        promptRect.sizeDelta = new Vector2(400f, 40f);
        TextMeshProUGUI promptText = CreateText("Texto", promptRect, "[E]", 20f, Orange, TextAlignmentOptions.Center);
        Stretch(promptText.rectTransform);
        AddShadow(promptText.gameObject);
        InteractionPrompt prompt = promptRect.gameObject.AddComponent<InteractionPrompt>();
        SetField(prompt, "label", promptText);
        SetField(interactor, "prompt", prompt);

        DialogueBox dialogue = CreateDialogueBox(ui, movement);

        // --- Puertas y mural (los dos personajes son jugables: no hay NPC) ---
        CreateDoor("Puerta_204", BlockX(0, 0.15f), 10f, new[] { "Aula 204. Hay una clase a medio empezar.", "Todos te miran. No es tu aula." });
        CreateDoor("Puerta_205", BlockX(0, 0.79f), 8f, new[] { "Aula 205. Está cerrada con llave.", "Sientes que se te hace tarde." });
        CreateDoor("Puerta_206", BlockX(1, 0.13f), 6f, new[] { "Aula 206. Un laboratorio vacío.", "Casi. Tu aula debería estar cerca." });
        CreateDoor("Mural", BlockX(1, 0.86f), -20f, new[]
        {
            "Mural de avisos: \"Inducción primer semestre, Aula 207\".",
            "Es la puerta de al lado. Respira, vas bien."
        });
        CreateDoor("Escaleras", BlockX(2, 0.86f), 5f, new[] { "Las gradas suben al piso 3.", "Todavía no tienes nada que hacer allá arriba." });
        Interactable goal = CreateDoor("Puerta_207", BlockX(1, 0.66f), 0f, new[] { "Aula 207. Tu nombre está en la lista de la puerta.", "Llegaste a tu primera clase." });

        // --- Pantallas de final y flujo del nivel ---
        LevelFlow flow = systems.AddComponent<LevelFlow>();
        SetField(flow, "player", movement);
        GameObject win = CreateEndPanel(ui, "Victoria", "HITO 1 COMPLETADO", "Encontraste tu aula. La Ruta de Inducción continúa...",
            Orange, false, out Button winMenu, out _);
        GameObject lose = CreateEndPanel(ui, "CrisisAnsiedad", "CRISIS DE ANSIEDAD", "Todo se volvió demasiado. Respira e inténtalo otra vez.",
            new Color(0.85f, 0.15f, 0.15f), true, out Button loseMenu, out Button retry);
        SetField(flow, "winPanel", win);
        SetField(flow, "winMenuButton", winMenu);
        SetField(flow, "losePanel", lose);
        SetField(flow, "loseMenuButton", loseMenu);
        SetField(flow, "retryButton", retry);
        UnityEventTools.AddPersistentListener(GetEvent(goal), flow.CompleteMilestone);

        // --- Menú de pausa (encima de todo) ---
        CreatePauseMenu(ui);

        EditorSceneManager.SaveScene(scene, LevelScenePath);

        // Coordenada x de un punto del bloque (fracción 0..1 de su ancho)
        float BlockX(int block, float fraction) => blockLeft[block] + fraction * blockWidth[block];

        // Puerta: un interactuable invisible frente a ella
        Interactable CreateDoor(string name, float x, float stressChange, string[] lines)
        {
            var go = new GameObject(name);
            go.transform.position = new Vector3(x, FloorY, 0f);
            return CreateInteractable(go, "Revisar", "", stressChange, lines);
        }
    }

    // Ancho en el mundo de un bloque: BlockHeight de alto con la proporción de su arte
    private static float BlockWorldWidth(string block)
    {
        string path = EscenarioPath + block + "/pasillo_piso2_" + block + ".ase";
        Sprite sprite = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().First();
        return BlockHeight * sprite.bounds.size.x / sprite.bounds.size.y;
    }

    // Opciones de personaje jugable: Animator y cuadros normales, de estrés y de ansiedad
    private static void CreatePlayerCharacter(GameObject player, Animator animator, SpriteRenderer sprite)
    {
        PlayerCharacter character = player.AddComponent<PlayerCharacter>();
        var so = new SerializedObject(character);
        so.FindProperty("animator").objectReferenceValue = animator;
        so.FindProperty("sprite").objectReferenceValue = sprite;
        SerializedProperty options = so.FindProperty("options");
        options.arraySize = Characters.Length;
        for (int i = 0; i < Characters.Length; i++)
        {
            string name = Characters[i];
            SerializedProperty option = options.GetArrayElementAtIndex(i);
            option.FindPropertyRelative("name").stringValue = "Personaje " + (i + 1);
            option.FindPropertyRelative("controller").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Animaciones/" + name + "/" + name + ".controller");
            SetSprites(option.FindPropertyRelative("normal"), CharacterFrames(name, ""));
            SetSprites(option.FindPropertyRelative("stress"), CharacterFrames(name, "_Estres"));
            SetSprites(option.FindPropertyRelative("anxiety"), CharacterFrames(name, "_Ansiedad"));
        }
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // Respirar (0-7) y caminar (8-15) de un personaje, en el orden de la hoja
    private static Sprite[] CharacterFrames(string character, string suffix)
    {
        return SheetFrames(PersonajesPath + character + "/" + character + "_Respirar" + suffix + ".png")
            .Concat(SheetFrames(PersonajesPath + character + "/" + character + "_Caminar" + suffix + ".png")).ToArray();
    }

    private static Sprite[] SheetFrames(string path)
    {
        Sprite[] frames = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>()
            .OrderBy(s => int.Parse(s.name.Substring(s.name.LastIndexOf('_') + 1))).ToArray();
        if (frames.Length != 8)
        {
            Debug.LogError("[GameSceneBuilder] La hoja " + path + " tiene " + frames.Length + " cuadros (se esperaban 8)");
        }
        return frames;
    }

    private static void SetSprites(SerializedProperty array, Sprite[] sprites)
    {
        array.arraySize = sprites.Length;
        for (int i = 0; i < sprites.Length; i++)
        {
            array.GetArrayElementAtIndex(i).objectReferenceValue = sprites[i];
        }
    }

    // Un bloque del pasillo: la versión normal y encima las de estrés y ansiedad
    private static void CreateBlock(string block, Transform parent, float centerX)
    {
        var root = new GameObject(ToTitle(block));
        root.transform.SetParent(parent);
        root.transform.position = new Vector3(centerX, 0f, 0f);

        string basePath = EscenarioPath + block + "/pasillo_piso2_" + block;
        CreateBackground("Normal", root.transform, basePath + ".ase", -30, 1f);
        SpriteRenderer stressLayer = CreateBackground("Estres", root.transform, basePath + "_estres.ase", -29, 0f);
        SpriteRenderer anxietyLayer = CreateBackground("Ansiedad", root.transform, basePath + "_ansiedad.ase", -28, 0f);

        StressScenery stressScenery = root.AddComponent<StressScenery>();
        SetField(stressScenery, "stressLayer", stressLayer);
        SetField(stressScenery, "anxietyLayer", anxietyLayer);
    }

    // Fondo escalado para medir BlockHeight de alto y centrado en el bloque
    private static SpriteRenderer CreateBackground(string name, Transform parent, string asePath, int order, float alpha)
    {
        Sprite sprite = AssetDatabase.LoadAllAssetsAtPath(asePath).OfType<Sprite>().FirstOrDefault();
        if (sprite == null)
        {
            Debug.LogError("[GameSceneBuilder] No se encontró el sprite de " + asePath);
            return null;
        }

        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        float scale = BlockHeight / sprite.bounds.size.y;
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

    // Personaje con el sprite y Animator en un hijo escalado (pivote en los pies)
    private static GameObject CreateCharacter(string name, string sheetPath, string controllerPath, Vector2 position, int order,
        out SpriteRenderer sprite, out Animator animator)
    {
        var root = new GameObject(name);
        root.transform.position = position;

        var visual = new GameObject("Sprite");
        visual.transform.SetParent(root.transform, false);
        visual.transform.localScale = Vector3.one * CharacterScale;
        sprite = visual.AddComponent<SpriteRenderer>();
        sprite.sprite = AssetDatabase.LoadAllAssetsAtPath(sheetPath).OfType<Sprite>().OrderBy(s => s.name).FirstOrDefault();
        sprite.sortingOrder = order;
        animator = visual.AddComponent<Animator>();
        animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(controllerPath);
        return root;
    }

    private static Interactable CreateInteractable(GameObject go, string prompt, string speaker, float stressChange, string[] lines)
    {
        Interactable item = go.AddComponent<Interactable>();
        var so = new SerializedObject(item);
        so.FindProperty("prompt").stringValue = prompt;
        so.FindProperty("speaker").stringValue = speaker;
        so.FindProperty("stressChange").floatValue = stressChange;
        SerializedProperty array = so.FindProperty("lines");
        array.arraySize = lines.Length;
        for (int i = 0; i < lines.Length; i++)
        {
            array.GetArrayElementAtIndex(i).stringValue = lines[i];
        }
        so.ApplyModifiedPropertiesWithoutUndo();
        return item;
    }

    private static UnityEngine.Events.UnityEvent GetEvent(Interactable item)
    {
        return (UnityEngine.Events.UnityEvent)typeof(Interactable)
            .GetField("onFinished", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .GetValue(item);
    }

    private static void CreateCamera(Transform target, float left, float right)
    {
        var camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        camGo.transform.position = new Vector3(0f, 0f, -10f);
        Camera cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = BlockHeight * 0.5f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        camGo.AddComponent<UniversalAdditionalCameraData>();
        camGo.AddComponent<CinemachineBrain>();

        // Límites del pasillo para el confiner
        var bounds = new GameObject("LimitesCamara");
        PolygonCollider2D polygon = bounds.AddComponent<PolygonCollider2D>();
        polygon.isTrigger = true;
        // Más alto que la vista: si mide lo mismo, el confiner lo reduce a un punto y la
        // cámara se queda quieta. El margen va arriba porque el jugador camina abajo,
        // así la cámara se apoya en el borde inferior y nunca muestra fuera del arte.
        float half = BlockHeight * 0.5f;
        polygon.points = new[]
        {
            new Vector2(left, -half), new Vector2(right, -half),
            new Vector2(right, half + 1f), new Vector2(left, half + 1f)
        };

        var vcamGo = new GameObject("CinemachineCamera");
        CinemachineCamera vcam = vcamGo.AddComponent<CinemachineCamera>();
        vcam.Follow = target;
        LensSettings lens = LensSettings.Default;
        lens.OrthographicSize = BlockHeight * 0.5f;
        lens.NearClipPlane = 0.3f;
        lens.FarClipPlane = 1000f;
        vcam.Lens = lens;
        CinemachinePositionComposer composer = vcamGo.AddComponent<CinemachinePositionComposer>();
        composer.CameraDistance = 10f;
        composer.Damping = new Vector3(0.6f, 0.6f, 0f);
        CinemachineConfiner2D confiner = vcamGo.AddComponent<CinemachineConfiner2D>();
        confiner.BoundingShape2D = polygon;
    }

    private static DialogueBox CreateDialogueBox(Transform ui, PlayerMovement player)
    {
        RectTransform panel = CreateRect("Dialogo", ui);
        panel.anchorMin = new Vector2(0.5f, 0f);
        panel.anchorMax = new Vector2(0.5f, 0f);
        panel.pivot = new Vector2(0.5f, 0f);
        panel.anchoredPosition = new Vector2(0f, 40f);
        panel.sizeDelta = new Vector2(1500f, 230f);
        Image background = panel.gameObject.AddComponent<Image>();
        background.color = PanelColor;
        Outline outline = panel.gameObject.AddComponent<Outline>();
        outline.effectColor = Orange;
        outline.effectDistance = new Vector2(4f, -4f);

        TextMeshProUGUI speaker = CreateText("Nombre", panel, "", 22f, Orange, TextAlignmentOptions.TopLeft);
        TopLeft(speaker.rectTransform, new Vector2(40f, -28f), new Vector2(1400f, 30f));
        TextMeshProUGUI body = CreateText("Texto", panel, "", 22f, TextLight, TextAlignmentOptions.TopLeft);
        TopLeft(body.rectTransform, new Vector2(40f, -78f), new Vector2(1420f, 110f));
        body.lineSpacing = 30f;
        TextMeshProUGUI hint = CreateText("Continuar", panel, "E >", 16f, Orange, TextAlignmentOptions.BottomRight);
        hint.rectTransform.anchorMin = hint.rectTransform.anchorMax = hint.rectTransform.pivot = new Vector2(1f, 0f);
        hint.rectTransform.anchoredPosition = new Vector2(-30f, 22f);
        hint.rectTransform.sizeDelta = new Vector2(200f, 24f);

        var holder = new GameObject("CajaDialogo");
        holder.transform.SetParent(ui, false);
        DialogueBox box = holder.AddComponent<DialogueBox>();
        SetField(box, "panel", panel.gameObject);
        SetField(box, "speakerText", speaker);
        SetField(box, "bodyText", body);
        SetField(box, "player", player);
        return box;
    }

    // Pantalla de final: capa oscura, título, mensaje y botones
    private static GameObject CreateEndPanel(Transform ui, string name, string title, string message, Color titleColor,
        bool withRetry, out Button menuButton, out Button retryButton)
    {
        RectTransform root = CreateRect(name, ui);
        Stretch(root);
        CreateImage("CapaOscura", root, LoadSprite(MenusPath + "Pausa/capa_oscura.png"), true);
        Image dim = CreateImage("Oscurecer", root, null);
        Stretch(dim.rectTransform);
        dim.color = new Color(0f, 0f, 0f, 0.55f);

        TextMeshProUGUI titleText = CreateText("Titulo", root, title, 56f, titleColor, TextAlignmentOptions.Center);
        Centered(titleText.rectTransform, new Vector2(0f, 220f), new Vector2(1700f, 80f));
        AddShadow(titleText.gameObject);
        TextMeshProUGUI messageText = CreateText("Mensaje", root, message, 22f, TextLight, TextAlignmentOptions.Center);
        Centered(messageText.rectTransform, new Vector2(0f, 110f), new Vector2(1500f, 80f));

        retryButton = null;
        float y = -40f;
        if (withRetry)
        {
            retryButton = CreateSpriteButton("Btn_Reintentar", root, "vacio", "REINTENTAR");
            Centered((RectTransform)retryButton.transform, new Vector2(0f, y), new Vector2(416f, 108f));
            y -= 130f;
        }
        menuButton = CreateSpriteButton("Btn_VolverMenu", root, "volver_menu", null);
        Centered((RectTransform)menuButton.transform, new Vector2(0f, y), new Vector2(416f, 108f));
        return root.gameObject;
    }

    private static void CreatePauseMenu(Transform ui)
    {
        RectTransform root = CreateRect("MenuPausa", ui);
        Stretch(root);
        CreateImage("CapaOscura", root, LoadSprite(MenusPath + "Pausa/capa_oscura.png"), true);

        // Laptop de pausa con sus tres botones
        RectTransform page = CreateLaptop("LaptopPausa", root, MenusPath + "Pausa/laptop_pausa_hoja_8cuadros_x4.png");
        Button resume = CreateSpriteButton("Btn_Reanudar", page, "reanudar", null);
        LaptopPosition((RectTransform)resume.transform, new Vector2(408f, 240f), new Vector2(416f, 108f));
        Button settingsButton = CreateSpriteButton("Btn_Configuracion", page, "configuracion", null);
        LaptopPosition((RectTransform)settingsButton.transform, new Vector2(408f, 360f), new Vector2(416f, 108f));
        Button menu = CreateSpriteButton("Btn_VolverMenu", page, "volver_menu", null);
        LaptopPosition((RectTransform)menu.transform, new Vector2(408f, 480f), new Vector2(416f, 108f));

        SettingsPanel settings = CreateSettingsPanel(root);

        PauseMenu pause = ui.gameObject.AddComponent<PauseMenu>();
        SetField(pause, "root", root.gameObject);
        SetField(pause, "pausePage", page.gameObject);
        SetField(pause, "settings", settings);
        SetField(pause, "resumeButton", resume);
        SetField(pause, "settingsButton", settingsButton);
        SetField(pause, "menuButton", menu);
    }

    // Laptop de Configuración: íconos sobre los dibujados, sliders y botón Atrás
    private static SettingsPanel CreateSettingsPanel(Transform parent)
    {
        string path = MenusPath + "Pausa/Configuracion/";
        RectTransform page = CreateLaptop("LaptopConfiguracion", parent, path + "laptop_configuracion_hoja_8cuadros_x4.png");

        Image musicIcon = CreateImage("IconoMusica", page, LoadSprite(path + "icono_musica_activo.png"));
        LaptopPosition(musicIcon.rectTransform, new Vector2(168f + 30f, 188f + 28f), new Vector2(60f, 56f));
        Image effectsIcon = CreateImage("IconoEfectos", page, LoadSprite(path + "icono_efectos_activo.png"));
        LaptopPosition(effectsIcon.rectTransform, new Vector2(168f + 30f, 316f + 28f), new Vector2(60f, 56f));

        Slider music = CreateSlider("SliderMusica", page, path, new Vector2(408f, 275f), out Image musicFill);
        Slider effects = CreateSlider("SliderEfectos", page, path, new Vector2(408f, 403f), out Image effectsFill);

        Button back = CreateSpriteButton("Btn_Atras", page, "atras", null);
        LaptopPosition((RectTransform)back.transform, new Vector2(408f, 515f), new Vector2(416f, 108f));

        SettingsPanel panel = page.gameObject.AddComponent<SettingsPanel>();
        SetField(panel, "musicSlider", music);
        SetField(panel, "effectsSlider", effects);
        SetField(panel, "musicFill", musicFill);
        SetField(panel, "effectsFill", effectsFill);
        SetField(panel, "musicIcon", musicIcon);
        SetField(panel, "effectsIcon", effectsIcon);
        SetField(panel, "musicOn", LoadSprite(path + "icono_musica_activo.png"));
        SetField(panel, "musicOff", LoadSprite(path + "icono_musica_silencio.png"));
        SetField(panel, "effectsOn", LoadSprite(path + "icono_efectos_activo.png"));
        SetField(panel, "effectsOff", LoadSprite(path + "icono_efectos_silencio.png"));
        SetField(panel, "backButton", back);
        page.gameObject.SetActive(false);
        return panel;
    }

    private static Slider CreateSlider(string name, Transform parent, string path, Vector2 center, out Image fill)
    {
        RectTransform root = CreateRect(name, parent);
        LaptopPosition(root, center, new Vector2(480f, 40f));
        Image background = CreateImage("Fondo", root, LoadSprite(path + "slider_fondo.png"));
        Stretch(background.rectTransform);

        // El relleno se recorta (Filled) en vez de estirarse
        fill = CreateImage("Relleno", root, LoadSprite(path + "slider_relleno.png"));
        Stretch(fill.rectTransform);
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = (int)Image.OriginHorizontal.Left;

        RectTransform area = CreateRect("ZonaPerilla", root);
        Stretch(area);
        area.offsetMin = new Vector2(20f, 0f);
        area.offsetMax = new Vector2(-20f, 0f);
        Image handle = CreateImage("Perilla", area, LoadSprite(path + "slider_perilla_normal.png"));
        handle.rectTransform.sizeDelta = new Vector2(40f, 24f);

        Slider slider = root.gameObject.AddComponent<Slider>();
        slider.handleRect = handle.rectTransform;
        slider.targetGraphic = handle;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.transition = Selectable.Transition.SpriteSwap;
        slider.spriteState = new SpriteState
        {
            highlightedSprite = LoadSprite(path + "slider_perilla_hover.png"),
            selectedSprite = LoadSprite(path + "slider_perilla_hover.png"),
            pressedSprite = LoadSprite(path + "slider_perilla_presionado.png")
        };
        return slider;
    }

    // Laptop animada (8 cuadros) centrada en pantalla, a tamaño real del arte x4
    private static RectTransform CreateLaptop(string name, Transform parent, string sheetPath)
    {
        Sprite[] frames = AssetDatabase.LoadAllAssetsAtPath(sheetPath).OfType<Sprite>()
            .OrderBy(s => int.Parse(s.name.Substring(s.name.LastIndexOf('_') + 1))).ToArray();
        Image image = CreateImage(name, parent, frames[0]);
        Centered(image.rectTransform, Vector2.zero, new Vector2(816f, 792f));
        UISpriteAnimation animation = image.gameObject.AddComponent<UISpriteAnimation>();
        var so = new SerializedObject(animation);
        SerializedProperty array = so.FindProperty("frames");
        array.arraySize = frames.Length;
        for (int i = 0; i < frames.Length; i++)
        {
            array.GetArrayElementAtIndex(i).objectReferenceValue = frames[i];
        }
        so.ApplyModifiedPropertiesWithoutUndo();
        return image.rectTransform;
    }

    // Coloca un hijo de la laptop usando coordenadas en píxeles del arte (origen arriba a la izquierda)
    private static void LaptopPosition(RectTransform rect, Vector2 center, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(center.x, -center.y);
        rect.sizeDelta = size;
    }

    // Botón con los 4 estados del arte (normal, hover, presionado, seleccionado)
    private static Button CreateSpriteButton(string name, Transform parent, string art, string label)
    {
        string basePath = MenusPath + "Botones/boton_" + art + "_";
        Image image = CreateImage(name, parent, LoadSprite(basePath + "normal.png"), true);
        Button button = image.gameObject.AddComponent<Button>();
        button.transition = Selectable.Transition.SpriteSwap;
        button.spriteState = new SpriteState
        {
            highlightedSprite = LoadSprite(basePath + "hover.png"),
            pressedSprite = LoadSprite(basePath + "presionado.png"),
            selectedSprite = LoadSprite(basePath + "seleccionado.png")
        };
        if (label != null)
        {
            TextMeshProUGUI text = CreateText("Texto", image.rectTransform, label, 22f, TextLight, TextAlignmentOptions.Center);
            Stretch(text.rectTransform);
        }
        return button;
    }

    // ================================================================
    // Menú principal
    // ================================================================

    private static void WireMainMenu()
    {
        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        Scene scene = EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
        GameObject[] roots = scene.GetRootGameObjects();
        Canvas canvas = roots.Select(r => r.GetComponentInChildren<Canvas>(true)).First(c => c != null);
        Transform ui = canvas.transform;

        // Quitar lo generado en una ejecución anterior
        foreach (string old in new[] { "Paneles", "MainMenuController" })
        {
            Transform found = FindDeep(ui, old);
            if (found != null)
            {
                Object.DestroyImmediate(found.gameObject);
            }
        }

        Button play = FindDeep(ui, "Btn_Jugar").GetComponent<Button>();
        Button options = FindDeep(ui, "Btn_Opciones").GetComponent<Button>();
        Button credits = FindDeep(ui, "Btn_Creditos").GetComponent<Button>();
        Button quit = FindDeep(ui, "Btn_Salir").GetComponent<Button>();
        GameObject buttons = play.transform.parent.gameObject;
        // Sin ?? : GetComponent devuelve un null falso de Unity en el Editor
        CanvasGroup group = buttons.GetComponent<CanvasGroup>();
        if (group == null)
        {
            group = buttons.AddComponent<CanvasGroup>();
        }

        // Capa con los paneles de Opciones y Créditos
        RectTransform overlay = CreateRect("Paneles", ui);
        Stretch(overlay);
        CreateImage("CapaOscura", overlay, LoadSprite(MenusPath + "Pausa/capa_oscura.png"), true);
        SettingsPanel settings = CreateSettingsPanel(overlay);
        GameObject creditsPanel = CreateCredits(overlay, out Button creditsBack);
        CharacterSelectPanel characterSelect = CreateCharacterSelect(overlay);

        var controllerGo = new GameObject("MainMenuController");
        controllerGo.transform.SetParent(ui, false);
        MainMenuController controller = controllerGo.AddComponent<MainMenuController>();
        SetField(controller, "playButton", play);
        SetField(controller, "optionsButton", options);
        SetField(controller, "creditsButton", credits);
        SetField(controller, "quitButton", quit);
        SetField(controller, "menuGroup", group);
        SetField(controller, "overlay", overlay.gameObject);
        SetField(controller, "settings", settings);
        SetField(controller, "characterSelect", characterSelect);
        SetField(controller, "credits", creditsPanel);
        SetField(controller, "creditsBackButton", creditsBack);

        EditorSceneManager.SaveScene(scene);
    }

    // "Elige tu personaje": una tarjeta por personaje con su vista animada y su botón
    private static CharacterSelectPanel CreateCharacterSelect(Transform parent)
    {
        RectTransform panel = CreateRect("ElegirPersonaje", parent);
        Centered(panel, Vector2.zero, new Vector2(1300f, 800f));
        Image background = panel.gameObject.AddComponent<Image>();
        background.color = PanelColor;
        Outline outline = panel.gameObject.AddComponent<Outline>();
        outline.effectColor = Orange;
        outline.effectDistance = new Vector2(4f, -4f);

        TextMeshProUGUI title = CreateText("Titulo", panel, "ELIGE TU PERSONAJE", 40f, Orange, TextAlignmentOptions.Center);
        Centered(title.rectTransform, new Vector2(0f, 320f), new Vector2(1200f, 60f));

        CharacterSelectPanel select = panel.gameObject.AddComponent<CharacterSelectPanel>();
        var so = new SerializedObject(select);
        SerializedProperty cards = so.FindProperty("cards");
        cards.arraySize = Characters.Length;
        for (int i = 0; i < Characters.Length; i++)
        {
            float x = (i - (Characters.Length - 1) * 0.5f) * 520f;
            string name = Characters[i];
            Sprite[] idle = SheetFrames(PersonajesPath + name + "/" + name + "_Respirar.png");
            Sprite[] walk = SheetFrames(PersonajesPath + name + "/" + name + "_Caminar.png");

            Image preview = CreateImage("Vista_" + (i + 1), panel, idle[0]);
            Centered(preview.rectTransform, new Vector2(x, 60f), new Vector2(384f, 384f));
            preview.preserveAspect = true;
            Button button = CreateSpriteButton("Btn_Personaje" + (i + 1), panel, "vacio", "PERSONAJE " + (i + 1));
            Centered((RectTransform)button.transform, new Vector2(x, -190f), new Vector2(416f, 108f));

            SerializedProperty card = cards.GetArrayElementAtIndex(i);
            card.FindPropertyRelative("button").objectReferenceValue = button;
            card.FindPropertyRelative("preview").objectReferenceValue = preview;
            SetSprites(card.FindPropertyRelative("idle"), idle);
            SetSprites(card.FindPropertyRelative("walk"), walk);
        }
        Button back = CreateSpriteButton("Btn_Atras", panel, "atras", null);
        Centered((RectTransform)back.transform, new Vector2(0f, -320f), new Vector2(416f, 108f));
        so.FindProperty("backButton").objectReferenceValue = back;
        so.ApplyModifiedPropertiesWithoutUndo();
        return select;
    }

    private static GameObject CreateCredits(Transform parent, out Button back)
    {
        RectTransform panel = CreateRect("Creditos", parent);
        Centered(panel, Vector2.zero, new Vector2(1300f, 760f));
        Image background = panel.gameObject.AddComponent<Image>();
        background.color = PanelColor;
        Outline outline = panel.gameObject.AddComponent<Outline>();
        outline.effectColor = Orange;
        outline.effectDistance = new Vector2(4f, -4f);

        TextMeshProUGUI title = CreateText("Titulo", panel, "CRÉDITOS", 40f, Orange, TextAlignmentOptions.Center);
        Centered(title.rectTransform, new Vector2(0f, 300f), new Vector2(1200f, 60f));
        string team =
            "<color=#F96F13>Alvaro Del Carpio</color>\nSistemas Core\n\n" +
            "<color=#F96F13>Carlos Valverde</color>\nGameplay\n\n" +
            "<color=#F96F13>Eduardo Condoreno</color>\nArte 2D y UI/UX\n\n" +
            "<color=#F96F13>Jhoel Herrera</color>\nNiveles, integración y QA";
        TextMeshProUGUI body = CreateText("Equipo", panel, team, 20f, TextLight, TextAlignmentOptions.Center);
        Centered(body.rectTransform, new Vector2(0f, 20f), new Vector2(1200f, 460f));
        body.lineSpacing = 20f;

        back = CreateSpriteButton("Btn_Atras", panel, "atras", null);
        Centered((RectTransform)back.transform, new Vector2(0f, -300f), new Vector2(416f, 108f));
        return panel.gameObject;
    }

    // ================================================================
    // Utilidades
    // ================================================================

    private static void UpdateBuildSettings()
    {
        var scenes = new List<EditorBuildSettingsScene>
        {
            new EditorBuildSettingsScene(MenuScenePath, true),
            new EditorBuildSettingsScene(LevelScenePath, true)
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

    // Corta en 8 cuadros las hojas de la laptop si todavía no están cortadas
    private static void SliceSheets()
    {
        string[] sheets =
        {
            MenusPath + "Pausa/laptop_pausa_hoja_8cuadros_x4.png",
            MenusPath + "Pausa/Configuracion/laptop_configuracion_hoja_8cuadros_x4.png"
        };
        foreach (string path in sheets)
        {
            if (AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().Count() == 8)
            {
                continue;
            }
            // Con el paquete 2D Sprite instalado, el corte se escribe por su data provider
            // (TextureImporter.spritesheet se ignora)
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.spriteImportMode = SpriteImportMode.Multiple;
            var factory = new SpriteDataProviderFactories();
            factory.Init();
            ISpriteEditorDataProvider provider = factory.GetSpriteEditorDataProviderFromObject(importer);
            provider.InitSpriteEditorDataProvider();
            provider.GetDataProvider<ITextureDataProvider>().GetTextureActualWidthAndHeight(out int textureWidth, out int textureHeight);
            int width = textureWidth / 8;
            string baseName = Path.GetFileNameWithoutExtension(path);
            provider.SetSpriteRects(Enumerable.Range(0, 8).Select(i => new SpriteRect
            {
                name = baseName + "_" + i,
                rect = new Rect(i * width, 0, width, textureHeight),
                alignment = SpriteAlignment.Center,
                pivot = new Vector2(0.5f, 0.5f),
                spriteID = GUID.Generate()
            }).ToArray());
            provider.Apply();
            importer.SaveAndReimport();
        }
    }

    private static Canvas CreateCanvas(string name)
    {
        var go = new GameObject(name);
        Canvas canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        go.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    private static RectTransform CreateRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    private static Image CreateImage(string name, Transform parent, Sprite sprite, bool raycast = false)
    {
        RectTransform rect = CreateRect(name, parent);
        Image image = rect.gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.raycastTarget = raycast;
        if (sprite != null && name == "CapaOscura")
        {
            Stretch(rect);
        }
        return image;
    }

    private static TextMeshProUGUI CreateText(string name, Transform parent, string text, float size, Color color,
        TextAlignmentOptions alignment)
    {
        RectTransform rect = CreateRect(name, parent);
        TextMeshProUGUI tmp = rect.gameObject.AddComponent<TextMeshProUGUI>();
        tmp.font = font;
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.alignment = alignment;
        tmp.raycastTarget = false;
        return tmp;
    }

    private static void AddShadow(GameObject go)
    {
        Shadow shadow = go.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.85f);
        shadow.effectDistance = new Vector2(3f, -3f);
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void TopLeft(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static void Centered(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static Sprite LoadSprite(string path)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null)
        {
            Debug.LogError("[GameSceneBuilder] Falta el sprite " + path);
        }
        return sprite;
    }

    private static Transform FindDeep(Transform parent, string name)
    {
        return parent.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == name);
    }

    private static string ToTitle(string block)
    {
        return string.Join("_", block.Split('_').Select(w => char.ToUpper(w[0]) + w.Substring(1)));
    }

    // Asigna un campo privado [SerializeField] desde el Editor
    private static void SetField(Object target, string field, Object value)
    {
        var so = new SerializedObject(target);
        SerializedProperty property = so.FindProperty(field);
        if (property == null)
        {
            Debug.LogError($"[GameSceneBuilder] {target.GetType().Name} no tiene el campo {field}");
            return;
        }
        property.objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
