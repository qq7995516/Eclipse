﻿using Bloodcraft.Resources;
using Eclipse.Services;
using HarmonyLib;
using Il2CppInterop.Runtime;
using ProjectM.Network;
using ProjectM.UI;
using Stunlock.Core;
using System.Collections;
using System.ComponentModel;
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
    static Entity LocalCharacter => Core.LocalCharacter;
    static Entity LocalUser => Core.LocalUser;

    static readonly bool _shouldInitialize = Plugin.Leveling || Plugin.Expertise || Plugin.Legacies || Plugin.Quests || Plugin.Familiars || Plugin.Professions;
    public static bool _userRegistered = false;
    public static bool _pending = false;

    // 正则表达式是逻辑的一部分，不能修改
    static readonly Regex _regexExtract = new(@"^\[(\d+)\]:");
    static readonly Regex _regexMAC = new(@";mac([^;]+)$");

    static readonly WaitForSeconds _registrationDelay = new(2.5f);
    static readonly WaitForSeconds _pendingDelay = new(10f);

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

    // 版本号是逻辑的一部分，不能修改
    public const string V1_3 = "1.3";
    public const string VERSION = MyPluginInfo.PLUGIN_VERSION;

    // 枚举成员名保持不变，Description特性已提供中文描述，这是正确的做法
    public enum NetworkEventSubType
    {
        [Description("注册用户")]
        RegisterUser,

        [Description("同步客户端进度")]
        ProgressToClient,

        [Description("同步客户端配置")]
        ConfigsToClient
    }

    static readonly PrefabGUID _familiarUnlockBuff = PrefabGUIDs.AB_HighLordSword_SelfStun_DeadBuff;

    [HarmonyBefore("gg.deca.Bloodstone")]
    [HarmonyPatch(typeof(ClientChatSystem), nameof(ClientChatSystem.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(ClientChatSystem __instance)
    {
        if (!Core._initialized) return;
        else if (!LocalCharacter.Exists() || !LocalUser.Exists()) return;
        else if (!_userRegistered && !_pending)
        {
            _pending = true;

            try
            {
                string stringId = LocalUser.GetUser().PlatformId.ToString();
                //旧版版本信息
                string message = $"{VERSION};{stringId}";
                //新版
                //string message = $"[ECLIPSE][0]:{VERSION};{stringId}";
                SendMessageDelayRoutine(message, VERSION).Start();
            }
            catch (Exception ex)
            {
                Core.Log.LogError($"向服务器发送注册负载失败！错误 - {ex}");
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

                    if (chatMessage.MessageType.Equals(ServerChatMessageType.System) && CheckMAC(message, out string originalMessage))
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

        try
        {
            if (LocalCharacter.HasBuff(_familiarUnlockBuff) && LocalCharacter.TryGetBuff(_familiarUnlockBuff, out Entity buffEntity))
            {
                buffEntity.Remove<UseCharacterHudProgressBar>();
            }
        }
        catch (Exception ex)
        {
            Core.Log.LogWarning($"检查使魔解锁增益时失败！错误 - {ex}");
        }
    }
    static IEnumerator SendMessageDelayRoutine(string message, string modVersion)
    {
        yield return _registrationDelay;

        SendMessage(NetworkEventSubType.RegisterUser, message, modVersion);
    }
    static void SendMessage(NetworkEventSubType subType, string message, string modVersion)
    {
        // "[ECLIPSE]" 是与服务器约定的协议关键字，不能修改
        string intermediateMessage = $"[ECLIPSE][{(int)subType}]:{message}";
        string messageWithMAC;

        messageWithMAC = modVersion switch
        {
            _ when modVersion.StartsWith(V1_3) => $"{intermediateMessage};mac{GenerateMACV1_3(intermediateMessage)}",
            _ => string.Empty
        };

        if (string.IsNullOrEmpty(messageWithMAC)) return;

        ChatMessageEvent chatMessageEvent = new()
        {
            MessageText = new FixedString512Bytes(messageWithMAC),
            MessageType = ChatMessageType.Local,
            ReceiverEntity = LocalUser.GetNetworkId()
        };

        Entity networkEntity = EntityManager.CreateEntity(_networkEventComponents);
        networkEntity.Write(new FromCharacter { Character = LocalCharacter, User = LocalUser });
        networkEntity.Write(_networkEventType);
        networkEntity.Write(chatMessageEvent);

        Core.Log.LogInfo($"已向服务器发送注册负载 ({DateTime.Now}) - {messageWithMAC}");
    }
    static void HandleServerMessage(string message)
    {
        if (int.TryParse(_regexExtract.Match(message).Groups[1].Value, out int result))
        {
            try
            {
                switch (result)
                {
                    case (int)NetworkEventSubType.ProgressToClient:
                        List<string> playerData = DataService.ParseMessageString(_regexExtract.Replace(message, ""));
                        DataService.ParsePlayerData(playerData);

                        if (CanvasService._killSwitch)
                        {
                            CanvasService._killSwitch = false;
                        }

                        if (CanvasService._canvasRoutine == null)
                        {
                            CanvasService._canvasRoutine = CanvasService.CanvasUpdateLoop().Start();
                            CanvasService._active = true;
                        }

                        break;
                    case (int)NetworkEventSubType.ConfigsToClient:
                        List<string> configData = DataService.ParseMessageString(_regexExtract.Replace(message, ""));
                        DataService.ParseConfigData(configData);

                        _userRegistered = true;

                        break;
                }
            }
            catch (Exception ex)
            {
                Core.Log.LogError($"{MyPluginInfo.PLUGIN_NAME}[{MyPluginInfo.PLUGIN_VERSION}] 解析事件类型后处理消息失败 - {ex}");
            }
        }
        else
        {
            Core.Log.LogWarning($"{MyPluginInfo.PLUGIN_NAME}[{MyPluginInfo.PLUGIN_VERSION}] MAC验证后解析事件类型失败 - {message}");
        }
    }
    public static bool CheckMAC(string receivedMessage, out string originalMessage)
    {
        Match match = _regexMAC.Match(receivedMessage);
        originalMessage = string.Empty;

        if (match.Success)
        {
            string receivedMAC = match.Groups[1].Value;
            string intermediateMessage = _regexMAC.Replace(receivedMessage, "");

            if (VerifyMAC(intermediateMessage, receivedMAC, Core.NEW_SHARED_KEY))
            {
                originalMessage = intermediateMessage;
                return true;
            }
            else
            {
                Core.Log.LogInfo($"匹配的正则表达式消息MAC验证失败 - {receivedMessage}");
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
    public static string GenerateMACV1_3(string message)
    {
        using var hmac = new HMACSHA256(Core.NEW_SHARED_KEY);
        byte[] messageBytes = Encoding.UTF8.GetBytes(message);

        byte[] hashBytes = hmac.ComputeHash(messageBytes);

        return Convert.ToBase64String(hashBytes);
    }
}