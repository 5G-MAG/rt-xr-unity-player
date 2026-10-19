using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace rt.xr.unity.app
{
    // The extension reference: every extension of ISO/IEC 23090-14 with what this build does with it and the scenes in the library that list it.
    public partial class App
    {
        private enum ExtStatus { Tested, InCode, Partly, NotSupported }

        private class ExtRef
        {
            public string Name; public ExtStatus Status; public string Note; public string Try;
        }

        private class TrackRef
        {
            public int Type; public string Name; public ExtStatus Status; public string Note;
        }

        // Status words: Tested = seen working on the test phone; In code = implemented, not yet run on a device;
        // Partly = some cases work and some do not; Not supported = no implementation, or the data it needs is missing.
        private static readonly ExtRef[] ExtRefs =
        {
            new ExtRef { Name = "MPEG_media", Status = ExtStatus.Tested, Note = "Local files, HTTP, HLS and DASH sources play with sound.", Try = "Open a scene with a video and press Play." },
            new ExtRef { Name = "MPEG_buffer_circular", Status = ExtStatus.Tested, Note = "Carries the decoded frames to the texture for the media above.", Try = "Open the media pipeline view in the lab tools." },
            new ExtRef { Name = "MPEG_texture_video", Status = ExtStatus.Tested, Note = "A video shown on a surface of the scene.", Try = "Open a TV or video plane scene and press Play." },
            new ExtRef { Name = "MPEG_accessor_timed", Status = ExtStatus.Tested, Note = "Headers sized as maf writes them; timed data of animation, scene updates, vertex correspondence and pose arrive on the media clock.", Try = "" },
            new ExtRef { Name = "MPEG_sampler_YCbCr", Status = ExtStatus.NotSupported, Note = "Needs the Vulkan format table, which is not available here.", Try = "" },
            new ExtRef { Name = "MPEG_audio_spatial", Status = ExtStatus.Partly, Note = "Object sources, the Annex D distance gain and reverb units (RT60, DSR, pre-delay) work; HOA stays silent because no clause gives its decoding to the output.", Try = "Switch spatial audio off in the lab tools and compare." },
            new ExtRef { Name = "MPEG_anchor", Status = ExtStatus.Partly, Note = "Floor and plane (wall) work; see the trackable types below.", Try = "Open an AR scene, tap to place, tap the circle to fix." },
            new ExtRef { Name = "MPEG_scene_interactivity", Status = ExtStatus.Tested, Note = "29 test scenes checked on the phone; user input and manipulate need a controller.", Try = "Open the behaviour monitor in the lab tools." },
            new ExtRef { Name = "MPEG_node_interactivity", Status = ExtStatus.Tested, Note = "Node-level proximity weights, visibility and physics-only nodes checked on the phone.", Try = "Open the behaviour monitor in the lab tools." },
            new ExtRef { Name = "MPEG_haptic", Status = ExtStatus.Partly, Note = "The haptic action is in the code; most cases are not run on a device.", Try = "" },
            new ExtRef { Name = "MPEG_haptic_material", Status = ExtStatus.NotSupported, Note = "Mostly absent in the code.", Try = "" },
            new ExtRef { Name = "MPEG_animation_timing", Status = ExtStatus.Tested, Note = "Follows a glat track (play, pause, stop at final) on the phone.", Try = "" },
            new ExtRef { Name = "MPEG_mesh_linking", Status = ExtStatus.Partly, Note = "A dependent mesh follows an animated skinned shadow mesh; one case checked, morph weights not tested.", Try = "" },
            new ExtRef { Name = "MPEG_scene_dynamic", Status = ExtStatus.Tested, Note = "JSON patches from a mett track move a node on the phone; new meshes and materials are not rebuilt in place.", Try = "" },
            new ExtRef { Name = "MPEG_viewport_recommended", Status = ExtStatus.InCode, Note = "Implemented; no timed file exists to test it (clause 7 defines no sample entry for it).", Try = "" },
            new ExtRef { Name = "KHR_lights_punctual", Status = ExtStatus.InCode, Note = "Lights are created; the MPEG timed intensity needs timed data.", Try = "Switch lighting off in the lab tools." },
            new ExtRef { Name = "MPEG_lights_texture_based", Status = ExtStatus.NotSupported, Note = "Mostly absent in the code.", Try = "" },
            new ExtRef { Name = "MPEG_node_avatar", Status = ExtStatus.NotSupported, Note = "Only the documented fallback: the node is shown as it is.", Try = "" },
            new ExtRef { Name = "MPEG_primitive_V3C", Status = ExtStatus.NotSupported, Note = "Only the documented fallback: the primitive is shown as it is.", Try = "" },
        };

        private static readonly TrackRef[] TrackRefs =
        {
            new TrackRef { Type = 0, Name = "TRACKABLE_FLOOR", Status = ExtStatus.Tested, Note = "Tap to place, resize, fix, unlock and rescan." },
            new TrackRef { Type = 1, Name = "TRACKABLE_VIEWER", Status = ExtStatus.InCode, Note = "Content is locked to the head; seen rendering, not tested while moving." },
            new TrackRef { Type = 2, Name = "TRACKABLE_CONTROLLER", Status = ExtStatus.NotSupported, Note = "No controller pose; a phone has no controller." },
            new TrackRef { Type = 3, Name = "TRACKABLE_PLANE", Status = ExtStatus.Partly, Note = "Wall placement works through the Place on Wall button; plane matching on a textured wall is untested." },
            new TrackRef { Type = 4, Name = "TRACKABLE_MARKER_2D", Status = ExtStatus.InCode, Note = "Image tracking is in the code; not tried with a marker." },
            new TrackRef { Type = 5, Name = "TRACKABLE_MARKER_3D", Status = ExtStatus.NotSupported, Note = "The object is never searched for." },
            new TrackRef { Type = 6, Name = "TRACKABLE_MARKER_GEO", Status = ExtStatus.InCode, Note = "In the code; needs geospatial services and an outdoor position." },
            new TrackRef { Type = 7, Name = "TRACKABLE_APPLICATION", Status = ExtStatus.NotSupported, Note = "Cloud anchors need cloud authentication, which is not set up." },
        };

        private static string StatusWord(ExtStatus s)
        {
            switch (s)
            {
                case ExtStatus.Tested: return "Tested on the phone";
                case ExtStatus.InCode: return "In the code, not tested";
                case ExtStatus.Partly: return "Partly";
                default: return "Not supported";
            }
        }

        private static Color StatusColor(ExtStatus s)
        {
            switch (s)
            {
                case ExtStatus.Tested: return Ui.Good;
                case ExtStatus.InCode: return Ui.Accent;
                case ExtStatus.Partly: return Ui.Warn;
                default: return Ui.Bad;
            }
        }

        private void ShowExtensionReference()
        {
            m_detEntry = null;
            m_detailsOpen = true;
            Ui.Show(m_detGroup, true);
            m_detTitle.text = "Extension reference";
            for (int i = m_detContent.childCount - 1; i >= 0; i--) Destroy(m_detContent.GetChild(i).gameObject);
            Ui.Size(Ui.Spacer(m_detContent, 0, 6).gameObject, -1, 6);

            var intro = Section("How to read this", "Each extension of ISO/IEC 23090-14 with what this build does with it, and the scenes in your library that list it. Tested means seen working on the test phone. In the code means implemented but not run on a device yet.");
            var counts = Ui.Flow(intro, 10, 10);
            foreach (ExtStatus s in new[] { ExtStatus.Tested, ExtStatus.InCode, ExtStatus.Partly, ExtStatus.NotSupported })
                Ui.Chip(counts.transform, StatusWord(s) + ": " + ExtRefs.Count(x => x.Status == s), StatusColor(s), false, 26);

            foreach (var x in ExtRefs)
            {
                var sec = Section(x.Name);
                Ui.Chip(sec, StatusWord(x.Status), StatusColor(x.Status), false, 26);
                Ui.Label(sec, x.Note, 28, Ui.Regular, Ui.Dim, TextAnchor.UpperLeft, true);
                if (!string.IsNullOrEmpty(x.Try)) Ui.Label(sec, "Try: " + x.Try, 28, Ui.Regular, Color.white, TextAnchor.UpperLeft, true);
                if (x.Name == "MPEG_anchor") BuildTrackableRows(sec);
                else ListReferenceScenes(sec, Catalog.Entries.Where(e => e.Has(x.Name)).ToList());
            }
        }

        private void BuildTrackableRows(Transform sec)
        {
            foreach (var t in TrackRefs)
            {
                Ui.Size(Ui.Spacer(sec, 0, 6).gameObject, -1, 6);
                Ui.Label(sec, t.Name, 30, Ui.Medium, Color.white, TextAnchor.UpperLeft, false);
                Ui.Chip(sec, StatusWord(t.Status), StatusColor(t.Status), false, 24);
                Ui.Label(sec, t.Note, 26, Ui.Regular, Ui.Dim, TextAnchor.UpperLeft, true);
                ListReferenceScenes(sec, Catalog.Entries.Where(e => e.TrackableTypes.Contains(t.Type)).ToList());
            }
        }

        private void ListReferenceScenes(Transform sec, List<SceneEntry> scenes)
        {
            if (scenes.Count == 0) { Ui.Label(sec, "No scene in the library lists it.", 26, Ui.Regular, Ui.Dim, TextAnchor.UpperLeft, false); return; }
            foreach (var e in scenes.Take(3)) BuildSceneRow(sec, e);
            if (scenes.Count > 3) Ui.Label(sec, "and " + (scenes.Count - 3) + " more in the library", 26, Ui.Regular, Ui.Dim, TextAnchor.UpperLeft, false);
        }
    }
}
