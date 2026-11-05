using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using H = Hito1SceneBuilder;
using H2 = Hito2SceneBuilder;
using H3 = Hito3SceneBuilder;

// Arma la escena del Hito 5 (Evaluaciones finales), el último nivel: un recorrido por
// todo el bloque con el reloj del día corriendo. Mezzanine (pagar en Cajas), Piso 1,
// Piso 2 (el pasillo de Eduardo), Piso 5 y, al final, Bienestar Estudiantil, unidos por
// ascensor y gradas. Tres exámenes finales en pisos distintos, el trabajo final y una
// crisis antes del último examen, en cualquier orden. Al terminar, Lucía y los
// compañeros despiden al jugador y la pantalla final muestra el resumen del semestre.
// Menú: Tools > UNIFRANZ-DISORIENTED. En modo batch:
//   Unity -batchmode -quit -projectPath . -executeMethod Hito5SceneBuilder.Build
public static class Hito5SceneBuilder
{
    private const string SourceScenePath = "Assets/Scenes/Piso2.unity";
    private const string ScenePath = "Assets/Scenes/Hito5.unity";
    private const string MenuScenePath = "Assets/Scenes/MainMenu.unity";
    private static readonly string[] PreviousScenes =
    {
        "Assets/Scenes/Hito1.unity", "Assets/Scenes/Hito2.unity", "Assets/Scenes/Hito3.unity", "Assets/Scenes/Hito4.unity"
    };

    private static readonly string[] MezzanineBlocks = { "arca", "cajas" };
    private static readonly string[] Piso1Blocks = { "pasillo", "fisiologia", "histologia", "simulacion" };
    private static readonly string[] Piso5Blocks = { "biblioteca", "aula_dinamica" };
    private static readonly string[] BienestarBlocks = { "sala", "consejeria" };

    // Cada piso se arma lejos en X (el Piso 2 de Eduardo queda donde está)
    private const float MezzanineOffsetX = 300f;
    private const float Piso1OffsetX = 600f;
    private const float Piso5OffsetX = 900f;
    private const float BienestarOffsetX = 1200f;

    private static readonly Color TextLight = new Color(0.92f, 0.92f, 0.95f);
    private static readonly Color Orange = new Color(0.976f, 0.435f, 0.075f);

    private static readonly string[] RemovedObjects =
        { "Puerta_204", "Puerta_205", "Puerta_206", "Puerta_207", "Mural", "Escaleras" };

