# CLAUDE.md

## Proyecto

**UNIFRANZ-DISORIENTED**: videojuego en **Unity 6.3 LTS (6000.3.16f1)**, 2D Side-Scrolling Pixel Art.

Juego de adaptación universitaria con progresión por **5 hitos** y un sistema de **Estrés/Ansiedad** que altera visual y auditivamente el entorno.

## Mi responsabilidad en el equipo

- Movimiento del jugador
- Cámara
- Input System
- Sistema de Estrés/Ansiedad
- Sistema de guardado
- Menú Principal (Hito 3, punto 1): escena, Canvas, título y botones. La navegación entre escenas (botones Jugar/Salir) la implementa otro integrante.

## Convenciones de carpetas

`Assets/Scripts` organizado por sistema:

- `Assets/Scripts/Player/`
- `Assets/Scripts/Camera/`
- `Assets/Scripts/Stress/`
- `Assets/Scripts/SaveSystem/`
- `Assets/Scripts/UI/` (efectos del menú; `UI/Editor/` para herramientas de Editor)

Assets de interfaz en `Assets/Art/UI/` (`MainMenu/`, `Fonts/`). Escena del menú: `Assets/Scenes/MainMenu.unity`.

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
  - `Assets/Scripts/UI` y `Assets/Art/UI` (solo para el menú)
- Leer solo los archivos estrictamente necesarios para cada tarea, no todo el proyecto.
