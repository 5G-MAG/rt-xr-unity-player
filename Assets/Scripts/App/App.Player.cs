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
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using MediaPlayer = GLTFast.MediaPlayer;

namespace rt.xr.unity.app
{
    public partial class App
    {
        private RectTransform m_playerRoot, m_hudRoot, m_bottom, m_rail, m_statsRoot;
        private CanvasGroup m_playerGroup, m_hudGroup;
        private Text m_pTitle, m_pSub, m_statsText;
        private bool m_hudVisible = true;
        private float m_hudLast;
        private float m_statsNext;
        private Button[] m_modeButtons = new Button[3];
        private Image[] m_modeBg = new Image[3];
        private Text[] m_modeText = new Text[3];
        private Image m_lensBg;
        private List<Animation> m_anims = new List<Animation>();
        private bool m_animPlaying;
        private Image m_animIcon;
        private readonly Dictionary<MediaPlayer, double> m_lastTime = new Dictionary<MediaPlayer, double>();
        private readonly Dictionary<MediaPlayer, Text> m_mediaStatus = new Dictionary<MediaPlayer, Text>();
        private readonly Dictionary<MediaPlayer, Image> m_mediaIcon = new Dictionary<MediaPlayer, Image>();
        private readonly Dictionary<MediaPlayer, bool> m_mediaPlaying = new Dictionary<MediaPlayer, bool>();
        private float m_mediaPoll;
        private Text m_arState;
        private RectTransform m_arCoach;
        private float m_arTrackingSince = -1f;
        private bool m_muted;

        // touch tracking for tap-to-toggle
        private Vector2 m_tapStart; private float m_tapTime; private int m_tapFinger = -1; private bool m_tapOverUi;

        private void BuildPlayer()
        {
            m_playerRoot = Ui.R("Player", m_root);
            Ui.Stretch(m_playerRoot);
            m_playerGroup = Ui.Group(m_playerRoot, false);
            m_playerGroup.blocksRaycasts = false;
            m_hudRoot = Ui.R("Hud", m_playerRoot);
            Ui.Stretch(m_hudRoot);
            m_hudGroup = Ui.Group(m_hudRoot, true);

            // top bar
            var top = Ui.R("Top", m_hudRoot);
            Ui.Place(top, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), Vector2.zero, new Vector2(0, 260));
            var tg = Ui.Box(top, Color.white, 0f, "Grad");
            tg.sprite = Ui.GradDown; tg.type = Image.Type.Simple;
            Ui.Stretch((RectTransform)tg.transform);
            ((RectTransform)tg.transform).offsetMax = new Vector2(0, 400);   // run on above the safe area so no hard edge shows at the notch
            var bar = Ui.R("Bar", top);
            Ui.Stretch(bar);
            var h = Ui.HBox(bar, 22, 0, TextAnchor.UpperLeft);
            h.padding = new RectOffset(32, 32, 28, 0);
            Ui.IconButton(bar, "back", 104, LeavePlayer);
            var col = Ui.R("Title", bar);
            Ui.VBox(col, 0, 0, TextAnchor.MiddleLeft);
            Ui.Size(col.gameObject, -1, 104, 1);
            m_pTitle = Ui.Label(col, "", 44, Ui.Bold, Color.white, TextAnchor.MiddleLeft, false);
            m_pSub = Ui.Label(col, "", 28, Ui.Regular, new Color(1, 1, 1, 0.75f), TextAnchor.MiddleLeft, false);
            Ui.IconButton(bar, "info", 104, () => ShowDetails(m_current));

            // right rail
            m_rail = Ui.R("Rail", m_hudRoot);
            Ui.Place(m_rail, new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0), new Vector2(-32, 520), new Vector2(104, 0));
            Ui.VBox(m_rail, 20, 0, TextAnchor.MiddleCenter, false);
            Ui.Fit(m_rail.gameObject);
            Ui.IconButton(m_rail, "eye", 104, () => { m_hudVisible = false; });
            Ui.IconButton(m_rail, "reset", 104, ResetView);
            var lens = Ui.IconButton(m_rail, "search", 104, ToggleLens);
            m_lensBg = lens.targetGraphic as Image;
            Ui.IconButton(m_rail, "tree", 104, () => ShowDetails(m_current, true));
            Ui.IconButton(m_rail, "camera", 104, TakeScreenshot);
            Ui.IconButton(m_rail, "lab", 104, ToggleTools);

