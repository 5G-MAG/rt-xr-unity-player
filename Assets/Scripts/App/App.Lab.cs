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

namespace rt.xr.unity.app
{
    public class Family
    {
        public string Id, Name, Icon;
        public Color Color;
        public string[] Exts;
        public bool Match(string ext)
        {
            foreach (var e in Exts)
            {
                if (e.EndsWith("*")) { if (ext.StartsWith(e.Substring(0, e.Length - 1))) return true; }
                else if (e == ext) return true;
            }
            return false;
        }
        public string ExtLabel { get { return string.Join(", ", Exts); } }
    }

    // The Extension Lab: a passport of the extension families, a page per family with the scenes that use it, and (in
    // App.LabTools.cs) tools inside the player for seeing and switching extensions.
    public partial class App
    {
        public static readonly Family[] Families =
        {
            new Family { Id = "video", Name = "Video textures", Icon = "video", Color = Ui.Accent, Exts = new[] { "MPEG_media", "MPEG_texture_video", "MPEG_buffer_circular", "MPEG_accessor_timed", "MPEG_sampler_YCbCr" } },
            new Family { Id = "audio", Name = "Spatial audio", Icon = "volume", Color = Ui.Audio, Exts = new[] { "MPEG_audio_spatial" } },
            new Family { Id = "anchors", Name = "Anchors", Icon = "anchor", Color = Ui.Good, Exts = new[] { "MPEG_anchor" } },
            new Family { Id = "interactivity", Name = "Interactivity", Icon = "touch", Color = Ui.Warn, Exts = new[] { "MPEG_scene_interactivity", "MPEG_node_interactivity" } },
            new Family { Id = "haptics", Name = "Haptics", Icon = "vibrate", Color = Ui.Bad, Exts = new[] { "MPEG_haptic" } },
            new Family { Id = "lighting", Name = "Lighting", Icon = "light", Color = Ui.LightC, Exts = new[] { "KHR_lights_punctual", "MPEG_lights_texture_based" } },
            new Family { Id = "materials", Name = "Materials", Icon = "layers", Color = Ui.MaterialC, Exts = new[] { "KHR_materials_*" } },
            new Family { Id = "compression", Name = "Compression and transforms", Icon = "cube", Color = Ui.CompressC, Exts = new[] { "KHR_draco_mesh_compression", "KHR_texture_basisu", "KHR_mesh_quantization", "KHR_texture_transform" } },
        };

        public static Family FamilyOfExtension(string ext) { return Families.FirstOrDefault(f => f.Match(ext)); }
        public static Family FamilyById(string id) { return Families.FirstOrDefault(f => f.Id == id); }

        private RectTransform m_labRoot, m_labContent;
        private CanvasGroup m_labGroup;
        private ScrollRect m_labScroll;
        private bool m_labDirty;

        private void BuildLab()
        {
            m_labRoot = Ui.R("Lab", m_root);
            Ui.Stretch(m_labRoot);
            m_labGroup = Ui.Group(m_labRoot, false);
            var bg = Ui.Box(m_labRoot, Ui.Bg, 0f, "Bg");
            Ui.Bleed((RectTransform)bg.transform);
            bg.raycastTarget = true;
            ScrollRect sr;
            m_labContent = Ui.ScrollV(m_labRoot, out sr, 30, 0, "LabScroll");
            m_labScroll = sr;
            var srt = (RectTransform)sr.transform;
            Ui.Stretch(srt);
            srt.offsetMin = new Vector2(0, TabsH);
        }

        // ------------------------------------------------------------------ passport

        public bool IsStamped(Family f) { return PlayerPrefs.GetInt("stamp." + f.Id, 0) == 1; }
        private int StampCount() { return Families.Count(IsStamped); }

        private void Stamp(SceneEntry e)
        {
            foreach (var f in Families) if (e.Extensions.Any(f.Match) && !IsStamped(f))
                {
                    PlayerPrefs.SetInt("stamp." + f.Id, 1);
                    m_newStamp = f;
                }
            PlayerPrefs.Save();
        }
        private Family m_newStamp;

        private void ClearStamps() { foreach (var f in Families) PlayerPrefs.DeleteKey("stamp." + f.Id); PlayerPrefs.Save(); m_labDirty = true; }

