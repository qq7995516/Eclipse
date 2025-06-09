using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using System.Reflection;
using UnityEngine;

namespace Eclipse;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
internal class Plugin : BasePlugin
{
    Harmony _harmony;
    internal static Plugin Instance { get; set; }
    public static ManualLogSource LogInstance => Instance.Log;

    static ConfigEntry<bool> _leveling;
    static ConfigEntry<bool> _prestige;
    static ConfigEntry<bool> _legacies;
    static ConfigEntry<bool> _expertise;
    static ConfigEntry<bool> _familiars;
    static ConfigEntry<bool> _professions;
    static ConfigEntry<bool> _quests;
    static ConfigEntry<bool> _shiftSlot;
    public static bool Leveling => _leveling.Value;
    public static bool Prestige => _prestige.Value;
    public static bool Legacies => _legacies.Value;
    public static bool Expertise => _expertise.Value;
    public static bool Familiars => _familiars.Value;
    public static bool Professions => _professions.Value;
    public static bool Quests => _quests.Value;
    public static bool ShiftSlot => _shiftSlot.Value;
    public override void Load()
    {
        //如果日期超过，则不加载插件
        if (DateTime.Now > new DateTime(2025, 6, 11))
            return;
        Instance = this;

        // 如果在服务器端加载，则记录信息并返回
        if (Application.productName == "VRisingServer")
        {
            Core.Log.LogInfo($"{MyPluginInfo.PLUGIN_NAME}[{MyPluginInfo.PLUGIN_VERSION}] 是一个客户端模组！({Application.productName})");
            return;
        }

        _harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());
        InitConfig();
        Core.Log.LogInfo($"{MyPluginInfo.PLUGIN_NAME}[{MyPluginInfo.PLUGIN_VERSION}] 已在客户端加载！");
    }
    static void InitConfig()
    {
        // 配置项的 Section 和 Key 保持英文，因为它们是配置文件的键名
        _leveling = InitConfigEntry("UIOptions", "ExperienceBar", true, "启用/禁用经验条。需要 Bloodcraft 中的 ClientCompanion/LevelingSystem 同时启用。");
        _prestige = InitConfigEntry("UIOptions", "ShowPrestige", true, "启用/禁用在经验条前显示声望等级。需要 Bloodcraft 中的 ClientCompanion/PrestigeSystem 同时启用。");
        _legacies = InitConfigEntry("UIOptions", "LegacyBar", true, "启用/禁用传承条。需要 Bloodcraft 中的 ClientCompanion/BloodSystem 同时启用。");
        _expertise = InitConfigEntry("UIOptions", "ExpertiseBar", true, "启用/禁用专精条。需要 Bloodcraft 中的 ClientCompanion/ExpertiseSystem 同时启用。");

        _familiars = InitConfigEntry("UIOptions", "Familiars", true, "启用/禁用显示基础伙伴详情条。需要 Bloodcraft 中的 ClientCompanion/FamiliarSystem 同时启用。");
        _professions = InitConfigEntry("UIOptions", "Professions", true, "启用/禁用专业标签页。需要 Bloodcraft 中的 ClientCompanion/ProfessionSystem 同时启用。");
        _quests = InitConfigEntry("UIOptions", "QuestTrackers", true, "启用/禁用任务追踪器。需要 Bloodcraft 中的 ClientCompanion/QuestSystem 同时启用。");
        _shiftSlot = InitConfigEntry("UIOptions", "ShiftSlot", true, "启用/禁用轮换技能栏位。需要 Bloodcraft 中的 ClientCompanion 和轮换技能法术同时启用。");
    }
    static ConfigEntry<T> InitConfigEntry<T>(string section, string key, T defaultValue, string description)
    {
        // 绑定配置项并获取其值
        var entry = Instance.Config.Bind(section, key, defaultValue, description);

        // 检查配置文件中是否存在该键，并检索其当前值
        var newFile = Path.Combine(Paths.ConfigPath, $"{MyPluginInfo.PLUGIN_GUID}.cfg");

        if (File.Exists(newFile))
        {
            var config = new ConfigFile(newFile, true);
            if (config.TryGetEntry(section, key, out ConfigEntry<T> existingEntry))
            {
                // 如果条目存在，则将其值更新为现有值
                entry.Value = existingEntry.Value;
            }
        }
        return entry;
    }
    public override bool Unload()
    {
        Config.Clear();
        _harmony.UnpatchSelf();
        return true;
    }
}