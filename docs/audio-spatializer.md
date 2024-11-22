
### Configure an Audio spatializer SDK

Support for spatial audio, Unity3D requires an Audio Spatializer has to be configured in the project settings *Edit > Project Settings > Audio > Spatializer Plugin*.

![Audio spatializer configuration](images/unity-audio-spatializer-config.jpeg)

**If no audio spatializer plugin is configured, audio will play without spatialization**.

Please refer to Unity's [documentation for details and a list of available plugins](https://docs.unity3d.com/Manual/VRAudioSpatializer.html). 

Unity provides a native audio spatializer SDK with a [simple spatializer implementation](https://docs.unity3d.com/Manual/AudioSpatializerSDK.html).