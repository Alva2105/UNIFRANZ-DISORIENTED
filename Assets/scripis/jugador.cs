using UnityEngine;
using UnityEngine.InputSystem;

public class jugador : MonoBehaviour
{
    public float velocidad = 5f;
    private Rigidbody2D rb;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        float movimientoH = 0f;

        // Comprobación directa mediante el teclado del Input System
        if (Keyboard.current != null)
        {
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
            {
                movimientoH = -1f; // Izquierda
            }
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
            {
                movimientoH = 1f; // Derecha
            }
        }

        rb.linearVelocity = new Vector2(movimientoH * velocidad, rb.linearVelocity.y);
    }
}