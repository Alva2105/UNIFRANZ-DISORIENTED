using UnityEngine;

// Movimiento lateral del jugador con un Rigidbody2D en modo Kinematic.
// Un cuerpo Kinematic no recibe gravedad ni choca solo con el escenario,
// así que aquí se simulan ambas cosas a mano: la gravedad se acumula en la
// velocidad vertical y las colisiones se resuelven con Rigidbody2D.Cast.
// Requiere además un Collider2D (p. ej. CapsuleCollider2D) en el mismo GameObject.
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    // --- Parámetros de velocidad ---
    [Header("Velocidad")]
    [Tooltip("Velocidad horizontal caminando, en unidades por segundo.")]
    [SerializeField] private float walkSpeed = 4f;

    [Tooltip("Multiplicador aplicado mientras se mantiene Sprint (1.2 = 20% más rápido).")]
    [SerializeField] private float sprintMultiplier = 1.2f;

    // --- Parámetros de gravedad ---
    [Header("Gravedad")]
    [Tooltip("Multiplicador sobre Physics2D.gravity.")]
    [SerializeField] private float gravityScale = 1f;

    [Tooltip("Velocidad máxima de caída, en unidades por segundo.")]
    [SerializeField] private float maxFallSpeed = 20f;

    // --- Parámetros de colisión ---
    [Header("Colisiones")]
    [Tooltip("Margen que se deja entre el collider y las superficies para no quedar incrustado.")]
    [SerializeField] private float skinWidth = 0.01f;

    [Tooltip("Pendiente máxima, en grados, que se considera suelo caminable.")]
    [SerializeField, Range(0f, 89f)] private float maxGroundAngle = 50f;

    // Distancia mínima por debajo de la cual no vale la pena hacer un Cast
    private const float MinMoveDistance = 0.0005f;

    // --- Referencias y estado interno ---
    private Rigidbody2D body;
    private PlayerControls controls;
    private ContactFilter2D contactFilter;
    private readonly RaycastHit2D[] hitBuffer = new RaycastHit2D[8];

    private float moveInput;
    private bool sprintHeld;
    private Vector2 velocity;
    private Vector2 groundNormal = Vector2.up;
    private float minGroundNormalY;

    // --- Estado público (solo lectura) para cámara, animación o estrés ---
    public bool IsGrounded { get; private set; }
    public bool IsSprinting => sprintHeld && Mathf.Abs(moveInput) > 0.01f;
    public Vector2 Velocity => velocity;

    private void Awake()
    {
        // Configuración del Rigidbody2D: Kinematic y sin gravedad propia,
        // porque la gravedad la calcula este script
        body = GetComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;

        // Instancia de las acciones de input generadas desde PlayerControls
        controls = new PlayerControls();

        // Filtro de colisiones: respeta la matriz de capas del proyecto e ignora triggers
        contactFilter.useTriggers = false;
        contactFilter.SetLayerMask(Physics2D.GetLayerCollisionMask(gameObject.layer));

        // Umbral de la normal para decidir qué superficies cuentan como suelo
        minGroundNormalY = Mathf.Cos(maxGroundAngle * Mathf.Deg2Rad);
    }

    private void OnEnable()
    {
        // Activar el Action Map del jugador junto con el componente
        controls.Player.Enable();
    }

    private void OnDisable()
    {
        // Desactivar el Action Map para no dejar acciones escuchando
        controls.Player.Disable();

        // Limpiar el input para que el jugador no siga moviéndose al reactivarse
        moveInput = 0f;
        sprintHeld = false;
    }

    private void OnDestroy()
    {
        // Liberar el asset de input creado en Awake
        controls.Dispose();
    }

    private void Update()
    {
        // Lectura del input cada frame; solo interesa el eje horizontal (sin salto)
        moveInput = controls.Player.Move.ReadValue<Vector2>().x;
        sprintHeld = controls.Player.Sprint.IsPressed();
    }

    private void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;

        // Velocidad horizontal según el input, aplicando el sprint si corresponde
        float speed = walkSpeed * (sprintHeld ? sprintMultiplier : 1f);
        velocity.x = moveInput * speed;

        // Gravedad manual acumulada en la velocidad vertical, con límite de caída
        velocity.y += Physics2D.gravity.y * gravityScale * dt;
        velocity.y = Mathf.Max(velocity.y, -maxFallSpeed);

        // Se recalcula el contacto con el suelo en cada paso de física
        IsGrounded = false;

        Vector2 delta = velocity * dt;

        // Movimiento horizontal a lo largo del suelo, para seguir las pendientes
        Vector2 alongGround = new Vector2(groundNormal.y, -groundNormal.x);
        Move(alongGround * delta.x, false);

        // Movimiento vertical: caída y detección de suelo
        Move(Vector2.up * delta.y, true);

        // Si no hay suelo debajo, la próxima pendiente de referencia es plana
        if (!IsGrounded)
        {
            groundNormal = Vector2.up;
        }
    }

    // Desplaza el cuerpo en la dirección indicada deteniéndose antes de chocar.
    // verticalPass indica si es el paso vertical, que es el que registra el suelo.
    private void Move(Vector2 move, bool verticalPass)
    {
        float distance = move.magnitude;
        if (distance < MinMoveDistance)
        {
            return;
        }

        // Proyectar el collider en la dirección del movimiento para encontrar obstáculos
        int count = body.Cast(move, contactFilter, hitBuffer, distance + skinWidth);
        for (int i = 0; i < count; i++)
        {
            Vector2 normal = hitBuffer[i].normal;

            // Una superficie suficientemente horizontal cuenta como suelo
            if (normal.y > minGroundNormalY)
            {
                IsGrounded = true;
                if (verticalPass)
                {
                    groundNormal = normal;
                    normal.x = 0f;
                }
            }

            // Quitar a la velocidad la componente que empuja contra la superficie
            float projection = Vector2.Dot(velocity, normal);
            if (projection < 0f)
            {
                velocity -= projection * normal;
            }

            // Recortar la distancia para quedar justo antes del impacto
            float allowed = hitBuffer[i].distance - skinWidth;
            if (allowed < distance)
            {
                distance = Mathf.Max(allowed, 0f);
            }
        }

        // Aplicar el desplazamiento resultante
        body.position += move.normalized * distance;
    }
}
