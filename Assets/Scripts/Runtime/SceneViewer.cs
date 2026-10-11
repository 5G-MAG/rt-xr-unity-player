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

        public bool autoplayAnimation = false;
        public bool showLog = false;

        int sceneIndex = 0;


#nullable enable
        SceneImport? gltf;
        List<MediaPlayer>? mediaPlayers = null;
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

        // set by the app before loading a scene that uses MPEG_anchor, to open it as a flat preview without an AR session
        public bool PreviewWithoutAR = false;
        // true when the scene lists MPEG_anchor, whether or not an AR session was started for it
        public bool SceneUsesAnchors { get; private set; }
        public IReadOnlyList<MediaPlayer> MediaPlayers { get { return mediaPlayers; } }
        public SceneImport CurrentImport { get { return gltf; } }

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
            // MPEG_viewport_recommended: the camera object that has recommended viewport data becomes the view
            var recommended = GLTFast.MpegViewportRecommendedRuntime.PreferredCamera;
            if (recommended != null && recommended != main)
            {
                main.CopyFrom(recommended);
                main.clearFlags = CameraClearFlags.Color;
                main.backgroundColor = Color.black;
                recommended.enabled = false;
                GLTFast.MpegViewportRecommendedRuntime.SetMirrorTarget(main);
                return;
            }
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

        public void EnsureAudioListenerExists()
        {
            // add a default listener
            UnityEngine.AudioListener[] listener = GetComponentsInChildren<UnityEngine.AudioListener>();
            if (listener.Length == 0)
            {
                Camera c = GetMainCamera();
                c.gameObject.AddComponent<AudioListener>();
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

        // an exception while loading reports an error to the caller instead of leaving it waiting
        async public void LoadGltf(string filePath)
        {
            try { await LoadGltfAsync(filePath); }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError("Loading glTF failed: " + e.Message + " " + e.StackTrace);
                if (onGlTFLoadError != null) onGlTFLoadError();
            }
        }

        private async System.Threading.Tasks.Task LoadGltfAsync(string filePath)
        {
            Uri path = new Uri(filePath, UriKind.RelativeOrAbsolute);
            
            if (!path.IsAbsoluteUri)
                path = new Uri(System.IO.Directory.GetCurrentDirectory()+"/"+path);

            Uri baseUri = UriHelper.GetBaseUri(path);

            gltf = new SceneImport();
            bool success = await gltf.Load(path);

            if (success)
            {

                var instantiator = new GameObjectInstantiator(gltf, transform);

                m_ARCameraEnabled = (gltf.GetSourceRoot().extensionsUsed != null) && (Array.IndexOf(gltf.GetSourceRoot().extensionsUsed, "MPEG_anchor") >= 0);
                SceneUsesAnchors = m_ARCameraEnabled;
                if (m_ARCameraEnabled && PreviewWithoutAR){
                    m_ARCameraEnabled = false;
                }
                if (m_ARCameraEnabled && QuestRig.Active){
                    // a headset session has no ARCore plane detection: MPEG_anchor nodes stay where the scene puts them
                    Debug.LogWarning("MPEG_anchor is not resolved on a headset");
                    m_ARCameraEnabled = false;
                }
                if (m_ARCameraEnabled){
                    if (!UnityEngine.XR.XRSettings.enabled){
                            Debug.LogWarning("this player doesn't support XR mode");
                    }
                    EnableARCamera();
                }

                await gltf.InstantiateSceneAsync(instantiator, sceneIndex);
                if (autoplayAnimation)
                {
                    var legacyAnimation = instantiator.sceneInstance.legacyAnimation;
                    if (legacyAnimation != null)
                    {
                        legacyAnimation.Play();
                    }
                }

                mediaPlayers = CreateMediaPlayers(gltf, baseUri);
                CreateVideoTextures(gltf, mediaPlayers);
                CreateAudioSources(gltf, instantiator, mediaPlayers);
                // timed metadata for the MPEG scene extensions (animation timing, viewport, mesh linking, lights)
                // Init on every load clears the buffer layouts cached for the previous scene
                var timedBridge = gameObject.GetComponent<TimedMetadataBridge>();
                if (timedBridge == null)
                {
                    timedBridge = gameObject.AddComponent<TimedMetadataBridge>();
                }
                timedBridge.Init(this);
                // 4.3: an object that has timed media components shall not be rendered until T0 + Tinit
                var gate = gameObject.GetComponent<TimedObjectGate>();
                if (gate == null) gate = gameObject.AddComponent<TimedObjectGate>();
                gate.Init(this);

                if (!m_ARCameraEnabled && !QuestRig.Active){
                    ConfigureInitialCamera();
                }
                EnsureAudioListenerExists();

                if (onGlTFLoadComplete != null){
                    onGlTFLoadComplete();
                }

            }
            else
            {
                UnityEngine.Debug.LogError("Loading glTF failed!");
                gltf = null; // can't call gltf.Dispose() if we didn't run an instantiator.
                if (onGlTFLoadError != null){
                    onGlTFLoadError();
                }

            }
        }

        public void UnloadGltfScene()
        {
            VirtualSceneGraph.ResetAll();

            // Destroy all game objects instances
            gltf?.Dispose();

            // Dispose of all media players
            if (mediaPlayers != null)
            {
                foreach (var mp in mediaPlayers)
                {
                    if (mp == null) continue;
                    var holder = mp.gameObject;
                    mp.Dispose();
                    // the object that was created only to hold the player goes with it
                    if (holder != null && holder.GetComponents<Component>().Length <= 2) Destroy(holder);
                }
                mediaPlayers.Clear();
                mediaPlayers = null;
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

            if (mediaPlayers != null)
            {
                // media with autoplay start as soon as they are ready, the media of an autoplayGroup together
                MediaPlayer.UpdateAutoplay(mediaPlayers);
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