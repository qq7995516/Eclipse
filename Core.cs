using BepInEx.Logging;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using Eclipse.Services;
using ProjectM;
using ProjectM.Physics;
using ProjectM.UI;
using Stunlock.Localization;
using System.Collections;
using Unity.Entities;
using UnityEngine;

namespace Eclipse;
internal class Core
{
    static World Client;
    public static EntityManager EntityManager => Client.EntityManager;
    public static CanvasService CanvasService { get; internal set; }
    public static PrefabCollectionSystem PrefabCollectionSystem { get; internal set; }
    public static ManualLogSource Log => Plugin.LogInstance;

    static MonoBehaviour monoBehaviour;

    public static bool hasInitialized = false;
    public static void Initialize(GameDataManager __instance)
    {
        if (hasInitialized) return;

        Client = __instance.World;
        PrefabCollectionSystem = Client.GetExistingSystemManaged<PrefabCollectionSystem>();

        hasInitialized = true;
    }
    public static void SetCanvas(UICanvasBase canvas)
    {
        CanvasService = new(canvas);
    }
    public static void StartCoroutine(IEnumerator routine)
    {
        if (monoBehaviour == null)
        {
            var go = new GameObject("Eclipse");
            monoBehaviour = go.AddComponent<IgnorePhysicsDebugSystem>();
            UnityEngine.Object.DontDestroyOnLoad(go);
        }
        monoBehaviour.StartCoroutine(routine.WrapToIl2Cpp());
    }
}