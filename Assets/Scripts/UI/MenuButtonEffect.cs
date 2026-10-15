using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Efecto visual de los botones del menú: al pasar el mouse o seleccionarlos con
// el teclado se resaltan con un marcador "> ", cambian de color y crecen un poco.
// Solo es feedback visual; la acción del botón (onClick) se asigna aparte.
[RequireComponent(typeof(Button))]
public class MenuButtonEffect : MonoBehaviour,
    IPointerEnterHandler, ISelectHandler, IDeselectHandler
{
    // --- Referencias ---
    [Tooltip("Texto del botón. Si se deja vacío se busca en los hijos.")]
    [SerializeField] private TMP_Text label;

    // --- Apariencia ---
    [SerializeField] private Color normalColor = new Color(0.85f, 0.85f, 0.85f);
    [SerializeField] private Color highlightColor = new Color(0.976f, 0.435f, 0.075f);
    [SerializeField] private string marker = "> ";
    [SerializeField] private float highlightScale = 1.06f;
    [SerializeField] private float scaleSpeed = 14f;

    // --- Estado interno ---
    private Button button;
    private string baseText;
    private bool isHighlighted;

    private void Awake()
    {
        // Buscar referencias y guardar el texto original del botón
        button = GetComponent<Button>();
        if (label == null)
        {
            label = GetComponentInChildren<TMP_Text>();
        }
        baseText = label.text;
        ApplyHighlight(false);
    }

    private void OnDisable()
    {
        // Volver al estado normal si el menú se oculta con el botón resaltado
        ApplyHighlight(false);
        transform.localScale = Vector3.one;
    }

    private void Update()
    {
        // Escala suave hacia el tamaño objetivo; usa tiempo sin escalar para
        // funcionar también con el juego en pausa (Time.timeScale = 0)
        float target = isHighlighted ? highlightScale : 1f;
        float scale = Mathf.Lerp(transform.localScale.x, target, scaleSpeed * Time.unscaledDeltaTime);
        transform.localScale = new Vector3(scale, scale, 1f);
    }

    // Al pasar el mouse se selecciona el botón, así mouse y teclado comparten un solo resaltado
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (button.interactable)
        {
            button.Select();
        }
    }

    public void OnSelect(BaseEventData eventData)
    {
        ApplyHighlight(true);
    }

    public void OnDeselect(BaseEventData eventData)
    {
        ApplyHighlight(false);
    }

    // Cambia el texto y el color según el estado de resaltado
    private void ApplyHighlight(bool highlighted)
    {
        isHighlighted = highlighted;
        if (label == null)
        {
            return;
        }
        label.text = highlighted ? marker + baseText : baseText;
        label.color = highlighted ? highlightColor : normalColor;
    }
}
