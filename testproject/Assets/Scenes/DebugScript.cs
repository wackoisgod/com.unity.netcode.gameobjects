using System.Collections.Generic;
using UnityEngine;
using MLAPI;

public class DebugScript : NetworkBehaviour
{
    private static DebugScript s_Singleton = null;
    private static List<string> s_TextList = new List<string>();

    public TextMesh InfoTextMesh;

    private void Awake()
    {
        if (s_Singleton == null)
        {
            s_Singleton = this;
        }
    }

    private void OnGUI()
    {
        if (s_Singleton != this)
        {
            return;
        }

        GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one * 3);
        GUI.color = Color.red;

        foreach (var infoText in s_TextList)
        {
            GUILayout.Label(infoText);
        }
    }

    private void Start()
    {
        var infoText = $"{name}:{NetworkObject.NetworkInstanceId}";
        infoText += $"\n{NetworkObject.GlobalObjectIdHash32} --- {NetworkObject.GlobalObjectIdString}";
        InfoTextMesh.text = infoText;
        s_TextList.Add(infoText);
        print(infoText);
    }

    public override void NetworkStart()
    {
        var infoText = $"{name}:{NetworkObject.NetworkInstanceId}";
        infoText += $"\nIsSpawned: {NetworkObject.IsSpawned}";
        infoText += $"\nIsSceneObject: {NetworkObject.IsSceneObject}";
        InfoTextMesh.text = infoText;
    }
}
