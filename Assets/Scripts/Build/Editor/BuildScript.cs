using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

// Genera los builds de Windows (Standalone) y Android (APK).
// Se usa desde el menú "Build" del Editor o en modo batch:
//   Unity.exe -batchmode -quit -projectPath . -executeMethod BuildScript.BuildWindows
//   Unity.exe -batchmode -quit -projectPath . -executeMethod BuildScript.BuildAndroid
public static class BuildScript
{
    // Carpeta de salida (ignorada por Git en .gitignore)
    private const string BuildsFolder = "Builds";

    [MenuItem("Build/Windows (Standalone)")]
    public static void BuildWindows()
    {
        string path = Path.Combine(BuildsFolder, "Windows", PlayerSettings.productName + ".exe");
        Build(BuildTarget.StandaloneWindows64, BuildTargetGroup.Standalone, path);
    }

    [MenuItem("Build/Android (APK)")]
    public static void BuildAndroid()
    {
        // APK instalable directamente (no AAB de Google Play)
        EditorUserBuildSettings.buildAppBundle = false;
        string fileName = PlayerSettings.productName.Replace(" ", "") + ".apk";
        string path = Path.Combine(BuildsFolder, "Android", fileName);
        Build(BuildTarget.Android, BuildTargetGroup.Android, path);
    }

    private static void Build(BuildTarget target, BuildTargetGroup group, string path)
    {
        // Solo las escenas habilitadas en Build Settings
        string[] scenes = EditorBuildSettings.scenes
            .Where(scene => scene.enabled)
            .Select(scene => scene.path)
            .ToArray();

        if (scenes.Length == 0)
        {
            Fail("No hay escenas habilitadas en Build Settings.");
            return;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(path));

        var options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = path,
            target = target,
            targetGroup = group,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        BuildSummary summary = report.summary;

        if (summary.result == BuildResult.Succeeded)
        {
            Debug.Log($"[BuildScript] Build {target} OK: {path} ({summary.totalSize / (1024 * 1024)} MB)");
        }
        else
        {
            Fail($"Build {target} falló: {summary.result} ({summary.totalErrors} errores)");
        }
    }

    private static void Fail(string message)
    {
        Debug.LogError("[BuildScript] " + message);

        // En modo batch se devuelve código de error para detectar el fallo
        if (Application.isBatchMode)
        {
            EditorApplication.Exit(1);
        }
    }
}
