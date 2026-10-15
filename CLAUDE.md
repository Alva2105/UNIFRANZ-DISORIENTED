# CLAUDE.md

## Proyecto

Videojuego en **Unity 6.3 LTS (6000.3.16f1)**, 2D Side-Scrolling Pixel Art.

Juego de adaptación universitaria con progresión por **5 hitos** y un sistema de **Estrés/Ansiedad** que altera visual y auditivamente el entorno.

## Mi responsabilidad en el equipo

- Movimiento del jugador
- Cámara
- Input System
- Sistema de Estrés/Ansiedad
- Sistema de guardado

## Convenciones de carpetas

`Assets/Scripts` organizado por sistema:

- `Assets/Scripts/Player/`
- `Assets/Scripts/Camera/`
- `Assets/Scripts/Stress/`
- `Assets/Scripts/SaveSystem/`

## Decisiones técnicas

- **Input:** usar el Input System nuevo de Unity (no el Input Manager legacy).
- **Movimiento del jugador:** `Rigidbody2D` en modo **Kinematic**, sin salto, con sprint.
- **Cámara:** Cinemachine 2D con Pixel Perfect.

## Estilo de código

- Nombres de clases y métodos en **inglés**.
- Comentarios en **español**.

## Alcance de trabajo

- No explorar ni leer `Assets/Art`, `Assets/Audio`, ni las carpetas de otros roles del equipo, salvo que la tarea lo pida explícitamente.
- Trabajar solo dentro de:
  - `Assets/Scripts/Player`
  - `Assets/Scripts/Camera`
  - `Assets/Scripts/Stress`
  - `Assets/Scripts/SaveSystem`
- Leer solo los archivos estrictamente necesarios para cada tarea, no todo el proyecto.
