using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace ARSurvival.EditorTools
{
    /// <summary>Builds the Android APK to Builds/ (git-ignored). Usable from the menu or batch mode.</summary>
    public static class BuildScript
    {
        const string OutputPath = "Builds/AR-Survival-Shooter.apk";

        [MenuItem("Tools/AR Survival/Build Android APK")]
        public static void BuildAndroid()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(OutputPath));
            EditorUserBuildSettings.buildAppBundle = false; // APK, not AAB, for direct install

            var options = new BuildPlayerOptions
            {
                scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
                locationPathName = OutputPath,
                target = BuildTarget.Android,
                options = BuildOptions.None,
            };

            var report = BuildPipeline.BuildPlayer(options);
            var summary = report.summary;
            Debug.Log($"[AR Survival] Build {summary.result}: {summary.outputPath} " +
                      $"({summary.totalSize / (1024f * 1024f):F1} MB, {summary.totalErrors} errors, {summary.totalTime:mm\\:ss})");

            if (Application.isBatchMode && summary.result != BuildResult.Succeeded)
                EditorApplication.Exit(1);
        }
    }
}
