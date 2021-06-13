using MLAPI;
using UnityEngine;

public class ForwardRightNetBhv : NetworkBehaviour
{
    public float ForwardMultiplier = 16;
    public float RightMultiplier = 90;

    private Vector3 m_InitPos;
    private Quaternion m_InitRot;

    private void Awake()
    {
        m_InitPos = transform.position;
        m_InitRot = transform.rotation;
    }

    private void Update()
    {
        if (!NetworkManager.IsConnectedClient)
        {
            if (NetworkManager.IsListening)
            {
                transform.Translate(0, 0, Time.deltaTime * ForwardMultiplier);
                transform.Rotate(0, Time.deltaTime * RightMultiplier, 0);
            }
            else
            {
                transform.position = m_InitPos;
                transform.rotation = m_InitRot;
            }
        }
    }
}
