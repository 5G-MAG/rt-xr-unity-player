/*
 * License: 5G-MAG Public License (v1.0)
 * Copyright: (C) 2026 5G-MAG Association
 *
 * For full license terms please see the LICENSE file distributed with this
 * program. If this file is missing then the license can be retrieved from
 * https://www.5g-mag.com/license.
 */

using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

// Command-line build of the Android phone app (ARCore, MobileXR scene), for tools/build-android.sh:
//
//   Unity -batchmode -quit -projectPath . -buildTarget Android -executeMethod BuildCli.BuildAndroid \
//         [-buildOutput Build/mobilexr/rt-xr-unity-player.apk] [-development]
//
// The scene is given here, so the build does not depend on the Scenes In Build list saved in the project. The editor exits with
// code 0 when the build succeeds and 1 otherwise.
public static class BuildCli
{
    private const string Scene = "Assets/Scenes/MobileXR.unity";
    private const string DefaultOutput = "Build/mobilexr/rt-xr-unity-player.apk";

    public static void BuildAndroid()
    {
        int code = 1;
        try
        {
            string output = Arg("-buildOutput") ?? DefaultOutput;
            bool development = HasArg("-development");

            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android &&
                !EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android))
            {
                Debug.LogError("BuildCli: could not switch the active build target to Android");
                return;
            }
            string dir = Path.GetDirectoryName(Path.GetFullPath(output));
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

            var options = new BuildPlayerOptions
            {
                scenes = new[] { Scene },
                locationPathName = output,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = development ? BuildOptions.Development : BuildOptions.None,
            };
            Debug.Log("BuildCli: building " + Scene + " to " + output + (development ? " (development)" : ""));
            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary s = report.summary;
            long apk = File.Exists(output) ? new FileInfo(output).Length : 0;
            Debug.Log("BuildCli: " + s.result + ", APK " + (apk / (1024f * 1024f)).ToString("0.0") + " MB, " +
                      s.totalTime.TotalSeconds.ToString("0") + " s, " + s.totalErrors + " errors, " + s.totalWarnings + " warnings");
            if (s.result == BuildResult.Succeeded) code = 0;
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
        }
        finally
        {
            EditorApplication.Exit(code);
        }
    }

    private static string Arg(string name)
    {
        string[] args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++)
            if (args[i] == name) return args[i + 1];
        return null;
    }

    private static bool HasArg(string name)
    {
        return Array.IndexOf(Environment.GetCommandLineArgs(), name) >= 0;
    }
}
