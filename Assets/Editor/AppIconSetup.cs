/*
 * License: 5G-MAG Public License (v1.0)
 * Author: Jordi J. Gimenez (5G-MAG)
 * Copyright: (C) 2026 5G-MAG Association
 *
 * For full license terms please see the LICENSE file distributed with this
 * program. If this file is missing then the license can be retrieved from
 * https://www.5g-mag.com/license.
 */

using UnityEditor;
using UnityEditor.Android;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

// Gives the Android app the launcher icon of the 5G-MAG apps: the brand blue (#00a0d2) with the 5G-MAG mark as supplied in the
// logo file and the app name under it, as in the DVB-I and 5GMSd Aware Android applications. The pictures are in
// Assets/Editor/AppIcons; the icon settings are applied at every Android build.
public class AppIconSetup : IPreprocessBuildWithReport
{
    public int callbackOrder { get { return 0; } }

    public void OnPreprocessBuild(BuildReport report)
    {
        if (report.summary.platform == BuildTarget.Android) Apply();
    }

    private static Texture2D Load(string name)
    {
        return AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Editor/AppIcons/" + name + ".png");
    }

    public static void Apply()
    {
        var background = Load("icon_background");
        var foreground = Load("icon_foreground");
        var legacy = Load("icon_legacy");
        var round = Load("icon_round");
        if (background == null || foreground == null || legacy == null || round == null) { Debug.LogWarning("AppIconSetup: icon pictures not found"); return; }
        PlayerSettings.productName = "XR Scene Player";
        var target = NamedBuildTarget.Android;
        Set(target, AndroidPlatformIconKind.Adaptive, background, foreground);
        Set(target, AndroidPlatformIconKind.Round, round);
        Set(target, AndroidPlatformIconKind.Legacy, legacy);
        AssetDatabase.SaveAssets();
        Debug.Log("AppIconSetup: launcher icon set");
    }

    private static void Set(NamedBuildTarget target, PlatformIconKind kind, params Texture2D[] textures)
    {
        var icons = PlayerSettings.GetPlatformIcons(target, kind);
        foreach (var icon in icons) icon.SetTextures(textures);
        PlayerSettings.SetPlatformIcons(target, kind, icons);
    }
}
