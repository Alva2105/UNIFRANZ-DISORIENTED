using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Pantalla "Elige el hito" que abre Seleccionar nivel: un botón por cada hito (1 a 5).
// Al elegir uno avisa a MainMenuController, que abre la elección de personaje para
// ese hito. Atrás vuelve al menú (Escape lo maneja MainMenuController, igual que en
// los demás paneles).
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

    public event Action<string> Chosen;
    public event Action Back;

    private void Awake()
    {
        foreach (Level level in levels)
        {
            string scene = level.scene;
            level.button.onClick.AddListener(() => Chosen?.Invoke(scene));
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
