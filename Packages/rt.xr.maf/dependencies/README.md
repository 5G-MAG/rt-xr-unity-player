# Adding pre-compiled native dependencies to Unity project

When adding and updating native dependencies, make sure to [configure them in unity for their respective target platform and architecture](https://docs.unity3d.com/6000.0/Documentation/Manual/plug-in-inspector.html).


# FFmpeg dependency

`avpipeline` has a dependency on *FFmpeg * to provide a baseline for various protocols, formats, and codecs.

Add the ffmpeg libraries to the relevant sub-directory for your target platform.
