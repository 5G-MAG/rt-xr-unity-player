/*
 * License: 5G-MAG Public License (v1.0)
 * Author: Jordi J. Gimenez (5G-MAG)
 * Copyright: (C) 2026 5G-MAG Association
 *
 * For full license terms please see the LICENSE file distributed with this
 * program. If this file is missing then the license can be retrieved from
 * https://www.5g-mag.com/license.
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SpatialTracking;
using UnityEngine.XR;

namespace rt.xr.unity
{
    // Makes the player run on a headset: the camera is parented to a rig on the floor and the headset drives its
    // pose. glTF scenes are authored in metres, so the rig is not scaled. Does nothing when no XR device becomes
    // active (phones, desktop).
    public class QuestRig : MonoBehaviour
    {
        public static bool Active { get; private set; }
        public static QuestRig Instance { get; private set; }
        // set by the app while the pointer is over a row that scrolls sideways, so the right stick scrolls instead of turning
        public static bool SuppressTurn;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (FindObjectOfType<SceneViewer>() == null) return;
            // the Meta Quest scene runs its own application
            if (FindObjectOfType<RtXrQuestApplication>() != null) return;
            new GameObject("QuestRig").AddComponent<QuestRig>();
        }

        private const float WalkSpeed = 1.6f;       // metres per second at full stick
        private const float SnapTurnDegrees = 30f;
        private Transform m_rig;
        private Camera m_cam;
        private bool m_turnReady = true;

        public Transform Rig { get { return m_rig; } }
        public Camera Cam { get { return m_cam; } }

        private IEnumerator Start()
        {
            // the headset's session only becomes active once it is worn, which may be after the app has started
            while (!XRSettings.isDeviceActive) yield return new WaitForSeconds(0.5f);
            Active = true;
            var viewer = FindObjectOfType<SceneViewer>();
            m_cam = viewer.GetMainCamera();
            m_rig = new GameObject("XRRig").transform;
            m_cam.transform.SetParent(m_rig, false);
            var driver = m_cam.gameObject.AddComponent<TrackedPoseDriver>();
            driver.SetPoseSource(TrackedPoseDriver.DeviceType.GenericXRDevice, TrackedPoseDriver.TrackedPose.Center);
            driver.trackingType = TrackedPoseDriver.TrackingType.RotationAndPosition;
            driver.updateType = TrackedPoseDriver.UpdateType.UpdateAndBeforeRender;

            var subsystems = new List<XRInputSubsystem>();
            SubsystemManager.GetSubsystems(subsystems);
            foreach (var s in subsystems) s.TrySetTrackingOriginMode(TrackingOriginModeFlags.Floor);

            Instance = this;
            Debug.Log($"QuestRig: device={XRSettings.loadedDeviceName} eye={XRSettings.eyeTextureWidth}x{XRSettings.eyeTextureHeight} refresh={XRDevice.refreshRate}");
        }

        // Left stick walks in the direction the head faces, right stick turns in steps.
        private void Update()
        {
            if (m_rig == null) return;
            Vector2 move, turn;
            Stick(XRNode.LeftHand, out move);
            Stick(XRNode.RightHand, out turn);
            if (SuppressTurn) turn = Vector2.zero;
            if (move.sqrMagnitude > 0.04f)
            {
                Vector3 fwd = Vector3.ProjectOnPlane(m_cam.transform.forward, Vector3.up).normalized;
                Vector3 right = Vector3.ProjectOnPlane(m_cam.transform.right, Vector3.up).normalized;
                m_rig.position += (fwd * move.y + right * move.x) * (WalkSpeed * Time.unscaledDeltaTime);
            }
            if (Mathf.Abs(turn.x) > 0.7f)
            {
                if (m_turnReady) m_rig.RotateAround(m_cam.transform.position, Vector3.up, Mathf.Sign(turn.x) * SnapTurnDegrees);
                m_turnReady = false;
            }
            else if (Mathf.Abs(turn.x) < 0.3f) m_turnReady = true;
        }

        private static bool Stick(XRNode node, out Vector2 value)
        {
            value = Vector2.zero;
            var device = InputDevices.GetDeviceAtXRNode(node);
            return device.isValid && device.TryGetFeatureValue(CommonUsages.primary2DAxis, out value);
        }
    }
}
