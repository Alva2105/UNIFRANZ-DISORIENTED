using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Minijuego "Memoria de anatomía": cartas boca abajo con huesos y la zona del cuerpo
// donde están. Con izquierda/derecha se recorre la mesa y con Interactuar se da vuelta
// una carta; hay que encontrar cada hueso con su zona. Cada error sube el estrés y, si
// el jugador está en Ansiedad, dos cartas tapadas se mezclan (la ansiedad confunde).
public class MemoryMinigame : MinigameBase
{
    [Serializable]
    public class Pair
    {
        public string first;
        public string second;
    }

    [Header("Memoria")]
    [SerializeField] private Pair[] pairs;
    [SerializeField] private Image[] cards;
    [SerializeField] private TMP_Text[] cardTexts;
    [SerializeField] private TMP_Text promptText;
    [SerializeField] private TMP_Text feedbackText;
    [SerializeField] private TMP_Text progressText;

    [SerializeField] private float stressPerMiss = 2f;
    [SerializeField] private float reliefPerfect = -12f;
    [Tooltip("Errores permitidos para considerar que fue un buen repaso.")]
    [SerializeField] private int goodMisses = 4;

    [SerializeField] private Color hiddenColor = new Color(0.16f, 0.18f, 0.3f);
    [SerializeField] private Color cursorColor = new Color(0.98f, 0.44f, 0.08f);
    [SerializeField] private Color shownColor = new Color(0.3f, 0.38f, 0.7f);
    [SerializeField] private Color matchedColor = new Color(0.2f, 0.5f, 0.35f);

    private string[] faces;
    private int[] pairOf;
    private bool[] shown;
    private bool[] matched;
    private int cursor;

    protected override IEnumerator Play(Action<float> result)
    {
        Deal();
        string key = ControlPlatform.InteractHint;
        promptText.text = "Une cada hueso con la zona del cuerpo donde está";
        feedbackText.text = $"< > recorre las cartas   {key} da vuelta";
        int misses = 0;
        int found = 0;
        int first = -1;
        cursor = 0;
        Redraw();

        while (found < pairs.Length)
        {
            progressText.text = $"Pares: {found}/{pairs.Length}   Errores: {misses}";
            int step = HorizontalPressed();
            if (step != 0)
            {
                cursor = (cursor + step + cards.Length) % cards.Length;
                Redraw();
            }
            if (ConfirmPressed() && !shown[cursor] && !matched[cursor])
            {
                shown[cursor] = true;
                Redraw();
                if (first < 0)
                {
                    first = cursor;
                }
                else if (pairOf[first] == pairOf[cursor])
                {
                    matched[first] = matched[cursor] = true;
                    found++;
                    AudioManager.PlaySfx(goodClip);
                    feedbackText.text = $"¡{faces[first]} - {faces[cursor]}!";
                    first = -1;
                    Redraw();
                }
                else
                {
                    misses++;
                    AudioManager.PlaySfx(badClip);
                    feedbackText.text = "No coinciden...";
                    if (StressSystem.Instance != null)
                    {
                        StressSystem.Instance.Add(stressPerMiss);
                    }
                    yield return new WaitForSeconds(0.9f);
                    shown[first] = shown[cursor] = false;
                    first = -1;
                    if (StressSystem.Instance != null && StressSystem.Instance.Level == StressLevel.Anxiety && ShuffleTwo())
                    {
                        feedbackText.text = "¡Las cartas se mezclaron! La ansiedad no te deja pensar.";
                    }
                    Redraw();
                }
            }
            yield return null;
        }

        progressText.text = $"Pares: {found}/{pairs.Length}   Errores: {misses}";
        bool good = misses <= goodMisses;
        promptText.text = good ? "¡Repaso completo!" : "Repaso terminado.";
        feedbackText.text = good ? "Te sientes list@ para el parcial práctico." : "Te costó, pero ya reconoces todos los huesos.";
        result(good ? reliefPerfect : reliefPerfect * 0.3f);
    }

    // Reparte las cartas mezcladas: cada par aporta dos (hueso y zona)
    private void Deal()
    {
        int count = pairs.Length * 2;
        faces = new string[count];
        pairOf = new int[count];
        shown = new bool[count];
        matched = new bool[count];
        for (int i = 0; i < pairs.Length; i++)
        {
            faces[i * 2] = pairs[i].first;
            faces[i * 2 + 1] = pairs[i].second;
            pairOf[i * 2] = pairOf[i * 2 + 1] = i;
        }
        for (int i = count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (faces[i], faces[j]) = (faces[j], faces[i]);
            (pairOf[i], pairOf[j]) = (pairOf[j], pairOf[i]);
        }
    }

    // Intercambia dos cartas tapadas al azar; false si no quedan dos para mezclar
    private bool ShuffleTwo()
    {
        var hidden = new System.Collections.Generic.List<int>();
        for (int i = 0; i < faces.Length; i++)
        {
            if (!matched[i] && !shown[i])
            {
                hidden.Add(i);
            }
        }
        if (hidden.Count < 2)
        {
            return false;
        }
        int a = hidden[UnityEngine.Random.Range(0, hidden.Count)];
        hidden.Remove(a);
        int b = hidden[UnityEngine.Random.Range(0, hidden.Count)];
        (faces[a], faces[b]) = (faces[b], faces[a]);
        (pairOf[a], pairOf[b]) = (pairOf[b], pairOf[a]);
        return true;
    }

    private void Redraw()
    {
        for (int i = 0; i < cards.Length; i++)
        {
            bool open = shown[i] || matched[i];
            cards[i].color = i == cursor ? cursorColor : matched[i] ? matchedColor : open ? shownColor : hiddenColor;
            cardTexts[i].text = open ? faces[i] : "?";
        }
    }
}
