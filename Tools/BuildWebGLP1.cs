using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

// Run with Unity Pipeline run_script; deliberately outside Assets/player assemblies.
public static class BuildWebGLP1
{
    public static string Main()
    {
        if (Application.isPlaying || EditorUtility.scriptCompilationFailed)
            throw new Exception("Exit Play and resolve compilation errors before building.");
        string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        if (new DirectoryInfo(root).Name != "PawsBeyond-P1")
            throw new Exception("This helper only builds the independent PawsBeyond-P1 project.");
        if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.WebGL, BuildTarget.WebGL))
            throw new Exception("Install WebGL Build Support for this exact Unity version.");
        const string scene = "Assets/Scenes/Playground.unity";
        if (!AssetDatabase.LoadAssetAtPath<SceneAsset>(scene)) throw new Exception("Missing Playground scene.");
        var profile = UnityEditor.Build.Profile.BuildProfile.GetActiveBuildProfile();
        if (profile && profile.overrideGlobalScenes)
            throw new Exception("Active profile overrides scenes. Disable its scene override before building.");
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(scene, true) };
        var target = NamedBuildTarget.WebGL;
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
        PlayerSettings.WebGL.decompressionFallback = true;
        PlayerSettings.WebGL.dataCaching = true;
        PlayerSettings.WebGL.debugSymbolMode = WebGLDebugSymbolMode.Off;
        PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;
        PlayerSettings.WebGL.initialMemorySize = 256;
        PlayerSettings.WebGL.maximumMemorySize = 2048;
        PlayerSettings.WebGL.memoryGrowthMode = WebGLMemoryGrowthMode.Geometric;
        PlayerSettings.WebGL.threadsSupport = false;
        PlayerSettings.WebGL.template = "APPLICATION:Default";
        PlayerSettings.defaultWebScreenWidth = 1280;
        PlayerSettings.defaultWebScreenHeight = 720;
        PlayerSettings.SetScriptingBackend(target, ScriptingImplementation.IL2CPP);
        PlayerSettings.SetApiCompatibilityLevel(target, ApiCompatibilityLevel.NET_Standard);
        PlayerSettings.SetManagedStrippingLevel(target, ManagedStrippingLevel.Low);
        PlayerSettings.stripEngineCode = true;
        UnityEditor.WebGL.UserBuildSettings.codeOptimization = UnityEditor.WebGL.WasmCodeOptimization.DiskSizeLTO;
        EditorUserBuildSettings.development = false;
        EditorUserBuildSettings.allowDebugging = false;
        EditorUserBuildSettings.connectProfiler = false;
        AssetDatabase.SaveAssets();
        // Unique outputs prevent stale files or overwriting a previous delivery.
        string folder = Path.Combine(root, "Builds", "WebGL-P1-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss"));
        Directory.CreateDirectory(folder);
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
            scenes = new[] { scene }, target = BuildTarget.WebGL,
            locationPathName = folder, options = BuildOptions.None
        });
        var s = report.summary;
        string json = Newtonsoft.Json.JsonConvert.SerializeObject(new {
            result = s.result.ToString(), output = folder, errors = s.totalErrors,
            warnings = s.totalWarnings, bytes = s.totalSize, seconds = s.totalTime.TotalSeconds,
            messages = report.steps.SelectMany(x => x.messages)
                .Where(x => x.type == LogType.Error || x.type == LogType.Warning)
                .Select(x => new { level = x.type.ToString(), x.content }).ToArray()
        }, Newtonsoft.Json.Formatting.Indented);
        File.WriteAllText(folder + "-BuildReport.json", json);
        if (s.result != BuildResult.Succeeded) throw new Exception(json);
        if (!File.Exists(Path.Combine(folder, "index.html"))) throw new Exception("Missing index.html.");
        return json;
    }
}
