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
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using System.Text;
using Unity.Profiling;
using UnityEngine.XR.ARFoundation;

namespace rt.xr.unity
{
    using GLTFast;
    // using GLTFast.Mpeg;

    public class SceneViewer : MonoBehaviour
    {

        [SerializeField] private string m_configFileLocation ;
    
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

        public bool autoplayAnimation = true;
        public bool showLog = false;

        int sceneIndex = 0;


#nullable enable
        MpegGltfImport? mpegGltfImport;
#nullable disable

        private Bounds bounds;
        private uint maxLogMessages = 15;
        private Queue logQueue = new Queue();

        public string ConfigFileLocation { get { return m_configFileLocation; } }

        public delegate void GlTFLoadComplete();
        public GlTFLoadComplete onGlTFLoadComplete;

        public delegate void GlTFLoadError();
        public GlTFLoadError onGlTFLoadError;


        [SerializeField]
        private GameObject arSessionPrefab;

        [SerializeField]
        private GameObject arSessionOriginPrefab;

        private GameObject arSessionInstance;
        private GameObject arSessionOriginInstance;

        bool m_ARCameraEnabled = false;
        public bool ARCameraEnabled { get { return m_ARCameraEnabled; } }

        public Camera GetMainCamera()
        {
            var cam = Camera.main;
            if (cam == null)
            {
                cam = new GameObject("Created camera").AddComponent<Camera>();
                cam.enabled = true;
                cam.tag = "MainCamera";
            }
            return cam;
        }

        /*
        void EnsureAudioListener()
        {
            // add a default listener
            UnityEngine.AudioListener[] listener = GetComponentsInChildren<UnityEngine.AudioListener>();
            if (listener.Length == 0)
            {
                Debug.Log("glTF scene doesn't specify an audio listener.");
                Camera c = GetMainCamera();
                c.gameObject.AddComponent<AudioListener>();
            }
        }
        */

        public void ConfigureInitialCamera()
        {
            Camera[] _cameras = FindObjectsOfType<Camera>();
            Camera _currentCamera = null;
            for(int i = 0; i < _cameras.Length; i++)
            {
                if(i == 0)
                {
                    _currentCamera = _cameras[i];
                    _currentCamera.tag = "MainCamera";
                }
            }
            
            var main = GetMainCamera();
            foreach(var cam in gameObject.GetComponentsInChildren<Camera>())
            {
                // GLTF document may define none or mutliple cameras 
                // use the first one if any
                if (cam != main)
                {
                    main.CopyFrom(cam);
                    main.clearFlags = CameraClearFlags.Color;
                    main.backgroundColor = Color.black;
                    cam.enabled = false;
                    return;
                }
            }
            bounds = Utils.ComputeSceneBounds();
            Utils.LookAt(main, bounds, transform.forward);
        }

        public void EnableARCamera(){
            if (arSessionInstance == null)
            {
                arSessionInstance = Instantiate(arSessionPrefab);
            }
            if (arSessionOriginInstance == null)
            {
                arSessionOriginInstance = Instantiate(arSessionOriginPrefab);
            }
        }

        public void DisableARCamera(){
            if (arSessionInstance != null)
            {
                ARSession arSession = arSessionInstance.GetComponent<ARSession>();
                arSession.enabled = false;
                arSession.Reset();
                arSession = null;
                Destroy(arSessionInstance);
                arSessionInstance = null;
            }

            if (arSessionOriginInstance != null)
            {
                Destroy(arSessionOriginInstance);
                arSessionOriginInstance = null;
            }
        }

        async public void LoadGltf(string filePath)
        {
            mpegGltfImport = new MpegGltfImport();
            bool success = await mpegGltfImport.LoadGltfAsync(filePath);

            if (success)
            {
                if (m_ARCameraEnabled || mpegGltfImport.IsImplicitXrPassthrough()){
                    if (!UnityEngine.XR.XRSettings.enabled){
                        Debug.LogWarning("this player doesn't support XR mode");
                    }
                    EnableARCamera();
                }
                await mpegGltfImport.InstantiateMainSceneAsync(transform);

/*
                if (autoplayAnimation)
                {
                    var legacyAnimation = instantiator.SceneInstance.legacyAnimation;
                    if (legacyAnimation != null)
                    {
                        legacyAnimation.Play();
                    }
                }
*/

                if (!m_ARCameraEnabled){
                    ConfigureInitialCamera();
                }

                if (onGlTFLoadComplete != null){
                    onGlTFLoadComplete();
                }

            }
            else
            {
                UnityEngine.Debug.LogError("Loading glTF failed!");
                mpegGltfImport = null; // can't call gltf.Dispose() if we didn't run an instantiator.
                if (onGlTFLoadError != null){
                    onGlTFLoadError();
                }

            }
        }

        public void UnloadGltfScene()
        {
            VirtualSceneGraph.ResetAll();

            // Destroy all game objects instances
            mpegGltfImport?.Dispose();

            // Dispose of all media players
            if (MediaImport.MediaPlayers != null)
            {
                foreach (var mp in MediaImport.MediaPlayers)
                {
                    mp.Dispose();
                }
                MediaImport.MediaPlayers.Clear();
            }
            if (m_ARCameraEnabled){
                DisableARCamera();
            }
            m_ARCameraEnabled = false;
            Camera[] _cameras = FindObjectsOfType<Camera>();
            for(int i = 0; i < _cameras.Length; i++)
            {
                Destroy(_cameras[i]);
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

        void HandleLog(string logString, string stackTrace, LogType type)
        {
            logQueue.Enqueue("[" + type + "] : " + logString);
            if (type == LogType.Exception)
                logQueue.Enqueue(stackTrace);
            while (logQueue.Count > maxLogMessages)
                logQueue.Dequeue();
        }

        void OnEnable()
        {
            Application.logMessageReceived += HandleLog;
        }

        private void OnDisable()
        {
            Application.logMessageReceived -= HandleLog;
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



            if (MediaImport.MediaPlayers.Count > 0)
            {
                foreach (var mp in MediaImport.MediaPlayers)
                {
                    if (mp.autoPlay)
                    {
                        mp.Play();
                    }
                }
            }

            if (Input.GetKeyDown(KeyCode.Tab))
            {
                ConfigureInitialCamera();
            }

            if (Input.GetKeyDown(KeyCode.L))
            {
                if(showLog){
                    showLog = false;
                } else {
                    showLog = true;
                }
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
            if (MediaImport.MediaPlayers != null)
            {
                foreach (var mp in MediaImport.MediaPlayers)
                {
                    mp.Dispose();
                }
            }
            
        }

        void OnGUI()
        {
            if (memoryUsageRecorder)
                GUI.TextArea(new Rect(10, 30, 250, 50), statsText);

            if (showLog)
            {
                int offset = 100;
                GUILayout.BeginArea(new Rect(0, offset, Screen.width, Screen.height-2*offset));
                GUILayout.BeginVertical();
                GUILayout.FlexibleSpace();
                GUILayout.Label("\n" + string.Join("\n", logQueue.ToArray()));
                GUILayout.EndVertical();
                GUILayout.EndArea();
            }
        }
    }
}