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
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.XR;
using rt.xr.unity;

namespace rt.xr.unity.app
{
    // In a headset the interface becomes a panel in the room, pointed at with the right controller (trigger clicks, right stick
    // scrolls) and shown or hidden with X on the left controller. The scene is placed in front of the viewer, or around the
    // viewer when it is room sized.
    public partial class App
    {
        public static bool Xr { get { return I != null && I.m_xr; } }

        private bool m_xr, m_xrVisible = true, m_xrTrigWas, m_xrXWas;
        private LineRenderer m_xrRay;
        private const float XrPanelScale = 0.00075f;      // metres per interface unit: the 1920 x 1080 panel is 1.44 m wide
        private const float XrPanelDistance = 1.0f;

        private void EnterXr()
        {
            var rig = QuestRig.Instance;
            m_xr = true;
            m_scaler.enabled = false;
            m_canvas.renderMode = RenderMode.WorldSpace;
            m_canvas.worldCamera = rig.Cam;
            var crt = (RectTransform)m_canvas.transform;
            crt.SetParent(rig.Rig, false);
            crt.anchorMin = crt.anchorMax = crt.pivot = new Vector2(0.5f, 0.5f);
            crt.sizeDelta = new Vector2(1920, 1080);
            crt.localScale = Vector3.one * XrPanelScale;
            m_root.anchorMin = Vector2.zero; m_root.anchorMax = Vector2.one;
            m_root.offsetMin = Vector2.zero; m_root.offsetMax = Vector2.zero;
            var go = new GameObject("XrRay");
            go.transform.SetParent(rig.Rig, false);
            m_xrRay = go.AddComponent<LineRenderer>();
            m_xrRay.material = Ui.LineMaterial();
            m_xrRay.startColor = m_xrRay.endColor = Ui.Accent;
            m_xrRay.startWidth = m_xrRay.endWidth = 0.004f;
            m_xrRay.positionCount = 2;
            m_xrRay.enabled = false;
            m_rig.Enabled = false;
            PlaceXrPanel();
            OnLayoutChanged();
            Debug.Log("App: headset mode");
        }

        private void PlaceXrPanel()
        {
            var head = QuestRig.Instance.Cam.transform;
            Vector3 flat = Vector3.ProjectOnPlane(head.forward, Vector3.up).normalized;
            if (flat == Vector3.zero) flat = Vector3.forward;
            var t = m_canvas.transform;
            t.position = head.position + flat * XrPanelDistance - Vector3.up * 0.12f;
            t.rotation = Quaternion.LookRotation(flat, Vector3.up);
        }

        // a scene smaller than a room is put 1.6 m in front of the viewer, standing on the floor; a room-sized one is put
        // around the viewer
        private void PlaceSceneForXr()
        {
            var rig = QuestRig.Instance;
            if (rig == null) return;
            Bounds b = rt.xr.Utils.ComputeBounds(m_viewer.gameObject);
            var head = rig.Cam.transform;
            Vector3 flat = Vector3.ProjectOnPlane(head.forward, Vector3.up).normalized;
            if (flat == Vector3.zero) flat = Vector3.forward;
            float footprint = Mathf.Max(b.size.x, b.size.z);
            if (footprint > 5f)
            {
                var floor = new Vector3(b.center.x, b.min.y, b.center.z);
                rig.Rig.position = floor - new Vector3(head.localPosition.x, 0, head.localPosition.z);
            }
            else
            {
                Vector3 target = new Vector3(head.position.x, rig.Rig.position.y, head.position.z) + flat * 1.6f;
                Vector3 bottom = new Vector3(b.center.x, b.min.y, b.center.z);
                m_viewer.transform.position += target - bottom;
            }
        }

        private static bool XrButton(XRNode node, InputFeatureUsage<bool> usage)
        {
            var d = InputDevices.GetDeviceAtXRNode(node);
            bool v;
            return d.isValid && d.TryGetFeatureValue(usage, out v) && v;
        }

        private Transform XrScope()
        {
            if (m_dlgOpen) return m_dlgRoot;
            if (m_openingOpen) return m_openRoot;
            if (m_detailsOpen) return m_detRoot;
            if (m_toolsOpen && m_toolsRoot != null) return m_toolsRoot;
            if (m_sheetOpen) return m_sheetRoot;
            if (m_screen == Screen_.Player) return m_playerRoot;
            if (m_screen == Screen_.Playground) return m_pgRoot;
            return m_root;
        }

