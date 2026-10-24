using UnityEngine;

// Busca el interactuable más cercano al jugador, muestra el aviso encima y lo
// usa al presionar Interact (E o Espacio).
public class PlayerInteractor : MonoBehaviour
{
    [SerializeField] private InteractionPrompt prompt;

    private PlayerControls controls;

    private void Awake()
    {
        controls = new PlayerControls();
    }

    private void OnEnable()
    {
        controls.Player.Interact.Enable();
    }

    private void OnDisable()
    {
        controls.Player.Interact.Disable();

        // Al descargar la escena el aviso puede destruirse antes que el jugador
        if (prompt != null)
        {
            prompt.Hide();
        }
    }

    private void OnDestroy()
    {
        controls.Dispose();
    }

    private void Update()
    {
        // Sin interacción durante diálogos, pausa o con el nivel terminado.
        // También se ignora el cuadro en que se cerró un diálogo, para que la
        // misma tecla no lo vuelva a abrir.
        bool levelOver = LevelFlow.Instance != null && LevelFlow.Instance.IsOver;
        if (DialogueBox.IsOpen || PauseMenu.IsPaused || levelOver ||
            DialogueBox.ClosedFrame == Time.frameCount)
        {
            prompt.Hide();
            return;
        }

        Interactable target = FindClosest();
        if (target == null)
        {
            prompt.Hide();
            return;
        }

        prompt.Show(target.Prompt, target.PromptPosition);
        if (controls.Player.Interact.WasPressedThisFrame())
        {
            prompt.Hide();
            target.Interact();
        }
    }

    private Interactable FindClosest()
    {
        Interactable best = null;
        float bestDistance = float.MaxValue;
        foreach (Interactable item in Interactable.Active)
        {
            float distance = Mathf.Abs(item.transform.position.x - transform.position.x);
            if (distance <= item.Range && distance < bestDistance)
            {
                best = item;
                bestDistance = distance;
            }
        }
        return best;
    }
}
