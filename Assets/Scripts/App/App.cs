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
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

namespace rt.xr.unity.app
{
    public class LogLine { public LogType Type; public string Text; }

    // The XR Scene Player interface: a scene library, a sheet before opening, an opening screen, the player controls, scene
    // details and settings. It replaces the stock list and buttons; the scene loading itself stays in SceneViewer.
    public partial class App : MonoBehaviour
    {
        public static App I { get; private set; }
        public const string ProductName = "XR Scene Player";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if (FindObjectOfType<SceneViewer>() == null) return;
            if (FindObjectOfType<App>() != null) return;
            new GameObject("XrApp").AddComponent<App>();
        }

        private SceneViewer m_viewer;
        private Canvas m_canvas;
        private CanvasScaler m_scaler;
        private RectTransform m_root;
        private CamRig m_rig;
        private SceneEntry m_current;
        private bool m_busy;
        private bool m_loadDone, m_loadFailed;
        private float m_openedAt;
        private float m_loadSeconds;
        private readonly List<LogLine> m_logs = new List<LogLine>();
        private ScreenOrientation m_lastOrientation;
        private Vector2Int m_lastScreen;
        private Rect m_lastSafe;

        public SceneViewer Viewer { get { return m_viewer; } }
        public SceneEntry Current { get { return m_current; } }
        public CamRig Rig { get { return m_rig; } }
        public RectTransform Root { get { return m_root; } }
        public IReadOnlyList<LogLine> Logs { get { return m_logs; } }

        // ------------------------------------------------------------------ lifecycle

        private void Awake()
        {
            I = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            Ui.Init();
            Application.targetFrameRate = 60;
            if (!rt.xr.unity.QuestRig.Active) Screen.orientation = ScreenOrientation.AutoRotation;
            Screen.autorotateToPortrait = Screen.autorotateToLandscapeLeft = Screen.autorotateToLandscapeRight = true;
            Screen.autorotateToPortraitUpsideDown = false;
            Screen.sleepTimeout = SleepTimeout.SystemSetting;

            m_viewer = FindObjectOfType<SceneViewer>();
            var legacy = FindObjectOfType<PlayerApplication>();
            if (legacy != null) legacy.enabled = false;      // the stock list and buttons are replaced by this interface
            // the stock EventSystem is a child of the stock canvas: take it out before the canvas is hidden
            var es = FindObjectOfType<UnityEngine.EventSystems.EventSystem>();
            if (es != null && es.transform.parent != null) es.transform.SetParent(null, false);
            foreach (var c in FindObjectsOfType<Canvas>()) if (c.renderMode == RenderMode.ScreenSpaceOverlay) c.gameObject.SetActive(false);
            m_viewer.showLog = false;
            m_viewer.onGlTFLoadComplete = () => { m_loadDone = true; };
            m_viewer.onGlTFLoadError = () => { m_loadFailed = true; };
            m_rig = gameObject.AddComponent<CamRig>();
            ResetCamera();

            Application.logMessageReceived += OnLog;
            BuildCanvas();
            BuildLibrary();
            BuildLab();
            BuildSettings();
            BuildPlayer();
            BuildTabs();
            BuildSheet();
            BuildDetails();
            BuildOpening();
            BuildDialogs();
            ShowLibrary();
            StartCoroutine(Catalog.Load(m_viewer.ConfigFileLocation));
            Catalog.Changed += OnCatalogChanged;
            StartCoroutine(AutoOpenRoutine());
        }