    [MenuItem("Tools/UNIFRANZ-DISORIENTED/Generar Hito 5 (Evaluaciones finales)")]
    public static void Build()
    {
        PrepareSprites();

        // Copia de Piso2 conservando el .meta de Hito5 si ya existe (mismo GUID)
        if (File.Exists(ScenePath))
        {
            File.Copy(SourceScenePath, ScenePath, true);
            AssetDatabase.ImportAsset(ScenePath, ImportAssetOptions.ForceUpdate);
        }
        else if (!AssetDatabase.CopyAsset(SourceScenePath, ScenePath))
        {
            Debug.LogError("[Hito5SceneBuilder] No se pudo copiar " + SourceScenePath);
            return;
        }
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        foreach (string name in RemovedObjects)
        {
            GameObject old = H.FindRoot(scene, name);
            if (old != null)
            {
                Object.DestroyImmediate(old);
            }
        }

        GameObject systems = H.FindRoot(scene, "Sistemas");
        GameObject player = H.FindRoot(scene, "Jugador");
        Transform ui = H.FindRoot(scene, "HUD").transform;
        float floorY = player.transform.position.y - 0.05f;

        // --- Escenario: el pasillo del Piso 2 se conserva y se suman los demás pisos ---
        H2.MeasureBlocks(scene, out float[] p2Left, out float[] p2Width);
        float P2(int block, float fraction) => p2Left[block] + fraction * p2Width[block];
        H.Floor mz = H.BuildFloor("Mezzanine", MezzanineBlocks, MezzanineOffsetX, floorY);
        H.Floor p1 = H.BuildFloor("Piso1", Piso1Blocks, Piso1OffsetX, floorY);
        H.Floor p5 = H.BuildFloor("Piso5", Piso5Blocks, Piso5OffsetX, floorY);
        H.Floor bs = H.BuildFloor("Bienestar", BienestarBlocks, BienestarOffsetX, floorY);

        PolygonCollider2D p2Bounds = H.FindRoot(scene, "LimitesCamara").GetComponent<PolygonCollider2D>();
        PolygonCollider2D mzBounds = CreateBounds("LimitesCamara_Mezzanine", mz);
        PolygonCollider2D p1Bounds = CreateBounds("LimitesCamara_Piso1", p1);
        PolygonCollider2D p5Bounds = CreateBounds("LimitesCamara_Piso5", p5);
        PolygonCollider2D bsBounds = CreateBounds("LimitesCamara_Bienestar", bs);
        GameObject vcamGo = H.FindRoot(scene, "CinemachineCamera");
        CinemachineCamera vcam = vcamGo.GetComponent<CinemachineCamera>();
        CinemachineConfiner2D confiner = vcamGo.GetComponent<CinemachineConfiner2D>();
        confiner.BoundingShape2D = mzBounds;

        // El día empieza en el Mezzanine, frente a Cajas
        player.transform.position = new Vector3(mz.X(0, 0.40f), floorY + 0.05f, 0f);
        vcamGo.transform.position = new Vector3(mz.X(0, 0.40f), vcamGo.transform.position.y, vcamGo.transform.position.z);

        // --- Estrés: el nivel más difícil ---
        StressSystem stress = systems.GetComponent<StressSystem>();
        H.SetFloat(stress, "passiveRate", 1f);
        H.SetFloat(stress, "sprintRate", 2.4f);

        // --- Audio ---
        AudioManager audio = systems.AddComponent<AudioManager>();
        H.Set(audio, "music", AssetDatabase.LoadAssetAtPath<AudioClip>(H.MusicPath));
        FootstepAudio footsteps = player.AddComponent<FootstepAudio>();
        H.SetArray(footsteps, "steps", Enumerable.Range(0, 5).Select(i => H.Sfx("footstep_concrete_00" + i)).ToArray());
        HeartbeatAudio heartbeat = systems.AddComponent<HeartbeatAudio>();
        H.Set(heartbeat, "lub", H.Sfx("impactSoft_heavy_000"));
        H.Set(heartbeat, "dub", H.Sfx("impactSoft_heavy_001"));

        // --- Checklist del Hito 5: del 2 al 6 en cualquier orden ---
        ObjectiveTracker tracker = systems.AddComponent<ObjectiveTracker>();
        H.SetString(tracker, "milestoneTitle", "HITO 5");
        H.SetObjectives(tracker, new[]
        {
            ("pago", "Regulariza tu pago en Cajas (Mezzanine)"),
            ("examen1", "Examen Piso 2"),
            ("examen2", "Examen Piso 1"),
            ("trabajo", "Trabajo final (Piso 5)"),
            ("crisis", "Calma (Piso 5)"),
            ("examen3", "Examen Piso 5"),
            ("bienestar", "Sube a Bienestar Estudiantil")
        });
        SetAnyOrder(tracker, "examen1", "examen2", "trabajo", "crisis", "examen3");
        H.Set(tracker, "objectiveCompletedClip", H.Sfx("confirmation_002"));
        H.Set(tracker, "milestoneCompletedClip", H.Sfx("confirmation_004"));
        TMP_Text objectiveText = H2.SetupObjectiveHud(ui, tracker);
        objectiveText.fontSizeMax = objectiveText.fontSize;
        objectiveText.fontSizeMin = 12f;
        objectiveText.enableAutoSizing = true;

        // --- Reloj del día para todo el nivel ---
        TextMeshProUGUI clock = H.CreateLabel(ui, "RelojDia", AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(H.FontPath),
            "", 20f, TextLight, 0f);
        var clockRect = clock.rectTransform;
        clockRect.anchorMin = clockRect.anchorMax = clockRect.pivot = new Vector2(0f, 1f);
        clockRect.anchoredPosition = ((RectTransform)objectiveText.transform).anchoredPosition + new Vector2(0f, -48f);
        clockRect.sizeDelta = new Vector2(900f, 34f);
        clock.alignment = TextAlignmentOptions.TopLeft;
        clockRect.SetSiblingIndex(objectiveText.transform.GetSiblingIndex() + 1);
        DayClock day = systems.AddComponent<DayClock>();
        H.Set(day, "tracker", tracker);
        H.Set(day, "clockText", clock);
        H.SetFloat(day, "totalSeconds", 540f);
        H.Set(day, "lateClip", H.Sfx("error_004"));

        // --- Minijuegos y fundido ---
        ExamMinigame exam1 = CreateExam(ui, systems, player, "MinijuegoExamenProgramacion", "examen1", "Programación", new[]
        {
            ("¿Qué estructura repite instrucciones mientras se cumple una condición?", new[] { "while", "if", "print" }, 0),
            ("Un arreglo guarda ___.", new[] { "un solo valor", "varios valores", "solo texto" }, 1),
            ("¿Qué operador compara si dos valores son iguales?", new[] { "=", "==", "+=" }, 1),
            ("Una función sirve para ___.", new[] { "borrar variables", "reutilizar código", "apagar el equipo" }, 1),
            ("Escribir mal una instrucción produce un error de ___.", new[] { "sintaxis", "lógica", "red" }, 0),
            ("¿Qué tipo de dato es verdadero o falso?", new[] { "entero", "booleano", "texto" }, 1)
        });
        ExamMinigame exam2 = CreateExam(ui, systems, player, "MinijuegoExamenFisiologia", "examen2", "Fisiología", new[]
        {
            ("La unidad funcional del riñón es la ___.", new[] { "neurona", "nefrona", "alvéolo" }, 1),
            ("El intercambio de gases ocurre en los ___.", new[] { "alvéolos", "bronquios", "riñones" }, 0),
            ("La insulina la produce el ___.", new[] { "hígado", "páncreas", "estómago" }, 1),
            ("La sangre oxigenada vuelve al corazón por las venas ___.", new[] { "cavas", "pulmonares", "yugulares" }, 1),
            ("La frecuencia cardiaca en reposo es de ___ latidos por minuto.", new[] { "20 a 40", "60 a 100", "140 a 180" }, 1),
            ("La hormona del estrés es el ___.", new[] { "cortisol", "glucagón", "estrógeno" }, 0)
        });
        ExamMinigame exam3 = CreateExam(ui, systems, player, "MinijuegoExamenAnatomia", "examen3", "Anatomía", new[]
        {
            ("¿Cuántas vértebras cervicales tiene el ser humano?", new[] { "5", "7", "12" }, 1),
            ("El único hueso móvil del cráneo es la ___.", new[] { "maxila", "mandíbula", "órbita" }, 1),
            ("La clavícula une el esternón con la ___.", new[] { "escápula", "costilla", "pelvis" }, 0),
            ("El músculo más grande del cuerpo es el ___.", new[] { "glúteo mayor", "bíceps", "deltoides" }, 0),
            ("El órgano más grande del cuerpo es la ___.", new[] { "piel", "hígado", "pulmón" }, 0),
            ("El fémur se articula con la cadera en el ___.", new[] { "acetábulo", "codo", "tobillo" }, 0)
        });
        PriorityMinigame work = CreateWorkMinigame(ui, systems, player);
        BreathingMinigame breathing = H.CreateBreathingMinigame(ui, systems, player, "RESPIRA CON CAMILA");
        H.SetString(breathing, "objectiveId", "crisis");
        H.SetFloat(breathing, "inhaleSeconds", 2.5f);
        H.SetFloat(breathing, "exhaleSeconds", 3.5f);
        H.SetFloat(breathing, "maxRelief", 30f);
        var breathingSo = new SerializedObject(breathing);
        breathingSo.FindProperty("cycles").intValue = 6;
        breathingSo.ApplyModifiedPropertiesWithoutUndo();
        CanvasGroup fade = H.CreateFade(ui);

        // --- Pantalla final: resumen del semestre (el juego termina, solo vuelve al menú) ---
        Transform win = H.FindDeep(ui, "Victoria");
        H.FindDeep(win, "Titulo").GetComponent<TMP_Text>().text = "¡SEMESTRE COMPLETADO!";
        TMP_Text message = H.FindDeep(win, "Mensaje").GetComponent<TMP_Text>();
        message.text = "Terminaste tu primer semestre en UNIFRANZ.";
        // El resumen ocupa varias líneas: título arriba, resumen al medio y el botón abajo
        var titleRect = (RectTransform)H.FindDeep(win, "Titulo");
        titleRect.anchoredPosition = new Vector2(titleRect.anchoredPosition.x, 330f);
        var messageRect = (RectTransform)message.transform;
        messageRect.anchoredPosition = new Vector2(messageRect.anchoredPosition.x, 140f);
        messageRect.sizeDelta = new Vector2(Mathf.Max(messageRect.sizeDelta.x, 1500f), 280f);
        var menuRect = (RectTransform)H.FindDeep(win, "Btn_VolverMenu");
        menuRect.anchoredPosition = new Vector2(menuRect.anchoredPosition.x, -120f);
        message.fontSizeMax = message.fontSize;
        message.fontSizeMin = 12f;
        message.enableAutoSizing = true;
        LevelFlow flow = systems.GetComponent<LevelFlow>();
        H.SetString(flow, "nextScene", "");
        SemesterSummary summary = systems.AddComponent<SemesterSummary>();
        H.Set(summary, "tracker", tracker);
        H.Set(summary, "message", message);
        H.SetArray(summary, "exams", new Object[] { exam1, exam2, exam3 });

        // --- Contenido ---
        Transform content = new GameObject("Hito5_Contenido").transform;
        Transform t = player.transform;
        PlayerMovement movement = player.GetComponent<PlayerMovement>();
        string[] firstPay = { "Primero regulariza tu pago en Cajas, en el Mezzanine. Sin eso no puedes rendir." };

        // Recorrido: Mezzanine <-> Piso 1 (ascensor), Piso 1 <-> Piso 2 (gradas),
        // Piso 2 <-> Piso 5 (gradas) y Piso 5 <-> Bienestar (gradas)
        Transform mzArrival = H.CreatePoint(content, "Llegada_Mezzanine", mz.X(0, 0.66f), floorY);
        Transform p1FromMz = H.CreatePoint(content, "Llegada_Piso1_Ascensor", p1.X(0, 0.62f), floorY);
        Transform p1FromP2 = H.CreatePoint(content, "Llegada_Piso1_Gradas", p1.X(0, 0.78f), floorY);
        Transform p2FromP1 = H.CreatePoint(content, "Llegada_Piso2", P2(2, 0.74f), floorY);
        Transform p5Arrival = H.CreatePoint(content, "Llegada_Piso5", p5.X(0, 0.34f), floorY);
        Transform p5FromBs = H.CreatePoint(content, "Llegada_Piso5_Bienestar", p5.X(1, 0.86f), floorY);
        Transform bsArrival = H.CreatePoint(content, "Llegada_Bienestar", bs.X(0, 0.30f), floorY);
        H.CreatePortal(content, "Ascensor_Subir", mz.X(0, 0.59f), floorY, "Ascensor al Piso 1", p1FromMz, p1Bounds, movement, vcam, confiner, fade);
        H.CreatePortal(content, "Ascensor_Bajar", p1.X(0, 0.54f), floorY, "Ascensor al Mezzanine", mzArrival, mzBounds, movement, vcam, confiner, fade);
        H.CreatePortal(content, "Gradas_Piso2", p1.X(0, 0.88f), floorY, "Subir al Piso 2", p2FromP1, p2Bounds, movement, vcam, confiner, fade);
        H.CreatePortal(content, "Gradas_Piso1", P2(2, 0.82f), floorY, "Bajar al Piso 1", p1FromP2, p1Bounds, movement, vcam, confiner, fade);
        H.CreatePortal(content, "Gradas_Piso5", P2(2, 0.93f), floorY, "Subir al Piso 5", p5Arrival, p5Bounds, movement, vcam, confiner, fade);
        H.CreatePortal(content, "Gradas_BajarPiso2", p5.X(0, 0.14f), floorY, "Bajar al Piso 2", p2FromP1, p2Bounds, movement, vcam, confiner, fade);
        H.CreatePortal(content, "Gradas_Bienestar", p5.X(1, 0.94f), floorY, "Subir a Bienestar", bsArrival, bsBounds, movement, vcam, confiner, fade);
        H.CreatePortal(content, "Gradas_BajarPiso5", bs.X(0, 0.12f), floorY, "Bajar al Piso 5", p5FromBs, p5Bounds, movement, vcam, confiner, fade);

        // Mezzanine: fila de Cajas
        GameObject cajera = H.CreatePerson(content, "Cajera", mz.X(1, 0.56f), floorY + H.CashierRise, t, true, false, 4);
        ObjectiveInteractable pay = cajera.AddComponent<ObjectiveInteractable>();
        H.ConfigureObjective(pay, "Hablar", "Sra. Rosa - Cajas", "pago", -5f,
            new[]
            {
                "Hola, mi vida. ¿Vienes a regularizar tu pago para los finales?",
                "A ver... listo, ya estás habilitad@ para rendir.",
                "Tus exámenes son en el Piso 1, el Piso 2 y el Piso 5. ¡Mucha suerte!"
            },
            new string[0], 0f,
            new[] { "Ya estás habilitad@. ¡Corre a tus exámenes!" });
        H.SetVector(pay, "promptOffset", new Vector2(0f, 3.5f));
        CreateZone(content, mz.X(1, 0.04f), mz.X(1, 0.40f), t, "Valeria", "Andres");

        // Piso 1: examen de Fisiología
        ObjectiveInteractable paredes = H.CreateNpc(content, "DocenteLab", p1.X(1, 0.36f), floorY, t,
            "Dra. Paredes - Examen de Fisiología", "Rendir", "examen2", 0f,
            new[] { "Siéntese. El examen final de Fisiología tiene 6 preguntas.", "Lea con calma. Empiece cuando quiera." },
            firstPay, 3f,
            new[] { "Ya rindió mi examen. ¡Éxitos en los demás!" });
        BeginOnFinish(paredes, exam2);
        H.CreateCrowd(content, p1.X(0, 0.28f), p1.X(0, 0.46f), floorY, t);
        CreateMoodExtra(content, "Kevin", p1.X(2, 0.40f), floorY, t, "Kevin - Ayudante",
            new[] { "¡Semana de finales! Tú puedes, ya llegaste hasta aquí.", "Un examen a la vez, sin pensar en los demás." },
            new[] { "Dicen que este semestre reprueba medio curso...", "¿Ya viste la hora? El día se pasa volando." });

        // Piso 2: examen de Programación en el aula 207
        ObjectiveInteractable ramirez = H.CreateNpc(content, "Docente", P2(1, 0.60f), floorY, t,
            "Lic. Ramírez - Examen de Programación", "Rendir", "examen1", 0f,
            new[] { "Bienvenid@ al examen final de Programación.", "Seis preguntas. Cuando esté list@, empezamos." },
            firstPay, 3f,
            new[] { "Ya entregó su examen. Siga con los demás." });
        BeginOnFinish(ramirez, exam1);
        H.CreateCrowd(content, P2(1, 0.20f), P2(1, 0.46f), floorY, t);
        CreateMoodExtra(content, "Sofia", P2(0, 0.40f), floorY, t, "Sofía - Estudiante",
            new[] { "¡Ya terminé Programación! Era más fácil de lo que pensaba.", "Lee bien cada pregunta, eso es todo." },
            new[] { "Las preguntas cambian de lugar si te pones nervios@...", "Yo casi me quedo en blanco. Casi." });

        // Piso 5: trabajo final en la biblioteca, crisis y examen de Anatomía
        ObjectiveInteractable elena = H.CreateNpc(content, "Bibliotecaria", p5.X(0, 0.62f), floorY, t,
            "Sra. Elena - Bibliotecaria", "Hablar", "trabajo", 0f,
            new[]
            {
                "¿Vienes a imprimir tu trabajo final? Antes ordena lo que te falta.",
                "Haz primero lo que vence antes, como en la mitad del semestre."
            },
            firstPay, 2f,
            new[] { "Tu trabajo ya quedó impreso y entregado. ¡Bien hecho!" });
        BeginOnFinish(elena, work);

        ObjectiveInteractable camila = H.CreateNpc(content, "Camila", p5.X(1, 0.24f), floorY, t,
            "Camila - Compañera", "Hablar", "crisis", 0f,
            new[]
            {
                "Oye... estás temblando. ¿Estás bien?",
                "Tú me ayudaste en la Sala de Simulación. Ahora me toca a mí.",
                "Respira conmigo antes del último examen. Despacio."
            },
            new[] { "¡Hola! Primero regulariza tu pago en Cajas, en el Mezzanine." }, 1f,
            new[] { "Ya estás mejor. ¡Vamos, que tú puedes!" });
        BeginOnFinish(camila, breathing);

        ObjectiveInteractable quispe = H.CreateNpc(content, "DocenteAnatomia", p5.X(1, 0.50f), floorY, t,
            "Dr. Quispe - Examen de Anatomía", "Rendir", "examen3", 0f,
            new[] { "El examen final de Anatomía es aquí, en el Aula Dinámica.", "Seis preguntas. Respire hondo y empiece." },
            firstPay, 3f,
            new[] { "Examen entregado. Ya puede descansar." });
        BeginOnFinish(quispe, exam3);
        H.CreateRestPoint(content, p5.X(1, 0.78f), floorY);
        CreateZone(content, p5.X(0, 0.30f), p5.X(0, 0.50f), t, "Marco");

        // Bienestar Estudiantil: final del semestre
        H.CreateNpc(content, "GuiaBienestar", bs.X(0, 0.56f), floorY, t,
            "Lucía - Guía de Bienestar", "Hablar", "bienestar", -100f,
            new[]
            {
                "¡Llegaste! Te estábamos esperando.",
                "Rendiste tus finales, entregaste tu trabajo y ayudaste a tus compañeros.",
                "Al principio no sabías ni dónde quedaba Cajas... y mírate ahora.",
                "Estresarse es normal. Lo importante es saber pedir ayuda y cuidarse.",
                "Terminaste tu primer semestre. ¡Bienvenid@ de verdad a UNIFRANZ!"
            },
            new[]
            {
                "¡Hola! Todavía tienes pendientes de los finales.",
                "Termínalos y vuelve: aquí te esperamos para celebrar."
            }, 0f,
            new[] { "¡Felicidades otra vez!" });
        H.CreateExtra(content, "Companero", bs.X(0, 0.40f), floorY, t, false, "Diego - Compañero", -3f, new[]
        {
            "¡Lo logramos! Primer semestre terminado.",
            "Gracias por ayudarme en el Anfiteatro. Sin eso no la contaba."
        });
        H.CreateExtra(content, "Kevin", bs.X(0, 0.72f), floorY, t, true, "Kevin - Ayudante", -3f, new[]
        {
            "¡Felicidades, nuev@! Bueno... ya no eres tan nuev@.",
            "El próximo semestre tú vas a ayudar a los que recién llegan."
        });
        H.CreateExtra(content, "Docente", bs.X(1, 0.64f), floorY, t, true, "Lic. Ramírez - Docente", -3f, new[]
        {
            "Lo vi llegar el primer día al Auditorio, muy perdid@.",
            "Hoy terminó su semestre. Estoy orgulloso. Nos vemos el próximo."
        });

        EditorSceneManager.SaveScene(scene);
        UpdateBuildSettings();
        AssetDatabase.SaveAssets();
        Debug.Log("[Hito5SceneBuilder] Listo: " + ScenePath);
    }

