using Eclipse.Patches;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using ProjectM.UI;
using Stunlock.Core;
using Stunlock.Localization;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static Eclipse.Services.CanvasService.UIObjectUtils;
using static Eclipse.Services.DataService;
using Image = UnityEngine.UI.Image;
using StringComparison = System.StringComparison;

// UI ideas
// how to summon familiars, choose class spells, etc with a menu on the client? could just have the various UI elements correspond to respective familiar lists, class spells, etc.
// like if menu 1 has 4 buttons they choose the spell to request from the server if the server detects a change in a certain component which the client would do when they click a button
// display class somewhere in UI

namespace Eclipse.Services;
internal class CanvasService
{
    static readonly bool ExperienceBar = Plugin.Leveling;
    static readonly bool ShowPrestige = Plugin.Prestige;
    static readonly bool LegacyBar = Plugin.Legacies;
    static readonly bool ExpertiseBar = Plugin.Expertise;
    static readonly bool QuestTracker = Plugin.Quests;

    static readonly WaitForSeconds Delay = new(2.5f);

    static UICanvasBase UICanvasBase;
    static Canvas Canvas;

    static GameObject ExperienceBarGameObject;
    static GameObject ExperienceInformationPanel;
    static LocalizedText ExperienceHeader;
    static LocalizedText ExperienceText;
    static LocalizedText ExperienceClassText;
    static Image ExperienceIcon;
    static Image ExperienceFill;
    static float ExperienceProgress = 0f;
    static int ExperienceLevel = 0;
    static int ExperiencePrestige = 0;
    static PlayerClass ClassType = PlayerClass.None;

    static GameObject LegacyBarGameObject;
    static GameObject LegacyInformationPanel;
    static LocalizedText FirstLegacyStat;
    static LocalizedText SecondLegacyStat;
    static LocalizedText ThirdLegacyStat;
    static LocalizedText LegacyHeader;
    static LocalizedText LegacyText;
    static Image LegacyIcon;
    static Image LegacyFill;
    static string LegacyType;
    static float LegacyProgress = 0f;
    static int LegacyLevel = 0;
    static int LegacyPrestige = 0;
    static List<string> LegacyBonusStats = ["","",""];

    static GameObject ExpertiseBarGameObject;
    static GameObject ExpertiseInformationPanel;
    static LocalizedText FirstExpertiseStat;
    static LocalizedText SecondExpertiseStat;
    static LocalizedText ThirdExpertiseStat;
    static LocalizedText ExpertiseHeader;
    static LocalizedText ExpertiseText;
    static Image ExpertiseIcon;
    static Image ExpertiseFill;
    static string ExpertiseType;
    static float ExpertiseProgress = 0f;
    static int ExpertiseLevel = 0;
    static int ExpertisePrestige = 0;
    static List<string> ExpertiseBonusStats = ["","",""];

    static GameObject DailyQuestObject;
    static LocalizedText DailyQuestHeader;
    static LocalizedText DailyQuestSubHeader;
    static int DailyProgress = 0;
    static int DailyGoal = 0;
    static string DailyTarget = "";

    static GameObject WeeklyQuestObject;
    static LocalizedText WeeklyQuestHeader;
    static LocalizedText WeeklyQuestSubHeader;
    static int WeeklyProgress = 0;
    static int WeeklyGoal = 0;
    static string WeeklyTarget = "";

    public static bool UIActive = true;
    public static readonly List<GameObject> ActiveObjects = [];

    public static bool Active = false;
    public static bool KillSwitch = false;
    public CanvasService(UICanvasBase canvas)
    {
        // Instantiate the ExperienceBar from the PlayerEntryPrefab and find the BotomBarCanvas
        UICanvasBase = canvas;
        InitializeBars(canvas);
        try
        {
            //InitializeSprites();
            //FindGameObjects(canvas.transform, Plugin.FilePaths[0], true);
        }
        catch (Exception ex)
        {
            Core.Log.LogError($"Failed to initialize blood sprites: {ex}");
        }
    }

