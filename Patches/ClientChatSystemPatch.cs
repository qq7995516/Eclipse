using Eclipse.Services;
using HarmonyLib;
using Il2CppInterop.Runtime;
using ProjectM;
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

    static readonly bool _shouldInitialize = Plugin.Leveling || Plugin.Expertise || Plugin.Legacies || Plugin.Quests || Plugin.Familiars || Plugin.Professions;
    public static bool _userRegistered = false;
    public static bool _registrationPending = false;

    static readonly Regex _regexExtract = new(@"^\[(\d+)\]:");
    static readonly Regex _regexMAC = new(@";mac([^;]+)$");

    static readonly WaitForSeconds _registrationDelay = new(5f);
    static readonly WaitForSeconds _pendingDelay = new(20f);

    static readonly ComponentType[] _networkEventComponents =
    [
        ComponentType.ReadOnly(Il2CppType.Of<FromCharacter>()),
        ComponentType.ReadOnly(Il2CppType.Of<NetworkEventType>()),
        ComponentType.ReadOnly(Il2CppType.Of<SendNetworkEventTag>()),
        ComponentType.ReadOnly(Il2CppType.Of<ChatMessageEvent>())
    ];

    static readonly NetworkEventType _networkEventType = new()
    {
        IsAdminEvent = false,
        EventId = NetworkEvents.EventId_ChatMessageEvent,
        IsDebugEvent = false,
    };

    public static Entity _localCharacter = Entity.Null;
    public static Entity _localUser = Entity.Null;

    static readonly List<string> _versions =
    [
        "1.3.2",
        "1.2.2"   
    ];
    public enum NetworkEventSubType
    {
        RegisterUser,
        ProgressToClient,
        ConfigsToClient
    }

    [HarmonyPatch(typeof(ClientChatSystem), nameof(ClientChatSystem.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(ClientChatSystem __instance)
    {
        if (!Core._initialized) return;
        else if (!_shouldInitialize) return;

        if (!_userRegistered && !_registrationPending && _localCharacter.Exists() && _localUser.Exists())
        {
            _registrationPending = true;

            try
            {
                string modVersion = _versions.First();
                string stringId = _localUser.GetUser().PlatformId.ToString();

                string message = $"{modVersion};{stringId}";
                SendMessageDelayRoutine(message, modVersion).Start();
            }
            catch (Exception ex)
            {
                Core.Log.LogError($"{MyPluginInfo.PLUGIN_NAME}[{MyPluginInfo.PLUGIN_VERSION}] failed to register with Bloodcraft on server! Error - {ex}");
            }
        }

        NativeArray<Entity> entities = __instance._ReceiveChatMessagesQuery.ToEntityArray(Allocator.Temp);

        try
        {
            foreach (Entity entity in entities)
            {
                if (entity.Has<ChatMessageServerEvent>())
                {
                    ChatMessageServerEvent chatMessage = entity.Read<ChatMessageServerEvent>();
                    string message = chatMessage.MessageText.Value;

                    if (CheckMAC(message, out string originalMessage))
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
    static IEnumerator SendMessageDelayRoutine(string message, string modVersion)
    {
        yield return _registrationDelay;

        SendMessage(NetworkEventSubType.RegisterUser, message, modVersion);
        ResetPendingDelayRoutine().Start();
    }
    static IEnumerator ResetPendingDelayRoutine()
    {
        yield return _pendingDelay;

        if (_userRegistered)
        {
            yield break;
        }

        int index = _versions.IndexOf(_versions.First());
        _versions.RemoveAt(index);

        _registrationPending = false;
    }
    static void SendMessage(NetworkEventSubType subType, string message, string modVersion)
    {
        string intermediateMessage = $"[ECLIPSE][{(int)subType}]:{message}";
        string messageWithMAC = string.Empty;

        switch (modVersion)
        {
            case "1.2.2":
                messageWithMAC = $"{intermediateMessage};mac{GenerateMACV1_2_2(intermediateMessage)}";
                break;
            case "1.3.2":
                messageWithMAC = $"{intermediateMessage};mac{GenerateMACV1_3_2(intermediateMessage)}";
                break;
        }

        if (string.IsNullOrEmpty(messageWithMAC)) return;

        ChatMessageEvent chatMessageEvent = new()
        {
            MessageText = new FixedString512Bytes(messageWithMAC),
            MessageType = ChatMessageType.Local,
            ReceiverEntity = _localUser.Read<NetworkId>()
        };

        Entity networkEntity = EntityManager.CreateEntity(_networkEventComponents);
        networkEntity.Write(new FromCharacter { Character = _localCharacter, User = _localUser });
        networkEntity.Write(_networkEventType);
        networkEntity.Write(chatMessageEvent);
    }
    static void HandleServerMessage(string message)
    {
        int eventType = int.Parse(_regexExtract.Match(message).Groups[1].Value);

        switch (eventType)
        {
            case (int)NetworkEventSubType.ProgressToClient:
                List<string> playerData = DataService.ParseMessageString(_regexExtract.Replace(message, ""));
                DataService.ParsePlayerData(playerData);

                if (CanvasService._killSwitch) CanvasService._killSwitch = false;
                if (!CanvasService._active) CanvasService._active = true;

                CanvasService.CanvasUpdateLoop().Start();
                break;
            case (int)NetworkEventSubType.ConfigsToClient:
                List<string> configData = DataService.ParseMessageString(_regexExtract.Replace(message, ""));
                DataService.ParseConfigData(configData);

                _userRegistered = true;
                break;
        }
    }
    public static bool CheckMAC(string receivedMessage, out string originalMessage)
    {
        /*
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
        */

        Match match = _regexMAC.Match(receivedMessage);
        originalMessage = "";

        if (match.Success)
        {
            string receivedMAC = match.Groups[1].Value;
            string intermediateMessage = _regexMAC.Replace(receivedMessage, "");

            if (VerifyMAC(intermediateMessage, receivedMAC, Core.NEW_SHARED_KEY))
            {
                originalMessage = intermediateMessage;

                return true;
            }
        }

        return false;
    }
    static bool VerifyMAC(string message, string receivedMAC, byte[] key)
    {
        using var hmac = new HMACSHA256(key);
        byte[] messageBytes = Encoding.UTF8.GetBytes(message);

        byte[] hashBytes = hmac.ComputeHash(messageBytes);
        string recalculatedMAC = Convert.ToBase64String(hashBytes);

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(recalculatedMAC),
            Encoding.UTF8.GetBytes(receivedMAC));
    }
    public static string GenerateMACV1_2_2(string message)
    {
        using var hmac = new HMACSHA256(Core.NEW_SHARED_KEY);
        byte[] messageBytes = Encoding.UTF8.GetBytes(message);

        byte[] hashBytes = hmac.ComputeHash(messageBytes);

        return Convert.ToBase64String(hashBytes);
    }
    public static string GenerateMACV1_3_2(string message)
    {
        using var hmac = new HMACSHA256(Core.NEW_SHARED_KEY);
        byte[] messageBytes = Encoding.UTF8.GetBytes(message);

        byte[] hashBytes = hmac.ComputeHash(messageBytes);

        return Convert.ToBase64String(hashBytes);
    }
}
