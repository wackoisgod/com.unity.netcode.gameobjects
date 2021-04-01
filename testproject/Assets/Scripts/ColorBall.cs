using UnityEngine;
using MLAPI;
using MLAPI.Messaging;
using MLAPI.NetworkVariable;

public class ColorBall : NetworkBehaviour
{
    public TextMesh InfoTM;

    private Material m_Material;
    private NetworkVariable<int> m_ColorCode = new NetworkVariable<int>(0);

    private void Awake()
    {
        m_Material = GetComponent<Renderer>().material;
    }

    private void Update()
    {
        InfoTM.text = $"[NetworkObjectId: {NetworkObjectId}]";

        if (Input.GetKeyDown(KeyCode.Return))
        {
            if (NetworkManager.IsServer)
            {
                ChangeColor();
            }
            else if (NetworkManager.IsClient)
            {
                ChangeColorServerRpc();
            }
        }

        switch (m_ColorCode.Value)
        {
            case 0:
                m_Material.color = Color.white;
                break;
            case 1:
                m_Material.color = Color.red;
                break;
            case 2:
                m_Material.color = Color.green;
                break;
            case 3:
                m_Material.color = Color.blue;
                break;
            case 4:
                m_Material.color = Color.yellow;
                break;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void ChangeColorServerRpc()
    {
        ChangeColor(true);
    }

    private void ChangeColor(bool isClient = false)
    {
        m_ColorCode.Value = Time.frameCount % 4;
    }
}
