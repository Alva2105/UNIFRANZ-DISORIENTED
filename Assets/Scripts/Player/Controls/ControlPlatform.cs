using UnityEngine;

// Punto único que decide qué controles usa cada plataforma.
//   - Windows (Standalone): teclado, con las teclas definidas en PlayerControls
//     (A/D o flechas para moverse, Shift para correr, E/Espacio para interactuar,
//     Escape para pausar). No necesita nada extra.
//   - Android: controles táctiles en pantalla (MobileControls), que presionan
//     esas mismas teclas de forma virtual. Así los scripts del juego no
//     distinguen de dónde viene el input.
public static class ControlPlatform
{
    // Verdadero en Android (y en el Device Simulator del Editor)
    public static bool UsesTouchControls => Application.isMobilePlatform;
}