        private void RefreshLab()
        {
            m_labDirty = false;
            float keep = m_labScroll.verticalNormalizedPosition;
            for (int i = m_labContent.childCount - 1; i >= 0; i--) Destroy(m_labContent.GetChild(i).gameObject);
            var c = m_labContent;
            float width = m_root.rect.width;

            var head = Pad(c, 56, 0);
            Ui.Label(head, "Extension Lab", 60, Ui.Bold, Color.white, TextAnchor.MiddleLeft, false);
            Ui.Label(head, "Open scenes that use each extension, then use the lab tools in the player to see them, switch them off and watch them work.", 30, Ui.Regular, Ui.Dim, TextAnchor.UpperLeft, true);

            // passport
            var pw = Pad(c, 8, 0);
            var card = Ui.Box(pw, Ui.Card, 36f, "Passport");
            Ui.VBox(card.transform, 20, 40);
            var top = Ui.R("Top", card.transform);
            Ui.HBox(top, 16, 0, TextAnchor.MiddleLeft);
            Ui.IconImage(top, "star", Ui.Warn, 56);
            var tl = Ui.Label(top, "Your passport", 40, Ui.Bold, Color.white, TextAnchor.MiddleLeft, false);
            Ui.Size(tl.gameObject, -1, -1, 1);
            int n = StampCount();
            Ui.Label(top, n + " of " + Families.Length, 34, Ui.Medium, Ui.Dim, TextAnchor.MiddleRight, false);
            var bar = Ui.Box(card.transform, new Color(1, 1, 1, 0.12f), 14f, "Bar");
            Ui.Size(bar.gameObject, -1, 28, 1);
            var fill = Ui.Box(bar.transform, Ui.Accent, 14f, "Fill");
            var frt = (RectTransform)fill.transform;
            frt.anchorMin = new Vector2(0, 0); frt.anchorMax = new Vector2(Mathf.Max(0.05f, n / (float)Families.Length), 1);
            frt.offsetMin = Vector2.zero; frt.offsetMax = Vector2.zero;
            var stamps = Ui.R("Stamps", card.transform);
            Ui.HBox(stamps, 14, 0, TextAnchor.MiddleLeft);
            Ui.Size(stamps.gameObject, -1, 76);
            foreach (var f in Families)
            {
                var d = Ui.Box(stamps, IsStamped(f) ? f.Color : new Color(1, 1, 1, 0.1f), 38f, "Stamp");
                Ui.Size(d.gameObject, 76, 76);
                var ic = Ui.IconImage(d.transform, f.Icon, IsStamped(f) ? Ui.OnAccent : new Color(1, 1, 1, 0.35f), 40);
                var irt = (RectTransform)ic.transform; irt.anchorMin = irt.anchorMax = new Vector2(0.5f, 0.5f); irt.anchoredPosition = Vector2.zero;
                ic.gameObject.GetComponent<LayoutElement>().ignoreLayout = true;
                irt.sizeDelta = new Vector2(40, 40);
            }
            Ui.Label(card.transform, "A stamp is added the first time you open a scene that lists one of the family's extensions.", 26, Ui.Regular, Ui.Dim, TextAnchor.UpperLeft, true);

            // playground entry
            var pg = Pad(c, 8, 0);
            var pgc = Ui.Box(pg, Ui.Card, 36f, "Playground");
            Ui.VBox(pgc.transform, 16, 40);
            var ph = Ui.R("Head", pgc.transform);
            Ui.HBox(ph, 18, 0, TextAnchor.MiddleLeft);
            Ui.IconImage(ph, "anchor", Ui.Good, 64);
            Ui.Label(ph, "Anchor playground", 42, Ui.Bold, Color.white, TextAnchor.MiddleLeft, false);
            Ui.Label(pgc.transform, "Place a model from the library in your room and choose how it is anchored: to the floor, to a plane, or to the view.", 30, Ui.Regular, Ui.Dim, TextAnchor.UpperLeft, true);
            var go = Ui.Pill(pgc.transform, "Open the playground", () => StartPlayground(), Ui.BtnStyle.Primary, "play");
            Ui.Size(go.gameObject, -1, 104, 1);

            // extension reference
            var rf = Pad(c, 8, 0);
            var rfc = Ui.Box(rf, Ui.Card, 36f, "Reference");
            Ui.VBox(rfc.transform, 16, 40);
            var rh = Ui.R("Head", rfc.transform);
            Ui.HBox(rh, 18, 0, TextAnchor.MiddleLeft);
            Ui.IconImage(rh, "layers", Ui.Accent, 64);
            Ui.Label(rh, "Extension reference", 42, Ui.Bold, Color.white, TextAnchor.MiddleLeft, false);
            Ui.Label(rfc.transform, "Every extension with what this build does with it and the scenes in the library that list it.", 30, Ui.Regular, Ui.Dim, TextAnchor.UpperLeft, true);
            var rb = Ui.Pill(rfc.transform, "Open the reference", () => ShowExtensionReference(), Ui.BtnStyle.Primary, "layers");
            Ui.Size(rb.gameObject, -1, 104, 1);

            // families
            var gridWrap = Pad(c, 8, 0);
            int cols = Landscape ? 4 : 2;
            float gap = 24f;
            float cell = (width - Margin * 2 - gap * (cols - 1)) / cols;
            var grid = Ui.R("Grid", gridWrap);
            var gl = grid.gameObject.AddComponent<GridLayoutGroup>();
            gl.cellSize = new Vector2(cell, 360f); gl.spacing = new Vector2(gap, gap);
            gl.constraint = GridLayoutGroup.Constraint.FixedColumnCount; gl.constraintCount = cols;
            Ui.Fit(grid.gameObject);
            foreach (var f in Families) BuildFamilyCard(grid, f, cell);
            Ui.Size(Ui.Spacer(c, 0, 80).gameObject, -1, 80);
            m_labScroll.verticalNormalizedPosition = Mathf.Clamp01(keep);
        }

