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
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

namespace rt.xr.unity.app
{
    public partial class App
    {
        private RectTransform m_detRoot, m_detContent;
        private CanvasGroup m_detGroup;
        private ScrollRect m_detScroll;
        private bool m_detailsOpen;
        private SceneEntry m_detEntry;
        private Text m_detTitle;
        private bool m_treeOpen;

        private void BuildDetails()
        {
            m_detRoot = Ui.R("Details", m_root);
            Ui.Stretch(m_detRoot);
            m_detGroup = Ui.Group(m_detRoot, false);
            var bg = Ui.Box(m_detRoot, Ui.Bg, 0f, "Bg");
            Ui.Bleed((RectTransform)bg.transform);
            bg.raycastTarget = true;
            ScrollRect sr;
            m_detContent = Ui.ScrollV(m_detRoot, out sr, 30, 0, "DetailsScroll");
            m_detScroll = sr;
            var srt = (RectTransform)sr.transform;
            Ui.Stretch(srt);
            srt.offsetMax = new Vector2(0, -150);
            var bar = Ui.R("Bar", m_detRoot);
            Ui.Place(bar, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), Vector2.zero, new Vector2(0, 150));
            var bb = Ui.Box(bar, new Color32(0x0D, 0x0D, 0x0D, 0xFF), 0f, "Bg");
            Ui.Stretch((RectTransform)bb.transform);
            bb.raycastTarget = true;
            var hb = Ui.R("Row", bar);
            Ui.Stretch(hb);
            var h = Ui.HBox(hb, 24, 0, TextAnchor.MiddleLeft);
            h.padding = new RectOffset(32, 32, 0, 0);
            Ui.IconButton(hb, "close", 100, CloseDetails, false, false);
            m_detTitle = Ui.Label(hb, "Scene details", 46, Ui.Bold, Color.white, TextAnchor.MiddleLeft, false);
            Ui.Size(m_detTitle.gameObject, -1, -1, 1);
        }

        public void ShowDetails(SceneEntry e, bool openTree = false)
        {
            if (e == null) return;
            m_detEntry = e;
            m_treeOpen = openTree;
            m_detailsOpen = true;
            Ui.Show(m_detGroup, true);
            RebuildDetails();
            m_detScroll.verticalNormalizedPosition = 1f;
        }

        private void CloseDetails()
        {
            m_detailsOpen = false;
            if (m_detGroup != null) Ui.Show(m_detGroup, false);
        }

        private HashSet<string> UnsupportedByLoader()
        {
            var set = new HashSet<string>();
            foreach (var l in m_logs)
            {
                var m = Regex.Match(l.Text, "ExtensionUnsupported;([A-Za-z0-9_]+)");
                if (m.Success) set.Add(m.Groups[1].Value);
            }
            return set;
        }

        private RectTransform Section(string title, string note = null)
        {
            var wrap = Pad(m_detContent, 8, 0);
            var card = Ui.Box(wrap, Ui.Card, 32f, "Section");
            Ui.VBox(card.transform, 18, 40);
            Ui.Label(card.transform, title, 40, Ui.Bold, Color.white, TextAnchor.MiddleLeft, false);
            if (note != null) Ui.Label(card.transform, note, 28, Ui.Regular, Ui.Dim, TextAnchor.UpperLeft, true);
            return (RectTransform)card.transform;
        }

