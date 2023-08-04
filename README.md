# sample gltf scene viewer implementing mpeg extensions 

## Build

**1. clone the project and its submodules**
```
mkdir gltfsceneviewer && cd gltfsceneviewer
git clone https://github.com/5G-MAG/rt-xr-unity-player.git .
git submodule update --init --recursive
```

**2. Fetch sample content**

We are in the process of setting up a Github LFS, see [here](https://github.com/5G-MAG/rt-xr-unity-player/issues/1).

In the meantime please download the sample content from [here](https://owncloud.fokus.fraunhofer.de/index.php/s/D9t6UsxTNhdeZ2i) and put it into the `content` folder (you might need to create the folder first)

The default content is the video sequence and should be place in the same folder as the executable for it to be loaded properly.


**3. Make sure libav's dynamic libraries are installed on your system**. 

This is a dependency of the sample media pipeline based on libav. For details, including compatible libav version, [see "dependency on libav" below](#dependency-on-libav).


**4. open the project in Unity**


**5. use the editor's play mode, or build the project and run the executable** (eg. `File > Build settings > Build > Clean Build`)

The project is currently developped and tested on windows only (precompiled maf library).


## Usage

from command line: 
```
SampleSceneViewer.exe --gltf path/to/scene.gltf
``` 

the path to the gltf document can be an http URI or a local path.

A default gltf document can be configured in Unity's editor:

![Adding a default gltf uri](/doc/images/default-gltf-uri.jpg "Adding a default gltf uri")

This is mostly usefull to try different assets directly in the editor. When specified, the --gltf flag has priority over the default value.


### Controls

#### XR

If an OpenXR HMD is detected, it is used to control the camera.

#### Mouse & Keyboard usage

| Key           | Action                |
|---------------|-----------------------|
| mouse drag    | look around           |
| arrow UP      | move forward          |
| arrow DOWN    | move backward         |
| arrow LEFT    | move left             |
| arrow RIGHT   | move right            |
| mouse wheel   | move up/down          |
| left shift    | faster                |
| right shift   | faster                |
| Tab           | reset main camera     |



## Dependencies

### Audio spatializer

Support for spatial audio, Unity3D requires an Audio Spatializer plugin to be configured in the project settings *Edit > Project Settings > Audio > Spatializer Plugin*.

**If no audio spatializer plugin is configured, audio will play without spatialization**.

Please refer to Unity's [documentation for details and a list of available plugins](https://docs.unity3d.com/Manual/VRAudioSpatializer.html). 
Unity offers a native audio spatializer SDK with a [simple spatializer implementation](https://docs.unity3d.com/Manual/AudioSpatializerSDK.html).


### **media pipelines and Media Access Functions**

The media pipelines and MAF API are implemented as C++ with c# bindings. 
These are bundled as a unity package pulled from a separate repository:[https://github.com/5G-MAG/rt-xr-maf-plugin](https://github.com/5G-MAG/rt-xr-maf-plugin), with precompiled binaries. To build your own media pipeline, see the [MAF library C++ source](https://github.com/5G-MAG/rt-xr-maf-native). The sample media pipeline implements audio/video decoding using libav.


#### **dependency on libav**

Libav must be available as a dynamic library on your system.

More specificaly, the following versions are expected, from `ffmpeg-5.1.2`'s shared libraries:
- libavcodec-59
- libavformat-59
- libavutil-57
- libswscale-6
- libswresample-4

You can build your own, or use pre-built binaries from [https://ffmpeg.org/download.html](https://ffmpeg.org/download.html#build-windows)

On windows, open the 'Edit environment variables' settings dialog, double click on 'Path', and add the directory containing the libav DLLs to the list of directories.


### **GLTFast fork**

The project requires a [custom fork of glTFast](https://github.com/5G-MAG/rt-xr-gITFast) implementing MPEG scene description extensions.

For development, the package is installed as a git submodule in the Package directory, making it an [embedded package](https://docs.unity3d.com/Manual/CustomPackages.html#EmbedMe) to the project.

#### working on glTFast

Close Unity Editor while working on the glTFast package.
Reopen the project from the Unity hub to test integration and rebuild the project.


## Documentation

SceneViewer's implementation overview: 

![Components diagram](/doc/images/SceneViewerComponentsDiagram.png "Adding a default gltf uri")
