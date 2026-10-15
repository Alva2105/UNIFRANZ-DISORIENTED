using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

// Herramienta de Editor que construye la escena del Menú Principal completa:
// cámara, EventSystem (Input System nuevo), Canvas, fondo, logo, título y botones.
// Se ejecuta desde: Tools > UNIFRANZ-DISORIENTED > Generar Menú Principal.
public static class MainMenuSceneBuilder
{
    // --- Rutas de assets y de la escena ---
    private const string ScenePath = "Assets/Scenes/MainMenu.unity";
    private const string BackgroundPath = "Assets/Art/UI/MainMenu/FondoMenuPrincipal.png";
    private const string LogoPath = "Assets/Art/UI/MainMenu/LogoUnifranz.png";
    private const string GradientPath = "Assets/Art/UI/MainMenu/PanelDegradado.png";
    private const string FontPath = "Assets/Art/UI/Fonts/PressStart2P-Regular.ttf";
    private const string FontAssetPath = "Assets/Art/UI/Fonts/PressStart2P SDF.asset";

    // Caracteres que se pre-cargan en el atlas de la fuente (ASCII + español)
    private const string FontCharacters =
        " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~" +
        "áéíóúÁÉÍÓÚñÑüÜ¿¡";

    // --- Paleta del menú (naranja institucional UNIFRANZ) ---
    private static readonly Color Orange = new Color(0.976f, 0.435f, 0.075f);
    private static readonly Color TextLight = new Color(0.85f, 0.85f, 0.85f);
    private static readonly Color TextDim = new Color(0.6f, 0.6f, 0.62f);

    [MenuItem("Tools/UNIFRANZ-DISORIENTED/Generar Menú Principal")]
    public static void Build()
    {
        // Confirmar antes de sobrescribir una escena existente
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null &&
            !EditorUtility.DisplayDialog("Generar Menú Principal",
                $"Ya existe {ScenePath}. ¿Sobrescribirla?", "Sobrescribir", "Cancelar"))
        {
            return;
        }