        private void RebuildDetails()
        {
            var e = m_detEntry;
            for (int i = m_detContent.childCount - 1; i >= 0; i--) Destroy(m_detContent.GetChild(i).gameObject);
            m_detTitle.text = e.Title;
            bool live = m_screen == Screen_.Player && m_current == e;
            Ui.Size(Ui.Spacer(m_detContent, 0, 6).gameObject, -1, 6);

            // facts
            var s = Section("Scene");
            if (!string.IsNullOrEmpty(e.Summary)) Ui.Label(s, e.Summary, 32, Ui.Regular, new Color(1, 1, 1, 0.85f), TextAnchor.UpperLeft, true);
            var kv = Ui.R("Facts", s); Ui.VBox(kv, 0, 0);
            Fact(kv, "File", e.FileName);
            Fact(kv, "Address", e.Uri);
            Fact(kv, "Source", e.Source);
            if (live) Fact(kv, "Opened in", m_loadSeconds.ToString("0.0") + " s");
            if (e.Tags.Length > 0) Fact(kv, "Tags", string.Join(", ", e.Tags));

            // extensions
            var unsupported = live ? UnsupportedByLoader() : new HashSet<string>();
            var sec = Section("Extensions", e.Extensions.Length == 0 ? (e.Peeked ? "The scene lists no extensions." : "The scene file has not been read yet.") :
                (live ? "As reported by the loader when the scene opened." : "As listed in the scene file."));
            foreach (var ext in e.Extensions)
            {
                var fam = FamilyOfExtension(ext);
                var row = Ui.R("Ext", sec);
                Ui.HBox(row, 18, 0, TextAnchor.MiddleLeft);
                Ui.Size(row.gameObject, -1, 74, 1);
                var dot = Ui.Box(row, fam != null ? fam.Color : Ui.Dim, 14f, "Dot");
                Ui.Size(dot.gameObject, 28, 28);
                var nm = Ui.Label(row, ext, 30, Ui.Regular, Color.white, TextAnchor.MiddleLeft, true);
                Ui.Size(nm.gameObject, -1, -1, 1);
                if (live)
                {
                    if (unsupported.Contains(ext)) Ui.Chip(row, "Not supported", Ui.Warn, false, 24);
                    else Ui.Chip(row, "Loaded", Ui.Good, false, 24);
                }
            }

            // media
            if (e.Media.Count > 0)
            {
                var ms = Section("Media");
                foreach (var m in e.Media)
                {
                    var row = Ui.R("Media", ms);
                    Ui.VBox(row, 2, 0);
                    string f = Uri.UnescapeDataString(m.Uri.Substring(m.Uri.LastIndexOf('/') + 1));
                    var h = Ui.R("Head", row);
                    Ui.HBox(h, 16, 0, TextAnchor.MiddleLeft);
                    var nm = Ui.Label(h, f, 32, Ui.Medium, Color.white, TextAnchor.MiddleLeft, true);
                    Ui.Size(nm.gameObject, -1, -1, 1);
                    if (m.Status == "missing") Ui.Chip(h, "Not found", Ui.Warn, false, 24);
                    else if (m.Status == "found") Ui.Chip(h, "Found", Ui.Good, false, 24);
                    Ui.Label(row, m.Uri, 24, Ui.Regular, Ui.Dim, TextAnchor.UpperLeft, true);
                }
            }

            // content notes
            if (e.Names.Count > 0)
            {
                var ns = Section("Named in the content's notes");
                var flow = Ui.Flow(ns, 10, 10);
                foreach (var n in e.Names) Ui.Chip(flow.transform, Humanize(n) + (n.StartsWith("TRIGGER") ? " trigger" : n.StartsWith("ACTION") ? " action" : " anchor"), n.StartsWith("TRIGGER") ? Ui.Warn : (n.StartsWith("ACTION") ? Ui.Accent : Ui.Good), false, 26);
            }

            // live: behaviours and tree
            if (live) BuildLiveDetails();

            // problems seen while loading
            var warn = m_logs.Where(l => (l.Type == LogType.Warning || l.Type == LogType.Error || l.Type == LogType.Exception)).Select(l => l.Text.Split('\n')[0]).Distinct().Where(t => !t.Contains("spatializer")).Take(12).ToList();
            if (live && warn.Count > 0)
            {
                var w = Section("Messages while opening", "Warnings and errors from the loader and the player.");
                foreach (var t in warn) Ui.Label(w, t, 26, Ui.Regular, Ui.Dim, TextAnchor.UpperLeft, true);
            }

            var act = Pad(m_detContent, 8, 80);
            var row2 = Ui.R("Actions", act);
            Ui.VBox(row2, 18, 0);
            var copy = Ui.Pill(row2, "Copy diagnostics", () => { Ui.Copy(Diagnostics()); Toast("Copied to the clipboard"); }, Ui.BtnStyle.Primary, "copy");
            Ui.Size(copy.gameObject, -1, 110, 1);
            if (!live)
            {
                var open = Ui.Pill(row2, "Open scene", () => { CloseDetails(); OpenScene(e); }, Ui.BtnStyle.Outline, "play");
                Ui.Size(open.gameObject, -1, 110, 1);
            }
        }

