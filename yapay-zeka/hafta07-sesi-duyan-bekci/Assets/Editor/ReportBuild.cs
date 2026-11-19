// File: Assets/Editor/ReportBuild.cs
// Builds a temporary macOS player for the test harness (Assets/Scripts/Dev/Hafta7Capture.cs).
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
            scenes = new[] { Hafta7SceneBuilder.ScenePath },
            locationPathName = System.IO.Path.Combine(dir, "Hafta7.app"),
            target = BuildTarget.StandaloneOSX,
            options = BuildOptions.None,
        };
        BuildReport report = BuildPipeline.BuildPlayer(options);
        Debug.Log($"[ReportBuild] {report.summary.result}, {report.summary.totalErrors} errors -> {options.locationPathName}");
        if (report.summary.result != BuildResult.Succeeded) EditorApplication.Exit(1);
    }
}
