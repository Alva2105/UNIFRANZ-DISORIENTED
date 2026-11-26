using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using H = Hito1SceneBuilder;

// Piezas de experiencia de usuario que comparten todos los hitos: la guía hacia el
// objetivo (flecha sobre el objetivo y pista en pantalla), los avisos de progreso y el
// nombre de cada piso, y la tarjeta de instrucciones de cada minijuego. La usan los
// generadores de escena de los hitos.
internal static class UxSceneBuilder
{
    private const string ArrowPath = "Assets/Sprites/UI/Guia/flecha_objetivo.png";

    private static readonly Color Orange = new Color(0.976f, 0.435f, 0.075f);
    private static readonly Color TextLight = new Color(0.92f, 0.92f, 0.95f);
    private static readonly Color PanelColor = new Color(0.06f, 0.07f, 0.16f, 0.88f);

    // Agrega la guía, los avisos y el nombre de cada piso. floors: objeto de límites de
    // cámara del piso, título y subtítulo para el aviso al llegar
    internal static void Add(Scene scene, Transform ui, GameObject systems, GameObject player, ObjectiveTracker tracker,
        params (string bounds, string title, string subtitle)[] floors)
    {
        H.ImportPixelArt(ArrowPath, 1);
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(H.FontPath);

        foreach (var floor in floors)
        {
            GameObject bounds = H.FindRoot(scene, floor.bounds);
            FloorLabel label = bounds.GetComponent<FloorLabel>() ?? bounds.AddComponent<FloorLabel>();
            H.SetString(label, "title", floor.title);
            H.SetString(label, "subtitle", floor.subtitle);
        }

        // --- Guía: flecha que flota sobre el objetivo y pista abajo al centro ---
        var markerGo = new GameObject("FlechaObjetivo");
        markerGo.transform.localScale = Vector3.one * H.CharacterScale;
        SpriteRenderer marker = markerGo.AddComponent<SpriteRenderer>();
        marker.sprite = H.LoadSprite(ArrowPath);
        marker.sortingOrder = 20;
        marker.enabled = false;

        RectTransform hintBar = Rect(ui, "PistaObjetivo");
        hintBar.anchorMin = hintBar.anchorMax = hintBar.pivot = new Vector2(0.5f, 0f);
        hintBar.anchoredPosition = new Vector2(0f, 40f);
        hintBar.sizeDelta = new Vector2(1100f, 56f);
        // Franja oscura para que la pista se lea sobre cualquier fondo
        Image hintImage = hintBar.gameObject.AddComponent<Image>();
        hintImage.color = PanelColor;
        hintImage.raycastTarget = false;
        hintBar.gameObject.SetActive(false);
        TextMeshProUGUI hint = H.CreateLabel(hintBar, "Texto", font, "", 20f, TextLight, 0f);
        hint.rectTransform.anchorMin = Vector2.zero;
        hint.rectTransform.anchorMax = Vector2.one;
        hint.rectTransform.sizeDelta = Vector2.zero;
        hint.enableAutoSizing = true;
        hint.fontSizeMax = 20f;
        hint.fontSizeMin = 12f;

        ObjectiveGuide guide = systems.AddComponent<ObjectiveGuide>();
        H.Set(guide, "tracker", tracker);
        H.Set(guide, "player", player.transform);
        H.Set(guide, "marker", marker);
        H.Set(guide, "hintText", hint);

        // --- Avisos de progreso: franja arriba al centro, bajo el objetivo ---
        RectTransform notice = Rect(ui, "AvisoProgreso");
        notice.anchorMin = notice.anchorMax = notice.pivot = new Vector2(0.5f, 1f);
        notice.anchoredPosition = new Vector2(0f, -150f);
        notice.sizeDelta = new Vector2(1200f, 150f);
        Image noticeImage = notice.gameObject.AddComponent<Image>();
        // Opaco: con transparencia el borde naranja se trasluce y el fondo se ve café
        noticeImage.color = new Color(PanelColor.r, PanelColor.g, PanelColor.b, 1f);
        noticeImage.raycastTarget = false;
        Outline outline = notice.gameObject.AddComponent<Outline>();
        outline.effectColor = Orange;
        outline.effectDistance = new Vector2(3f, -3f);
        CanvasGroup group = notice.gameObject.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        group.blocksRaycasts = false;
        group.interactable = false;
        TextMeshProUGUI title = H.CreateLabel(notice, "Titulo", font, "", 28f, Orange, 40f);
        title.rectTransform.sizeDelta = new Vector2(1160f, 50f);
        TextMeshProUGUI body = H.CreateLabel(notice, "Mensaje", font, "", 18f, TextLight, -25f);
        body.rectTransform.sizeDelta = new Vector2(1160f, 80f);
        body.lineSpacing = 20f;
        body.enableAutoSizing = true;
        body.fontSizeMax = 18f;
        body.fontSizeMin = 11f;

        // Debajo del menú de pausa, para que la pausa siga quedando encima
        Transform pause = H.FindDeep(ui, "MenuPausa");
        if (pause != null)
        {
            notice.SetSiblingIndex(pause.GetSiblingIndex());
            hintBar.SetSiblingIndex(pause.GetSiblingIndex());
        }

        ProgressNotices notices = systems.AddComponent<ProgressNotices>();
        H.Set(notices, "tracker", tracker);
        H.Set(notices, "player", player.transform);
        H.Set(notices, "group", group);
        H.Set(notices, "titleText", title);
        H.Set(notices, "bodyText", body);
    }

    // Tarjeta de instrucciones del minijuego: tapa el contenido del panel (no el título)
    // hasta que el jugador presiona Interactuar
    internal static void AddMinigameIntro(MinigameBase game, GameObject panel)
    {
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(H.FontPath);
        // Centrada y más baja que el título del panel, así el título queda siempre a la vista
        RectTransform card = Rect(panel.transform, "Instrucciones");
        card.anchorMin = card.anchorMax = card.pivot = new Vector2(0.5f, 0.5f);
        card.anchoredPosition = new Vector2(0f, -40f);
        card.sizeDelta = new Vector2(1700f, 640f);
        // Opaca: la barra de tiempo y las casillas del minijuego no deben verse a través
        card.gameObject.AddComponent<Image>().color = new Color(0.04f, 0.05f, 0.12f, 1f);
        card.SetAsLastSibling();
        TextMeshProUGUI text = H.CreateLabel(card, "Texto", font, "", 24f, TextLight, 0f);
        text.rectTransform.sizeDelta = new Vector2(1500f, 520f);
        text.lineSpacing = 30f;
        text.richText = true;
        H.CreateLabel(card, "Encabezado", font, "CÓMO SE JUEGA", 22f, Orange, 260f);
        card.gameObject.SetActive(false);
        H.Set(game, "introGroup", card.gameObject);
        H.Set(game, "introText", text);
    }

    private static RectTransform Rect(Transform parent, string name)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }
}