        // test aid: a file named autoopen.txt in the app's data folder names a scene (a part of its file name) to open by itself,
        // optionally followed by |flat to open it as a flat preview
        private IEnumerator AutoOpenRoutine()
        {
            string file = System.IO.Path.Combine(Application.persistentDataPath, "autoopen.txt");
            float t = 0f;
            while (t < 60f && (Catalog.Loading || Catalog.Entries.Count == 0 || Catalog.PendingPeeks > 0)) { t += Time.unscaledDeltaTime; yield return null; }
            if (!System.IO.File.Exists(file)) yield break;
            string[] parts = System.IO.File.ReadAllText(file).Trim().Split('|');
            System.IO.File.Delete(file);
            string flags = parts.Length > 1 ? string.Join("|", parts, 1, parts.Length - 1) : "";
            if (flags.Contains("landscape")) Screen.orientation = ScreenOrientation.LandscapeLeft;
            if (flags.Contains("portrait")) Screen.orientation = ScreenOrientation.Portrait;
            yield return new WaitForSecondsRealtime(0.5f);
            if (parts[0].Trim() == "@check") { RunAllScenes(); yield break; }
            if (parts[0].Trim() == "@lab") { ShowLab(); yield break; }
            if (parts[0].Trim() == "@settings") { ShowSettings(); yield break; }
            var e = Catalog.Entries.FirstOrDefault(x => x.FileName.ToLowerInvariant().Contains(parts[0].Trim().ToLowerInvariant()));
            if (e == null) { Debug.Log("autoopen: no scene matches " + parts[0]); yield break; }
            m_testSceneCam = flags.Contains("scenecam");
            rt.xr.unity.TimedObjectGate.Enabled = !flags.Contains("nogate");
            m_cleanShot = flags.Contains("clean");
            OpenScene(e, flags.Contains("flat"));
        }

        // test aid (flag scenecam): the interactivity test scenes assume the camera node of the scene from the first frame
        private bool m_testSceneCam;
        // test aid (flag clean): no controls and no statistics on the screen, for the thumbnails of the catalog
        private bool m_cleanShot;

        private void OnApplicationFocus(bool focus)
        {
            if (!focus || Catalog.Loading || Catalog.Entries.Count == 0) return;
            // scenes that could not be reached while the phone was asleep or off the network are tried again
            if (Catalog.Entries.Any(e => e.PeekFailed)) StartCoroutine(Catalog.Load(m_viewer.ConfigFileLocation));
        }

        private void OnDestroy()
        {
            Application.logMessageReceived -= OnLog;
            Catalog.Changed -= OnCatalogChanged;
        }

        private void OnLog(string text, string stack, LogType type)
        {
            if (m_logs.Count > 400) m_logs.RemoveRange(0, 100);
            m_logs.Add(new LogLine { Type = type, Text = text });
        }

        private void BuildCanvas()
        {
            var go = new GameObject("XrCanvas", typeof(RectTransform));
            go.transform.SetParent(transform, false);
            m_canvas = go.AddComponent<Canvas>();
            m_canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            m_canvas.sortingOrder = 50;
            m_scaler = go.AddComponent<CanvasScaler>();
            m_scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            go.AddComponent<GraphicRaycaster>();
            m_root = Ui.R("SafeArea", go.transform);
            Ui.Stretch(m_root);
            ApplyLayout(true);
        }

        // The short side of the screen is always 1080 interface units, in portrait and in landscape.
        private void ApplyLayout(bool force)
        {
            if (m_xr) return;
            var size = new Vector2Int(Screen.width, Screen.height);
            if (!force && size == m_lastScreen && Screen.safeArea == m_lastSafe) return;
            m_lastScreen = size; m_lastSafe = Screen.safeArea;
            bool portrait = Screen.height >= Screen.width;
            m_scaler.referenceResolution = portrait ? new Vector2(1080, 2400) : new Vector2(2400, 1080);
            m_scaler.matchWidthOrHeight = portrait ? 0f : 1f;
            var sa = Screen.safeArea;
            m_root.anchorMin = new Vector2(sa.xMin / Screen.width, sa.yMin / Screen.height);
            m_root.anchorMax = new Vector2(sa.xMax / Screen.width, sa.yMax / Screen.height);
            m_root.offsetMin = Vector2.zero; m_root.offsetMax = Vector2.zero;
            OnLayoutChanged();
        }

        public bool Landscape { get { return m_root != null && m_root.rect.width > 1f ? m_root.rect.width > m_root.rect.height : Screen.width > Screen.height; } }

        private void Update()
        {
            ApplyLayout(false);
            UpdateXr();
            UpdateLibrary();
            UpdatePlayer();
            UpdateLab();
            UpdatePlayground();
            UpdateToast();
            if (Input.GetKeyDown(KeyCode.Escape)) OnBack();
        }

        // ------------------------------------------------------------------ navigation

        private enum Screen_ { Library, Lab, Settings, Player, Playground }
        private Screen_ m_screen = Screen_.Library;

