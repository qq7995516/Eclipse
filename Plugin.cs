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
        //������ڳ������򲻼��ز��
        if (DateTime.Now > new DateTime(2025, 6, 11))
            return;
        Instance = this;

        // ����ڷ������˼��أ����¼��Ϣ������
        if (Application.productName == "VRisingServer")
        {
            Core.Log.LogInfo($"{MyPluginInfo.PLUGIN_NAME}[{MyPluginInfo.PLUGIN_VERSION}] ��һ���ͻ���ģ�飡({Application.productName})");
            return;
        }

        _harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());
        InitConfig();
        Core.Log.LogInfo($"{MyPluginInfo.PLUGIN_NAME}[{MyPluginInfo.PLUGIN_VERSION}] ���ڿͻ��˼��أ�");
    }
    static void InitConfig()
    {
        // ������� Section �� Key ����Ӣ�ģ���Ϊ�����������ļ��ļ���
        _leveling = InitConfigEntry("UIOptions", "ExperienceBar", true, "����/���þ���������Ҫ Bloodcraft �е� ClientCompanion/LevelingSystem ͬʱ���á�");
        _prestige = InitConfigEntry("UIOptions", "ShowPrestige", true, "����/�����ھ�����ǰ��ʾ�����ȼ�����Ҫ Bloodcraft �е� ClientCompanion/PrestigeSystem ͬʱ���á�");
        _legacies = InitConfigEntry("UIOptions", "LegacyBar", true, "����/���ô���������Ҫ Bloodcraft �е� ClientCompanion/BloodSystem ͬʱ���á�");
        _expertise = InitConfigEntry("UIOptions", "ExpertiseBar", true, "����/����ר��������Ҫ Bloodcraft �е� ClientCompanion/ExpertiseSystem ͬʱ���á�");

        _familiars = InitConfigEntry("UIOptions", "Familiars", true, "����/������ʾ�����������������Ҫ Bloodcraft �е� ClientCompanion/FamiliarSystem ͬʱ���á�");
        _professions = InitConfigEntry("UIOptions", "Professions", true, "����/����רҵ��ǩҳ����Ҫ Bloodcraft �е� ClientCompanion/ProfessionSystem ͬʱ���á�");
        _quests = InitConfigEntry("UIOptions", "QuestTrackers", true, "����/��������׷��������Ҫ Bloodcraft �е� ClientCompanion/QuestSystem ͬʱ���á�");
        _shiftSlot = InitConfigEntry("UIOptions", "ShiftSlot", true, "����/�����ֻ�������λ����Ҫ Bloodcraft �е� ClientCompanion ���ֻ����ܷ���ͬʱ���á�");
    }
    static ConfigEntry<T> InitConfigEntry<T>(string section, string key, T defaultValue, string description)
    {
        // ���������ȡ��ֵ
        var entry = Instance.Config.Bind(section, key, defaultValue, description);

        // ��������ļ����Ƿ���ڸü����������䵱ǰֵ
        var newFile = Path.Combine(Paths.ConfigPath, $"{MyPluginInfo.PLUGIN_GUID}.cfg");

        if (File.Exists(newFile))
        {
            var config = new ConfigFile(newFile, true);
            if (config.TryGetEntry(section, key, out ConfigEntry<T> existingEntry))
            {
                // �����Ŀ���ڣ�����ֵ����Ϊ����ֵ
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