        private int SceneCount(Family f) { return Catalog.Entries.Count(e => e.Extensions.Any(f.Match)); }

        private void BuildFamilyCard(Transform parent, Family f, float w)
        {
            var card = Ui.Box(parent, Ui.Card, 32f, "Family " + f.Id);
            Ui.VBox(card.transform, 10, 30, TextAnchor.UpperLeft);
            var discRow = Ui.R("DiscRow", card.transform);
            Ui.HBox(discRow, 0, 0, TextAnchor.MiddleLeft);
            Ui.Size(discRow.gameObject, -1, 84, 1);
            var disc = Ui.Box(discRow, new Color(f.Color.r, f.Color.g, f.Color.b, 0.2f), 40f, "Disc");
            Ui.Size(disc.gameObject, 84, 84);
            var ic = Ui.IconImage(disc.transform, f.Icon, f.Color, 46);
            var irt = (RectTransform)ic.transform; irt.anchorMin = irt.anchorMax = new Vector2(0.5f, 0.5f); irt.anchoredPosition = Vector2.zero; irt.sizeDelta = new Vector2(46, 46);
            ic.gameObject.GetComponent<LayoutElement>().ignoreLayout = true;
            var nm = Ui.Label(card.transform, f.Name, 34, Ui.Bold, Color.white, TextAnchor.UpperLeft, true);
            nm.verticalOverflow = VerticalWrapMode.Truncate;
            Ui.Size(nm.gameObject, -1, 90);
            Ui.Label(card.transform, Ui.Plural(SceneCount(f), "scene", "scenes"), 27, Ui.Regular, Ui.Dim, TextAnchor.UpperLeft, false);
            var chipRow = Ui.R("ChipRow", card.transform);
            Ui.HBox(chipRow, 0, 0, TextAnchor.MiddleLeft);
            Ui.Size(chipRow.gameObject, -1, 48, 1);
            if (IsStamped(f)) Ui.Chip(chipRow, "Tried", Ui.Good, false, 24);
            else Ui.Chip(chipRow, "Not tried", Ui.Dim, false, 24);
            Ui.Click(card.gameObject, card, () => ShowFamily(f));
        }

        // ------------------------------------------------------------------ family page

