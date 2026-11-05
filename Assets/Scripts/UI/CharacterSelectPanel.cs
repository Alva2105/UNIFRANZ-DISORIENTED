using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Pantalla "Elige tu personaje" que abre Jugar. Cada tarjeta muestra al personaje
// respirando y, mientras está seleccionada, caminando. Al elegir se guarda en
// GameSettings y se abre el selector de hitos (o, si no hay, se carga el nivel).
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

    [SerializeField] private string gameScene = "Hito1";
    [Tooltip("Selector de hitos que se abre al elegir personaje (opcional).")]
    [SerializeField] private LevelSelectPanel levelSelect;
    [SerializeField] private Card[] cards;
    [SerializeField] private Button backButton;
    [SerializeField] private float framesPerSecond = 8f;

    public event Action Closed;

    private float time;

    private void Awake()
    {
        for (int i = 0; i < cards.Length; i++)
        {
            int index = i;
            cards[i].button.onClick.AddListener(() => Choose(index));
        }
        backButton.onClick.AddListener(Close);
        if (levelSelect != null)
        {
            levelSelect.gameObject.SetActive(false);
            levelSelect.Back += () => gameObject.SetActive(true);
        }
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
        if (levelSelect != null)
        {
            gameObject.SetActive(false);
            levelSelect.gameObject.SetActive(true);
            return;
        }
        SceneManager.LoadScene(gameScene);
    }

    public void Close()
    {
        gameObject.SetActive(false);
        Closed?.Invoke();
    }
}
