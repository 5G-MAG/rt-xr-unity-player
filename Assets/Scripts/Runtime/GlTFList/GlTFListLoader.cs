using System.Collections.Generic;
using UnityEngine;
using System;

[System.Serializable] public struct glTFFile
{
    public string name;
    public string path;
}


public class GlTFListLoader : MonoBehaviour
{
    public System.Action<string> selected;

    [SerializeField] private GlTFListItem m_GlTFItemPrefab;
    [SerializeField] private RectTransform m_ListItemLocation;

    private List<GlTFListItem> m_ListItems;

    public void PopulateMenu(List<glTFFile> files)
    {
        m_ListItems = new List<GlTFListItem>();

        foreach(glTFFile file in files)
        {
            GlTFListItem _itm = Instantiate(m_GlTFItemPrefab, m_ListItemLocation);
            _itm.SetProperties(selected, file);
            m_ListItems.Add(_itm);
        }
    }

    public void ShowList()
    {
        for (int i = 0; i < m_ListItems.Count; i++)
        {
            m_ListItems[i].gameObject.SetActive(true);
        }
    }

    public void HideList()
    {
        for(int i = 0; i < m_ListItems.Count; i++)
        {
            m_ListItems[i].gameObject.SetActive(false);
        }
    }

    private void OnDisable()
    {
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