        private void OnBack()
        {
            if (DialogOpen()) { CloseDialogs(); return; }
            if (m_detailsOpen) { CloseDetails(); return; }
            if (m_screen == Screen_.Playground) { LeavePlayground(); return; }
            if (m_sheetOpen) { CloseSheet(); return; }
            if (m_openingOpen) return;
            if (m_screen == Screen_.Player) { LeavePlayer(); return; }
            if (m_screen == Screen_.Lab || m_screen == Screen_.Settings) { ShowLibrary(); return; }
            Application.Quit();
        }

        private void ShowLibrary()
        {
            m_screen = Screen_.Library;
            SetTabs();
            m_libToTop = m_libToTop || Catalog.Entries.Count == 0;
            RefreshLibrary();
            Ui.Show(m_libGroup, true); Ui.Show(m_labGroup, false); Ui.Show(m_setGroup, false); Ui.Show(m_playerGroup, false);
            Ui.Show(m_tabsGroup, true);
        }

        public void ShowLab()
        {
            m_screen = Screen_.Lab;
            SetTabs();
            RefreshLab();
            Ui.Show(m_libGroup, false); Ui.Show(m_labGroup, true); Ui.Show(m_setGroup, false); Ui.Show(m_playerGroup, false);
            Ui.Show(m_tabsGroup, true);
        }

        public void ShowSettings()
        {
            m_screen = Screen_.Settings;
            SetTabs();
            RefreshSettings();
            Ui.Show(m_libGroup, false); Ui.Show(m_labGroup, false); Ui.Show(m_setGroup, true); Ui.Show(m_playerGroup, false);
            Ui.Show(m_tabsGroup, true);
        }

        private void OnCatalogChanged()
        {
            if (m_screen == Screen_.Library) RefreshLibrary();
            if (m_screen == Screen_.Lab) RefreshLab();
        }

        // ------------------------------------------------------------------ opening a scene

        public void OpenScene(SceneEntry e, bool previewWithoutAR = false)
        {
            if (m_busy || e == null) return;
            m_busy = true;
            if (m_screen == Screen_.Lab) m_returnToLab = true;
            m_current = e;
            m_logs.Clear();
            m_loadDone = m_loadFailed = false;
            CloseSheet();
            StartCoroutine(OpenRoutine(e, previewWithoutAR));
        }

        private IEnumerator OpenRoutine(SceneEntry e, bool noAR)
        {
            ShowOpening(e);
            float t0 = Time.realtimeSinceStartup;
            m_openedAt = t0;
            SetStep(0, e.Peeked && !e.PeekFailed ? StepState.Ok : (e.PeekFailed ? StepState.Warn : StepState.Working), e.PeekFailed ? "Not read yet" : "");
            bool needMedia = e.Media.Count > 0;
            if (needMedia)
            {
                SetStep(1, StepState.Working, "");
                yield return Catalog.Preflight(e, null);
                int missing = e.Media.Count(m => m.Status == "missing");
                SetStep(1, missing == 0 ? StepState.Ok : StepState.Warn, missing == 0 ? Ui.Plural(e.Media.Count, "file", "files") : Ui.Plural(missing, "file not found", "files not found"));
            }
            else SetStep(1, StepState.Skipped, "none");
            SetStep(2, StepState.Working, "");
            yield return null;

            m_viewer.PreviewWithoutAR = noAR;
            GLTFast.AnchorPlacement.Clear();
            GLTFast.AnchorPlacement.TapToPlace = !noAR && e.UsesAnchor && PlayerPrefs.GetInt("set.taptoplace2", 1) == 1;
            GLTFast.AnchorPlacement.FaceUserOnPlacement = PlayerPrefs.GetInt("set.faceuser", 0) == 1;
            GLTFast.AnchorPlacement.AnchorToPlane = PlayerPrefs.GetInt("set.planeanchor", 0) == 1;
            GLTFast.AnchorPlacement.PlaceOnAnySurface = PlayerPrefs.GetInt("set.anysurface", 0) == 1;
            GLTFast.AnchorPlacement.GroundOnPlacement = PlayerPrefs.GetInt("set.groundplace", 0) == 1;
            rt.xr.unity.MediaPlayer.HoldAutoplay = PlayerPrefs.GetInt("set.holdplay", 1) == 1 && !m_testSceneCam;
            rt.xr.unity.MediaPlayer.ContentShown = false;
            rt.xr.unity.MediaPlayer.AwaitContentShown = !noAR && e.UsesAnchor && !m_testSceneCam;
            AudioListener.volume = rt.xr.unity.MediaPlayer.AwaitContentShown ? 0f : 1f;
            if (noAR && e.UsesAnchor) MakePreviewOrigin();
            if (!noAR && e.UsesAnchor) { SetStep(2, StepState.Working, "camera"); yield return EnsureCameraPermission(); }
            SceneImport.MediaOverrides = MediaSourceStore.For(e.Uri, e.Media.Count);
            m_viewer.LoadGltf(e.Uri);
            float guard = 0f;
            while (!m_loadDone && !m_loadFailed && guard < 180f) { guard += Time.unscaledDeltaTime; yield return null; }
            if (m_loadFailed || !m_loadDone)
            {
                string why = LoadFailureText(e);
                SetStep(2, StepState.Failed, "failed");
                yield return new WaitForSecondsRealtime(0.4f);
                HideOpening();
                UnloadScene();
                ShowLibrary();
                m_busy = false;
                m_lastFailure = why.Split('\n')[0];
                if (!m_batch) ShowError("The scene did not open", why, () => OpenScene(e, noAR));
                yield break;
            }
            m_loadSeconds = Time.realtimeSinceStartup - t0;
            SetStep(2, StepState.Ok, m_loadSeconds.ToString("0.0") + " s");
            PostLoad(e);
            float shown = Time.realtimeSinceStartup - t0;
            if (shown < 0.9f) yield return new WaitForSecondsRealtime(0.9f - shown);
            HideOpening();
            EnterPlayer();
            m_busy = false;
        }