    // ================================================================
    // Contenido
    // ================================================================

    private static PolygonCollider2D CreateBounds(string name, H.Floor floor)
    {
        PolygonCollider2D bounds = new GameObject(name).AddComponent<PolygonCollider2D>();
        bounds.isTrigger = true;
        H.SetBounds(bounds, floor);
        return bounds;
    }

    // El diálogo del NPC no completa el objetivo: abre el minijuego, que lo completa
    private static void BeginOnFinish(ObjectiveInteractable item, MinigameBase game)
    {
        H.SetBool(item, "completesObjective", false);
        UnityEditor.Events.UnityEventTools.AddPersistentListener(H.GetEvent(item, typeof(Interactable), "onFinished"), game.Begin);
    }

    private static void BeginOnFinish(ObjectiveInteractable item, BreathingMinigame game)
    {
        H.SetBool(item, "completesObjective", false);
        UnityEditor.Events.UnityEventTools.AddPersistentListener(H.GetEvent(item, typeof(Interactable), "onFinished"), game.Begin);
    }

    private static void CreateMoodExtra(Transform parent, string sheet, float x, float floorY, Transform player,
        string speaker, string[] lines, string[] linesAnxious)
    {
        GameObject npc = H.CreatePerson(parent, sheet, x, floorY, player);
        StressAwareInteractable item = npc.AddComponent<StressAwareInteractable>();
        H.ConfigureInteractable(item, "Hablar", speaker, lines, -2f);
        H.SetStrings(item, "linesAnxious", linesAnxious);
    }

