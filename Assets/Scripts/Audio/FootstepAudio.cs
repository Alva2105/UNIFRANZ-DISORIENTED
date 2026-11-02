using UnityEngine;

// Pasos del jugador sobre el piso cerámico: suenan mientras camina por el suelo,
// más seguido al correr, con un clip y un tono al azar para que no se repitan igual.
[RequireComponent(typeof(PlayerMovement))]
public class FootstepAudio : MonoBehaviour
{
    [SerializeField] private AudioClip[] steps;

    [Tooltip("Segundos entre pasos caminando.")]
    [SerializeField] private float walkInterval = 0.42f;

    [Tooltip("Segundos entre pasos corriendo.")]
    [SerializeField] private float sprintInterval = 0.3f;

    [Range(0f, 1f)]
    [SerializeField] private float volume = 0.5f;

    private PlayerMovement movement;
    private AudioSource source;
    private float timer;

    private void Awake()
    {
        movement = GetComponent<PlayerMovement>();
        source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
    }

    private void Update()
    {
        bool walking = movement.enabled && movement.IsGrounded && Mathf.Abs(movement.Velocity.x) > 0.1f;
        if (!walking || steps.Length == 0)
        {
            // Al empezar a caminar el primer paso suena enseguida
            timer = 0f;
            return;
        }

        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            timer = movement.IsSprinting ? sprintInterval : walkInterval;
            source.pitch = Random.Range(0.9f, 1.1f);
            source.PlayOneShot(steps[Random.Range(0, steps.Length)], volume * AudioManager.EffectsVolume);
        }
    }
}