        private void ShowFamily(Family f)
        {
            m_detEntry = null;
            m_detailsOpen = true;
            Ui.Show(m_detGroup, true);
            m_detTitle.text = f.Name;
            for (int i = m_detContent.childCount - 1; i >= 0; i--) Destroy(m_detContent.GetChild(i).gameObject);
            Ui.Size(Ui.Spacer(m_detContent, 0, 6).gameObject, -1, 6);

            var s = Section("Extensions in this family", IsStamped(f) ? "You have tried this family." : "Open one of the scenes below to try it.");
            var flow = Ui.Flow(s, 10, 10);
            foreach (var ext in f.Exts) Ui.Chip(flow.transform, ext, f.Color, false, 26);
            var listed = Catalog.Entries.SelectMany(e => e.Extensions).Where(f.Match).Distinct().ToList();
            if (f.Exts.Any(x => x.EndsWith("*")) && listed.Count > 0) Ui.Label(s, "Seen in the library: " + string.Join(", ", listed), 26, Ui.Regular, Ui.Dim, TextAnchor.UpperLeft, true);

            var scenes = Catalog.Entries.Where(e => e.Extensions.Any(f.Match)).ToList();
            var ss = Section("Scenes that use it", scenes.Count == 0 ? "None of the scenes in the library lists these extensions." : Ui.Plural(scenes.Count, "scene", "scenes"));
            foreach (var e in scenes.Take(14)) BuildSceneRow(ss, e);

            var tools = Section("What you can do");
            switch (f.Id)
            {
                case "anchors":
                    Tip(tools, "Open an AR scene, or use the flat preview to see where its nodes sit.");
                    var pgb = Ui.Pill(tools, "Anchor playground", () => { CloseDetails(); StartPlayground(); }, Ui.BtnStyle.Primary, "anchor");
                    Ui.Size(pgb.gameObject, -1, 104, 1);
                    break;
                case "interactivity":
                    Tip(tools, "In a scene, open the lab tools and turn on the behaviour monitor to watch triggers and actions fire, and the trigger volumes to see their ranges.");
                    break;
                case "video":
                    Tip(tools, "In a scene, open the lab tools and the media pipeline view to watch the buffer between the decoder and the texture.");
                    break;
                case "audio":
                    Tip(tools, "In a scene, switch spatial audio off in the lab tools to hear the same sound without positioning.");
                    break;
                case "lighting":
                    Tip(tools, "In a scene, switch lighting off in the lab tools to see the scene without its lights.");
                    break;
                case "materials":
                    Tip(tools, "In a scene, switch materials off in the lab tools to see the geometry with one plain material.");
                    break;
                case "haptics":
                    Tip(tools, "None of the scenes in the library lists a haptic extension. This button feels the phone's vibrator, so you know what a pulse is like on this device.");
                    var hb = Ui.Pill(tools, "Feel a pulse", () => { Handheld.Vibrate(); }, Ui.BtnStyle.Primary, "vibrate");
                    Ui.Size(hb.gameObject, -1, 104, 1);
                    break;
                default:
                    Tip(tools, "The extension lens outlines the parts of a scene that carry extension data. Open the lab tools in any scene.");
                    break;
            }
        }

        private void Tip(Transform parent, string text) { Ui.Label(parent, text, 30, Ui.Regular, Ui.Dim, TextAnchor.UpperLeft, true); }

        private void BuildSceneRow(Transform parent, SceneEntry e)
        {
            var row = Ui.Box(parent, new Color(1, 1, 1, 0.05f), 24f, "SceneRow");
            Ui.HBox(row.transform, 22, 0, TextAnchor.MiddleLeft);
            row.GetComponent<HorizontalLayoutGroup>().padding = new RectOffset(14, 14, 14, 14);
            Ui.Size(row.gameObject, -1, -1, 1);
            var th = Ui.R("Thumb", row.transform);
            Ui.Size(th.gameObject, 190, 120);
            var mask = Ui.Box(th, Ui.Raised, 16f, "Frame");
            Ui.Stretch((RectTransform)mask.transform);
            mask.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            if (e.Thumb != null)
            {
                var ri = Ui.R("Img", mask.transform).gameObject.AddComponent<RawImage>();
                Ui.Stretch((RectTransform)ri.transform);
                Ui.Cover(ri, e.Thumb, 190f / 120f);
            }
            var col = Ui.R("Text", row.transform);
            Ui.VBox(col, 2, 0, TextAnchor.MiddleLeft);
            Ui.Size(col.gameObject, -1, -1, 1);
            var t = Ui.Label(col, e.Title, 32, Ui.Medium, Color.white, TextAnchor.MiddleLeft, true);
            t.verticalOverflow = VerticalWrapMode.Truncate;
            Ui.Size(t.gameObject, -1, 44);
            string sub = !string.IsNullOrEmpty(e.Variant) ? e.Variant : e.FileName;
            var s = Ui.Label(col, sub, 26, Ui.Regular, Ui.Dim, TextAnchor.MiddleLeft, false);
            Ui.Size(s.gameObject, -1, 34);
            Ui.Click(row.gameObject, row, () => { CloseDetails(); ShowSheet(e); });
        }

        private void UpdateLab()
        {
            if (m_screen == Screen_.Lab && m_labDirty) RefreshLab();
            UpdateLabTools();
        }
    }
}
