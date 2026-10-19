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
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Networking;

namespace rt.xr.unity.app
{
    [Serializable] public class MetaJson { public string name; public string summary; public string[] tags; public string screenshot; public string path; }
    [Serializable] public class PeekAlt { public string uri; public string mimeType; }
    [Serializable] public class PeekMediaItem { public string name; public PeekAlt[] alternatives; }
    [Serializable] public class PeekMedia { public PeekMediaItem[] media; }
    [Serializable] public class PeekTrackable { public int type; }
    [Serializable] public class PeekAnchor { public PeekTrackable[] trackables; }
    [Serializable] public class PeekExt { public PeekMedia MPEG_media; public PeekAnchor MPEG_anchor; }
    [Serializable] public class GltfPeek { public string[] extensionsUsed; public PeekExt extensions; }

    public class MediaRef
    {
        public string Name;
        public string Uri;          // resolved against the scene file
        public string Mime;
        public string Status = "unknown";   // found, missing, unknown
    }

    public class SceneEntry
    {
        public string Uri;
        public string FileName;
        public string Title;
        public string Summary = "";
        public string Variant = "";
        public string[] Tags = new string[0];
        public string ScreenshotUri;
        public Texture2D Thumb;
        public string[] Extensions = new string[0];
        public List<MediaRef> Media = new List<MediaRef>();
        public List<int> TrackableTypes = new List<int>();     // the trackable types the scene's MPEG_anchor lists (0 floor, 1 viewer, 2 controller, 3 geometry, 4 marker 2D, 5 marker 3D, 6 marker geo, 7 application)
        public List<string> Names = new List<string>();     // TRIGGER_*, ACTION_*, TRACKABLE_* named in the content's own summary
        public bool HasMetadata, Peeked, PeekFailed;
        public string PeekError;

        public bool Has(string ext) { return Array.IndexOf(Extensions, ext) >= 0; }
        public bool UsesAnchor { get { return Has("MPEG_anchor"); } }
        public bool UsesInteractivity { get { return Has("MPEG_scene_interactivity") || Has("MPEG_node_interactivity"); } }
        public bool UsesMedia { get { return Has("MPEG_media") || Media.Count > 0; } }
        public bool UsesHaptics { get { return Has("MPEG_haptic"); } }
        public bool IsRemote { get { return Uri.StartsWith("http", StringComparison.OrdinalIgnoreCase); } }
        public string Source
        {
            get
            {
                if (!IsRemote) return "On this device";
                try { return new Uri(Uri).Authority; } catch { return "Server"; }
            }
        }
    }

    // The list of scenes. It starts from the Paths file (one scene URI per line, as before), reads the metadata the content
    // ships next to each scene (metadata/<name>.json, version 2: name, summary, tags, screenshot) and peeks into each scene
    // file for the extensions it lists and the media it asks for.
    public static class Catalog
    {
        public static readonly List<SceneEntry> Entries = new List<SceneEntry>();
        public static string PathsFile = "";
        public static string Problem = "";
        public static bool Loading;
        public static event Action Changed;
        public static int PendingPeeks;

        public static string SourcesFile { get { return Path.Combine(Application.persistentDataPath, "sources.txt"); } }

        public static string LocatePaths(string configured)
        {
            return string.IsNullOrEmpty(configured) ? Path.Combine(Application.persistentDataPath, "Paths") : configured;
        }