        private string LoadFailureText(SceneEntry e)
        {
            var sb = new StringBuilder();
            foreach (var l in m_logs)
            {
                if (l.Type == LogType.Error || l.Type == LogType.Exception)
                {
                    string t = l.Text.Split('\n')[0];
                    if (t.StartsWith("Loading glTF failed")) continue;
                    if (sb.ToString().Contains(t)) continue;
                    sb.AppendLine(t);
                }
            }
            if (sb.Length == 0) sb.AppendLine("The loader did not say why.");
            sb.AppendLine(e.Uri);
            return sb.ToString().TrimEnd();
        }

        // With an AR session the camera that shows the camera picture is the one of the XR Origin. The scene also has a camera tagged
        // MainCamera, and drawing that one over it hides the picture, so every other camera is switched off.
        // diagnostics: what the AR session reports about planes, for the log and the scene check
        private IEnumerator WatchPlanes()
        {
            UnityEngine.XR.ARFoundation.ARPlaneManager pm = null;
            float t = 0f;
            while (pm == null && t < 20f) { pm = FindObjectOfType<UnityEngine.XR.ARFoundation.ARPlaneManager>(); t += Time.unscaledDeltaTime; yield return null; }
            if (pm == null) { Debug.Log("App: no plane manager in the AR session"); yield break; }
            if (GLTFast.AnchorPlacement.TapToPlace) EnsureRaycastManager();
            Debug.Log("App: plane manager found, classification supported=" + (pm.descriptor != null && pm.descriptor.supportsClassification) + " mode=" + pm.requestedDetectionMode);
            pm.planesChanged += a =>
            {
                foreach (var p in a.added) Debug.Log("App: plane added " + p.trackableId + " " + p.alignment + " " + p.classification + " size " + p.size.x.ToString("0.00") + "x" + p.size.y.ToString("0.00"));
                foreach (var p in a.updated) if (p.classification != UnityEngine.XR.ARSubsystems.PlaneClassification.None && !m_planeLogged.Contains(p.trackableId.ToString() + p.classification)) { m_planeLogged.Add(p.trackableId.ToString() + p.classification); Debug.Log("App: plane updated " + p.trackableId + " " + p.alignment + " " + p.classification); }
            };
        }
        private readonly HashSet<string> m_planeLogged = new HashSet<string>();

        public Camera UseArCamera()
        {
            Camera ar = null;
            var origin = FindObjectOfType<Unity.XR.CoreUtils.XROrigin>();
            if (origin != null) ar = origin.Camera;
            if (ar == null) foreach (var c in FindObjectsOfType<Camera>()) if (c.GetComponent<UnityEngine.XR.ARFoundation.ARCameraBackground>() != null) { ar = c; break; }
            if (ar == null) return null;
            foreach (var c in FindObjectsOfType<Camera>()) if (c != ar) { c.enabled = false; if (c.CompareTag("MainCamera")) c.tag = "Untagged"; }
            ar.enabled = true; ar.tag = "MainCamera";
            Debug.Log("App: AR camera " + ar.name + " clear=" + ar.clearFlags + " depth=" + ar.depth);
            return ar;
        }