        private static bool XrShown(Component c)
        {
            for (var t = c.transform; t != null; t = t.parent)
            {
                var g = t.GetComponent<CanvasGroup>();
                if (g != null && (g.alpha < 0.05f || !g.blocksRaycasts) && !g.ignoreParentGroups) return false;
                if (!t.gameObject.activeSelf) return false;
            }
            return true;
        }

        private void UpdateXr()
        {
            if (!m_xr)
            {
                if (QuestRig.Active && QuestRig.Instance != null && QuestRig.Instance.Cam != null && m_canvas != null) EnterXr();
                return;
            }
            bool x = XrButton(XRNode.LeftHand, CommonUsages.primaryButton);
            if (x && !m_xrXWas) { m_xrVisible = !m_xrVisible; m_canvas.enabled = m_xrVisible; if (m_xrVisible) PlaceXrPanel(); }
            m_xrXWas = x;
            QuestRig.SuppressTurn = false;
            var hand = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            Vector3 pos; Quaternion rot;
            if (!m_xrVisible || !hand.isValid || !hand.TryGetFeatureValue(CommonUsages.devicePosition, out pos) || !hand.TryGetFeatureValue(CommonUsages.deviceRotation, out rot))
            {
                m_xrRay.enabled = false;
                return;
            }
            var rig = QuestRig.Instance.Rig;
            Vector3 origin = rig.TransformPoint(pos);
            Vector3 dir = rig.rotation * rot * Vector3.forward;
            var plane = new Plane(m_canvas.transform.forward * -1f, m_canvas.transform.position);
            float enter;
            m_xrRay.enabled = true;
            m_xrRay.SetPosition(0, origin);
            if (!plane.Raycast(new Ray(origin, dir), out enter) || enter > 6f) { m_xrRay.SetPosition(1, origin + dir * 1.2f); return; }
            Vector3 p = origin + dir * enter;
            m_xrRay.SetPosition(1, p);

            var scope = XrScope();
            Selectable best = null;
            ScrollRect scroll = null;
            foreach (var s in scope.GetComponentsInChildren<Selectable>(false))
            {
                if (!s.interactable || !XrShown(s)) continue;
                var rt = s.transform as RectTransform;
                if (rt == null) continue;
                Vector3 l = rt.InverseTransformPoint(p);
                if (rt.rect.Contains(new Vector2(l.x, l.y))) best = s;      // later siblings are drawn on top
            }
            foreach (var sr in scope.GetComponentsInChildren<ScrollRect>(false))
            {
                if (!XrShown(sr)) continue;
                var rt = (RectTransform)sr.transform;
                Vector3 l = rt.InverseTransformPoint(p);
                if (rt.rect.Contains(new Vector2(l.x, l.y))) scroll = sr;
            }

            bool trig = XrButton(XRNode.RightHand, CommonUsages.triggerButton);
            if (trig && !m_xrTrigWas && best != null)
                ExecuteEvents.Execute(best.gameObject, new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left }, ExecuteEvents.pointerClickHandler);
            m_xrTrigWas = trig;

            if (scroll != null)
            {
                Vector2 stick = Vector2.zero;
                var rd = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
                rd.TryGetFeatureValue(CommonUsages.primary2DAxis, out stick);
                if (scroll.vertical && Mathf.Abs(stick.y) > 0.2f && scroll.content != null && scroll.viewport != null)
                {
                    float room = Mathf.Max(1f, scroll.content.rect.height - scroll.viewport.rect.height);
                    scroll.verticalNormalizedPosition = Mathf.Clamp01(scroll.verticalNormalizedPosition + stick.y * Time.unscaledDeltaTime * 1400f / room);
                }
                else if (scroll.horizontal && !scroll.vertical && Mathf.Abs(stick.x) > 0.2f && scroll.content != null && scroll.viewport != null)
                {
                    float room = Mathf.Max(1f, scroll.content.rect.width - scroll.viewport.rect.width);
                    scroll.horizontalNormalizedPosition = Mathf.Clamp01(scroll.horizontalNormalizedPosition + stick.x * Time.unscaledDeltaTime * 1400f / room);
                    QuestRig.SuppressTurn = true;
                }
            }
        }
    }
}
