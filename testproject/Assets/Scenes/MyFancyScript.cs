using System;
using System.IO;
using MLAPI.Hashing;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class MyFancyScript : MonoBehaviour
{
#if UNITY_EDITOR
    public void OnValidate()
    {
        print("onvalidate");
        var assetPath = AssetDatabase.GetAssetPath(this);
        print(assetPath);
        DoTheThing(assetPath);
    }
#endif

    public void DoTheThing(string assetPath)
    {
        print($"before: {FancyGuid}");

        var tokens = FancyGuid?.Split(':') ?? new string[0];
        if (tokens.Length == 0)
        {
            tokens = new string[2];
        }
        else if (tokens.Length == 1)
        {
            tokens = new string[] { tokens[0], string.Empty };
        }

        if (!string.IsNullOrEmpty(assetPath))
        {
            tokens[0] = assetPath;
        }

        if (string.IsNullOrEmpty(tokens[1]))
        {
            tokens[1] = Guid.NewGuid().ToString();
        }

        FancyGuid = $"{tokens[0]}:{tokens[1]}";
        NetworkPrefabId = XXHash.Hash64(FancyGuid);

        print($"after: {FancyGuid}");

        // print($"do the thing: {assetPath}");
        // if (string.IsNullOrEmpty(FancyGuid))
        // {
        //     FancyGuid = $"{assetPath}:{Guid.NewGuid()}";
        // }
        // else
        // {
        //     var tokens = FancyGuid.Split(':');
        //     if (tokens.Length == 0 || string.IsNullOrEmpty(tokens[0]) || (!string.IsNullOrEmpty(assetPath) && assetPath != tokens[0]))
        //     {
        //         FancyGuid = $"{assetPath}:{Guid.NewGuid()}";
        //     }
        // }
        // check my name part of my id thingzzz
        // re-change it if needed
    }

    public string FancyGuid;

    [SerializeField]
    internal ulong NetworkPrefabId;

    private void Start()
    {
        print($"{name}: {FancyGuid}");
    }
}
