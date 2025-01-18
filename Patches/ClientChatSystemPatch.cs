using Eclipse.Services;
using HarmonyLib;
using Il2CppInterop.Runtime;
using ProjectM.Network;
using ProjectM.UI;
using System.Collections;
using System.Text;
using System.Text.RegularExpressions;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using CryptographicOperations = System.Security.Cryptography.CryptographicOperations;
using HMACSHA256 = System.Security.Cryptography.HMACSHA256;

namespace Eclipse.Patches;

[HarmonyPatch]
internal static class ClientChatSystemPatch
{
    static EntityManager EntityManager => Core.EntityManager;

    static readonly bool ShouldInitialize = Plugin.Leveling || Plugin.Expertise || Plugin.Legacies || Plugin.Quests || Plugin.Familiars || Plugin.Professions;
    public static bool UserRegistered = false;

    static readonly Regex regexExtract = new(@"^\[(\d+)\]:");
    static readonly Regex regexMAC = new(@";mac([^;]+)$");

    static readonly WaitForSeconds RegistrationDelay = new(5f);

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
        ConfigsToClient
        //UpdateShiftSlot
    }

    [HarmonyPatch(typeof(ClientChatSystem), nameof(ClientChatSystem.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(ClientChatSystem __instance)
    {
        if (!Core._hasInitialized || !ShouldInitialize) return;

        if (!UserRegistered && localCharacter != Entity.Null && localUser != Entity.Null)
        {
            UserRegistered = true;

            try
            {
                string modVersion = MyPluginInfo.PLUGIN_VERSION;
                string stringId = localUser.ReadRO<User>().PlatformId.ToString();

                string message = $"{modVersion};{stringId}";
                Core.StartCoroutine(DelayedSendMessage(message));
            }
            catch (Exception ex)
            {
                Core.Log.LogError($"{MyPluginInfo.PLUGIN_NAME}[{MyPluginInfo.PLUGIN_VERSION}] failed to register with Bloodcraft on server! Error - {ex}");
            }
        }

        //NativeArray<Entity> entities = __instance.EntityQueries[1].ToEntityArray(Allocator.Temp);
        NativeArray<Entity> entities = __instance._ReceiveChatMessagesQuery.ToEntityArray(Allocator.Temp);

        try
        {
            foreach (Entity entity in entities)
            {
                if (entity.Has<ChatMessageServerEvent>())
                {
                    ChatMessageServerEvent chatMessage = entity.ReadRO<ChatMessageServerEvent>();
                    string message = chatMessage.MessageText.Value;

                    if (VerifyMAC(message, out string originalMessage))
                    {
                        HandleServerMessage(originalMessage);
                        EntityManager.DestroyEntity(entity);
                    }
                }
            }
        }
        finally
        {
            entities.Dispose();
        }
    }
    static IEnumerator DelayedSendMessage(string message)
    {
        yield return RegistrationDelay;

        SendMessage(NetworkEventSubType.RegisterUser, message);
    }
    static void SendMessage(NetworkEventSubType subType, string message)
    {
        string intermediateMessage = $"[ECLIPSE][{(int)subType}]:{message}";
        string messageWithMAC = $"{intermediateMessage};mac{GenerateMAC(intermediateMessage)}";

        ChatMessageEvent chatMessageEvent = new()
        {
            MessageText = new FixedString512Bytes(messageWithMAC),
            MessageType = ChatMessageType.Local,
            ReceiverEntity = localUser.ReadRO<NetworkId>()
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
                List<string> playerData = DataService.ParseMessageString(regexExtract.Replace(message, ""));
                DataService.ParsePlayerData(playerData);

                if (CanvasService._killSwitch) CanvasService._killSwitch = false;
                if (!CanvasService._active) CanvasService._active = true;

                CanvasService.CanvasUpdateLoop().Start();
                break;
            case (int)NetworkEventSubType.ConfigsToClient:
                List<string> configData = DataService.ParseMessageString(regexExtract.Replace(message, ""));
                DataService.ParseConfigData(configData);
                break;
                /*
            case (int)NetworkEventSubType.UpdateShiftSlot:
                List<string> abilityData = DataService.ParseMessageString(regexExtract.Replace(message, ""));
                DataService.ParseAbilityData(abilityData);

                if (CanvasService.KillSwitch) CanvasService.KillSwitch = false;
                if (!CanvasService.ShiftActive) CanvasService.ShiftActive = true;

                Core.StartCoroutine(CanvasService.ShiftUpdateLoop());

                break;
                */
        }
    }
    public static bool VerifyMAC(string receivedMessage, out string originalMessage)
    {
        Match match = regexMAC.Match(receivedMessage);
        originalMessage = "";

        if (match.Success)
        {
            string receivedMAC = match.Groups[1].Value;
            string intermediateMessage = regexMAC.Replace(receivedMessage, "");
            string recalculatedMAC = GenerateMAC(intermediateMessage);

            if (CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(recalculatedMAC), Encoding.UTF8.GetBytes(receivedMAC)))
            {
                originalMessage = intermediateMessage;

                return true;
            }
        }

        return false;
    }
    public static string GenerateMAC(string message)
    {
        using var hmac = new HMACSHA256(Core.NEW_SHARED_KEY);
        byte[] messageBytes = Encoding.UTF8.GetBytes(message);
        byte[] hashBytes = hmac.ComputeHash(messageBytes);
        return Convert.ToBase64String(hashBytes);
    }
}
