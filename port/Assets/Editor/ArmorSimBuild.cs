using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class ArmorSimBuild
{
    public static void BuildiOS()
    {
        string[] scenes = EditorBuildSettings.scenes
            .Where(scene => scene.enabled)
            .Select(scene => scene.path)
            .ToArray();

        PlayerSettings.companyName = "GalimovDynamics";
        PlayerSettings.productName = "ArmorSim";
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.IOS, ScriptingImplementation.IL2CPP);
        PlayerSettings.SetArchitecture(NamedBuildTarget.IOS, 1);
        PlayerSettings.iOS.sdkVersion = iOSSdkVersion.DeviceSDK;
        PlayerSettings.iOS.targetOSVersionString = "15.0";
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
        PlayerSettings.allowedAutorotateToPortrait = false;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.allowedAutorotateToLandscapeRight = true;
        PlayerSettings.allowedAutorotateToLandscapeLeft = true;

        if (!EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.iOS, BuildTarget.iOS))
        {
            throw new Exception("Не удалось переключиться на iOS. Проверь установку iOS Build Support.");
        }

        string output = Environment.GetEnvironmentVariable("ARMORSIM_XCODE_PATH");
        if (string.IsNullOrEmpty(output))
        {
            output = Path.GetFullPath("Build/Xcode");
        }
        Directory.CreateDirectory(output);

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = output,
            target = BuildTarget.iOS,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result != BuildResult.Succeeded)
        {
            throw new Exception($"iOS export failed: {report.summary.result}, errors={report.summary.totalErrors}");
        }

        Debug.Log($"[ArmorSimBuild] Xcode project exported to {output}");
    }
}
