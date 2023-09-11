/*
 * Copyright (c) 2023 MotionSpell
 * Licensed under the License terms and conditions for use, reproduction,
 * and distribution of 5GMAG software (the “License”).
 * You may not use this file except in compliance with the License.
 * You may obtain a copy of the License at https://www.5g-mag.com/license .
 * Unless required by applicable law or agreed to in writing, software distributed under the License is
 * distributed on an “AS IS” BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and limitations under the License.
 */

using System;
using System.Collections.Generic;

using UnityEngine;
using System.Text;
using Unity.Profiling;

#nullable enable

namespace rt.xr.unity
{

    using GLTFast;

    public class SceneViewer : MonoBehaviour
    {

        public string defaultGltfSouceUri = "";

        string statsText = "";
        ProfilerRecorder totalReservedMemoryRecorder;
        ProfilerRecorder gcReservedMemoryRecorder;
        ProfilerRecorder systemUsedMemoryRecorder;

        public bool memoryUsageRecorder; // should not be editable at runtime
        bool memoryUsageRecording;
        int avgFpsTotal = 0;
        int avgFpsCount = 0;
        int minFps = int.MaxValue;
        int maxFps = int.MinValue;

        bool autoplay = true;

        int sceneIndex = 0;
        SceneImport? gltf;
        List<MediaPlayer>? mediaPlayers = null;

        Bounds bounds;

