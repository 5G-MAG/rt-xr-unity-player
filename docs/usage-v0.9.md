## XR Player usage

The XR Player can be configured with a default scene.

## Getting content

In the root folder of the project create a folder `content` and enter the folder:

`mkdir content && cd content`

Now fetch the content from our [rt-xr-content](https://github.com/5G-MAG/rt-xr-content) repository:

`git clone https://github.com/5G-MAG/rt-xr-content`

from command line: 
```
SampleSceneViewer.exe --gltf path/to/scene.gltf
``` 

the path to the gltf document can be an http URI or a local path.

A default gltf document can be configured in Unity's editor:

![Adding a default gltf uri](/doc/images/default-gltf-uri.jpg "Adding a default gltf uri")

This is mostly usefull to try different assets directly in the editor. When specified, the --gltf flag has priority over the default value.


### Default GLTF scene configuration

![default-scene-configuration](../images/unity-player-default-scene-config.png)
1. Locate the `Scenes` directory in the Unity editor 
2. Load the main scene of the project
3. Select the `GltfSceneViewer` component
4. Configure the default Gltf URI

### Launch a custom gltf document with a pre-built  
