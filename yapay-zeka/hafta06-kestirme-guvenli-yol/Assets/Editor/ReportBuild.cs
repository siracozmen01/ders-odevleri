// File: Assets/Editor/ReportBuild.cs
// Builds a temporary Windows player for the report harness (Assets/Scripts/Dev/ReportCapture.cs).
//   -executeMethod ReportBuild.Build -buildOutput <folder>
using System;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class ReportBuild
{
    public static void Build()
    {
        string[] args = Environment.GetCommandLineArgs();
        int i = Array.IndexOf(args, "-buildOutput");
        string dir = i >= 0 && i + 1 < args.Length ? args[i + 1] : "Builds/Report";
        var options = new BuildPlayerOptions
        {
            scenes = new[] { Hafta6SceneBuilder.ScenePath },
            locationPathName = System.IO.Path.Combine(dir, "Hafta6.exe"),
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None,
        };
        BuildReport report = BuildPipeline.BuildPlayer(options);
        Debug.Log($"[ReportBuild] {report.summary.result}, {report.summary.totalErrors} errors -> {options.locationPathName}");
        if (report.summary.result != BuildResult.Succeeded) EditorApplication.Exit(1);
    }
}
