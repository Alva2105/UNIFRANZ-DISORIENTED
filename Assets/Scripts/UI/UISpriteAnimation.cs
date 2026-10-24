using UnityEngine;
using UnityEngine.UI;

// Reproduce una secuencia de sprites sobre una Image de UI usando tiempo sin
// escalar, para que siga animándose con el juego en pausa (Time.timeScale = 0).
[RequireComponent(typeof(Image))]
public class UISpriteAnimation : MonoBehaviour
{
    [SerializeField] private Sprite[] frames;
    [SerializeField] private float framesPerSecond = 8f;

    private Image image;
    private float time;

    private void Awake()
    {
        image = GetComponent<Image>();
    }

    private void OnEnable()
    {
        time = 0f;
    }

    private void Update()
    {
        if (frames == null || frames.Length == 0)
        {
            return;
        }
        time += Time.unscaledDeltaTime;
        image.sprite = frames[(int)(time * framesPerSecond) % frames.Length];
    }
}
