# UNIFRANZ: DESORIENTED

## Descripción

**UNIFRANZ: DESORIENTED** es un videojuego de aventura y exploración en **2D Side-Scrolling**, desarrollado como parte de la materia de **Programación Gráfica y Multimedia I** de la carrera de Ingeniería de Sistemas de UNIFRANZ.

El videojuego representa la experiencia de adaptación de un estudiante de primer semestre durante su primer día en la universidad. El jugador deberá recorrer diferentes espacios del **Bloque A de UNIFRANZ La Paz**, interactuar con el entorno y completar una Ruta de Inducción compuesta por cinco hitos principales.

A medida que avanza la experiencia, la presión y la desorientación pueden afectar progresivamente el estado emocional del protagonista. El sistema central del videojuego representa la transición entre **Estrés y Ansiedad**, generando alteraciones en la interfaz, el audio, la iluminación y la percepción del entorno.

El proyecto busca combinar una experiencia de orientación universitaria con elementos de exploración, tensión y terror psicológico.

---

## Género

- Aventura
- Exploración
- Terror psicológico
- Puzzle y minijuegos

La jugabilidad se desarrolla mediante una perspectiva **2D Side-Scrolling**, permitiendo al jugador desplazarse lateralmente por diferentes escenarios, interactuar con objetos y NPCs, completar objetivos y avanzar a través de los diferentes hitos de inducción.

---

## Plataformas

El videojuego se compila para:

- **Windows** (ejecutable para PC)
- **Android** (APK, en orientación horizontal)

### Controles

| Acción | Windows | Android |
|---|---|---|
| Moverse | A/D o flechas | Botones `<` y `>` |
| Correr | Shift | Botón `CORRER` |
| Interactuar y avanzar diálogos | E o Espacio | Botón `USAR` |
| Pausar | Escape | Botón `II` |

Los menús se usan con el mouse en Windows y tocando la pantalla en Android.

---

## Estilo visual

El videojuego utiliza una estética:

> **2D Pixel Art Side-Scrolling**

Los escenarios representan diferentes espacios reconocibles del Bloque A de UNIFRANZ La Paz, incluyendo pasillos, aulas, laboratorios y áreas comunes.

La identidad visual presenta una transformación progresiva relacionada con el estado psicológico del protagonista:

- Estado inicial: ambiente institucional y visualmente estable.
- Incremento del Estrés: aparición progresiva de tensión visual.
- Estado de Ansiedad: alteraciones en la percepción, iluminación, interfaz y ambientación.

La dirección artística toma como referencia la representación visual y el desplazamiento lateral de videojuegos Pixel Art de exploración, adaptando los espacios universitarios a la identidad visual del proyecto.

---

## Tecnologías utilizadas

El proyecto utiliza las siguientes tecnologías y herramientas:

### Motor de desarrollo

- **Unity 6.3 LTS**

### Lenguaje de programación

- **C#**

### Control de versiones

- **Git**
- **GitHub**

### Herramientas complementarias

- Herramientas de creación y edición de Pixel Art.
- Herramientas de edición de audio y efectos de sonido.
- Visual Studio Code u otro entorno compatible con C#.

---

# Integrantes del equipo

| Integrante | Rol principal | Área de responsabilidad |
|---|---|---|
| **Alvaro Del Carpio** | Programador de Sistemas Core | Sistemas fundamentales, movimiento, físicas, cámara, Estrés/Ansiedad y HUD |
| **Carlos Valverde** | Programador de Gameplay | Interacciones, NPCs, diálogos, hitos de inducción, minijuegos y progresión |
| **Eduardo Condoreno** | Artista 2D y Diseñador de UI/UX | Personajes, sprites, Tilesets, escenarios, interfaz y estética visual |
| **Jhoel Herrera** | Diseñador de Niveles, Integrador y QA | Diseño de niveles, integración, audio, testing y control de calidad |

### Usuarios de GitHub

- **Alvaro Del Carpio** — `Alva2105`
- **Carlos Valverde** — `NazoVruytX`
- **Eduardo Condoreno** — `EduardoC2006`
- **Jhoel Herrera** — `MrPirate-29`

---

# Estado del proyecto

## Prototipo jugable en desarrollo

Actualmente se puede jugar el primer nivel, ubicado en el piso 2 del Bloque A.

Ya está implementado:

- Menú principal con Jugar, Opciones, Créditos y Salir.
- Elección entre los dos personajes jugables.
- Pasillo continuo del piso 2 con aulas, mural y escaleras interactivas.
- Primer hito de inducción: encontrar el aula 207.
- Sistema de Estrés y Ansiedad, con cambios en el escenario, los personajes y el HUD.
- Diálogos, menú de pausa y configuración.
- Pantallas de victoria y de crisis de ansiedad.
- Exportación a Windows y Android, con controles táctiles en Android.

Las siguientes etapas incorporarán:

- Los hitos de inducción restantes.
- Minijuegos.
- Alteraciones auditivas.
- Integración general.
- Pruebas de jugabilidad y corrección de errores.

---

## Estructura del proyecto

El proyecto separa los recursos gráficos, las escenas y los scripts usados durante el desarrollo.

```text
Assets/
│
├── Animaciones/        Clips y Animator de los personajes y del HUD.
│
├── art/                Fondos, tiles, personajes base y UI del menú principal.
│
├── Scenes/
│   ├── MainMenu.unity  Menú principal.
│   ├── Piso2.unity     Primer nivel jugable.
│   └── Nivel01.unity   Escena del prototipo inicial.
│
├── Scripts/
│   ├── Build/          Exportación a Windows y Android.
│   ├── Gameplay/       Interacciones, diálogos y flujo del nivel.
│   ├── Player/         Movimiento, personajes y controles.
│   ├── SaveSystem/     Configuración guardada.
│   ├── Stress/         Sistema de Estrés y Ansiedad.
│   └── UI/             Menús, HUD y pausa.
│
└── Sprites/
    ├── Escenario/      Pasillos del piso 2 en sus tres estados.
    ├── Personajes/     Hojas de los personajes en sus tres estados.
    └── UI/             Menús, laptop de pausa y barra de ansiedad.
```