    // Zona de aglomeración con gente de fondo
    private static void CreateZone(Transform parent, float start, float end, Transform player, params string[] people)
    {
        var zoneGo = new GameObject("ZonaAglomeracion");
        zoneGo.transform.SetParent(parent);
        StressZone zone = zoneGo.AddComponent<StressZone>();
        H.Set(zone, "player", player);
        H.SetFloat(zone, "minX", start);
        H.SetFloat(zone, "maxX", end);
        H.SetFloat(zone, "stressPerSecond", 3f);
        float floorY = player.position.y - 0.05f;
        for (int i = 0; i < people.Length; i++)
        {
            float x = Mathf.Lerp(start, end, (i + 1f) / (people.Length + 1f));
            H.CreatePerson(parent, people[i], x, floorY, player, false, i % 2 == 1, 4 + i % 3);
        }
    }

    // ================================================================
    // Minijuegos
    // ================================================================

    private static ExamMinigame CreateExam(Transform ui, GameObject systems, GameObject player, string panelName,
        string objectiveId, string subject, (string prompt, string[] options, int correct)[] questions)
    {
        RectTransform panel = H2.CreatePanel(ui, panelName, "EXAMEN FINAL - " + subject.ToUpper());
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(H.FontPath);
        TextMeshProUGUI prompt = H.CreateLabel(panel, "Pregunta", font, "", 24f, TextLight, 210f);
        prompt.rectTransform.sizeDelta = new Vector2(1600f, 120f);
        prompt.lineSpacing = 20f;
        Object[] boxes = H3.CreateBoxes(panel, "Opcion_", 3, 440f, new Vector2(400f, 110f), 20f, 22f, out Object[] texts);
        Image fill = H3.CreateTimerBar(panel, -110f);
        TextMeshProUGUI feedback = H.CreateLabel(panel, "Mensaje", font, "", 20f, TextLight, -200f);
        TextMeshProUGUI progress = H.CreateLabel(panel, "Progreso", font, "", 18f, Orange, -270f);

        ExamMinigame game = systems.AddComponent<ExamMinigame>();
        H2.ConfigureMinigame(game, panel.gameObject, player, objectiveId);
        H.SetString(game, "subject", subject);
        H.Set(game, "promptText", prompt);
        H.SetArray(game, "optionBoxes", boxes);
        H.SetArray(game, "optionTexts", texts);
        H.Set(game, "timerFill", fill);
        H.Set(game, "feedbackText", feedback);
        H.Set(game, "progressText", progress);

        var so = new SerializedObject(game);
        SerializedProperty array = so.FindProperty("questions");
        array.arraySize = questions.Length;
        for (int i = 0; i < questions.Length; i++)
        {
            SerializedProperty item = array.GetArrayElementAtIndex(i);
            item.FindPropertyRelative("prompt").stringValue = questions[i].prompt;
            item.FindPropertyRelative("correct").intValue = questions[i].correct;
            SerializedProperty options = item.FindPropertyRelative("options");
            options.arraySize = questions[i].options.Length;
            for (int k = 0; k < questions[i].options.Length; k++)
            {
                options.GetArrayElementAtIndex(k).stringValue = questions[i].options[k];
            }
        }
        so.ApplyModifiedPropertiesWithoutUndo();
        return game;
    }

