using System.Collections.Generic;
using UnityEngine;
using rt.xr.unity;
using System;

[System.Serializable] public struct glTFFile
{
    public string name;
    public string path;
}

public class GlTFListLoader : MonoBehaviour
{
    [SerializeField] private List<glTFFile> m_Items;
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
        for(int i = 0; i < m_Items.Count; i++)
        {
            GlTFListItem _itm = Instantiate(m_GlTFItemPrefab, m_ListItemLocation);
            _itm.SetProperties(LoadGltfScene, m_Items[i]);
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