        public static IEnumerator Load(string configuredPaths)
        {
            Loading = true;
            Problem = "";
            Entries.Clear();
            PathsFile = LocatePaths(configuredPaths);
            var lines = new List<string>();
            var roots = new List<string>();
            if (File.Exists(PathsFile)) { lines.AddRange(File.ReadAllLines(PathsFile)); roots.Add(PathsFile); }
            if (File.Exists(SourcesFile))
                foreach (var s in File.ReadAllLines(SourcesFile)) if (!string.IsNullOrWhiteSpace(s)) lines.Add(s.Trim());
            if (lines.Count == 0) Problem = "No list of scenes found at " + PathsFile;

            string baseDir = null;
            try { baseDir = new Uri(PathsFile).AbsoluteUri; baseDir = baseDir.Substring(0, baseDir.LastIndexOf('/') + 1); } catch { }

            var seen = new HashSet<string>();
            foreach (var raw in lines)
            {
                string line = raw.Trim();
                if (line.Length == 0 || line.StartsWith("#")) continue;
                // a source that is itself a list of scenes
                if (line.StartsWith("http", StringComparison.OrdinalIgnoreCase) && !line.EndsWith(".gltf", StringComparison.OrdinalIgnoreCase) && !line.EndsWith(".glb", StringComparison.OrdinalIgnoreCase))
                {
                    using (var req = UnityWebRequest.Get(line))
                    {
                        req.timeout = 8;
                        yield return req.SendWebRequest();
                        if (req.result != UnityWebRequest.Result.Success) { Problem = "Could not read the list at " + line + " (" + req.error + ")"; continue; }
                        string listBase = line.Substring(0, line.LastIndexOf('/') + 1);
                        foreach (var l2 in req.downloadHandler.text.Split('\n'))
                        {
                            string x = l2.Trim().TrimEnd('\r');
                            if (x.Length == 0 || x.StartsWith("#")) continue;
                            AddEntry(x, listBase, seen);
                        }
                    }
                    continue;
                }
                AddEntry(line, baseDir, seen);
            }
            Loading = false;
            if (Changed != null) Changed();
            foreach (var e in Entries.ToArray()) PendingPeeks++;
            foreach (var e in Entries.ToArray())
                Runner.Start(Limited(e));
        }

        // At most a few scenes are read at once, so a long list does not flood the network or the phone.
        private const int MaxConcurrentPeeks = 6;
        private static int s_activePeeks;
        private static float s_lastChanged;
        private static IEnumerator Limited(SceneEntry e)
        {
            while (s_activePeeks >= MaxConcurrentPeeks) yield return null;
            s_activePeeks++;
            yield return Enrich(e);
            s_activePeeks--;
            // while many scenes are pending, tell the library at most every 1.5 s; at the end always
            if (PendingPeeks <= 0 || Time.unscaledTime - s_lastChanged > 1.5f)
            {
                s_lastChanged = Time.unscaledTime;
                if (Changed != null) Changed();
            }
        }

        private static void AddEntry(string line, string baseDir, HashSet<string> seen)
        {
            try
            {
                Uri uri;
                if (!Uri.TryCreate(line, UriKind.Absolute, out uri))
                    uri = new Uri(new Uri(baseDir ?? "file:///"), line);
                if (!seen.Add(uri.AbsoluteUri)) return;
                string file = Uri.UnescapeDataString(uri.Segments[uri.Segments.Length - 1]);
                Entries.Add(new SceneEntry { Uri = uri.AbsoluteUri, FileName = file, Title = Prettify(file) });
            }
            catch (Exception e) { Debug.LogWarning("Catalog: skipped '" + line + "': " + e.Message); }
        }

        public static string Prettify(string file)
        {
            string n = Path.GetFileNameWithoutExtension(file).Replace('_', ' ').Replace('-', ' ');
            return n.Length > 0 ? char.ToUpper(n[0]) + n.Substring(1) : file;
        }

