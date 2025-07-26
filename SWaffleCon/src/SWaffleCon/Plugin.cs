using BepInEx;
using BepInEx.Logging;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWaffleCon;

[BepInAutoPlugin]
public partial class Plugin : BaseUnityPlugin
{
    internal static ManualLogSource Log { get; private set; } = null!;

    private void Awake()
    {
        Log = Logger;
        Log.LogInfo($"Plugin {Name} is loaded!");

        // Create RemoteCommandHost GameObject. 
        // This is necessary to keep the remote console server alive.
        var go = new GameObject("RemoteCommandHost");
        go.AddComponent<RemoteConsoleServer>();
        DontDestroyOnLoad(go);
    }

    public static readonly Queue<Action> mainThreadActions = new();

    private void Update()
    {
        while (mainThreadActions.Count > 0)
        {
            var action = mainThreadActions.Dequeue();
            action?.Invoke();
        }
    }
}
