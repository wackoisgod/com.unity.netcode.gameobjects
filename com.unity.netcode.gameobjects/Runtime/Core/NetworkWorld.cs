using System.Collections;
using System.Collections.Generic;

namespace Unity.Netcode
{
    public class NetworkWorld
    {
        public NetworkManager GetManager() => new NetworkManager();
        public NetworkTransport GetTransport() => new NetworkTransport();
        public NativeArray<NetworkObject> GetNetworkObjects() => default;
    }
}
