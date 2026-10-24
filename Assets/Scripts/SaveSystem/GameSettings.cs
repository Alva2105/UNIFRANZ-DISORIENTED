using System;
using UnityEngine;

// Preferencias del jugador guardadas en PlayerPrefs: los volúmenes de música y
// efectos (el sistema de audio debe leerlos y escuchar Changed) y el personaje
// elegido en el menú.
public static class GameSettings
{
    private const string MusicKey = "VolumenMusica";
    private const string EffectsKey = "VolumenEfectos";
    private const string CharacterKey = "Personaje";
    private const float DefaultVolume = 0.8f;

    public static event Action Changed;

    public static float MusicVolume
    {
        get => PlayerPrefs.GetFloat(MusicKey, DefaultVolume);
        set => Save(MusicKey, value);
    }

    public static float EffectsVolume
    {
        get => PlayerPrefs.GetFloat(EffectsKey, DefaultVolume);
        set => Save(EffectsKey, value);
    }

    // Índice del personaje jugable (0 = Personaje 1, 1 = Personaje 2)
    public static int SelectedCharacter
    {
        get => PlayerPrefs.GetInt(CharacterKey, 0);
        set
        {
            PlayerPrefs.SetInt(CharacterKey, value);
            PlayerPrefs.Save();
        }
    }

    private static void Save(string key, float value)
    {
        PlayerPrefs.SetFloat(key, Mathf.Clamp01(value));
        PlayerPrefs.Save();
        Changed?.Invoke();
    }
}
