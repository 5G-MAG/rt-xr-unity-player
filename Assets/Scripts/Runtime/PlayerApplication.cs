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

    private static string GetParentUriString(Uri uri)
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

    public void onGlTFLoadComplete(){
        Debug.LogWarning("onGlTFLoadComplete");
        EnableCameraController();
    }

    /*
    public void onGlTFLoadError(){
        Debug.LogWarning("onGlTFLoadError");
    }
    */

    public static List<glTFFile> ParsePlaylist(string playlistURI)
    {
        var files = new List<glTFFile>();
        string _defaultRootLocation = GetParentUriString(new Uri(playlistURI));
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

    public string LocatePlaylist()
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

    public void EnableCameraController()
    {
        m_CameraControlDropdown.gameObject.SetActive(true);
        CameraControlTypeChanged();
    }

    public void DisableCameraController()
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
            AddOrbitCameraComponent();
        } else {
            AddFpvCameraComponent();
        }
    }

    public void AddOrbitCameraComponent(){
        FirstPersonCameraController fpv = GetComponent<FirstPersonCameraController>();
        if (fpv != null){
            Destroy(fpv);
        }
        gameObject.AddComponent(typeof(OrbitControl));
    }

    public void AddFpvCameraComponent(){
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

    // Start is called before the first frame update
    void Start()
    {

        m_Viewer = FindObjectOfType<SceneViewer>();
        if(m_Viewer == null)
        {
            Debug.LogError("Can't load GlTF list items, Viewer is null");
            return;
        }

        m_GlTFListMenu.selected += LoadGlTFAsset;

        string playlistURI = LocatePlaylist();
        if (playlistURI != ""){
            var items = ParsePlaylist(playlistURI);
            m_GlTFListMenu.PopulateMenu(items);
            m_GlTFListMenu.ShowList();
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
            DisableCameraController();
        }
        catch (Exception e)
        {
            m_Viewer.showLog = true;
            UnityEngine.Debug.LogError(e);
        }
    }

}
