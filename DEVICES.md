# Specifications
- TS 26.119 (rel. 18): Device Media Capabilities for Augmented Reality Services
- TR 26.928 (rel. 18): Extended Reality (XR) in 5G; 4.8.1 - Device Types


## Mobile XR - XR5G-P1

### Android ARcore

- Ensure the build is configured for Android
- Configure the [ARCore](https://docs.unity3d.com/Packages/com.unity.xr.arcore@5.2/manual/index.html) plugin for ARFoundation
- Add the optional [ARCore extensions package](https://developers.google.com/ar/develop/unity-arf/getting-started-extensions?ar_foundations_version=6)
- Configure the player for OpenGL ES 3.2 graphics
- In Unity's build settings, select the MobileXR scene only

## HMD - XR5G-V4 / XR5G-A3

### Meta / Android / OpenXR

- Ensure the build is configured for Android
- Configure the [OpenXR Meta](https://docs.unity3d.com/Packages/com.unity.xr.meta-openxr@2.1/manual/index.html) plugin for ARFoundation
- Configure OpenXR interaction profiles (simple controller, hand interaction profile)
- Configure the player for Vulkan graphics
- In Unity's build settings, select the MetaQuestARF scene only