    // Prioriza tus tareas más difícil (D5): seis pendientes de fin de semestre
    private static PriorityMinigame CreateWorkMinigame(Transform ui, GameObject systems, GameObject player)
    {
        RectTransform panel = H2.CreatePanel(ui, "MinijuegoTrabajoFinal", "TRABAJO FINAL: ORDENA LO QUE FALTA");
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(H.FontPath);
        TextMeshProUGUI prompt = H.CreateLabel(panel, "Indicacion", font, "", 24f, TextLight, 260f);
        Object[] cards = H3.CreateBoxes(panel, "Tarea_", 6, 300f, new Vector2(280f, 250f), 10f, 15f, out Object[] texts);
        TextMeshProUGUI feedback = H.CreateLabel(panel, "Mensaje", font, "", 20f, TextLight, -200f);
        TextMeshProUGUI progress = H.CreateLabel(panel, "Progreso", font, "", 18f, Orange, -270f);

        PriorityMinigame game = systems.AddComponent<PriorityMinigame>();
        H2.ConfigureMinigame(game, panel.gameObject, player, "trabajo");
        H.SetArray(game, "cards", cards);
        H.SetArray(game, "cardTexts", texts);
        H.Set(game, "promptText", prompt);
        H.Set(game, "feedbackText", feedback);
        H.Set(game, "progressText", progress);
        H.SetFloat(game, "stressPerError", 4f);
        var so = new SerializedObject(game);
        SerializedProperty array = so.FindProperty("tasks");
        var tasks = new[]
        {
            ("Devolver libros", 4), ("Entregar trabajo final", 1), ("Ensayo de Inglés", 5),
            ("Estudiar Anatomía", 2), ("Pagar inscripción", 6), ("Repasar Fisiología", 3)
        };
        array.arraySize = tasks.Length;
        for (int i = 0; i < tasks.Length; i++)
        {
            SerializedProperty item = array.GetArrayElementAtIndex(i);
            item.FindPropertyRelative("name").stringValue = tasks[i].Item1;
            item.FindPropertyRelative("daysLeft").intValue = tasks[i].Item2;
        }
        so.ApplyModifiedPropertiesWithoutUndo();
        return game;
    }