            // bottom panel
            m_bottom = Ui.R("Bottom", m_hudRoot);
            Ui.Place(m_bottom, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), Vector2.zero, new Vector2(0, 0));
            Ui.VBox(m_bottom, 22, 0, TextAnchor.LowerCenter);
            Ui.Fit(m_bottom.gameObject);

            // stats overlay
            m_statsRoot = Ui.R("Stats", m_playerRoot);
            Ui.Place(m_statsRoot, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(32, -260), new Vector2(520, 0));
            var sb = Ui.Box(m_statsRoot, new Color(0, 0, 0, 0.62f), 26f, "Bg");
            Ui.Stretch((RectTransform)sb.transform);
            sb.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            Ui.VBox(m_statsRoot, 0, 24);
            Ui.Fit(m_statsRoot.gameObject);
            m_statsText = Ui.Label(m_statsRoot, "", 26, Ui.Regular, Color.white, TextAnchor.UpperLeft, true);
            m_statsRoot.gameObject.SetActive(false);
        }

        private void EnterPlayer()
        {
            m_screen = Screen_.Player;
            Ui.Show(m_libGroup, false); Ui.Show(m_labGroup, false); Ui.Show(m_setGroup, false); Ui.Show(m_tabsGroup, false);
            Ui.Show(m_playerGroup, true);
            m_playerGroup.blocksRaycasts = true;
            var e = m_current;
            m_pTitle.text = e.Title;
            m_pSub.text = e.Source + " · " + e.FileName;
            RememberRecent(e);
            RebuildBottom();
            m_hudVisible = true; m_hudLast = Time.unscaledTime;
            m_hudGroup.alpha = 1f;
            m_statsRoot.gameObject.SetActive(PlayerPrefs.GetInt("set.stats", 0) == 1);
            if (PlayerPrefs.GetInt("hint.orbit", 0) == 0 && !m_viewer.ARCameraEnabled)
            {
                PlayerPrefs.SetInt("hint.orbit", 1); PlayerPrefs.Save();
                Toast("Drag to orbit, pinch to zoom, tap to hide the controls", 4f);
            }
            OnPlayerEntered(e);
        }

        private void LeavePlayerCleanup()
        {
            m_playerGroup.blocksRaycasts = false;
            m_mediaStatus.Clear(); m_mediaIcon.Clear(); m_mediaPlaying.Clear(); m_lastTime.Clear(); m_mediaStopped.Clear(); m_mediaAll = new List<MediaPlayer>();
            m_anims.Clear();
            AudioListener.volume = (GLTFast.MediaPlayer.AwaitContentShown && !GLTFast.MediaPlayer.ContentShown) ? 0f : 1f; m_muted = false;
            CloseDetails();
            OnPlayerLeft();
        }

        private void ResetView()
        {
            if (m_rig == null) return;
            if (m_rig.CurrentMode == CamRig.Mode.Orbit) m_rig.FitOrbit();
            else m_rig.SetMode(m_rig.CurrentMode);
            Toast("View reset", 1.2f);
        }

        // ------------------------------------------------------------------ bottom controls

        private void RebuildBottom()
        {
            for (int i = m_bottom.childCount - 1; i >= 0; i--) Destroy(m_bottom.GetChild(i).gameObject);
            m_mediaStatus.Clear(); m_mediaIcon.Clear(); m_mediaPlaying.Clear();
            var gradient = Ui.Box(m_bottom, Color.white, 0f, "Grad");
            gradient.sprite = Ui.GradUp; gradient.type = Image.Type.Simple;
            gradient.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            Ui.Stretch((RectTransform)gradient.transform);
            ((RectTransform)gradient.transform).offsetMin = new Vector2(0, -400);
            gradient.rectTransform.offsetMax = new Vector2(0, 0);
            gradient.rectTransform.offsetMin = new Vector2(0, 0);

            var pad = Ui.R("Pad", m_bottom);
            Ui.VBox(pad, 22, 0, TextAnchor.LowerCenter);
            pad.GetComponent<VerticalLayoutGroup>().padding = new RectOffset(36, 36, 140, 36);

            bool ar = m_viewer.ARCameraEnabled;
            if (ar) BuildArCoach(pad);

            // media: one row per item, with play, pause and sound
            var players = FindObjectsOfType<MediaPlayer>().Where(p => p != null && p.HasPipeline).ToList();
            var medias = SafeMedias();
            // Table 4, controls: "Specifies that media controls should be displayed (such as a play/pause button etc)." Default false: the
            // controls are shown for a scene that has a media with controls true, or when the setting asks for them in every scene.
            m_mediaControls = PlayerPrefs.GetInt("set.alwayscontrols", 1) == 1 || (medias != null && medias.Any(m => m != null && m.controls));
            Debug.Log("XRTEST media controls shown=" + (players.Count > 0 && m_mediaControls) + " media=" + players.Count + " controlsFlag=" + (medias != null && medias.Any(m => m != null && m.controls)));
            if (players.Count > 0 && m_mediaControls)
            {
                var card = Ui.Box(pad, new Color(0.08f, 0.08f, 0.08f, 0.82f), 32f, "Media");
                Ui.VBox(card.transform, 8, 24);
                m_mediaAll = players;
                foreach (var p in players) m_mediaPlaying[p] = p.autoPlay && !GLTFast.MediaPlayer.HoldAutoplay;
                // one row for the whole scene: play, pause and stop act on every media together
                for (int i = 0; i < Math.Min(1, players.Count); i++)
                {
                    var mp = players[i];
                    string name = (medias != null && i < medias.Length && !string.IsNullOrEmpty(medias[i].name)) ? medias[i].name : "Media " + (i + 1);
                    // Table 4, name: the label of the media; the file name is used only when the scene gives none
                    if (string.IsNullOrEmpty(medias != null && i < medias.Length ? medias[i].name : null) && m_current != null && i < m_current.Media.Count && !string.IsNullOrEmpty(m_current.Media[i].Uri))
                    {
                        string f = m_current.Media[i].Uri; name = Uri.UnescapeDataString(f.Substring(f.LastIndexOf('/') + 1));
                    }
                    if (players.Count > 1) name += ", " + players.Count + " tracks";
                    var info = Ui.R("Info", card.transform);
                    Ui.VBox(info, 0, 0, TextAnchor.MiddleCenter);
                    Ui.Size(info.gameObject, -1, 84, 1);
                    Ui.Label(info, name, 32, Ui.Medium, Color.white, TextAnchor.MiddleCenter, false);
                    m_mediaStatus[mp] = Ui.Label(info, "Ready", 26, Ui.Regular, Ui.Dim, TextAnchor.MiddleCenter, false);
                    m_mediaPlaying[mp] = mp.autoPlay && !GLTFast.MediaPlayer.HoldAutoplay;
                }
                // transport in the style of the V3C player: a large white play button between two round glass buttons, labels below
                var bar = Ui.R("Transport", card.transform);
                Ui.HBox(bar, 56, 0, TextAnchor.LowerCenter);
                Ui.Size(bar.gameObject, -1, 200, 1);
                var first = players[0];
                var stopB = TransportButton(bar, "stop", 104, "Stop", false, () => StopMedia(first), null);
                var playB = TransportButton(bar, "play", 140, "Play", true, () => ToggleMedia(first), t => m_playLabel = t);
                m_mediaIcon[first] = playB.transform.GetChild(0).GetComponent<Image>();
                var soundB = TransportButton(bar, "volume", 104, "Sound", false, () => { m_muted = !m_muted; AudioListener.volume = m_muted ? 0f : 1f; var im = m_soundIcon; if (im != null) im.sprite = Ui.Icon(m_muted ? "mute" : "volume"); if (m_volume != null) m_volume.SetValueWithoutNotify(m_muted ? 0f : 1f); }, null);
                m_soundIcon = soundB.transform.GetChild(0).GetComponent<Image>();
                m_volume = Ui.Slider(card.transform, m_muted ? 0f : Mathf.Clamp01(AudioListener.volume), v =>
                {
                    AudioListener.volume = v;
                    m_muted = v <= 0.001f;
                    if (m_soundIcon != null) m_soundIcon.sprite = Ui.Icon(m_muted ? "mute" : "volume");
                });
            }

            // animation
            m_anims = m_viewer.GetComponentsInChildren<Animation>(true).Where(a => a.GetClipCount() > 0).ToList();
            if (m_anims.Count > 0)
            {
                m_animPlaying = m_anims.Any(a => a.isPlaying);
                var card = Ui.Box(pad, new Color(0.08f, 0.08f, 0.08f, 0.82f), 32f, "Animation");
                var row = Ui.R("Row", card.transform);
                Ui.HBox(row, 20, 0, TextAnchor.MiddleLeft);
                row.GetComponent<HorizontalLayoutGroup>().padding = new RectOffset(24, 24, 12, 12);
                Ui.Size(row.gameObject, -1, 112, 1);
                var btn = Ui.IconButton(row, m_animPlaying ? "pause" : "play", 84, ToggleAnimation, true);
                m_animIcon = btn.transform.GetChild(0).GetComponent<Image>();
                var col = Ui.R("Info", row);
                Ui.VBox(col, 0, 0, TextAnchor.MiddleLeft);
                Ui.Size(col.gameObject, -1, -1, 1);
                Ui.Label(col, "Animation", 32, Ui.Medium, Color.white, TextAnchor.MiddleLeft, false);
                Ui.Label(col, Ui.Plural(m_anims.Sum(a => a.GetClipCount()), "clip", "clips"), 26, Ui.Regular, Ui.Dim, TextAnchor.MiddleLeft, false);
                var le = card.gameObject.AddComponent<VerticalLayoutGroup>();
                le.padding = new RectOffset(0, 0, 0, 0); le.childControlWidth = true; le.childControlHeight = true;
            }

            // camera mode
            if (!ar && !rt.xr.unity.QuestRig.Active)
            {
                var seg = Ui.Box(pad, new Color(0.08f, 0.08f, 0.08f, 0.82f), 52f, "Modes");
                Ui.Size(seg.gameObject, -1, 112, 1);
                var hb = Ui.HBox(seg.transform, 8, 0, TextAnchor.MiddleCenter, true);
                hb.padding = new RectOffset(10, 10, 10, 10);
                string[] names = { "Orbit", "Walk", "Scene camera" };
                string[] icons = { "orbit", "walk", "video" };
                for (int i = 0; i < 3; i++)
                {
                    if (i == 2 && !m_rig.HasSceneCamera) continue;
                    int k = i;
                    var cell = Ui.Box(seg.transform, Ui.Accent, 42f, "Mode" + i);
                    Ui.Size(cell.gameObject, -1, -1, 1, 1);
                    Ui.HBox(cell.transform, 10, 0, TextAnchor.MiddleCenter);
                    var ic = Ui.IconImage(cell.transform, icons[i], Color.white, 40);
                    var tx = Ui.Label(cell.transform, names[i], 32, Ui.Medium, Color.white, TextAnchor.MiddleCenter, false);
                    m_modeBg[i] = cell; m_modeText[i] = tx;
                    m_modeButtons[i] = Ui.Click(cell.gameObject, cell, () => { m_rig.SetMode((CamRig.Mode)k); UpdateModeButtons(); });
                }
                UpdateModeButtons();
            }
        }
        private Image m_soundIcon;
        private Text m_playLabel;

        // round button with a caption below it, as in the V3C player transport bar
        private static Button TransportButton(Transform parent, string icon, float size, string label, bool primary, Action onClick, Action<Text> keep)
        {
            var col = Ui.R("Transport_" + label, parent);
            Ui.VBox(col, 8, 0, TextAnchor.UpperCenter, false);
            Ui.Size(col.gameObject, 190, -1);
            var btn = Ui.IconButton(col, icon, size, onClick, primary, true);
            var lb = Ui.Label(col, label, 26, Ui.Medium, Ui.Dim, TextAnchor.UpperCenter, false);
            if (keep != null) keep(lb);
            return btn;
        }

        private void UpdateModeButtons()
        {
            for (int i = 0; i < 3; i++)
            {
                if (m_modeBg[i] == null) continue;
                bool on = (int)m_rig.CurrentMode == i;
                m_modeBg[i].color = on ? Ui.Accent : new Color(0, 0, 0, 0);
                if (m_modeText[i] != null) m_modeText[i].color = on ? Color.white : Ui.Dim;
            }
        }

        private Slider m_volume;
        private List<MediaPlayer> m_mediaAll = new List<MediaPlayer>();
        private readonly HashSet<MediaPlayer> m_mediaStopped = new HashSet<MediaPlayer>();

        // Play, pause and stop act on every media of the scene together: they share the scene's timeline (a video and its sound are
        // often two media). Play and pause keep the position (pause freezes the picture and silences the sound).
        private void ToggleMedia(MediaPlayer clicked)
        {
            var all = m_mediaAll.Where(p => p != null).ToList();
            if (all.Any(p => m_mediaStopped.Contains(p))) { RestartScene(); return; }
            bool playing = m_mediaPlaying.ContainsKey(clicked) && m_mediaPlaying[clicked];
            // the user's choice wins for a moment: the clock of a media that was just paused still moves for a few frames
            m_userToggleAt = Time.unscaledTime;
            m_lastAdvance.Clear();
            foreach (var mp in all)
            {
                try
                {
                    if (playing) mp.PausePlayback();
                    else if (!mp.Started) mp.Play();
                    else mp.ResumePlayback();
                    m_mediaPlaying[mp] = !playing;
                    if (m_mediaIcon.ContainsKey(mp)) m_mediaIcon[mp].sprite = Ui.Icon(!playing ? "pause" : "play");
                }
                catch (Exception ex) { Toast("Could not " + (playing ? "pause" : "play") + " the media: " + ex.Message, 3f); }
            }
        }

        // Stop ends the playback and frees the streams; Play then starts the scene again from the beginning (a stream cannot be rewound).
        private void StopMedia(MediaPlayer clicked)
        {
            foreach (var mp in m_mediaAll.Where(p => p != null).ToList())
            {
                try
                {
                    mp.Stop();
                    m_mediaStopped.Add(mp);
                    m_mediaPlaying[mp] = false;
                    if (m_mediaIcon.ContainsKey(mp)) m_mediaIcon[mp].sprite = Ui.Icon("play");
                }
                catch (Exception ex) { Debug.LogWarning("Stop: " + ex.Message); }
            }
            Toast("Stopped. Press play to start again from the beginning.", 3f);
        }

        private void RestartScene()
        {
            var e = m_current;
            if (e == null) return;
            bool flat = !m_viewer.ARCameraEnabled;
            LeavePlayer();
            OpenScene(e, flat);
        }

        private void ToggleAnimation()
        {
            if (m_anims.Count == 0) return;
            if (m_animPlaying)
            {
                foreach (var a in m_anims) foreach (AnimationState st in a) st.speed = 0f;
                m_animPlaying = false;
            }
            else
            {
                foreach (var a in m_anims)
                {
                    foreach (AnimationState st in a) st.speed = 1f;
                    if (!a.isPlaying) a.Play();
                }
                m_animPlaying = true;
            }
            if (m_animIcon != null) m_animIcon.sprite = Ui.Icon(m_animPlaying ? "pause" : "play");
        }

        private void BuildArCoach(Transform parent)
        {
            var card = Ui.Box(parent, new Color(0.08f, 0.08f, 0.08f, 0.9f), 36f, "ArCoach");
            m_arCoach = (RectTransform)card.transform;
            Ui.VBox(card.transform, 14, 36);
            var row = Ui.R("Head", card.transform);
            Ui.HBox(row, 18, 0, TextAnchor.MiddleLeft);
            Ui.IconImage(row, "anchor", Ui.Good, 60);
            Ui.Label(row, "AR scene", 42, Ui.Bold, Color.white, TextAnchor.MiddleLeft, false);
            m_arState = Ui.Label(card.transform, "Starting the camera", 32, Ui.Regular, Ui.Dim, TextAnchor.UpperLeft, true);
            var uses = m_current != null ? m_current.Names.Where(n => n.StartsWith("TRACKABLE")).Select(Humanize).ToList() : new List<string>();
            if (uses.Count > 0) Ui.Label(card.transform, "Uses: " + string.Join(", ", uses), 28, Ui.Regular, Ui.Dim, TextAnchor.UpperLeft, true);
            var pv = Ui.Pill(card.transform, "Preview without AR", () => { var e = m_current; LeavePlayer(); OpenScene(e, true); }, Ui.BtnStyle.Ghost, "eye");
            Ui.Size(pv.gameObject, -1, 100, 1);
            m_arTrackingSince = -1f; m_arPlaced = false; m_oldPlanes.Clear();
            m_userScale = 1f; m_userYaw = 0f; m_gestActive = false; m_placeOnWall = false; m_scanAgain = false;
            if (GLTFast.AnchorPlacement.TapToPlace && GLTFast.AnchorPlacement.HasPlaceable)
            {
                var placeRow = Ui.R("PlaceRow", card.transform);
                Ui.HBox(placeRow, 16, 0, TextAnchor.MiddleLeft);
                Ui.Size(placeRow.gameObject, -1, 100, 1);
                m_arLockBtn = Ui.Pill(placeRow, "Lock in place", ToggleArLock, Ui.BtnStyle.Ghost);
                Ui.Size(m_arLockBtn.gameObject, -1, 100, 1);
                var rs = Ui.Pill(placeRow, "Reset size", ResetArTransform, Ui.BtnStyle.Ghost);
                Ui.Size(rs.gameObject, -1, 100, 1);
                var modeRow = Ui.R("ModeRow", card.transform);
                Ui.HBox(modeRow, 16, 0, TextAnchor.MiddleLeft);
                Ui.Size(modeRow.gameObject, -1, 100, 1);
                m_floorBtn = Ui.Pill(modeRow, "On the floor", () => SetSurfaceMode(false), Ui.BtnStyle.Ghost);
                Ui.Size(m_floorBtn.gameObject, -1, 100, 1);
                m_wallBtn = Ui.Pill(modeRow, "On a wall", () => SetSurfaceMode(true), Ui.BtnStyle.Ghost);
                Ui.Size(m_wallBtn.gameObject, -1, 100, 1);
                m_modeHelp = Ui.Label(card.transform, "", 28, Ui.Regular, Ui.Dim, TextAnchor.MiddleLeft, false);
                SetSurfaceMode(false);
            }
        }

        // In AR a placed scene faces the user, not a fixed direction: the fill light shines from the viewer's side, so the front of a surface
        // is lit and not its back.
        private void FillLightFollowsCamera()
        {
            if (m_viewer == null || !m_viewer.ARCameraEnabled || Camera.main == null) return;
            var fl = GameObject.Find("FillLight");
            if (fl == null) return;
            fl.transform.rotation = Camera.main.transform.rotation * Quaternion.Euler(18f, -18f, 0f);
        }

        // The side buttons sit above the bottom panel, whatever its height (it grows with the AR placement rows).
        private void KeepRailAboveBottomPanel()
        {
            if (m_rail == null || m_bottom == null) return;
            float h = LayoutUtility.GetPreferredHeight(m_bottom);
            if (h < 1f) h = ((RectTransform)m_bottom).rect.height;
            var p = m_rail.anchoredPosition;
            float y = h + 28f;
            if (Mathf.Abs(p.y - y) > 1f) m_rail.anchoredPosition = new Vector2(p.x, y);
        }

        private static Material s_planeMat;

        // Surfaces known when the user pressed Unlock are not offered again: placing anew needs surfaces scanned since then.
        private readonly HashSet<UnityEngine.XR.ARSubsystems.TrackableId> m_oldPlanes = new HashSet<UnityEngine.XR.ARSubsystems.TrackableId>();
        private int FreshSurfaceCount(bool vertical)
        {
            var pm = FindObjectOfType<ARPlaneManager>();
            if (pm == null) return 0;
            int n = 0;
            foreach (var plane in pm.trackables)
                if (plane != null && !m_oldPlanes.Contains(plane.trackableId) && (plane.alignment == PlaneAlignment.Vertical) == vertical) n++;
            return n;
        }

        private Button m_arLockBtn;
        private Button m_wallBtn;
        private bool m_placeOnWall;
        private bool m_scanAgain;

        private Button m_floorBtn;
        private Text m_modeHelp;
        // Floor: the scene stands on a horizontal surface (floor, table). Wall: it hangs upright on a vertical surface. The detected
        // surfaces are drawn green (horizontal) and blue (vertical), and only the kind that matches the choice is drawn.
        private void SetSurfaceMode(bool wall)
        {
            m_placeOnWall = wall;
            if (m_floorBtn != null) m_floorBtn.GetComponent<Image>().color = wall ? new Color(1, 1, 1, 0.12f) : Ui.Accent;
            if (m_wallBtn != null) m_wallBtn.GetComponent<Image>().color = wall ? Ui.Accent : new Color(1, 1, 1, 0.12f);
            if (m_floorBtn != null) { var t = m_floorBtn.GetComponentInChildren<Text>(); if (t != null) t.color = wall ? Color.white : Ui.OnAccent; }
            if (m_wallBtn != null) { var t = m_wallBtn.GetComponentInChildren<Text>(); if (t != null) t.color = wall ? Ui.OnAccent : Color.white; }
            if (m_modeHelp != null) m_modeHelp.text = wall
                ? "On a wall: tap a blue area. The scene hangs upright, facing you."
                : "On the floor: tap a green area. The scene stands on the floor or a table.";
        }
        private float m_userScale = 1f, m_userYaw = 0f;
        private bool m_gestActive;
        private float m_gestDist, m_gestAngle;

        private void ToggleArLock()
        {
            GLTFast.AnchorPlacement.Locked = !GLTFast.AnchorPlacement.Locked;
            var label = m_arLockBtn != null ? m_arLockBtn.GetComponentInChildren<Text>() : null;
            if (label != null) label.text = GLTFast.AnchorPlacement.Locked ? "Unlock" : "Lock in place";
            if (GLTFast.AnchorPlacement.Locked) { HideReticle(); m_scanAgain = false; }
            else
            {
                HideReticle(); m_scanAgain = true;
                // new surfaces must be scanned: the ones known now are set aside
                var pmU = PlayerPrefs.GetInt("set.unlockscan", 1) == 1 ? FindObjectOfType<ARPlaneManager>() : null;
                if (pmU != null) foreach (var pl in pmU.trackables) if (pl != null) m_oldPlanes.Add(pl.trackableId);
            }   // unlocked: the surfaces are scanned and drawn again until the scene is placed
            Toast(GLTFast.AnchorPlacement.Locked ? "Locked. The scene stays where it is." : "Unlocked. Move the phone to scan the surfaces, then tap where the scene should go.", 3.5f);
        }

        private void ResetArTransform()
        {
            m_userScale = 1f; m_userYaw = 0f;
            GLTFast.AnchorPlacement.SetUserTransform(1f, 0f);
        }

        // Two fingers on a placed scene: pinch resizes it, twisting turns it about the vertical axis.
        private void UpdateArGestures()
        {
            if (!GLTFast.AnchorPlacement.TapToPlace || !GLTFast.AnchorPlacement.HasPlaceable || !m_viewer.ARCameraEnabled) return;
            if (GLTFast.AnchorPlacement.AnyAwaiting || GLTFast.AnchorPlacement.Locked || Input.touchCount != 2) { m_gestActive = false; return; }
            var a = Input.GetTouch(0); var b = Input.GetTouch(1);
            Vector2 d = b.position - a.position;
            float dist = d.magnitude;
            float ang = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
            if (!m_gestActive || a.phase == TouchPhase.Began || b.phase == TouchPhase.Began)
            {
                m_gestActive = true; m_gestDist = dist; m_gestAngle = ang;
                return;
            }
            float ratio = dist / Mathf.Max(m_gestDist, 1f);
            m_userScale = Mathf.Clamp(m_userScale * ratio, 0.05f, 20f);
            m_userYaw -= Mathf.DeltaAngle(m_gestAngle, ang);
            m_gestDist = dist; m_gestAngle = ang;
            GLTFast.AnchorPlacement.SetUserTransform(m_userScale, m_userYaw);
        }

        // ------------------------------------------------------------------ per frame

        private void UpdatePlayer()
        {
            if (m_screen != Screen_.Player) return;
            if (m_viewer != null) m_viewer.showLog = PlayerPrefs.GetInt("set.showlog", 0) == 1;

            // tap to show or hide the controls
            var touches = Input.touches;
            if (!m_detailsOpen && !m_dlgOpen)
            {
                foreach (var t in touches)
                {
                    if (t.phase == TouchPhase.Began && m_tapFinger < 0)
                    {
                        m_tapFinger = t.fingerId; m_tapStart = t.position; m_tapTime = Time.unscaledTime;
                        m_tapOverUi = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(t.fingerId);
                    }
                    else if (t.fingerId == m_tapFinger && (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled))
                    {
                        bool tap = t.phase == TouchPhase.Ended && !m_tapOverUi && Time.unscaledTime - m_tapTime < 0.35f && (t.position - m_tapStart).magnitude < 24f && Input.touchCount <= 1;
                        m_tapFinger = -1;
                        if (tap)
                        {
                            // with the controls hidden, a tap brings them back (unless it is placing the scene in AR)
                            bool placing = m_viewer.ARCameraEnabled && GLTFast.AnchorPlacement.HasPlaceable && GLTFast.AnchorPlacement.AnyAwaiting;
                            if (!m_hudVisible && !placing) { m_hudVisible = true; m_hudLast = Time.unscaledTime; TryTapVideo(t.position); }
                            else if (!TryTapVideo(t.position) && !TryTapToPlace(t.position) && !OnSceneTap(t.position)) { m_hudVisible = !m_hudVisible; m_hudLast = Time.unscaledTime; }
                        }
                    }
                    if (t.phase != TouchPhase.Ended && t.phase != TouchPhase.Canceled && t.fingerId != m_tapFinger && m_tapFinger >= 0) m_tapFinger = -1;
                }
            }
            KeepRailAboveBottomPanel();
            FillLightFollowsCamera();
            if (m_xr) { m_hudVisible = true; m_hudLast = Time.unscaledTime; }
            if (m_toolsOpen || m_detailsOpen || m_dlgOpen) m_hudLast = Time.unscaledTime;
            if (m_hudVisible && PlayerPrefs.GetInt("set.autohide", 1) == 1 && Time.unscaledTime - m_hudLast > 7f && !m_viewer.ARCameraEnabled) m_hudVisible = false;
            if (Input.touchCount > 0 && m_hudVisible) m_hudLast = Time.unscaledTime;
            if (m_cleanShot) { m_hudVisible = false; if (m_statsRoot != null) m_statsRoot.gameObject.SetActive(false); }
            Ui.Fade(m_hudGroup, m_hudVisible ? 1f : 0f);
            if (m_rig != null) m_rig.Enabled = !(m_detailsOpen || m_dlgOpen);

            if (Time.unscaledTime > m_mediaPoll) { m_mediaPoll = Time.unscaledTime + 0.25f; PollMedia(); }
            if (m_arState != null) UpdateAr();
            UpdateArGestures();
            UpdateReticle();
            UpdateVideoOverlay();
            UpdatePlaneVisibility();
            if (m_statsRoot.gameObject.activeSelf && Time.unscaledTime > m_statsNext) { m_statsNext = Time.unscaledTime + 0.25f; UpdateStats(); }
        }

        private void PollMedia()
        {
            foreach (var kv in m_mediaStatus.ToList())
            {
                var mp = kv.Key;
                if (mp == null) continue;
                double last;
                m_lastTime.TryGetValue(mp, out last);
                // the clock moves in steps of frames and the poll runs at its own pace: the media is called advancing when its time moved
                // within the last second, so the status does not flip between playing and buffering on every poll
                if (mp.CurrentTime > last + 0.001) m_lastAdvance[mp] = Time.unscaledTime;
                float la; m_lastAdvance.TryGetValue(mp, out la);
                bool advancing = Time.unscaledTime - la < 1f && la > 0f;
                m_lastTime[mp] = mp.CurrentTime;
                string s;
                if (m_mediaStopped.Contains(mp)) s = "Stopped";
                else if (mp.Error) s = "Error in the media pipeline";
                else if (!mp.Started) s = "Ready, press play";
                else if (mp.CurrentTime < 0) s = "Starting";
                else if (Time.unscaledTime - m_userToggleAt < 2.5f && m_mediaPlaying.ContainsKey(mp)) s = m_mediaPlaying[mp] ? "Playing " + FormatTime(mp.CurrentTime) : "Paused at " + FormatTime(Math.Max(0, mp.CurrentTime));
                else if (advancing) s = "Playing " + FormatTime(mp.CurrentTime);
                else s = m_mediaPlaying.ContainsKey(mp) && m_mediaPlaying[mp] ? "Buffering " + FormatTime(mp.CurrentTime) : "Paused at " + FormatTime(Math.Max(0, mp.CurrentTime));
                kv.Value.text = s;
                kv.Value.color = mp.Error ? Ui.Bad : Ui.Dim;
                if (m_playLabel != null) m_playLabel.text = AnyMediaPlaying() ? "Pause" : "Play";
                if (advancing && Time.unscaledTime - m_userToggleAt > 2.5f && m_mediaPlaying.ContainsKey(mp) && !m_mediaPlaying[mp]) { m_mediaPlaying[mp] = true; if (m_mediaIcon.ContainsKey(mp)) m_mediaIcon[mp].sprite = Ui.Icon("pause"); }
            }
        }

        private float m_userToggleAt = -10f;
        private readonly Dictionary<MediaPlayer, float> m_lastAdvance = new Dictionary<MediaPlayer, float>();
        private static string FormatTime(double t) { int s = (int)Math.Max(0, t); return (s / 60) + ":" + (s % 60).ToString("00"); }

        // The AR session of the viewer detects planes but has no raycast component: add one to the XR Origin so taps can hit them.
        private ARRaycastManager EnsureRaycastManager()
        {
            var rm = FindObjectOfType<ARRaycastManager>();
            if (rm != null) return rm;
            var origin = FindObjectOfType<Unity.XR.CoreUtils.XROrigin>();
            if (origin == null) return null;
            rm = origin.gameObject.AddComponent<ARRaycastManager>();
            Debug.Log("App: raycast manager added to the XR Origin");
            return rm;
        }

        // Tap on a surface: the scene is placed there provisionally, with a blue circle on the surface. Tapping the circle again fixes
        // the scene; tapping elsewhere moves it. Returns true when the tap was used for placing.
        private bool TryTapToPlace(Vector2 screenPos)
        {
            if (!GLTFast.AnchorPlacement.TapToPlace || !GLTFast.AnchorPlacement.HasPlaceable || !m_viewer.ARCameraEnabled) return false;
            if (GLTFast.AnchorPlacement.Locked && !GLTFast.AnchorPlacement.AnyAwaiting) { Toast("The scene is fixed. Press Unlock to move it.", 2.5f); return true; }
            var rm = EnsureRaycastManager();
            if (rm == null) { Toast("This phone gave no surfaces to tap on.", 3f); return true; }
            // nothing is placed before a surface has been found
            if (!m_placeOnWall && FreshSurfaceCount(false) == 0) { Toast("Move the phone slowly over the floor first. Tap when green surfaces show.", 3.5f); return true; }
            var cam = Camera.main;
            var hits = new List<ARRaycastHit>();
            if (m_placeOnWall && cam != null)
            {
                return PlaceOnWall(rm, screenPos, cam, hits);
            }
            bool planeHit = rm.Raycast(screenPos, hits, TrackableType.PlaneWithinPolygon);
            Debug.Log("App: tap " + screenPos + " planes=" + hits.Count + (hits.Count > 0 && hits[0].trackable is ARPlane ? " first=" + ((ARPlane)hits[0].trackable).alignment : ""));
            int oldHits = 0, freshHits = 0;
            foreach (var h0 in hits) { var p0 = h0.trackable as ARPlane; if (p0 == null) continue; if (m_oldPlanes.Contains(p0.trackableId)) oldHits++; else freshHits++; }
            Debug.Log("App: tap surfaces under the finger: fresh " + freshHits + ", set aside " + oldHits);
            if (planeHit)
            {
                foreach (var h in hits)
                {
                    var plane = h.trackable as ARPlane;
                    if (plane == null || m_oldPlanes.Contains(plane.trackableId)) continue;
                    if (ConfirmOnReticle(h.pose.position)) return true;
                    if (GLTFast.AnchorPlacement.TryPlace(plane, h.pose, true))
                    {
                        m_scanAgain = false;
                        bool wall = plane.alignment == PlaneAlignment.Vertical;
                        Vector3 n = plane.transform.up;
                        if (cam != null && Vector3.Dot(n, cam.transform.position - h.pose.position) < 0f) n = -n;
                        ShowReticle(h.pose.position, n);
                        return true;
                    }
                }
            }
            // No plane took the tap. Held upright the phone is looking at a wall, which often has no plane: place the scene on the wall.
            if (LooksAtWall(cam))
            {
                return PlaceOnWall(rm, screenPos, cam, hits);
            }
            // No plane there (a plain wall often has none): use the surface the phone estimates from the feature points under the finger.
            hits.Clear();
            if (rm.Raycast(screenPos, hits, TrackableType.FeaturePoint))
            {
                var h = hits[0];
                Vector3 n = h.pose.up;
                bool wall = Mathf.Abs(Vector3.Dot(n, Vector3.up)) < 0.5f;
                Debug.Log("App: tap feature point normal=" + n.ToString("0.00") + " wall=" + wall);
                if (ConfirmOnReticle(h.pose.position)) return true;
                if (GLTFast.AnchorPlacement.TryPlacePose(h.pose, wall, true))
                {
                    m_scanAgain = false;
                    if (cam != null && Vector3.Dot(n, cam.transform.position - h.pose.position) < 0f) n = -n;
                    ShowReticle(h.pose.position, n);
                    return true;
                }
            }
            if (freshHits == 0 && oldHits > 0) Toast("That was a surface from before Unlock. Tap a green surface.", 3.5f);
            else if (freshHits > 0) Toast("The scene could not be placed on that surface. Tap another green surface.", 3.5f);
            else Toast("No surface there yet. Move the phone slowly so it can see the floor or the wall.", 3.5f);
            return true;
        }

        // Wall: upright, facing the viewer. The distance comes from a detected surface or a feature point under the finger, else 1.5 m.
        private bool PlaceOnWall(ARRaycastManager rm, Vector2 screenPos, Camera cam, List<ARRaycastHit> hits)
        {
            // Wall: upright, facing the viewer. The distance comes from a detected surface or a feature point under the finger, else 1.5 m.
            Vector3 facing = -Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up);
            if (facing.sqrMagnitude < 1e-4f) facing = -cam.transform.forward;
            facing.Normalize();
            Ray ray = cam.ScreenPointToRay(screenPos);
            Vector3 wallPoint = ray.origin + ray.direction * 1.5f;
            string source = "view 1.5 m";
            if (rm.Raycast(screenPos, hits, TrackableType.PlaneWithinPolygon | TrackableType.FeaturePoint) && hits.Count > 0)
            {
                wallPoint = hits[0].pose.position; source = "hit " + hits[0].hitType;
                var vertical = hits[0].trackable as ARPlane;
                if (vertical != null && vertical.alignment == PlaneAlignment.Vertical)
                {
                    facing = vertical.transform.up;
                    if (Vector3.Dot(facing, cam.transform.position - wallPoint) < 0f) facing = -facing;
                    facing = Vector3.ProjectOnPlane(facing, Vector3.up).normalized;
                }
            }
            Debug.Log("App: tap on wall at " + wallPoint.ToString("0.00") + " from " + source);
            if (ConfirmOnReticle(wallPoint)) return true;
            var wallPose = new Pose(wallPoint, Quaternion.FromToRotation(Vector3.up, facing));
            if (GLTFast.AnchorPlacement.TryPlacePose(wallPose, true, true))
            {
                m_scanAgain = false;
                ShowReticle(wallPoint, facing);
                return true;
            }
            Toast("The scene could not be placed there.", 2.5f);
            return true;
        }

        // Held upright the phone looks at a wall, held steeply down it looks at the floor.
        private static bool LooksAtWall(Camera cam)
        {
            return cam != null && cam.transform.forward.y > -0.55f;
        }

        // ---- play button on the video itself -------------------------------------------------------------------------------

        private GameObject m_playOverlay;
        private bool m_mediaControls;
        private readonly List<Renderer> m_videoRenderers = new List<Renderer>();
        private float m_videoScan;

        private void RefreshVideoRenderers()
        {
            m_videoRenderers.Clear();
            var targets = new HashSet<Texture>();
            foreach (var mp in m_mediaAll)
                if (mp != null) foreach (var tex in mp.VideoTargetTextures) targets.Add(tex);
            if (targets.Count == 0) return;
            foreach (var r in FindObjectsOfType<Renderer>())
            {
                if (r == null || !r.enabled || r.gameObject == m_playOverlay) continue;
                var m = r.sharedMaterial;
                if (m != null && m.HasProperty("_MainTex") && m.mainTexture != null && targets.Contains(m.mainTexture)) m_videoRenderers.Add(r);
                else if (m != null && m.HasProperty("baseColorTexture") && m.GetTexture("baseColorTexture") != null && targets.Contains(m.GetTexture("baseColorTexture"))) m_videoRenderers.Add(r);
            }
        }

        private bool AnyMediaPlaying() { foreach (var v in m_mediaPlaying.Values) if (v) return true; return false; }

        // A tap on the video plays or pauses it.
        private bool TryTapVideo(Vector2 screenPos)
        {
            var cam = Camera.main;
            if (cam == null || m_mediaAll.Count == 0 || !m_mediaControls) return false;
            if (GLTFast.AnchorPlacement.HasPlaceable && GLTFast.AnchorPlacement.AnyAwaiting) return false;   // the scene is not placed yet
            if (m_videoRenderers.Count == 0) RefreshVideoRenderers();
            Ray ray = cam.ScreenPointToRay(screenPos);
            foreach (var r in m_videoRenderers)
            {
                if (r == null) continue;
                var b = r.bounds;
                b.Expand(0.05f);
                if (b.IntersectRay(ray))
                {
                    var first = m_mediaAll.FirstOrDefault(p => p != null);
                    if (first != null) ToggleMedia(first);
                    return true;
                }
            }
            return false;
        }

        // A round play button over the video while nothing is playing.
        private void UpdateVideoOverlay()
        {
            if (m_mediaAll.Count == 0 || !m_mediaControls) { if (m_playOverlay != null) m_playOverlay.SetActive(false); return; }
            if (Time.unscaledTime > m_videoScan) { m_videoScan = Time.unscaledTime + 1f; RefreshVideoRenderers(); }
            var cam = Camera.main;
            Renderer target = null;
            foreach (var r in m_videoRenderers) if (r != null && r.gameObject.activeInHierarchy) { target = r; break; }
            bool show = target != null && cam != null && !AnyMediaPlaying() && !(GLTFast.AnchorPlacement.HasPlaceable && GLTFast.AnchorPlacement.AnyAwaiting);
            if (!show) { if (m_playOverlay != null) m_playOverlay.SetActive(false); return; }
            if (m_playOverlay == null)
            {
                const int size = 128;
                var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
                var px = new Color32[size * size];
                for (int y = 0; y < size; y++)
                    for (int x = 0; x < size; x++)
                    {
                        float dx = (x + 0.5f) / size * 2f - 1f, dy = (y + 0.5f) / size * 2f - 1f;
                        float r = Mathf.Sqrt(dx * dx + dy * dy);
                        px[y * size + x] = new Color(0f, 0f, 0f, Mathf.Clamp01((0.95f - r) / 0.06f) * 0.62f);
                    }
                tex.SetPixels32(px); tex.Apply(false, true); tex.filterMode = FilterMode.Bilinear;
                m_playOverlay = new GameObject("PlayOverlay");
                var disc = m_playOverlay.AddComponent<SpriteRenderer>();
                disc.sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
                disc.sortingOrder = 200;
                var glyph = new GameObject("Glyph");
                glyph.transform.SetParent(m_playOverlay.transform, false);
                glyph.transform.localPosition = new Vector3(0f, 0f, -0.001f);
                var gr = glyph.AddComponent<SpriteRenderer>();
                var icon = Ui.Icon("play");
                gr.sprite = icon; gr.sortingOrder = 201;
                if (icon != null) glyph.transform.localScale = Vector3.one * (0.6f / (icon.rect.width / icon.pixelsPerUnit));
            }
            m_playOverlay.SetActive(true);
            var bounds = target.bounds;
            Vector3 toCam = (cam.transform.position - bounds.center);
            var dims = new[] { bounds.size.x, bounds.size.y, bounds.size.z };
            System.Array.Sort(dims);
            float diameter = Mathf.Clamp(dims[1] * 0.3f, 0.06f, 0.5f);   // the smaller of the two large sides of the screen
            m_playOverlay.transform.position = bounds.center + toCam.normalized * 0.02f;
            m_playOverlay.transform.rotation = Quaternion.LookRotation(-toCam.normalized, cam.transform.up);
            m_playOverlay.transform.localScale = Vector3.one * diameter;
        }

        private GameObject m_reticle;
        private Vector3 m_reticleNormal = Vector3.up;

        // A tap on the blue circle fixes the scene where it is.
        private bool ConfirmOnReticle(Vector3 hitPosition)
        {
            if (m_reticle == null || !m_reticle.activeSelf) return false;
            if (Vector3.Distance(hitPosition, m_reticle.transform.position) > 0.3f) return false;
            FixScene();
            return true;
        }

        private void FixScene()
        {
            GLTFast.AnchorPlacement.Locked = true;
            if (m_reticle != null) m_reticle.SetActive(false);
            var label = m_arLockBtn != null ? m_arLockBtn.GetComponentInChildren<Text>() : null;
            if (label != null) label.text = "Unlock";
            Toast("Scene fixed here. Press Unlock to move it.", 3f);
        }

        private void ShowReticle(Vector3 position, Vector3 normal)
        {
            if (m_reticle == null)
            {
                const int size = 128;
                var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
                var pixels = new Color32[size * size];
                Color accent = Ui.Accent;
                for (int y = 0; y < size; y++)
                    for (int x = 0; x < size; x++)
                    {
                        float dx = (x + 0.5f) / size * 2f - 1f, dy = (y + 0.5f) / size * 2f - 1f;
                        float r = Mathf.Sqrt(dx * dx + dy * dy);
                        float ring = Mathf.Clamp01(1f - Mathf.Abs(r - 0.86f) / 0.1f);
                        float fill = r < 0.8f ? 0.22f : 0f;
                        float a = Mathf.Max(ring, fill);
                        pixels[y * size + x] = new Color(accent.r, accent.g, accent.b, a);
                    }
                tex.SetPixels32(pixels); tex.Apply(false, true);
                tex.filterMode = FilterMode.Bilinear;
                var sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size / 0.4f);
                m_reticle = new GameObject("PlaceReticle");
                var sr = m_reticle.AddComponent<SpriteRenderer>();
                sr.sprite = sprite;
                sr.sortingOrder = 100;
            }
            m_reticleNormal = normal.normalized;
            m_reticle.SetActive(!GLTFast.AnchorPlacement.Locked);
            m_reticle.transform.position = position + m_reticleNormal * 0.004f;
            m_reticle.transform.rotation = Quaternion.LookRotation(m_reticleNormal);
        }

        private void HideReticle() { if (m_reticle != null) m_reticle.SetActive(false); }

        private void UpdateReticle()
        {
            if (m_reticle == null || !m_reticle.activeSelf) return;
            float s = 1f + 0.08f * Mathf.Sin(Time.unscaledTime * 5f);
            m_reticle.transform.localScale = new Vector3(s, s, s);
        }

        // Once the scene is placed the detected surfaces are no longer drawn (they stay usable for placing again).
        private void UpdatePlaneVisibility()
        {
            if (!GLTFast.AnchorPlacement.TapToPlace || !GLTFast.AnchorPlacement.HasPlaceable) return;
            var pm = FindObjectOfType<ARPlaneManager>();
            if (pm == null) return;
            bool placed = !GLTFast.AnchorPlacement.AnyAwaiting && !m_scanAgain;
            foreach (var plane in pm.trackables)
            {
                if (plane == null) continue;
                bool vertical = plane.alignment == PlaneAlignment.Vertical;
                bool wanted = !placed && (vertical == m_placeOnWall) && !m_oldPlanes.Contains(plane.trackableId);
                if (plane.gameObject.activeSelf != wanted) plane.gameObject.SetActive(wanted);
                if (!wanted) continue;
                DrawSurface(plane, vertical);
            }
            // surfaces that are no longer wanted lose their drawing
            foreach (var kv in m_surfaceViz.ToList())
            {
                if (kv.Key == null || !kv.Key.gameObject.activeSelf) { if (kv.Value != null) kv.Value.SetActive(false); }
            }
        }

        // Each detected surface is drawn by the app itself: a translucent flat mesh over its boundary, green for horizontal and blue for
        // vertical surfaces (the prefab's own drawing was not visible on the test phone).
        private readonly Dictionary<ARPlane, GameObject> m_surfaceViz = new Dictionary<ARPlane, GameObject>();
        private readonly Dictionary<ARPlane, int> m_surfaceVizCount = new Dictionary<ARPlane, int>();
        private void DrawSurface(ARPlane plane, bool vertical)
        {
            var pmr = plane.GetComponent<MeshRenderer>();
            if (pmr != null) pmr.enabled = false;
            GameObject viz;
            if (!m_surfaceViz.TryGetValue(plane, out viz) || viz == null)
            {
                viz = new GameObject("SurfaceViz");
                viz.transform.SetParent(plane.transform, false);
                viz.transform.localPosition = new Vector3(0f, 0.004f, 0f);
                viz.AddComponent<MeshFilter>();
                var mr2 = viz.AddComponent<MeshRenderer>();
                if (s_planeMat == null) { var sh = Shader.Find("Sprites/Default") ?? Shader.Find("UI/Default"); if (sh != null) s_planeMat = new Material(sh); }
                var mat = new Material(s_planeMat) { color = vertical ? new Color(0.25f, 0.6f, 1f, 0.45f) : new Color(0.2f, 0.85f, 0.45f, 0.45f) };
                mr2.sharedMaterial = mat;
                mr2.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                mr2.receiveShadows = false;
                m_surfaceViz[plane] = viz; m_surfaceVizCount[plane] = -1;
            }
            viz.SetActive(true);
            var b = plane.boundary;
            if (b.Length < 3) return;
            int last; m_surfaceVizCount.TryGetValue(plane, out last);
            if (last != b.Length || Time.frameCount % 30 == 0)
            {
                var verts = new Vector3[b.Length + 1]; var tris = new int[b.Length * 3 * 2];
                Vector2 c = Vector2.zero; for (int i = 0; i < b.Length; i++) c += b[i]; c /= b.Length;
                verts[b.Length] = new Vector3(c.x, 0, c.y);
                for (int i = 0; i < b.Length; i++)
                {
                    verts[i] = new Vector3(b[i].x, 0, b[i].y);
                    int n = (i + 1) % b.Length;
                    tris[i * 6] = i; tris[i * 6 + 1] = n; tris[i * 6 + 2] = b.Length;
                    tris[i * 6 + 3] = i; tris[i * 6 + 4] = b.Length; tris[i * 6 + 5] = n;   // both sides
                }
                var mf = viz.GetComponent<MeshFilter>(); if (mf.sharedMesh == null) mf.sharedMesh = new Mesh();
                var mesh = mf.sharedMesh; mesh.Clear(); mesh.vertices = verts; mesh.triangles = tris; mesh.RecalculateBounds();
                m_surfaceVizCount[plane] = b.Length;
            }
        }

        private void UpdateAr()
        {
            var st = ARSession.state;
            string text;
            switch (st)
            {
                case ARSessionState.SessionTracking:
                    if (GLTFast.AnchorPlacement.TapToPlace && GLTFast.AnchorPlacement.HasPlaceable)
                        if ((GLTFast.AnchorPlacement.AnyAwaiting || m_scanAgain) && FreshSurfaceCount(m_placeOnWall) == 0) text = m_placeOnWall ? "Move the phone slowly along the wall." : "Move the phone slowly over the floor.";
                        else if (GLTFast.AnchorPlacement.AnyAwaiting) text = "Tap a green surface where the scene should go.";
                        else if (m_scanAgain) text = "Tap a new green surface to move the scene there.";
                        else if (GLTFast.AnchorPlacement.Locked) text = "Scene fixed in place. Press Unlock to move it.";
                        else text = "Tap the blue circle again to fix the scene here, or tap elsewhere to move it. Pinch to resize, twist to turn.";
                    else
                        text = m_arPlaced ? "Scene placed on the floor. Look around to find it." : "Tracking. Move around to place the scene.";
                    if (m_arTrackingSince < 0) m_arTrackingSince = Time.unscaledTime;
                    break;
                case ARSessionState.SessionInitializing: text = "Move your phone slowly so it can see the room."; m_arTrackingSince = -1; break;
                case ARSessionState.Unsupported: text = "This phone does not support AR. Use the flat preview."; break;
                case ARSessionState.NeedsInstall: text = "AR services need to be installed or updated."; break;
                case ARSessionState.CheckingAvailability: text = "Checking AR support."; break;
                case ARSessionState.Installing: text = "Installing AR services."; break;
                case ARSessionState.Ready: text = "Starting the camera."; break;
                default: text = "AR state: " + st; break;
            }
            if (st == ARSessionState.SessionTracking && Time.unscaledTime > m_arNextProbe)
            {
                m_arNextProbe = Time.unscaledTime + 1f;
                var pmg = FindObjectOfType<ARPlaneManager>();
                if (pmg != null) { int act = 0, tot = 0; foreach (var pl in pmg.trackables) { tot++; if (pl.gameObject.activeSelf) act++; } Debug.Log("App: planes " + act + " of " + tot + " active, awaiting=" + GLTFast.AnchorPlacement.AnyAwaiting + " scanAgain=" + m_scanAgain + " prefab=" + (pmg.planePrefab != null)); }
                foreach (var a in FindObjectsOfType<UnityEngine.XR.ARFoundation.ARAnchor>())
                {
                    if (a.transform.childCount > 0) m_arPlaced = true;
                    // measurement of the anchor: where it is and how well it is tracked
                    var cm = Camera.main;
                    Debug.Log("App: anchor " + a.trackableId + " " + a.trackingState + " pos=" + a.transform.position.ToString("0.000") + " cam=" + (cm != null ? cm.transform.position.ToString("0.000") : "-"));
                }
            }
            // the scene is shown once it is anchored, or once the user has chosen the spot: only then do its media start (clause 4.3, T0)
            if (GLTFast.MediaPlayer.AwaitContentShown && !GLTFast.MediaPlayer.ContentShown
                && (m_arPlaced || (GLTFast.AnchorPlacement.TapToPlace && GLTFast.AnchorPlacement.HasPlaceable && !GLTFast.AnchorPlacement.AnyAwaiting)))
            {
                GLTFast.MediaPlayer.ContentShown = true;
                AudioListener.volume = 1f;
                Debug.Log("App: content shown");
            }
            if (m_arPlaced && st == ARSessionState.SessionTracking) text = "Scene placed on the floor. Look around to find it.";
            if (st != m_arLastState) { m_arLastState = st; Debug.Log("App: AR session state " + st); }
            m_arState.text = text;
            m_arState.color = st == ARSessionState.Unsupported ? Ui.Warn : Ui.Dim;
        }

        private void UpdateStats()
        {
            var sb = new System.Text.StringBuilder();
            float dt = Time.unscaledDeltaTime;
            m_fps = Mathf.Lerp(m_fps, 1f / Mathf.Max(dt, 0.0001f), 0.3f);
            sb.AppendLine("<b>" + m_fps.ToString("0") + " fps</b>  " + (1000f / Mathf.Max(m_fps, 1f)).ToString("0.0") + " ms");
            var rend = m_viewer.GetComponentsInChildren<Renderer>();
            long tris = 0;
            foreach (var mf in m_viewer.GetComponentsInChildren<MeshFilter>()) if (mf.sharedMesh != null) tris += mf.sharedMesh.triangles.Length / 3;
            sb.AppendLine(Ui.Plural(rend.Length, "renderer", "renderers") + ", " + tris.ToString("N0") + " triangles");
            sb.AppendLine(Ui.Plural(FindObjectsOfType<Light>().Length, "light", "lights") + ", " + Ui.Plural(FindObjectsOfType<MediaPlayer>().Length, "media item", "media items"));
            sb.AppendLine("Memory " + (UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong() / 1048576) + " MB");
            sb.Append("Opened in " + m_loadSeconds.ToString("0.0") + " s");
            m_statsText.text = sb.ToString();
        }
        private float m_fps = 60f;
        private float m_arNextProbe;
        private bool m_arPlaced;
        private ARSessionState m_arLastState = (ARSessionState)(-1);
    }
}
