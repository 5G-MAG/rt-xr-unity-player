/*
 * License: 5G-MAG Public License (v1.0)
 * Author: Jordi J. Gimenez (5G-MAG)
 * Copyright: (C) 2026 5G-MAG Association
 *
 * For full license terms please see the LICENSE file distributed with this
 * program. If this file is missing then the license can be retrieved from
 * https://www.5g-mag.com/license.
 */

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace rt.xr.unity.app
{
    // Touch camera for the phone: Orbit (drag to turn, pinch to zoom, two fingers to pan), Walk (left half moves, right
    // half looks) and the scene's own camera. Orbit goes round the middle of the scene's bounds, so the scene is framed
    // wherever it sits relative to the origin.
    public class CamRig : MonoBehaviour
    {
        public enum Mode { Orbit, Walk, Scene }

        public Mode CurrentMode { get; private set; }
        public bool HasSceneCamera { get { return m_sceneCam != null; } }
        public bool Enabled = true;

        private Camera m_cam;
        private Camera m_sceneCam;
        private Bounds m_bounds;
        private Vector3 m_target;
        private float m_dist, m_yaw, m_pitch, m_minDist, m_maxDist;
        private float m_eye = 1.6f;
        private readonly Dictionary<int, bool> m_onUi = new Dictionary<int, bool>();
        private float m_walkSpeed = 1.5f;
        private Vector2 m_stickOrigin; private int m_stickFinger = -1;

        public void Attach(Camera cam, Bounds bounds, Camera sceneCam)
        {
            m_cam = cam; m_bounds = bounds; m_sceneCam = sceneCam;
            float size = Mathf.Max(0.05f, bounds.size.magnitude);
            m_walkSpeed = Mathf.Clamp(size * 0.25f, 0.5f, 6f);
            m_eye = Mathf.Clamp(bounds.min.y + 1.6f, bounds.min.y + 0.1f, bounds.max.y + 2f);
            FitOrbit();
            SetMode(Mode.Orbit);
        }

        public void Detach() { m_cam = null; m_sceneCam = null; }

        public void FocusOn(Bounds b)
        {
            if (m_cam == null) return;
            if (CurrentMode != Mode.Orbit) SetMode(Mode.Orbit);
            m_target = b.center;
            float radius = Mathf.Max(0.02f, b.extents.magnitude);
            m_dist = Mathf.Clamp(radius / Mathf.Sin(FitFov() * 0.5f * Mathf.Deg2Rad) * 1.4f, m_minDist, m_maxDist);
            ApplyOrbit();
        }

        public void FitOrbit()
        {
            if (m_cam == null) return;
            m_target = m_bounds.center;
            float radius = Mathf.Max(0.05f, m_bounds.extents.magnitude);
            m_dist = radius / Mathf.Sin(FitFov() * 0.5f * Mathf.Deg2Rad) * 1.08f;
            m_minDist = radius * 0.1f; m_maxDist = m_dist * 6f;
            m_yaw = 205f; m_pitch = 18f;
            ApplyClipping();
            ApplyOrbit();
        }

        // the narrower of the vertical and horizontal fields of view, so the scene fits in portrait as well as landscape
        private float FitFov()
        {
            float v = Mathf.Max(20f, m_cam.fieldOfView) * Mathf.Deg2Rad;
            float h = 2f * Mathf.Atan(Mathf.Tan(v * 0.5f) * Mathf.Max(0.3f, (float)Screen.width / Screen.height));
            return Mathf.Min(v, h) * Mathf.Rad2Deg;
        }

        private void ApplyClipping()
        {
            if (m_cam == null) return;
            m_cam.nearClipPlane = Mathf.Max(0.01f, m_minDist * 0.1f);
            m_cam.farClipPlane = Mathf.Max(100f, m_maxDist * 4f);
        }

        public void SetMode(Mode mode)
        {
            if (mode == Mode.Scene && m_sceneCam == null) mode = Mode.Orbit;
            CurrentMode = mode;
            if (m_cam == null) return;
            if (mode == Mode.Orbit) { ApplyOrbit(); }
            else if (mode == Mode.Walk)
            {
                Vector3 p = m_cam.transform.position;
                Vector3 fwd = m_cam.transform.forward;
                m_yaw = Mathf.Atan2(fwd.x, fwd.z) * Mathf.Rad2Deg; m_pitch = 0f;
                // start at the scene's own camera if it has one, else where the orbit camera is, lowered to eye height
                if (m_sceneCam != null) { p = m_sceneCam.transform.position; fwd = m_sceneCam.transform.forward; m_yaw = Mathf.Atan2(fwd.x, fwd.z) * Mathf.Rad2Deg; }
                else p.y = m_eye;
                m_cam.transform.position = p;
                m_cam.transform.rotation = Quaternion.Euler(0, m_yaw, 0);
            }
            else if (mode == Mode.Scene)
            {
                m_cam.transform.SetPositionAndRotation(m_sceneCam.transform.position, m_sceneCam.transform.rotation);
                m_cam.fieldOfView = m_sceneCam.fieldOfView;
            }
        }

        private void ApplyOrbit()
        {
            if (m_cam == null) return;
            var rot = Quaternion.Euler(m_pitch, m_yaw, 0);
            m_cam.transform.position = m_target - rot * Vector3.forward * m_dist;
            m_cam.transform.LookAt(m_target);
        }

        private bool OverUi(Touch t)
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(t.fingerId);
        }

        private void LateUpdate()
        {
            if (m_cam == null || !Enabled) return;
            var touches = Input.touches;
            for (int i = 0; i < touches.Length; i++)
            {
                var t = touches[i];
                if (t.phase == TouchPhase.Began) m_onUi[t.fingerId] = OverUi(t);
                if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled) { m_onUi.Remove(t.fingerId); if (t.fingerId == m_stickFinger) m_stickFinger = -1; }
            }
            var free = new List<Touch>();
            foreach (var t in touches) { bool ui; if (!(m_onUi.TryGetValue(t.fingerId, out ui) && ui) && t.phase != TouchPhase.Ended && t.phase != TouchPhase.Canceled) free.Add(t); }

            if (CurrentMode == Mode.Orbit) UpdateOrbit(free);
            else if (CurrentMode == Mode.Walk) UpdateWalk(free);
        }

        private float Dpi { get { return Mathf.Max(100f, Screen.dpi > 0 ? Screen.dpi : 400f); } }

        private void UpdateOrbit(List<Touch> t)
        {
            if (t.Count == 1)
            {
                var d = t[0].deltaPosition / Dpi * 180f;     // about 180 degrees per inch
                m_yaw += d.x; m_pitch = Mathf.Clamp(m_pitch - d.y, -85f, 85f);
            }
            else if (t.Count >= 2)
            {
                Vector2 a0 = t[0].position - t[0].deltaPosition, b0 = t[1].position - t[1].deltaPosition;
                float prev = Vector2.Distance(a0, b0), cur = Vector2.Distance(t[0].position, t[1].position);
                if (prev > 1f && cur > 1f) m_dist = Mathf.Clamp(m_dist * prev / cur, m_minDist, m_maxDist);
                Vector2 mid = (t[0].deltaPosition + t[1].deltaPosition) * 0.5f;
                float perPixel = m_dist * 2f * Mathf.Tan(m_cam.fieldOfView * 0.5f * Mathf.Deg2Rad) / Screen.height;
                m_target += (-m_cam.transform.right * mid.x - m_cam.transform.up * mid.y) * perPixel;
            }
            ApplyOrbit();
        }

        private void UpdateWalk(List<Touch> t)
        {
            Vector2 move = Vector2.zero;
            foreach (var touch in t)
            {
                bool left = touch.rawPosition.x < Screen.width * 0.5f;
                if (left)
                {
                    if (touch.phase == TouchPhase.Began || m_stickFinger != touch.fingerId) { m_stickFinger = touch.fingerId; m_stickOrigin = touch.rawPosition; }
                    Vector2 d = (touch.position - m_stickOrigin) / (Dpi * 0.5f);
                    move = Vector2.ClampMagnitude(d, 1f);
                }
                else
                {
                    var d = touch.deltaPosition / Dpi * 160f;
                    m_yaw += d.x; m_pitch = Mathf.Clamp(m_pitch - d.y, -80f, 80f);
                }
            }
            m_cam.transform.rotation = Quaternion.Euler(m_pitch, m_yaw, 0);
            Vector3 fwd = Vector3.ProjectOnPlane(m_cam.transform.forward, Vector3.up).normalized;
            Vector3 right = Vector3.ProjectOnPlane(m_cam.transform.right, Vector3.up).normalized;
            m_cam.transform.position += (fwd * move.y + right * move.x) * m_walkSpeed * Time.unscaledDeltaTime;
        }
    }
}
