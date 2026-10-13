/*
 * License: 5G-MAG Public License (v1.0)
 * Author: Jordi J. Gimenez (5G-MAG)
 * Copyright: (C) 2026 5G-MAG Association
 *
 * For full license terms please see the LICENSE file distributed with this
 * program. If this file is missing then the license can be retrieved from
 * https://www.5g-mag.com/license.
 */
using System.IO;
using Google.XR.ARCoreExtensions.Internal;
using UnityEditor;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features.Interactions;
using UnityEngine.XR.OpenXR.Features.MetaQuestSupport;

// Sets the Android build up for Meta Quest: OpenXR instead of ARCore as the XR loader, the Meta Quest feature and
// the Touch controller profiles, and Vulkan. Batch mode: Unity -executeMethod QuestSetup.Run
public static class QuestSetup
{
    public static void Run()
    {
        // OpenXR project validation rejects gamma colour space on OpenGLES and wants Vulkan for the buffer-discard setting
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.Vulkan });
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel29;

        XRGeneralSettingsPerBuildTarget perBuild;
        if (!EditorBuildSettings.TryGetConfigObject(XRGeneralSettings.k_SettingsKey, out perBuild) || perBuild == null)
        {
            perBuild = ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();
            Directory.CreateDirectory("Assets/XR");
            AssetDatabase.CreateAsset(perBuild, "Assets/XR/XRGeneralSettingsPerBuildTarget.asset");
            EditorBuildSettings.AddConfigObject(XRGeneralSettings.k_SettingsKey, perBuild, true);
        }

        const BuildTargetGroup Android = BuildTargetGroup.Android;
        if (!perBuild.HasSettingsForBuildTarget(Android)) perBuild.CreateDefaultSettingsForBuildTarget(Android);
        if (!perBuild.HasManagerSettingsForBuildTarget(Android)) perBuild.CreateDefaultManagerSettingsForBuildTarget(Android);
        perBuild.SettingsForBuildTarget(Android).InitManagerOnStart = true;
        var manager = perBuild.ManagerSettingsForBuildTarget(Android);
        XRPackageMetadataStore.RemoveLoader(manager, "UnityEngine.XR.ARCore.ARCoreLoader", Android);
        bool assigned = XRPackageMetadataStore.AssignLoader(manager, "UnityEngine.XR.OpenXR.OpenXRLoader", Android);
        Debug.Log($"QuestSetup: OpenXR loader assigned={assigned}");
        EditorUtility.SetDirty(perBuild);
        AssetDatabase.SaveAssets();

        UnityEditor.XR.OpenXR.Features.FeatureHelpers.RefreshFeatures(Android);   // creates the feature objects
        var xr = OpenXRSettings.GetSettingsForBuildTargetGroup(Android);
        if (xr == null)
        {
            Debug.LogError("QuestSetup: no OpenXR settings for Android");
            return;
        }
        var quest = xr.GetFeature<MetaQuestFeature>();
        Enable(quest);
        if (quest != null)
        {
            quest.AddTargetDevice("eureka", "Quest 3", true);     // OpenXR 1.10 lists Quest, Quest 2 and Quest Pro only
            quest.AddTargetDevice("panther", "Quest 3S", true);
        }
        DisableTargetDevice(quest, "cambria");   // Quest Pro: it makes the manifest require eye tracking, which other headsets lack
        Enable(xr.GetFeature<OculusTouchControllerProfile>());
        Enable(xr.GetFeature<MetaQuestTouchProControllerProfile>());
        Enable(xr.GetFeature<HandInteractionProfile>());
        EditorUtility.SetDirty(xr);
        AssetDatabase.SaveAssets();

        // a headset has no GPS or camera for the geospatial and cloud-anchor modules, which add location permissions
        // and a required GPS feature to the manifest
        var arcore = ARCoreExtensionsProjectSettings.Instance;
        arcore.GeospatialEnabled = false;
        arcore.GeospatialEditorEnabled = false;
        arcore.AndroidAuthenticationStrategySetting = AndroidAuthenticationStrategy.DoNotUse;
        arcore.Save();
        Debug.Log("QuestSetup: done");
    }

    private static void DisableTargetDevice(MetaQuestFeature feature, string manifestName)
    {
        if (feature == null) return;
        var so = new SerializedObject(feature);
        var devices = so.FindProperty("targetDevices");
        for (int i = 0; devices != null && i < devices.arraySize; i++)
        {
            var d = devices.GetArrayElementAtIndex(i);
            if (d.FindPropertyRelative("manifestName").stringValue == manifestName) d.FindPropertyRelative("enabled").boolValue = false;
        }
        so.ApplyModifiedProperties();
    }

    private static void Enable(UnityEngine.XR.OpenXR.Features.OpenXRFeature feature)
    {
        if (feature == null) { Debug.LogWarning("QuestSetup: a feature was not found"); return; }
        feature.enabled = true;
        EditorUtility.SetDirty(feature);
    }
}
