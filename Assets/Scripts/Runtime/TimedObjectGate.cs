// ISO/IEC 23090-14:2024(E), clause 4.3: "An object that has timed media components, shall not be rendered until the indicated T0 + Tinit
// of these components. T0 + Tinit of all the timed media components of the same object shall be equal."
// An object is a renderer whose material uses the texture of a video (MPEG_texture_video). It is not rendered until every media it shows has
// started and presented its first frame. The static image of the texture (the fallback of 5.3.1.1) is for a player that does not support the
// extension: the NOTE of 5.3.1.1 allows it before startTime, but the normative text of 4.3 does not. When a media cannot start (pipeline
// error) the object is rendered with its static image, so that it is not left empty.

using System.Collections.Generic;
using UnityEngine;
using MediaPlayer = GLTFast.MediaPlayer;

namespace rt.xr.unity
{
    public class TimedObjectGate : MonoBehaviour
    {
        class Gated
        {
            public Renderer Renderer;
            public List<MediaPlayer> Media = new List<MediaPlayer>();
            public bool Shown;
            public bool Warned;
        }

        SceneViewer viewer;
        readonly List<Gated> gated = new List<Gated>();
        float nextScan;
        float scanUntil;
        int scannedPlayers = -1;

        public static bool Enabled = true;
        /// <summary>Seconds of media time after which a started media without picture no longer hides its object.</summary>
        public static double NoPictureTimeout = 5.0;
        /// <summary>Number of renderers waiting for their media (test aid).</summary>
        public int Waiting { get { int n = 0; foreach (var g in gated) if (!g.Shown) n++; return n; } }
        public int Count { get { return gated.Count; } }
        /// <summary>The renderers that show a video texture.</summary>
        public System.Collections.Generic.List<Renderer> VideoRenderers() { var l = new System.Collections.Generic.List<Renderer>(); foreach (var g in gated) if (g.Renderer != null) l.Add(g.Renderer); return l; }

        public void Init(SceneViewer sceneViewer)
        {
            viewer = sceneViewer;
            foreach (var g in gated) if (g.Renderer != null) g.Renderer.forceRenderingOff = false;
            gated.Clear();
            scannedPlayers = -1;
            nextScan = 0f;
            scanUntil = Time.unscaledTime + 15f;
        }

        void Update()
        {
            if (!Enabled || viewer == null) return;
            var players = viewer.MediaPlayers;
            if (players == null) return;
            if (Time.unscaledTime >= nextScan && Time.unscaledTime < scanUntil)
            {
                nextScan = Time.unscaledTime + 0.25f;
                Scan(players);
            }
            foreach (var g in gated)
            {
                if (g.Shown || g.Renderer == null) continue;
                // media that wait for the user's play are not started, so their object shows its static image meanwhile
                if (MediaPlayer.HoldAutoplay && (!MediaPlayer.AwaitContentShown || MediaPlayer.ContentShown))
                {
                    g.Renderer.forceRenderingOff = false;
                    g.Shown = true;
                    continue;
                }
                bool ready = true;
                foreach (var mp in g.Media)
                {
                    if (mp == null) continue;
                    if (mp.Error) continue;                      // cannot start: the static image is rendered
                    if (!mp.FirstFramePresented)
                    {
                        // a media that is running but delivers no picture does not keep the object empty for ever (implementation limit, no clause)
                        if (mp.Started && mp.CurrentTime > NoPictureTimeout) { if (!g.Warned) { g.Warned = true; Debug.LogWarning("Clause 4.3: the media of " + g.Renderer.name + " delivers no picture " + NoPictureTimeout + " s after its start, the static image is rendered"); } continue; }
                        ready = false; break;
                    }
                }
                if (ready && MediaPlayer.AwaitContentShown && !MediaPlayer.ContentShown) ready = false;
                if (ready)
                {
                    g.Renderer.forceRenderingOff = false;
                    g.Shown = true;
                }
            }
        }

        void Scan(IReadOnlyList<MediaPlayer> players)
        {
            // textures of each media
            var textureOwner = new Dictionary<Texture, List<MediaPlayer>>();
            foreach (var mp in players)
            {
                if (mp == null) continue;
                foreach (var tex in mp.VideoTargetTextures)
                {
                    List<MediaPlayer> list;
                    if (!textureOwner.TryGetValue(tex, out list)) { list = new List<MediaPlayer>(); textureOwner[tex] = list; }
                    if (!list.Contains(mp)) list.Add(mp);
                }
            }
            if (textureOwner.Count == 0 || textureOwner.Count == scannedPlayers) return;
            scannedPlayers = textureOwner.Count;
            foreach (var r in GetComponentsInChildren<Renderer>(true))
            {
                if (r == null || gated.Exists(x => x.Renderer == r)) continue;
                var media = new List<MediaPlayer>();
                foreach (var m in r.sharedMaterials)
                {
                    if (m == null) continue;
                    foreach (var name in new[] { "_MainTex", "_BaseMap", "baseColorTexture", "_BaseColorTexture" })
                    {
                        if (!m.HasProperty(name)) continue;
                        var tex = m.GetTexture(name);
                        List<MediaPlayer> owners;
                        if (tex != null && textureOwner.TryGetValue(tex, out owners)) foreach (var o in owners) if (!media.Contains(o)) media.Add(o);
                    }
                }
                if (media.Count == 0) continue;
                // T0 + Tinit of all the timed media components of one object shall be equal
                double first = media[0].ScheduledStartTime;
                foreach (var o in media)
                {
                    if (System.Math.Abs(o.ScheduledStartTime - first) > 1e-6)
                    {
                        Debug.LogWarning("Clause 4.3: the timed media of the object " + r.name + " do not have the same start time (T0 + Tinit shall be equal)");
                        break;
                    }
                }
                var gate = new Gated { Renderer = r, Media = media };
                r.forceRenderingOff = true;
                gated.Add(gate);
            }
        }

        void OnDestroy()
        {
            foreach (var g in gated) if (g.Renderer != null) g.Renderer.forceRenderingOff = false;
        }
    }
}
