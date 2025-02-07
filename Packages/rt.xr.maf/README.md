<h1 align="center">Media Access Function (MAF) API Unity3D packager</h1>
<p align="center">
  <img src="https://img.shields.io/badge/Status-Under_Development-yellow" alt="Under Development">
  <img src="https://img.shields.io/github/v/tag/5G-MAG/rt-xr-maf-plugin?label=version" alt="Version">
  <img src="https://img.shields.io/badge/License-5G--MAG%20Public%20License%20(v1.0)-blue" alt="License">
</p>

## Introduction
This repository is a dependency of the [rt-xr-unity-player](https://github.com/5G-MAG/rt-xr-unity-player). It provides build artifacts from [rt-xr-maf-native](https://github.com/5G-MAG/rt-xr-maf-native).

Additional information can be found at: https://5g-mag.github.io/Getting-Started/pages/xr-media-integration-in-5g/

## Usage

### Adding to Unity projects

The package can be added to a Unity3D project using the package manager UI, by using ["Add package from git URL"](https://docs.unity3d.com/Manual/upm-ui-giturl.html) to download and add it to the project's `Packages` directory.

Alternatively, it is possible to simply clone the repository to a local directory `git clone https://github.com/5G-MAG/rt-xr-maf-plugin.git rt.xr.maf`,
then chose to [Add package from disk](https://docs.unity3d.com/Manual/upm-ui-local.html).

## Package content

**/!\\ Do not edit source code in these directories directly /!\\**

## Tests

Tests run in Unity3D's test runner.

## Dependencies

### ffmpeg libraries

The `avpipeline` plugin makes use of ffmpeg libraries.

The pre-compiled ffmpeg libraries are [compiled to comply with LGPL](https://ffmpeg.org/legal.html), with only a subset of all codecs available.
To use additional codec libraries (such as libx264 or libx265), make sure to link against your own ffmpeg libraries when building your project.

## Implementation notes

During initial development of this package, the C# bindings have been provided a clear separation between the media pipeline implementation in native code, and Unity3D managed code.

However this approach has limitations, and it should be emphasized that a Unity native plugin can use the MAF API directly in native code.

## License

Licensed under the License terms and conditions for use, reproduction, and distribution of 5G-MAG Public License v1.0 (the “License”).

You may not use this file except in compliance with [the License](/LICENSE.md).You may obtain a copy of the License at https://www.5g-mag.com/license.

Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an “AS IS” BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and limitations under the License.
