using TMPro;
using UnityEngine;

// Aviso "[E] Hablar" de la UI que sigue a un punto del mundo. Va en un Canvas
// Screen Space - Overlay.
public class InteractionPrompt : MonoBehaviour
{
    [SerializeField] private TMP_Text label;
    [SerializeField] private string keyHint = "[E]";

    private Camera mainCamera;
    private Vector3 worldPosition;

    private void Awake()
    {
        mainCamera = Camera.main;
        gameObject.SetActive(false);
    }

    public void Show(string action, Vector3 position)
    {
        worldPosition = position;
        label.text = keyHint + " " + action;
        gameObject.SetActive(true);
        Follow();
    }

    public void Hide()
    {
        if (gameObject.activeSelf)
        {
            gameObject.SetActive(false);
        }
    }

    private void LateUpdate()
    {
        Follow();
    }

    private void Follow()
    {
        transform.position = mainCamera.WorldToScreenPoint(worldPosition);
    }
}
