using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.OnScreen;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Controles táctiles para Android. Se crean solos al cargar una escena jugable
// (una escena con PlayerMovement), así que no hace falta agregarlos a mano en
// cada escena. En Windows no se crean y el juego se controla con teclado.
//
// Cada botón usa un OnScreenButton del Input System que simula una tecla de
// PlayerControls, por eso PlayerMovement, PlayerInteractor, PauseMenu y
// DialogueBox funcionan igual en ambas plataformas sin cambios.
public class MobileControls : MonoBehaviour
{
    // Resolución de referencia del Canvas, la misma que usa el menú principal
    private static readonly Vector2 ReferenceResolution = new Vector2(1920f, 1080f);

    private static readonly Color ButtonColor = new Color(1f, 1f, 1f, 0.25f);
    private static readonly Color LabelColor = new Color(1f, 1f, 1f, 0.9f);

    private static Sprite circleSprite;

    // Se ocultan con CanvasGroup y no desactivando el objeto, para que un botón
    // presionado siga recibiendo el momento en que se suelta y la tecla no quede pegada
    private CanvasGroup leftButton;
    private CanvasGroup rightButton;
    private CanvasGroup sprintButton;
    private CanvasGroup interactButton;
    private CanvasGroup pauseButton;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        if (!ControlPlatform.UsesTouchControls)
        {
            return;
        }

        // La primera escena ya está cargada; las siguientes llegan por el evento
        SceneManager.sceneLoaded += (scene, mode) => TryCreate();
        TryCreate();
    }

    private static void TryCreate()
    {
        // Solo en escenas jugables y una sola vez por escena
        if (FindAnyObjectByType<PlayerMovement>() == null || FindAnyObjectByType<MobileControls>() != null)
        {
            return;
        }

        new GameObject("MobileControls").AddComponent<MobileControls>();
    }

    private void Awake()
    {
        EnsureEventSystem();
        BuildCanvas();
    }

    private void Update()
    {
        // Sin controles durante la pausa o con el nivel terminado (los menús se tocan directo)
        bool levelOver = LevelFlow.Instance != null && LevelFlow.Instance.IsOver;
        bool hidden = PauseMenu.IsPaused || levelOver;

        // En un diálogo solo queda el botón de interactuar, que avanza el texto
        bool gameplay = !hidden && !DialogueBox.IsOpen;

        SetVisible(leftButton, gameplay);
        SetVisible(rightButton, gameplay);
        SetVisible(sprintButton, gameplay);
        SetVisible(pauseButton, gameplay);
        SetVisible(interactButton, !hidden);
    }

    private static void SetVisible(CanvasGroup button, bool visible)
    {
        button.alpha = visible ? 1f : 0f;
        button.blocksRaycasts = visible;
    }

    // Los botones necesitan un EventSystem con el módulo del Input System para recibir toques
    private static void EnsureEventSystem()
    {
        if (EventSystem.current != null)
        {
            return;
        }

        new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
    }

    private void BuildCanvas()
    {
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;

        // Escala según el alto para que los botones midan lo mismo en pantallas anchas
        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = ReferenceResolution;
        scaler.matchWidthOrHeight = 1f;

        gameObject.AddComponent<GraphicRaycaster>();

        // Izquierda: caminar. Derecha: interactuar y correr. Arriba a la derecha: pausa
        leftButton = CreateButton("Left", "<", "<Keyboard>/a", new Vector2(0f, 0f), new Vector2(170f, 170f), 200f, 90f);
        rightButton = CreateButton("Right", ">", "<Keyboard>/d", new Vector2(0f, 0f), new Vector2(420f, 170f), 200f, 90f);
        interactButton = CreateButton("Interact", "USAR", "<Keyboard>/e", new Vector2(1f, 0f), new Vector2(-180f, 190f), 230f, 46f);
        sprintButton = CreateButton("Sprint", "CORRER", "<Keyboard>/leftShift", new Vector2(1f, 0f), new Vector2(-430f, 140f), 180f, 34f);
        pauseButton = CreateButton("Pause", "II", "<Keyboard>/escape", new Vector2(1f, 1f), new Vector2(-100f, -100f), 120f, 46f);
    }

    // Crea un botón circular anclado a una esquina que mantiene presionada la tecla indicada
    private CanvasGroup CreateButton(string name, string label, string controlPath,
        Vector2 anchor, Vector2 position, float size, float fontSize)
    {
        // Se crea inactivo para asignar la tecla antes de que el OnScreenButton se registre
        var button = new GameObject(name, typeof(RectTransform));
        button.SetActive(false);
        button.transform.SetParent(transform, false);

        var rect = (RectTransform)button.transform;
        rect.anchorMin = rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(size, size);

        var image = button.AddComponent<Image>();
        image.sprite = GetCircleSprite();
        image.color = ButtonColor;

        var onScreen = button.AddComponent<OnScreenButton>();
        onScreen.controlPath = controlPath;

        var text = new GameObject("Label", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
        text.transform.SetParent(button.transform, false);
        var textRect = text.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = textRect.offsetMax = Vector2.zero;
        text.text = label;
        text.fontSize = fontSize;
        text.fontStyle = FontStyles.Bold;
        text.color = LabelColor;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;

        button.SetActive(true);
        return button.AddComponent<CanvasGroup>();
    }

    // Círculo generado por código para no depender de un sprite del proyecto
    private static Sprite GetCircleSprite()
    {
        if (circleSprite != null)
        {
            return circleSprite;
        }

        const int size = 128;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float radius = size / 2f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(radius, radius));
                float alpha = Mathf.Clamp01(radius - distance);
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        texture.Apply();

        circleSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        return circleSprite;
    }
}
