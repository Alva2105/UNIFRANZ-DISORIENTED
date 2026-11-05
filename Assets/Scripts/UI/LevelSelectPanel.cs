using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Pantalla "Elige el hito" que se abre después de elegir personaje: un botón por cada
// hito (1 a 5) que carga su escena. Atrás vuelve a la elección de personaje (Escape lo
// maneja MainMenuController, igual que en los demás paneles).
public class LevelSelectPanel : MonoBehaviour
{
    [Serializable]
    public class Level
    {
        public Button button;
        public string scene;
    }

    [SerializeField] private Level[] levels;
    [SerializeField] private Button backButton;

    public event Action Back;

    private void Awake()
    {
        foreach (Level level in levels)
        {
            string scene = level.scene;
            level.button.onClick.AddListener(() => SceneManager.LoadScene(scene));
        }
        backButton.onClick.AddListener(Close);
    }

    private void OnEnable()
    {
        EventSystem.current?.SetSelectedGameObject(levels[0].button.gameObject);
    }

    public void Close()
    {
        gameObject.SetActive(false);
        Back?.Invoke();
    }
}
