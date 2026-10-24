using UnityEngine;

// Conecta el movimiento con el Animator del personaje: activa "Caminando" cuando
// hay velocidad horizontal y voltea el sprite hacia donde camina.
[RequireComponent(typeof(PlayerMovement))]
public class PlayerAnimation : MonoBehaviour
{
    private static readonly int WalkingParam = Animator.StringToHash("Caminando");

    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer sprite;

    [Tooltip("Velocidad de la animación al correr (1 = normal).")]
    [SerializeField] private float sprintAnimSpeed = 1.3f;

    [Tooltip("Marcar si el arte original mira a la izquierda.")]
    [SerializeField] private bool artFacesLeft;

    private const float MinSpeed = 0.05f;

    private PlayerMovement movement;

    private void Awake()
    {
        movement = GetComponent<PlayerMovement>();
    }

    private void Update()
    {
        float vx = movement.enabled ? movement.Velocity.x : 0f;
        bool walking = Mathf.Abs(vx) > MinSpeed;

        animator.SetBool(WalkingParam, walking);
        animator.speed = walking && movement.IsSprinting ? sprintAnimSpeed : 1f;

        if (walking)
        {
            sprite.flipX = (vx < 0f) != artFacesLeft;
        }
    }
}