        // Guardar cambios pendientes de la escena abierta antes de cambiar de escena
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }

        // TextMeshPro necesita sus recursos esenciales importados
        if (TMP_Settings.instance == null)
        {
            EditorUtility.DisplayDialog("Generar Menú Principal",
                "Faltan los recursos de TextMeshPro.\n\nImpórtalos desde Window > TextMeshPro > " +
                "Import TMP Essential Resources y vuelve a ejecutar esta herramienta.", "OK");
            return;
        }

        // --- Assets: configuración de importación y fuente ---
        Sprite background = ImportSprite(BackgroundPath, FilterMode.Point);
        Sprite logo = ImportSprite(LogoPath, FilterMode.Bilinear);
        Sprite gradient = ImportSprite(GradientPath, FilterMode.Bilinear);
        TMP_FontAsset font = GetOrCreateFontAsset();
        if (background == null || logo == null || gradient == null || font == null)
        {
            EditorUtility.DisplayDialog("Generar Menú Principal",
                "No se encontraron todos los assets en Assets/Art/UI. Revisa la consola.", "OK");
            return;
        }

        // --- Escena nueva y vacía ---
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        CreateCamera();
        Canvas canvas = CreateCanvas();
        Transform root = canvas.transform;

        // --- Fondo: cubre toda la pantalla sin deformarse ---
        RectTransform bg = CreateRect("Background", root);
        Stretch(bg);
        Image bgImage = bg.gameObject.AddComponent<Image>();
        bgImage.sprite = background;
        bgImage.raycastTarget = false;
        AspectRatioFitter fitter = bg.gameObject.AddComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        fitter.aspectRatio = background.rect.width / background.rect.height;

        // --- Parpadeo de luz sobre el fondo ---
        RectTransform flicker = CreateRect("LightFlicker", root);
        Stretch(flicker);
        Image flickerImage = flicker.gameObject.AddComponent<Image>();
        flickerImage.color = new Color(0f, 0f, 0f, 0f);
        flickerImage.raycastTarget = false;
        MenuFlicker flickerFx = flicker.gameObject.AddComponent<MenuFlicker>();
        SetField(flickerFx, "target", flickerImage);

        // --- Panel lateral oscuro con degradado para que el texto se lea ---
        RectTransform panel = CreateRect("SidePanel", root);
        panel.anchorMin = new Vector2(0f, 0f);
        panel.anchorMax = new Vector2(0f, 1f);
        panel.pivot = new Vector2(0f, 0.5f);
        panel.sizeDelta = new Vector2(1100f, 0f);
        panel.anchoredPosition = Vector2.zero;
        Image panelImage = panel.gameObject.AddComponent<Image>();
        panelImage.sprite = gradient;
        panelImage.raycastTarget = false;

        // --- Logo de la universidad ---
        RectTransform logoRect = CreateRect("Logo", panel);
        TopLeft(logoRect, new Vector2(110f, -80f), new Vector2(420f, 177f));
        Image logoImage = logoRect.gameObject.AddComponent<Image>();
        logoImage.sprite = logo;
        logoImage.preserveAspect = true;
        logoImage.raycastTarget = false;

        // --- Título con sombra y efecto glitch ---
        RectTransform titleGroup = CreateRect("TitleGroup", panel);
        TopLeft(titleGroup, new Vector2(110f, -300f), new Vector2(800f, 180f));
        TextMeshProUGUI shadow = CreateText("TitleShadow", titleGroup, "UNIFRANZ\nDISORIENTED", font, 64f,
            new Color(0.12f, 0.02f, 0.02f, 0.9f), TextAlignmentOptions.TopLeft);
        Stretch(shadow.rectTransform);
        shadow.rectTransform.anchoredPosition = new Vector2(6f, -6f);
        TextMeshProUGUI title = CreateText("Title", titleGroup, "UNIFRANZ\nDISORIENTED", font, 64f,
            Orange, TextAlignmentOptions.TopLeft);
        Stretch(title.rectTransform);
        shadow.lineSpacing = title.lineSpacing = 20f;
        TitleGlitch glitch = titleGroup.gameObject.AddComponent<TitleGlitch>();
        SetField(glitch, "target", titleGroup);
        SetField(glitch, "text", title);

        // --- Subtítulo ---
        TextMeshProUGUI subtitle = CreateText("Subtitle", panel, "Tu primer día en el Bloque A", font, 18f,
            TextLight, TextAlignmentOptions.TopLeft);
        TopLeft(subtitle.rectTransform, new Vector2(114f, -500f), new Vector2(800f, 40f));

        // --- Contenedor de botones en columna ---
        RectTransform buttons = CreateRect("Buttons", panel);
        TopLeft(buttons, new Vector2(100f, -580f), new Vector2(560f, 330f));
        VerticalLayoutGroup layout = buttons.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 14f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        // Los botones quedan sin acción: la navegación entre escenas (punto 2) la conecta otro integrante
        Button play = CreateMenuButton("Btn_Jugar", buttons, "JUGAR", font);
        CreateMenuButton("Btn_Opciones", buttons, "OPCIONES", font);
        CreateMenuButton("Btn_Creditos", buttons, "CRÉDITOS", font);
        CreateMenuButton("Btn_Salir", buttons, "SALIR", font);

        // --- Pie de página ---
        TextMeshProUGUI footer = CreateText("Footer", root,
            "UNIFRANZ La Paz - Programación Gráfica y Multimedia - v0.1", font, 12f,
            TextDim, TextAlignmentOptions.BottomRight);
        RectTransform footerRect = footer.rectTransform;
        footerRect.anchorMin = footerRect.anchorMax = footerRect.pivot = new Vector2(1f, 0f);
        footerRect.anchoredPosition = new Vector2(-32f, 28f);
        footerRect.sizeDelta = new Vector2(1200f, 30f);

        CreateEventSystem(play.gameObject);

        // --- Guardar la escena y registrarla como primera en Build Profiles ---
        EditorSceneManager.SaveScene(scene, ScenePath);
        AddSceneToBuildFirst(ScenePath);

        Selection.activeObject = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
        Debug.Log($"[MainMenuSceneBuilder] Escena generada en {ScenePath} y agregada como escena 0 del build.");
    }

    // Cámara ortográfica con fondo negro (el menú se dibuja en un Canvas Overlay)
    private static void CreateCamera()
    {
        GameObject go = new GameObject("Main Camera");
        go.tag = "MainCamera";
        Camera cam = go.AddComponent<Camera>();
        cam.orthographic = true;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        go.AddComponent<AudioListener>();
        go.transform.position = new Vector3(0f, 0f, -10f);
    }

    // Canvas en Screen Space Overlay escalado a 1920x1080
    private static Canvas CreateCanvas()
    {
        GameObject go = new GameObject("Canvas_MainMenu");
        go.layer = LayerMask.NameToLayer("UI");
        Canvas canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        go.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    // EventSystem con el módulo del Input System nuevo (no el Input Manager legacy)
    private static void CreateEventSystem(GameObject firstSelected)
    {
        GameObject go = new GameObject("EventSystem");
        EventSystem eventSystem = go.AddComponent<EventSystem>();
        eventSystem.firstSelectedGameObject = firstSelected;
        go.AddComponent<InputSystemUIInputModule>();
    }

    // Botón del menú: fondo semitransparente, texto pixel art y efecto de resaltado
    private static Button CreateMenuButton(string name, Transform parent, string text, TMP_FontAsset font)
    {
        RectTransform rect = CreateRect(name, parent);
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = Color.white;

        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0f, 0f, 0f, 0.35f);
        colors.highlightedColor = new Color(Orange.r, Orange.g, Orange.b, 0.25f);
        colors.selectedColor = colors.highlightedColor;
        colors.pressedColor = new Color(Orange.r, Orange.g, Orange.b, 0.5f);
        colors.disabledColor = new Color(0.2f, 0.2f, 0.2f, 0.35f);
        colors.fadeDuration = 0.08f;
        button.colors = colors;
        button.navigation = new Navigation { mode = Navigation.Mode.Vertical, wrapAround = true };

        LayoutElement element = rect.gameObject.AddComponent<LayoutElement>();
        element.preferredHeight = 72f;

        TextMeshProUGUI label = CreateText("Label", rect, text, font, 30f, TextLight,
            TextAlignmentOptions.MidlineLeft);
        Stretch(label.rectTransform);
        label.rectTransform.offsetMin = new Vector2(28f, 0f);

        MenuButtonEffect effect = rect.gameObject.AddComponent<MenuButtonEffect>();
        SetField(effect, "label", label);
        return button;
    }

    // --- Utilidades de creación de UI ---

    private static RectTransform CreateRect(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.layer = LayerMask.NameToLayer("UI");
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    private static TextMeshProUGUI CreateText(string name, Transform parent, string text, TMP_FontAsset font,
        float size, Color color, TextAlignmentOptions alignment)
    {
        RectTransform rect = CreateRect(name, parent);
        TextMeshProUGUI tmp = rect.gameObject.AddComponent<TextMeshProUGUI>();
        tmp.font = font;
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.alignment = alignment;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.raycastTarget = false;
        return tmp;
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

    // Asigna un campo privado [SerializeField] desde el Editor
    private static void SetField(Object component, string field, Object value)
    {
        SerializedObject so = new SerializedObject(component);
        so.FindProperty(field).objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // --- Assets ---

    // Importa la textura como Sprite sin compresión con el filtro indicado
    private static Sprite ImportSprite(string path, FilterMode filter)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
        {
            Debug.LogError($"[MainMenuSceneBuilder] No se encontró la textura {path}");
            return null;
        }
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.filterMode = filter;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    // Crea (una sola vez) el Font Asset de TextMeshPro para la fuente pixel art
    private static TMP_FontAsset GetOrCreateFontAsset()
    {
        TMP_FontAsset existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
        if (existing != null)
        {
            return existing;
        }

        Font source = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
        if (source == null)
        {
            Debug.LogError($"[MainMenuSceneBuilder] No se encontró la fuente {FontPath}");
            return null;
        }

        TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(source, 64, 6, GlyphRenderMode.SDFAA,
            1024, 1024, AtlasPopulationMode.Dynamic, true);
        fontAsset.name = "PressStart2P SDF";
        AssetDatabase.CreateAsset(fontAsset, FontAssetPath);

        // El atlas y el material se guardan como sub-assets del Font Asset
        fontAsset.atlasTextures[0].name = "PressStart2P SDF Atlas";
        AssetDatabase.AddObjectToAsset(fontAsset.atlasTextures[0], fontAsset);
        fontAsset.material.name = "PressStart2P SDF Material";
        AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);

        // Pre-cargar los caracteres y fijar el atlas (estático) para que no cambie en cada ejecución
        if (!fontAsset.TryAddCharacters(FontCharacters, out string missing))
        {
            Debug.LogWarning($"[MainMenuSceneBuilder] Caracteres no disponibles en la fuente: {missing}");
        }
        fontAsset.atlasPopulationMode = AtlasPopulationMode.Static;

        EditorUtility.SetDirty(fontAsset.atlasTextures[0]);
        EditorUtility.SetDirty(fontAsset);
        AssetDatabase.SaveAssets();
        return fontAsset;
    }

    // Coloca la escena como la primera del build sin quitar las que ya existen
    private static void AddSceneToBuildFirst(string path)
    {
        var scenes = EditorBuildSettings.scenes.Where(s => s.path != path).ToList();
        scenes.Insert(0, new EditorBuildSettingsScene(path, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
