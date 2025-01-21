using BepInEx.Logging;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using Eclipse.Resources;
using Eclipse.Services;
using ProjectM;
using ProjectM.Physics;
using ProjectM.Scripting;
using ProjectM.UI;
using System.Collections;
using Unity.Entities;
using UnityEngine;

namespace Eclipse;
internal class Core
{
    public static World _client;
    public static EntityManager EntityManager => _client.EntityManager;
    public static ClientScriptMapper ClientScriptMapper { get; internal set; }
    public static ClientGameManager ClientGameManager => ClientScriptMapper._ClientGameManager;
    public static CanvasService CanvasService { get; internal set; }
    public static PrefabCollectionSystem PrefabCollectionSystem { get; internal set; }
    public static GameDataSystem GameDataSystem { get; internal set; }
    public static ManagedDataSystem ManagedDataSystem { get; internal set; }
    public static UIDataSystem UIDataSystem { get; internal set; }
    public static ServerTime ServerTime => ClientGameManager.ServerTime;
    public static ManualLogSource Log => Plugin.LogInstance;

    static MonoBehaviour _monoBehaviour;
    public static byte[] NEW_SHARED_KEY { get; internal set; }

    public static bool _initialized = false;
    public static void Initialize(GameDataManager __instance)
    {
        if (_initialized) return;

        _client = __instance.World;
        _ = new Localization();

        PrefabCollectionSystem = _client.GetExistingSystemManaged<PrefabCollectionSystem>();
        ManagedDataSystem = _client.GetExistingSystemManaged<ManagedDataSystem>();
        GameDataSystem = _client.GetExistingSystemManaged<GameDataSystem>();
        ClientScriptMapper = _client.GetExistingSystemManaged<ClientScriptMapper>();

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

        NEW_SHARED_KEY = Convert.FromBase64String(SecretManager.GetNewSharedKey());

        _initialized = true;
    }
    public static void SetCanvas(UICanvasBase canvas)
    {
        CanvasService = new(canvas);
    }
    public static void StartCoroutine(IEnumerator routine)
    {
        if (_monoBehaviour == null)
        {
            var go = new GameObject("Eclipse");
            _monoBehaviour = go.AddComponent<IgnorePhysicsDebugSystem>();
            UnityEngine.Object.DontDestroyOnLoad(go);
        }

        _monoBehaviour.StartCoroutine(routine.WrapToIl2Cpp());
    }
}