using Eclipse.Services;
using HarmonyLib;
using Il2CppInterop.Runtime;
using ProjectM.Network;
using ProjectM.UI;
using System.Text.RegularExpressions;
using Unity.Collections;
using Unity.Entities;

namespace Eclipse.Patches;

[HarmonyPatch]
internal static class ClientChatSystemPatch
{
    static EntityManager EntityManager => Core.EntityManager;

    static readonly bool ShouldInitialize = Plugin.Leveling || Plugin.Expertise || Plugin.Legacies;
    static bool UserRegistered = false;

    static readonly Regex regexMatch = new(@"^\[\d+\]:");
    static readonly Regex regexExtract = new(@"^\[(\d+)\]:");

    static readonly ComponentType[] NetworkEventComponents =
    [
        ComponentType.ReadOnly(Il2CppType.Of<FromCharacter>()),
        ComponentType.ReadOnly(Il2CppType.Of<NetworkEventType>()),
        ComponentType.ReadOnly(Il2CppType.Of<SendNetworkEventTag>()),
        ComponentType.ReadOnly(Il2CppType.Of<ChatMessageEvent>())
    ];

    static readonly NetworkEventType networkEventType = new()
    {
        IsAdminEvent = false,
        EventId = NetworkEvents.EventId_ChatMessageEvent,
        IsDebugEvent = false,
    };

    public static Entity localCharacter = Entity.Null;
    public static Entity localUser = Entity.Null;
    public enum NetworkEventSubType
    {
        RegisterUser,
        ProgressToClient,
    }

    [HarmonyPatch(typeof(ClientChatSystem), nameof(ClientChatSystem.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(ClientChatSystem __instance)
    {
        if (!Core.hasInitialized || !ShouldInitialize) return;

        if (!UserRegistered && localCharacter != Entity.Null && localUser != Entity.Null)
        {
            UserRegistered = true;
            try
            {
                string message = localUser.Read<User>().PlatformId.ToString();
                SendMessage(NetworkEventSubType.RegisterUser, message);
            }
            catch (Exception ex)
            {
                Core.Log.LogError($"{MyPluginInfo.PLUGIN_NAME}[{MyPluginInfo.PLUGIN_VERSION}] failed to register user... {ex}");
            }
        }

        NativeArray<Entity> entities = __instance.__query_172511197_1.ToEntityArray(Allocator.Temp);
        try
        {
            foreach (Entity entity in entities)
            {
                if (entity.Has<ChatMessageServerEvent>())
                {
                    ChatMessageServerEvent chatMessage = entity.Read<ChatMessageServerEvent>();
                    string message = chatMessage.MessageText.Value;

                    if (regexMatch.IsMatch(message))
                    {
                        HandleServerMessage(message);
                        Core.Log.LogInfo($"Received progress: {message}");
                        CanvasService.PlayerData = CanvasService.ParseString(message[1..]);
                        EntityManager.DestroyEntity(entity);
                        if (!CanvasService.Active) Core.StartCoroutine(CanvasService.CanvasUpdateLoop());
                    }
                }
            }
        }
        finally
        {
            entities.Dispose();
        }
    }
    static void SendMessage(NetworkEventSubType subType, string message)
    {
        ChatMessageEvent chatMessageEvent = new()
        {
            MessageText = new FixedString512Bytes($"[{(int)subType}]:{message}"),
            MessageType = ChatMessageType.Local,
            ReceiverEntity = localUser.Read<NetworkId>()
        };

        Entity networkEntity = EntityManager.CreateEntity(NetworkEventComponents);
        networkEntity.Write(new FromCharacter { Character = localCharacter, User = localUser });
        networkEntity.Write(networkEventType);
        networkEntity.Write(chatMessageEvent);
    }
    static void HandleServerMessage(string message)
    {
        int eventType = int.Parse(regexExtract.Match(message).Groups[1].Value);
        switch (eventType)
        {
            case (int)NetworkEventSubType.ProgressToClient:
                CanvasService.PlayerData = CanvasService.ParseString(regexExtract.Replace(message, ""));
                if (!CanvasService.Active) Core.StartCoroutine(CanvasService.CanvasUpdateLoop());
                break;
        }
    }
}
