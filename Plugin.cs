using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using System.Reflection;

namespace Eclipse;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
internal class Plugin : BasePlugin
{
    Harmony _harmony;
    internal static Plugin Instance { get; private set; }
    public static Harmony Harmony => Instance._harmony;
    public static ManualLogSource LogInstance => Instance.Log;

    //static readonly string ConfigFiles = Path.Combine(Paths.ConfigPath, MyPluginInfo.PLUGIN_NAME); // Eclipse folder if needed

    static ConfigEntry<bool> leveling;
    static ConfigEntry<bool> prestige;
    static ConfigEntry<bool> legacies;
    static ConfigEntry<bool> expertise;
    public static bool Leveling => leveling.Value;
    public static bool Prestige => prestige.Value;
    public static bool Legacies => legacies.Value;
    public static bool Expertise => expertise.Value;

    public override void Load()
    {
        Instance = this;
        _harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());
        InitConfig();
        Core.Log.LogInfo($"{MyPluginInfo.PLUGIN_NAME}[{MyPluginInfo.PLUGIN_VERSION}] loaded! Note that mod will not continue to initialization on dedicated servers.");
    }
    static void InitConfig()
    {
        //CreateDirectory(ConfigFiles);
        leveling = InitConfigEntry("UIOptions", "ExperienceBar", false, "Enable/Disable the experience bar, requires both ClientCompanion/LevelingSystem to be enabled in Bloodcraft.");
        prestige = InitConfigEntry("UIOptions", "ShowPrestige", false, "Enable/Disable showing prestige level in front of experience bar, requires both ClientCompanion/PrestigeSystem to be enabled in Bloodcraft .");
        legacies = InitConfigEntry("UIOptions", "LegacyBar", false, "Enable/Disable the legacy bar, requires both ClientCompanion/LegacySystem to be enabled in Bloodcraft.");
        expertise = InitConfigEntry("UIOptions", "ExpertiseBar", false, "Enable/Disable the expertise bar, requires both ClientCompanion/ExpertiseSystem to be enabled in Bloodcraft.");
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
}
