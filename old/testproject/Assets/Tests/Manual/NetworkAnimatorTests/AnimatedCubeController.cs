using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace Tests.Manual.NetworkAnimatorTests
{
    [RequireComponent(typeof(Animator))]
    public class AnimatedCubeController : NetworkBehaviour
    {
        private Animator m_Animator;
        private bool m_Rotate;
        private NetworkAnimator m_NetworkAnimator;

        private void Awake()
        {
            m_Animator = GetComponent<Animator>();
            m_NetworkAnimator = GetComponent<NetworkAnimator>();
            m_Rotate = m_Animator.GetBool("Rotate");
        }

        public override void OnNetworkSpawn()
        {
            if (!IsClient || !IsOwner)
            {
                enabled = false;
            }
        }

        private void Update()
        {
            {
                if (Input.GetKeyDown(KeyCode.C))
                {
                    ToggleRotateAnimationServerRpc();
                }
                if (Input.GetKeyDown(KeyCode.Space))
                {
                    PlayPulseAnimationServerRpc();
                }
            }
        }

        private void ToggleRotateAnimation()
        {
            m_Rotate = !m_Rotate;
            m_Animator.SetBool("Rotate", m_Rotate);
        }

        private void PlayPulseAnimation()
        {
            m_NetworkAnimator.SetTrigger("Pulse");
        }

        [ServerRpc]
        public void ToggleRotateAnimationServerRpc()
        {
            ToggleRotateAnimation();
        }

        [ServerRpc]
        public void PlayPulseAnimationServerRpc()
        {
            PlayPulseAnimation();
        }
    }
}
