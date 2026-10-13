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
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace rt.xr.unity.app
{
    public partial class App
    {
        private RectTransform m_setRoot, m_setContent;
        private CanvasGroup m_setGroup;
        private ScrollRect m_setScroll;
        private InputField m_sourceInput;

        private void BuildSettings()
        {
            m_setRoot = Ui.R("Settings", m_root);
            Ui.Stretch(m_setRoot);
            m_setGroup = Ui.Group(m_setRoot, false);
            var bg = Ui.Box(m_setRoot, Ui.Bg, 0f, "Bg");
            Ui.Bleed((RectTransform)bg.transform);
            bg.raycastTarget = true;
            ScrollRect sr;
            m_setContent = Ui.ScrollV(m_setRoot, out sr, 30, 0, "SettingsScroll");
            m_setScroll = sr;
            var srt = (RectTransform)sr.transform;
            Ui.Stretch(srt);
            srt.offsetMin = new Vector2(0, TabsH);
        }

        private void RefreshSettings()
        {
            float keep = m_setScroll.verticalNormalizedPosition;
            for (int i = m_setContent.childCount - 1; i >= 0; i--) Destroy(m_setContent.GetChild(i).gameObject);
            var c = m_setContent;
            var head = Pad(c, 56, 0);
            Ui.Label(head, "Settings", 60, Ui.Bold, Color.white, TextAnchor.MiddleLeft, false);

            var player = Section2("Player");
            Toggle(player, "Show statistics", "Frame rate, scene size and memory on top of the scene.", "set.stats", false);
            Toggle(player, "Hide the controls by themselves", "They come back when you tap the scene.", "set.autohide", true);
            Toggle(player, "Light scenes that have no light", "Adds a soft light so such scenes are not dark.", "set.filllight", true);
            Toggle(player, "Start in the Walk camera", "Instead of Orbit.", "set.walk", false);
            Toggle(player, "Tap to place AR scenes", "Not part of the specification. On: choose the surface and the spot yourself, resize and turn the scene. Off: the scene is placed where its anchor says (the detected floor or plane).", "set.taptoplace2", true);
            Toggle(player, "Sit the scene on the surface", "Not part of the specification, only with tap to place. On: the scene stands on the spot you tap, ignoring the height its file gives it. Off: the file's own offset is kept.", "set.groundplace", false);
            Toggle(player, "Brighter video", "Not part of the specification. Off by default: the picture is shown as the video has it. On: twice as bright, for a dark video in a lit room.", "set.brightvideo2", false);
            Toggle(player, "Start media only when I press play", "Not part of the specification, which says a media with autoplay true starts by itself. On: videos and sounds wait for the play button.", "set.holdplay", true);
            Toggle(player, "Show media controls in every scene", "The specification asks for controls when a media has controls true. On: they are shown in every scene that has media.", "set.alwayscontrols", true);
            Toggle(player, "Show the log on the screen", "For developers: prints the app's log over the scene.", "set.showlog", false);
            Toggle(player, "Hide the marker picture in marker scenes", "Some marker scenes also draw the marker image as content. On: it is not drawn, so only the real marker and the content anchored to it show.", "set.hidemarkerpicture", true);
            var mw = Ui.Pill(player, MarkerWidthLabel(), null, Ui.BtnStyle.Ghost, null, 32, 96);
            var mwText = mw.GetComponentInChildren<Text>();
            mw.onClick.AddListener(() =>
            {
                float[] steps = { 0f, 10f, 15f, 20f, 25f, 30f, 40f, 60f };
                float cur = PlayerPrefs.GetFloat("set.markerwidthcm", 0f);
                int i = System.Array.FindIndex(steps, x => Mathf.Approximately(x, cur));
                PlayerPrefs.SetFloat("set.markerwidthcm", steps[(i + 1) % steps.Length]); PlayerPrefs.Save();
                if (mwText != null) mwText.text = MarkerWidthLabel();
            });
            Ui.Label(player, "Real width of the marker image you point the camera at, so AR can place content on it. Tap to change. Scene: the size the scene file gives.", 26, Ui.Regular, Ui.Dim, TextAnchor.UpperLeft, true);
            Toggle(player, "Place on any surface", "Not part of the specification. On: floor, table or wall, whatever you tap. Off: only the surface the scene asks for.", "set.anysurface", false);
            Toggle(player, "Anchor to the detected surface", "Off: a free-standing anchor at the spot you tap. On: the anchor follows the surface as it is refined.", "set.planeanchor", false);
            Toggle(player, "Turn placed scenes toward me", "Not part of the specification: the scene keeps the orientation its file gives.", "set.faceuser", false);

            var src = Section2("Sources", "Where the scenes come from.");
            var kv = Ui.R("Facts", src); Ui.VBox(kv, 0, 0);
            Fact(kv, "List file", Catalog.PathsFile);
            Fact(kv, "Scenes", Catalog.Entries.Count.ToString());
            string[] extra = File.Exists(Catalog.SourcesFile) ? File.ReadAllLines(Catalog.SourcesFile).Where(l => !string.IsNullOrWhiteSpace(l)).ToArray() : new string[0];
            foreach (var line in extra)
            {
                var row = Ui.R("Source", src);
                Ui.HBox(row, 16, 0, TextAnchor.MiddleLeft);
                var t = Ui.Label(row, line, 28, Ui.Regular, Color.white, TextAnchor.MiddleLeft, true);
                Ui.Size(t.gameObject, -1, -1, 1);
            }
            var inputBox = Ui.Box(src, Ui.Raised, 36f, "Input");
            Ui.Size(inputBox.gameObject, -1, 100, 1);
            Ui.HBox(inputBox.transform, 12, 0, TextAnchor.MiddleLeft);
            inputBox.GetComponent<HorizontalLayoutGroup>().padding = new RectOffset(30, 30, 0, 0);
            var area = Ui.R("InputArea", inputBox.transform);
            Ui.Size(area.gameObject, -1, -1, 1, 1);
            var input = area.gameObject.AddComponent<InputField>();
            var txt = Ui.Label(area, "", 32, Ui.Regular, Color.white, TextAnchor.MiddleLeft, false, "Text");
            Ui.Stretch((RectTransform)txt.transform); txt.raycastTarget = true;
            var ph = Ui.Label(area, "http://server/scenes/Paths or a .gltf address", 32, Ui.Regular, new Color(1, 1, 1, 0.4f), TextAnchor.MiddleLeft, false, "Placeholder");
            Ui.Stretch((RectTransform)ph.transform);
            input.textComponent = txt; input.placeholder = ph; input.lineType = InputField.LineType.SingleLine;
            input.keyboardType = TouchScreenKeyboardType.URL;
            m_sourceInput = input;
            var add = Ui.Pill(src, "Add this source", () =>
            {
                string v = m_sourceInput.text.Trim();
                if (v.Length == 0) return;
                Catalog.AddSource(v); m_sourceInput.text = "";
                Toast("Source added. Reloading.");
                StartCoroutine(Catalog.Load(m_viewer.ConfigFileLocation));
                RefreshSettings();
            }, Ui.BtnStyle.Primary, "link");
            Ui.Size(add.gameObject, -1, 104, 1);
            var paste = Ui.Pill(src, "Add the address on the clipboard", () =>
            {
                string v = GUIUtility.systemCopyBuffer != null ? GUIUtility.systemCopyBuffer.Trim() : "";
                if (!(v.StartsWith("http://") || v.StartsWith("https://") || v.StartsWith("file://"))) { Toast("The clipboard has no address."); return; }
                Catalog.AddSource(v);
                Toast("Source added. Reloading.");
                StartCoroutine(Catalog.Load(m_viewer.ConfigFileLocation));
                RefreshSettings();
            }, Ui.BtnStyle.Ghost, "copy");
            Ui.Size(paste.gameObject, -1, 104, 1);
            var row2 = Ui.R("SourceButtons", src);
            Ui.HBox(row2, 16, 0, TextAnchor.MiddleLeft);
            Ui.Size(row2.gameObject, -1, 100, 1);
            var reload = Ui.Pill(row2, "Reload scenes", () => { StartCoroutine(Catalog.Load(m_viewer.ConfigFileLocation)); Toast("Reloading"); }, Ui.BtnStyle.Ghost, "refresh");
            Ui.Size(reload.gameObject, -1, 100, 1);
            if (extra.Length > 0)
            {
                var clear = Ui.Pill(row2, "Remove added sources", () => ShowConfirm("Remove added sources?", "The list file stays; only sources added here are removed.", "Remove", () => { Catalog.ClearSources(); StartCoroutine(Catalog.Load(m_viewer.ConfigFileLocation)); RefreshSettings(); }), Ui.BtnStyle.Danger, "trash");
                Ui.Size(clear.gameObject, -1, 100, 1);
            }

            var lab = Section2("Lab");
            var chk = Ui.Pill(lab, "Open every scene and report", () => ShowConfirm("Check every scene?", "Each scene opens in turn as a flat preview, then the app lists what it observed. It takes a minute or two.", "Start", RunAllScenes), Ui.BtnStyle.Primary, "check");
            Ui.Size(chk.gameObject, -1, 104, 1);
            var reset = Ui.Pill(lab, "Clear the passport", () => ShowConfirm("Clear the passport?", "The stamps for the extensions you tried are removed.", "Clear", () => { ClearStamps(); Toast("Passport cleared"); }), Ui.BtnStyle.Ghost, "star");
            Ui.Size(reset.gameObject, -1, 100, 1);

            var about = Section2("About");
            if (m_logo != null)
            {
                var lrow = Ui.R("LogoRow", about);
                Ui.HBox(lrow, 0, 0, TextAnchor.MiddleLeft);
                var lg = Ui.R("Logo", lrow);
                lg.gameObject.AddComponent<RawImage>().texture = m_logo;
                Ui.Size(lg.gameObject, 640, 640f * m_logo.height / m_logo.width);
            }
            Ui.Label(about, ProductName, 44, Ui.Bold, Color.white, TextAnchor.UpperLeft, false);
            Ui.Label(about, "A phone and headset player for MPEG-I Scene Description scenes, a 5G-MAG Reference Tool.", 30, Ui.Regular, Ui.Dim, TextAnchor.UpperLeft, true);
            var ak = Ui.R("Facts", about); Ui.VBox(ak, 0, 0);
            Fact(ak, "Version", Application.version);
            Fact(ak, "Unity", Application.unityVersion);
            Fact(ak, "Device", SystemInfo.deviceModel);
            Fact(ak, "Licence", "5G-MAG Public License v1.0");
            var cp = Ui.Pill(about, "Copy diagnostics", () => { Ui.Copy(Diagnostics()); Toast("Copied to the clipboard"); }, Ui.BtnStyle.Ghost, "copy");
            Ui.Size(cp.gameObject, -1, 100, 1);
            Ui.Size(Ui.Spacer(c, 0, 80).gameObject, -1, 80);
            m_setScroll.verticalNormalizedPosition = Mathf.Clamp01(keep);
        }

        private static string MarkerWidthLabel()
        {
            float cm = PlayerPrefs.GetFloat("set.markerwidthcm", 0f);
            return "Marker width: " + (cm > 0f ? cm.ToString("0") + " cm" : "Scene");
        }

        private RectTransform Section2(string title, string note = null)
        {
            var wrap = Pad(m_setContent, 0, 0);
            var card = Ui.Box(wrap, Ui.Card, 32f, "Section");
            Ui.VBox(card.transform, 16, 40);
            Ui.Label(card.transform, title, 40, Ui.Bold, Color.white, TextAnchor.MiddleLeft, false);
            if (note != null) Ui.Label(card.transform, note, 28, Ui.Regular, Ui.Dim, TextAnchor.UpperLeft, true);
            return (RectTransform)card.transform;
        }

        private void Toggle(Transform parent, string title, string desc, string key, bool def)
        {
            var row = Ui.R("Toggle", parent);
            Ui.HBox(row, 24, 0, TextAnchor.MiddleLeft);
            row.GetComponent<HorizontalLayoutGroup>().padding = new RectOffset(0, 0, 14, 14);
            Ui.Size(row.gameObject, -1, -1, 1);
            var col = Ui.R("Text", row);
            Ui.VBox(col, 2, 0);
            Ui.Size(col.gameObject, -1, -1, 1);
            Ui.Label(col, title, 34, Ui.Medium, Color.white, TextAnchor.UpperLeft, true);
            Ui.Label(col, desc, 27, Ui.Regular, Ui.Dim, TextAnchor.UpperLeft, true);
            bool on = PlayerPrefs.GetInt(key, def ? 1 : 0) == 1;
            var track = Ui.Box(row, on ? Ui.Accent : new Color(1, 1, 1, 0.2f), 40f, "Track");
            Ui.Size(track.gameObject, 112, 64);
            var knob = Ui.Box(track.transform, Color.white, 28f, "Knob");
            var krt = (RectTransform)knob.transform;
            krt.anchorMin = krt.anchorMax = new Vector2(on ? 1f : 0f, 0.5f); krt.pivot = new Vector2(on ? 1f : 0f, 0.5f);
            krt.anchoredPosition = new Vector2(on ? -6 : 6, 0); krt.sizeDelta = new Vector2(52, 52);
            knob.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            Ui.Click(track.gameObject, track, () =>
            {
                bool now = PlayerPrefs.GetInt(key, def ? 1 : 0) == 1;
                PlayerPrefs.SetInt(key, now ? 0 : 1); PlayerPrefs.Save();
                RefreshSettings();
            });
        }
    }
}
