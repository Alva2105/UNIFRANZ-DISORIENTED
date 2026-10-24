using System;
using System.Collections.Generic;
using UnityEngine;

// Personaje jugable elegido en el menú (los dos son jugables, ninguno es NPC) y su
// aspecto según el nivel de estrés. El Animator siempre reproduce los cuadros de la
// hoja normal; en LateUpdate cada cuadro se cambia por el mismo cuadro de la hoja de
// Estrés o de Ansiedad, así no hacen falta clips ni Animator por nivel.
public class PlayerCharacter : MonoBehaviour
{
    [Serializable]
    public class Option
    {
        public string name;
        public RuntimeAnimatorController controller;

        [Tooltip("Cuadros de la hoja normal: respirar (0-7) y caminar (8-15).")]
        public Sprite[] normal;

        [Tooltip("Mismos cuadros, en el mismo orden, de la hoja de Estrés.")]
        public Sprite[] stress;

        [Tooltip("Mismos cuadros, en el mismo orden, de la hoja de Ansiedad.")]
        public Sprite[] anxiety;
    }

    [SerializeField] private Option[] options;
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer sprite;

    private Option current;
    private StressLevel level;
    private readonly Dictionary<Sprite, int> frameIndex = new Dictionary<Sprite, int>();

    private void Awake()
    {
        int selected = Mathf.Clamp(GameSettings.SelectedCharacter, 0, options.Length - 1);
        current = options[selected];
        animator.runtimeAnimatorController = current.controller;
        sprite.sprite = current.normal[0];
        for (int i = 0; i < current.normal.Length; i++)
        {
            frameIndex[current.normal[i]] = i;
        }
    }

    private void Start()
    {
        if (StressSystem.Instance != null)
        {
            StressSystem.Instance.LevelChanged += OnLevelChanged;
            level = StressSystem.Instance.Level;
        }
    }

    private void OnDestroy()
    {
        if (StressSystem.Instance != null)
        {
            StressSystem.Instance.LevelChanged -= OnLevelChanged;
        }
    }

    private void OnLevelChanged(StressLevel newLevel)
    {
        level = newLevel;
    }

    // Después del Animator: reemplaza el cuadro normal por el del nivel actual
    private void LateUpdate()
    {
        if (level == StressLevel.Normal || !frameIndex.TryGetValue(sprite.sprite, out int i))
        {
            return;
        }
        Sprite[] frames = level == StressLevel.Anxiety ? current.anxiety : current.stress;
        if (i < frames.Length)
        {
            sprite.sprite = frames[i];
        }
    }
}