    static readonly Dictionary<int, string> RomanNumerals = new()
    {
        {100, "C"}, {90, "XC"}, {50, "L"}, {40, "XL"},
        {10, "X"}, {9, "IX"}, {5, "V"}, {4, "IV"},
        {1, "I"}
    };
    static string IntegerToRoman(int num)
    {
        string result = string.Empty;

        foreach (var item in RomanNumerals)
        {
            while (num >= item.Key)
            {
                result += item.Value;
                num -= item.Key;
            }
        }

        return result;
    }
    public static List<string> ParseMessageString(string configString)
    {
        if (string.IsNullOrEmpty(configString))
        {
            return [];
        }
        return [..configString.Split(',')];
    }
    public static void ParseConfigData(List<string> configData)
    {
        int index = 0;

        ConfigData parsedConfigData = new(
            configData[index++], // prestigeMultiplier
            configData[index++], // statSynergyMultiplier
            string.Join(",", configData.Skip(index).Take(12)), // Combine the next 11 elements for weaponStatValues
            string.Join(",", configData.Skip(index += 12).Take(12)), // Combine the following 11 elements for bloodStatValues
            string.Join(",", configData.Skip(index += 12)) // Combine all remaining elements for classStatSynergies
        );

        PrestigeStatMultiplier = parsedConfigData.PrestigeStatMultiplier;
        ClassStatMultiplier = parsedConfigData.ClassStatMultiplier;

        WeaponStatValues = parsedConfigData.WeaponStatValues;

        BloodStatValues = parsedConfigData.BloodStatValues;

        ClassStatSynergies = parsedConfigData.ClassStatSynergies;
    }
    public static void ParsePlayerData(List<string> playerData) // want to do stat bonuses as well if chosen for expertise/legacy
    {
        int index = 0;

        ExperienceData experienceData = new(playerData[index++], playerData[index++], playerData[index++], playerData[index++]);
        LegacyData legacyData = new(playerData[index++], playerData[index++], playerData[index++], playerData[index++], playerData[index++]);
        ExpertiseData expertiseData = new(playerData[index++], playerData[index++], playerData[index++], playerData[index++], playerData[index++]);
        QuestData dailyQuestData = new(playerData[index++], playerData[index++], playerData[index++]);
        QuestData weeklyQuestData = new(playerData[index++], playerData[index++], playerData[index]);

        ExperienceProgress = experienceData.Progress;
        ExperienceLevel = experienceData.Level;
        ExperiencePrestige = experienceData.Prestige;
        ClassType = experienceData.Class;

        LegacyProgress = legacyData.Progress;
        LegacyLevel = legacyData.Level;
        LegacyPrestige = legacyData.Prestige;
        LegacyType = legacyData.LegacyType;
        LegacyBonusStats = legacyData.BonusStats;

        ExpertiseProgress = expertiseData.Progress;
        ExpertiseLevel = expertiseData.Level;
        ExpertisePrestige = expertiseData.Prestige;
        ExpertiseType = expertiseData.ExpertiseType;
        ExpertiseBonusStats = expertiseData.BonusStats;

        DailyProgress = dailyQuestData.Progress;
        DailyGoal = dailyQuestData.Goal;
        DailyTarget = dailyQuestData.Target;

        WeeklyProgress = weeklyQuestData.Progress;
        WeeklyGoal = weeklyQuestData.Goal;
        WeeklyTarget = weeklyQuestData.Target;
    }
    public static IEnumerator CanvasUpdateLoop() // need to find another component, can abstract data to whatever just need something relatively unused that syncs. Check SyncingComponents or w/e that was called
    {
        while (true)
        {
            if (KillSwitch)
            {
                Active = false;
                break;
            }

            if (!Active) Active = true;

            if (!UIActive) // don't update if not active
            {
                yield return Delay;
                continue;
            }

            if (ExperienceBar)
            {
                ExperienceFill.fillAmount = ExperienceProgress;

                if (ExperienceText.GetText() != ExperienceLevel.ToString())
                {
                    ExperienceText.ForceSet(ExperienceLevel.ToString());
                }

                if (ShowPrestige && ExperiencePrestige != 0)
                {
                    ExperienceHeader.ForceSet($"Experience {IntegerToRoman(ExperiencePrestige)}");
                }

                if (ClassType != PlayerClass.None)
                {
                    if (!ExperienceClassText.enabled) ExperienceClassText.enabled = true;
                    ExperienceClassText.ForceSet(ClassType.ToString());
                }
                else
                {
                    ExperienceClassText.ForceSet("");
                    ExperienceClassText.enabled = false;
                }
            }

            if (LegacyBar)
            {
                LegacyFill.fillAmount = LegacyProgress;

                if (LegacyHeader.GetText() != LegacyType)
                {
                    if (ShowPrestige && LegacyPrestige != 0)
                    {
                        LegacyType = $"{LegacyType} {IntegerToRoman(LegacyPrestige)}";
                    }

                    LegacyHeader.ForceSet(LegacyType);
                }

                if (LegacyText.GetText() != LegacyLevel.ToString())
                {
                    LegacyText.ForceSet(LegacyLevel.ToString());
                }

                if (LegacyBonusStats[0] != "None" && FirstLegacyStat.GetText() != LegacyBonusStats[0])
                {
                    if (!FirstLegacyStat.enabled) FirstLegacyStat.enabled = true;
                    string statInfo = GetStatInfo(LegacyBonusStats[0]);
                    FirstLegacyStat.ForceSet(statInfo);
                }
                else if (LegacyBonusStats[0] == "None" && FirstLegacyStat.enabled)
                {
                    FirstLegacyStat.ForceSet("");
                    FirstLegacyStat.enabled = false;
                }

                if (LegacyBonusStats[1] != "None" && SecondLegacyStat.GetText() != LegacyBonusStats[1])
                {
                    if (!SecondLegacyStat.enabled) SecondLegacyStat.enabled = true;
                    string statInfo = GetStatInfo(LegacyBonusStats[1]);
                    SecondLegacyStat.ForceSet(statInfo);
                }
                else if (LegacyBonusStats[1] == "None" && SecondLegacyStat.enabled)
                {
                    SecondLegacyStat.ForceSet("");
                    SecondLegacyStat.enabled = false;
                }

                if (LegacyBonusStats[2] != "None" && ThirdLegacyStat.GetText() != LegacyBonusStats[2])
                {
                    if (!ThirdLegacyStat.enabled) ThirdLegacyStat.enabled = true;
                    string statInfo = GetStatInfo(LegacyBonusStats[2]);
                    ThirdLegacyStat.ForceSet(statInfo);
                }
                else if (LegacyBonusStats[2] == "None" && ThirdLegacyStat.enabled)
                {
                    ThirdLegacyStat.ForceSet("");
                    ThirdLegacyStat.enabled = false;
                }
            }

            if (ExpertiseBar)
            {
                ExpertiseFill.fillAmount = ExpertiseProgress;

                if (ExpertiseHeader.GetText() != ExpertiseType)
                {
                    if (ShowPrestige && ExpertisePrestige != 0)
                    {
                        ExpertiseType = $"{ExpertiseType} {IntegerToRoman(ExpertisePrestige)}";
                    }

                    ExpertiseHeader.ForceSet(ExpertiseType);
                }

                if (ExpertiseText.GetText() != ExpertiseLevel.ToString())
                {
                    ExpertiseText.ForceSet(ExpertiseLevel.ToString());
                }

                if (ExpertiseBonusStats[0] != "None" && FirstExpertiseStat.GetText() != ExpertiseBonusStats[0])
                {
                    if (!FirstExpertiseStat.enabled) FirstExpertiseStat.enabled = true;
                    string statInfo = GetStatInfo(ExpertiseBonusStats[0]);
                    FirstExpertiseStat.ForceSet(statInfo);
                }
                else if (ExpertiseBonusStats[0] == "None" && FirstExpertiseStat.enabled)
                {
                    FirstExpertiseStat.ForceSet("");
                    FirstExpertiseStat.enabled = false;
                }

                if (ExpertiseBonusStats[1] != "None" && SecondExpertiseStat.GetText() != ExpertiseBonusStats[1])
                {
                    if (!SecondExpertiseStat.enabled) SecondExpertiseStat.enabled = true;
                    string statInfo = GetStatInfo(ExpertiseBonusStats[1]);
                    SecondExpertiseStat.ForceSet(statInfo);
                }
                else if (ExpertiseBonusStats[1] == "None" && SecondExpertiseStat.enabled)
                {
                    SecondExpertiseStat.ForceSet("");
                    SecondExpertiseStat.enabled = false;
                }

                if (ExpertiseBonusStats[2] != "None" && ThirdExpertiseStat.GetText() != ExpertiseBonusStats[2])
                {
                    if (!ThirdExpertiseStat.enabled) ThirdExpertiseStat.enabled = true;
                    string statInfo = GetStatInfo(ExpertiseBonusStats[2]);
                    ThirdExpertiseStat.ForceSet(statInfo);
                }
                else if (ExpertiseBonusStats[2] == "None" && ThirdExpertiseStat.enabled)
                {
                    ThirdExpertiseStat.ForceSet("");
                    ThirdExpertiseStat.enabled = false;
                }
            }

            if (QuestTracker)
            {
                if (DailyProgress != DailyGoal)
                {
                    if (!DailyQuestObject.gameObject.active) DailyQuestObject.gameObject.active = true;
                    DailyQuestSubHeader.ForceSet($"<color=white>{DailyTarget}</color>: {DailyProgress}/<color=yellow>{DailyGoal}</color>");
                }
                else if (DailyProgress == DailyGoal)
                {
                    DailyQuestObject.gameObject.active = false;
                }

                if (WeeklyProgress != WeeklyGoal)
                {
                    if (!WeeklyQuestObject.gameObject.active) WeeklyQuestObject.gameObject.active = true;
                    WeeklyQuestSubHeader.ForceSet($"<color=white>{WeeklyTarget}</color>: {WeeklyProgress}/<color=yellow>{WeeklyGoal}</color>");
                }
                else if (WeeklyProgress == WeeklyGoal)
                {
                    WeeklyQuestObject.gameObject.active = false;
                }
            }

            yield return Delay;
        }
    }
    static string GetStatInfo(string statType)
    {
        if (Enum.GetNames(typeof(WeaponStatType)).Any(stat => stat.Equals(statType, StringComparison.OrdinalIgnoreCase)) && Enum.TryParse(statType, out WeaponStatType weaponStat))
        {
            float statValue = WeaponStatValues[weaponStat]; // basic scaling, then need to do for prestige mult and class mult
            float prestigeMultiplier = ExpertisePrestige > 0 ? 1 + (PrestigeStatMultiplier * ExpertisePrestige) : 1f;
            float classMultiplier = !ClassType.Equals(PlayerClass.None) ? ClassStatSynergies[ClassType].WeaponStats.Contains(weaponStat) ? ClassStatMultiplier : 1f : 1f;

            //Core.Log.LogInfo($"StatValue: {statValue} | PrestigeMultiplier: {prestigeMultiplier} | ClassMultiplier: {classMultiplier} | ExpertiseLevel: {ExpertiseLevel}");
            statValue = statValue * prestigeMultiplier * classMultiplier * ((float)ExpertiseLevel / 100f); //  need to send over max levels as well ;_;
            //Core.Log.LogInfo($"StatValue: {statValue}");

            string statValueString = WeaponStatFormats[weaponStat] switch
            {
                "integer" => ((int)statValue).ToString(),
                "decimal" => statValue.ToString("F2"),
                "percentage" => (statValue * 100f).ToString("F0") + "%",
                _ => statValue.ToString(),
            };

            string displayString = $"<color=#00FFFF>{WeaponStatAbbreviations[weaponStat]}</color>: <color=#90EE90>{statValueString}</color>";
            return displayString;
        }
        else if (Enum.GetNames(typeof(BloodStatType)).Any(stat => stat.Equals(statType, StringComparison.OrdinalIgnoreCase)) && Enum.TryParse(statType, out BloodStatType bloodStat))
        {
            float statValue = BloodStatValues[bloodStat];
            float prestigeMultiplier = LegacyPrestige > 0 ? 1 + (PrestigeStatMultiplier * LegacyPrestige) : 1f;
            float classMultiplier = !ClassType.Equals(PlayerClass.None) ? ClassStatSynergies[ClassType].BloodStats.Contains(bloodStat) ? ClassStatMultiplier : 1f : 1f;

            //Core.Log.LogInfo($"StatValue: {statValue} | PrestigeMultiplier: {prestigeMultiplier} | ClassMultiplier: {classMultiplier} | LegacyLevel: {LegacyLevel}");
            statValue = statValue * prestigeMultiplier * classMultiplier * LegacyLevel;
            //Core.Log.LogInfo($"StatValue: {statValue}");

            string displayString = $"<color=#00FFFF>{BloodStatAbbreviations[bloodStat]}</color>: <color=#90EE90>{statValue.ToString("F0") + "%"}</color>";
            return displayString;
        }

        return "";
    }
    static void InitializeBars(UICanvasBase canvas)
    {
        GameObject CanvasObject = FindTargetUIObject(canvas.transform.root, "BottomBarCanvas");
        Canvas bottomBarCanvas = CanvasObject.GetComponent<Canvas>();

        Canvas = bottomBarCanvas;
        GameObject objectPrefab = canvas.TargetInfoParent.gameObject;
        GameObject tooltipPrefab = canvas.BottomBarParentPrefab.FakeTooltip.gameObject;

        GameObject bloodOrbParent = FindTargetUIObject(canvas.transform.root, "BloodOrbParent");
        if (bloodOrbParent != null)
        {
            RectTransform bloodOrbParentRectTransform = bloodOrbParent.GetComponent<RectTransform>();
            Il2CppStructArray<Vector3> worldCorners = new(4);
            bloodOrbParentRectTransform.GetWorldCorners(worldCorners);
            GameplayInputSystemPatch.bottomLeft = worldCorners[0];
            GameplayInputSystemPatch.topRight = worldCorners[2];
        }

        // Get MiniMap south icon on the compass to set locations
        GameObject MiniMapSouthObject = FindTargetUIObject(canvas.transform.root, "S");
        RectTransform MiniMapSouthRectTransform = MiniMapSouthObject.GetComponent<RectTransform>();

        int barNumber = 1; // ref and increment for spacing of bars to account for config options

        // Configure ExperienceBar
        if (ExperienceBar)
        {
            GameObject ExperienceBarObject = GameObject.Instantiate(objectPrefab);
            ExperienceBarGameObject = ExperienceBarObject;
            RectTransform ExperienceBarRectTransform = ExperienceBarObject.GetComponent<RectTransform>();
            GameObject.DontDestroyOnLoad(ExperienceBarObject);
            SceneManager.MoveGameObjectToScene(ExperienceBarObject, SceneManager.GetSceneByName("VRisingWorld"));
            ExperienceBarRectTransform.SetParent(bottomBarCanvas.transform, false);

            ExperienceFill = FindTargetUIObject(ExperienceBarRectTransform.transform, "Fill").GetComponent<Image>();
            ExperienceHeader = FindTargetUIObject(ExperienceBarRectTransform.transform, "Name").GetComponent<LocalizedText>();
            ExperienceText = FindTargetUIObject(ExperienceBarRectTransform.transform, "LevelText").GetComponent<LocalizedText>();

            ExperienceInformationPanel = FindTargetUIObject(ExperienceBarRectTransform.transform, "InformationPanel");

            // Assign LocalizedText for player class
            ExperienceClassText = FindTargetUIObject(ExperienceInformationPanel.transform, "ProffesionInfo").GetComponent<LocalizedText>();
            ExperienceClassText.ForceSet("");
            ExperienceClassText.enabled = false;
            LocalizedText ExperienceFirstText = FindTargetUIObject(ExperienceInformationPanel.transform, "BloodInfo").GetComponent<LocalizedText>();
            ExperienceFirstText.ForceSet("");
            ExperienceFirstText.enabled = false;
            LocalizedText ExperienceSecondText = FindTargetUIObject(ExperienceInformationPanel.transform, "PlatformUserName").GetComponent<LocalizedText>();
            ExperienceSecondText.ForceSet("");
            ExperienceSecondText.enabled = false;

            // Configure ExperienceBar
            ConfigureBar(ExperienceBarRectTransform, MiniMapSouthObject, MiniMapSouthRectTransform, ExperienceFill, ExperienceHeader, ExperienceText, CanvasObject.layer, 1f, "Experience", Color.green, ref barNumber);
            ExperienceBarObject.SetActive(true);
            ActiveObjects.Add(ExperienceBarObject);
        }

        // Configure LegacyBar
        if (LegacyBar)
        {
            GameObject LegacyBarObject = GameObject.Instantiate(objectPrefab);
            LegacyBarGameObject = LegacyBarObject;
            RectTransform LegacyBarRectTransform = LegacyBarObject.GetComponent<RectTransform>();
            GameObject.DontDestroyOnLoad(LegacyBarObject);
            SceneManager.MoveGameObjectToScene(LegacyBarObject, SceneManager.GetSceneByName("VRisingWorld"));
            LegacyBarRectTransform.SetParent(bottomBarCanvas.transform, false);

            LegacyFill = FindTargetUIObject(LegacyBarRectTransform.transform, "Fill").GetComponent<Image>();
            LegacyHeader = FindTargetUIObject(LegacyBarRectTransform.transform, "Name").GetComponent<LocalizedText>();
            LegacyText = FindTargetUIObject(LegacyBarRectTransform.transform, "LevelText").GetComponent<LocalizedText>();

            LegacyInformationPanel = FindTargetUIObject(LegacyBarRectTransform.transform, "InformationPanel");

            // Assign LocalizedText for LegacyInformationPanel
            FirstLegacyStat = FindTargetUIObject(LegacyInformationPanel.transform, "BloodInfo").GetComponent<LocalizedText>();
            FirstLegacyStat.ForceSet("");
            FirstLegacyStat.enabled = false;
            SecondLegacyStat = FindTargetUIObject(LegacyInformationPanel.transform, "ProffesionInfo").GetComponent<LocalizedText>();
            SecondLegacyStat.ForceSet("");
            FirstLegacyStat.Text.color = SecondLegacyStat.Text.color;
            SecondLegacyStat.enabled = false;
            ThirdLegacyStat = FindTargetUIObject(LegacyInformationPanel.transform, "PlatformUserName").GetComponent<LocalizedText>();
            ThirdLegacyStat.ForceSet("");
            ThirdLegacyStat.enabled = false;
            ThirdLegacyStat.Text.color = SecondLegacyStat.Text.color;

            // Configure LegacyBar
            ConfigureBar(LegacyBarRectTransform, MiniMapSouthObject, MiniMapSouthRectTransform, LegacyFill, LegacyHeader, LegacyText, CanvasObject.layer, 1f, "Legacy", Color.red, ref barNumber);
            LegacyBarObject.SetActive(true);
            ActiveObjects.Add(LegacyBarObject);
        }

        // Configure ExpertiseBar
        if (ExpertiseBar)
        {
            GameObject ExpertiseBarObject = GameObject.Instantiate(objectPrefab);
            ExpertiseBarGameObject = ExpertiseBarObject;
            RectTransform ExpertiseBarRectTransform = ExpertiseBarObject.GetComponent<RectTransform>();
            GameObject.DontDestroyOnLoad(ExpertiseBarObject);
            SceneManager.MoveGameObjectToScene(ExpertiseBarObject, SceneManager.GetSceneByName("VRisingWorld"));
            ExpertiseBarRectTransform.SetParent(bottomBarCanvas.transform, false);

            ExpertiseFill = FindTargetUIObject(ExpertiseBarRectTransform.transform, "Fill").GetComponent<Image>();
            ExpertiseHeader = FindTargetUIObject(ExpertiseBarRectTransform.transform, "Name").GetComponent<LocalizedText>();
            ExpertiseText = FindTargetUIObject(ExpertiseBarRectTransform.transform, "LevelText").GetComponent<LocalizedText>();

            ExpertiseInformationPanel = FindTargetUIObject(ExpertiseBarRectTransform.transform, "InformationPanel");

            // Assign LocalizedText for ExpertiseInformationPanel
            FirstExpertiseStat = FindTargetUIObject(ExpertiseInformationPanel.transform, "BloodInfo").GetComponent<LocalizedText>();
            FirstExpertiseStat.ForceSet("");
            FirstExpertiseStat.enabled = false;
            SecondExpertiseStat = FindTargetUIObject(ExpertiseInformationPanel.transform, "ProffesionInfo").GetComponent<LocalizedText>();
            SecondExpertiseStat.ForceSet("");
            SecondExpertiseStat.enabled = false;
            FirstExpertiseStat.Text.color = SecondExpertiseStat.Text.color;
            ThirdExpertiseStat = FindTargetUIObject(ExpertiseInformationPanel.transform, "PlatformUserName").GetComponent<LocalizedText>();
            ThirdExpertiseStat.ForceSet("");
            ThirdExpertiseStat.enabled = false;
            ThirdExpertiseStat.Text.color = SecondExpertiseStat.Text.color;

            // Configure ExpertiseBar
            ConfigureBar(ExpertiseBarRectTransform, MiniMapSouthObject, MiniMapSouthRectTransform, ExpertiseFill, ExpertiseHeader, ExpertiseText, CanvasObject.layer, 1f, "Expertise", Color.grey, ref barNumber);
            ExpertiseBarObject.SetActive(true);
            ActiveObjects.Add(ExpertiseBarObject);
        }

        if (QuestTracker)
        {
            // Instantiate quest tooltip
            GameObject DailyQuestTooltipObject = GameObject.Instantiate(tooltipPrefab);
            GameObject WeeklyQuestTooltipObject = GameObject.Instantiate(tooltipPrefab);

            DailyQuestObject = DailyQuestTooltipObject;
            WeeklyQuestObject = WeeklyQuestTooltipObject;

            RectTransform DailyQuestTransform = DailyQuestTooltipObject.GetComponent<RectTransform>();
            RectTransform WeeklyQuestTransform = WeeklyQuestTooltipObject.GetComponent<RectTransform>();

            GameObject.DontDestroyOnLoad(DailyQuestTooltipObject);
            GameObject.DontDestroyOnLoad(WeeklyQuestTooltipObject);

            SceneManager.MoveGameObjectToScene(DailyQuestTooltipObject, SceneManager.GetSceneByName("VRisingWorld"));
            SceneManager.MoveGameObjectToScene(WeeklyQuestTooltipObject, SceneManager.GetSceneByName("VRisingWorld"));

            DailyQuestTransform.SetParent(bottomBarCanvas.transform, false);
            WeeklyQuestTransform.SetParent(bottomBarCanvas.transform, false);

            // Activate quest tooltips
            DailyQuestTooltipObject.gameObject.active = true;
            WeeklyQuestTooltipObject.gameObject.active = true;

            // Deactivate unwanted objects in quest tooltips
            GameObject DailyEntries = FindTargetUIObject(DailyQuestTooltipObject.transform, "InformationEntries");
            GameObject WeeklyEntries = FindTargetUIObject(WeeklyQuestTooltipObject.transform, "InformationEntries");
            DeactivateChildrenExceptNamed(DailyEntries.transform, "TooltipHeader");
            DeactivateChildrenExceptNamed(WeeklyEntries.transform, "TooltipHeader");

            // Activate TooltipHeaders
            GameObject DailyTooltipHeader = FindTargetUIObject(DailyQuestTooltipObject.transform, "TooltipHeader");
            GameObject WeeklyTooltipHeader = FindTargetUIObject(WeeklyQuestTooltipObject.transform, "TooltipHeader");
            DailyTooltipHeader.SetActive(true);
            WeeklyTooltipHeader.SetActive(true);

            // Activate Icon&Name container
            GameObject DailyIconNameObject = FindTargetUIObject(DailyTooltipHeader.transform, "Icon&Name");
            GameObject WeeklyIconNameObject = FindTargetUIObject(WeeklyTooltipHeader.transform, "Icon&Name");
            DailyIconNameObject.SetActive(true);
            WeeklyIconNameObject.SetActive(true);

            // Deactivate LevelFrames for now, might be good to use in future
            GameObject DailyLevelFrame = FindTargetUIObject(DailyIconNameObject.transform, "LevelFrame");
            GameObject WeeklyLevelFrame = FindTargetUIObject(WeeklyIconNameObject.transform, "LevelFrame");
            DailyLevelFrame.SetActive(false);
            WeeklyLevelFrame.SetActive(false);

            // Deactivate ReforgeCosts to get bare windows
            GameObject DailyReforge = FindTargetUIObject(DailyQuestTooltipObject.transform, "Tooltip_ReforgeCost");
            GameObject WeeklyReforge = FindTargetUIObject(WeeklyQuestTooltipObject.transform, "Tooltip_ReforgeCost");
            WeeklyReforge.SetActive(false);
            DailyReforge.SetActive(false);

            // Deactivate TooltipIcons to get rid of bone sword image on right
            GameObject DailyTooltipIcon = FindTargetUIObject(DailyTooltipHeader.transform, "TooltipIcon");
            GameObject WeeklyTooltipIcon = FindTargetUIObject(WeeklyTooltipHeader.transform, "TooltipIcon");
            WeeklyTooltipIcon.SetActive(false);
            DailyTooltipIcon.SetActive(false);

            // Assign LocalizedText for QuestHeaders
            GameObject DailyQuestSubHeaderObject = FindTargetUIObject(DailyIconNameObject.transform, "TooltipSubHeader");
            GameObject WeeklyQuestSubHeaderObject = FindTargetUIObject(WeeklyIconNameObject.transform, "TooltipSubHeader");
            DailyQuestHeader = FindTargetUIObject(DailyIconNameObject.transform, "TooltipHeader").GetComponent<LocalizedText>();
            DailyQuestHeader.Text.fontSize *= 2f;
            DailyQuestSubHeader = DailyQuestSubHeaderObject.GetComponent<LocalizedText>();
            DailyQuestSubHeader.Text.enableAutoSizing = false;
            DailyQuestSubHeader.Text.autoSizeTextContainer = false;
            DailyQuestSubHeader.Text.enableWordWrapping = false;
            ContentSizeFitter DailyQuestFitter = DailyQuestSubHeaderObject.GetComponent<ContentSizeFitter>();
            DailyQuestFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            DailyQuestFitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
            WeeklyQuestHeader = FindTargetUIObject(WeeklyIconNameObject.transform, "TooltipHeader").GetComponent<LocalizedText>();
            WeeklyQuestHeader.Text.fontSize *= 2f;
            WeeklyQuestSubHeader = WeeklyQuestSubHeaderObject.GetComponent<LocalizedText>();
            WeeklyQuestSubHeader.Text.enableAutoSizing = false;
            WeeklyQuestSubHeader.Text.autoSizeTextContainer = false;
            WeeklyQuestSubHeader.Text.enableWordWrapping = false;
            ContentSizeFitter WeeklyQuestFitter = WeeklyQuestSubHeaderObject.GetComponent<ContentSizeFitter>();
            WeeklyQuestFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            WeeklyQuestFitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

            // Assign testing text for quest headers
            DailyQuestHeader.ForceSet("Daily Quest");
            DailyQuestHeader.Text.color = Color.green;
            DailyQuestSubHeader.ForceSet("UnitName: 0/0"); // for refreshing these guess check if goal is at target kills in loop and don't update if so?
            WeeklyQuestHeader.ForceSet("Weekly Quest");
            WeeklyQuestHeader.Text.color = Color.magenta;
            WeeklyQuestSubHeader.ForceSet("UnitName: 0/0");

            // Set layer for quest tooltips
            DailyQuestTransform.gameObject.layer = CanvasObject.layer;
            WeeklyQuestTransform.gameObject.layer = CanvasObject.layer;

            // Reduce window widths
            DailyQuestTransform.sizeDelta = new Vector2(DailyQuestTransform.sizeDelta.x * 0.4f, DailyQuestTransform.sizeDelta.y);
            WeeklyQuestTransform.sizeDelta = new Vector2(WeeklyQuestTransform.sizeDelta.x * 0.4f, WeeklyQuestTransform.sizeDelta.y);

            //Core.Log.LogInfo($"DailyQuestTransform: {DailyQuestTransform.position.x},{DailyQuestTransform.position.y},{DailyQuestTransform.position.z}");

            // Set positions for quest tooltips
            //int windowNumber = 1;
            //DailyQuestTransform.anchoredPosition = new(DailyQuestTransform.anchoredPosition.x, DailyQuestTransform.anchoredPosition.y * 2);
            DailyQuestTransform.position = new Vector3(1600f, 275f, 0f);
            WeeklyQuestTransform.position = new Vector3(1600f, 200f, 0f);
            //Core.Log.LogInfo($"DailyQuestTransform: {DailyQuestTransform.position.x},{DailyQuestTransform.position.y},{DailyQuestTransform.position.z}");
            // Add objects to list for toggling later
            ActiveObjects.Add(DailyQuestTooltipObject);
            ActiveObjects.Add(WeeklyQuestTooltipObject);
        }
    }
    static void ConfigureBar(RectTransform barRectTransform, GameObject referenceObject, RectTransform referenceRectTransform, 
       Image fillImage,LocalizedText textHeader, LocalizedText levelText, int layer, float sizeMultiplier, string barHeaderText, Color fillColor, ref int barNumber)
    {
        float rectWidth = barRectTransform.rect.width;
        float sizeOffsetX = ((rectWidth * sizeMultiplier) - rectWidth) * (1 - barRectTransform.pivot.x);
        barRectTransform.localScale *= 0.75f;
        barRectTransform.position = new Vector3(referenceObject.transform.position.x - sizeOffsetX * 2, (referenceObject.transform.position.y * 0.9f) - (referenceRectTransform.rect.height * 2.25f * barNumber), referenceObject.transform.position.z);
        barRectTransform.gameObject.layer = layer;

        fillImage.fillAmount = 0f;
        fillImage.color = fillColor;

        levelText.ForceSet("0");
        textHeader.ForceSet(barHeaderText);
        textHeader.Text.fontSize *= 1.5f;

        FindTargetUIObject(barRectTransform.transform, "DamageTakenFill").GetComponent<Image>().fillAmount = 0f;
        FindTargetUIObject(barRectTransform.transform, "AbsorbFill").GetComponent<Image>().fillAmount = 0f;

        barNumber++;
    } 
    public static class UIObjectUtils
    {
        static readonly Dictionary<BloodType, string> BloodIcons = new()
        {
            { BloodType.Worker, "BloodIcon_Small_Worker" },
            { BloodType.Warrior, "BloodIcon_Small_Warrior" },
            { BloodType.Scholar, "BloodIcon_Small_Scholar" },
            { BloodType.Rogue, "BloodType_Rogue_Small" },
            { BloodType.Mutant, "BloodType_Putrid_Small" },
            { BloodType.Draculin, "BloodType_Draculin_Small" },
            { BloodType.Immortal, "BloodType_Dracula_Small" },
            { BloodType.Creature, "BloodType_Beast_Small" },
            { BloodType.Brute, "BloodIcon_Small_Brute" }
        };