    // ================================================================
    // Datos, sprites y escenas
    // ================================================================

    private static void SetAnyOrder(ObjectiveTracker tracker, params string[] ids)
    {
        var so = new SerializedObject(tracker);
        SerializedProperty array = so.FindProperty("objectives");
        for (int i = 0; i < array.arraySize; i++)
        {
            SerializedProperty item = array.GetArrayElementAtIndex(i);
            item.FindPropertyRelative("anyOrder").boolValue = ids.Contains(item.FindPropertyRelative("id").stringValue);
        }
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void PrepareSprites()
    {
        H.PrepareSprites();
        foreach (string sheet in new[] { "DocenteLab", "Bibliotecaria", "Camila", "DocenteAnatomia" })
        {
            H.ImportPixelArt(H.NpcPath + sheet + "_Respirar.png", 8);
            H.ImportPixelArt(H.NpcPath + sheet + "_Caminar.png", 8);
        }
        foreach (var (floorName, blocks) in new[] { ("Piso1", Piso1Blocks), ("Piso5", Piso5Blocks), ("Bienestar", BienestarBlocks) })
        {
            foreach (string block in blocks)
            {
                foreach (string suffix in new[] { "", "_estres", "_ansiedad" })
                {
                    H.ImportBackground("Assets/Sprites/Escenario/" + floorName + "/" + floorName.ToLower() + "_" + block + suffix + ".png");
                }
            }
        }
    }

    // Orden: menú, Hito 1 a Hito 5 y luego el resto que ya hubiera
    private static void UpdateBuildSettings()
    {
        var scenes = new List<EditorBuildSettingsScene> { new EditorBuildSettingsScene(MenuScenePath, true) };
        scenes.AddRange(PreviousScenes.Select(path => new EditorBuildSettingsScene(path, true)));
        scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
        foreach (EditorBuildSettingsScene existing in EditorBuildSettings.scenes)
        {
            if (scenes.All(s => s.path != existing.path))
            {
                scenes.Add(existing);
            }
        }
        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
