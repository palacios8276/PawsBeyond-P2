using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Build.Reporting;

// Editor-only helper executed through Unity Pipeline run_script, outside Assets.
public static class BuildP1
{
    public static string Main()
    {
        if (Application.isPlaying) throw new Exception("Exit Play before building");
        const string scene = "Assets/Scenes/Playground.unity";
        if (!AssetDatabase.LoadAssetAtPath<SceneAsset>(scene)) throw new Exception("Missing P1 scene");
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(scene, true) };
        string folder = Path.GetFullPath("Builds/PawsBeyond-P1");
        Directory.CreateDirectory(folder);
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { scene },
            locationPathName = Path.Combine(folder, "PawsBeyond.exe"),
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        });
        var summary = report.summary;
        var messages = report.steps.SelectMany(step => step.messages)
            .Where(message => message.type == LogType.Error || message.type == LogType.Warning)
            .Select(message => new { level = message.type.ToString(), message.content }).ToArray();
        string json = Newtonsoft.Json.JsonConvert.SerializeObject(new
        {
            scene, result = summary.result.ToString(), target = summary.platform.ToString(),
            output = summary.outputPath, errors = summary.totalErrors, warnings = summary.totalWarnings,
            bytes = summary.totalSize, seconds = summary.totalTime.TotalSeconds, messages
        }, Newtonsoft.Json.Formatting.Indented);
        File.WriteAllText(Path.Combine(folder, "BuildReport.json"), json);
        if (summary.result != BuildResult.Succeeded) throw new Exception("P1 build failed: " + json);
        return json;
    }
}
