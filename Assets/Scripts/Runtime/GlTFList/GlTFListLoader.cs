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

        // Get the Paths.txt file
        string _pathFileName =  "Paths.txt";
        string _pathFile = Application.dataPath + "/../" + _pathFileName;

#if UNITY_ANDROID && !UNITY_EDITOR
        _pathFile = Application.persistentDataPath + "/" + _pathFileName;
#endif

        // Every lines should contains a path to a glTF file
        string[] _lines = File.ReadAllLines(_pathFile);

        // Create list of button based on detected paths
        for(int i = 0; i < _lines.Length; i++)
        {
            GlTFListItem _itm = Instantiate(m_GlTFItemPrefab, m_ListItemLocation);
            
            glTFFile _file = new glTFFile
            {
                path = _lines[i],
                name = _lines[i]
            };

            _itm.SetProperties(LoadGltfScene, _file);
            m_ListItems.Add(_itm);
        }
    }

    private void LoadGltfScene(string _path)
    {
        m_Viewer.LoadScene(_path);
        m_BackBtn.SetActive(true);
        HideList();
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
        for(int i = 0; i < m_ListItems.Count; i++)
        {
            m_ListItems[i].gameObject.SetActive(true);
        }
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