        public static IEnumerator Fetch(string uri, Action<string, byte[]> done)
        {
            // local files are read directly; the app's own folder is always readable that way
            if (uri.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
            {
                string p = Uri.UnescapeDataString(new Uri(uri).LocalPath);
                if (File.Exists(p))
                {
                    byte[] b = null; string err = null;
                    try { b = File.ReadAllBytes(p); } catch (Exception e) { err = e.Message; }
                    done(err, b);
                    yield break;
                }
            }
            using (var req = UnityWebRequest.Get(uri))
            {
                req.timeout = 10;
                yield return req.SendWebRequest();
                if (req.result != UnityWebRequest.Result.Success) done(req.error + (req.responseCode > 0 ? " (HTTP " + req.responseCode + ")" : ""), null);
                else done(null, req.downloadHandler.data);
            }
        }

        private static IEnumerator Enrich(SceneEntry e)
        {
            Uri scene = new Uri(e.Uri);
            string dir = e.Uri.Substring(0, e.Uri.LastIndexOf('/') + 1);
            string stem = Path.GetFileNameWithoutExtension(e.FileName);

            // metadata: metadata/<stem>.json, else metadata/scene.json
            MetaJson meta = null;
            foreach (var cand in new[] { dir + "metadata/" + Uri.EscapeDataString(stem) + ".json", dir + "metadata/scene.json" })
            {
                string text = null;
                yield return Fetch(cand, (err, data) => { if (err == null && data != null) text = System.Text.Encoding.UTF8.GetString(data); });
                if (text == null) continue;
                try { meta = JsonUtility.FromJson<MetaJson>(text); } catch { meta = null; }
                if (meta != null && !string.IsNullOrEmpty(meta.name)) { e.ScreenshotUri = string.IsNullOrEmpty(meta.screenshot) ? null : dir + meta.screenshot; break; }
                meta = null;
            }
            // the metadata of a file named after another scene (such as scene.json beside scene_extra_camera.gltf) describes
            // the scene it belongs to; the name is kept, the summary is only used when the file is that scene
            if (meta != null)
            {
                e.HasMetadata = true;
                bool sameScene = !string.IsNullOrEmpty(meta.path) && string.Equals(meta.path, e.FileName, StringComparison.OrdinalIgnoreCase);
                bool isStem = string.Equals(meta.name, stem, StringComparison.OrdinalIgnoreCase);
                e.Title = isStem ? Prettify(meta.name) : meta.name;
                if (!sameScene && !isStem) e.Variant = Prettify(stem);
                if (e.Title.Equals("Scene", StringComparison.OrdinalIgnoreCase))
                {
                    string[] parts = dir.TrimEnd('/').Split('/');
                    e.Title = Prettify(Uri.UnescapeDataString(parts[parts.Length - 1]));
                }
                if (!string.IsNullOrEmpty(meta.summary)) e.Summary = meta.summary.Trim();
                if (meta.tags != null) e.Tags = meta.tags;
                foreach (Match m in Regex.Matches(e.Summary, "(TRIGGER|ACTION|TRACKABLE)_[A-Z0-9_]+")) if (!e.Names.Contains(m.Value)) e.Names.Add(m.Value);
            }

            // the scene file: extensions and media
            string json = null;
            yield return Fetch(e.Uri, (err, data) =>
            {
                if (err != null) e.PeekError = err;
                else if (data != null && data.Length > 4 && !(data[0] == 'g' && data[1] == 'l' && data[2] == 'T' && data[3] == 'F'))
                    json = System.Text.Encoding.UTF8.GetString(data);
                else e.Peeked = true;       // binary glTF: not read here
            });
            if (json != null)
            {
                try
                {
                    var peek = JsonUtility.FromJson<GltfPeek>(json);
                    if (peek != null)
                    {
                        if (peek.extensionsUsed != null) e.Extensions = peek.extensionsUsed;
                        if (peek.extensions != null && peek.extensions.MPEG_anchor != null && peek.extensions.MPEG_anchor.trackables != null)
                        {
                            string[] tn = { "TRACKABLE_FLOOR", "TRACKABLE_VIEWER", "TRACKABLE_CONTROLLER", "TRACKABLE_GEOMETRY", "TRACKABLE_MARKER_2D", "TRACKABLE_MARKER_3D", "TRACKABLE_MARKER_GEO", "TRACKABLE_APPLICATION" };
                            foreach (var t in peek.extensions.MPEG_anchor.trackables)
                            {
                                if (!e.TrackableTypes.Contains(t.type)) e.TrackableTypes.Add(t.type);
                                if (t.type >= 0 && t.type < tn.Length && !e.Names.Contains(tn[t.type])) e.Names.Add(tn[t.type]);
                            }
                        }
                        if (peek.extensions != null && peek.extensions.MPEG_media != null && peek.extensions.MPEG_media.media != null)
                            foreach (var m in peek.extensions.MPEG_media.media)
                            {
                                if (m.alternatives == null) continue;
                                foreach (var a in m.alternatives)
                                {
                                    if (string.IsNullOrEmpty(a.uri)) continue;
                                    string resolved;
                                    Uri abs;
                                    if (Uri.TryCreate(a.uri, UriKind.Absolute, out abs) && a.uri.Contains("://")) resolved = abs.AbsoluteUri;
                                    else resolved = new Uri(scene, a.uri).AbsoluteUri;
                                    e.Media.Add(new MediaRef { Name = m.name, Uri = resolved, Mime = a.mimeType });
                                    break;      // the first alternative is the one the player uses
                                }
                            }
                    }
                    e.Peeked = true;
                }
                catch (Exception ex) { e.PeekFailed = true; e.PeekError = ex.Message; }
            }
            else if (e.PeekError != null) e.PeekFailed = true;

            // screenshot
            if (!string.IsNullOrEmpty(e.ScreenshotUri))
            {
                byte[] bytes = null;
                yield return Fetch(e.ScreenshotUri, (err, data) => { if (err == null) bytes = data; });
                if (bytes != null)
                {
                    var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    if (tex.LoadImage(bytes)) { tex.wrapMode = TextureWrapMode.Clamp; e.Thumb = tex; }
                }
            }
            PendingPeeks--;
        }

        // Checks that the media a scene asks for can be reached, so the player can say so before opening it.
        public static IEnumerator Preflight(SceneEntry e, Action done)
        {
            foreach (var m in e.Media)
            {
                bool found = false;
                if (m.Uri.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
                {
                    try { found = File.Exists(Uri.UnescapeDataString(new Uri(m.Uri).LocalPath)); } catch { }
                }
                else
                {
                    using (var req = UnityWebRequest.Head(m.Uri))
                    {
                        req.timeout = 6;
                        yield return req.SendWebRequest();
                        found = req.result == UnityWebRequest.Result.Success;
                    }
                }
                m.Status = found ? "found" : "missing";
            }
            if (done != null) done();
        }

        public static void AddSource(string line)
        {
            File.AppendAllText(SourcesFile, line.Trim() + "\n");
        }

        public static void ClearSources()
        {
            if (File.Exists(SourcesFile)) File.Delete(SourcesFile);
        }

        public static void NotifyChanged() { if (Changed != null) Changed(); }
    }

    // Runs coroutines that must outlive a screen.
    public class Runner : MonoBehaviour
    {
        private static Runner s_instance;
        public static Runner Instance
        {
            get
            {
                if (s_instance == null)
                {
                    var go = new GameObject("AppRunner");
                    DontDestroyOnLoad(go);
                    s_instance = go.AddComponent<Runner>();
                }
                return s_instance;
            }
        }
        public static Coroutine Start(IEnumerator routine) { return Instance.StartCoroutine(routine); }
    }
}


namespace rt.xr.unity.app
{
    // Application feature, not part of ISO/IEC 23090-14: a media address the user gave for a scene replaces the one in the scene file
    // when the scene is opened. The scene file is not changed. Stored per scene address and media index.
    public static class MediaSourceStore
    {
        private static string Key(string scene, int i) { return "media|" + scene + "|" + i; }
        public static string Get(string scene, int i) { return UnityEngine.PlayerPrefs.GetString(Key(scene, i), ""); }
        public static void Set(string scene, int i, string address)
        {
            if (string.IsNullOrWhiteSpace(address)) UnityEngine.PlayerPrefs.DeleteKey(Key(scene, i));
            else UnityEngine.PlayerPrefs.SetString(Key(scene, i), address.Trim());
            UnityEngine.PlayerPrefs.Save();
        }
        public static System.Collections.Generic.Dictionary<int, string> For(string scene, int count)
        {
            var d = new System.Collections.Generic.Dictionary<int, string>();
            for (int i = 0; i < count; i++) { var v = Get(scene, i); if (v.Length > 0) d[i] = v; }
            return d;
        }
    }
}