        public string GetSourceUriFromCommandLineArgs()
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                int j = i + 1;
                if (args[i] == "--gltf")
                {
                    if (j < args.Length)
                    {
                        return args[j];
                    }
                    else
                    {
                        Debug.LogWarningFormat("--gltf command line option not followed by asset path");
                    }
                }
            }
            return defaultGltfSouceUri;
        }

        public void ConfigureInitialCamera()
        {
            /*
             * Configure camera[0] as the default camera, otherwise creates a new camera looking at the scene
             */
            var main = GetMainCamera();
            if(main.gameObject.GetComponent<CameraController>() == null)
            {
                main.gameObject.AddComponent<CameraController>();
            }

            foreach(var cam in gameObject.GetComponentsInChildren<Camera>())
            {
                if (cam != main)
                {
                    main.CopyFrom(cam);
                    return;
                }
            }

            bounds = Utils.ComputeSceneBounds();
            Utils.LookAt(main, bounds, transform.forward);            
        }

        public Camera GetMainCamera()
        {
            var cam = Camera.main;
            if (cam == null)
            {
                cam = gameObject.AddComponent<Camera>();
                cam.enabled = true;
                cam.tag = "MainCamera";
                // Assert.AreEqual(cam, Camera.main);
            }
            return cam;
        }

        public void EnsureAudioListenerExists()
        {
            // add a default listener
            UnityEngine.AudioListener[] listener = GetComponentsInChildren<UnityEngine.AudioListener>();
            if (listener.Length == 0)
            {
                UnityEngine.Camera.main.gameObject.AddComponent<AudioListener>();
            }
        }

        static List<MediaPlayer> CreateMediaPlayers(SceneImport gltfImport, Uri baseUri)
        {
            List<MediaPipelineConfig> configs = gltfImport.GetMediaPipelineConfigs(); // one config per media
            var players = new List<MediaPlayer>(configs.Count);
            for (var c = 0; c < configs.Count; c++)
            {
                var mp = MediaPlayer.Create(gltfImport.GetMedia(c, baseUri), configs[c]);
                if (mp == null)
                {
                    throw new Exception("failed to create media player");
                }
                players.Add(mp);
            }
            return players;
        }

        static void CreateVideoTextures(SceneImport gltfImport, List<MediaPlayer> mediaPlayers)
        {
            List<VideoTexture> videoTextures = gltfImport.CreateVideoTextures();
            foreach (VideoTexture vt in videoTextures)
            {
                int mediaIdx = gltfImport.GetBufferSourceMediaIndex(vt.bufferId);
                mediaPlayers[mediaIdx].AddVideoTexture(vt);
            }
        }

        static void CreateAudioSources(SceneImport gltfImport, GameObjectInstantiator instantiator, List<MediaPlayer> mediaPlayers)
        {
            if (instantiator.sceneInstance.audioSources is null)
            {
                return;
            }
            foreach (SpatialAudioSource aSrc in instantiator.sceneInstance.audioSources)
            {
                int mediaIdx = gltfImport.GetBufferSourceMediaIndex(aSrc.BufferId);
                mediaPlayers[mediaIdx].AddAudioSource(aSrc);
            }
        }


        async void Start()
        {
            string p = GetSourceUriFromCommandLineArgs();
            Uri path = new Uri(p, UriKind.RelativeOrAbsolute);
            
            if (!path.IsAbsoluteUri)
                path = new Uri(System.IO.Directory.GetCurrentDirectory()+"/"+p);
            Uri baseUri = UriHelper.GetBaseUri(path);

            if (p == "")
            {
                Debug.LogError("Source GLTF document path not configured. Use `--gltf ` command line argument, followed by the document URI.");
                Application.Quit(1);
            }

            gltf = new SceneImport();
            bool success = await gltf.Load(path);

            if (success)
            {
                if (sceneIndex > (gltf.sceneCount - 1))
                {
                    Debug.LogError($"invalid scene index:{sceneIndex }, when document contains {gltf.sceneCount}");
                    Application.Quit(1);
                }

                var instantiator = new GameObjectInstantiator(gltf, transform);

                await gltf.InstantiateSceneAsync(instantiator, sceneIndex); 
                
                mediaPlayers = CreateMediaPlayers(gltf, baseUri);
                CreateVideoTextures(gltf, mediaPlayers);
                CreateAudioSources(gltf, instantiator, mediaPlayers);

                ConfigureInitialCamera();
                EnsureAudioListenerExists();
                
            }
            else
            {
                UnityEngine.Debug.LogError("Loading glTF failed!");
                Application.Quit(1);
            }

        }

        private void enableMemoryRecorder()
        {
            if (memoryUsageRecording)
            {
                return;
            }
            totalReservedMemoryRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "Total Reserved Memory");
            gcReservedMemoryRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Reserved Memory");
            systemUsedMemoryRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "System Used Memory");
            memoryUsageRecording = true;
        }

        private void disposeMemoryRecorder()
        {
            if (!memoryUsageRecording)
            {
                return;
            }
            totalReservedMemoryRecorder.Dispose();
            gcReservedMemoryRecorder.Dispose();
            systemUsedMemoryRecorder.Dispose();
            memoryUsageRecording = false;
        }

        private void OnDisable()
        {
            disposeMemoryRecorder();
        }

        void Update()
        {
            if (memoryUsageRecorder)
            {
                if (!memoryUsageRecording)
                    enableMemoryRecorder();
                var sb = new StringBuilder(800);

                // int currentFps = (int)(1f / Time.unscaledDeltaTime);
                int currentFps = (int)(1f / Time.deltaTime);
                avgFpsTotal += currentFps;
                avgFpsCount++;
                if (currentFps != 0)
                {
                    minFps = Math.Min(minFps, currentFps);
                }
                maxFps = Math.Max(maxFps, currentFps);

                sb.AppendLine($"FPS: {avgFpsTotal/avgFpsCount} | {minFps} ~ {maxFps} ");
                if (totalReservedMemoryRecorder.Valid)
                    sb.AppendLine($"Total Reserved Memory: {totalReservedMemoryRecorder.LastValue}");
                if (gcReservedMemoryRecorder.Valid)
                    sb.AppendLine($"GC Reserved Memory: {gcReservedMemoryRecorder.LastValue}");
                if (systemUsedMemoryRecorder.Valid)
                    sb.AppendLine($"System Used Memory: {systemUsedMemoryRecorder.LastValue}");
                statsText = sb.ToString();
            } else {
                if (memoryUsageRecording)
                    disposeMemoryRecorder();
            }

            if (autoplay && (mediaPlayers != null))
            {
                foreach (var mp in mediaPlayers)
                {
                    mp.Play();
                }
                autoplay = false;
            }

            if (Input.GetKeyDown(KeyCode.Tab))
            {
                ConfigureInitialCamera();
            }

        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(1, 0, 0, 0.75f);
            Gizmos.DrawCube(bounds.center, bounds.size);

            Gizmos.color = new Color(0, 1, 0, 0.75f);
            Gizmos.DrawCube(new Vector3(0, 0, 0), new Vector3(5, 5, 5));
        }

        void OnDestroy()
        {
            if (mediaPlayers != null)
            {
                foreach (var mp in mediaPlayers)
                {
                    mp.Dispose();
                }
            }
        }

        void OnGUI()
        {
            if (memoryUsageRecorder)
                GUI.TextArea(new Rect(10, 30, 250, 50), statsText);
        }

    }

}