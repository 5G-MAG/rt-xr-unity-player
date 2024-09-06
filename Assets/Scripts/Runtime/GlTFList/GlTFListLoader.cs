using System.Collections.Generic;
using UnityEngine;
using rt.xr.unity;
using System;
using System.IO;

[System.Serializable] public struct glTFFile
{
    public string name;
    public string path;
}

public class GlTFListLoader : MonoBehaviour
{
    [SerializeField] private GlTFListItem m_GlTFItemPrefab;
    [SerializeField] private RectTransform m_ListItemLocation;
    [SerializeField] private GameObject m_BackBtn;

    private SceneViewer m_Viewer;
    private List<GlTFListItem> m_ListItems;

    private string ResolveGltfDocumentURI(string root, string path)
    {
        // relative path is relative to playlist
        if (path.StartsWith('.')){
            return root + path.Substring(2);
        }
        return path;
    }

    private static string GetParentUriString(Uri uri)
    {
        return uri.AbsoluteUri.Remove(uri.AbsoluteUri.Length - uri.Segments[uri.Segments.Length -1].Length - uri.Query.Length);
    }

    private void OnEnable()
    {
        m_Viewer = FindObjectOfType<SceneViewer>();
        if(m_Viewer == null)
        {
            Debug.LogError("Can't load GlTF list items, Viewer is null");
            return;
        }
        
        m_BackBtn.SetActive(false);
        m_ListItems = new List<GlTFListItem>();

        string m_PlaylistUri = m_Viewer.ConfigFileLocation;
        if(m_PlaylistUri == "")
        {
            m_PlaylistUri = Path.Combine(Application.persistentDataPath, "Paths");
        }

        Debug.LogWarning("Loading xr player config file: "+m_PlaylistUri);
        
        if(!System.IO.File.Exists(m_PlaylistUri)){
            Debug.LogError("Config file not found: "+m_PlaylistUri);
            m_Viewer.showLog = true;
            return;
        }

        string _defaultRootLocation = GetParentUriString(new Uri(m_PlaylistUri));
        string[] _lines = File.ReadAllLines(m_PlaylistUri);
        
        // Create list of button based path list
        for(int i = 0; i < _lines.Length; i++)
        {
            GlTFListItem _itm = Instantiate(m_GlTFItemPrefab, m_ListItemLocation);
            
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

                _itm.SetProperties(LoadGltfScene, _file);
                m_ListItems.Add(_itm);
            } catch (Exception e)
            {
                Debug.LogError(e);
            }
        }
    }

    private void LoadGltfScene(string _path)
    {
        try
        {
            m_Viewer.LoadGltf(_path);
            m_BackBtn.SetActive(true);
            HideList();
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogError(e);
        }
    }

    private void ShowList()
    {
        for (int i = 0; i < m_ListItems.Count; i++)
        {
            m_ListItems[i].gameObject.SetActive(true);
        }
    }

    private void HideList()
    {
        for(int i = 0; i < m_ListItems.Count; i++)
        {
            m_ListItems[i].gameObject.SetActive(false);
        }
    }

    public void OnBackButtonPressed()
    {
        m_Viewer.UnloadGltfScene();
        m_BackBtn.SetActive(false);
        ShowList();
    }

    private void OnDisable()
    {
        m_Viewer = null;
        if(m_ListItems != null)
        {
            for(int i = 0; i < m_ListItems.Count; i++)
            {
                GlTFListItem _itm = m_ListItems[i];
                Destroy(_itm.gameObject);
            }
        }
    }
}