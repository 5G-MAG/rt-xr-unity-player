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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

namespace rt.xr.unity.app
{
    // Lab tools inside the player: the extension lens, a switch per extension family, a monitor for interactivity, the ranges
    // of proximity and collision triggers, and the media pipeline.
    public partial class App
    {
        private class LensItem
        {
            public GameObject Go; public Color Color; public string Label; public Renderer[] Rends;
            public RectTransform Rt; public float Until = -1f; public Text Txt;
        }

        private readonly List<LensItem> m_lens = new List<LensItem>();
        private RectTransform m_lensRoot;
        private bool m_lensOn;
        private RectTransform m_toolsRoot, m_toolsPanel, m_toolsContent, m_nodeCard, m_eventsRoot, m_pipeRoot, m_legend;
        private CanvasGroup m_toolsGroup;
        private ScrollRect m_toolsScroll;
        private bool m_toolsOpen, m_eventsOn, m_pipeOn, m_volumesOn;
        private Text m_eventsText;
        private readonly Queue<string> m_eventQueue = new Queue<string>();
        private readonly List<string> m_eventLines = new List<string>();
        private readonly HashSet<string> m_off = new HashSet<string>();
        private readonly Dictionary<Renderer, Material[]> m_origMats = new Dictionary<Renderer, Material[]>();
        private GameObject m_vizRoot;
        private readonly List<Text> m_pipeTexts = new List<Text>();
        private readonly List<Image> m_pipeFills = new List<Image>();
        private readonly List<MediaPlayer> m_pipePlayers = new List<MediaPlayer>();
        private float m_pipeNext;

        // ------------------------------------------------------------------ which parts carry what

        private List<Family> FamiliesOfGameObject(GameObject go)
        {
            var list = new List<Family>();
            Action<string> add = id => { var f = FamilyById(id); if (f != null && !list.Contains(f)) list.Add(f); };
            foreach (var comp in go.GetComponents<Component>())
            {
                if (comp == null) continue;
                string n = comp.GetType().Name;
                if (comp is Light) add("lighting");
                else if (comp is AudioSource || n.Contains("SpatialAudio")) add("audio");
                else if (n.Contains("Anchor")) add("anchors");
                else if (n.Contains("Behavior") || n.EndsWith("Trigger")) add("interactivity");
                else if (n.Contains("Haptic")) add("haptics");
            }
            return list;
        }

        private static string Nice(string typeName)
        {
            string s = typeName.Replace("Trigger", "").Replace("Action", "");
            s = Regex.Replace(s, "([a-z])([A-Z])", "$1 $2");
            return s.Trim();
        }

        private string DescribeBehaviour(GLTFast.Behavior b)
        {
            Func<string, string> names = field =>
            {
                try
                {
                    var f = typeof(GLTFast.Behavior).GetField(field, BindingFlags.NonPublic | BindingFlags.Instance);
                    var arr = f != null ? f.GetValue(b) as Array : null;
                    if (arr == null) return "?";
                    return string.Join(", ", arr.Cast<object>().Where(o => o != null).Select(o => Nice(o.GetType().Name)));
                }
                catch { return "?"; }
            };
            return "When: " + names("m_Trigger") + ". Then: " + names("m_Action") + ".";
        }

        // ------------------------------------------------------------------ player hooks

        // Test aid: the state of the scene's nodes a few seconds after it opened, one line per node, so runs can be compared without a screenshot.
        private IEnumerator LogSceneState(SceneEntry e)
        {
            float waited = 0f;
            foreach (float at in new[] { 3f, 5f, 8f, 11f, 20f, 30f, 45f, 60f, 90f })
            {
                yield return new WaitForSecondsRealtime(at - waited);
                waited = at;
                var sb = new System.Text.StringBuilder("XRTEST state t=" + at + " " + e.FileName);
                var cam = m_viewer != null ? m_viewer.GetMainCamera() : null;
                if (cam != null) sb.Append(" cam=" + cam.transform.position.ToString("0.0"));
                foreach (var r in FindObjectsOfType<Renderer>())
                {
                    if (r == null || !GLTFast.VirtualSceneGraph.IsNodeGameObject(r.gameObject)) continue;
                    sb.Append(" | " + r.gameObject.name + "<" + r.GetType().Name + ">" + (r.enabled && r.gameObject.activeInHierarchy ? (r.forceRenderingOff ? " gated " : " on ") : " off ") + r.transform.position.ToString("0.00") + " rot=" + r.transform.eulerAngles.ToString("0") + " b=" + r.bounds.center.ToString("0.00") + " size=" + r.bounds.size.ToString("0.0") + " vis=" + r.isVisible + " layer=" + r.gameObject.layer + " mat=" + (r.sharedMaterial != null ? r.sharedMaterial.shader.name : "none"));
                }
                foreach (var mp in FindObjectsOfType<rt.xr.unity.MediaPlayer>())
                    sb.Append(" | media " + mp.name + " pipeline=" + mp.HasPipeline + " started=" + mp.Started + " t=" + mp.CurrentTime.ToString("0.0") + " err=" + mp.Error + " auto=" + mp.autoPlay + " fin=" + mp.Finished + " pipe=" + mp.DebugState);
                foreach (var a in FindObjectsOfType<AudioSource>())
                    sb.Append(" | audio " + a.gameObject.name + " playing=" + a.isPlaying + " vol=" + a.volume.ToString("0.00") + " 3d=" + a.spatialBlend.ToString("0.0") + (a.GetComponent<GLTFast.SpatialAudioSource>() != null ? " gain=" + a.GetComponent<GLTFast.SpatialAudioSource>().CurrentDistanceGain.ToString("0.000") + " dist=" + (cam != null ? Vector3.Distance(cam.transform.position, a.transform.position).ToString("0.00") : "?") : ""));
                foreach (var tg in FindObjectsOfType<rt.xr.unity.TimedObjectGate>()) sb.Append(" | timedObjects=" + tg.Count + " waiting=" + tg.Waiting);
                foreach (var ru in FindObjectsOfType<GLTFast.MpegReverbUnit>()) sb.Append(" | reverb " + ru.Id + " peak=" + ru.Peak.ToString("0.0000"));
                Debug.Log(sb.ToString());
            }
        }

