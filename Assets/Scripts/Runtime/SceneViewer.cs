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

namespace rt.xr.unity
{
    using GLTFast;

    enum ArSessionInitialization {
        DISABLED,
        ENABLED,
        AUTO
    }

    public class SceneViewer : MonoBehaviour
    {

        [SerializeField]
        private bool autoplayAnimation = true;

        int sceneIndex = 0;


#nullable enable
        SceneImport? gltf;
        List<MediaPlayer>? mediaPlayers = null;
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
                        var MPEG_anchor_extensionsUsed = (gltf.GetSourceRoot().extensionsUsed != null) &&
                            (Array.IndexOf(gltf.GetSourceRoot().extensionsUsed, "MPEG_anchor") >= 0);
                        return MPEG_anchor_extensionsUsed;
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

        async public void LoadGltf(string filePath)
        {
            Uri path = new Uri(filePath, UriKind.RelativeOrAbsolute);
            
            if (!path.IsAbsoluteUri)
                path = new Uri(System.IO.Directory.GetCurrentDirectory()+"/"+path);

            Uri baseUri = UriHelper.GetBaseUri(path);

            gltf = new SceneImport();
            bool success = await gltf.Load(path);

            if (success)
            {
                Debug.LogWarning("Loaded: " + path);

                var instantiator = new GameObjectInstantiator(gltf, transform);

                if (this.PassThroughEnabled){
                    if (UnityEngine.XR.XRSettings.enabled){
                        EnableARCamera();
                    } else {
                        Debug.LogWarning("XR not available");
                    }
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

                // if (!this.PassThroughEnabled){                
                //     ConfigureInitialCamera();
                // }
                EnsureAudioListenerExists();

                Debug.LogWarning("glTF load complete");

                if (onGlTFLoadComplete != null){
                    onGlTFLoadComplete();
                }
                
            }
            else
            {
                Debug.LogError("Loading glTF failed!");
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
                    mp.Dispose();
                }
                mediaPlayers.Clear();
                mediaPlayers = null;
            }
            if (this.PassThroughEnabled){
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

            if (mediaPlayers != null)
            {
                foreach (var mp in mediaPlayers)
                {
                    if (mp.autoPlay)
                    {
                        mp.Play();
                    }
                }
            }

            // if (Input.GetKeyDown(KeyCode.Tab))
            // {
            //     ConfigureInitialCamera();
            // }

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


    }
}