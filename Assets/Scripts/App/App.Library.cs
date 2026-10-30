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
    public partial class App
    {
        private const float TabsH = 150f;
        private const int Margin = 48;

        private RectTransform m_libRoot, m_libContent, m_tabsRoot;
        private CanvasGroup m_libGroup, m_tabsGroup;
        private ScrollRect m_libScroll;
        private InputField m_search;
        private RectTransform m_searchBox, m_searchWrap;
        private bool m_searchOpen;
        private string m_query = "";
        private bool m_libDirty;
        private float m_libNextRebuild;
        private readonly Image[] m_tabIcons = new Image[3];
        private readonly Text[] m_tabLabels = new Text[3];
        private Texture2D m_logo;


        // ------------------------------------------------------------------ build

        private void BuildLibrary()
        {
            m_logo = Resources.Load<Texture2D>("App/Logo5GMAG");
            if (m_logo != null) m_logo.filterMode = FilterMode.Bilinear;
            m_libRoot = Ui.R("Library", m_root);
            Ui.Stretch(m_libRoot);
            m_libGroup = Ui.Group(m_libRoot, true);
            var bg = Ui.Box(m_libRoot, Ui.Bg, 0f, "Bg");
            Ui.Bleed((RectTransform)bg.transform);
            bg.raycastTarget = true;
            ScrollRect sr;
            m_libContent = Ui.ScrollV(m_libRoot, out sr, 36f, 0, "LibraryScroll");
            m_libScroll = sr;
            var srt = (RectTransform)sr.transform;
            Ui.Stretch(srt);
            srt.offsetMin = new Vector2(0, TabsH);
            srt.offsetMax = new Vector2(0, -BarH);
            BuildBlueBar();
        }

        private const float BarH = 130f;

        // The header of the DVB-I Android application: a dark bar with the 5G-MAG logo of the website at the start and the action at
        // the end; the app name and a line under it follow below the bar
        private void BuildBlueBar()
        {
            var bar = Ui.R("TopBar", m_libRoot);
            Ui.Place(bar, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), Vector2.zero, new Vector2(0, BarH));
            var bg = Ui.Box(bar, Ui.Bg, 0f, "Bg");
            Ui.Stretch((RectTransform)bg.transform);
            ((RectTransform)bg.transform).offsetMin = new Vector2(-1200, 0);
            ((RectTransform)bg.transform).offsetMax = new Vector2(1200, 1200);       // up over the status area
            bg.raycastTarget = true;
            var row = Ui.R("Row", bar);
            Ui.Stretch(row);
            var h = Ui.HBox(row, 20, 0, TextAnchor.MiddleCenter);
            h.padding = new RectOffset(44, 36, 0, 0);
            if (m_logo != null)
            {
                var lg = Ui.R("Logo", row);
                var ri = lg.gameObject.AddComponent<RawImage>();
                ri.texture = m_logo; ri.raycastTarget = false;
                Ui.Size(lg.gameObject, 78f * m_logo.width / m_logo.height, 78f);
            }
            Ui.Size(Ui.Spacer(row, 1).gameObject, -1, -1, 1);
            var search = Ui.IconButton(row, "search", 96, () => { m_searchOpen = !m_searchOpen; if (!m_searchOpen) m_query = ""; m_libDirty = true; m_libNextRebuild = 0; }, false, false);
        }

        private void BuildTabs()
        {
            m_tabsRoot = Ui.R("Tabs", m_root);
            Ui.Place(m_tabsRoot, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), Vector2.zero, new Vector2(0, TabsH));
            m_tabsGroup = Ui.Group(m_tabsRoot, true);
            var bg = Ui.Box(m_tabsRoot, new Color32(0x0D, 0x0D, 0x0D, 0xFF), 0f, "Bg");
            Ui.Bleed((RectTransform)bg.transform);
            ((RectTransform)bg.transform).offsetMax = new Vector2(1200, 0);      // down and sideways only, never up over the screen
            bg.raycastTarget = true;
            var line = Ui.Box(m_tabsRoot, Ui.Line, 0f, "Line");
            Ui.Place((RectTransform)line.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), Vector2.zero, new Vector2(0, 2));
            var row = Ui.R("Row", m_tabsRoot);
            Ui.Stretch(row);
            Ui.HBox(row, 0, 0, TextAnchor.MiddleCenter, true);
            string[] names = { "Library", "Lab", "Settings" };
            string[] icons = { "grid", "lab", "gear" };
            Action[] acts = { ShowLibrary, ShowLab, ShowSettings };
            for (int i = 0; i < 3; i++)
            {
                int k = i;
                var cell = Ui.Box(row, new Color(0, 0, 0, 0), 0f, "Tab" + i);
                Ui.Size(cell.gameObject, -1, -1, 1, 1);
                Ui.VBox(cell.transform, 6, 0, TextAnchor.MiddleCenter, false);
                m_tabIcons[i] = Ui.IconImage(cell.transform, icons[i], Ui.Dim, 52);
                m_tabLabels[i] = Ui.Label(cell.transform, names[i], 28, Ui.Medium, Ui.Dim, TextAnchor.MiddleCenter, false);
                Ui.Click(cell.gameObject, cell, acts[k]);
            }
        }

        private void SetTabs()
        {
            int sel = m_screen == Screen_.Library ? 0 : (m_screen == Screen_.Lab ? 1 : (m_screen == Screen_.Settings ? 2 : -1));
            for (int i = 0; i < 3; i++)
            {
                Color c = i == sel ? Ui.Accent : Ui.Dim;
                if (m_tabIcons[i] != null) m_tabIcons[i].color = c;
                if (m_tabLabels[i] != null) m_tabLabels[i].color = c;
            }
        }

        private void OnLayoutChanged()
        {
            m_libDirty = true;
            m_labDirty = true;
        }

        // ------------------------------------------------------------------ content

        private void RefreshLibrary() { m_libDirty = true; m_libNextRebuild = 0f; }

        // the content grows while it is built, so a scroll position kept from before is only right once the layout has settled
        private bool m_libToTop = true;
        private System.Collections.IEnumerator ScrollTopNextFrame(ScrollRect sr)
        {
            yield return null;
            yield return null;
            if (sr != null) sr.verticalNormalizedPosition = 1f;
        }

        private void UpdateLibrary()
        {
            if (m_screen != Screen_.Library || !m_libDirty || m_libContent == null) return;
            if (Time.unscaledTime < m_libNextRebuild) return;
            m_libNextRebuild = Time.unscaledTime + 0.35f;
            m_libDirty = false;
            RebuildLibrary();
        }

        // App guidance, not specification text: what to do in this app to see a scene's feature at work
        public static List<string> TryHints(SceneEntry e)
        {
            var h = new List<string>();
            if (e.Names.Contains("TRIGGER_PROXIMITY")) h.Add("Switch to the Walk camera and move toward the objects, with the interactivity monitor on.");
            if (e.Names.Contains("TRIGGER_COLLISION")) h.Add("Turn on ranges and positions to see the collision boxes, and watch the interactivity monitor.");
            if (e.Names.Contains("TRIGGER_VISIBILITY")) h.Add("Orbit so the object leaves and enters the view, with the interactivity monitor on.");
            if (e.Names.Contains("TRIGGER_USER_INPUT")) h.Add("Tap the objects, with the interactivity monitor on.");
            if (e.TrackableTypes.Contains(4)) h.Add("Show the marker image from the scene's textures folder to the camera.");
            if (e.TrackableTypes.Contains(0) || e.TrackableTypes.Contains(3)) h.Add("Move the phone slowly over a surface until it is found.");
            if (e.UsesMedia) h.Add("Use the media row to pause and resume, and the media pipeline view to watch the buffer.");
            return h;
        }

        public static List<KeyValuePair<string, Color>> Badges(SceneEntry e)
        {
            var list = new List<KeyValuePair<string, Color>>();
            bool video = e.Has("MPEG_texture_video") || e.Media.Any(m => (m.Mime ?? "").StartsWith("video"));
            bool audio = e.Has("MPEG_audio_spatial") || e.Media.Any(m => (m.Mime ?? "").StartsWith("audio"));
            if (video) list.Add(new KeyValuePair<string, Color>("Video", Ui.Accent));
            if (audio) list.Add(new KeyValuePair<string, Color>("Audio", Ui.Audio));
            if (e.UsesAnchor) list.Add(new KeyValuePair<string, Color>("AR", Ui.Good));
            if (e.UsesInteractivity) list.Add(new KeyValuePair<string, Color>("Interactive", Ui.Warn));
            if (e.UsesHaptics) list.Add(new KeyValuePair<string, Color>("Haptics", Ui.Bad));
            return list;
        }

        private List<string> Recent()
        {
            return PlayerPrefs.GetString("recent", "").Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries).ToList();
        }

        private void RememberRecent(SceneEntry e)
        {
            var r = Recent();
            r.Remove(e.Uri);
            r.Insert(0, e.Uri);
            if (r.Count > 8) r.RemoveRange(8, r.Count - 8);
            PlayerPrefs.SetString("recent", string.Join("\n", r));
            PlayerPrefs.Save();
        }

        private void RebuildLibrary()
        {
            float keep = m_libScroll.verticalNormalizedPosition;
            // the search box survives a rebuild: destroying it would close the keyboard after every letter
            if (!m_searchOpen && m_searchWrap != null) { Destroy(m_searchWrap.gameObject); m_searchWrap = null; m_search = null; }
            for (int i = m_libContent.childCount - 1; i >= 0; i--)
            {
                var ch = m_libContent.GetChild(i);
                if (m_searchWrap != null && ch == m_searchWrap) continue;
                Destroy(ch.gameObject);
            }
            var c = m_libContent;
            float width = m_root.rect.width;

            var title = Pad(c, 8, 4);
            Ui.Label(title, ProductName, 56, Ui.Medium, Color.white, TextAnchor.MiddleLeft, false);
            Ui.Label(title, "MPEG-I Scene Description", 28, Ui.Regular, Ui.Dim, TextAnchor.MiddleLeft, false);

            if (m_searchOpen)
            {
                if (m_searchWrap == null) BuildSearchBox(c);
                else m_searchWrap.SetAsLastSibling();
            }

            var entries = Catalog.Entries;
            if (entries.Count == 0)
            {
                BuildEmptyState(c);
                m_libScroll.verticalNormalizedPosition = 1f;
                return;
            }

            if (!string.IsNullOrEmpty(m_query))
            {
                string q = m_query.ToLowerInvariant();
                var hits = entries.Where(e => (e.Title + " " + e.Summary + " " + string.Join(" ", e.Tags) + " " + string.Join(" ", e.Extensions) + " " + e.FileName).ToLowerInvariant().Contains(q)).ToList();
                AddRow(c, "Results", hits, width);
                if (hits.Count == 0) Ui.Label(Pad(c), "Nothing matches \"" + m_query + "\".", 34, Ui.Regular, Ui.Dim);
                return;
            }

            // hero: the last scene opened, else the first one with a picture
            SceneEntry hero = null;
            var rec = Recent();
            if (rec.Count > 0) hero = entries.FirstOrDefault(e => e.Uri == rec[0]);
            if (hero == null) hero = entries.FirstOrDefault(e => e.Thumb != null && e.Tags.Contains("demo") && !e.UsesAnchor) ?? entries.FirstOrDefault(e => e.Thumb != null) ?? entries[0];
            BuildHero(c, hero, width, rec.Count > 0 && hero.Uri == rec[0]);

            var recent = rec.Select(u => entries.FirstOrDefault(e => e.Uri == u)).Where(e => e != null).ToList();
            if (recent.Count > 1) AddRow(c, "Recently opened", recent, width);
            AddRow(c, "Demos", entries.Where(e => e.Tags.Contains("demo")).ToList(), width);
            AddRow(c, "AR scenes", entries.Where(e => e.UsesAnchor).ToList(), width);
            AddRow(c, "Interactivity", entries.Where(e => e.UsesInteractivity).ToList(), width);
            AddRow(c, "Video and audio", entries.Where(e => e.UsesMedia).ToList(), width);
            AddRow(c, "Test scenes", entries.Where(e => e.Tags.Contains("testing")).ToList(), width);
            var rest = entries.Where(e => !e.Tags.Contains("demo") && !e.Tags.Contains("testing") && !e.UsesAnchor && !e.UsesInteractivity && !e.UsesMedia).ToList();
            AddRow(c, "More scenes", rest, width);

            var foot = Pad(c);
            string status = Catalog.PendingPeeks > 0 ? "Reading scene details (" + Catalog.PendingPeeks + " left)" : Ui.Plural(entries.Count, "scene", "scenes");
            Ui.Label(foot, status + " · " + ShortPath(Catalog.PathsFile), 26, Ui.Regular, Ui.Dim, TextAnchor.MiddleLeft);
            Ui.Size(Ui.Spacer(c, 0, 60).gameObject, -1, 60);
            m_libScroll.verticalNormalizedPosition = Mathf.Clamp01(keep);
            if (m_libToTop) { m_libToTop = false; StartCoroutine(ScrollTopNextFrame(m_libScroll)); }
        }

        public static void BrandMark(Transform parent, float size)
        {
            var box = Ui.Box(parent, Ui.Accent, size * 0.24f, "Brand");
            Ui.Size(box.gameObject, size, size);
            var t = Ui.Label(box.transform, "XR", (int)(size * 0.42f), Ui.Bold, Ui.OnAccent, TextAnchor.MiddleCenter, false);
            Ui.Stretch((RectTransform)t.transform);
        }

        private static string ShortPath(string p) { return string.IsNullOrEmpty(p) ? "" : (p.Length > 60 ? "..." + p.Substring(p.Length - 57) : p); }

        private RectTransform Pad(Transform parent, int top = 0, int bottom = 0)
        {
            var rt = Ui.R("Pad", parent);
            Ui.VBox(rt, 0, 0);
            rt.GetComponent<VerticalLayoutGroup>().padding = new RectOffset(Margin, Margin, top, bottom);
            return rt;
        }

        private void BuildSearchBox(Transform c)
        {
            var wrap = Pad(c, 0, 0);
            m_searchWrap = wrap;
            var box = Ui.Box(wrap, Ui.Raised, 40f, "Search");
            Ui.Size(box.gameObject, -1, 100, 1);
            var hb = Ui.HBox(box.transform, 16, 0, TextAnchor.MiddleLeft);
            hb.padding = new RectOffset(34, 34, 0, 0);
            Ui.IconImage(box.transform, "search", Ui.Dim, 44);
            var textRt = Ui.R("InputArea", box.transform);
            Ui.Size(textRt.gameObject, -1, -1, 1, 1);
            var input = textRt.gameObject.AddComponent<InputField>();
            var txt = Ui.Label(textRt, "", 38, Ui.Regular, Color.white, TextAnchor.MiddleLeft, false, "Text");
            Ui.Stretch((RectTransform)txt.transform);
            txt.raycastTarget = true;
            var ph = Ui.Label(textRt, "Search scenes, extensions, tags", 38, Ui.Regular, new Color(1, 1, 1, 0.4f), TextAnchor.MiddleLeft, false, "Placeholder");
            Ui.Stretch((RectTransform)ph.transform);
            input.textComponent = txt; input.placeholder = ph; input.text = m_query;
            input.onValueChanged.AddListener(v => { m_query = v; m_libDirty = true; m_libNextRebuild = Time.unscaledTime + 0.3f; });
            m_search = input;
        }

        private void BuildEmptyState(Transform c)
        {
            var wrap = Pad(c, 100, 0);
            var card = Ui.Box(wrap, Ui.Card, 32f, "Empty");
            Ui.VBox(card.transform, 24, 56, TextAnchor.UpperLeft);
            Ui.IconImage(card.transform, "cube", Ui.Accent, 110);
            Ui.Label(card.transform, Catalog.Loading ? "Looking for scenes" : "No scenes yet", 52, Ui.Bold, Color.white);
            string msg = string.IsNullOrEmpty(Catalog.Problem) ? "Add the address of a scene or of a list of scenes." : Catalog.Problem;
            Ui.Label(card.transform, msg + "\n\nThe player reads a list with one scene address per line from:", 34, Ui.Regular, Ui.Dim);
            var path = Ui.Label(card.transform, Catalog.PathsFile, 28, Ui.Regular, Color.white);
            Ui.Pill(card.transform, "Add a source", () => ShowSettings(), Ui.BtnStyle.Primary, "link");
        }

        private void BuildHero(Transform c, SceneEntry e, float width, bool recent)
        {
            var wrap = Pad(c, 12, 0);
            float w = width - Margin * 2;
            float h = Landscape ? 520f : Mathf.Min(620f, w * 0.62f);
            var card = Ui.Box(wrap, Ui.Card, 36f, "Hero");
            Ui.Size(card.gameObject, -1, h, 1);
            card.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            BuildThumb((RectTransform)card.transform, e, w / h, true);
            var grad = Ui.Box(card.transform, Color.white, 0f, "Grad");
            grad.sprite = Ui.GradUp; grad.type = Image.Type.Simple;
            Ui.Stretch((RectTransform)grad.transform);
            var col = Ui.R("Text", card.transform);
            Ui.Place(col, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(0, 40), new Vector2(-96, 0));
            Ui.VBox(col, 12, 0, TextAnchor.LowerLeft);
            Ui.Fit(col.gameObject);
            var kick = Ui.Label(col, recent ? "CONTINUE" : "FEATURED", 26, Ui.Medium, Ui.Accent, TextAnchor.MiddleLeft, false);
            Ui.Label(col, e.Title, 60, Ui.Bold, Color.white, TextAnchor.MiddleLeft, true);
            if (!string.IsNullOrEmpty(e.Summary))
            {
                var s = Ui.Label(col, e.Summary, 32, Ui.Regular, new Color(1, 1, 1, 0.82f), TextAnchor.UpperLeft, true);
                s.verticalOverflow = VerticalWrapMode.Truncate;
                Ui.Size(s.gameObject, -1, 80);
            }
            var row = Ui.R("Actions", col);
            Ui.HBox(row, 16, 0, TextAnchor.MiddleLeft);
            Ui.Size(row.gameObject, -1, 104);
            Ui.Pill(row, "Open scene", () => OpenScene(e), Ui.BtnStyle.Primary, "play");
            foreach (var b in Badges(e)) Ui.Chip(row, b.Key, b.Value);
            Ui.Click(card.gameObject, card, () => ShowSheet(e));
            // buttons inside the card must stay clickable: the card click is on the background only
        }

        private void BuildThumb(RectTransform parent, SceneEntry e, float aspect, bool darken)
        {
            var holder = Ui.R("Thumb", parent);
            Ui.Stretch(holder);
            if (e.Thumb != null)
            {
                var ri = holder.gameObject.AddComponent<RawImage>();
                Ui.Cover(ri, e.Thumb, aspect);
                if (darken) ri.color = new Color(0.85f, 0.85f, 0.85f, 1f);
            }
            else
            {
                var ph = Ui.Box(holder, Ui.Raised, 0f, "Placeholder");
                Ui.Stretch((RectTransform)ph.transform);
                var ic = Ui.IconImage(holder, FamilyIconFor(e), new Color(1, 1, 1, 0.25f), 120);
                var irt = (RectTransform)ic.transform;
                irt.anchorMin = irt.anchorMax = new Vector2(0.5f, 0.5f); irt.anchoredPosition = Vector2.zero;
                if (irt.GetComponent<LayoutElement>() != null) irt.GetComponent<LayoutElement>().ignoreLayout = true;
                irt.sizeDelta = new Vector2(120, 120);
            }
        }

        private string FamilyIconFor(SceneEntry e)
        {
            if (e.UsesAnchor) return "anchor";
            if (e.UsesInteractivity) return "touch";
            if (e.UsesMedia) return "video";
            return "cube";
        }

        private void AddRow(Transform c, string title, List<SceneEntry> items, float width)
        {
            if (items.Count == 0) return;
            var head = Pad(c, 8, 0);
            var hb = Ui.R("RowHead", head);
            Ui.HBox(hb, 16, 0, TextAnchor.LowerLeft);
            Ui.Label(hb, title, 42, Ui.Bold, Color.white, TextAnchor.LowerLeft, false);
            var cnt = Ui.Label(hb, Ui.Plural(items.Count, "scene", "scenes"), 28, Ui.Regular, Ui.Dim, TextAnchor.LowerLeft, false);
            ScrollRect sr;
            float cardW = Landscape ? 560f : 520f;
            var content = Ui.ScrollH(c, out sr, 24, Margin, 540f, "Row " + title);
            foreach (var e in items) BuildCard(content, e, cardW);
        }

        private void BuildCard(Transform parent, SceneEntry e, float w)
        {
            var card = Ui.Box(parent, Ui.Card, 28f, "Card");
            Ui.Size(card.gameObject, w, 520);
            card.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            Ui.VBox(card.transform, 0, 0, TextAnchor.UpperLeft);
            float th = w / 1.6f;
            var thumb = Ui.R("ThumbFrame", card.transform);
            Ui.Size(thumb.gameObject, -1, th);
            BuildThumb(thumb, e, 1.6f, false);
            var chips = Ui.R("Chips", thumb);
            Ui.Place(chips, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 0), new Vector2(18, 16), new Vector2(-36, 44));
            Ui.HBox(chips, 8, 0, TextAnchor.LowerLeft);
            foreach (var b in Badges(e).Take(3)) Ui.Chip(chips, b.Key, b.Value, true, 24);
            var tx = Ui.R("Text", card.transform);
            Ui.VBox(tx, 6, 0, TextAnchor.UpperLeft);
            tx.GetComponent<VerticalLayoutGroup>().padding = new RectOffset(26, 26, 20, 16);
            Ui.Size(tx.gameObject, -1, -1, 1, 1);
            var t = Ui.Label(tx, e.Title, 34, Ui.Medium, Color.white, TextAnchor.UpperLeft, true);
            t.verticalOverflow = VerticalWrapMode.Truncate;
            Ui.Size(t.gameObject, -1, 48);
            string sub = e.PeekFailed ? "Could not be reached" : !string.IsNullOrEmpty(e.Variant) ? e.Variant : (!string.IsNullOrEmpty(e.Summary) ? e.Summary : e.Source);
            var s = Ui.Label(tx, sub, 27, Ui.Regular, Ui.Dim, TextAnchor.UpperLeft, true);
            s.verticalOverflow = VerticalWrapMode.Truncate;
            Ui.Size(s.gameObject, -1, 76);
            Ui.Click(card.gameObject, card, () => ShowSheet(e));
        }
    }
}
