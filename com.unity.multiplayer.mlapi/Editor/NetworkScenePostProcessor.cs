using MLAPI;
using MLAPI.Hashing;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UnityEditor
{
    internal class NetworkScenePostProcessor : IProcessSceneWithReport
    {
        public int callbackOrder => int.MaxValue;

        public void OnProcessScene(Scene scene, BuildReport report)
        {
            var networkObjects = Object.FindObjectsOfType<NetworkObject>();
            foreach (var networkObject in networkObjects)
            {
                if (networkObject.IsSceneObject != null)
                {
                    continue;
                }

                var globalObjectId = GlobalObjectId.GetGlobalObjectIdSlow(networkObject);
                var gObjIdHash64 = XXHash.Hash64(globalObjectId.ToString());

                networkObject.NetworkInstanceId = gObjIdHash64;
            }
        }
    }
}
