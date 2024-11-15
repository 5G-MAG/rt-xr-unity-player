<h1 align="center">XR Unity Player</h1>
<p align="center">
  <img src="https://img.shields.io/badge/Status-Under_Development-yellow" alt="Under Development">
  <img src="https://img.shields.io/github/v/tag/5G-MAG/rt-xr-unity-player?label=version" alt="Version">
  <img src="https://img.shields.io/badge/License-5G--MAG%20Public%20License%20(v1.0)-blue" alt="License">
</p>


## Introduction

The XR Unity Player is an interactive and XR-capable glTF scene viewer supporting glTF extensions specified in the MPEG-I Scene Description framework ([ISO/IEC 23090-14](https://www.iso.org/standard/86439.html)), implemented in Unity3D. These extensions support features such as video textures, spatial audio sources, interactivity behaviors, AR anchors, ...

Additional information can be found at: https://5g-mag.github.io/Getting-Started/pages/xr-media-integration-in-5g/



## Clone the unity project and embedded dependencies

```
git clone --recursive https://github.com/5G-MAG/rt-xr-unity-player.git
```

The project has dependency on packages tracked as git submodules:
- *rt-xr-glTFast*: a fork of unity's glTF support package adding support for MPEG extensions.
- *rt-xr-maf-native*: media pipelines supporting the MAF API.


> [!INFO] 
> When pulling changes, submodules aren't updated by default. It has to be explicitly requested, eg. using: `git pull --recurse-submodules`




## Build the project and install it on an Android device

![Build the Unity project](docs/images/unity-build-player.png)
1. Locate the `Build Settings` menu
2. Make sure that Android is the selected platform, [change as needed](#changing-the-build-target-platform)
3. Check that Mobile XR is the default scene
4. Select the device on which the application will be installed
5. Build & Run


## Upload content to an Android device & configure the player

This section assumes adb is installed on the machine, and Android smartphone is connected, with *developer mode* enabled on the phone.

Clone the `rt-xr-content` repository:
```
git clone https://github.com/5G-MAG/rt-xr-content.git
```

Push glTF content to the phone:
```
cd rt-xr-content
adb push ./awards /storage/emulated/0/Android/data/com.fivegmag.rtxrplayer/files/awards
```

Create a file named *'Paths'* listing gltf documents to be exposed in the player, one per line:
```
/storage/emulated/0/Android/data/com.fivegmag.rtxrplayer/files/awards/scene.gltf
/storage/emulated/0/Android/data/com.fivegmag.rtxrplayer/files/awards/scene_floor_anchoring.gltf
```

Upload the *'Paths'* file to the Android device:
```
adb push ./Paths /storage/emulated/0/Android/data/com.fivegmag.rtxrplayer/files/Paths
```


## Supported Unity Editor version

The project supports the [Unity3D 2022 LTS editor release](https://unity.com/releases/editor/qa/lts-releases).


## Supported platforms

It is currently developped and tested on Android devices.

**By default, the project is compiled for Android 9.0 (API Level 28), targeting arm64 architexture.**

This can be changed in Unity's *"Player settings"* panel, under the *"Settings for Android"* tab, in the *"Other settings"* section.

Mobile XR scenarios using the *MPEG_anchor* glTF extension are supported on **Android** through the [Google ARCore](https://docs.unity3d.com/Packages/com.unity.xr.arcore@5.1/manual/index.html) plugin. Google maintains a [list of compatible XR devices](https://developers.google.com/ar/devices?hl=fr).


## License

This project is developed under 5G-MAG's Public License. For the full license terms, please see the LICENSE file distributed along with the repository or retrieve it from [here](https://drive.google.com/file/d/1cinCiA778IErENZ3JN52VFW-1ffHpx7Z/view).

