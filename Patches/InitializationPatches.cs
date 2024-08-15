using Eclipse.Services;
using HarmonyLib;
using ProjectM;
using ProjectM.UI;
using Unity.Collections;
using Unity.Entities;

namespace Eclipse.Patches;

[HarmonyPatch]
internal static class InitializationPatches
{
    static readonly bool ShouldInitialize = Plugin.Leveling; // will use operators with other bools as options are added in the future
    static bool SetCanvas = false;
    static bool SetPlayer = false;

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
                    Plugin.Harmony.Unpatch(typeof(GameDataManager).GetMethod("OnUpdate"), typeof(InitializationPatches).GetMethod("OnUpdatePostfix"));
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
        if (ShouldInitialize && !SetCanvas && Core.hasInitialized)
        {
            SetCanvas = true;
            Core.SetCanvas(canvas);
            Plugin.Harmony.Unpatch(typeof(UICanvasSystem).GetMethod("UpdateHideIfDisabled"), typeof(InitializationPatches).GetMethod("OnUpdatePostfix"));
        }
    }

    [HarmonyPatch(typeof(GetCharacterHUDSystem), nameof(GetCharacterHUDSystem.OnUpdate))]
    [HarmonyPostfix]
    static void OnUpdatePostfix(GetCharacterHUDSystem __instance)
    {
        if (ShouldInitialize && !SetPlayer && Core.hasInitialized)
        {
            NativeArray<Entity> players = __instance.__query_1404660282_0.ToEntityArray(Allocator.Temp);
            try
            {
                foreach (Entity player in players)
                {
                    SetPlayer = true;
                    CanvasService.LocalCharacter = player;
                    Core.StartCoroutine(CanvasService.CanvasUpdateLoop());
                    break;
                }
            }
            finally
            {
                players.Dispose();
                Plugin.Harmony.Unpatch(typeof(GetCharacterHUDSystem).GetMethod("OnUpdate"), typeof(InitializationPatches).GetMethod("OnUpdatePostfix"));
            }
        }
    }
}
