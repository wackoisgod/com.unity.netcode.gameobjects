using MLAPI;
using UnityEngine;

public class PrintInfo : MonoBehaviour
{
    private static string s_InfoText = string.Empty;

    private void Start()
    {
        var networkObject = GetComponent<NetworkObject>();
        var infoText = $"{name}: {networkObject.NetworkInstanceId}";
        // print(infoText);
        s_InfoText += $"{infoText}\n";
    }

    private void OnGUI()
    {
        GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one * 3);
        GUI.color = Color.black;
        GUILayout.Label(s_InfoText);
    }
}
