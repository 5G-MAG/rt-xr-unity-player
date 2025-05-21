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
            mpegGltfImport = new MpegGltfImport();
            bool success = await mpegGltfImport.LoadGltfAsync(filePath);

            if (success)
            {
                if (this.PassThroughEnabled){
                    if (!UnityEngine.XR.XRSettings.enabled){
                        Debug.LogWarning("XR not available");
                    }
                    EnableARCamera();
                }
                await mpegGltfImport.InstantiateMainSceneAsync(transform);

/*
                if (autoplayAnimation)
                {
                    var legacyAnimation = instantiator.sceneInstance.legacyAnimation;
                    if (legacyAnimation != null)
                    {
                        legacyAnimation.Play();
                    }
                }
*/

                // if (!this.PassThroughEnabled){                
                //     ConfigureInitialCamera();
                // }

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

            // if (Input.GetKeyDown(KeyCode.Tab))
            // {
            //     ConfigureInitialCamera();
            // }

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


    }
}