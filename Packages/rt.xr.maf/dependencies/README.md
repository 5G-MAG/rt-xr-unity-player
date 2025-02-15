# Adding pre-compiled native dependencies to Unity project

When adding and updating native dependencies, make sure to [configure them in unity for their respective target platform and architecture](https://docs.unity3d.com/6000.0/Documentation/Manual/plug-in-inspector.html).

If a shared library fails loading despite proper configuration, **it is very likely that one of its dependencies is not loaded as expected**.

## FFmpeg dependency

`avpipeline` has a dependency on *FFmpeg * to provide a baseline for various protocols, formats, and codecs.

Add the ffmpeg libraries to the relevant sub-directory for your target platform.

## libc++

See: https://developer.android.com/ndk/guides/cpp-support#libc

> libc++ is not a system library. If you use libc++_shared.so, it must be included in your app. If you're building your application with Gradle this is handled automatically.
