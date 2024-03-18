# 5GMAG - XR Player

This project is an interactive and XR capable gltf scene viewer supporting GLTF extensions specified in [ISO/IEC 23090-14](https://www.iso.org/standard/86439.html), implemented in Unity3D.

These extensions support features such as video textures, spatial audio sources, interactivity behaviors, AR anchors, ...

The [dedicated 5GMAG wiki section](https://5g-mag.github.io/Getting-Started/pages/xr-media-integration-in-5g/) provides an overview of the project and additional ressources, including the implementation status of [ISO/IEC 23090-14](https://www.iso.org/standard/86439.html).

## Supported platforms

The project supports the latest [Unity3D LTS editor release](https://unity.com/releases/editor/qa/lts-releases), Unity 2022.3.

The XR Player feature set dependends on the target platform. See the [features page](https://5g-mag.github.io/Getting-Started/pages/xr-media-integration-in-5g/features) implementation status.

It is currently developped, tested and built for Windows and Android targets.

## Getting the code

The projet has dependencies which are not delivered through UPM (Unity's Package Manager), but instead are tracked as git submodules.

**clone the project and fetch all submodules**
```
mkdir gltfsceneviewer && cd gltfsceneviewer
git clone https://github.com/5G-MAG/rt-xr-unity-player.git .
git submodule update --init --recursive
```


## Building

### Building the Unity project

![Build the Unity project](docs/images/unity-build-player.png)
1. Locate the `Build Settings` menu
2. Review the target platform, [change as needed](#changing-the-build-target-platform)
3. Review the build type
4. Build

## Configuring the project

### Changing the build target platform

![Build target configuration](docs/images/unity-build-change-target.png)
1. in the build settings, select the target platform
2. click on the "switch platform" button


### Configure an Audio spatializer SDK

Support for spatial audio, Unity3D requires an Audio Spatializer has to be configured in the project settings *Edit > Project Settings > Audio > Spatializer Plugin*.

**If no audio spatializer plugin is configured, audio will play without spatialization**.

Please refer to Unity's [documentation for details and a list of available plugins](https://docs.unity3d.com/Manual/VRAudioSpatializer.html). 

Unity provides a native audio spatializer SDK with a [simple spatializer implementation](https://docs.unity3d.com/Manual/AudioSpatializerSDK.html).


### Configure an XR Plugin 

https://docs.unity3d.com/Manual/xr-configure-providers.html
