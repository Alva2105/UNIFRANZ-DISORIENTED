using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Pantalla de Configuración (laptop): sliders de música y efectos guardados en
// GameSettings. Los íconos cambian a "silencio" cuando el volumen llega a cero.
// La usan el menú principal y el menú de pausa; avisan con Closed para volver.
public class SettingsPanel : MonoBehaviour
{
    // --- Sliders ---
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider effectsSlider;

    [Tooltip("Rellenos de tipo Filled: se recortan en vez de estirarse, así el pixel art no se deforma.")]
    [SerializeField] private Image musicFill;
    [SerializeField] private Image effectsFill;

    // --- Íconos de estado ---
    [SerializeField] private Image musicIcon;
    [SerializeField] private Image effectsIcon;
    [SerializeField] private Sprite musicOn;
    [SerializeField] private Sprite musicOff;
    [SerializeField] private Sprite effectsOn;
    [SerializeField] private Sprite effectsOff;

    [SerializeField] private Button backButton;

    public event Action Closed;

    private void Awake()
    {
        musicSlider.onValueChanged.AddListener(value =>
        {
            GameSettings.MusicVolume = value;
            Refresh();
        });
        effectsSlider.onValueChanged.AddListener(value =>
        {
            GameSettings.EffectsVolume = value;
            Refresh();
        });
        backButton.onClick.AddListener(Close);
    }

    private void OnEnable()
    {
        // Mostrar los valores guardados sin volver a escribirlos
        musicSlider.SetValueWithoutNotify(GameSettings.MusicVolume);
        effectsSlider.SetValueWithoutNotify(GameSettings.EffectsVolume);
        Refresh();
        EventSystem.current?.SetSelectedGameObject(musicSlider.gameObject);
    }

    public void Close()
    {
        gameObject.SetActive(false);
        Closed?.Invoke();
    }

    private void Refresh()
    {
        musicFill.fillAmount = musicSlider.normalizedValue;
        effectsFill.fillAmount = effectsSlider.normalizedValue;
        musicIcon.sprite = musicSlider.value > 0f ? musicOn : musicOff;
        effectsIcon.sprite = effectsSlider.value > 0f ? effectsOn : effectsOff;
    }
}
