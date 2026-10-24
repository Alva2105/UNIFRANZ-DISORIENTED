using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

// Algo con lo que el jugador puede interactuar (NPC, puerta, cartel). Al usarlo
// muestra un diálogo y, al cerrarlo, cambia el estrés y dispara onFinished
// (por ejemplo, completar un hito). El jugador lo detecta por distancia.
public class Interactable : MonoBehaviour
{
    // Registro de los interactuables activos, para buscar el más cercano sin físicas
    public static readonly List<Interactable> Active = new List<Interactable>();

    // --- Detección ---
    [Tooltip("Texto del aviso, p. ej. \"Hablar\" o \"Revisar\".")]
    [SerializeField] private string prompt = "Hablar";

    [Tooltip("Distancia horizontal máxima desde la que se puede usar.")]
    [SerializeField] private float range = 1.5f;

    [Tooltip("Posición del aviso respecto al objeto.")]
    [SerializeField] private Vector2 promptOffset = new Vector2(0f, 3.8f);

    // --- Diálogo ---
    [SerializeField] private string speaker;
    [SerializeField, TextArea(2, 4)] private string[] lines;

    // --- Efecto ---
    [Tooltip("Estrés que suma al terminar (negativo para calmar).")]
    [SerializeField] private float stressChange;

    [Tooltip("Si está marcado, el cambio de estrés solo se aplica la primera vez.")]
    [SerializeField] private bool stressOnce = true;

    [SerializeField] private UnityEvent onFinished = new UnityEvent();

    private bool used;

    public string Prompt => prompt;
    public float Range => range;
    public Vector3 PromptPosition => transform.position + (Vector3)promptOffset;

    private void OnEnable()
    {
        Active.Add(this);
    }

    private void OnDisable()
    {
        Active.Remove(this);
    }

    public void Interact()
    {
        DialogueBox.Instance.Show(speaker, lines, Finish);
    }

    private void Finish()
    {
        if (!(stressOnce && used) && StressSystem.Instance != null)
        {
            StressSystem.Instance.Add(stressChange);
        }
        used = true;
        onFinished.Invoke();
    }
}
