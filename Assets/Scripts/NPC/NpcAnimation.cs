using UnityEngine;

// Animación de un NPC: respira en su pose y, si tiene patrulla, camina de un lado a
// otro con su hoja de caminar. Cuando el jugador se acerca se detiene y lo mira,
// para que se pueda hablar con él. No usa Animator porque solo son dos hojas.
public class NpcAnimation : MonoBehaviour
{
    [SerializeField] private SpriteRenderer sprite;
    [SerializeField] private Sprite[] idleFrames;
    [SerializeField] private Sprite[] walkFrames;
    [SerializeField] private float framesPerSecond = 8f;

    [Tooltip("Jugador al que mira el NPC. Vacío: el NPC no se gira.")]
    [SerializeField] private Transform player;

    [Tooltip("Distancia horizontal desde la que el NPC se gira hacia el jugador.")]
    [SerializeField] private float lookDistance = 6f;

    [Tooltip("Hacia dónde mira el arte original: true si mira a la derecha.")]
    [SerializeField] private bool artFacesRight = true;

    [Header("Patrulla (opcional)")]
    [Tooltip("Distancia que camina a cada lado de su posición inicial. 0 = no camina.")]
    [SerializeField] private float patrolDistance;
    [SerializeField] private float walkSpeed = 1.2f;

    [Tooltip("Segundos que se queda quieto en cada extremo.")]
    [SerializeField] private float pauseSeconds = 2f;

    [Tooltip("Si el jugador está más cerca que esto, deja de caminar.")]
    [SerializeField] private float stopDistance = 2.5f;

    private float time;
    private float startX;
    private int direction = 1;
    private float pauseTimer;

    private void Start()
    {
        // Desfase al azar para que los NPCs no respiren todos al mismo tiempo
        time = Random.value;
        startX = transform.position.x;
        pauseTimer = Random.Range(0f, pauseSeconds);
    }

    private void Update()
    {
        time += Time.deltaTime;
        bool walking = Patrol();
        Sprite[] frames = walking && walkFrames.Length > 0 ? walkFrames : idleFrames;
        if (frames.Length > 0)
        {
            sprite.sprite = frames[(int)(time * framesPerSecond) % frames.Length];
        }

        if (walking)
        {
            Face(direction > 0);
        }
        else if (player != null && Mathf.Abs(player.position.x - transform.position.x) <= lookDistance)
        {
            Face(player.position.x > transform.position.x);
        }
    }

    // Mueve al NPC entre sus dos extremos; devuelve true mientras camina
    private bool Patrol()
    {
        if (patrolDistance <= 0f || DialogueBox.IsOpen)
        {
            return false;
        }
        if (player != null && Mathf.Abs(player.position.x - transform.position.x) < stopDistance)
        {
            return false;
        }
        if (pauseTimer > 0f)
        {
            pauseTimer -= Time.deltaTime;
            return false;
        }

        float target = startX + direction * patrolDistance;
        Vector3 position = transform.position;
        position.x = Mathf.MoveTowards(position.x, target, walkSpeed * Time.deltaTime);
        transform.position = position;

        if (Mathf.Approximately(position.x, target))
        {
            direction = -direction;
            pauseTimer = pauseSeconds;
        }
        return true;
    }

    private void Face(bool right)
    {
        sprite.flipX = right != artFacesRight;
    }
}