        private void BuildLiveDetails()
        {
            // behaviours the loader created for this scene
            var behaviours = FindObjectsOfType<GLTFast.Behavior>().ToList();
            if (behaviours.Count > 0)
            {
                var b = Section("Behaviours", Ui.Plural(behaviours.Count, "behaviour", "behaviours") + " created from the scene's interactivity data.");
                int n = 0;
                foreach (var bh in behaviours)
                {
                    if (n++ >= 25) break;
                    var d = DescribeBehaviour(bh);
                    var row = Ui.R("Bh", b);
                    Ui.VBox(row, 2, 0);
                    Ui.Label(row, bh.gameObject.name, 30, Ui.Medium, Color.white, TextAnchor.UpperLeft, true);
                    Ui.Label(row, d, 26, Ui.Regular, Ui.Dim, TextAnchor.UpperLeft, true);
                }
            }

            // scene tree
            var t = Section("Scene tree", "Nodes the loader created, with the parts it attached. Tap a row to find it in the scene.");
            var toggle = Ui.Pill(t, m_treeOpen ? "Hide the tree" : "Show the tree", () => { m_treeOpen = !m_treeOpen; RebuildDetails(); }, Ui.BtnStyle.Ghost, "tree");
            Ui.Size(toggle.gameObject, -1, 96, 1);
            if (m_treeOpen)
            {
                int count = 0;
                foreach (Transform child in m_viewer.transform) AddTreeRow(t, child, 0, ref count);
                if (count >= 300) Ui.Label(t, "Showing the first 300 nodes.", 26, Ui.Regular, Ui.Dim);
            }
        }

        private void AddTreeRow(RectTransform parent, Transform tr, int depth, ref int count)
        {
            if (count >= 300) return;
            count++;
            var go = tr.gameObject;
            var fams = FamiliesOfGameObject(go);
            var row = Ui.Box(parent, new Color(1, 1, 1, 0.04f), 16f, "Node");
            Ui.HBox(row.transform, 14, 0, TextAnchor.MiddleLeft);
            row.GetComponent<HorizontalLayoutGroup>().padding = new RectOffset(18 + depth * 34, 18, 12, 12);
            Ui.Size(row.gameObject, -1, -1, 1);
            string icon = go.GetComponent<Light>() != null ? "light" : (go.GetComponent<Camera>() != null ? "camera" : (go.GetComponent<Renderer>() != null ? "cube" : "tree"));
            Ui.IconImage(row.transform, icon, Ui.Dim, 36);
            var nm = Ui.Label(row.transform, go.name, 28, Ui.Regular, go.activeInHierarchy ? Color.white : Ui.Dim, TextAnchor.MiddleLeft, false);
            Ui.Size(nm.gameObject, -1, -1, 1);
            foreach (var f in fams) { var d = Ui.Box(row.transform, f.Color, 12f, "Fam"); Ui.Size(d.gameObject, 24, 24); }
            Ui.Click(row.gameObject, row, () => { CloseDetails(); HighlightNode(go); });
            foreach (Transform c in tr) AddTreeRow(parent, c, Mathf.Min(depth + 1, 6), ref count);
        }
    }
}
