using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Herramienta de Editor que agrega al Menú Principal el selector de hitos (1 a 5).
// No regenera el menú: abre la escena actual y arma el panel "ElegirNivel" copiando el
// estilo de los paneles y botones que ya existen (fondo y título de "ElegirPersonaje",
// botones del menú lateral). Se puede volver a ejecutar: reemplaza el panel anterior.
// Menú: Tools > UNIFRANZ-DISORIENTED. En modo batch:
//   Unity -batchmode -quit -projectPath . -executeMethod LevelSelectMenuBuilder.Build
public static class LevelSelectMenuBuilder
{
    private const string ScenePath = "Assets/Scenes/MainMenu.unity";
    private const string PanelName = "ElegirNivel";

    private static readonly (string scene, string label)[] Levels =
    {
        ("Hito1", "HITO 1 - INDUCCIÓN"),
        ("Hito2", "HITO 2 - PRIMERAS CLASES"),
        ("Hito3", "HITO 3 - MITAD DEL SEMESTRE"),
        ("Hito4", "HITO 4 - PARCIALES"),
        ("Hito5", "HITO 5 - EXÁMENES FINALES")
    };

    [MenuItem("Tools/UNIFRANZ-DISORIENTED/Agregar selector de niveles al menú")]
    public static void Build()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        CharacterSelectPanel characterSelect = Find<CharacterSelectPanel>(scene);
        MainMenuController controller = Find<MainMenuController>(scene);
        Transform menuButtons = FindDeep(controller.transform.root, "Buttons");
        if (characterSelect == null || controller == null || menuButtons == null)
        {
            Debug.LogError("[LevelSelectMenuBuilder] Falta la elección de personaje, el controlador o los botones del menú");
            return;
        }

        // Reemplaza el panel de una ejecución anterior
        Transform old = characterSelect.transform.parent.Find(PanelName);
        if (old != null)
        {
            Object.DestroyImmediate(old.gameObject);
        }

        // Panel: copia de "ElegirPersonaje" con solo su título y su botón Atrás
        GameObject panel = Object.Instantiate(characterSelect.gameObject, characterSelect.transform.parent);
        panel.name = PanelName;
        panel.transform.SetSiblingIndex(characterSelect.transform.GetSiblingIndex() + 1);
        Object.DestroyImmediate(panel.GetComponent<CharacterSelectPanel>());
        foreach (Transform child in panel.transform.Cast<Transform>().ToArray())
        {
            if (child.name != "Titulo" && child.name != "Btn_Atras")
            {
                Object.DestroyImmediate(child.gameObject);
            }
        }
        TMP_Text title = panel.transform.Find("Titulo").GetComponentInChildren<TMP_Text>(true);
        if (title != null)
        {
            title.text = "ELIGE EL HITO";
        }

        // Botones: copia del grupo de botones del menú lateral con un botón por hito
        GameObject group = Object.Instantiate(menuButtons.gameObject, panel.transform);
        group.name = "Hitos";
        Transform template = group.transform.GetChild(0);
        foreach (Transform child in group.transform.Cast<Transform>().Skip(1).ToArray())
        {
            Object.DestroyImmediate(child.gameObject);
        }
        // El alto de cada botón lo da su LayoutElement (el layout del grupo lo aplica)
        var groupRect = (RectTransform)group.transform;
        var element = template.GetComponent<LayoutElement>();
        float buttonHeight = element != null && element.preferredHeight > 0f ? element.preferredHeight : 72f;
        groupRect.anchorMin = groupRect.anchorMax = groupRect.pivot = new Vector2(0.5f, 0.5f);
        groupRect.anchoredPosition = new Vector2(0f, -10f);
        var layout = group.GetComponent<VerticalLayoutGroup>();
        float spacing = layout != null ? layout.spacing : 14f;
        groupRect.sizeDelta = new Vector2(640f, Levels.Length * buttonHeight + (Levels.Length - 1) * spacing);

        var buttons = new Button[Levels.Length];
        for (int i = 0; i < Levels.Length; i++)
        {
            Transform item = i == 0 ? template : Object.Instantiate(template.gameObject, group.transform).transform;
            item.name = "Btn_" + Levels[i].scene;
            if (layout == null)
            {
                ((RectTransform)item).anchoredPosition = new Vector2(0f, (Levels.Length * 0.5f - i - 0.5f) * (buttonHeight + spacing));
            }
            TMP_Text label = item.GetComponentInChildren<TMP_Text>(true);
            label.text = Levels[i].label;
            label.enableAutoSizing = true;
            label.fontSizeMax = label.fontSize;
            label.fontSizeMin = 10f;
            buttons[i] = item.GetComponent<Button>();
        }

        // Componente del selector
        LevelSelectPanel levelSelect = panel.AddComponent<LevelSelectPanel>();
        var so = new SerializedObject(levelSelect);
        SerializedProperty levels = so.FindProperty("levels");
        levels.arraySize = Levels.Length;
        for (int i = 0; i < Levels.Length; i++)
        {
            SerializedProperty level = levels.GetArrayElementAtIndex(i);
            level.FindPropertyRelative("button").objectReferenceValue = buttons[i];
            level.FindPropertyRelative("scene").stringValue = Levels[i].scene;
        }
        so.FindProperty("backButton").objectReferenceValue = panel.transform.Find("Btn_Atras").GetComponent<Button>();
        so.ApplyModifiedPropertiesWithoutUndo();
        panel.SetActive(false);

        // Elegir personaje abre el selector; Escape lo cierra desde el controlador
        SetReference(characterSelect, "levelSelect", levelSelect);
        SetReference(controller, "levelSelect", levelSelect);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[LevelSelectMenuBuilder] Listo: selector de " + Levels.Length + " hitos en " + ScenePath);
    }

    private static T Find<T>(Scene scene) where T : Component
    {
        return scene.GetRootGameObjects().Select(r => r.GetComponentInChildren<T>(true)).FirstOrDefault(c => c != null);
    }

    private static Transform FindDeep(Transform parent, string name)
    {
        return parent.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == name);
    }

    private static void SetReference(Object target, string field, Object value)
    {
        var so = new SerializedObject(target);
        so.FindProperty(field).objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
