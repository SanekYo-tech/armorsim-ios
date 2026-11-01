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
        string bundleId = GetArgument("-armorsimBundleId");
        if (string.IsNullOrWhiteSpace(bundleId))
        {
            bundleId = Environment.GetEnvironmentVariable("IOS_BUNDLE_ID");
        }
        if (string.IsNullOrWhiteSpace(bundleId))
        {
            throw new Exception("Bundle ID не задан: передай -armorsimBundleId <id> или установи переменную окружения IOS_BUNDLE_ID.");
        }
        bundleId = bundleId.Trim();

        string output = GetArgument("-armorsimOutputPath");
        if (string.IsNullOrWhiteSpace(output))
        {
            output = Environment.GetEnvironmentVariable("ARMORSIM_XCODE_PATH");
        }
        if (string.IsNullOrWhiteSpace(output))
        {
            output = Path.GetFullPath("Build/Xcode");
        }
        output = Path.GetFullPath(output);

        ArmorSimProjectSetup.Apply(bundleId);

        string[] scenes = EditorBuildSettings.scenes
            .Where(scene => scene.enabled)
            .Select(scene => scene.path)
            .ToArray();

        PlayerSettings.companyName = "GalimovDynamics";
        PlayerSettings.productName = "ArmorSim";
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.iOS, ScriptingImplementation.IL2CPP);
        PlayerSettings.SetArchitecture(NamedBuildTarget.iOS, 1);
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

    private static string GetArgument(string name)
    {
        string[] args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (!string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string value = args[i + 1];
            if (!string.IsNullOrWhiteSpace(value) && !value.StartsWith("-", StringComparison.Ordinal))
            {
                return value;
            }
        }

        return null;
    }
}
