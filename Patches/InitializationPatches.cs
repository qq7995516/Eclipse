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

    static readonly bool _shouldInitialize = Plugin.Leveling || Plugin.Expertise || Plugin.Legacies || Plugin.Familiars || Plugin.Quests; // will use operators with other bools as options are added in the future
    static bool _setCanvas = false;

    [HarmonyPatch(typeof(GameDataManager), nameof(GameDataManager.OnUpdate))]
    [HarmonyPostfix]
    static void OnUpdatePostfix(GameDataManager __instance)
    {
        if (!__instance.GameDataInitialized || !__instance.World.IsCreated) return;

        try
        {
            if (_shouldInitialize && !Core._initialized)
            {
                Core.Initialize(__instance);

                if (Core._initialized)
                {
                    Core.Log.LogInfo($"|{MyPluginInfo.PLUGIN_NAME}[{MyPluginInfo.PLUGIN_VERSION}] initialized on client!");
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
        if (!_setCanvas && Core._initialized)
        {
            _setCanvas = true;

            Core.SetCanvas(canvas);
        }
    }

    [HarmonyPatch(typeof(CommonClientDataSystem), nameof(CommonClientDataSystem.OnUpdate))]
    [HarmonyPostfix]
    static void OnUpdatePostfix(CommonClientDataSystem __instance)
    {
        if (!Core._initialized) return;

        NativeArray<Entity> entities = __instance.__query_1840110765_0.ToEntityArray(Allocator.Temp);

        try
        {
            foreach (Entity entity in entities)
            {
                if (entity.Has<LocalUser>()) ClientChatSystemPatch._localUser = entity;
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
                if (entity.Has<LocalCharacter>()) ClientChatSystemPatch._localCharacter = entity;
                CanvasService._localCharacter = entity;

                break;
            }
        }
        finally
        {
            entities.Dispose();
        }
    }

    [HarmonyPatch(typeof(ClientBootstrapSystem), nameof(ClientBootstrapSystem.OnDestroy))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(ClientBootstrapSystem __instance)
    {
        CanvasService._killSwitch = true;

        CanvasService._shiftRoutine.Stop();
        CanvasService._canvasRoutine.Stop();

        CanvasService._active = false;
        CanvasService._shiftActive = false;
        CanvasService._ready = false;

        ClientChatSystemPatch._userRegistered = false;
        ClientChatSystemPatch._pending = false;
        ClientChatSystemPatch._versions = new([ClientChatSystemPatch.VERSION, ClientChatSystemPatch.V1_2_2]);

        ClientChatSystemPatch._localCharacter = Entity.Null;
        ClientChatSystemPatch._localUser = Entity.Null;
        CanvasService._version = string.Empty;

        CanvasService.ArmorStatCache.Clear();
        CanvasService.GrimoireStatCache.Clear();
        CanvasService.WeaponStatCache.Clear();

        CanvasService.OriginalArmorStatsCache.Clear();
        CanvasService.OriginalGrimoireStatsCache.Clear();
        CanvasService.OriginalWeaponStatsCache.Clear();

        _setCanvas = false;

        foreach (GameObject gameObject in CanvasService.UIObjectStates.Keys) // destroy to let resolution be changed and elements get recreated to match new scaling?
        {
            if (gameObject != null)
            {
                GameObject.Destroy(gameObject);
            }
        }

        foreach (GameObject gameObject in CanvasService.ProfessionObjects) // destroy to let resolution be changed and elements get recreated to match new scaling?
        {
            if (gameObject != null)
            {
                GameObject.Destroy(gameObject);
            }
        }

        CanvasService._abilityTooltipData = null;
        CanvasService._dailyQuestIcon = null;
        CanvasService._weeklyQuestIcon = null;

        CanvasService.UIObjectStates.Clear();
        CanvasService.ProfessionObjects.Clear();
        CanvasService.SpriteMap.Clear();

        Core.Reset();
    }
}