        private void PostLoad(SceneEntry e)
        {
            Debug.Log("App: opened " + e.FileName + " ar=" + m_viewer.ARCameraEnabled + " usesAnchors=" + m_viewer.SceneUsesAnchors + " preview=" + m_viewer.PreviewWithoutAR + " cameras=" + FindObjectsOfType<Camera>().Length);
            if (m_viewer.ARCameraEnabled)
            {
                UseArCamera();
                StartCoroutine(WatchPlanes());
                m_rig.Detach();
                EnsureLight();
                Stamp(e);
                return;
            }
            var cam = m_viewer.GetMainCamera();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.07f, 0.07f, 0.08f, 1f);
            cam.cullingMask = ~0;
            if (rt.xr.unity.QuestRig.Active)
            {
                m_rig.Detach();
                PlaceSceneForXr();
                EnsureLight();
                Stamp(e);
                return;
            }
            if (m_viewer.ARCameraEnabled) { m_rig.Detach(); }
            else
            {
                Bounds b = rt.xr.Utils.ComputeBounds(m_viewer.gameObject);
                Camera sceneCam = null;
                foreach (var c in m_viewer.GetComponentsInChildren<Camera>(true)) if (c != cam) { sceneCam = c; break; }
                m_rig.Attach(cam, b, sceneCam);
                if (PlayerPrefs.GetInt("set.walk", 0) == 1) m_rig.SetMode(CamRig.Mode.Walk);
                if (m_testSceneCam && sceneCam != null) m_rig.SetMode(CamRig.Mode.Scene);
            }
            if (m_viewer.PreviewWithoutAR && m_viewer.SceneUsesAnchors) StopAnchorParts();
            EnsureLight();
            Stamp(e);
        }

        // a flat preview has no AR session: the anchor parts would only report errors every frame
        private void StopAnchorParts()
        {
            foreach (var mb in m_viewer.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (mb == null) continue;
                string n = mb.GetType().Name;
                if (n.StartsWith("Trackable") || n == "AnchorInstance") mb.enabled = false;
            }
            // the anchor parts switch the nodes they hold off until something is anchored: a preview shows them where the scene puts them
            foreach (var t in m_viewer.GetComponentsInChildren<Transform>(true))
                if (!t.gameObject.activeSelf && t.GetComponent<Camera>() == null && t.GetComponent<Light>() == null) t.gameObject.SetActive(true);
        }

        private void EnsureLight()
        {
            bool any = false;
            foreach (var l in FindObjectsOfType<Light>()) if (l.enabled && l.gameObject.name != "FillLight") { any = true; break; }
            var old = GameObject.Find("FillLight");
            if (old != null) Destroy(old);
            m_filled = false;
            if (any || PlayerPrefs.GetInt("set.filllight", 1) == 0) return;
            var go = new GameObject("FillLight");
            go.transform.rotation = Quaternion.Euler(48f, -35f, 0f);
            var light = go.AddComponent<Light>();
            light.type = LightType.Directional; light.intensity = 1.1f; light.shadows = LightShadows.None;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.45f, 0.45f, 0.47f);
            ApplyStudioReflections();
            m_filled = true;
        }
        private bool m_filled;
        private static Cubemap s_studio;

        // metallic materials need something to reflect: a plain studio gradient, only when the scene brings no light of its own
        private static void ApplyStudioReflections()
        {
            if (s_studio == null)
            {
                const int n = 32;
                s_studio = new Cubemap(n, TextureFormat.RGBA32, false);
                for (int f = 0; f < 6; f++)
                {
                    var px = new Color[n * n];
                    for (int y = 0; y < n; y++)
                        for (int x = 0; x < n; x++)
                        {
                            float t = 1f - y / (float)(n - 1);       // 1 at the top of a side face
                            float v = f == (int)CubemapFace.PositiveY ? 0.95f : (f == (int)CubemapFace.NegativeY ? 0.12f : Mathf.Lerp(0.14f, 0.9f, Mathf.Pow(t, 1.4f)));
                            px[y * n + x] = new Color(v, v, v * 1.02f, 1f);
                        }
                    s_studio.SetPixels(px, (CubemapFace)f);
                }
                s_studio.Apply();
            }
            RenderSettings.defaultReflectionMode = UnityEngine.Rendering.DefaultReflectionMode.Custom;
            RenderSettings.customReflection = s_studio;
            RenderSettings.reflectionIntensity = 1f;
        }
        public bool SceneHadNoLights { get { return m_filled; } }

        // AR needs the camera: ask for it before the AR session starts and wait for the answer
        public IEnumerator EnsureCameraPermission()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Camera)) yield break;
            UnityEngine.Android.Permission.RequestUserPermission(UnityEngine.Android.Permission.Camera);
            float t = 0f;
            yield return new WaitForSecondsRealtime(0.5f);
            while (t < 30f && !UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Camera)) { t += Time.unscaledDeltaTime; yield return null; }
