using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Pantalla "Elige tu personaje" que abre Jugar (o Seleccionar nivel, después de elegir
// el hito). Cada tarjeta muestra al personaje respirando y, mientras está
// seleccionada, caminando. Al elegir se guarda en GameSettings y se carga el nivel:
// el Hito 1 desde Jugar, o el hito que se eligió en el selector.
public class CharacterSelectPanel : MonoBehaviour
{
    [Serializable]
    public class Card
    {
        public Button button;
        public Image preview;
        public Sprite[] idle;
        public Sprite[] walk;
    }

    [Tooltip("Nivel que carga Jugar.")]
    [SerializeField] private string gameScene = "Hito1";
    [SerializeField] private Card[] cards;
    [SerializeField] private Button backButton;
    [SerializeField] private float framesPerSecond = 8f;

    public event Action Closed;

    private float time;
    private string sceneToLoad;

    private void Awake()
    {
        for (int i = 0; i < cards.Length; i++)
        {
            int index = i;
            cards[i].button.onClick.AddListener(() => Choose(index));
        }
        backButton.onClick.AddListener(Close);
    }

    // Abre la pantalla; scene vacío carga el nivel de Jugar (Hito 1)
    public void Show(string scene)
    {
        sceneToLoad = string.IsNullOrEmpty(scene) ? gameScene : scene;
        gameObject.SetActive(true);
    }

    private void OnEnable()
    {
        time = 0f;
        int last = Mathf.Clamp(GameSettings.SelectedCharacter, 0, cards.Length - 1);
        EventSystem.current?.SetSelectedGameObject(cards[last].button.gameObject);
    }

    private void Update()
    {
        time += Time.unscaledDeltaTime;
        int frame = (int)(time * framesPerSecond);
        GameObject selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        foreach (Card card in cards)
        {
            Sprite[] frames = selected == card.button.gameObject ? card.walk : card.idle;
            card.preview.sprite = frames[frame % frames.Length];
        }
    }

    private void Choose(int index)
    {
        GameSettings.SelectedCharacter = index;
        SceneManager.LoadScene(string.IsNullOrEmpty(sceneToLoad) ? gameScene : sceneToLoad);
    }

    public void Close()
    {
        gameObject.SetActive(false);
        Closed?.Invoke();
    }
}
