/*
 * Copyright (c) 2024 MotionSpell
 * Licensed under the License terms and conditions for use, reproduction,
 * and distribution of 5GMAG software (the “License”).
 * You may not use this file except in compliance with the License.
 * You may obtain a copy of the License at https://www.5g-mag.com/license .
 * Unless required by applicable law or agreed to in writing, software distributed under the License is
 * distributed on an “AS IS” BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and limitations under the License.
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using rt.xr.unity;
using UnityEngine.UI;
using TMPro;

public class PlayerApplication : MonoBehaviour
{

    [SerializeField] private GlTFListLoader m_GlTFListMenu;
    [SerializeField] private SceneViewer m_Viewer;
    [SerializeField] private GameObject m_BackBtn;
    [SerializeField] private TMPro.TMP_Dropdown m_CameraControlDropdown;

    void Start()
    {

        m_Viewer = FindObjectOfType<SceneViewer>();
        if(m_Viewer == null)
        {
            Debug.LogError("Can't load GlTF list items, Viewer is null");
            return;
        }
        resetCameraBackground();

        m_GlTFListMenu.selected += LoadGlTFAsset;

        string playlistURI = locatePlaylist();
        if (playlistURI != ""){
            var items = parsePlaylist(playlistURI);
            m_GlTFListMenu.PopulateMenu(items);
            m_GlTFListMenu.ShowList();
        }
    }

    private static string getParentUriString(Uri uri)
    {
        return uri.AbsoluteUri.Remove(uri.AbsoluteUri.Length - uri.Segments[uri.Segments.Length -1].Length - uri.Query.Length);
    }

    private void Awake()
    {
        m_BackBtn.gameObject.SetActive(false);
        m_CameraControlDropdown.gameObject.SetActive(false);
        m_Viewer.onGlTFLoadComplete = onGlTFLoadComplete;
        // m_Viewer.onGlTFLoadError = onGlTFLoadError;
    }

    private void resetCameraBackground(){
        Camera cam = m_Viewer.GetMainCamera();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
    }

    private void onGlTFLoadComplete(){
        if (!m_Viewer.ARCameraEnabled){
            enableCameraController();
        } else {
            disableCameraController();
        }
    }

    /*
    public void onGlTFLoadError(){
        Debug.LogWarning("onGlTFLoadError");
    }
    */

    private static List<glTFFile> parsePlaylist(string playlistURI)
    {
        var files = new List<glTFFile>();
        string _defaultRootLocation = getParentUriString(new Uri(playlistURI));
        string[] _lines = File.ReadAllLines(playlistURI);

        for(int i = 0; i < _lines.Length; i++)
        {
            try
            {
                string line = _lines[i];
  
                Uri uri;
                if(!Uri.TryCreate(line, UriKind.Absolute, out uri)){
                    System.UriBuilder uriBuilder = new System.UriBuilder(_defaultRootLocation);
                    uriBuilder.Path += line;
                    uri = uriBuilder.Uri;
                }

                string name = Uri.UnescapeDataString(uri.Segments[uri.Segments.Length - 1]);

                glTFFile _file = new glTFFile
                {
                    path = uri.ToString(),
                    name = name
                };

                files.Add(_file);

            } catch (Exception e)
            {
                Debug.LogError(e);
            }
        }
        return files;
    }

    private string locatePlaylist()
    {
        string playlistURI = "";
        
        if(m_Viewer.ConfigFileLocation == "")
        {
            playlistURI = Path.Combine(Application.persistentDataPath, "Paths");
        } else {
            playlistURI = m_Viewer.ConfigFileLocation;
        }

        Debug.LogWarning("Loading xr player config file: "+playlistURI);

        if(!System.IO.File.Exists(playlistURI)){
            Debug.LogError("Config file not found: " + playlistURI);
            m_Viewer.showLog = true;
            return "";
        }

        return playlistURI;
    }

    private void enableCameraController()
    {
        m_CameraControlDropdown.gameObject.SetActive(true);
        CameraControlTypeChanged();
    }

    private void disableCameraController()
    {
        if (m_CameraControlDropdown.value == 0){
            Destroy(GetComponent<OrbitControl>());
        } else {
            Destroy(GetComponent<FirstPersonCameraController>());
        }
        m_CameraControlDropdown.gameObject.SetActive(false);
    }

    public void CameraControlTypeChanged(){
        if (m_CameraControlDropdown.value == 0){
            addOrbitCameraComponent();
        } else {
            addFpvCameraComponent();
        }
    }

    private void addOrbitCameraComponent(){
        FirstPersonCameraController fpv = GetComponent<FirstPersonCameraController>();
        if (fpv != null){
            Destroy(fpv);
        }
        gameObject.AddComponent(typeof(OrbitControl));
    }

    private void addFpvCameraComponent(){
        OrbitControl orbit = GetComponent<OrbitControl>();
        if (orbit != null){
            Destroy(orbit);
        }
        gameObject.AddComponent(typeof(FirstPersonCameraController));
    }
    
    public void LoadGlTFAsset(string path)
    {
        try
        {
            m_Viewer.LoadGltf(path);
            m_GlTFListMenu.HideList();
            m_BackBtn.gameObject.SetActive(true);
        }
        catch (Exception e)
        {
            m_Viewer.showLog = true;
            UnityEngine.Debug.LogError(e);
        }
    }

    public void UnloadGlTFAsset()
    {
        try
        {
            m_Viewer.UnloadGltfScene();
            m_CameraControlDropdown.gameObject.SetActive(false);
            m_BackBtn.gameObject.SetActive(false);
            m_GlTFListMenu.ShowList();
            disableCameraController();
            resetCameraBackground();
        }
        catch (Exception e)
        {
            m_Viewer.showLog = true;
            UnityEngine.Debug.LogError(e);
        }
    }

}