        private void OnPlayerEntered(SceneEntry e)
        {
            m_off.Clear(); m_origMats.Clear();
            m_eventLines.Clear(); m_eventQueue.Clear();
            m_eventsOn = false; m_pipeOn = false; m_volumesOn = false;
            if (m_eventsRoot != null) m_eventsRoot.gameObject.SetActive(false);
            if (m_pipeRoot != null) m_pipeRoot.gameObject.SetActive(false);
            foreach (var b in FindObjectsOfType<GLTFast.Behavior>())
            {
                var bb = b;
                try { bb.AddGameEngineAction(() => { Debug.Log("XRTEST fired t=" + Time.timeSinceLevelLoad.ToString("0.0") + " " + DescribeBehaviour(bb)); m_eventQueue.Enqueue(Time.unscaledTime.ToString("0.0") + "|" + bb.gameObject.name + "|" + DescribeBehaviour(bb)); }); } catch { }
            }
            StartCoroutine(LogSceneState(e));
            if (m_newStamp != null)
            {
                Toast("New stamp: " + m_newStamp.Name, 3.2f);
                m_newStamp = null;
            }
        }

        private void OnPlayerLeft()
        {
            LensOff();
            CloseTools();
            ClearVolumes();
            if (m_eventsRoot != null) m_eventsRoot.gameObject.SetActive(false);
            if (m_pipeRoot != null) m_pipeRoot.gameObject.SetActive(false);
            m_off.Clear(); m_origMats.Clear();
        }

        // ------------------------------------------------------------------ lens

        private void EnsureLensRoot()
        {
            if (m_lensRoot != null) return;
            m_lensRoot = Ui.R("Lens", m_playerRoot);
            Ui.Stretch(m_lensRoot);
            m_lensRoot.SetSiblingIndex(0);
            m_legend = Ui.R("Legend", m_playerRoot);
            Ui.Place(m_legend, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -250), new Vector2(-64, 0));
            var fl = Ui.Flow(m_legend, 10, 10);
            Destroy(fl);
        }

        private void ToggleLens()
        {
            if (rt.xr.unity.QuestRig.Active) { Toast("The lens is for the phone screen. In the headset use the scene tree and the tools.", 3f); return; }
            if (m_lensOn) { LensOff(); Toast("Lens off", 1.2f); return; }
            LensBuild();
            if (m_lens.Count == 0) { Toast("Nothing in this scene carries extension data the player attached.", 3f); return; }
            m_lensOn = true;
            if (m_lensBg != null) m_lensBg.color = Ui.Accent;
            Toast("Lens on. Tap an outlined part for details.", 3f);
        }

        private void LensBuild()
        {
            EnsureLensRoot();
            ClearLens();
            var present = new List<Family>();
            foreach (var t in m_viewer.GetComponentsInChildren<Transform>(true))
            {
                var fams = FamiliesOfGameObject(t.gameObject);
                if (fams.Count == 0) continue;
                AddLensItem(t.gameObject, fams[0].Color, string.Join(" · ", fams.Select(f => f.Name)), -1f);
                foreach (var f in fams) if (!present.Contains(f)) present.Add(f);
            }
            for (int i = m_legend.childCount - 1; i >= 0; i--) Destroy(m_legend.GetChild(i).gameObject);
            var flow = m_legend.gameObject.AddComponent<FlowLayout>(); flow.spacingX = 10; flow.spacingY = 10;
            Ui.Fit(m_legend.gameObject);
            foreach (var f in present) Ui.Chip(m_legend, f.Name, f.Color, true, 24);
            m_legend.gameObject.SetActive(true);
        }

