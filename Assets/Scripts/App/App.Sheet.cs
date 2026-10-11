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
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace rt.xr.unity.app
{
    public enum StepState { Pending, Working, Ok, Warn, Failed, Skipped }

    public partial class App
    {
        // ------------------------------------------------------------------ scene sheet

        private RectTransform m_sheetRoot, m_sheetPanel;
        private CanvasGroup m_sheetGroup;
        private bool m_sheetOpen;
        private SceneEntry m_sheetEntry;
        private ScrollRect m_sheetScroll;
        private RectTransform m_sheetContent;

        private void BuildSheet()
        {
            m_sheetRoot = Ui.R("Sheet", m_root);
            Ui.Stretch(m_sheetRoot);
            m_sheetGroup = Ui.Group(m_sheetRoot, false);
            var scrim = Ui.Box(m_sheetRoot, Ui.Scrim, 0f, "Scrim");
            Ui.Bleed((RectTransform)scrim.transform);
            Ui.Click(scrim.gameObject, scrim, CloseSheet);
            var panel = Ui.Box(m_sheetRoot, new Color32(0x18, 0x18, 0x18, 0xFF), 44f, "Panel");
            m_sheetPanel = (RectTransform)panel.transform;
            panel.raycastTarget = true;
            m_sheetPanel.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            ScrollRect sr;
            m_sheetContent = Ui.ScrollV(m_sheetPanel, out sr, 22, 0, "SheetScroll");
            m_sheetScroll = sr;
            Ui.Stretch((RectTransform)sr.transform);
        }

        private void LayoutSheet()
        {
            float w = m_root.rect.width, h = m_root.rect.height;
            float pw = Mathf.Min(w, 1280f);
            float ph = Mathf.Min(h * (Landscape ? 0.96f : 0.86f), 1750f);
            Ui.Place(m_sheetPanel, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, Landscape ? 10 : -44), new Vector2(pw, ph + (Landscape ? 0 : 44)));
        }

        private void ShowSheet(SceneEntry e)
        {
            m_sheetEntry = e;
            LayoutSheet();
            RebuildSheet();
            m_sheetOpen = true;
            Ui.Show(m_sheetGroup, true);
            StartCoroutine(Catalog.Preflight(e, () => { if (m_sheetOpen && m_sheetEntry == e) RebuildSheet(); }));
        }

        private void CloseSheet()
        {
            m_sheetOpen = false;
            if (m_sheetGroup != null) Ui.Show(m_sheetGroup, false);
        }

        private void RebuildSheet()
        {
            var e = m_sheetEntry;
            float keep = m_sheetScroll.verticalNormalizedPosition;
            for (int i = m_sheetContent.childCount - 1; i >= 0; i--) Destroy(m_sheetContent.GetChild(i).gameObject);
            var c = m_sheetContent;
            float pw = m_sheetPanel.rect.width;

            var grab = Ui.R("GrabRow", c);
            Ui.Size(grab.gameObject, -1, 40);
            var g = Ui.Box(grab, new Color(1, 1, 1, 0.3f), 6f, "Grab");
            Ui.Place((RectTransform)g.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -6), new Vector2(110, 10));

            var th = Ui.R("Thumb", c);
            Ui.Size(th.gameObject, -1, Landscape ? 340 : 600);
            var thumbMask = Ui.Box(th, Color.white, 0f, "Frame");
            Ui.Stretch((RectTransform)thumbMask.transform);
            thumbMask.color = new Color(0, 0, 0, 0);
            BuildThumb(th, e, pw / (Landscape ? 340f : 600f), false);

            var body = Ui.R("Body", c);
            Ui.VBox(body, 18, 0);
            body.GetComponent<VerticalLayoutGroup>().padding = new RectOffset(52, 52, 8, 52);
            var chips = Ui.R("Chips", body);
            Ui.HBox(chips, 10, 0, TextAnchor.MiddleLeft);
            Ui.Size(chips.gameObject, -1, 44);
            foreach (var b in Badges(e)) Ui.Chip(chips, b.Key, b.Value);
            foreach (var t in e.Tags) if (t == "demo" || t == "testing") Ui.Chip(chips, t == "demo" ? "Demo" : "Test scene", Ui.Dim);
            if (!string.IsNullOrEmpty(e.Variant)) Ui.Chip(chips, e.Variant, Ui.Dim);
            Ui.Label(body, e.Title, 60, Ui.Bold, Color.white, TextAnchor.UpperLeft, true);
            if (!string.IsNullOrEmpty(e.Summary)) Ui.Label(body, e.Summary, 34, Ui.Regular, new Color(1, 1, 1, 0.82f), TextAnchor.UpperLeft, true);

            // content notes: the trigger, action and anchor names the content itself mentions
            if (e.Names.Count > 0)
            {
                var notes = Ui.Flow(body, 10, 10);
                foreach (var n in e.Names) Ui.Chip(notes.transform, Humanize(n), n.StartsWith("TRIGGER") ? Ui.Warn : (n.StartsWith("ACTION") ? Ui.Accent : Ui.Good), false, 24);
            }

            var kv = Ui.R("Facts", body);
            Ui.VBox(kv, 0, 0);
            Fact(kv, "Source", e.Source);
            Fact(kv, "File", e.FileName);
            if (e.Media.Count == 0) Fact(kv, "Media", e.UsesMedia ? "Listed by the scene" : "None");
            else
            {
                int missing = e.Media.Count(m => m.Status == "missing"), found = e.Media.Count(m => m.Status == "found");
                string st = (found + missing) == 0 ? "Checking" : (missing == 0 ? "All found" : Ui.Plural(missing, "file not found", "files not found"));
                Fact(kv, "Media", Ui.Plural(e.Media.Count, "file", "files") + ", " + st.ToLowerInvariant(), missing > 0 ? Ui.Warn : (Color?)null);
            }
            Fact(kv, "Needs", e.UsesAnchor ? "Camera and motion tracking (AR)" : (e.Media.Any(m => m.Uri.StartsWith("http")) ? "Network for media" : "Nothing special"));
            if (e.Extensions.Length > 0) Fact(kv, "Extensions", Ui.Plural(e.Extensions.Length, "listed", "listed"));

            var hints = TryHints(e);
            if (hints.Count > 0)
            {
                var hb = Ui.Box(body, new Color(0f, 0.63f, 0.82f, 0.12f), 28f, "Try");
                Ui.VBox(hb.transform, 8, 28);
                Ui.Label(hb.transform, "Try", 28, Ui.Bold, Ui.Accent, TextAnchor.UpperLeft, false);
                foreach (var t in hints) Ui.Label(hb.transform, t, 28, Ui.Regular, Color.white, TextAnchor.UpperLeft, true);
            }

            var btns = Ui.R("Buttons", body);
            Ui.HBox(btns, 18, 0, TextAnchor.MiddleLeft);
            Ui.Size(btns.gameObject, -1, 112);
            var open = Ui.Pill(btns, e.UsesAnchor ? "Open in AR" : "Open scene", () => OpenScene(e), Ui.BtnStyle.Primary, "play");
            Ui.Size(open.gameObject, -1, 112, 1);
            var det = Ui.Pill(btns, "Details", () => ShowDetails(e), Ui.BtnStyle.Outline, "info");
            if (e.UsesAnchor)
            {
                var pv = Ui.Pill(body, "Preview without AR", () => OpenScene(e, true), Ui.BtnStyle.Ghost, "eye");
                Ui.Size(pv.gameObject, -1, 104, 1);
            }
            StartCoroutine(FitSheetNextFrame(keep));
        }

        private IEnumerator FitSheetNextFrame(float keep)
        {
            yield return null;
            LayoutRebuilder.ForceRebuildLayoutImmediate(m_sheetContent);
            float need = LayoutUtility.GetPreferredHeight(m_sheetContent);
            float max = Mathf.Min(m_root.rect.height * (Landscape ? 0.96f : 0.9f), 1900f);
            float h = Mathf.Min(max, need + 30f);
            float pw = Mathf.Min(m_root.rect.width, 1280f);
            Ui.Place(m_sheetPanel, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, Landscape ? 10 : -44), new Vector2(pw, h + (Landscape ? 0 : 44)));
            m_sheetScroll.verticalNormalizedPosition = Mathf.Clamp01(keep);
        }

        private void Fact(Transform parent, string k, string v, Color? color = null)
        {
            var row = Ui.R("Fact", parent);
            Ui.HBox(row, 24, 0, TextAnchor.UpperLeft);
            row.GetComponent<HorizontalLayoutGroup>().padding = new RectOffset(0, 0, 18, 18);
            Ui.Size(row.gameObject, -1, -1, 1);
            var kk = Ui.Label(row, k, 32, Ui.Regular, Ui.Dim, TextAnchor.UpperLeft, false);
            Ui.Size(kk.gameObject, 210);
            var vv = Ui.Label(row, v, 32, Ui.Regular, color ?? Color.white, TextAnchor.UpperLeft, true);
            Ui.Size(vv.gameObject, -1, -1, 1);
            var line = Ui.Box(parent, Ui.Line, 0f, "Line");
            Ui.Size(line.gameObject, -1, 2);
        }

        // ------------------------------------------------------------------ opening screen

        private RectTransform m_openRoot;
        private CanvasGroup m_openGroup;
        private bool m_openingOpen;
        private Text m_openTitle, m_openKicker;
        private RectTransform m_openThumbHolder;
        private readonly Image[] m_stepIcon = new Image[3];
        private readonly Text[] m_stepDetail = new Text[3];
        private readonly StepState[] m_stepState = new StepState[3];
        private RectTransform m_openNote;

        private void BuildOpening()
        {
            m_openRoot = Ui.R("Opening", m_root);
            Ui.Stretch(m_openRoot);
            m_openGroup = Ui.Group(m_openRoot, false);
            var bg = Ui.Box(m_openRoot, Ui.Bg, 0f, "Bg");
            Ui.Bleed((RectTransform)bg.transform);
            bg.raycastTarget = true;
            m_openThumbHolder = Ui.R("Thumb", m_openRoot);
            Ui.Stretch(m_openThumbHolder);
            var dim = Ui.Box(m_openRoot, new Color(0.07f, 0.07f, 0.07f, 0.72f), 0f, "Dim");
            Ui.Bleed((RectTransform)dim.transform);
            var col = Ui.R("Col", m_openRoot);
            Ui.Place(col, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900, 0));
            Ui.VBox(col, 30, 0, TextAnchor.MiddleLeft);
            Ui.Fit(col.gameObject);
            m_openKicker = Ui.Label(col, "OPENING", 28, Ui.Medium, Ui.Accent, TextAnchor.MiddleLeft, false);
            m_openTitle = Ui.Label(col, "", 68, Ui.Bold, Color.white, TextAnchor.MiddleLeft, true);
            Ui.Size(Ui.Spacer(col, 0, 10).gameObject, -1, 10);
            string[] names = { "Scene file", "Media files", "Building the scene" };
            for (int i = 0; i < 3; i++)
            {
                var row = Ui.R("Step" + i, col);
                Ui.HBox(row, 24, 0, TextAnchor.MiddleLeft);
                Ui.Size(row.gameObject, -1, 64, 1);
                m_stepIcon[i] = Ui.IconImage(row, "check", Ui.Dim, 48);
                var l = Ui.Label(row, names[i], 40, Ui.Medium, Color.white, TextAnchor.MiddleLeft, false);
                Ui.Size(l.gameObject, -1, -1, 1);
                m_stepDetail[i] = Ui.Label(row, "", 32, Ui.Regular, Ui.Dim, TextAnchor.MiddleRight, false);
            }
            m_openNote = Ui.R("Note", col);
            Ui.HBox(m_openNote, 10, 0, TextAnchor.MiddleLeft);
        }

        private void ShowOpening(SceneEntry e)
        {
            m_openingOpen = true;
            m_openTitle.text = e.Title;
            for (int i = m_openThumbHolder.childCount - 1; i >= 0; i--) Destroy(m_openThumbHolder.GetChild(i).gameObject);
            if (e.Thumb != null)
            {
                var ri = Ui.R("Img", m_openThumbHolder).gameObject.AddComponent<RawImage>();
                Ui.Stretch((RectTransform)ri.transform);
                Ui.Cover(ri, e.Thumb, (float)m_root.rect.width / Mathf.Max(1f, m_root.rect.height));
            }
            for (int i = 0; i < 3; i++) SetStep(i, StepState.Pending, "");
            for (int i = m_openNote.childCount - 1; i >= 0; i--) Destroy(m_openNote.GetChild(i).gameObject);
            if (e.UsesAnchor) Ui.Chip(m_openNote, "AR scene", Ui.Good);
            Ui.Show(m_openGroup, true);
        }

        private void HideOpening()
        {
            m_openingOpen = false;
            Ui.Show(m_openGroup, false);
        }

        private void SetStep(int i, StepState s, string detail)
        {
            m_stepState[i] = s;
            var icon = m_stepIcon[i];
            icon.sprite = Ui.Icon(s == StepState.Ok ? "check" : (s == StepState.Warn ? "warning" : (s == StepState.Failed ? "close" : (s == StepState.Working ? "refresh" : "check"))));
            icon.color = s == StepState.Ok ? Ui.Good : (s == StepState.Warn ? Ui.Warn : (s == StepState.Failed ? Ui.Bad : (s == StepState.Working ? Ui.Accent : new Color(1, 1, 1, 0.25f))));
            icon.rectTransform.localRotation = Quaternion.identity;
            m_stepDetail[i].text = detail;
            m_stepDetail[i].color = s == StepState.Warn ? Ui.Warn : (s == StepState.Failed ? Ui.Bad : Ui.Dim);
            if (s == StepState.Warn && i == 1 && m_current != null && !m_openNoteMedia)
            {
                m_openNoteMedia = true;
                Ui.Chip(m_openNote, "Opens without the missing media", Ui.Warn);
            }
            if (s == StepState.Pending) m_openNoteMedia = false;
        }
        private bool m_openNoteMedia;

        private void UpdateOpening()
        {
            if (!m_openingOpen) return;
            for (int i = 0; i < 3; i++)
                if (m_stepState[i] == StepState.Working) m_stepIcon[i].rectTransform.Rotate(0, 0, -360f * Time.unscaledDeltaTime);
        }

        // ------------------------------------------------------------------ dialogs and toast

        private RectTransform m_dlgRoot, m_dlgCard;
        private CanvasGroup m_dlgGroup;
        private bool m_dlgOpen;
        private RectTransform m_toastRoot;
        private CanvasGroup m_toastGroup;
        private Text m_toastText;
        private float m_toastUntil;

        private void BuildDialogs()
        {
            m_dlgRoot = Ui.R("Dialog", m_root);
            Ui.Stretch(m_dlgRoot);
            m_dlgGroup = Ui.Group(m_dlgRoot, false);
            var scrim = Ui.Box(m_dlgRoot, Ui.Scrim, 0f, "Scrim");
            Ui.Bleed((RectTransform)scrim.transform);
            scrim.raycastTarget = true;
            var card = Ui.Box(m_dlgRoot, Ui.Card, 40f, "Card");
            m_dlgCard = (RectTransform)card.transform;
            card.raycastTarget = true;
            Ui.Place(m_dlgCard, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(960, 0));
            Ui.VBox(m_dlgCard, 24, 56, TextAnchor.UpperLeft);
            Ui.Fit(m_dlgCard.gameObject);

            m_toastRoot = Ui.R("Toast", m_root);
            Ui.Place(m_toastRoot, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 260), new Vector2(0, 0));
            m_toastGroup = Ui.Group(m_toastRoot, false);
            var tb = Ui.Box(m_toastRoot, new Color(0.1f, 0.1f, 0.1f, 0.94f), 40f, "Bg");
            Ui.Stretch((RectTransform)tb.transform);
            tb.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            HBox_(m_toastRoot);
            m_toastText = Ui.Label(m_toastRoot, "", 34, Ui.Medium, Color.white, TextAnchor.MiddleCenter, true);
            m_toastGroup.interactable = false; m_toastGroup.blocksRaycasts = false;
        }

        private static void HBox_(RectTransform rt)
        {
            var h = Ui.HBox(rt, 0, 0, TextAnchor.MiddleCenter);
            h.padding = new RectOffset(48, 48, 26, 26);
            Ui.Fit(rt.gameObject, true, true);
        }

        public void Toast(string text, float seconds = 2.4f)
        {
            if (m_toastText == null) return;
            m_toastText.text = text;
            m_toastUntil = Time.unscaledTime + seconds;
            m_toastGroup.alpha = 1f;
        }

        private void UpdateToast()
        {
            UpdateOpening();
            if (m_toastGroup == null) return;
            float target = Time.unscaledTime < m_toastUntil ? 1f : 0f;
            m_toastGroup.alpha = Mathf.MoveTowards(m_toastGroup.alpha, target, Time.unscaledDeltaTime * 6f);
        }

        private bool DialogOpen() { return m_dlgOpen; }
        private void CloseDialogs() { m_dlgOpen = false; Ui.Show(m_dlgGroup, false); }

        private void ClearDialog()
        {
            for (int i = m_dlgCard.childCount - 1; i >= 0; i--) Destroy(m_dlgCard.GetChild(i).gameObject);
        }

        public void ShowError(string title, string detail, Action retry)
        {
            ClearDialog();
            var c = m_dlgCard;
            Ui.IconImage(c, "warning", Ui.Warn, 84);
            Ui.Label(c, title, 54, Ui.Bold, Color.white, TextAnchor.UpperLeft, true);
            var box = Ui.Box(c, new Color32(0x14, 0x14, 0x14, 0xFF), 24f, "Detail");
            Ui.VBox(box.transform, 0, 28);
            var d = Ui.Label(box.transform, detail, 28, Ui.Regular, Ui.Dim, TextAnchor.UpperLeft, true);
            var acts = Ui.R("Actions", c);
            Ui.HBox(acts, 16, 0, TextAnchor.MiddleLeft);
            Ui.Size(acts.gameObject, -1, 104);
            if (retry != null) Ui.Pill(acts, "Retry", () => { CloseDialogs(); retry(); }, Ui.BtnStyle.Primary, "refresh");
            Ui.Pill(acts, "Copy details", () => { Ui.Copy(title + "\n" + detail + "\n\n" + Diagnostics()); Toast("Copied"); }, Ui.BtnStyle.Ghost, "copy");
            Ui.Pill(acts, "Close", CloseDialogs, Ui.BtnStyle.Outline);
            m_dlgOpen = true;
            Ui.Show(m_dlgGroup, true);
        }

        public void ShowConfirm(string title, string text, string ok, Action onOk)
        {
            ClearDialog();
            var c = m_dlgCard;
            Ui.Label(c, title, 54, Ui.Bold, Color.white, TextAnchor.UpperLeft, true);
            Ui.Label(c, text, 34, Ui.Regular, Ui.Dim, TextAnchor.UpperLeft, true);
            var acts = Ui.R("Actions", c);
            Ui.HBox(acts, 16, 0, TextAnchor.MiddleLeft);
            Ui.Size(acts.gameObject, -1, 104);
            Ui.Pill(acts, ok, () => { CloseDialogs(); onOk(); }, Ui.BtnStyle.Primary);
            Ui.Pill(acts, "Cancel", CloseDialogs, Ui.BtnStyle.Outline);
            m_dlgOpen = true;
            Ui.Show(m_dlgGroup, true);
        }
    }
}
