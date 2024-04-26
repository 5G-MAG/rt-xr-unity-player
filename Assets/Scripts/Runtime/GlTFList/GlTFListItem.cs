using rt.xr.unity;
using TMPro;
using UnityEngine;

public class GlTFListItem : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI m_Txt;
    private string m_Path;

    private System.Action<string> m_OnGltfLoadRequest;

    public void SetProperties(System.Action<string> _req, glTFFile _property)
    {
        m_Txt.text = _property.name;
        m_Path = _property.path;
        m_OnGltfLoadRequest = _req;
    }

    // Called by UI
    public void LoadItem()
    {
        Debug.Log($"Loading glTF scene at {m_Path}");
        m_OnGltfLoadRequest?.Invoke(m_Path);
    }
}
