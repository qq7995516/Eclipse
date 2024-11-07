using BepInEx.Logging;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using Eclipse.Services;
using ProjectM;
using ProjectM.Physics;
using ProjectM.UI;
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
        

        /*
        foreach (var kvp in Client.m_SystemLookup)
        {
            Il2CppSystem.Type systemType = kvp.Key;
            ComponentSystemBase systemBase = kvp.Value;
            if (systemBase.EntityQueries.Length == 0) continue;

            Core.Log.LogInfo("=============================");
            Core.Log.LogInfo(systemType.FullName);
            foreach (EntityQuery query in systemBase.EntityQueries)
            {
                EntityQueryDesc entityQueryDesc = query.GetEntityQueryDesc();
                Core.Log.LogInfo($" All: {string.Join(",", entityQueryDesc.All)}");
                Core.Log.LogInfo($" Any: {string.Join(",", entityQueryDesc.Any)}");
                Core.Log.LogInfo($" Absent: {string.Join(",", entityQueryDesc.Absent)}");
                Core.Log.LogInfo($" None: {string.Join(",", entityQueryDesc.None)}");
            }
            Core.Log.LogInfo("=============================");
        }
        */

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