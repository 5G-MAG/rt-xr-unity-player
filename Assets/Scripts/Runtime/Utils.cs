/*
 * Copyright (c) 2023 MotionSpell
 * Licensed under the License terms and conditions for use, reproduction,
 * and distribution of 5GMAG software (the “License”).
 * You may not use this file except in compliance with the License.
 * You may obtain a copy of the License at https://www.5g-mag.com/license .
 * Unless required by applicable law or agreed to in writing, software distributed under the License is
 * distributed on an “AS IS” BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and limitations under the License.
 */

#nullable enable

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace rt.xr
{

    public class Utils
    {

        public static Bounds ComputeSceneBounds()
        {
            var renderers = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);
            if (renderers == null || renderers.Length == 0)
            {
                return new Bounds(Vector3.zero, Vector3.one);
            }
            Bounds b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                b.Encapsulate(renderers[i].bounds);
            }
            return b;
        }

        public static Bounds ComputeBounds(GameObject go)
        {
            var renderers = go.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                return new Bounds(Vector3.zero, Vector3.one);
            }
            go.GetComponent<Renderer>();
            Bounds b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                b.Encapsulate(renderers[i].bounds);
            }
            return b;
        }

        public static void LookAt(Camera cam, Bounds bounds, Vector3 forward)
        {
            /* 
             * position camera so that scene bounds fit in its vertical FOV
             */
            if (cam.orthographic)
            {
                Debug.LogWarning("Utils.LookAt(): orthographic camera not implemented.");
            }
            float distance = bounds.size.y * 0.5f / Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
            cam.transform.position = bounds.center + Vector3.Project(bounds.size, forward) + forward * distance;
            cam.transform.LookAt(bounds.center);
        }

        /// <summary>
        /// Returns whether any of XR Subsystem is running, meaning that we 
        /// are in XR mode on a XR device
        /// </summary>
        public static bool IsInXrMode()
        {
#if UNITY_ANDROID
            List<XRDisplaySubsystem> xrDisplaySubSystems = new List<XRDisplaySubsystem>();
            SubsystemManager.GetSubsystems(xrDisplaySubSystems);

            for(int i = 0; i < xrDisplaySubSystems.Count; i++)
            {
                if(xrDisplaySubSystems[i].running)
                {
                    return true;
                }
            }
#endif
            return false;
        }

    }
}


