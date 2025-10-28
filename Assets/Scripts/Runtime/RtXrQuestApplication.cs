using System;
using System.IO;
using UnityEngine;

using rt.xr.unity;

public class RtXrQuestApplication : MonoBehaviour
{
    [SerializeField] 
    private string m_gltfSceneLocation;
    private SceneViewer m_Viewer;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        m_Viewer = FindFirstObjectByType<SceneViewer>();
        if(m_Viewer == null)
        {
            Debug.LogError("RtXrQuestApp - SceneViewer component missing");
            return;
        }

        if (m_gltfSceneLocation == ""){
            m_gltfSceneLocation = Path.Combine(Application.persistentDataPath, "default.gltf");
        }
        Debug.Log("RtXrQuestApp - Loading " + m_gltfSceneLocation);
        m_Viewer.LoadGltf(m_gltfSceneLocation);
   
    }

}
