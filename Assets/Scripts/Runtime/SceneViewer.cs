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
using UnityEngine.XR.ARFoundation;
using MediaPlayer = GLTFast.MediaPlayer;

namespace rt.xr.unity
{
    using GLTFast;

    enum ArSessionInitialization {
        DISABLED = 0,
        ENABLED = 1,
        AUTO = 2
    }

    public class SceneViewer : MonoBehaviour
    {



#nullable enable
        MpegGltfImport? mpegGltfImport;
#nullable disable

        private Bounds bounds;

        public delegate void GlTFLoadComplete();
        public GlTFLoadComplete onGlTFLoadComplete;

        public delegate void GlTFLoadError();
        public GlTFLoadError onGlTFLoadError;

        [SerializeField]
        private ArSessionInitialization m_Passthrough = ArSessionInitialization.AUTO;

        [SerializeField]
        
        private GameObject m_ARSessionPrefab;
        private GameObject m_arSessionInstance;

        [SerializeField]
        private GameObject m_XROriginPrefab;
        private GameObject m_xrOriginInstance;

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

        // the file that lists the glTF documents the player shows (default: Paths in the persistent data path of the app)
        [SerializeField] private string m_configFileLocation;
        public string ConfigFileLocation { get { return m_configFileLocation; } }

        // the on-screen log of the player (LogHandler), which the app hides behind its own screens
        public bool showLog
        {
            get { var l = FindFirstObjectByType<LogHandler>(); return l != null && l.showLog; }
            set { var l = FindFirstObjectByType<LogHandler>(); if (l != null) l.showLog = value; }
        }

        // set by the app before loading a scene that uses MPEG_anchor, to open it as a flat preview without an AR session
        public bool PreviewWithoutAR = false;
        // true when the scene lists MPEG_anchor, whether or not an AR session was started for it
        public bool SceneUsesAnchors { get; private set; }
        public IReadOnlyList<MediaPlayer> MediaPlayers { get { return VirtualSceneGraph.GetAllMediaPlayers(); } }
        public GltfImport CurrentImport { get { return mpegGltfImport?.Import; } }

        // true when an AR session runs for the scene: it is anchored, not previewed flat, and no headset drives the camera
        public bool ARCameraEnabled
        {
            get { return mpegGltfImport != null && !PreviewWithoutAR && !QuestRig.Active && PassThroughEnabled; }
        }

        public bool PassThroughEnabled { 
            get { 
                switch (m_Passthrough) {
                    case ArSessionInitialization.DISABLED:
                        return false;
                    case ArSessionInitialization.ENABLED:
                        return (m_XROriginPrefab != null) && (m_ARSessionPrefab != null);
                    default: // ArSessionInitialization.AUTO
                        if ((m_XROriginPrefab == null) || (m_ARSessionPrefab == null)){
                            return false;                            
                        }
                        return mpegGltfImport.IsImplicitXrPassthrough();
                }
            }
        }

        public void ConfigureInitialCamera()
        {
            Camera[] _cameras = FindObjectsByType(typeof(Camera), FindObjectsSortMode.None) as Camera[];
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

        public void EnableARCamera(){
            if (m_arSessionInstance == null)
            {
                m_arSessionInstance = Instantiate(m_ARSessionPrefab);
            }
            if (m_xrOriginInstance == null)
            {
                m_xrOriginInstance = Instantiate(m_XROriginPrefab);
            }
        }

        public void DisableARCamera(){
            if (m_arSessionInstance != null)
            {
                ARSession arSession = m_arSessionInstance.GetComponent<ARSession>();
                arSession.enabled = false;
                arSession.Reset();
                arSession = null;
                Destroy(m_arSessionInstance);
                m_arSessionInstance = null;
            }

            if (m_xrOriginInstance != null)
            {
                Destroy(m_xrOriginInstance);
                m_xrOriginInstance = null;
            }
        }

        // an exception while loading reports an error to the caller instead of leaving it waiting
        async public void LoadGltf(string filePath)
        {
            try { await LoadGltfAsync(filePath); }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError("Loading glTF failed: " + e.Message + " " + e.StackTrace);
                if (onGlTFLoadError != null){
                    onGlTFLoadError();
                }
            }
        }

        private async System.Threading.Tasks.Task LoadGltfAsync(string filePath)
        {
            mpegGltfImport = new MpegGltfImport();
            bool success = await mpegGltfImport.LoadGltfAsync(filePath);

            if (success)
            {
                var root = mpegGltfImport.Import.GetSourceRoot();
                SceneUsesAnchors = (root.extensionsUsed != null) && (Array.IndexOf(root.extensionsUsed, "MPEG_anchor") >= 0);
                if (this.ARCameraEnabled){
                    if (!UnityEngine.XR.XRSettings.enabled){
                        Debug.LogWarning("XR not available");
                    }
                    EnableARCamera();
                }
                else if (SceneUsesAnchors && QuestRig.Active)
                {
                    // a headset session has no ARCore plane detection: MPEG_anchor nodes stay where the scene puts them
                    Debug.LogWarning("MPEG_anchor is not resolved on a headset");
                }
                await mpegGltfImport.InstantiateMainSceneAsync(transform);

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

                if (!this.ARCameraEnabled && !QuestRig.Active){
                    ConfigureInitialCamera();
                }
                EnsureAudioListenerExists();

                if (onGlTFLoadComplete != null){
                    onGlTFLoadComplete();
                }

            }
            else
            {
                UnityEngine.Debug.LogError("Loading glTF failed! " + filePath);
                mpegGltfImport = null; // can't call gltf.Dispose() if we didn't run an instantiator.
                if (onGlTFLoadError != null){
                    onGlTFLoadError();
                }

            }
        }

        public void UnloadGltfScene()
        {
            // Dispose of all media players
            bool wasAR = this.ARCameraEnabled;
            foreach (var mp in VirtualSceneGraph.GetAllMediaPlayers())
            {
                if (mp == null) continue;
                var holder = mp.gameObject;
                mp.Dispose();
                // the object that was created only to hold the player goes with it
                if (holder != null && holder.GetComponents<Component>().Length <= 2) Destroy(holder);
            }

            VirtualSceneGraph.ResetAll();

            mpegGltfImport?.Dispose();

            if (wasAR)
            {
                DisableARCamera();
            }
            Camera[] _cameras = FindObjectsByType(typeof(Camera), FindObjectsSortMode.None) as Camera[];
            for(int i = 0; i < _cameras.Length; i++)
            {
                Destroy(_cameras[i]);
            }

        }

        void Update()
        {
            // media with autoplay start as soon as they are ready, the media of an autoplayGroup together
            MediaPlayer.UpdateAutoplay(VirtualSceneGraph.GetAllMediaPlayers());
        }

        void OnDestroy()
        {
            foreach (var mp in VirtualSceneGraph.GetAllMediaPlayers())
            {
                mp.Dispose();
            }            
        }


    }
}