        static readonly Dictionary<BloodType, Sprite> BloodSprites = [];
        static void InitializeSprites()
        {
            List<string> spriteNames = [.. BloodIcons.Values];
            Il2CppArrayBase<Sprite> allSprites = Resources.FindObjectsOfTypeAll<Sprite>();

            if (!File.Exists(Plugin.FilePaths[1])) File.Create(Plugin.FilePaths[1]).Dispose();

            using StreamWriter writer = new(Plugin.FilePaths[1], false);
            foreach (Sprite sprite in allSprites)
            {
                writer.WriteLine(sprite.name);
            }

            var matchedSprites = allSprites
                .Where(sprite => spriteNames.Contains(sprite.name))
                .ToDictionary(sprite => BloodIcons.First(pair => pair.Value == sprite.name).Key, sprite => sprite);

            foreach (var pair in matchedSprites)
            {
                //Core.Log.LogInfo($"BloodType: {pair.Key} | Sprite: {pair.Value.name}");
                BloodSprites[pair.Key] = pair.Value;
            }
        }
        public static GameObject FindTargetUIObject(Transform root, string targetName)
        {
            // Stack to hold the transforms to be processed
            Stack<(Transform transform, int indentLevel)> transformStack = new();
            transformStack.Push((root, 0));

            // HashSet to keep track of visited transforms to avoid cyclic references
            HashSet<Transform> visited = [];

            Il2CppArrayBase<Transform> children = root.GetComponentsInChildren<Transform>(true);

            List<Transform> transforms = [.. children];

            while (transformStack.Count > 0)
            {
                var (current, indentLevel) = transformStack.Pop();

                if (!visited.Add(current))
                {
                    // If we have already visited this transform, skip it
                    continue;
                }

                if (current.gameObject.name.Equals(targetName, StringComparison.OrdinalIgnoreCase))
                {
                    // Return the transform if the name matches
                    return current.gameObject;
                }

                // Create an indentation string based on the indent level
                //string indent = new('|', indentLevel);

                // Print the current GameObject's name and some basic info
                //Core.Log.LogInfo($"{indent}{current.gameObject.name} ({current.gameObject.scene.name})");

                // Add all children to the stack
                foreach (Transform child in transforms)
                {
                    if (child.parent == current)
                    {
                        transformStack.Push((child, indentLevel + 1));
                    }
                }
            }
            return null;
        }
        static LocalizationKey? InsertValue(string value, string key, string hexString)
        {
            if (AssetGuid.TryParse(hexString, out AssetGuid asset))
            {
                LocalizedKeyValue localizedKey = LocalizedKeyValue.Create(key, value);
                LocalizedString localizedString = LocalizedString.Create(asset, localizedKey);
                return new LocalizationKey(localizedString._LocalizationGUID);
            }
            return null;
        }
        public static void FindLoadedObjects<T>() where T : UnityEngine.Object
        {
            Il2CppReferenceArray<UnityEngine.Object> resources = Resources.FindObjectsOfTypeAll(Il2CppType.Of<T>());
            Core.Log.LogInfo($"Found {resources.Length} {Il2CppType.Of<T>().FullName}'s!");
            foreach (UnityEngine.Object resource in resources)
            {
                Core.Log.LogInfo($"Sprite: {resource.name}");
            }
        }
        public static Texture2D CreateFrameBorder(Vector2 size, int borderWidth, Color borderColor)
        {
            // Create a new Texture2D
            Texture2D texture = new((int)size.x, (int)size.y);

            // Fill the texture with a transparent color
            Color[] fillColor = new Color[texture.width * texture.height];
            for (int i = 0; i < fillColor.Length; i++)
            {
                fillColor[i] = Color.clear;
            }
            texture.SetPixels(fillColor);

            // Draw the border, make fraction of fontsize
            borderWidth = Mathf.RoundToInt(borderWidth * 0.5f); // Border width as a fraction of the font size
            for (int y = 0; y < texture.height; y++)
            {
                for (int x = 0; x < texture.width; x++)
                {
                    if (x < borderWidth || x >= texture.width - borderWidth || y < borderWidth || y >= texture.height - borderWidth)
                    {
                        texture.SetPixel(x, y, borderColor);
                    }
                }
            }

            // Apply changes to the texture
            texture.Apply();

            return texture;
        }
        public static Sprite CreateSpriteFromTexture(Texture2D texture)
        {
            // Create a new sprite with the texture
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));

