#!/usr/bin/env bash
# Builds the Android phone app (ARCore, MobileXR scene) from the command line, and optionally installs and starts it.
#
#   tools/build-android.sh [--output <apk>] [--dev] [--install] [--run] [-s <serial>] [--no-build]
#
#   --output <apk>  where to write the APK (default: Build/mobilexr/rt-xr-unity-player.apk)
#   --dev           development build
#   --install       install the APK on the connected device (adb install -r)
#   --run           install, then start the app
#   -s <serial>     the adb device to use when several are connected
#   --no-build      skip the build and only install or run the existing APK
#
# Unity is found from ProjectSettings/ProjectVersion.txt in the Unity Hub folder; set UNITY to the editor executable to use
# another one. The Unity editor must not have this project open: batch mode cannot share it.
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
PACKAGE="com.fivegmag.rtxrplayer"
ACTIVITY="com.unity3d.player.UnityPlayerActivity"
OUTPUT="Build/mobilexr/rt-xr-unity-player.apk"
LOG="$ROOT/Build/build-android.log"
DEV=0 INSTALL=0 RUN=0 BUILD=1 SERIAL=""

usage() { sed -n '2,14p' "$0" | sed 's/^# \{0,1\}//'; }
die() { echo "build-android: $*" >&2; exit 1; }

while [ $# -gt 0 ]; do
    case "$1" in
        --output) [ $# -ge 2 ] || die "--output needs a path"; OUTPUT="$2"; shift 2 ;;
        --dev) DEV=1; shift ;;
        --install) INSTALL=1; shift ;;
        --run) INSTALL=1; RUN=1; shift ;;
        -s) [ $# -ge 2 ] || die "-s needs a device serial"; SERIAL="$2"; shift 2 ;;
        --no-build) BUILD=0; shift ;;
        -h|--help) usage; exit 0 ;;
        *) usage >&2; die "unknown option: $1" ;;
    esac
done

case "$OUTPUT" in /*) APK="$OUTPUT" ;; *) APK="$ROOT/$OUTPUT" ;; esac

find_unity() {
    if [ -n "${UNITY:-}" ]; then
        [ -x "$UNITY" ] || die "UNITY is set but not executable: $UNITY"
        echo "$UNITY"; return
    fi
    local ver
    ver="$(sed -n 's/^m_EditorVersion: *//p' "$ROOT/ProjectSettings/ProjectVersion.txt" | tr -d '\r')"
    [ -n "$ver" ] || die "cannot read the Unity version from ProjectSettings/ProjectVersion.txt"
    local c
    for c in "/Applications/Unity/Hub/Editor/$ver/Unity.app/Contents/MacOS/Unity" \
             "$HOME/Unity/Hub/Editor/$ver/Editor/Unity" \
             "/opt/unity/editors/$ver/Editor/Unity"; do
        if [ -x "$c" ]; then echo "$c"; return; fi
    done
    die "Unity $ver not found in the Unity Hub folder; install it with the Android module, or set UNITY"
}

if [ "$BUILD" -eq 1 ]; then
    UNITY_BIN="$(find_unity)"
    mkdir -p "$(dirname "$LOG")"
    args=(-batchmode -quit -projectPath "$ROOT" -buildTarget Android
          -executeMethod BuildCli.BuildAndroid -buildOutput "$APK" -logFile "$LOG")
    [ "$DEV" -eq 1 ] && args+=(-development)
    echo "build-android: building with $UNITY_BIN (log: $LOG)"
    set +e
    "$UNITY_BIN" "${args[@]}"
    status=$?
    set -e
    if [ "$status" -ne 0 ]; then
        if grep -q "another Unity instance is running" "$LOG" 2>/dev/null; then
            die "the project is open in the Unity editor; close it and try again"
        fi
        grep -E "^BuildCli: |error CS[0-9]+|Error building Player" "$LOG" 2>/dev/null | tail -n 20 >&2 || true
        die "build failed (exit $status), see $LOG"
    fi
    grep "^BuildCli: " "$LOG" | tail -n 1 || true
    [ -f "$APK" ] || die "Unity reported success but $APK is missing"
    echo "build-android: $APK"
fi

if [ "$INSTALL" -eq 1 ]; then
    command -v adb >/dev/null 2>&1 || die "adb not found on the PATH"
    [ -f "$APK" ] || die "no APK at $APK"
    ADB=(adb)
    [ -n "$SERIAL" ] && ADB+=(-s "$SERIAL")
    "${ADB[@]}" get-state >/dev/null 2>&1 || die "no device connected (or several: choose one with -s <serial>)"
    echo "build-android: installing on $("${ADB[@]}" get-serialno)"
    "${ADB[@]}" install -r "$APK"
    if [ "$RUN" -eq 1 ]; then
        "${ADB[@]}" shell am start -S -n "$PACKAGE/$ACTIVITY"
    fi
fi
