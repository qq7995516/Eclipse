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
    internal static Plugin Instance { get; private set; }
    public static ManualLogSource LogInstance => Instance.Log;

    public static readonly List<string> DirectoryPaths =
    [
        Path.Combine(Paths.ConfigPath, MyPluginInfo.PLUGIN_NAME) // 0
    ];

    public static readonly List<string> FilePaths =
    [
        Path.Combine(DirectoryPaths[0], "game_objects.json"), // 0
        Path.Combine(DirectoryPaths[0], "sprites.json"), // 1
    ];

    static ConfigEntry<bool> leveling;
    static ConfigEntry<bool> prestige;
    static ConfigEntry<bool> legacies;
    static ConfigEntry<bool> expertise;
    static ConfigEntry<bool> familiars;
    static ConfigEntry<bool> professions;
    static ConfigEntry<bool> quests;
    static ConfigEntry<bool> shiftSlot;
    public static bool Leveling => leveling.Value;
    public static bool Prestige => prestige.Value;
    public static bool Legacies => legacies.Value;
    public static bool Expertise => expertise.Value;
    public static bool Familiars => familiars.Value;
    public static bool Professions => professions.Value;
    public static bool Quests => quests.Value;
    public static bool ShiftSlot => shiftSlot.Value;
    public override void Load()
    {
        Instance = this;

        if (Application.productName == "VRisingServer")
        {
            Core.Log.LogInfo($"{MyPluginInfo.PLUGIN_NAME}[{MyPluginInfo.PLUGIN_VERSION}] is a client mod! ({Application.productName})");
            return;
        }

        _harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());
        InitConfig();
        Core.Log.LogInfo($"{MyPluginInfo.PLUGIN_NAME}[{MyPluginInfo.PLUGIN_VERSION}] loaded on client!");
    }
    static void InitConfig()
    {
        foreach (string path in DirectoryPaths)
        {
            CreateDirectory(path);
        }

        leveling = InitConfigEntry("UIOptions", "ExperienceBar", true, "Enable/Disable the experience bar, requires both ClientCompanion/LevelingSystem to be enabled in Bloodcraft.");
        prestige = InitConfigEntry("UIOptions", "ShowPrestige", true, "Enable/Disable showing prestige level in front of experience bar, requires both ClientCompanion/PrestigeSystem to be enabled in Bloodcraft.");
        legacies = InitConfigEntry("UIOptions", "LegacyBar", true, "Enable/Disable the legacy bar, requires both ClientCompanion/BloodSystem to be enabled in Bloodcraft.");
        expertise = InitConfigEntry("UIOptions", "ExpertiseBar", true, "Enable/Disable the expertise bar, requires both ClientCompanion/ExpertiseSystem to be enabled in Bloodcraft.");
        familiars = InitConfigEntry("UIOptions", "Familiars", true, "Enable/Disable showing basic familiar details bar, requires both ClientCompanion/FamiliarSystem to be enabled in Bloodcraft.");
        professions = InitConfigEntry("UIOptions", "Professions", true, "Enable/Disable the professions tab, requires both ClientCompanion/ProfessionSystem to be enabled in Bloodcraft.");
        quests = InitConfigEntry("UIOptions", "QuestTrackers", true, "Enable/Disable the quest tracker, requires both ClientCompanion/QuestSystem to be enabled in Bloodcraft.");
        //shiftSlot = InitConfigEntry("UIOptions", "ShiftSlot", true, "Enable/Disable the shift slot, requires both ClientCompanion and shift slot spell to be enabled in Bloodcraft.");
    }
    static ConfigEntry<T> InitConfigEntry<T>(string section, string key, T defaultValue, string description)
    {
        // Bind the configuration entry and get its value
        var entry = Instance.Config.Bind(section, key, defaultValue, description);

        // Check if the key exists in the configuration file and retrieve its current value
        var newFile = Path.Combine(Paths.ConfigPath, $"{MyPluginInfo.PLUGIN_GUID}.cfg");

        if (File.Exists(newFile))
        {
            var config = new ConfigFile(newFile, true);
            if (config.TryGetEntry(section, key, out ConfigEntry<T> existingEntry))
            {
                // If the entry exists, update the value to the existing value
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
    static void CreateDirectory(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
    }
}
