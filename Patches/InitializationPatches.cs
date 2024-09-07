using Eclipse.Services;
using HarmonyLib;
using ProjectM;
using ProjectM.Network;
using ProjectM.UI;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Eclipse.Patches;

[HarmonyPatch]
internal static class InitializationPatches
{
    static EntityManager EntityManager => Core.EntityManager;

    static readonly bool ShouldInitialize = Plugin.Leveling || Plugin.Expertise || Plugin.Legacies || Plugin.Quests; // will use operators with other bools as options are added in the future
    static bool SetCanvas = false;

    [HarmonyPatch(typeof(GameDataManager), nameof(GameDataManager.OnUpdate))]
    [HarmonyPostfix]
    static void OnUpdatePostfix(GameDataManager __instance)
    {
        try
        {
            if (ShouldInitialize && __instance.GameDataInitialized && !Core.hasInitialized)
            {
                Core.Initialize(__instance);
                if (Core.hasInitialized)
                {
                    Core.Log.LogInfo($"|{MyPluginInfo.PLUGIN_NAME}[{MyPluginInfo.PLUGIN_VERSION}] initialized on client|");
                }
            }
        }
        catch (Exception ex)
        {
            Core.Log.LogError($"{MyPluginInfo.PLUGIN_NAME}[{MyPluginInfo.PLUGIN_VERSION}] failed to initialize on client, exiting on try-catch... {ex}");
        }
    }

    [HarmonyPatch(typeof(UICanvasSystem), nameof(UICanvasSystem.UpdateHideIfDisabled))]
    [HarmonyPostfix]
    static void OnUpdatePostfix(UICanvasBase canvas)
    {
        if (!SetCanvas && Core.hasInitialized)
        {
            SetCanvas = true;
            Core.SetCanvas(canvas);
        }
    }
    
    [HarmonyPatch(typeof(CommonClientDataSystem), nameof(CommonClientDataSystem.OnUpdate))]
    [HarmonyPostfix]
    static void OnUpdatePostfix(CommonClientDataSystem __instance)
    {
        if (Core.hasInitialized)
        {
            NativeArray<Entity> entities = __instance.__query_1840110765_0.ToEntityArray(Allocator.Temp);
            try
            {
                foreach (Entity entity in entities)
                {
                    if (entity.Has<LocalUser>()) ClientChatSystemPatch.localUser = entity;
                    break;
                }
            }
            finally
            {
                entities.Dispose();
            }

            entities = __instance.__query_1840110765_1.ToEntityArray(Allocator.Temp);

            try
            {
                foreach (Entity entity in entities)
                {
                    if (entity.Has<LocalCharacter>()) ClientChatSystemPatch.localCharacter = entity;
                    break;
                }
            }
            finally
            {
                entities.Dispose();
            }
        }
    }

    [HarmonyPatch(typeof(ClientBootstrapSystem), nameof(ClientBootstrapSystem.OnDestroy))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(ClientBootstrapSystem __instance)
    {
        CanvasService.KillSwitch = true;
        CanvasService.Active = false;

        ClientChatSystemPatch.UserRegistered = false;
        ClientChatSystemPatch.localCharacter = Entity.Null;
        ClientChatSystemPatch.localUser = Entity.Null;

        SetCanvas = false;
        Core.hasInitialized = false;

        foreach(GameObject gameObject in CanvasService.ActiveObjects) // destroy to let resolution be changed and elements get recreated to match new scaling?
        {
            if (gameObject != null)
            {
                GameObject.Destroy(gameObject);
            }
        }
        
        CanvasService.ActiveObjects.Clear();
    }
}
