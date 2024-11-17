using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using ProjectM;
using ProjectM.UI;
using System.Collections;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static Eclipse.Services.CanvasService.GameObjectUtilities;
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
    static readonly bool FamiliarBar = Plugin.Familiars;
    static readonly bool ProfessionBars = Plugin.Professions;
    static readonly bool QuestTracker = Plugin.Quests;
    public enum UIElement
    {
        Experience,
        Legacy,
        Expertise,
        Familiars,
        Professions,
        Daily,
        Weekly
    }

    static readonly Dictionary<int, string> RomanNumerals = new()
    {
        {100, "C"}, {90, "XC"}, {50, "L"}, {40, "XL"},
        {10, "X"}, {9, "IX"}, {5, "V"}, {4, "IV"},
        {1, "I"}
    };

    static readonly List<string> SpriteNames = 
    [
        "BloodIcon_Cursed",
        "BloodIcon_Small_Cursed",
        "BloodIcon_Small_Holy",
        "BloodIcon_Warrior",
        "BloodIcon_Small_Warrior",
        "Poneti_Icon_Hammer_30",
        "Poneti_Icon_Bag",
        "Poneti_Icon_Res_93"
    ];

    static readonly Dictionary<string, Sprite> SpriteMap = [];

    static readonly Regex ClassNameRegex = new("(?<!^)([A-Z])");

    /*
    static readonly Dictionary<PlayerClass, string> ClassColorHexMap = new()
    {
        { PlayerClass.ShadowBlade, "#A020F0" },   // ignite purple (Hex: A020F0)
        { PlayerClass.DemonHunter, "#FFD700" },  // static yellow (Hex: FFD700)
        { PlayerClass.BloodKnight, "#FF0000" },  // leech red (Hex: FF0000)
        { PlayerClass.ArcaneSorcerer, "#008080" },   // weaken teal (Hex: 008080)
        { PlayerClass.VampireLord, "#00FFFF" },     // chill cyan (Hex: 00FFFF)
        { PlayerClass.DeathMage, "#00FF00" }    // condemn green (Hex: 00FF00)
    };
    */

    static readonly Dictionary<PlayerClass, Color> ClassColorHexMap = new()
    {
        { PlayerClass.ShadowBlade, new Color(0.63f, 0.13f, 0.94f) },  // ignite purple
        { PlayerClass.DemonHunter, new Color(1f, 0.84f, 0f) },        // static yellow
        { PlayerClass.BloodKnight, new Color(1f, 0f, 0f) },           // leech red
        { PlayerClass.ArcaneSorcerer, new Color(0f, 0.5f, 0.5f) },    // weaken teal
        { PlayerClass.VampireLord, new Color(0f, 1f, 1f) },           // chill cyan
        { PlayerClass.DeathMage, new Color(0f, 1f, 0f) }              // condemn green
    };

    static readonly WaitForSeconds Delay = new(1f); // won't ever update faster than 2.5s intervals since that's roughly how often the server sends updates which I find acceptable for now

    // object references for UI elements
    static UICanvasBase UICanvasBase;
    static Canvas Canvas;

    static GameObject ExperienceBarGameObject;
    static GameObject ExperienceInformationPanel;
    static LocalizedText ExperienceHeader;
    static LocalizedText ExperienceText;
    static LocalizedText ExperienceFirstText;
    static LocalizedText ExperienceClassText;
    static LocalizedText ExperienceSecondText;
    static Image ExperienceFill;
    public static float ExperienceProgress = 0f;
    public static int ExperienceLevel = 0;
    public static int ExperiencePrestige = 0;
    public static int ExperienceMaxLevel = 90;
    public static PlayerClass ClassType = PlayerClass.None;

    static GameObject LegacyBarGameObject;
    static GameObject LegacyInformationPanel;
    static LocalizedText FirstLegacyStat;
    static LocalizedText SecondLegacyStat;
    static LocalizedText ThirdLegacyStat;
    static LocalizedText LegacyHeader;
    static LocalizedText LegacyText;
    static Image LegacyFill;
    public static string LegacyType;
    public static float LegacyProgress = 0f;
    public static int LegacyLevel = 0;
    public static int LegacyPrestige = 0;
    public static int LegacyMaxLevel = 100;
    public static List<string> LegacyBonusStats = ["","",""];

    static GameObject ExpertiseBarGameObject;
    static GameObject ExpertiseInformationPanel;
    static LocalizedText FirstExpertiseStat;
    static LocalizedText SecondExpertiseStat;
    static LocalizedText ThirdExpertiseStat;
    static LocalizedText ExpertiseHeader;
    static LocalizedText ExpertiseText;
    static Image ExpertiseFill;
    public static string ExpertiseType;
    public static float ExpertiseProgress = 0f;
    public static int ExpertiseLevel = 0;
    public static int ExpertisePrestige = 0;
    public static int ExpertiseMaxLevel = 100;
    public static List<string> ExpertiseBonusStats = ["", "", ""];

    static GameObject FamiliarBarGameObject;
    static GameObject FamiliarInformationPanel; // show physical power, spell power and max health? ah need to send those too, rip
    static LocalizedText FamiliarMaxHealth;
    static LocalizedText FamiliarPhysicalPower;
    static LocalizedText FamiliarSpellPower;
    static LocalizedText FamiliarHeader;
    static LocalizedText FamiliarText;
    static Image FamiliarFill;
    public static float FamiliarProgress = 0f;
    public static int FamiliarLevel = 1;
    public static int FamiliarPrestige = 0;
    public static int FamiliarMaxLevel = 90;
    public static string FamiliarName = "";
    public static List<string> FamiliarStats = ["", "", ""];

    public static int ProfessionMaxLevel = 100;

    static GameObject EnchantingBarGameObject;
    static LocalizedText EnchantingLevelText;
    static Image EnchantingFill;
    public static float EnchantingProgress = 0f;
    public static int EnchantingLevel = 0;

    static GameObject AlchemyBarGameObject;
    static LocalizedText AlchemyLevelText;
    static Image AlchemyFill;
    public static float AlchemyProgress = 0f;
    public static int AlchemyLevel = 0;

    static GameObject HarvestingGameObject;
    static LocalizedText HarvestingLevelText;
    static Image HarvestingFill;
    public static float HarvestingProgress = 0f;
    public static int HarvestingLevel = 0;

    static GameObject BlacksmithingBarGameObject;
    static LocalizedText BlacksmithingLevelText;
    static Image BlacksmithingFill;
    public static float BlacksmithingProgress = 0f;
    public static int BlacksmithingLevel = 0;

    static GameObject TailoringBarGameObject;
    static LocalizedText TailoringLevelText;
    static Image TailoringFill;
    public static float TailoringProgress = 0f;
    public static int TailoringLevel = 0;

    static GameObject WoodcuttingBarGameObject;
    static LocalizedText WoodcuttingLevelText;
    static Image WoodcuttingFill;
    public static float WoodcuttingProgress = 0f;
    public static int WoodcuttingLevel = 0;

    static GameObject MiningBarGameObject;
    static LocalizedText MiningLevelText;
    static Image MiningFill;
    public static float MiningProgress = 0f;
    public static int MiningLevel = 0;

    static GameObject FishingBarGameObject;
    static LocalizedText FishingLevelText;
    static Image FishingFill;
    public static float FishingProgress = 0f;
    public static int FishingLevel = 0;

    static GameObject DailyQuestObject;
    static LocalizedText DailyQuestHeader;
    static LocalizedText DailyQuestSubHeader;
    static Image DailyQuestIcon;
    public static TargetType DailyTargetType = TargetType.Kill;
    public static int DailyProgress = 0;
    public static int DailyGoal = 0;
    public static string DailyTarget = "";
    public static bool DailyVBlood = false;

    static GameObject WeeklyQuestObject;
    static LocalizedText WeeklyQuestHeader;
    static LocalizedText WeeklyQuestSubHeader;
    static Image WeeklyQuestIcon;
    public static TargetType WeeklyTargetType = TargetType.Kill;
    public static int WeeklyProgress = 0;
    public static int WeeklyGoal = 0;
    public static string WeeklyTarget = "";
    public static bool WeeklyVBlood = false;

    static int Layer;
    static int BarNumber;
    static int GraphBarNumber;
    static float WindowOffset;

    const float BAR_HEIGHT_SPACING = 0.075f;
    //const float BAR_WIDTH_SPACING = 0.075f;
    const float BAR_WIDTH_SPACING = 0.065f;

    public static readonly List<GameObject> ActiveObjects = [];

    public static bool Active = false;
    public static bool KillSwitch = false;
    public CanvasService(UICanvasBase canvas)
    {
        UICanvasBase = canvas;
        Canvas = GameObject.Find("HUDCanvas(Clone)/BottomBarCanvas").GetComponent<Canvas>();

        Layer = Canvas.gameObject.layer;
        BarNumber = 0;
        GraphBarNumber = 0;
        WindowOffset = 0f;

        //RectTransform MapCompassSouthTransform = GameObject.Find("HUDCanvas(Clone)/HUDClockCanvas/HUDMinimap/MiniMapParent(Clone)/Root/Panel/Compass/S").GetComponent<RectTransform>();
        //ReferenceOffsetX = (MapCompassSouthTransform.anchorMin + MapCompassSouthTransform.pivot * (MapCompassSouthTransform.anchorMax - MapCompassSouthTransform.anchorMin)).normalized.x;
        FindSpritesByName(SpriteNames);

        InitializeBloodButton();
        InitializeUI();
    }
    static void InitializeUI()
    {
        if (ExperienceBar) ConfigureHorizontalProgressBar(ref ExperienceBarGameObject, ref ExperienceInformationPanel, ref ExperienceFill, ref ExperienceText, ref ExperienceHeader, UIElement.Experience, Color.green, ref ExperienceFirstText, ref ExperienceClassText, ref ExperienceSecondText);

        if (LegacyBar) ConfigureHorizontalProgressBar(ref LegacyBarGameObject, ref LegacyInformationPanel, ref LegacyFill, ref LegacyText, ref LegacyHeader, UIElement.Legacy, Color.red, ref FirstLegacyStat, ref SecondLegacyStat, ref ThirdLegacyStat);

        if (ExpertiseBar) ConfigureHorizontalProgressBar(ref ExpertiseBarGameObject, ref ExpertiseInformationPanel, ref ExpertiseFill, ref ExpertiseText, ref ExpertiseHeader, UIElement.Expertise, Color.grey, ref FirstExpertiseStat, ref SecondExpertiseStat, ref ThirdExpertiseStat);

        if (FamiliarBar) ConfigureHorizontalProgressBar(ref FamiliarBarGameObject, ref FamiliarInformationPanel, ref FamiliarFill, ref FamiliarText, ref FamiliarHeader, UIElement.Familiars, Color.yellow, ref FamiliarMaxHealth, ref FamiliarPhysicalPower, ref FamiliarSpellPower);

        if (QuestTracker)
        {
            ConfigureQuestWindow(ref DailyQuestObject, UIElement.Daily, Color.green, ref DailyQuestHeader, ref DailyQuestSubHeader, ref DailyQuestIcon);
            ConfigureQuestWindow(ref WeeklyQuestObject, UIElement.Weekly, Color.magenta, ref WeeklyQuestHeader, ref WeeklyQuestSubHeader, ref WeeklyQuestIcon);
        }

        if (ProfessionBars)
        {
            ConfigureVerticalProgressBar(ref EnchantingBarGameObject, ref EnchantingFill, ref EnchantingLevelText, ProfessionColors[Profession.Enchanting]);
            ConfigureVerticalProgressBar(ref AlchemyBarGameObject, ref AlchemyFill, ref AlchemyLevelText, ProfessionColors[Profession.Alchemy]);
            ConfigureVerticalProgressBar(ref HarvestingGameObject, ref HarvestingFill, ref HarvestingLevelText, ProfessionColors[Profession.Harvesting]);
            ConfigureVerticalProgressBar(ref BlacksmithingBarGameObject, ref BlacksmithingFill, ref BlacksmithingLevelText, ProfessionColors[Profession.Blacksmithing]);
            ConfigureVerticalProgressBar(ref TailoringBarGameObject, ref TailoringFill, ref TailoringLevelText, ProfessionColors[Profession.Tailoring]);
            ConfigureVerticalProgressBar(ref WoodcuttingBarGameObject, ref WoodcuttingFill, ref WoodcuttingLevelText, ProfessionColors[Profession.Woodcutting]);
            ConfigureVerticalProgressBar(ref MiningBarGameObject, ref MiningFill, ref MiningLevelText, ProfessionColors[Profession.Mining]);
            ConfigureVerticalProgressBar(ref FishingBarGameObject, ref FishingFill, ref FishingLevelText, ProfessionColors[Profession.Fishing]);
        }
    }
    static void InitializeBloodButton()
    {
        // Find blood (the one with raycasting) to add button for UI toggling
        GameObject bloodObject = GameObject.Find("HUDCanvas(Clone)/BottomBarCanvas/BottomBar(Clone)/Content/BloodOrbParent/BloodOrb/BlackBackground/Blood");

        // Add button
        SimpleStunButton stunButton = bloodObject.AddComponent<SimpleStunButton>();
        stunButton.onClick.AddListener(new Action(ToggleUIObjects));
    }
    public static IEnumerator CanvasUpdateLoop()
    {
        while (true)
        {
            if (KillSwitch) // stop running if player leaves game
            {
                Active = false;
                break;
            }
            else if (!Active) // don't update if not active from blood orb click
            {
                yield return Delay;
                continue;
            }

            if (ExperienceBar)
            {
                UpdateBar(ExperienceProgress, ExperienceLevel, ExperienceMaxLevel, ExperiencePrestige, ExperienceText, ExperienceHeader, ExperienceFill, UIElement.Experience);
                UpdateClass(ClassType, ExperienceClassText);
            }

            if (LegacyBar)
            {
                UpdateBar(LegacyProgress, LegacyLevel, LegacyMaxLevel, LegacyPrestige, LegacyText, LegacyHeader, LegacyFill, UIElement.Legacy, LegacyType);
                UpdateStats(LegacyBonusStats, [FirstLegacyStat, SecondLegacyStat, ThirdLegacyStat], GetBloodStatInfo);
            }

            if (ExpertiseBar)
            {
                UpdateBar(ExpertiseProgress, ExpertiseLevel, ExpertiseMaxLevel, ExpertisePrestige, ExpertiseText, ExpertiseHeader, ExpertiseFill, UIElement.Expertise, ExpertiseType);
                UpdateStats(ExpertiseBonusStats, [FirstExpertiseStat, SecondExpertiseStat, ThirdExpertiseStat], GetWeaponStatInfo);
            }

            if (FamiliarBar)
            {
                UpdateBar(FamiliarProgress, FamiliarLevel, FamiliarMaxLevel, FamiliarPrestige, FamiliarText, FamiliarHeader, FamiliarFill, UIElement.Familiars, FamiliarName);
                UpdateFamiliarStats(FamiliarStats, [FamiliarMaxHealth, FamiliarPhysicalPower, FamiliarSpellPower]);
            }

            if (QuestTracker)
            {
                UpdateQuests(DailyQuestObject, DailyQuestSubHeader, DailyQuestIcon, DailyTargetType, DailyTarget, DailyProgress, DailyGoal, DailyVBlood);
                UpdateQuests(WeeklyQuestObject, WeeklyQuestSubHeader, WeeklyQuestIcon, WeeklyTargetType, WeeklyTarget, WeeklyProgress, WeeklyGoal, WeeklyVBlood);
            }

            if (ProfessionBars)
            {
                UpdateProfessions(EnchantingProgress, EnchantingLevel, ProfessionMaxLevel, EnchantingLevelText, EnchantingFill);
                UpdateProfessions(AlchemyProgress, AlchemyLevel, ProfessionMaxLevel, AlchemyLevelText, AlchemyFill);
                UpdateProfessions(HarvestingProgress, HarvestingLevel, ProfessionMaxLevel, HarvestingLevelText, HarvestingFill);
                UpdateProfessions(BlacksmithingProgress, BlacksmithingLevel, ProfessionMaxLevel, BlacksmithingLevelText, BlacksmithingFill);
                UpdateProfessions(TailoringProgress, TailoringLevel, ProfessionMaxLevel, TailoringLevelText, TailoringFill);
                UpdateProfessions(WoodcuttingProgress, WoodcuttingLevel, ProfessionMaxLevel, WoodcuttingLevelText, WoodcuttingFill);
                UpdateProfessions(MiningProgress, MiningLevel, ProfessionMaxLevel, MiningLevelText, MiningFill);
                UpdateProfessions(FishingProgress, FishingLevel, ProfessionMaxLevel, FishingLevelText, FishingFill);
            }

            yield return Delay;
        }
    }
    static void UpdateProfessions(float progress, int level, int maxLevel, LocalizedText levelText, Image fill)
    {
        string levelString = level.ToString();

        if (level == maxLevel)
        {
            fill.fillAmount = 1f;
        }
        else
        {
            fill.fillAmount = progress;
        }

        if (levelText.GetText() != levelString)
        {
            levelText.ForceSet(levelString);
        }
    }
    static void UpdateBar(float progress, int level, int maxLevel, int prestiges, LocalizedText levelText, LocalizedText barHeader, Image fill, UIElement element, string type = "")
    {
        string levelString = level.ToString();

        if (type == "Frailed" || type == "Familiar")
        {
            levelString = "N/A";
        }

        if (level == maxLevel)
        {
            fill.fillAmount = 1f;
        }
        else
        {
            fill.fillAmount = progress;
        }

        if (levelText.GetText() != levelString)
        {
            levelText.ForceSet(levelString);
        }

        if (ShowPrestige && prestiges != 0)
        {
            string header = "";

            if (element.Equals(UIElement.Experience))
            {
                header = $"{element} {IntegerToRoman(prestiges)}";
            }
            else if (element.Equals(UIElement.Legacy))
            {
                header = $"{type} {IntegerToRoman(prestiges)}";
            }
            else if (element.Equals(UIElement.Expertise))
            {
                header = $"{type} {IntegerToRoman(prestiges)}";
            }
            else if (element.Equals(UIElement.Familiars))
            {
                header = $"{type} {IntegerToRoman(prestiges)}";
            }

            barHeader.ForceSet(header);
        }
        else if (!string.IsNullOrEmpty(type))
        {
            if (barHeader.GetText() != type)
            {
                barHeader.ForceSet(type);
            }
        }
    }
    static void UpdateClass(PlayerClass classType, LocalizedText classText)
    {
        if (classType != PlayerClass.None)
        {
            if (!classText.enabled) classText.enabled = true;

            string formattedClassName = FormatClassName(classType);
            classText.ForceSet(formattedClassName);

            if (ClassColorHexMap.TryGetValue(classType, out Color classColor))
            {
                classText.Text.color = classColor;
            }
        }
        else
        {
            classText.ForceSet("");
            classText.enabled = false;
        }
    }
    static string FormatClassName(PlayerClass classType)
    {
        return ClassNameRegex.Replace(classType.ToString(), " $1");
    }
    static void UpdateStats(List<string> bonusStats, List<LocalizedText> statTexts, Func<string, string> getStatInfo)
    {
        for (int i = 0; i < 3; i++) // hard coding this for now
        {
            if (bonusStats[i] != "None")
            {
                if (!statTexts[i].enabled) statTexts[i].enabled = true;
                string statInfo = getStatInfo(bonusStats[i]);
                statTexts[i].ForceSet(statInfo);
            }
            else if (bonusStats[i] == "None" && statTexts[i].enabled)
            {
                statTexts[i].ForceSet("");
                statTexts[i].enabled = false;
            }
        }
    }
    static void UpdateFamiliarStats(List<string> familiarStats, List<LocalizedText> statTexts)
    {
        for (int i = 0; i < 3; i++)
        {
            if (!string.IsNullOrEmpty(familiarStats[i]))
            {
                if (!statTexts[i].enabled) statTexts[i].enabled = true;
                string statInfo = $"<color=#00FFFF>{FamiliarStatStringAbbreviations[i]}</color>: <color=#90EE90>{familiarStats[i]}</color>";
                statTexts[i].ForceSet(statInfo);
            }
            else if (statTexts[i].enabled)
            {
                statTexts[i].ForceSet("");
                statTexts[i].enabled = false;
            }
        }
    }
    static void UpdateQuests(GameObject questObject, LocalizedText questSubHeader, Image questIcon, TargetType targetType, string target, int progress, int goal, bool isVBlood)
    {
        if (progress != goal)
        {
            if (!questObject.gameObject.active) questObject.gameObject.active = true;
            questSubHeader.ForceSet($"<color=white>{target}</color>: {progress}/<color=yellow>{goal}</color>");

            if (targetType.Equals(TargetType.Kill))
            {
                if (isVBlood && questIcon.sprite.name != "BloodIcon_Cursed" && SpriteMap.TryGetValue("BloodIcon_Cursed", out Sprite vBloodSprite))
                {
                    if (!questIcon.gameObject.active) questIcon.gameObject.active = true;
                    questIcon.sprite = vBloodSprite;
                }
                else if (!isVBlood && questIcon.sprite.name != "BloodIcon_Warrior" && SpriteMap.TryGetValue("BloodIcon_Warrior", out Sprite unitSprite))
                {
                    if (!questIcon.gameObject.active) questIcon.gameObject.active = true;
                    questIcon.sprite = unitSprite;
                }
            }
            else if (targetType.Equals(TargetType.Craft) && questIcon.sprite.name != "Poneti_Icon_Hammer_30" && SpriteMap.TryGetValue("Poneti_Icon_Hammer_30", out Sprite craftingSprite))
            {
                if (!questIcon.gameObject.active) questIcon.gameObject.active = true;
                questIcon.sprite = craftingSprite;
            }
            else if (targetType.Equals(TargetType.Gather) && questIcon.sprite.name != "Poneti_Icon_Res_93" && SpriteMap.TryGetValue("Poneti_Icon_Res_93", out Sprite gatherSprite))
            {
                if (!questIcon.gameObject.active) questIcon.gameObject.active = true;
                questIcon.sprite = gatherSprite;
            }
        }
        else
        {
            questObject.gameObject.active = false;
            questIcon.gameObject.active = false;
        }
    }
    static string GetWeaponStatInfo(string statType)
    {
        if (Enum.TryParse(statType, out WeaponStatType weaponStat))
        {
            if (WeaponStatValues.TryGetValue(weaponStat, out float statValue))
            {
                float classMultiplier = ClassSynergy(weaponStat, ClassType, ClassStatSynergies);
                statValue *= ((1 + (PrestigeStatMultiplier * ExpertisePrestige)) * classMultiplier * ((float)ExpertiseLevel / ExpertiseMaxLevel));
                return FormatWeaponStat(weaponStat, statValue);
            }
        }

        return "";
    }
    static string GetBloodStatInfo(string statType)
    {
        if (Enum.TryParse(statType, out BloodStatType bloodStat))
        {
            if (BloodStatValues.TryGetValue(bloodStat, out float statValue))
            {
                float classMultiplier = ClassSynergy(bloodStat, ClassType, ClassStatSynergies);
                statValue *= ((1 + (PrestigeStatMultiplier * LegacyPrestige)) * classMultiplier * ((float)LegacyLevel / LegacyMaxLevel));

                string displayString = $"<color=#00FFFF>{BloodStatTypeAbbreviations[bloodStat]}</color>: <color=#90EE90>{(statValue * 100).ToString("F0") + "%"}</color>";
                return displayString;
            }
        }

        return "";
    }
    static void ConfigureQuestWindow(ref GameObject questObject, UIElement questType, Color headerColor, ref LocalizedText header, ref LocalizedText subHeader, ref Image questIcon)
    {
        // Instantiate quest tooltip
        questObject = GameObject.Instantiate(UICanvasBase.BottomBarParentPrefab.FakeTooltip.gameObject);
        RectTransform questTransform = questObject.GetComponent<RectTransform>();

        // Prevent quest window from being destroyed on scene load and move to scene
        GameObject.DontDestroyOnLoad(questObject);
        SceneManager.MoveGameObjectToScene(questObject, SceneManager.GetSceneByName("VRisingWorld"));

        // Set parent and activate quest window
        questTransform.SetParent(Canvas.transform, false);
        questTransform.gameObject.layer = Layer;
        questObject.SetActive(true);

        // Deactivate unwanted objects in quest tooltips
        GameObject entries = FindTargetUIObject(questObject.transform, "InformationEntries");
        DeactivateChildrenExceptNamed(entries.transform, "TooltipHeader");

        // Activate TooltipHeader
        GameObject tooltipHeader = FindTargetUIObject(questObject.transform, "TooltipHeader");
        tooltipHeader.SetActive(true);

        // Activate Icon&Name container
        GameObject iconNameObject = FindTargetUIObject(tooltipHeader.transform, "Icon&Name");
        iconNameObject.SetActive(true);

        // Deactivate LevelFrames and ReforgeCosts
        GameObject levelFrame = FindTargetUIObject(iconNameObject.transform, "LevelFrame");
        levelFrame.SetActive(false);
        GameObject reforgeCost = FindTargetUIObject(questObject.transform, "Tooltip_ReforgeCost");
        reforgeCost.SetActive(false);

        // Deactivate TooltipIcon
        GameObject tooltipIcon = FindTargetUIObject(tooltipHeader.transform, "TooltipIcon");
        RectTransform tooltipIconTransform = tooltipIcon.GetComponent<RectTransform>();

        // Set position relative to parent
        tooltipIconTransform.anchorMin = new Vector2(tooltipIconTransform.anchorMin.x, 0.55f);
        tooltipIconTransform.anchorMax = new Vector2(tooltipIconTransform.anchorMax.x, 0.55f);

        // Set the pivot to the vertical center
        tooltipIconTransform.pivot = new Vector2(tooltipIconTransform.pivot.x, 0.55f);

        questIcon = tooltipIcon.GetComponent<Image>();
        if (questType.Equals(UIElement.Daily))
        {
            if (SpriteMap.ContainsKey("BloodIcon_Small_Warrior"))
            {
                questIcon.sprite = SpriteMap["BloodIcon_Small_Warrior"];
            }
        }
        else if (questType.Equals(UIElement.Weekly))
        {
            if (SpriteMap.ContainsKey("BloodIcon_Warrior"))
            {
                questIcon.sprite = SpriteMap["BloodIcon_Warrior"];
            }
        }
        tooltipIconTransform.sizeDelta = new Vector2(tooltipIconTransform.sizeDelta.x * 0.35f, tooltipIconTransform.sizeDelta.y * 0.35f);

        // Set LocalizedText for QuestHeaders
        GameObject subHeaderObject = FindTargetUIObject(iconNameObject.transform, "TooltipSubHeader");
        header = FindTargetUIObject(iconNameObject.transform, "TooltipHeader").GetComponent<LocalizedText>();
        header.Text.fontSize *= 2f;
        header.Text.color = headerColor;
        subHeader = subHeaderObject.GetComponent<LocalizedText>();
        subHeader.Text.enableAutoSizing = false;
        subHeader.Text.autoSizeTextContainer = false;
        subHeader.Text.enableWordWrapping = false;

        // Configure the subheader's content size fitter
        ContentSizeFitter subHeaderFitter = subHeaderObject.GetComponent<ContentSizeFitter>();
        subHeaderFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        subHeaderFitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

        // Size window and set anchors
        questTransform.sizeDelta = new Vector2(questTransform.sizeDelta.x * 0.65f, questTransform.sizeDelta.y);
        questTransform.anchorMin = new Vector2(1, WindowOffset); // Anchored to bottom-right
        questTransform.anchorMax = new Vector2(1, WindowOffset);
        questTransform.pivot = new Vector2(1, WindowOffset);
        questTransform.anchoredPosition = new Vector2(0, WindowOffset);

        // Set header text
        header.ForceSet(questType.ToString() + " Quest");
        subHeader.ForceSet("UnitName: 0/0"); // For testing, can be updated later

        // Add to active objects
        ActiveObjects.Add(questObject);
        WindowOffset += 0.075f;
    }
    static void ConfigureHorizontalProgressBar(ref GameObject barGameObject, ref GameObject informationPanelObject, ref Image fill, ref LocalizedText level, ref LocalizedText header, UIElement element, Color fillColor, ref LocalizedText firstText, ref LocalizedText secondText, ref LocalizedText thirdText)
    {
        // Instantiate the bar object from the prefab
        barGameObject = GameObject.Instantiate(UICanvasBase.TargetInfoParent.gameObject);

        // DontDestroyOnLoad, change scene
        GameObject.DontDestroyOnLoad(barGameObject);
        SceneManager.MoveGameObjectToScene(barGameObject, SceneManager.GetSceneByName("VRisingWorld"));

        RectTransform barRectTransform = barGameObject.GetComponent<RectTransform>();
        barRectTransform.SetParent(Canvas.transform, false);
        barRectTransform.gameObject.layer = Layer;

        // Set anchor and pivot to middle-upper-right
        float offsetY = BAR_HEIGHT_SPACING * BarNumber;
        float offsetX = 1f - BAR_WIDTH_SPACING;
        barRectTransform.anchorMin = new Vector2(offsetX, 0.6f - offsetY);
        barRectTransform.anchorMax = new Vector2(offsetX, 0.6f - offsetY);
        barRectTransform.pivot = new Vector2(offsetX, 0.6f - offsetY);

        // Best scale found so far for different resolutions
        barRectTransform.localScale = new Vector3(0.7f, 0.7f, 1f);

        // Assign fill, header, and level text components
        fill = FindTargetUIObject(barRectTransform.transform, "Fill").GetComponent<Image>();
        level = FindTargetUIObject(barRectTransform.transform, "LevelText").GetComponent<LocalizedText>();
        header = FindTargetUIObject(barRectTransform.transform, "Name").GetComponent<LocalizedText>();

        // Set initial values
        fill.fillAmount = 0f;
        fill.color = fillColor;
        level.ForceSet("0");
        header.ForceSet(element.ToString());
        header.Text.fontSize *= 1.5f;

        // Set these to 0 so don't appear, deactivating instead seemed funky
        FindTargetUIObject(barRectTransform.transform, "DamageTakenFill").GetComponent<Image>().fillAmount = 0f;
        FindTargetUIObject(barRectTransform.transform, "AbsorbFill").GetComponent<Image>().fillAmount = 0f;

        // Configure informationPanels
        informationPanelObject = FindTargetUIObject(barRectTransform.transform, "InformationPanel");
        ConfigureInformationPanel(ref informationPanelObject, ref firstText, ref secondText, ref thirdText, element);

        // Increment for spacing
        BarNumber++;
        barGameObject.SetActive(true);
        ActiveObjects.Add(barGameObject);
    }
    static void ConfigureVerticalProgressBar(ref GameObject barGameObject, ref Image fill, ref LocalizedText level, Color fillColor)
    {
        // Instantiate the bar object from the prefab
        barGameObject = GameObject.Instantiate(UICanvasBase.TargetInfoParent.gameObject);

        // Don't destroy on load, move to the correct scene
        GameObject.DontDestroyOnLoad(barGameObject);
        SceneManager.MoveGameObjectToScene(barGameObject, SceneManager.GetSceneByName("VRisingWorld"));

        RectTransform barRectTransform = barGameObject.GetComponent<RectTransform>();
        barRectTransform.SetParent(Canvas.transform, false);
        barRectTransform.gameObject.layer = Layer;

        // Define the number of professions (bars)
        int totalBars = 8;

        // Calculate the total width and height for the bars
        float totalBarAreaWidth = 0.185f;
        float barWidth = totalBarAreaWidth / (float)totalBars; // Width of each bar

        // Calculate the starting X position to center the bar graph and position added bars appropriately
        float padding = 1f - (0.075f * 2.25f); // BAR_WIDTH_SPACING previously 0.075f
        float offsetX = padding + (barWidth * GraphBarNumber / 1.45f); // previously used 1.5f

        // scale size
        Vector3 updatedScale = new(0.4f, 1f, 1f);
        barRectTransform.localScale = updatedScale;

        // positioning
        float offsetY = 0.235f; // try 0.24f if needs adjusting?
        barRectTransform.anchorMin = new Vector2(offsetX, offsetY);
        barRectTransform.anchorMax = new Vector2(offsetX, offsetY);
        barRectTransform.pivot = new Vector2(offsetX, offsetY);

        // Assign fill and level text components
        fill = FindTargetUIObject(barRectTransform.transform, "Fill").GetComponent<Image>();
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = 0;
        fill.fillAmount = 0f; // This will be set based on profession level
        fill.color = fillColor;

        // **Rotate the bar by 90 degrees around the Z-axis**
        barRectTransform.localRotation = Quaternion.Euler(0, 0, 90);

        // Assign and adjust the level text component
        level = FindTargetUIObject(barRectTransform.transform, "LevelText").GetComponent<LocalizedText>();
        RectTransform levelRectTransform = level.GetComponent<RectTransform>();

        // **Rotate the level text back by -90 degrees to keep it upright**
        levelRectTransform.localRotation = Quaternion.Euler(0, 0, -90);

        // Hide unnecessary UI elements
        var headerObject = FindTargetUIObject(barRectTransform.transform, "Name");
        headerObject?.SetActive(false);

        GameObject informationPanelObject = FindTargetUIObject(barRectTransform.transform, "InformationPanel");
        informationPanelObject?.SetActive(false);

        // Set these to 0 so don't appear, deactivating instead seemed funky
        FindTargetUIObject(barRectTransform.transform, "DamageTakenFill").GetComponent<Image>().fillAmount = 0f;
        FindTargetUIObject(barRectTransform.transform, "AbsorbFill").GetComponent<Image>().fillAmount = 0f;

        // Set the level text to display the profession level
        level.ForceSet("0"); // Or set to the profession level if available
        
        // Increment GraphBarNumber for horizontal spacing within the bar graph
        GraphBarNumber++;

        barGameObject.SetActive(true);
        ActiveObjects.Add(barGameObject);
    }
    static void ConfigureInformationPanel(ref GameObject informationPanelObject, ref LocalizedText firstText, ref LocalizedText secondText, ref LocalizedText thirdText, UIElement element)
    {
        switch (element)
        {
            case UIElement.Experience:
                ConfigureExperiencePanel(ref informationPanelObject, ref firstText, ref secondText, ref thirdText);
                break;
            default:
                ConfigureDefaultPanel(ref informationPanelObject, ref firstText, ref secondText, ref thirdText);
                break;
        }
    }
    static void ConfigureExperiencePanel(ref GameObject panel, ref LocalizedText firstText, ref LocalizedText secondText, ref LocalizedText thirdText)
    {
        secondText = FindTargetUIObject(panel.transform, "ProffesionInfo").GetComponent<LocalizedText>();
        secondText.Text.fontSize *= 1.2f;
        secondText.ForceSet("");
        secondText.enabled = false;

        firstText = FindTargetUIObject(panel.transform, "BloodInfo").GetComponent<LocalizedText>();
        firstText.ForceSet("");
        firstText.enabled = false;

        thirdText = FindTargetUIObject(panel.transform, "PlatformUserName").GetComponent<LocalizedText>();
        thirdText.ForceSet("");
        thirdText.enabled = false;
    }
    static void ConfigureDefaultPanel(ref GameObject panel, ref LocalizedText firstText, ref LocalizedText secondText, ref LocalizedText thirdText)
    {
        firstText = FindTargetUIObject(panel.transform, "BloodInfo").GetComponent<LocalizedText>();
        firstText.Text.fontSize *= 1.1f;
        firstText.ForceSet("");
        firstText.enabled = false;

        secondText = FindTargetUIObject(panel.transform, "ProffesionInfo").GetComponent<LocalizedText>(); // having to refer to mispelled names is fun >_>
        secondText.Text.fontSize *= 1.1f;
        secondText.ForceSet("");
        secondText.enabled = false;

        thirdText = FindTargetUIObject(panel.transform, "PlatformUserName").GetComponent<LocalizedText>();
        thirdText.Text.fontSize *= 1.1f;
        thirdText.ForceSet("");
        thirdText.enabled = false;
    }
    static float ClassSynergy<T>(T statType, PlayerClass classType, Dictionary<PlayerClass, (List<WeaponStatType> WeaponStatTypes, List<BloodStatType> BloodStatTypes)> classStatSynergy)
    {
        if (classType.Equals(PlayerClass.None))
            return 1f;

        // Check if the stat type exists in the class synergies for the current class
        if (typeof(T) == typeof(WeaponStatType) && classStatSynergy[classType].WeaponStatTypes.Contains((WeaponStatType)(object)statType))
        {
            return ClassStatMultiplier;
        }
        else if (typeof(T) == typeof(BloodStatType) && classStatSynergy[classType].BloodStatTypes.Contains((BloodStatType)(object)statType))
        {
            return ClassStatMultiplier;
        }

        return 1f; // Return default multiplier if stat is not present in the class synergy
    }
    static string FormatWeaponStat(WeaponStatType weaponStat, float statValue)
    {
        string statValueString = WeaponStatFormats[weaponStat] switch
        {
            "integer" => ((int)statValue).ToString(),
            "decimal" => statValue.ToString("F2"),
            "percentage" => (statValue * 100f).ToString("F0") + "%",
            _ => statValue.ToString(),
        };

        string displayString = $"<color=#00FFFF>{WeaponStatTypeAbbreviations[weaponStat]}</color>: <color=#90EE90>{statValueString}</color>";
        return displayString;
    }
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
    static void ToggleUIObjects()
    {
        Active = !Active;

        foreach (GameObject gameObject in ActiveObjects)
        {
            gameObject.active = Active;
        }
    }
    public static class GameObjectUtilities
    {
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
        public static void FindLoadedObjects<T>() where T : UnityEngine.Object
        {
            Il2CppReferenceArray<UnityEngine.Object> resources = Resources.FindObjectsOfTypeAll(Il2CppType.Of<T>());
            Core.Log.LogInfo($"Found {resources.Length} {Il2CppType.Of<T>().FullName}'s!");
            foreach (UnityEngine.Object resource in resources)
            {
                Core.Log.LogInfo($"Sprite: {resource.name}");
            }
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
        public static void FindSpritesByName(List<string> SpriteNames)
        {
            Il2CppArrayBase<Sprite> allSprites = Resources.FindObjectsOfTypeAll<Sprite>();

            var matchedSprites = allSprites
                .Where(sprite => SpriteNames.Contains(sprite.name))
                .ToDictionary(sprite => SpriteNames.First(pair => pair == sprite.name), sprite => sprite);

            foreach (var pair in matchedSprites)
            {
                SpriteMap[pair.Key] = pair.Value;
            }
        }
    }
    static void OnDropDownChanged(int optionIndex)
    {
        Core.Log.LogInfo($"OnDropDownChanged {optionIndex}");
    }

    static void OnDeselect(bool selected)
    {
        Core.Log.LogInfo($"OnDeselect {selected}");
    }
}
/*
    static GameObject DropdownMenu;
    static TMP_Dropdown DropdownSelection;
    static List<string> Selections = ["1","2","3"];
    static LocalizedText DropdownText;
    static void ConfigureFamiliarObject()
    {
        
        OptionsPanel_Interface optionsPanelInterface = GameObject.FindObjectOfType<OptionsPanel_Interface>();

        if (optionsPanelInterface != null)
        {
            Core.Log.LogInfo("OptionsPanel_Interface found!");

            familiarObject = new GameObject("FamiliarObject");

            GameObject.DontDestroyOnLoad(familiarObject);
            SceneManager.MoveGameObjectToScene(familiarObject, SceneManager.GetSceneByName("VRisingWorld"));

            RectTransform familiarTransform = familiarObject.AddComponent<RectTransform>();
            familiarTransform.SetParent(Canvas.transform, false);
            familiarObject.layer = Layer;

            familiarTransform.anchorMin = new Vector2(0.5f, 0.5f);
            familiarTransform.anchorMax = new Vector2(0.5f, 0.5f);
            familiarTransform.pivot = new Vector2(0.5f, 0.5f);

            List<string> stringOptions = ["Box1", "Box2", "Box3"];
            Il2CppSystem.Collections.Generic.List<string> testOptions = new(stringOptions.Count);

            foreach (string testOption in stringOptions)
            {
                testOptions.Add(testOption);
            }

            OptionsHelper.AddDropdown(familiarTransform, optionsPanelInterface.DropdownPrefab, false, LocalizationKey.Empty, LocalizationKey.Empty, testOptions, 1, 2, new Action<int>(OnDropDownChanged));
            familiarObject.SetActive(true);
        }
        else
        {
            Core.Log.LogInfo("OptionsPanel_Interface not found...");
        }
        

        try
        {
            Core.Log.LogInfo("Creating dropdown menu...");
            GameObject dropdownMenuObject = new("DropdownMenu");
DropdownMenu = dropdownMenuObject;
            RectTransform dropdownRectTransform = dropdownMenuObject.AddComponent<RectTransform>();

Core.Log.LogInfo("Making persistent and moving to scene before setting parent...");
            GameObject.DontDestroyOnLoad(dropdownMenuObject);
            SceneManager.MoveGameObjectToScene(dropdownMenuObject, SceneManager.GetSceneByName("VRisingWorld"));
            dropdownRectTransform.SetParent(Canvas.transform, false);

            Core.Log.LogInfo("Adding Dropdown component...");
            TMP_Dropdown dropdownMenu = dropdownMenuObject.AddComponent<TMP_Dropdown>();
DropdownSelection = dropdownMenu;

            Core.Log.LogInfo("Setting dropdown position/anchors...");
            dropdownRectTransform.anchorMin = new Vector2(0.5f, 0.5f);
dropdownRectTransform.anchorMax = new Vector2(0.5f, 0.5f);
dropdownRectTransform.pivot = new Vector2(0.5f, 0.5f);
dropdownRectTransform.anchoredPosition = Vector2.zero;
            dropdownRectTransform.sizeDelta = new Vector2(160f, 30f);

// Caption Text
Core.Log.LogInfo("Creating caption text...");
            GameObject captionObject = new("CaptionText");
TextMeshProUGUI dropdownText = captionObject.AddComponent<TextMeshProUGUI>();
dropdownText.text = "Familiar Boxes";
            RectTransform captionRectTransform = captionObject.GetComponent<RectTransform>();

GameObject.DontDestroyOnLoad(captionObject);
            SceneManager.MoveGameObjectToScene(captionObject, SceneManager.GetSceneByName("VRisingWorld"));
            captionObject.transform.SetParent(dropdownMenu.transform, false);

            Core.Log.LogInfo("Setting caption anchors and size...");
            captionRectTransform.anchorMin = new Vector2(0, 0.5f);
captionRectTransform.anchorMax = new Vector2(1, 0.5f);
captionRectTransform.pivot = new Vector2(0.5f, 0.5f);
captionRectTransform.sizeDelta = new Vector2(0, 30f);
captionRectTransform.anchoredPosition = Vector2.zero;

            Core.Log.LogInfo("Setting caption text properties...");
            dropdownText.font = ExperienceClassText.Text.font;
            dropdownText.fontSize = (int) ExperienceClassText.Text.fontSize;
dropdownText.color = ExperienceClassText.Text.color;
            dropdownText.alignment = TextAlignmentOptions.Center;

            // Create Dropdown Template
            Core.Log.LogInfo("Creating dropdown template...");
            GameObject templateObject = new("Template");
RectTransform templateRectTransform = templateObject.AddComponent<RectTransform>();
templateObject.transform.SetParent(dropdownMenuObject.transform, false);

            templateRectTransform.anchorMin = new Vector2(0, 0);
templateRectTransform.anchorMax = new Vector2(1, 0);
templateRectTransform.pivot = new Vector2(0.5f, 1f);
templateRectTransform.sizeDelta = new Vector2(0, 90f);

// Add background to the template
Core.Log.LogInfo("Adding background to template...");
            Image templateBackground = templateObject.AddComponent<Image>();
templateBackground.color = new Color(0, 0, 0, 0.5f);

// Create Viewport
GameObject viewportObject = new("Viewport");
RectTransform viewportRectTransform = viewportObject.AddComponent<RectTransform>();
viewportObject.transform.SetParent(templateObject.transform, false);

            viewportRectTransform.anchorMin = Vector2.zero;
            viewportRectTransform.anchorMax = Vector2.one;
            viewportRectTransform.sizeDelta = Vector2.zero;

            Mask viewportMask = viewportObject.AddComponent<Mask>();
viewportMask.showMaskGraphic = false;

            // Create Content
            Core.Log.LogInfo("Creating content object...");
            GameObject contentObject = new("Content");
RectTransform contentRectTransform = contentObject.AddComponent<RectTransform>();
contentObject.transform.SetParent(viewportObject.transform, false);

            contentRectTransform.anchorMin = new Vector2(0, 1);
contentRectTransform.anchorMax = new Vector2(1, 1);
contentRectTransform.pivot = new Vector2(0.5f, 1f);
contentRectTransform.sizeDelta = new Vector2(0, 90f);

// Create Item Template
GameObject itemObject = new("Item");
itemObject.transform.SetParent(contentObject.transform, false);

            RectTransform itemRectTransform = itemObject.AddComponent<RectTransform>();
itemRectTransform.anchorMin = new Vector2(0, 0.5f);
itemRectTransform.anchorMax = new Vector2(1, 0.5f);
itemRectTransform.sizeDelta = new Vector2(0, 25f);

// Add Toggle to the item
Core.Log.LogInfo("Adding toggle to item...");
            Toggle itemToggle = itemObject.AddComponent<Toggle>();
itemToggle.isOn = false;

            // Create 'Item Background'
            GameObject itemBackgroundObject = new("ItemBackground");
itemBackgroundObject.transform.SetParent(itemObject.transform, false);

            RectTransform itemBackgroundRect = itemBackgroundObject.AddComponent<RectTransform>();
itemBackgroundRect.anchorMin = Vector2.zero;
            itemBackgroundRect.anchorMax = Vector2.one;
            itemBackgroundRect.sizeDelta = Vector2.zero;

            Image itemBackgroundImage = itemBackgroundObject.AddComponent<Image>();
itemBackgroundImage.color = new Color(1, 1, 1, 1);

// Create 'Item Checkmark'
GameObject itemCheckmarkObject = new("ItemCheckmark");
itemCheckmarkObject.transform.SetParent(itemBackgroundObject.transform, false);

            RectTransform itemCheckmarkRect = itemCheckmarkObject.AddComponent<RectTransform>();
itemCheckmarkRect.anchorMin = new Vector2(0, 0.5f);
itemCheckmarkRect.anchorMax = new Vector2(0, 0.5f);
itemCheckmarkRect.pivot = new Vector2(0.5f, 0.5f);
itemCheckmarkRect.sizeDelta = new Vector2(20, 20);
itemCheckmarkRect.anchoredPosition = new Vector2(10, 0);

Image itemCheckmarkImage = itemCheckmarkObject.AddComponent<Image>();
// Assign a sprite to the checkmark image if available
// itemCheckmarkImage.sprite = yourCheckmarkSprite;

// Create 'Item Label'
GameObject itemLabelObject = new("ItemLabel");
itemLabelObject.transform.SetParent(itemBackgroundObject.transform, false);

            RectTransform itemLabelRect = itemLabelObject.AddComponent<RectTransform>();
itemLabelRect.anchorMin = new Vector2(0, 0);
itemLabelRect.anchorMax = new Vector2(1, 1);
itemLabelRect.offsetMin = new Vector2(20, 0);
itemLabelRect.offsetMax = new Vector2(0, 0);

TextMeshProUGUI itemLabelText = itemLabelObject.AddComponent<TextMeshProUGUI>();
itemLabelText.font = ExperienceClassText.Text.font;
            itemLabelText.fontSize = (int) ExperienceClassText.Text.fontSize;
itemLabelText.color = ExperienceClassText.Text.color;
            itemLabelText.alignment = TextAlignmentOptions.Left;
            itemLabelText.text = "Option";

            // Configure the Toggle component
            itemToggle.targetGraphic = itemBackgroundImage;
            itemToggle.graphic = itemCheckmarkImage;
            itemToggle.isOn = false;

            // Assign the itemText property of the dropdown
            dropdownMenu.itemText = itemLabelText;

            // Add ScrollRect to the template
            ScrollRect scrollRect = templateObject.AddComponent<ScrollRect>();
scrollRect.content = contentRectTransform;
            scrollRect.viewport = viewportRectTransform;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;

            // Disable the item template and template by default
            itemObject.SetActive(false);
            templateObject.SetActive(false);

            // Set layers
            itemObject.layer = Canvas.gameObject.layer;
            itemBackgroundObject.layer = Canvas.gameObject.layer;
            itemCheckmarkObject.layer = Canvas.gameObject.layer;
            itemLabelObject.layer = Canvas.gameObject.layer;

            // Remove redundant addition of TextMeshProUGUI to itemObject

            Core.Log.LogInfo("Adding background to dropdown...");
            Image dropdownImage = dropdownMenuObject.AddComponent<Image>();
dropdownImage.color = new Color(0, 0, 0, 0.5f);
dropdownImage.type = Image.Type.Sliced;

            // Assign properties to the dropdown
            dropdownMenu.template = templateRectTransform;
            dropdownMenu.targetGraphic = dropdownImage;
            dropdownMenu.captionText = dropdownText;
            // dropdownMenu.itemText is already assigned above

            Core.Log.LogInfo("Setting initial dropdown options...");
            dropdownMenu.ClearOptions();
            Il2CppSystem.Collections.Generic.List<string> selections = new(Selections.Count);
            foreach (string selection in Selections)
            {
                selections.Add(selection);
            }

            Core.Log.LogInfo("Adding dropdown options and listener...");
dropdownMenu.AddOptions(selections);
dropdownMenu.RefreshShownValue();
dropdownMenu.onValueChanged.AddListener(new Action<int>(OnDropDownChanged));

Core.Log.LogInfo("Setting layer and activating...");
dropdownMenuObject.layer = Canvas.gameObject.layer;
dropdownMenuObject.SetActive(true);

            
            Core.Log.LogInfo("Creating dropdown menu...");
            // might need to use own canvas for this if BottomBarParent throws a fit which it probably will
            GameObject dropdownMenuObject = new("DropdownMenu");
            DropdownMenu = dropdownMenuObject;
            RectTransform dropdownRectTransform = dropdownMenuObject.AddComponent<RectTransform>();

            Core.Log.LogInfo("Making persistent and moving to scene before setting parent...");
            // DontDestroyOnLoad, move to proper scene, set canvas as parent
            GameObject.DontDestroyOnLoad(dropdownMenuObject);
            SceneManager.MoveGameObjectToScene(dropdownMenuObject, SceneManager.GetSceneByName("VRisingWorld"));
            dropdownRectTransform.SetParent(Canvas.transform, false);

            Core.Log.LogInfo("Adding Dropdown component...");
            // Add dropdown components and configure position, starting state, etc
            TMP_Dropdown dropdownMenu = dropdownMenuObject.AddComponent<TMP_Dropdown>();
            DropdownSelection = dropdownMenu;

            Core.Log.LogInfo("Setting dropdown position/anchors...");     
            dropdownRectTransform.anchorMin = new Vector2(0.5f, 0.5f);  // Centered
            dropdownRectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            dropdownRectTransform.pivot = new Vector2(0.5f, 0.5f);
            dropdownRectTransform.anchoredPosition = Vector2.zero;  // Centered on screen
            dropdownRectTransform.sizeDelta = new Vector2(160f, 30f);  // Test with a larger size

            // captionText
            Core.Log.LogInfo("Creating caption text...");
            GameObject captionObject = new("CaptionText");
            TextMeshProUGUI dropdownText = captionObject.AddComponent<TextMeshProUGUI>();
            dropdownText.text = "Familiar Boxes";
            RectTransform captionRectTransform = captionObject.AddComponent<RectTransform>();

            // DontDestroyOnLoad, move to proper scene, set canvas as parent
            GameObject.DontDestroyOnLoad(captionObject);
            SceneManager.MoveGameObjectToScene(captionObject, SceneManager.GetSceneByName("VRisingWorld"));
            captionObject.transform.SetParent(dropdownMenu.transform, false);

            // Anchor the text to stretch across the width of the dropdown
            Core.Log.LogInfo("Setting caption anchors and size...");
            captionRectTransform.anchorMin = new Vector2(0, 0.5f);  // Anchored to the middle of the dropdown
            captionRectTransform.anchorMax = new Vector2(1, 0.5f);  // Stretched horizontally
            captionRectTransform.pivot = new Vector2(0.5f, 0.5f);   // Center pivot

            // Set size and position relative to dropdown
            captionRectTransform.sizeDelta = new Vector2(0, 30f);   // Matches dropdown height (30 units)
            captionRectTransform.anchoredPosition = new Vector2(0, 0);  // Centered within dropdown

            // Configure the font, font size, and other properties
            Core.Log.LogInfo("Setting caption text properties...");
            dropdownText.font = ExperienceClassText.Text.font;
            dropdownText.fontSize = (int)ExperienceClassText.Text.fontSize;
            dropdownText.color = ExperienceClassText.Text.color;
            dropdownText.alignment = TextAlignmentOptions.Center;

            // Create Dropdown Template (needed for displaying options)
            Core.Log.LogInfo("Creating dropdown template...");
            GameObject templateObject = new("Template");
            RectTransform templateRectTransform = templateObject.AddComponent<RectTransform>();
            templateObject.transform.SetParent(dropdownMenuObject.transform, false);

            // Set up the template’s size and positioning
            templateRectTransform.anchorMin = new Vector2(0, 0);
            templateRectTransform.anchorMax = new Vector2(1, 0);
            templateRectTransform.pivot = new Vector2(0.5f, 1f);
            templateRectTransform.sizeDelta = new Vector2(0, 90f);  // Size for showing options

            // Add a background to the template (optional for styling)
            Core.Log.LogInfo("Adding background to template...");
            Image templateBackground = templateObject.AddComponent<Image>();
            templateBackground.color = new Color(0, 0, 0, 0.5f);  // Semi-transparent background

            // Create Viewport for scrolling within the template
            GameObject viewportObject = new("Viewport");
            RectTransform viewportRectTransform = viewportObject.AddComponent<RectTransform>();
            viewportObject.transform.SetParent(templateObject.transform, false);

            viewportRectTransform.anchorMin = Vector2.zero;
            viewportRectTransform.anchorMax = Vector2.one;
            viewportRectTransform.sizeDelta = Vector2.zero;

            Mask viewportMask = viewportObject.AddComponent<Mask>();
            viewportMask.showMaskGraphic = false;  // Hide the mask graphic

            // Create Content for the options list
            Core.Log.LogInfo("Creating content object...");
            GameObject contentObject = new("Content");
            RectTransform contentRectTransform = contentObject.AddComponent<RectTransform>();
            contentObject.transform.SetParent(viewportObject.transform, false);

            contentRectTransform.anchorMin = new Vector2(0, 1);
            contentRectTransform.anchorMax = new Vector2(1, 1);
            contentRectTransform.pivot = new Vector2(0.5f, 1f);
            contentRectTransform.sizeDelta = new Vector2(0, 90f);

            // Create Item (this will be duplicated for each dropdown option)
            GameObject itemObject = new("Item");
            itemObject.transform.SetParent(contentObject.transform, false);

            RectTransform itemRectTransform = itemObject.AddComponent<RectTransform>();
            itemRectTransform.anchorMin = new Vector2(0, 0.5f);
            itemRectTransform.anchorMax = new Vector2(1, 0.5f);
            itemRectTransform.sizeDelta = new Vector2(0, 25f);

            // Add Toggle to the item (this is required for each option)
            Core.Log.LogInfo("Adding toggle to item...");
            Toggle itemToggle = itemObject.AddComponent<Toggle>();
            itemToggle.isOn = false;  // Default to off

            // Create 'Item Background' GameObject
            GameObject itemBackgroundObject = new("ItemBackground");
            itemBackgroundObject.transform.SetParent(itemObject.transform, false);

            RectTransform itemBackgroundRect = itemBackgroundObject.AddComponent<RectTransform>();
            itemBackgroundRect.anchorMin = Vector2.zero;
            itemBackgroundRect.anchorMax = Vector2.one;
            itemBackgroundRect.sizeDelta = Vector2.zero;

            Image itemBackgroundImage = itemBackgroundObject.AddComponent<Image>();
            itemBackgroundImage.color = new Color(1, 1, 1, 1); // White background

            // Create 'Item Checkmark' GameObject
            GameObject itemCheckmarkObject = new("ItemCheckmark");
            itemCheckmarkObject.transform.SetParent(itemBackgroundObject.transform, false);

            RectTransform itemCheckmarkRect = itemCheckmarkObject.AddComponent<RectTransform>();
            itemCheckmarkRect.anchorMin = new Vector2(0, 0.5f);
            itemCheckmarkRect.anchorMax = new Vector2(0, 0.5f);
            itemCheckmarkRect.pivot = new Vector2(0.5f, 0.5f);
            itemCheckmarkRect.sizeDelta = new Vector2(20, 20);
            itemCheckmarkRect.anchoredPosition = new Vector2(10, 0);

            Image itemCheckmarkImage = itemCheckmarkObject.AddComponent<Image>();
            // Assign a sprite to the checkmark image if available
            // itemCheckmarkImage.sprite = yourCheckmarkSprite;

            // Create 'Item Label' GameObject
            GameObject itemLabelObject = new("ItemLabel");
            itemLabelObject.transform.SetParent(itemBackgroundObject.transform, false);

            RectTransform itemLabelRect = itemLabelObject.AddComponent<RectTransform>();
            itemLabelRect.anchorMin = new Vector2(0, 0);
            itemLabelRect.anchorMax = new Vector2(1, 1);
            itemLabelRect.offsetMin = new Vector2(20, 0); // Left padding
            itemLabelRect.offsetMax = new Vector2(0, 0);

            TextMeshProUGUI itemLabelText = itemLabelObject.AddComponent<TextMeshProUGUI>();
            itemLabelText.font = ExperienceClassText.Text.font;
            itemLabelText.fontSize = (int)ExperienceClassText.Text.fontSize;
            itemLabelText.color = ExperienceClassText.Text.color;
            itemLabelText.alignment = TextAlignmentOptions.Left;
            itemLabelText.text = "Option"; // Placeholder text

            // Configure the Toggle component
            itemToggle.targetGraphic = itemBackgroundImage;
            itemToggle.graphic = itemCheckmarkImage;
            itemToggle.isOn = false;

            // Assign the itemText property of the dropdown
            dropdownMenu.itemText = itemLabelText;

            // Add ScrollRect to the template
            ScrollRect scrollRect = templateObject.AddComponent<ScrollRect>();
            scrollRect.content = contentRectTransform;
            scrollRect.viewport = viewportRectTransform;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;

            // Disable the template by default
            templateObject.SetActive(false);

            // set layers
            itemObject.layer = Canvas.gameObject.layer;
            itemBackgroundObject.layer = Canvas.gameObject.layer;
            itemCheckmarkObject.layer = Canvas.gameObject.layer;
            itemLabelObject.layer = Canvas.gameObject.layer;

            // Add TextMeshProUGUI for the option text
            Core.Log.LogInfo("Adding text to item...");
            TextMeshProUGUI itemText = itemObject.AddComponent<TextMeshProUGUI>();
            //itemText.text = "Option";  // Placeholder text for the option
            itemText.font = ExperienceClassText.Text.font;
            itemText.fontSize = (int)ExperienceClassText.Text.fontSize;
            itemText.color = ExperienceClassText.Text.color;
            itemText.alignment = TextAlignmentOptions.Center;

            Core.Log.LogInfo("Adding background to dropdown...");
            Image dropdownImage = dropdownMenuObject.AddComponent<Image>();
            dropdownImage.color = new Color(0, 0, 0, 0.5f);
            dropdownImage.type = Image.Type.Sliced;

            dropdownMenu.template = templateRectTransform;
            dropdownMenu.targetGraphic = dropdownImage;
            dropdownMenu.captionText = dropdownText;
            dropdownMenu.itemText = itemText;

            Core.Log.LogInfo("Setting initial dropdown options...");
            // clear defaults, set empty options
            dropdownMenu.ClearOptions();
            Il2CppSystem.Collections.Generic.List<string> selections = new(Selections.Count);
            foreach (string selection in Selections)
            {
                selections.Add(selection);
            }

            Core.Log.LogInfo("Adding dropdown options and listener...");
            dropdownMenu.AddOptions(selections);
            dropdownMenu.RefreshShownValue();
            dropdownMenu.onValueChanged.AddListener(new Action<int>(OnDropDownChanged));

            Core.Log.LogInfo("Setting layer and activating...");
            dropdownMenuObject.layer = Canvas.gameObject.layer;
            dropdownMenuObject.SetActive(true);
            
        }
        catch (Exception e)
        {
            Core.Log.LogError(e);
        }
    }
public static GameObject CreateUIObject(string name, GameObject parent, Vector2 sizeDelta = default)
{
    GameObject obj = new(name)
    {
        layer = 5,
        hideFlags = HideFlags.HideAndDontSave,
    };

    if (parent)
    {
        obj.transform.SetParent(parent.transform, false);
    }

    RectTransform rect = obj.AddComponent<RectTransform>();
    rect.sizeDelta = sizeDelta;
    return obj;
}
//static GameObject DropdownMenu;
//static TMP_Dropdown DropdownSelection;
//static List<string> Selections = ["1","2","3"];
//static LocalizedText DropdownText;

static void OnDropDownChanged(int optionIndex)
{
    Core.Log.LogInfo($"Selected {Selections[optionIndex]}");
}

if (Familiars) // drop down menu testing
{
    try
    {
        Core.Log.LogInfo("Creating dropdown menu...");
        // might need to use own canvas for this if BottomBarParent throws a fit which it probably will
        GameObject dropdownMenuObject = new("DropdownMenu");
        DropdownMenu = dropdownMenuObject;
        RectTransform dropdownRectTransform = dropdownMenuObject.AddComponent<RectTransform>();

        Core.Log.LogInfo("Making persistent and moving to scene before setting parent...");
        // DontDestroyOnLoad, move to proper scene, set canvas as parent
        GameObject.DontDestroyOnLoad(dropdownMenuObject);
        SceneManager.MoveGameObjectToScene(dropdownMenuObject, SceneManager.GetSceneByName("VRisingWorld"));
        dropdownRectTransform.SetParent(bottomBarCanvas.transform, false);

        Core.Log.LogInfo("Adding Dropdown component...");
        // Add dropdown components and configure position, starting state, etc
        TMP_Dropdown dropdownMenu = dropdownMenuObject.AddComponent<TMP_Dropdown>();
        DropdownSelection = dropdownMenu;

        Core.Log.LogInfo("Setting dropdown position/anchors...");

        // set anchors/pivot

        //dropdownRectTransform.anchorMin = new Vector2(1, 0.2f); // Anchored to bottom-right
        //dropdownRectTransform.anchorMax = new Vector2(1, 0.2f);
        //dropdownRectTransform.pivot = new Vector2(1, 0.2f);
        //dropdownRectTransform.anchoredPosition = new Vector2(0f, 0.2f); // Anchored to the bottom right corner above quest windows
        //dropdownRectTransform.sizeDelta = new Vector2(80f, 30f);        


        dropdownRectTransform.anchorMin = new Vector2(0.5f, 0.5f);  // Centered
        dropdownRectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        dropdownRectTransform.pivot = new Vector2(0.5f, 0.5f);
        dropdownRectTransform.anchoredPosition = Vector2.zero;  // Centered on screen
        dropdownRectTransform.sizeDelta = new Vector2(160f, 30f);  // Test with a larger size

        // captionText
        GameObject captionObject = new("CaptionText");
        TMP_Text dropdownText = captionObject.AddComponent<TMP_Text>();
        dropdownText.text = "FamiliarBoxes";
        RectTransform captionRectTransform = captionObject.GetComponent<RectTransform>();

        // DontDestroyOnLoad, move to proper scene, set canvas as parent
        GameObject.DontDestroyOnLoad(captionObject);
        SceneManager.MoveGameObjectToScene(captionObject, SceneManager.GetSceneByName("VRisingWorld"));
        captionObject.transform.SetParent(dropdownMenu.transform, false);

        // Anchor the text to stretch across the width of the dropdown
        captionRectTransform.anchorMin = new Vector2(0, 0.5f);  // Anchored to the middle of the dropdown
        captionRectTransform.anchorMax = new Vector2(1, 0.5f);  // Stretched horizontally
        captionRectTransform.pivot = new Vector2(0.5f, 0.5f);   // Center pivot

        // Set size and position relative to dropdown
        captionRectTransform.sizeDelta = new Vector2(0, 30f);   // Matches dropdown height (30 units)
        captionRectTransform.anchoredPosition = new Vector2(0, 0);  // Centered within dropdown

        // Configure the font, font size, and other properties
        dropdownText.font = ExperienceClassText.Text.font;
        dropdownText.fontSize = (int)ExperienceClassText.Text.fontSize;
        dropdownText.color = ExperienceClassText.Text.color;
        dropdownText.alignment = TextAlignmentOptions.Center;

        // Create Dropdown Template (needed for displaying options)
        GameObject templateObject = new("Template");
        RectTransform templateRectTransform = templateObject.AddComponent<RectTransform>();
        templateObject.transform.SetParent(dropdownMenuObject.transform, false);

        // Set up the template’s size and positioning
        templateRectTransform.anchorMin = new Vector2(0, 0);
        templateRectTransform.anchorMax = new Vector2(1, 0);
        templateRectTransform.pivot = new Vector2(0.5f, 1f);
        templateRectTransform.sizeDelta = new Vector2(0, 90f);  // Size for showing options

        // Add a background to the template (optional for styling)
        Image templateBackground = templateObject.AddComponent<Image>();
        templateBackground.color = new Color(0, 0, 0, 0.5f);  // Semi-transparent background

        // Create Viewport for scrolling within the template
        GameObject viewportObject = new("Viewport");
        RectTransform viewportRectTransform = viewportObject.AddComponent<RectTransform>();
        viewportObject.transform.SetParent(templateObject.transform, false);

        viewportRectTransform.anchorMin = Vector2.zero;
        viewportRectTransform.anchorMax = Vector2.one;
        viewportRectTransform.sizeDelta = Vector2.zero;

        Mask viewportMask = viewportObject.AddComponent<Mask>();
        viewportMask.showMaskGraphic = false;  // Hide the mask graphic

        // Create Content for the options list
        GameObject contentObject = new("Content");
        RectTransform contentRectTransform = contentObject.AddComponent<RectTransform>();
        contentObject.transform.SetParent(viewportObject.transform, false);

        contentRectTransform.anchorMin = new Vector2(0, 1);
        contentRectTransform.anchorMax = new Vector2(1, 1);
        contentRectTransform.pivot = new Vector2(0.5f, 1f);
        contentRectTransform.sizeDelta = new Vector2(0, 90f);

        // Create Item (this will be duplicated for each dropdown option)
        GameObject itemObject = new("Item");
        itemObject.transform.SetParent(contentObject.transform, false);

        RectTransform itemRectTransform = itemObject.AddComponent<RectTransform>();
        itemRectTransform.anchorMin = new Vector2(0, 0.5f);
        itemRectTransform.anchorMax = new Vector2(1, 0.5f);
        itemRectTransform.sizeDelta = new Vector2(0, 25f);

        // Add Toggle to the item (this is required for each option)
        Toggle itemToggle = itemObject.AddComponent<Toggle>();
        itemToggle.isOn = false;  // Default to off

        // Add TextMeshProUGUI for the option text
        TMP_Text itemText = itemObject.AddComponent<TMP_Text>();
        //itemText.text = "Option";  // Placeholder text for the option
        itemText.font = ExperienceClassText.Text.font;
        itemText.fontSize = (int)ExperienceClassText.Text.fontSize;
        itemText.color = ExperienceClassText.Text.color;
        itemText.alignment = TextAlignmentOptions.Center;

        Image dropdownImage = dropdownMenuObject.AddComponent<Image>();
        dropdownImage.color = new Color(0, 0, 0, 0.5f);
        dropdownImage.type = Image.Type.Sliced;

        dropdownMenu.template = templateRectTransform;
        dropdownMenu.targetGraphic = dropdownImage;
        dropdownMenu.captionText = dropdownText;
        dropdownMenu.itemText = itemText;

        Core.Log.LogInfo("Setting initial dropdown options...");
        // clear defaults, set empty options
        dropdownMenu.ClearOptions();
        Il2CppSystem.Collections.Generic.List<string> selections = new(Selections.Count);
        foreach (string selection in Selections)
        {
            selections.Add(selection);
        }

        Core.Log.LogInfo("Adding dropdown options and listener...");
        dropdownMenu.AddOptions(selections);
        dropdownMenu.RefreshShownValue();
        dropdownMenu.onValueChanged.AddListener(new Action<int>(OnDropDownChanged));

        Core.Log.LogInfo("Setting layer and activating...");
        dropdownMenuObject.layer = CanvasObject.layer;
        dropdownMenuObject.SetActive(true);
    }
    catch (Exception e)
    {
        Core.Log.LogError(e);
    }
}
*/