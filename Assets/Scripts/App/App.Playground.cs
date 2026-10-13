/*
 * License: 5G-MAG Public License (v1.0)
 * Author: Jordi J. Gimenez (5G-MAG)
 * Copyright: (C) 2026 5G-MAG Association
 *
 * For full license terms please see the LICENSE file distributed with this
 * program. If this file is missing then the license can be retrieved from
 * https://www.5g-mag.com/license.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace rt.xr.unity.app
{
    // Anchor playground: put a model from the library in the room and choose how it is anchored. It uses the same AR session
    // as the AR scenes. Floor and Plane use the surfaces the phone has found; View keeps the model in front of the camera;
    // World fixes it where it was placed.
    public partial class App
    {
        private RectTransform m_pgRoot, m_pgPicks, m_pgChips;
        private CanvasGroup m_pgGroup;
        private Text m_pgState, m_pgHelp;
        private bool m_pgOpen, m_pgPlacing;
        private string m_pgAnchor = "floor";
        private SceneEntry m_pgEntry;
        private readonly List<GameObject> m_pgPlaced = new List<GameObject>();
        private List<SceneEntry> m_pgChoices = new List<SceneEntry>();
        private float m_pgNext;

        private void BuildPlayground()
        {
            m_pgRoot = Ui.R("Playground", m_root);
            Ui.Stretch(m_pgRoot);
            m_pgGroup = Ui.Group(m_pgRoot, false);

            var top = Ui.R("Top", m_pgRoot);
            Ui.Place(top, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), Vector2.zero, new Vector2(0, 260));
            var tg = Ui.Box(top, Color.white, 0f, "Grad");
            tg.sprite = Ui.GradDown; tg.type = Image.Type.Simple;
            Ui.Stretch((RectTransform)tg.transform);
            var bar = Ui.R("Bar", top);
            Ui.Stretch(bar);
            var h = Ui.HBox(bar, 22, 0, TextAnchor.UpperLeft);
            h.padding = new RectOffset(32, 32, 28, 0);
            Ui.IconButton(bar, "back", 104, LeavePlayground);
            var col = Ui.R("Title", bar);
            Ui.VBox(col, 0, 0, TextAnchor.MiddleLeft);
            Ui.Size(col.gameObject, -1, 104, 1);
            Ui.Label(col, "Anchor playground", 44, Ui.Bold, Color.white, TextAnchor.MiddleLeft, false);
            m_pgState = Ui.Label(col, "Starting the camera", 28, Ui.Regular, new Color(1, 1, 1, 0.75f), TextAnchor.MiddleLeft, false);

            var panel = Ui.Box(m_pgRoot, new Color(0.07f, 0.07f, 0.07f, 0.92f), 44f, "Panel");
            var prt = (RectTransform)panel.transform;
            Ui.Place(prt, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(0, -44), new Vector2(0, 0));
            Ui.VBox(prt, 18, 36);
            prt.GetComponent<VerticalLayoutGroup>().padding = new RectOffset(40, 40, 36, 80);
            Ui.Fit(prt.gameObject);
            panel.raycastTarget = true;
            Ui.Label(prt, "Place", 30, Ui.Medium, Ui.Dim, TextAnchor.MiddleLeft, false);
            ScrollRect sr;
            m_pgPicks = Ui.ScrollH(prt, out sr, 18, 0, 220f, "Picks");
            Ui.Label(prt, "Anchor to", 30, Ui.Medium, Ui.Dim, TextAnchor.MiddleLeft, false);
            m_pgChips = Ui.Flow(prt, 12, 12).GetComponent<RectTransform>();
            m_pgHelp = Ui.Label(prt, "", 27, Ui.Regular, Ui.Dim, TextAnchor.UpperLeft, true);
            var row = Ui.R("Buttons", prt);
            Ui.HBox(row, 16, 0, TextAnchor.MiddleLeft);
            Ui.Size(row.gameObject, -1, 108, 1);
            var place = Ui.Pill(row, "Place here", PlaceObject, Ui.BtnStyle.Primary, "pin");
            Ui.Size(place.gameObject, -1, 108, 1);
            Ui.Pill(row, "Clear", ClearPlaced, Ui.BtnStyle.Ghost, "trash");

        }

        private void StartPlayground() { StartCoroutine(StartPlaygroundRoutine()); }

        private System.Collections.IEnumerator StartPlaygroundRoutine()
        {
            if (m_busy || m_pgOpen) yield break;
            yield return EnsureCameraPermission();
            StartPlaygroundNow();
        }

        private void StartPlaygroundNow()
        {
            if (m_busy || m_pgOpen) return;
            if (m_pgRoot == null) BuildPlayground();
            m_pgOpen = true;
            m_screen = Screen_.Playground;
            Ui.Show(m_libGroup, false); Ui.Show(m_labGroup, false); Ui.Show(m_setGroup, false); Ui.Show(m_tabsGroup, false); Ui.Show(m_playerGroup, false);
            Ui.Show(m_pgGroup, true);
            m_pgChoices = Catalog.Entries.Where(e => e.Thumb != null && !e.UsesAnchor && !e.UsesMedia && !e.UsesInteractivity && string.IsNullOrEmpty(e.Variant)).Take(8).ToList();
            if (m_pgChoices.Count == 0) m_pgChoices = Catalog.Entries.Where(e => !e.UsesAnchor && !e.UsesMedia).Take(8).ToList();
            m_pgEntry = m_pgChoices.FirstOrDefault();
            RebuildPgPicks();
            RebuildPgChips();
            try
            {
                m_viewer.EnableARCamera();
                UseArCamera();
            }
            catch (Exception ex)
            {
                m_pgState.text = "Could not start AR: " + ex.Message;
            }
            EnsureLightForPlayground();
            m_pgHelp.text = "Point the camera at a surface and move slowly, then tap Place here.";
        }

        private void EnsureLightForPlayground()
        {
            if (GameObject.Find("FillLight") != null) return;
            var go = new GameObject("FillLight");
            go.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            var l = go.AddComponent<Light>(); l.type = LightType.Directional; l.intensity = 1.1f;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.5f, 0.5f, 0.52f);
        }

        private void RebuildPgPicks()
        {
            for (int i = m_pgPicks.childCount - 1; i >= 0; i--) Destroy(m_pgPicks.GetChild(i).gameObject);
            foreach (var e in m_pgChoices)
            {
                var sel = e == m_pgEntry;
                var card = Ui.Box(m_pgPicks, sel ? Ui.AccentSoft : new Color(1, 1, 1, 0.06f), 24f, "Pick");
                Ui.Size(card.gameObject, 250, 210);
                Ui.VBox(card.transform, 6, 10, TextAnchor.UpperCenter);
                var th = Ui.R("Thumb", card.transform);
                Ui.Size(th.gameObject, -1, 110, 1);
                var mask = Ui.Box(th, Ui.Raised, 14f, "Frame");
                Ui.Stretch((RectTransform)mask.transform);
                mask.gameObject.AddComponent<Mask>().showMaskGraphic = true;
                if (e.Thumb != null) { var ri = Ui.R("Img", mask.transform).gameObject.AddComponent<RawImage>(); Ui.Stretch((RectTransform)ri.transform); Ui.Cover(ri, e.Thumb, 230f / 110f); }
                var t = Ui.Label(card.transform, e.Title, 22, sel ? Ui.Medium : Ui.Regular, sel ? Color.white : Ui.Dim, TextAnchor.UpperCenter, true);
                t.verticalOverflow = VerticalWrapMode.Truncate;
                Ui.Size(t.gameObject, -1, 56);
                var ee = e;
                Ui.Click(card.gameObject, card, () => { m_pgEntry = ee; RebuildPgPicks(); });
            }
        }

        private static readonly string[][] PgAnchors =
        {
            new[] { "floor", "Floor" }, new[] { "plane", "Plane" }, new[] { "view", "View" }, new[] { "world", "World" },
        };

        private void RebuildPgChips()
        {
            for (int i = m_pgChips.childCount - 1; i >= 0; i--) Destroy(m_pgChips.GetChild(i).gameObject);
            foreach (var a in PgAnchors)
            {
                string id = a[0];
                bool sel = m_pgAnchor == id;
                var chip = Ui.Chip(m_pgChips, a[1], sel ? Ui.Accent : Ui.Dim, sel, 30);
                Ui.Click(chip.gameObject, chip, () => { m_pgAnchor = id; RebuildPgChips(); });
            }
            var m = Ui.Chip(m_pgChips, "Marker", new Color(1, 1, 1, 0.3f), false, 30);
            var c = Ui.Chip(m_pgChips, "Controller", new Color(1, 1, 1, 0.3f), false, 30);
            m_pgHelp.text = AnchorHelp();
        }

        private string AnchorHelp()
        {
            switch (m_pgAnchor)
            {
                case "floor": return "Floor: the model sits on a horizontal surface the phone has found. Marker and Controller anchors are not available in this playground; open the marker scenes from the library for those.";
                case "plane": return "Plane: the model sits on any surface the phone has found.";
                case "view": return "View: the model stays in front of the camera as you move.";
                default: return "World: the model stays where you placed it in the room.";
            }
        }

        private async void PlaceObject()
        {
            if (m_pgPlacing || m_pgEntry == null) return;
            var cam = Camera.main != null ? Camera.main : m_viewer.GetMainCamera();
            if (cam == null) return;
            Pose pose = new Pose();
            bool have = false;
            Transform parent = null;
            var flatFwd = Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up).normalized;
            if (flatFwd == Vector3.zero) flatFwd = Vector3.forward;
            if (m_pgAnchor == "floor" || m_pgAnchor == "plane")
            {
                var rm = EnsureRaycastManager();
                var hits = new List<ARRaycastHit>();
                if (rm != null && rm.Raycast(new Vector2(Screen.width / 2f, Screen.height / 2f), hits, TrackableType.PlaneWithinPolygon))
                {
                    foreach (var h in hits)
                    {
                        var plane = h.trackable as ARPlane;
                        if (m_pgAnchor == "floor" && plane != null && plane.alignment != PlaneAlignment.HorizontalUp) continue;
                        pose = h.pose; have = true; break;
                    }
                }
                if (!have) { Toast(rm == null ? "This phone gave no surfaces. Try View or World." : "No surface there yet. Move the phone slowly over the floor.", 3.5f); return; }
            }
            else if (m_pgAnchor == "view")
            {
                parent = cam.transform;
                pose = new Pose(cam.transform.position + cam.transform.forward * 0.9f - cam.transform.up * 0.15f, Quaternion.LookRotation(-flatFwd, Vector3.up));
                have = true;
            }
            else
            {
                pose = new Pose(cam.transform.position + flatFwd * 1.2f - Vector3.up * 0.4f, Quaternion.LookRotation(-flatFwd, Vector3.up));
                have = true;
            }
            m_pgPlacing = true;
            Toast("Placing " + m_pgEntry.Title, 1.5f);
            try
            {
                var imp = new GLTFast.GltfImport(logger: new GLTFast.Logging.ConsoleLogger());
                bool ok = await imp.Load(new Uri(m_pgEntry.Uri));
                if (!ok) { Toast("That model did not load", 3f); m_pgPlacing = false; return; }
                var root = new GameObject("Placed " + m_pgEntry.Title);
                await imp.InstantiateMainSceneAsync(root.transform);
                var rends = root.GetComponentsInChildren<Renderer>();
                float scale = 1f;
                if (rends.Length > 0)
                {
                    Bounds b = rends[0].bounds; foreach (var r in rends) b.Encapsulate(r.bounds);
                    float size = Mathf.Max(b.size.x, b.size.y, b.size.z);
                    scale = size > 0.0001f ? 0.5f / size : 1f;
                    // the foot of the model on the anchor point
                    float foot = b.min.y - root.transform.position.y;
                    root.transform.localScale = Vector3.one * scale;
                    root.transform.SetPositionAndRotation(pose.position - Vector3.up * foot * scale, pose.rotation);
                }
                else root.transform.SetPositionAndRotation(pose.position, pose.rotation);
                if (parent != null) root.transform.SetParent(parent, true);
                m_pgPlaced.Add(root);
                m_pgState.text = "Placed " + m_pgPlaced.Count + ", anchored to " + m_pgAnchor;
                PlayerPrefs.SetInt("stamp.anchors", 1); PlayerPrefs.Save();
            }
            catch (Exception ex) { Toast("Could not place it: " + ex.Message, 3f); }
            m_pgPlacing = false;
        }

        private void ClearPlaced()
        {
            foreach (var g in m_pgPlaced) if (g != null) Destroy(g);
            m_pgPlaced.Clear();
        }

        private void LeavePlayground()
        {
            if (!m_pgOpen) return;
            ClearPlaced();
            try { m_viewer.DisableARCamera(); } catch { }
            m_pgOpen = false;
            Ui.Show(m_pgGroup, false);
            var old = GameObject.Find("FillLight");
            if (old != null) Destroy(old);
            var cam = m_viewer.GetMainCamera();
            ResetCamera();
            ShowLab();
        }

        private void UpdatePlayground()
        {
            if (!m_pgOpen || Time.unscaledTime < m_pgNext) return;
            m_pgNext = Time.unscaledTime + 0.4f;
            var pm = FindObjectOfType<ARPlaneManager>();
            string planes = pm != null ? ", " + Ui.Plural(pm.trackables.count, "surface", "surfaces") + " found" : "";
            if (m_pgPlaced.Count == 0) m_pgState.text = ARSession.state + planes;
        }
    }
}
