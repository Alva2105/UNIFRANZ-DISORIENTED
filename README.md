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

## Plataforma objetivo

El videojuego está diseñado para ser ejecutado en:

- **PC**
- **Sistema operativo Windows**

La versión final del proyecto será compilada como un ejecutable para PC.

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

## Prototipo inicial en desarrollo

Actualmente, el proyecto se encuentra en su fase inicial de desarrollo.

Durante esta etapa se trabaja principalmente en:

- Organización del repositorio.
- Configuración inicial del proyecto en Unity.
- Organización de la estructura de carpetas.
- Creación de la escena inicial.
- Incorporación de assets iniciales.
- Construcción del escenario base mediante Tiles y Tilemaps.
- Implementación progresiva de los sistemas fundamentales.

Las siguientes etapas incorporarán progresivamente:

- Movimiento completo del personaje.
- Sistema de interacción.
- NPCs y diálogos.
- Cinco hitos de inducción.
- Minijuegos.
- Sistema de Estrés y Ansiedad.
- Alteraciones visuales y auditivas.
- Integración general.
- Pruebas de jugabilidad y corrección de errores.

---

## Estructura del proyecto

El proyecto se encuentra organizado siguiendo una estructura básica para separar los recursos gráficos, escenas y scripts utilizados durante el desarrollo.

```text
Assets/
│
├── art/
│   ├── Backgrounds/
│   │   └── Fondos y recursos visuales de los escenarios.
│   │
│   ├── Tiles/
│   │   └── Sprites y elementos utilizados para construir pisos,
│   │       plataformas y estructuras del escenario.
│   │
│   └── Characters/
│       └── Sprites y recursos gráficos de los personajes.
│
├── Scenes/
│   └── Nivel01.unity
│       └── Escena principal del prototipo inicial.
│
└── Scripts/
    └── Scripts en C# utilizados para implementar las mecánicas
        y sistemas del videojuego.
