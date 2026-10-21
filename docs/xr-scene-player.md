# XR Scene Player interface

The player opens on a scene library instead of a bare list. This page describes what the interface does and where its
parts are in the code. It describes the behaviour of the code; it makes no claim about conformance to ISO/IEC 23090-14.

All of the interface is built in code (`Assets/Scripts/App`), so the Unity scene file is unchanged. The stock canvas, list and buttons are
hidden when the app starts.

## Scenes and their details

The list of scenes is the `Paths` file, as before: one scene address per line (a file path or an HTTP address). Sources can also be
added in Settings, either a scene address or the address of a list file (same format as `Paths`).

For each scene the player reads, when it can:

- `metadata/<scene name>.json`, or else `metadata/scene.json`, next to the scene file. This is the metadata shipped in
  [rt-xr-content](https://github.com/5G-MAG/rt-xr-content): `name`, `summary`, `tags`, `screenshot`. The screenshot is the card picture.
- The scene file itself, for `extensionsUsed` and the media listed under `MPEG_media`. The extensions give the badges on the cards
  (Video, Audio, AR, Interactive, Haptics). The media are checked before the scene opens, so a missing file is reported instead of leaving a black screen.

When a scene file has no metadata of its own (for example `scene_extra_camera.gltf`, which shares `scene.json` with its siblings), the
shared name is used and the file name is shown as the variant.

## Screens

| Screen | What it does |
| --- | --- |
| Library | Hero for the scene opened last, rows by tag and by what a scene uses, search. |
| Scene sheet | Picture, summary, source, media check, what the scene needs. Open it, see its details, or open an AR scene as a flat preview. |
| Opening | Shows each step and what went wrong: scene file, media files, building the scene. A scene with missing media opens without them. |
| Player | Camera modes (Orbit, Walk, the scene's own camera), reset view, screenshot, media controls (play, pause, sound), animation control. Tap the scene to show or hide the controls. |
| Scene details | Extensions with the loader's report, media with their state, the behaviours the loader created, the scene tree, copy diagnostics. |
| Settings | Statistics overlay, auto-hide, fill light, start camera, sources, scene check, About. |

Orbit goes round the middle of the scene's bounds, and the clip planes follow the bounds. A scene with no light of its own gets a soft light and a
plain reflection, so metallic materials are not black.

## Extension Lab

The Lab tab has a passport (a stamp the first time a scene using one of eight extension families is opened) and a page per family with the
scenes that use it. In the player, the flask button opens the lab tools:

- **Extension lens**: outlines the parts of a scene that carry parts the loader attached (lights, audio sources, anchors, interactivity),
  coloured by family. Tap an outline for details, focus or hide.
- **Interactivity monitor**: lists each behaviour as it fires, with its trigger and action types.
- **Ranges and positions**: proximity ranges, audio source distances, light ranges, and collision, visibility and user input volumes.
- **Media pipeline**: pipeline state, buffer fill and the number of video textures and audio sources per media item.
- **Scene tree**: nodes and the parts attached to each; tap a row to find it in the scene.
- **Switch off**: lighting, spatial audio, interactivity, materials and video can be switched off for the current scene, to see what they add. Anchors reload the scene as a flat preview.

The Anchor playground (Lab tab) places a model from the library in the room and anchors it to the floor, to a plane, to the view or to
the world, using the same AR session as the AR scenes.

"Open every scene and report" in Settings opens each scene in turn as a flat preview and lists what was observed (opened or not,
extensions the loader reported as unsupported, missing media). It writes the same report to `scene-check.txt` in the app's data folder.

## Headset

On a Meta Quest the same screens are a panel in the room: point with the right controller, trigger to click, right stick to scroll, X on
the left controller to show or hide the panel. A scene smaller than a room is placed 1.6 m in front of the viewer; a room-sized scene is placed around the
viewer. The extension lens is for the phone screen and is not available in the headset.

## Test aids

A file named `autoopen.txt` in the app's data folder (next to `Paths`) names a scene, by a part of its file name, to open by itself when the app
starts. After a `|`, `flat` opens it as a flat preview and `landscape` or `portrait` sets the orientation. `@check`, `@lab` and `@settings`
open the scene check, the Lab and Settings.

## Known limits

- The user input trigger always reports true in the glTFast fork (`UserInputNodeTrigger.Resolve`), so a scene using it behaves as if the input were given.
- Scenes whose anchors are markers (2D, 3D, geolocation) or detected geometry need a live AR session. In a flat preview the loader's anchor code reports errors every frame, and the scene check skips them.
- Anchor types that need a marker, a controller or geolocation are not available in the playground.
- Scenes in sub-folders of the app's data folder could not be read on one phone (HTTP 404 on a `file://` address); the same scenes load over HTTP.