        private LensItem AddLensItem(GameObject go, Color color, string label, float seconds)
        {
            var item = new LensItem { Go = go, Color = color, Label = label, Rends = go.GetComponentsInChildren<Renderer>() };
            var box = Ui.R("Box", m_lensRoot);
            box.anchorMin = box.anchorMax = new Vector2(0.5f, 0.5f);
            var img = box.gameObject.AddComponent<Image>();
            img.sprite = Ui.Outline; img.type = Image.Type.Sliced; img.color = color; img.raycastTarget = false;
            img.pixelsPerUnitMultiplier = 1.6f;
            var tag = Ui.Box(box, color, 14f, "Tag");
            var trt = (RectTransform)tag.transform;
            Ui.Place(trt, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 0), new Vector2(0, 6), new Vector2(0, 0));
            Ui.HBox(tag.transform, 0, 0, TextAnchor.MiddleCenter).padding = new RectOffset(14, 14, 4, 4);
            Ui.Fit(tag.gameObject, true, true);
            item.Txt = Ui.Label(tag.transform, label, 24, Ui.Medium, Ui.OnAccent, TextAnchor.MiddleCenter, false);
            item.Rt = box;
            item.Until = seconds > 0 ? Time.unscaledTime + seconds : -1f;
            m_lens.Add(item);
            return item;
        }

        private void ClearLens()
        {
            foreach (var i in m_lens) if (i.Rt != null) Destroy(i.Rt.gameObject);
            m_lens.Clear();
        }

        private void LensOff()
        {
            ClearLens();
            m_lensOn = false;
            if (m_lensBg != null) m_lensBg.color = Ui.Glass;
            if (m_legend != null) m_legend.gameObject.SetActive(false);
        }

        private bool ScreenRectOf(LensItem item, out Rect rect)
        {
            rect = new Rect();
            var cam = m_viewer.GetMainCamera();
            if (cam == null || item.Go == null) return false;
            Bounds b;
            bool have = false;
            b = new Bounds(item.Go.transform.position, Vector3.one * 0.05f);
            foreach (var r in item.Rends) { if (r == null || !r.enabled) continue; if (!have) { b = r.bounds; have = true; } else b.Encapsulate(r.bounds); }
            if (!have)
            {
                float size = Mathf.Max(0.1f, rt.xr.Utils.ComputeBounds(m_viewer.gameObject).size.magnitude * 0.04f);
                b = new Bounds(item.Go.transform.position, Vector3.one * size);
            }
            Vector2 min = new Vector2(float.MaxValue, float.MaxValue), max = new Vector2(float.MinValue, float.MinValue);
            bool anyFront = false;
            for (int i = 0; i < 8; i++)
            {
                Vector3 corner = b.center + new Vector3(((i & 1) == 0 ? -1 : 1) * b.extents.x, ((i & 2) == 0 ? -1 : 1) * b.extents.y, ((i & 4) == 0 ? -1 : 1) * b.extents.z);
                Vector3 sp = cam.WorldToScreenPoint(corner);
                if (sp.z > 0.01f) anyFront = true;
                min = Vector2.Min(min, sp); max = Vector2.Max(max, sp);
            }
            if (!anyFront) return false;
            rect = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
            return true;
        }

        private void UpdateLensItems()
        {
            if (m_lens.Count == 0 || m_lensRoot == null) return;
            for (int i = m_lens.Count - 1; i >= 0; i--)
            {
                var it = m_lens[i];
                if (it.Go == null || (it.Until > 0 && Time.unscaledTime > it.Until)) { if (it.Rt != null) Destroy(it.Rt.gameObject); m_lens.RemoveAt(i); continue; }
                Rect sr;
                if (!ScreenRectOf(it, out sr)) { it.Rt.gameObject.SetActive(false); continue; }
                it.Rt.gameObject.SetActive(true);
                Vector2 a, b;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(m_lensRoot, new Vector2(sr.xMin, sr.yMin), null, out a);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(m_lensRoot, new Vector2(sr.xMax, sr.yMax), null, out b);
                Vector2 size = new Vector2(Mathf.Max(70f, Mathf.Abs(b.x - a.x) + 16f), Mathf.Max(70f, Mathf.Abs(b.y - a.y) + 16f));
                it.Rt.anchoredPosition = (a + b) * 0.5f;
                it.Rt.sizeDelta = size;
            }
        }

        // a tap while the lens is on: pick the outlined part under the finger
        private bool OnSceneTap(Vector2 screenPos)
        {
            if (!m_lensOn) return false;
            LensItem best = null; float bestArea = float.MaxValue;
            foreach (var it in m_lens)
            {
                Rect r;
                if (!ScreenRectOf(it, out r)) continue;
                r.xMin -= 20; r.yMin -= 20; r.xMax += 20; r.yMax += 20;
                if (r.Contains(screenPos) && r.width * r.height < bestArea) { best = it; bestArea = r.width * r.height; }
            }
            if (best == null) return false;
            ShowNodeCard(best.Go);
            return true;
        }

        private void ShowNodeCard(GameObject go)
        {
            ClearDialog();
            var c = m_dlgCard;
            var fams = FamiliesOfGameObject(go);
            Ui.Label(c, go.name, 50, Ui.Bold, Color.white, TextAnchor.UpperLeft, true);
            var flow = Ui.Flow(c, 10, 10);
            foreach (var f in fams) Ui.Chip(flow.transform, f.Name, f.Color, false, 26);
            var comps = go.GetComponents<Component>().Where(x => x != null && !(x is Transform)).Select(x => x.GetType().Name).Distinct().ToList();
            var box = Ui.Box(c, new Color32(0x14, 0x14, 0x14, 0xFF), 24f, "Parts");
            Ui.VBox(box.transform, 4, 28);
            Ui.Label(box.transform, "Parts the loader attached", 26, Ui.Medium, Ui.Dim, TextAnchor.UpperLeft, false);
            Ui.Label(box.transform, comps.Count == 0 ? "None besides the node itself." : string.Join("\n", comps), 28, Ui.Regular, Color.white, TextAnchor.UpperLeft, true);
            var acts = Ui.R("Actions", c);
            Ui.HBox(acts, 16, 0, TextAnchor.MiddleLeft);
            Ui.Size(acts.gameObject, -1, 104);
            Ui.Pill(acts, "Focus", () => { CloseDialogs(); FocusNode(go); }, Ui.BtnStyle.Primary, "target");
            Ui.Pill(acts, go.activeSelf ? "Hide it" : "Show it", () => { go.SetActive(!go.activeSelf); CloseDialogs(); }, Ui.BtnStyle.Ghost, "eye");
            Ui.Pill(acts, "Close", CloseDialogs, Ui.BtnStyle.Outline);
            m_dlgOpen = true;
            Ui.Show(m_dlgGroup, true);
        }

        private void FocusNode(GameObject go)
        {
            var rends = go.GetComponentsInChildren<Renderer>();
            Bounds b = new Bounds(go.transform.position, Vector3.one * 0.3f);
            bool have = false;
            foreach (var r in rends) { if (!have) { b = r.bounds; have = true; } else b.Encapsulate(r.bounds); }
            if (m_rig != null) m_rig.FocusOn(b);
        }

        // a node picked in the scene tree: find it and outline it for a few seconds
        private void HighlightNode(GameObject go)
        {
            EnsureLensRoot();
            FocusNode(go);
            AddLensItem(go, Ui.Accent, go.name, 4f);
            m_hudVisible = true; m_hudLast = Time.unscaledTime;
        }

        // ------------------------------------------------------------------ tools panel

        private void BuildTools()
        {
            m_toolsRoot = Ui.R("Tools", m_playerRoot);
            Ui.Stretch(m_toolsRoot);
            m_toolsGroup = Ui.Group(m_toolsRoot, false);
            var scrim = Ui.Box(m_toolsRoot, new Color(0, 0, 0, 0.45f), 0f, "Scrim");
            Ui.Bleed((RectTransform)scrim.transform);
            Ui.Click(scrim.gameObject, scrim, CloseTools);
            var panel = Ui.Box(m_toolsRoot, new Color32(0x18, 0x18, 0x18, 0xFF), 44f, "Panel");
            m_toolsPanel = (RectTransform)panel.transform;
            panel.raycastTarget = true;
            panel.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            ScrollRect sr;
            m_toolsContent = Ui.ScrollV(m_toolsPanel, out sr, 20, 0, "ToolsScroll");
            m_toolsScroll = sr;
            Ui.Stretch((RectTransform)sr.transform);
        }

        private void ToggleTools() { if (m_toolsOpen) CloseTools(); else OpenTools(); }

        private void OpenTools()
        {
            if (m_toolsRoot == null) BuildTools();
            float w = m_root.rect.width, h = m_root.rect.height;
            float pw = Mathf.Min(w, 1280f);
            float ph = Mathf.Min(h * (Landscape ? 0.96f : 0.78f), 1500f);
            Ui.Place(m_toolsPanel, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, Landscape ? 10 : -44), new Vector2(pw, ph + (Landscape ? 0 : 44)));
            RebuildTools();
            m_toolsOpen = true;
            Ui.Show(m_toolsGroup, true);
            StartCoroutine(FitToolsNextFrame(pw, ph));
        }

        private IEnumerator FitToolsNextFrame(float pw, float max)
        {
            yield return null;
            LayoutRebuilder.ForceRebuildLayoutImmediate(m_toolsContent);
            float need = LayoutUtility.GetPreferredHeight(m_toolsContent);
            float h = Mathf.Min(max, need + 30f);
            Ui.Place(m_toolsPanel, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, Landscape ? 10 : -44), new Vector2(pw, h + (Landscape ? 0 : 44)));
        }

        private void CloseTools()
        {
            m_toolsOpen = false;
            if (m_toolsGroup != null) Ui.Show(m_toolsGroup, false);
        }

        private bool ScenePresent(string familyId)
        {
            var f = FamilyById(familyId);
            if (m_current != null && m_current.Extensions.Any(f.Match)) return true;
            return false;
        }

        private void RebuildTools()
        {
            if (m_toolsOpen) StartCoroutine(FitToolsNextFrame(m_toolsPanel.rect.width, Mathf.Min(m_root.rect.height * (Landscape ? 0.96f : 0.78f), 1500f)));
            for (int i = m_toolsContent.childCount - 1; i >= 0; i--) Destroy(m_toolsContent.GetChild(i).gameObject);
            var c = m_toolsContent;
            var pad = Pad(c, 36, 60);
            Ui.VBox(pad, 20, 0);
            pad.GetComponent<VerticalLayoutGroup>().padding = new RectOffset(Margin, Margin, 36, 60);
            var grab = Ui.R("Grab", pad);
            Ui.Size(grab.gameObject, -1, 24);
            var g = Ui.Box(grab, new Color(1, 1, 1, 0.3f), 6f, "G");
            Ui.Place((RectTransform)g.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(110, 10));
            Ui.Label(pad, "Lab tools", 54, Ui.Bold, Color.white, TextAnchor.MiddleLeft, false);
            if (m_current != null) Ui.Label(pad, m_current.Title, 30, Ui.Regular, Ui.Dim, TextAnchor.MiddleLeft, false);

            // see
            ToolsHead(pad, "See");
            ToolRow(pad, "search", "Extension lens", m_lensOn ? "On" : "Off", m_lensOn, Ui.Accent, () => { CloseTools(); ToggleLens(); });
            bool hasInter = m_current != null && m_current.UsesInteractivity;
            ToolRow(pad, "bolt", "Interactivity monitor", hasInter ? (m_eventsOn ? "On" : "Off") : "No interactivity", m_eventsOn, Ui.Warn, hasInter ? (Action)(() => { m_eventsOn = !m_eventsOn; EnsureEvents(); RebuildTools(); }) : null);
            ToolRow(pad, "target", "Ranges and positions", m_volumesOn ? "On" : "Off", m_volumesOn, Ui.Warn, () => { ToggleVolumes(); RebuildTools(); });
            bool hasMedia = FindObjectsOfType<MediaPlayer>().Length > 0;
            ToolRow(pad, "wave", "Media pipeline", hasMedia ? (m_pipeOn ? "On" : "Off") : "No media", m_pipeOn, Ui.Accent, hasMedia ? (Action)(() => { m_pipeOn = !m_pipeOn; EnsurePipe(); RebuildTools(); }) : null);
            ToolRow(pad, "tree", "Scene tree", "Parts the loader created", false, Ui.Dim, () => { CloseTools(); ShowDetails(m_current, true); });

            // switch off
            ToolsHead(pad, "Switch off, to see what it adds");
            bool any = false;
            foreach (var f in Families)
            {
                bool present = ScenePresent(f.Id);
                if (!present) continue;
                any = true;
                bool off = m_off.Contains(f.Id);
                bool effect = f.Id == "lighting" || f.Id == "audio" || f.Id == "interactivity" || f.Id == "materials" || f.Id == "video" || f.Id == "anchors";
                string state = !effect ? "No visible effect" : (off ? "Switched off" : "On");
                string fid = f.Id;
                ToolRow(pad, f.Icon, f.Name, state, effect && off, f.Color, effect ? (Action)(() => { SwitchFamily(fid, !m_off.Contains(fid)); RebuildTools(); }) : null);
            }
            if (!any) Ui.Label(pad, "This scene lists no extensions to switch.", 28, Ui.Regular, Ui.Dim, TextAnchor.UpperLeft, true);

            // notes
            if (m_current != null && m_current.Names.Count > 0)
            {
                ToolsHead(pad, "Named in the content's notes");
                var flow = Ui.Flow(pad, 10, 10);
                foreach (var n in m_current.Names) Ui.Chip(flow.transform, Humanize(n), n.StartsWith("TRIGGER") ? Ui.Warn : (n.StartsWith("ACTION") ? Ui.Accent : Ui.Good), false, 26);
                if (m_current.Names.Contains("TRIGGER_USER_INPUT")) Ui.Label(pad, "The player's user input trigger always reports true, so a scene using it acts as if the input is already given.", 26, Ui.Regular, Ui.Dim, TextAnchor.UpperLeft, true);
            }
        }

        private void ToolsHead(Transform p, string t)
        {
            var l = Ui.Label(p, t, 34, Ui.Bold, Ui.Accent, TextAnchor.LowerLeft, false);
            Ui.Size(l.gameObject, -1, 70);
        }

        private void ToolRow(Transform p, string icon, string title, string state, bool on, Color color, Action onClick)
        {
            var row = Ui.Box(p, on ? new Color(color.r, color.g, color.b, 0.16f) : new Color(1, 1, 1, 0.05f), 28f, "Tool");
            Ui.HBox(row.transform, 22, 0, TextAnchor.MiddleLeft);
            row.GetComponent<HorizontalLayoutGroup>().padding = new RectOffset(26, 26, 20, 20);
            Ui.Size(row.gameObject, -1, -1, 1);
            Ui.IconImage(row.transform, icon, onClick == null ? Ui.Dim : (on ? color : Color.white), 52);
            var col = Ui.R("Text", row.transform);
            Ui.VBox(col, 0, 0, TextAnchor.MiddleLeft);
            Ui.Size(col.gameObject, -1, -1, 1);
            Ui.Label(col, title, 34, Ui.Medium, onClick == null ? Ui.Dim : Color.white, TextAnchor.MiddleLeft, false);
            Ui.Label(col, state, 27, Ui.Regular, on ? color : Ui.Dim, TextAnchor.MiddleLeft, false);
            if (onClick != null) Ui.Click(row.gameObject, row, onClick);
        }

        // ------------------------------------------------------------------ switching families off

        private void SwitchFamily(string id, bool off)
        {
            if (off) m_off.Add(id); else m_off.Remove(id);
            switch (id)
            {
                case "lighting":
                    foreach (var l in FindObjectsOfType<Light>()) if (l.gameObject.name != "FillLight") l.enabled = !off;
                    if (off && !m_filled) EnsureFillForLab();
                    break;
                case "audio":
                    foreach (var a in m_viewer.GetComponentsInChildren<AudioSource>(true)) a.spatialBlend = off ? 0f : 1f;
                    break;
                case "interactivity":
                    foreach (var bc in FindObjectsOfType<GLTFast.BehaviorController>()) bc.enabled = !off;
                    break;
                case "materials":
                    SwapMaterials(off);
                    break;
                case "video":
                    foreach (var mp in FindObjectsOfType<MediaPlayer>()) { try { if (off) mp.Stop(); else mp.Play(); } catch { } }
                    break;
                case "anchors":
                    var e = m_current;
                    if (e != null) { CloseTools(); LeavePlayer(); OpenScene(e, off); }
                    break;
            }
        }

        private void EnsureFillForLab()
        {
            // with the scene's lights off the scene would go black; keep the soft light so the difference is the lights, not the view
            if (GameObject.Find("FillLight") != null) return;
            var go = new GameObject("FillLight");
            go.transform.rotation = Quaternion.Euler(48f, -35f, 0f);
            var light = go.AddComponent<Light>();
            light.type = LightType.Directional; light.intensity = 0.5f; light.shadows = LightShadows.None;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.35f, 0.35f, 0.37f);
        }

        private void SwapMaterials(bool off)
        {
            if (off)
            {
                var plain = new Material(Shader.Find("Standard") ?? Shader.Find("Diffuse") ?? Shader.Find("Sprites/Default"));
                plain.color = new Color(0.8f, 0.8f, 0.82f);
                foreach (var r in m_viewer.GetComponentsInChildren<Renderer>(true))
                {
                    if (!m_origMats.ContainsKey(r)) m_origMats[r] = r.sharedMaterials;
                    var arr = new Material[r.sharedMaterials.Length];
                    for (int i = 0; i < arr.Length; i++) arr[i] = plain;
                    r.sharedMaterials = arr;
                }
            }
            else
            {
                foreach (var kv in m_origMats) if (kv.Key != null) kv.Key.sharedMaterials = kv.Value;
                m_origMats.Clear();
            }
        }

        // ------------------------------------------------------------------ interactivity monitor

        private void EnsureEvents()
        {
            if (m_eventsRoot == null)
            {
                m_eventsRoot = Ui.R("Events", m_playerRoot);
                Ui.Place(m_eventsRoot, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(32, -560), new Vector2(640, 0));
                var bg = Ui.Box(m_eventsRoot, new Color(0, 0, 0, 0.66f), 26f, "Bg");
                Ui.Stretch((RectTransform)bg.transform);
                bg.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
                Ui.VBox(m_eventsRoot, 6, 24);
                Ui.Fit(m_eventsRoot.gameObject);
                var title = Ui.Label(m_eventsRoot, "Interactivity", 28, Ui.Bold, Ui.Warn, TextAnchor.MiddleLeft, false);
                m_eventsText = Ui.Label(m_eventsRoot, "Waiting for a trigger.", 26, Ui.Regular, Color.white, TextAnchor.UpperLeft, true);
            }
            m_eventsRoot.gameObject.SetActive(m_eventsOn);
        }

        private void DrainEvents()
        {
            bool changed = false;
            while (m_eventQueue.Count > 0)
            {
                var parts = m_eventQueue.Dequeue().Split('|');
                m_eventLines.Add("<color=#B3B3B3>" + parts[0] + "s</color> " + parts[2]);
                if (m_eventLines.Count > 6) m_eventLines.RemoveAt(0);
                changed = true;
                Toast("Fired: " + parts[2], 1.6f);
            }
            if (changed && m_eventsText != null) m_eventsText.text = string.Join("\n", m_eventLines);
        }

        // ------------------------------------------------------------------ trigger ranges

        private void ToggleVolumes()
        {
            if (m_volumesOn) { ClearVolumes(); m_volumesOn = false; return; }
            m_vizRoot = new GameObject("LabViz");
            m_volumesOn = true;
            int made = 0;
            foreach (var p in FindObjectsOfType<GLTFast.ProximityNodeTrigger>())
            {
                float lo = ReadFloat(p, "m_LowerDistance"), hi = ReadFloat(p, "m_UpperDistance");
                if (hi > 0.0001f && hi < 1000f) { Rings(p.transform.position, hi, Ui.Warn); made++; }
                if (lo > 0.0001f && lo < hi) { Rings(p.transform.position, lo, Ui.Warn); }
            }
            foreach (var t in FindObjectsOfType<GLTFast.CollisionNodeTrigger>())
            {
                var col = t.GetComponent<Collider>();
                if (col != null) { WireBox(col.bounds, Ui.Bad); made++; }
            }
            foreach (var v in FindObjectsOfType<GLTFast.VisibilityNodeTrigger>())
            {
                var r = v.GetComponent<Renderer>();
                if (r != null) { WireBox(r.bounds, Ui.Accent); made++; }
            }
            foreach (var u in FindObjectsOfType<GLTFast.UserInputNodeTrigger>())
            {
                var r = u.GetComponentInChildren<Renderer>();
                if (r != null) { WireBox(r.bounds, Ui.Good); made++; }
            }
            foreach (var a in m_viewer.GetComponentsInChildren<AudioSource>(true))
            {
                Rings(a.transform.position, Mathf.Clamp(a.minDistance, 0.05f, 30f), Ui.Audio); made++;
            }
            foreach (var l in FindObjectsOfType<Light>())
            {
                if (l.gameObject.name == "FillLight") continue;
                if (l.type == LightType.Directional) { var lr = NewLine(Ui.LightC, 2, VizWidth()); var o = rt.xr.Utils.ComputeBounds(m_viewer.gameObject).center; lr.SetPosition(0, o - l.transform.forward * 1.5f); lr.SetPosition(1, o); }
                else Rings(l.transform.position, Mathf.Clamp(l.range, 0.05f, 30f), Ui.LightC);
                made++;
            }
            if (made == 0) { Toast("This scene has nothing to show here."); ClearVolumes(); m_volumesOn = false; }
            else Toast("Rings: proximity range (amber), audio distance (purple), light range (yellow). Boxes: collision (red), visibility (blue), user input (green).", 5f);
        }

        private void ClearVolumes() { if (m_vizRoot != null) Destroy(m_vizRoot); m_vizRoot = null; m_volumesOn = false; }

        private static float ReadFloat(object o, string field)
        {
            try { var f = o.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance); return f != null ? (float)f.GetValue(o) : 0f; } catch { return 0f; }
        }

        private LineRenderer NewLine(Color c, int points, float width)
        {
            var go = new GameObject("Line");
            go.transform.SetParent(m_vizRoot.transform, false);
            var lr = go.AddComponent<LineRenderer>();
            lr.material = Ui.LineMaterial();
            lr.startColor = lr.endColor = c;
            lr.widthMultiplier = width;
            lr.positionCount = points;
            lr.useWorldSpace = true;
            lr.numCapVertices = 2;
            return lr;
        }

        private float VizWidth() { return Mathf.Max(0.004f, rt.xr.Utils.ComputeBounds(m_viewer.gameObject).size.magnitude * 0.0025f); }

        private void Rings(Vector3 center, float radius, Color color)
        {
            for (int axis = 0; axis < 3; axis++)
            {
                var lr = NewLine(color, 65, VizWidth());
                for (int i = 0; i <= 64; i++)
                {
                    float a = i / 64f * Mathf.PI * 2f;
                    Vector3 p = axis == 0 ? new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0) : (axis == 1 ? new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) : new Vector3(0, Mathf.Cos(a), Mathf.Sin(a)));
                    lr.SetPosition(i, center + p * radius);
                }
            }
        }

        private void WireBox(Bounds b, Color color)
        {
            Vector3 c = b.center, e = b.extents;
            Vector3[] v = new Vector3[8];
            for (int i = 0; i < 8; i++) v[i] = c + new Vector3(((i & 1) == 0 ? -1 : 1) * e.x, ((i & 2) == 0 ? -1 : 1) * e.y, ((i & 4) == 0 ? -1 : 1) * e.z);
            int[,] edges = { { 0, 1 }, { 2, 3 }, { 4, 5 }, { 6, 7 }, { 0, 2 }, { 1, 3 }, { 4, 6 }, { 5, 7 }, { 0, 4 }, { 1, 5 }, { 2, 6 }, { 3, 7 } };
            for (int i = 0; i < 12; i++)
            {
                var lr = NewLine(color, 2, VizWidth());
                lr.SetPosition(0, v[edges[i, 0]]); lr.SetPosition(1, v[edges[i, 1]]);
            }
        }

        // ------------------------------------------------------------------ media pipeline

        private void EnsurePipe()
        {
            if (m_pipeRoot != null) { Destroy(m_pipeRoot.gameObject); m_pipeRoot = null; }
            m_pipeTexts.Clear(); m_pipeFills.Clear(); m_pipePlayers.Clear();
            if (!m_pipeOn) return;
            m_pipeRoot = Ui.R("Pipeline", m_playerRoot);
            Ui.Place(m_pipeRoot, new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-32, -260), new Vector2(620, 0));
            var bg = Ui.Box(m_pipeRoot, new Color(0, 0, 0, 0.68f), 26f, "Bg");
            Ui.Stretch((RectTransform)bg.transform);
            bg.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            Ui.VBox(m_pipeRoot, 8, 24);
            Ui.Fit(m_pipeRoot.gameObject);
            Ui.Label(m_pipeRoot, "Media pipeline", 28, Ui.Bold, Ui.Accent, TextAnchor.MiddleLeft, false);
            var medias = SafeMedias();
            int idx = 0;
            foreach (var mp in FindObjectsOfType<MediaPlayer>())
            {
                string name = (m_current != null && idx < m_current.Media.Count) ? Uri.UnescapeDataString(m_current.Media[idx].Uri.Substring(m_current.Media[idx].Uri.LastIndexOf('/') + 1)) : "Media " + (idx + 1);
                idx++;
                Ui.Label(m_pipeRoot, name, 26, Ui.Medium, Color.white, TextAnchor.MiddleLeft, false);
                var line = Ui.Label(m_pipeRoot, "", 24, Ui.Regular, Ui.Dim, TextAnchor.UpperLeft, true);
                var bar = Ui.Box(m_pipeRoot, new Color(1, 1, 1, 0.14f), 10f, "Bar");
                Ui.Size(bar.gameObject, -1, 18, 1);
                var fill = Ui.Box(bar.transform, Ui.Accent, 10f, "Fill");
                var frt = (RectTransform)fill.transform;
                frt.anchorMin = new Vector2(0, 0); frt.anchorMax = new Vector2(0, 1); frt.offsetMin = Vector2.zero; frt.offsetMax = Vector2.zero;
                m_pipeTexts.Add(line); m_pipeFills.Add(fill); m_pipePlayers.Add(mp);
            }
        }

        private void UpdatePipe()
        {
            if (!m_pipeOn || m_pipeRoot == null || Time.unscaledTime < m_pipeNext) return;
            m_pipeNext = Time.unscaledTime + 0.25f;
            for (int i = 0; i < m_pipePlayers.Count; i++)
            {
                var mp = m_pipePlayers[i];
                if (mp == null) continue;
                var sb = new System.Text.StringBuilder();
                sb.Append("Pipeline " + mp.PipelineStateName.ToLowerInvariant());
                float fill = 0f; int cnt = 0, cap = 0;
                foreach (var b in mp.Buffers) { try { cnt += b.Count; cap += b.Capacity; } catch { } }
                if (cap > 0) fill = cnt / (float)cap;
                sb.Append(" · buffer " + cnt + " of " + cap + " frames");
                sb.Append("\n" + Ui.Plural(mp.VideoTextureCount, "video texture", "video textures") + ", " + Ui.Plural(mp.AudioReaderCount, "audio source", "audio sources"));
                m_pipeTexts[i].text = sb.ToString();
                var frt = (RectTransform)m_pipeFills[i].transform;
                frt.anchorMax = new Vector2(Mathf.Clamp01(fill), 1);
            }
        }

        // ------------------------------------------------------------------ screenshot

        private void TakeScreenshot() { StartCoroutine(ScreenshotRoutine()); }

        private IEnumerator ScreenshotRoutine()
        {
            bool was = m_hudVisible;
            float a = m_hudGroup.alpha;
            m_hudGroup.alpha = 0f;
            bool lens = m_lensRoot != null && m_lensRoot.gameObject.activeSelf;
            yield return new WaitForEndOfFrame();
            var tex = ScreenCapture.CaptureScreenshotAsTexture();
            m_hudGroup.alpha = a;
            if (tex == null) { Toast("Could not take the picture"); yield break; }
            byte[] png = tex.EncodeToPNG();
            Destroy(tex);
            string dir = Path.Combine(Application.persistentDataPath, "screenshots");
            Directory.CreateDirectory(dir);
            string stem = m_current != null ? Regex.Replace(m_current.Title, "[^A-Za-z0-9]+", "_") : "scene";
            string file = Path.Combine(dir, stem + "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".png");
            File.WriteAllBytes(file, png);
            Toast("Saved " + Path.GetFileName(file), 3f);
            Debug.Log("Screenshot saved: " + file);
        }

        private void UpdateLabTools()
        {
            if (m_screen != Screen_.Player) return;
            UpdateLensItems();
            DrainEvents();
            UpdatePipe();
        }
    }
}