            return sprite;
        }
        public static void DeactivateChildrenExceptNamed(Transform root, string targetName)
        {
            // Stack to hold the transforms to be processed
            Stack<(Transform transform, int indentLevel)> transformStack = new();
            transformStack.Push((root, 0));

            // HashSet to keep track of visited transforms to avoid cyclic references
            HashSet<Transform> visited = [];

            Il2CppArrayBase<Transform> children = root.GetComponentsInChildren<Transform>();
            List<Transform> transforms = [.. children];

            while (transformStack.Count > 0)
            {
                var (current, indentLevel) = transformStack.Pop();

                if (!visited.Add(current))
                {
                    // If we have already visited this transform, skip it
                    continue;
                }

                // Add all children to the stack
                foreach (Transform child in transforms)
                {
                    if (child.parent == current)
                    {
                        transformStack.Push((child, indentLevel + 1));
                    }

                    if (!child.name.Equals(targetName)) child.gameObject.SetActive(false);
                }
            }
        }
        public static void FindGameObjects(Transform root, string filePath = "", bool includeInactive = false)
        {
            // Stack to hold the transforms to be processed
            Stack<(Transform transform, int indentLevel)> transformStack = new();
            transformStack.Push((root, 0));

            // HashSet to keep track of visited transforms to avoid cyclic references
            HashSet<Transform> visited = [];

            Il2CppArrayBase<Transform> children = root.GetComponentsInChildren<Transform>(includeInactive);
            List<Transform> transforms = [..children];

            if (string.IsNullOrEmpty(filePath))
            {
                while (transformStack.Count > 0)
                {
                    var (current, indentLevel) = transformStack.Pop();

                    if (!visited.Add(current))
                    {
                        // If we have already visited this transform, skip it
                        continue;
                    }

                    List<string> objectComponents = FindGameObjectComponents(current.gameObject);

                    // Create an indentation string based on the indent level
                    string indent = new('|', indentLevel);

                    // Write the current GameObject's name and some basic info to the file

                    // Add all children to the stack
                    foreach (Transform child in transforms)
                    {
                        if (child.parent == current)
                        {
                            transformStack.Push((child, indentLevel + 1));
                        }
                    }
                }
                return;
            }

            if (!File.Exists(filePath)) File.Create(filePath).Dispose();

            using StreamWriter writer = new(filePath, false);
            while (transformStack.Count > 0)
            {
                var (current, indentLevel) = transformStack.Pop();

                if (!visited.Add(current))
                {
                    // If we have already visited this transform, skip it
                    continue;
                }

                List<string> objectComponents = FindGameObjectComponents(current.gameObject);

                // Create an indentation string based on the indent level
                string indent = new('|', indentLevel);

                // Write the current GameObject's name and some basic info to the file
                writer.WriteLine($"{indent}{current.gameObject.name} | {string.Join(",", objectComponents)} | [{current.gameObject.scene.name}]");

                // Add all children to the stack
                foreach (Transform child in transforms)
                {
                    if (child.parent == current)
                    {
                        transformStack.Push((child, indentLevel + 1));
                    }
                }
            }
        }
        public static List<string> FindGameObjectComponents(GameObject parentObject)
        {
            List<string> components = [];

            int componentCount = parentObject.GetComponentCount();
            for (int i = 0; i < componentCount; i++)
            {
                components.Add($"{parentObject.GetComponentAtIndex(i).GetIl2CppType().FullName}({i})");
            }

            return components;
        }
        public static void GatherInfo(GameObject gameObject)
        {
            /*
            GameObject BloodType = FindTargetUIObject(gameObject.transform, "BloodType");
            GameObject Percentage = FindTargetUIObject(gameObject.transform, "Percentage");
            GameObject Icon = FindTargetUIObject(gameObject.transform, "Icon");
            GameObject BloodOrb = FindTargetUIObject(gameObject.transform, "BloodOrb");
            GameObject OrbBorder = FindTargetUIObject(gameObject.transform, "Orb border");
            GameObject BloodFill = FindTargetUIObject(gameObject.transform, "BloodFill");
            GameObject Glass = FindTargetUIObject(gameObject.transform, "Glass");
            GameObject BlackBackground = FindTargetUIObject(gameObject.transform, "BlackBackground");
            GameObject Blood = FindTargetUIObject(gameObject.transform, "Blood");

            FindGameObjects(gameObject.transform, true);

            FindGameObjectComponents(BloodType, "BloodType");
            FindGameObjectComponents(Percentage, "Percentage");
            FindGameObjectComponents(Icon, "Icon");
            FindGameObjectComponents(BloodOrb, "BloodOrb");
            FindGameObjectComponents(OrbBorder, "Orb border");
            FindGameObjectComponents(BloodFill, "BloodFill");
            FindGameObjectComponents(Glass, "Glass");
            FindGameObjectComponents(BlackBackground, "BlackBackground");
            FindGameObjectComponents(Blood, "Blood");
            */
        }
    }
}
