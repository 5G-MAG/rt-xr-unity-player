# Testing the player on a device

This page is for a person or a coding agent who drives the XR Scene Player on an Android phone over `adb` and reads what it did
from the device log. A test run opens a scene (or runs the scene check) and writes `XRTEST` lines to the log. Outside test runs
those lines and the work behind them are switched off, so the app does not pay for them in normal use.

| | |
|---|---|
| Package | `com.fivegmag.rtxrplayer` |
| Activity | `com.unity3d.player.UnityPlayerActivity` |
| Data folder | `/storage/emulated/0/Android/data/com.fivegmag.rtxrplayer/files` (holds `Paths`, the scenes, `autoopen.txt`, `scene-check.txt`) |
| Log tag | `Unity` |

The scenes a run can open are the ones listed in `Paths` (see [xr-scene-player.md](xr-scene-player.md)).

## Starting a run

A run takes a **command** (described below). There are two ways to give it.

**Intent extra (preferred):** one command, no file to push. `-S` stops the app first, so the run starts from a fresh launch.

```sh
adb logcat -c
adb shell am start -S -n com.fivegmag.rtxrplayer/com.unity3d.player.UnityPlayerActivity --es xrtest "'scene_ycbcr|autoplay|until=30'"
```

The inner single quotes are needed: `adb shell` passes the line to the device's shell, where `|` would otherwise start a pipe.

**`autoopen.txt`:** the file is read once, when the app starts and the scene library has loaded, and is then deleted.

```sh
echo 'scene_ycbcr|autoplay|until=30' > autoopen.txt
adb push autoopen.txt /storage/emulated/0/Android/data/com.fivegmag.rtxrplayer/files/
adb logcat -c
adb shell am force-stop com.fivegmag.rtxrplayer
adb shell monkey -p com.fivegmag.rtxrplayer -c android.intent.category.LAUNCHER 1
```

When both are given, the intent extra is used and `autoopen.txt` is left in place for a later launch.

## Commands

`<target>|<flag>|<flag>...`

| Target | What it does |
|---|---|
| a part of a scene file name, e.g. `scene_ycbcr` | Opens the first scene of the library whose file name contains it (case does not matter). |
| `@check` | The scene check: opens every scene as a flat preview, one after the other, and reports what it observed. |
| `@lab` | Shows the Extension Lab. |
| `@settings` | Shows Settings. |

| Flag | Effect |
|---|---|
| `flat` | Opens the scene as a flat preview (no AR camera). |
| `landscape`, `portrait` | Locks the screen orientation. |
| `autoplay` | Media with `autoplay` true start by themselves. Without this flag the setting "Start media only when I press play", which is **on by default**, holds them, and a run sees no video or audio playing. |
| `scenecam` | Uses the camera node of the scene from the first frame (the interactivity test scenes assume it). Also implies `autoplay`, and the content is shown without waiting for an AR placement. |
| `nogate` | Turns off the timed object gate: objects that show a video are drawn before its first frame. |
| `clean` | No controls and no statistics on the screen (for the thumbnails of the catalog). |
| `until=<s>` | Logs the scene state up to `<s>` seconds after the scene opened (default 90), then ends the run. |

## Reading a run

```sh
adb logcat -d -s Unity | grep XRTEST
```

A run starts with `XRTEST run start` and ends with `XRTEST run done` (scene) or `XRTEST check done` (`@check`). Wait for the
end line before reading the result. Each line is one log message; Unity also logs a stack trace after each message, on the
following lines without the `XRTEST` prefix.

| Line | When | Fields |
|---|---|---|
| `XRTEST run start` | the command is read | `source=intent\|autoopen`, `command=`, `app=` (version), `device=` (model) |
| `XRTEST media controls` | the player opens | `shown=`, `media=` (media players), `controlsFlag=` (a media has `controls` true) |
| `XRTEST state t=<s> <file>` | 3, 5, 8, 11, 20, 30, 45, 60, 90 s after the scene opened, up to `until` | `cam=` camera position, then one `\|`-separated part per renderer: name, `on`/`off`/`gated`, position, `rot=`, `b=` bounds centre, `size=`, `vis=`, `layer=`, `mat=` shader, `tex=`/`mainTex=` sizes, `baseColor=`, `texpix=` three colours sampled from the texture (centre, ¼, ¾); then `media …` (`pipeline=`, `started=`, `t=`, `err=`, `auto=`, `fin=`, `pipe=`), `audio …`, `timedObjects=`/`waiting=`, `reverb …` |
| `XRTEST fired t=<s>` | an interactivity behavior fires | its triggers and actions |
| `XRTEST timed accessor wanted: <n>` | a scene consumer asks for a timed accessor | accessor index |
| `XRTEST timed publish accessor <n>` | a timed metadata sample is delivered | `t=`, `bytes=`, `count=`, `clock=` (media time) |
| `XRTEST scene update enqueued` | a scene update sample is queued | `media=`, `t=`, `sync=`, `bytes=`, `clock=` |
| `XRTEST behavior <n> is on-going …` | a scene update removes a running behavior | — |
| `XRTEST animation …` | `MPEG_animation_timing`: controller ready, an event applied, and once a second the state of the animated objects | — |
| `XRTEST check <OK\|SKIP\|FAIL> <uri> …` | `@check`, one per scene | load time, `unsupported:` extensions, `media missing:`, or why it did not open |
| `XRTEST check done` | `@check` finished | `opened=`, `skipped=`, `failed=`, `file=` (the report, also in `scene-check.txt`); `reason=notstarted` when the check could not start |
| `XRTEST run done` | the run ended | `scene=`, `reason=complete` (last state logged), `left` (the player was left before), `nomatch` (no scene matches, with `match=`), or `screen=lab\|settings reason=shown` |

Very long `state` lines (scenes with many nodes) may be cut by `logcat`.

## When the app dies

If the process is gone before the end line, it crashed or the system stopped it:

```sh
adb shell pidof com.fivegmag.rtxrplayer                         # nothing: the app is not running
adb logcat -d -b crash                                          # a native or Java crash
adb logcat -d -b events | grep -E "am_proc_died|killinfo"        # killinfo: stopped by the low-memory killer
```

To follow memory during a run, sample `adb shell dumpsys meminfo <pid>` once a second. `TOTAL PSS`, `Gfx dev` (GPU memory)
and `Unknown` (Unity's own allocations) are the interesting lines.

## Cost

The diagnostics are switched on by the run (`GLTFast.MpegDiagnostics.Enabled`), never in normal use. Within a run:

- every `XRTEST` line is a log call with a stack trace (the project logs script stack traces for all message types);
- each `state` line samples each renderer's texture through a 64×64 copy, a few kilobytes. Reading textures back at full
  size used to keep two full copies of each one per renderer until the end of the frame. With the 8192×8192 textures of the
  TV scenes, that made the app use about 2.3 GB on a phone, and the system stopped it.

Memory or timing measured during a run therefore includes this small overhead.