#else
            yield break;
#endif
        }

        // The anchor code looks for an XR Origin even when no AR session runs. A flat preview gives it a plain one.
        private GameObject m_previewOrigin;
        private void MakePreviewOrigin()
        {
            DestroyPreviewOrigin();
            try
            {
                m_previewOrigin = new GameObject("PreviewXROrigin");
                var xo = m_previewOrigin.AddComponent<Unity.XR.CoreUtils.XROrigin>();
                var offset = new GameObject("Camera Offset");
                offset.transform.SetParent(m_previewOrigin.transform, false);
                xo.CameraFloorOffsetObject = offset;
                xo.Camera = m_viewer.GetMainCamera();
            }
            catch (Exception ex) { Debug.LogWarning("Preview origin: " + ex.Message); }
        }
        private void DestroyPreviewOrigin() { if (m_previewOrigin != null) Destroy(m_previewOrigin); m_previewOrigin = null; }

        public void UnloadScene()
        {
            if (m_reticle != null) { Destroy(m_reticle); m_reticle = null; }
            GLTFast.AnchorPlacement.Clear();
            DestroyPreviewOrigin();
            try { m_viewer.UnloadGltfScene(); } catch (Exception ex) { Debug.LogWarning("Unload: " + ex.Message); }
            m_rig.Detach();
            LensOff();
            var old = GameObject.Find("FillLight");
            if (old != null) Destroy(old);
            ResetCamera();
        }

        private void ResetCamera()
        {
            var cam = m_viewer.GetMainCamera();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Ui.Bg;
            cam.cullingMask = ~0;
        }

        public void LeavePlayer()
        {
            if (m_busy) return;
            Ui.Show(m_playerGroup, false);
            LeavePlayerCleanup();
            UnloadScene();
            if (m_returnToLab) { m_returnToLab = false; ShowLab(); } else ShowLibrary();
        }
        private bool m_returnToLab;
        public void ReturnToLabOnLeave() { m_returnToLab = true; }

        // ------------------------------------------------------------------ small helpers

        public static string Humanize(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return "";
            string s = Regex.Replace(raw, "^(TRIGGER|ACTION|TRACKABLE)_", "");
            s = s.Replace('_', ' ').ToLowerInvariant();
            return char.ToUpper(s[0]) + s.Substring(1);
        }

        public GLTFast.Schema.Media[] SafeMedias()
        {
            try { return m_viewer.CurrentImport != null ? m_viewer.CurrentImport.GetMedias() : null; } catch { return null; }
        }

        public string Diagnostics()
        {
            var sb = new StringBuilder();
            sb.AppendLine(ProductName + " " + Application.version + " (Unity " + Application.unityVersion + ")");
            sb.AppendLine("Device: " + SystemInfo.deviceModel + ", " + SystemInfo.operatingSystem);
            if (m_current != null)
            {
                sb.AppendLine("Scene: " + m_current.Title + " (" + m_current.Uri + ")");
                sb.AppendLine("Extensions: " + (m_current.Extensions.Length == 0 ? "none listed" : string.Join(", ", m_current.Extensions)));
                foreach (var m in m_current.Media) sb.AppendLine("Media: " + m.Uri + " [" + m.Status + "]");
                sb.AppendLine("Opened in " + m_loadSeconds.ToString("0.0") + " s");
            }
            sb.AppendLine("Log:");
            foreach (var l in m_logs.Where(x => x.Type != LogType.Log).Take(60)) sb.AppendLine("  [" + l.Type + "] " + l.Text.Split('\n')[0]);
            return sb.ToString();
        }
    }
}
