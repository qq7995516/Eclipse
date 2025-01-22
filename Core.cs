using BepInEx.Logging;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using Eclipse.Resources;
using Eclipse.Services;
using ProjectM;
using ProjectM.Physics;
using ProjectM.Scripting;
using ProjectM.Shared;
using ProjectM.UI;
using Stunlock.Core;
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

        try
        {
            ModifyPrefabs();
        }
        catch (Exception ex)
        {
            Log.LogError($"Failed to modify prefabs: {ex}");
        }

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

    static readonly PrefabGUID _copperWires = new(-456161884);
    static readonly PrefabGUID _primalEssence = new(1566989408);
    static readonly PrefabGUID _extractShardRecipe = new(1743327679);
    static readonly PrefabGUID _itemBuildingEMP = new(-1447213995);
    static readonly PrefabGUID _depletedBattery = new(1270271716);
    static readonly PrefabGUID _chargedBatteryRecipe = new(-40415372);
    static readonly PrefabGUID _batteryCharge = new(-77555820);
    static readonly PrefabGUID _itemJewelTemplate = new(1075994038);
    static readonly PrefabGUID _lesserStygian = new(2103989354);
    static readonly PrefabGUID _bloodEssence = new(862477668);
    static readonly PrefabGUID _batHide = new(1262845777);
    static readonly PrefabGUID _copperWiresRecipe = new(-2031309726);
    static void ModifyPrefabs()
    {
        if (PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(_itemBuildingEMP, out Entity prefabEntity))
        {
            if (!prefabEntity.Has<Salvageable>())
            {
                prefabEntity.AddWith((ref Salvageable salvageable) =>
                {
                    salvageable.RecipeGUID = PrefabGUID.Empty;
                    salvageable.SalvageFactor = 1f;
                    salvageable.SalvageTimer = 60f;
                });

                var recipeRequirementBuffer = EntityManager.AddBuffer<RecipeRequirementBuffer>(prefabEntity);
                recipeRequirementBuffer.Add(new RecipeRequirementBuffer { Guid = _depletedBattery, Amount = 5 });
            }
        }

        if (PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(_extractShardRecipe, out prefabEntity))
        {
            if (prefabEntity.Has<RecipeOutputBuffer>())
            {
                var recipeOutputBuffer = prefabEntity.ReadBuffer<RecipeOutputBuffer>();
                recipeOutputBuffer.Add(new RecipeOutputBuffer { Guid = _itemJewelTemplate, Amount = 1 });
            }
        }

        if (PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(_chargedBatteryRecipe, out prefabEntity))
        {
            if (prefabEntity.Has<RecipeRequirementBuffer>())
            {
                var recipeRequirementBuffer = prefabEntity.ReadBuffer<RecipeRequirementBuffer>();
                recipeRequirementBuffer.Add(new RecipeRequirementBuffer { Guid = _batteryCharge, Amount = 1 });
            }
        }

        if (PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(_copperWires, out prefabEntity))
        {
            if (!prefabEntity.Has<Salvageable>())
            {
                prefabEntity.AddWith((ref Salvageable salvageable) =>
                {
                    salvageable.RecipeGUID = PrefabGUID.Empty;
                    salvageable.SalvageFactor = 1f;
                    salvageable.SalvageTimer = 15f;
                });

                var recipeRequirementBuffer = prefabEntity.AddBuffer<RecipeRequirementBuffer>();
                recipeRequirementBuffer.Add(new RecipeRequirementBuffer { Guid = _batteryCharge, Amount = 1 });
            }
        }

        if (PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(_primalEssence, out prefabEntity))
        {
            if (!prefabEntity.Has<Salvageable>())
            {
                prefabEntity.AddWith((ref Salvageable salvageable) =>
                {
                    salvageable.RecipeGUID = PrefabGUID.Empty;
                    salvageable.SalvageFactor = 1f;
                    salvageable.SalvageTimer = 5f;
                });

                var recipeRequirementBuffer = prefabEntity.AddBuffer<RecipeRequirementBuffer>();
                recipeRequirementBuffer.Add(new RecipeRequirementBuffer { Guid = _batteryCharge, Amount = 5 });
            }
        }

        if (PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(_batHide, out prefabEntity))
        {
            if (!prefabEntity.Has<Salvageable>())
            {
                prefabEntity.Add<Salvageable>();
            }

            prefabEntity.With((ref Salvageable salvageable) =>
            {
                salvageable.RecipeGUID = PrefabGUID.Empty;
                salvageable.SalvageFactor = 1f;
                salvageable.SalvageTimer = 15f;
            });

            if (!prefabEntity.Has<RecipeRequirementBuffer>())
            {
                prefabEntity.AddBuffer<RecipeRequirementBuffer>();
            }

            var recipeRequirementBuffer = prefabEntity.ReadBuffer<RecipeRequirementBuffer>();
            recipeRequirementBuffer.Add(new RecipeRequirementBuffer { Guid = _lesserStygian, Amount = 3 });
            recipeRequirementBuffer.Add(new RecipeRequirementBuffer { Guid = _bloodEssence, Amount = 5 });
        }
    }
}