using System.Collections;
using MLAPI;
using UnityEngine;

public class NetworkControlsUI : MonoBehaviour
{
    public NetworkObject MovingCubeNetObj;
    public GameObject SimpleCapsulePrefab;
    public GameObject ComplexCapsulePrefab;
    public GameObject ComplexSpherePrefab;

    private NetworkManager m_NetworkManager;
    private string m_TestOneStatus;
    private string m_TestTwoStatus;
    private GameObject m_SimpleCapsuleInstance;
    private GameObject m_ComplexCapsuleInstance;
    private GameObject m_ComplexSphereInstance;

    private void Awake()
    {
        m_NetworkManager = GetComponent<NetworkManager>();
    }

    private void OnGUI()
    {
#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
        GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one * 2);
#endif // UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX

        if (m_NetworkManager.IsListening && !m_NetworkManager.IsConnectedClient)
        {
            if (GUILayout.Button("Stop Host"))
            {
                m_NetworkManager.StopHost();
            }

            if (string.IsNullOrEmpty(m_TestOneStatus) && string.IsNullOrEmpty(m_TestTwoStatus))
            {
                if (GUILayout.Button("Test1.Run()"))
                {
                    StartCoroutine(RunTestOne());
                }

                if (GUILayout.Button("Test2.Run()"))
                {
                    StartCoroutine(RunTestTwo());
                }
            }
            else
            {
                GUILayout.Label($"Test1: {m_TestOneStatus}");
                GUILayout.Label($"Test2: {m_TestTwoStatus}");
            }
        }
        else if (m_NetworkManager.IsConnectedClient)
        {
            if (GUILayout.Button("Disconnect Client"))
            {
                m_NetworkManager.StopClient();
            }
        }
        else
        {
            if (GUILayout.Button("Listen Host"))
            {
                m_NetworkManager.StartHost();
            }

            if (GUILayout.Button("Connect Client"))
            {
                m_NetworkManager.StartClient();
            }
        }
    }

    private IEnumerator RunTestOne()
    {
        m_TestOneStatus = "Run";
        yield return new WaitForSeconds(1);

        m_TestOneStatus += " > Spawn Simple Capsule";
        m_SimpleCapsuleInstance = Instantiate(SimpleCapsulePrefab);
        var sCapsuleNetObj = m_SimpleCapsuleInstance.GetComponent<NetworkObject>();
        sCapsuleNetObj.Spawn();
        yield return new WaitForSeconds(3);

        m_TestOneStatus += " > Reparent Capsule";
        sCapsuleNetObj.TrySetParent(MovingCubeNetObj, false);
        yield return new WaitForSeconds(3);

        m_TestOneStatus += " > Despawn/Destroy";
        sCapsuleNetObj.Despawn(/* destroy = */ true);
        yield return new WaitForSeconds(5);
        m_TestOneStatus = null;
    }

    private IEnumerator RunTestTwo()
    {
        m_TestTwoStatus = "Run";
        yield return new WaitForSeconds(1);

        m_TestTwoStatus += " > Spawn Complex Capsule";
        m_ComplexCapsuleInstance = Instantiate(ComplexCapsulePrefab);
        var sCapsuleNetObj = m_ComplexCapsuleInstance.GetComponent<NetworkObject>();
        sCapsuleNetObj.Spawn();
        yield return new WaitForSeconds(3);

        m_TestTwoStatus += " > Reparent Capsule";
        sCapsuleNetObj.TrySetParent(MovingCubeNetObj, false);
        yield return new WaitForSeconds(3);

        m_TestTwoStatus += " > Spawn Complex Sphere";
        m_ComplexSphereInstance = Instantiate(ComplexSpherePrefab);
        var sSphereNetObj = m_ComplexSphereInstance.GetComponent<NetworkObject>();
        sSphereNetObj.Spawn();
        yield return new WaitForSeconds(3);

        m_TestTwoStatus += " > Reparent Sphere";
        sSphereNetObj.TrySetParent(sCapsuleNetObj, false);
        yield return new WaitForSeconds(3);

        m_TestTwoStatus += " > Despawn/Destroy";
        sSphereNetObj.Despawn(/* destroy = */ true);
        sCapsuleNetObj.Despawn(/* destroy = */ true);
        yield return new WaitForSeconds(5);
        m_TestTwoStatus = null;
    }
}
