using UnityEngine;

// Basura tirada en el pasillo: al recogerla desaparece y el jugador la lleva hasta el
// contenedor de reciclaje (RecycleBin). Cada objeto recogido calma un poco.
public class TrashPickup : Interactable
{
    // Basura que el jugador lleva encima y todavía no botó
    public static int Carried { get; private set; }

    [SerializeField] private string itemName = "una botella";
    [SerializeField] private AudioClip pickClip;

    private void Start()
    {
        // Al cargar la escena se empieza sin nada en las manos
        Carried = 0;
    }

    public override void Interact()
    {
        Carried++;
        AudioManager.PlaySfx(pickClip);
        if (StressSystem.Instance != null)
        {
            StressSystem.Instance.Add(stressChange);
        }
        DialogueBox.Instance.Show(speaker, new[] { $"Recoges {itemName}. Llevas {Carried} en las manos." }, null);
        gameObject.SetActive(false);
    }

    // El contenedor vacía lo que lleva el jugador
    public static int DropAll()
    {
        int count = Carried;
        Carried = 0;
        return count;
    }
}
