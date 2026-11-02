using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

// Gradas entre pisos dentro de un mismo nivel (p. ej. Planta Baja <-> Mezzanine).
// Cada piso está en otra parte de la escena: al usar las gradas la pantalla se
// funde a negro, el jugador aparece en el destino y la cámara pasa a los límites
// del otro piso.
public class FloorPortal : Interactable
{
    [Header("Destino")]
    [SerializeField] private Transform destination;
    [Tooltip("Límites de cámara del piso de destino.")]
    [SerializeField] private PolygonCollider2D destinationBounds;

    [Header("Referencias")]
    [SerializeField] private PlayerMovement player;
    [SerializeField] private CinemachineCamera vcam;
    [SerializeField] private CinemachineConfiner2D confiner;
    [SerializeField] private CanvasGroup fade;

    [SerializeField] private float fadeSeconds = 0.35f;
    [SerializeField] private AudioClip travelClip;

    private static bool traveling;

    public override void Interact()
    {
        if (!traveling)
        {
            StartCoroutine(Travel());
        }
    }

    private IEnumerator Travel()
    {
        traveling = true;
        player.enabled = false;
        AudioManager.PlaySfx(travelClip);
        yield return Fade(0f, 1f);

        // Mover al jugador y avisar a la cámara del salto para que no lo siga deslizándose
        Vector3 from = player.transform.position;
        Vector3 to = destination.position;
        player.GetComponent<Rigidbody2D>().position = to;
        player.transform.position = to;
        confiner.BoundingShape2D = destinationBounds;
        confiner.InvalidateBoundingShapeCache();
        vcam.OnTargetObjectWarped(player.transform, to - from);
        vcam.PreviousStateIsValid = false;
        yield return null;

        yield return Fade(1f, 0f);
        player.enabled = true;
        traveling = false;
    }

    private IEnumerator Fade(float from, float to)
    {
        for (float t = 0f; t < fadeSeconds; t += Time.deltaTime)
        {
            fade.alpha = Mathf.Lerp(from, to, t / fadeSeconds);
            yield return null;
        }
        fade.alpha = to;
    }
}
