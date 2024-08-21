using Eclipse.Services;
using HarmonyLib;
using Il2CppInterop.Runtime;
using ProjectM;
using ProjectM.Network;
using ProjectM.Shared.Systems;
using ProjectM.UI;
using Unity.Collections;
using Unity.Entities;

namespace Eclipse.Patches;

[HarmonyPatch]
internal static class InitializationPatches
{
    static EntityManager EntityManager => Core.EntityManager;

    static readonly bool ShouldInitialize = Plugin.Leveling || Plugin.Expertise || Plugin.Legacies; // will use operators with other bools as options are added in the future
    static bool SetCanvas = false;
    //static bool SetPlayer = false;
    static bool OptedIn = false;

    static readonly ComponentType[] NetworkComponents =
    [
        ComponentType.ReadOnly(Il2CppType.Of<NetworkEventType>()),
        ComponentType.ReadOnly(Il2CppType.Of<SendNetworkEventTag>()),
        ComponentType.ReadOnly(Il2CppType.Of<ChatMessageEvent>())
    ];

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

    /*
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
    */

    [HarmonyPatch(typeof(ClientChatSystem), nameof(ClientChatSystem.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(ClientChatSystem __instance)
    {
        if (!Core.hasInitialized) return;
        if (!OptedIn)
        {
            OptedIn = true;
            ClientSystemChatUtils.AddLocalMessage(__instance.EntityManager, "Eclipse client message...", ServerChatMessageType.Local);
        }
        NativeArray<Entity> messages = __instance.__query_172511197_1.ToEntityArray(Allocator.Temp);
        try
        {
            foreach (Entity message in messages)
            {
                Core.Log.LogInfo($"Handling message...");
                if (message.Has<ChatMessageServerEvent>())
                {
                    ChatMessageServerEvent chatMessage = message.Read<ChatMessageServerEvent>();
                    if (chatMessage.MessageText.IsEmpty) continue;
                    string messageText = chatMessage.MessageText.Value;
                    if (messageText.StartsWith("+") && messageText.Length == 15)
                    {
                        Core.Log.LogInfo($"Received progress: {messageText}");
                        CanvasService.PlayerData = CanvasService.ParseString(messageText[1..]);
                        EntityManager.DestroyEntity(message);
                        if (!CanvasService.Active) Core.StartCoroutine(CanvasService.CanvasUpdateLoop());
                    }
                    else
                    {
                        Core.Log.LogInfo($"Received message: {messageText} | {messageText.Length}");
                    }
                }
            }
        }
        finally
        {
            messages.Dispose();
        }   
    }
}
