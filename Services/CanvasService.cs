using Bloodcraft.Resources;
using Eclipse.Patches;
using Eclipse.Utilities;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using ProjectM;
using ProjectM.Shared;
using ProjectM.UI;
using Stunlock.Core;
using StunShared.UI;
using System.Collections;
using System.ComponentModel;
using System.Text.RegularExpressions;
using TMPro;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static Eclipse.Services.CanvasService.GameObjectUtilities;
using static Eclipse.Services.DataService;
using Image = UnityEngine.UI.Image;
using StringComparison = System.StringComparison;

namespace Eclipse.Services;

/// <summary>
/// 管理游戏内的UI元素，例如经验条、任务追踪器和各种信息面板。
/// </summary>
internal class CanvasService
{
    /// <summary>
    /// 获取实体管理器实例。
    /// </summary>
    static EntityManager EntityManager => Core.EntityManager;
    /// <summary>
    /// 获取系统服务实例。
    /// </summary>
    static SystemService SystemService => Core.SystemService;
    /// <summary>
    /// 获取托管数据注册表。
    /// </summary>
    static ManagedDataRegistry ManagedDataRegistry => SystemService.ManagedDataSystem.ManagedDataRegistry;
    /// <summary>
    /// 获取本地玩家角色实体。
    /// </summary>
    static Entity LocalCharacter => Core.LocalCharacter;

    static readonly bool _experienceBar = Plugin.Leveling;
    static readonly bool _showPrestige = Plugin.Prestige;
    static readonly bool _legacyBar = Plugin.Legacies;
    static readonly bool _expertiseBar = Plugin.Expertise;
    static readonly bool _familiarBar = Plugin.Familiars;
    static readonly bool _professionBars = Plugin.Professions;
    static readonly bool _questTracker = Plugin.Quests;
    static readonly bool _shiftSlot = Plugin.ShiftSlot;

    /// <summary>
    /// 定义了各种UI元素的枚举。
    /// </summary>
    public enum UIElement
    {
        /// <summary>
        /// 经验UI元素。
        /// </summary>
        [Description("经验")]
        Experience,

        /// <summary>
        /// 传承UI元素。
        /// </summary>
        [Description("传承")]
        Legacy,

        /// <summary>
        /// 专精UI元素。
        /// </summary>
        [Description("专精")]
        Expertise,

        /// <summary>
        /// 伙伴UI元素。
        /// </summary>
        [Description("伙伴")]
        Familiars,

        /// <summary>
        /// 专业UI元素。
        /// </summary>
        [Description("专业")]
        Professions,

        /// <summary>
        /// 每日任务UI元素。
        /// </summary>
        [Description("每日")]
        Daily,

        /// <summary>
        /// 每周任务UI元素。
        /// </summary>
        [Description("每周")]
        Weekly,

        /// <summary>
        /// 轮换栏位UI元素。
        /// </summary>
        [Description("轮换栏位")]
        ShiftSlot
    }

    static readonly Dictionary<int, string> _romanNumerals = new()
    {
        {100, "C"}, {90, "XC"}, {50, "L"}, {40, "XL"},
        {10, "X"}, {9, "IX"}, {5, "V"}, {4, "IV"},
        {1, "I"}
    };

    static readonly List<string> _spriteNames =
    [
        "BloodIcon_Cursed",
        "BloodIcon_Small_Cursed",
        "BloodIcon_Small_Holy",
        "BloodIcon_Warrior",
        "BloodIcon_Small_Warrior",
        "Poneti_Icon_Hammer_30",
        "Poneti_Icon_Bag",
        "Poneti_Icon_Res_93",
        SHIFT_SPRITE, // 仍然不明白为什么这个精灵在设置后就是不像其他精灵一样工作，是基础材质的问题吗？不知道
        "Stunlock_Icon_Item_Jewel_Collection4",
        "Stunlock_Icon_Bag_Background_Alchemy",
        "Poneti_Icon_Alchemy_02_mortar",
        "Stunlock_Icon_Bag_Background_Jewel",
        "Poneti_Icon_runic_tablet_12",
        "Stunlock_Icon_Bag_Background_Woodworking",
        "Stunlock_Icon_Bag_Background_Herbs",
        "Poneti_Icon_Herbalism_35_fellherb",
        "Stunlock_Icon_Bag_Background_Fish",
        "Poneti_Icon_Cooking_28_fish",
        "Poneti_Icon_Cooking_60_oceanfish",
        "Stunlock_Icon_Bag_Background_Armor",
        "Poneti_Icon_Tailoring_38_fiercloth",
        "FantasyIcon_ResourceAndCraftAddon (56)",
        "Stunlock_Icon_Bag_Background_Weapon",
        "Poneti_Icon_Sword_v2_48",
        "Poneti_Icon_Hammer_30",
        "Stunlock_Icon_Bag_Background_Consumable",
        "Poneti_Icon_Quest_131",
        "FantasyIcon_Wood_Hallow",
        "Poneti_Icon_Engineering_59_mega_fishingrod",
        "Poneti_Icon_Axe_v2_04",
        "Poneti_Icon_Blacksmith_21_big_grindstone",
        "FantasyIcon_Flowers (11)",
        "FantasyIcon_MagicItem (105)",
        "Item_MagicSource_General_T05_Relic",
        "Stunlock_Icon_BloodRose",
        "Poneti_Icon_Blacksmith_24_bigrune_grindstone",
        "Item_MagicSource_General_T04_FrozenEye",
        "Stunlock_Icon_SpellPoint_Blood1",
        "Stunlock_Icon_SpellPoint_Unholy1",
        "Stunlock_Icon_SpellPoint_Frost1",
        "Stunlock_Icon_SpellPoint_Chaos1",
        "Stunlock_Icon_SpellPoint_Frost1",
        "Stunlock_Icon_SpellPoint_Storm1",
        "Stunlock_Icon_SpellPoint_Illusion1",
        "spell_level_icon"
    ];

    /// <summary>
    /// 技能图标名称的前缀。
    /// </summary>
    public const string ABILITY_ICON = "Stunlock_Icon_Ability_Spell_";
    /// <summary>
    /// NPC技能图标的名称。
    /// </summary>
    public const string NPC_ABILITY = "Ashka_M1_64";

    static readonly Dictionary<Profession, string> _professionIcons = new()
    {
        { Profession.Enchanting, "Item_MagicSource_General_T04_FrozenEye" },
        { Profession.Alchemy, "FantasyIcon_MagicItem (105)" },
        { Profession.Harvesting, "Stunlock_Icon_BloodRose" },
        { Profession.Blacksmithing, "Poneti_Icon_Blacksmith_24_bigrune_grindstone" },
        { Profession.Tailoring, "FantasyIcon_ResourceAndCraftAddon (56)" },
        { Profession.Woodcutting, "Poneti_Icon_Axe_v2_04" },
        { Profession.Mining, "Poneti_Icon_Hammer_30" },
        { Profession.Fishing, "Poneti_Icon_Engineering_59_mega_fishingrod" }
    };
    /// <summary>
    /// 获取已加载精灵的只读字典。键是精灵名称，值是精灵对象。
    /// </summary>
    public static IReadOnlyDictionary<string, Sprite> Sprites => _sprites;
    static readonly Dictionary<string, Sprite> _sprites = [];

    static Sprite _questKillStandardUnit;
    static Sprite _questKillVBloodUnit;

    static readonly Regex _classNameRegex = new("(?<!^)([A-Z])");
    /// <summary>
    /// 用于从技能组名称中提取技能名称的正则表达式。
    /// </summary>
    public static readonly Regex AbilitySpellRegex = new(@"(?<=AB_).*(?=_Group)");

    static readonly Dictionary<PlayerClass, Color> _classColorHexMap = new()
    {
        { PlayerClass.ShadowBlade, new Color(0.6f, 0.1f, 0.9f) },  // 点燃 紫色
        { PlayerClass.DemonHunter, new Color(1f, 0.8f, 0f) },        // 静电 黄色
        { PlayerClass.BloodKnight, new Color(1f, 0f, 0f) },           // 汲取 红色
        { PlayerClass.ArcaneSorcerer, new Color(0f, 0.5f, 0.5f) },    // 削弱 青色
        { PlayerClass.VampireLord, new Color(0f, 1f, 1f) },           // 冰冷 青色
        { PlayerClass.DeathMage, new Color(0f, 1f, 0f) }              // 谴责 绿色
    };

    /// <summary>
    /// 版本字符串常量 "1.3"。
    /// </summary>
    public const string V1_3 = "1.3";

    static readonly WaitForSeconds _delay = new(1f);
    static readonly WaitForSeconds _shiftDelay = new(0.1f);

    static UICanvasBase _canvasBase;
    static Canvas _bottomBarCanvas;
    static Canvas _targetInfoPanelCanvas;
    /// <summary>
    /// 当前插件版本。
    /// </summary>
    public static string _version = string.Empty;

    static GameObject _experienceBarGameObject;
    static GameObject _experienceInformationPanel;
    static LocalizedText _experienceHeader;
    static LocalizedText _experienceText;
    static LocalizedText _experienceFirstText;
    static LocalizedText _experienceClassText;
    static LocalizedText _experienceSecondText;
    static Image _experienceFill;
    /// <summary>
    /// 当前经验进度。
    /// </summary>
    public static float _experienceProgress = 0f;
    /// <summary>
    /// 当前经验等级。
    /// </summary>
    public static int _experienceLevel = 0;
    /// <summary>
    /// 当前经验声望等级。
    /// </summary>
    public static int _experiencePrestige = 0;
    /// <summary>
    /// 最大经验等级。
    /// </summary>
    public static int _experienceMaxLevel = 90;
    /// <summary>
    /// 当前玩家职业类型。
    /// </summary>
    public static PlayerClass _classType = PlayerClass.None;

    static GameObject _legacyBarGameObject;
    static GameObject _legacyInformationPanel;
    static LocalizedText _firstLegacyStat;
    static LocalizedText _secondLegacyStat;
    static LocalizedText _thirdLegacyStat;
    static LocalizedText _legacyHeader;
    static LocalizedText _legacyText;
    static Image _legacyFill;
    /// <summary>
    /// 当前传承类型。
    /// </summary>
    public static string _legacyType;
    /// <summary>
    /// 当前传承进度。
    /// </summary>
    public static float _legacyProgress = 0f;
    /// <summary>
    /// 当前传承等级。
    /// </summary>
    public static int _legacyLevel = 0;
    /// <summary>
    /// 当前传承声望等级。
    /// </summary>
    public static int _legacyPrestige = 0;
    /// <summary>
    /// 最大传承等级。
    /// </summary>
    public static int _legacyMaxLevel = 100;
    /// <summary>
    /// 传承提供的额外属性列表。
    /// </summary>
    public static List<string> _legacyBonusStats = ["", "", ""];

    static GameObject _expertiseBarGameObject;
    static GameObject _expertiseInformationPanel;
    static LocalizedText _firstExpertiseStat;
    static LocalizedText _secondExpertiseStat;
    static LocalizedText _thirdExpertiseStat;
    static LocalizedText _expertiseHeader;
    static LocalizedText _expertiseText;
    static Image _expertiseFill;
    /// <summary>
    /// 当前专精类型。
    /// </summary>
    public static string _expertiseType;
    /// <summary>
    /// 当前专精进度。
    /// </summary>
    public static float _expertiseProgress = 0f;
    /// <summary>
    /// 当前专精等级。
    /// </summary>
    public static int _expertiseLevel = 0;
    /// <summary>
    /// 当前专精声望等级。
    /// </summary>
    public static int _expertisePrestige = 0;
    /// <summary>
    /// 最大专精等级。
    /// </summary>
    public static int _expertiseMaxLevel = 100;
    /// <summary>
    /// 专精提供的额外属性列表。
    /// </summary>
    public static List<string> _expertiseBonusStats = ["", "", ""];

    static GameObject _familiarBarGameObject;
    static GameObject _familiarInformationPanel;
    static LocalizedText _familiarMaxHealth;
    static LocalizedText _familiarPhysicalPower;
    static LocalizedText _familiarSpellPower;
    static LocalizedText _familiarHeader;
    static LocalizedText _familiarText;
    static Image _familiarFill;
    /// <summary>
    /// 当前伙伴进度。
    /// </summary>
    public static float _familiarProgress = 0f;
    /// <summary>
    /// 当前伙伴等级。
    /// </summary>
    public static int _familiarLevel = 1;
    /// <summary>
    /// 当前伙伴声望等级。
    /// </summary>
    public static int _familiarPrestige = 0;
    /// <summary>
    /// 最大伙伴等级。
    /// </summary>
    public static int _familiarMaxLevel = 90;
    /// <summary>
    /// 当前伙伴名称。
    /// </summary>
    public static string _familiarName = "";
    /// <summary>
    /// 伙伴属性列表。
    /// </summary>
    public static List<string> _familiarStats = ["", "", ""];

    /// <summary>
    /// 指示是否启用装备加成。
    /// </summary>
    public static bool _equipmentBonus = false;
    const float MAX_PROFESSION_LEVEL = 100f;
    const float EQUIPMENT_BONUS = 0.1f;

    static GameObject _enchantingBarGameObject;
    static LocalizedText _enchantingLevelText;
    static Image _enchantingProgressFill;
    static Image _enchantingFill;
    /// <summary>
    /// 当前附魔进度。
    /// </summary>
    public static float _enchantingProgress = 0f;
    /// <summary>
    /// 当前附魔等级。
    /// </summary>
    public static int _enchantingLevel = 0;

    static GameObject _alchemyBarGameObject;
    static LocalizedText _alchemyLevelText;
    static Image _alchemyProgressFill;
    static Image _alchemyFill;
    /// <summary>
    /// 当前炼金进度。
    /// </summary>
    public static float _alchemyProgress = 0f;
    /// <summary>
    /// 当前炼金等级。
    /// </summary>
    public static int _alchemyLevel = 0;

    static GameObject _harvestingGameObject;
    static LocalizedText _harvestingLevelText;
    static Image _harvestingProgressFill;
    static Image _harvestingFill;
    /// <summary>
    /// 当前草药学进度。
    /// </summary>
    public static float _harvestingProgress = 0f;
    /// <summary>
    /// 当前草药学等级。
    /// </summary>
    public static int _harvestingLevel = 0;

    static GameObject _blacksmithingBarGameObject;
    static LocalizedText _blacksmithingLevelText;
    static Image _blacksmithingProgressFill;
    static Image _blacksmithingFill;
    /// <summary>
    /// 当前锻造进度。
    /// </summary>
    public static float _blacksmithingProgress = 0f;
    /// <summary>
    /// 当前锻造等级。
    /// </summary>
    public static int _blacksmithingLevel = 0;

    static GameObject _tailoringBarGameObject;
    static LocalizedText _tailoringLevelText;
    static Image _tailoringProgressFill;
    static Image _tailoringFill;
    /// <summary>
    /// 当前裁缝进度。
    /// </summary>
    public static float _tailoringProgress = 0f;
    /// <summary>
    /// 当前裁缝等级。
    /// </summary>
    public static int _tailoringLevel = 0;

    static GameObject _woodcuttingBarGameObject;
    static LocalizedText _woodcuttingLevelText;
    static Image _woodcuttingProgressFill;
    static Image _woodcuttingFill;
    /// <summary>
    /// 当前伐木进度。
    /// </summary>
    public static float _woodcuttingProgress = 0f;
    /// <summary>
    /// 当前伐木等级。
    /// </summary>
    public static int _woodcuttingLevel = 0;

    static GameObject _miningBarGameObject;
    static LocalizedText _miningLevelText;
    static Image _miningProgressFill;
    static Image _miningFill;
    /// <summary>
    /// 当前采矿进度。
    /// </summary>
    public static float _miningProgress = 0f;
    /// <summary>
    /// 当前采矿等级。
    /// </summary>
    public static int _miningLevel = 0;

    static GameObject _fishingBarGameObject;
    static LocalizedText _fishingLevelText;
    static Image _fishingProgressFill;
    static Image _fishingFill;
    /// <summary>
    /// 当前钓鱼进度。
    /// </summary>
    public static float _fishingProgress = 0f;
    /// <summary>
    /// 当前钓鱼等级。
    /// </summary>
    public static int _fishingLevel = 0;

    static GameObject _dailyQuestObject;
    static LocalizedText _dailyQuestHeader;
    static LocalizedText _dailyQuestSubHeader;
    /// <summary>
    /// 每日任务图标。
    /// </summary>
    public static Image _dailyQuestIcon;
    /// <summary>
    /// 每日任务目标类型。
    /// </summary>
    public static TargetType _dailyTargetType = TargetType.Kill;
    /// <summary>
    /// 每日任务当前进度。
    /// </summary>
    public static int _dailyProgress = 0;
    /// <summary>
    /// 每日任务目标数量。
    /// </summary>
    public static int _dailyGoal = 0;
    /// <summary>
    /// 每日任务目标名称。
    /// </summary>
    public static string _dailyTarget = "";
    /// <summary>
    /// 指示每日任务目标是否为VBlood单位。
    /// </summary>
    public static bool _dailyVBlood = false;

    static GameObject _weeklyQuestObject;
    static LocalizedText _weeklyQuestHeader;
    static LocalizedText _weeklyQuestSubHeader;
    /// <summary>
    /// 每周任务图标。
    /// </summary>
    public static Image _weeklyQuestIcon;
    /// <summary>
    /// 每周任务目标类型。
    /// </summary>
    public static TargetType _weeklyTargetType = TargetType.Kill;
    /// <summary>
    /// 每周任务当前进度。
    /// </summary>
    public static int _weeklyProgress = 0;
    /// <summary>
    /// 每周任务目标数量。
    /// </summary>
    public static int _weeklyGoal = 0;
    /// <summary>
    /// 每周任务目标名称。
    /// </summary>
    public static string _weeklyTarget = "";
    /// <summary>
    /// 指示每周任务目标是否为VBlood单位。
    /// </summary>
    public static bool _weeklyVBlood = false;

    static PrefabGUID _abilityGroupPrefabGUID;

    /// <summary>
    /// 当前技能的工具提示数据。
    /// </summary>
    public static AbilityTooltipData _abilityTooltipData;
    static readonly ComponentType _abilityTooltipDataComponent = ComponentType.ReadOnly(Il2CppType.Of<AbilityTooltipData>());

    /// <summary>
    /// 技能栏的虚拟对象，用于轮换栏位。
    /// </summary>
    public static GameObject _abilityDummyObject;
    /// <summary>
    /// 技能栏条目组件，用于轮换栏位。
    /// </summary>
    public static AbilityBarEntry _abilityBarEntry;
    /// <summary>
    /// 技能栏条目的UI状态，用于轮换栏位。
    /// </summary>
    public static AbilityBarEntry.UIState _uiState;

    /// <summary>
    /// 冷却时间的父级游戏对象。
    /// </summary>
    public static GameObject _cooldownParentObject;
    /// <summary>
    /// 显示冷却时间的文本组件。
    /// </summary>
    public static TextMeshProUGUI _cooldownText;
    /// <summary>
    /// 充能冷却时间的图像游戏对象。
    /// </summary>
    public static GameObject _chargeCooldownImageObject;
    /// <summary>
    /// 显示充能次数的文本游戏对象。
    /// </summary>
    public static GameObject _chargesTextObject;
    /// <summary>
    /// 显示充能次数的文本组件。
    /// </summary>
    public static TextMeshProUGUI _chargesText;
    /// <summary>
    /// 冷却时间填充图像。
    /// </summary>
    public static Image _cooldownFillImage;
    /// <summary>
    /// 充能冷却时间填充图像。
    /// </summary>
    public static Image _chargeCooldownFillImage;

    static GameObject _abilityEmptyIcon;
    static GameObject _abilityIcon;

    static GameObject _keybindObject;

    /// <summary>
    /// 当前轮换法术的索引。
    /// </summary>
    public static int _shiftSpellIndex = -1;
    const float COOLDOWN_FACTOR = 8f;

    /// <summary>
    /// 技能冷却结束时间。
    /// </summary>
    public static double _cooldownEndTime = 0;
    /// <summary>
    /// 技能剩余冷却时间。
    /// </summary>
    public static float _cooldownRemaining = 0f;
    /// <summary>
    /// 技能总冷却时间。
    /// </summary>
    public static float _cooldownTime = 0f;
    /// <summary>
    /// 当前技能充能次数。
    /// </summary>
    public static int _currentCharges = 0;
    /// <summary>
    /// 最大技能充能次数。
    /// </summary>
    public static int _maxCharges = 0;
    /// <summary>
    /// 技能充能结束时间。
    /// </summary>
    public static double _chargeUpEndTime = 0;
    /// <summary>
    /// 技能充能所需时间。
    /// </summary>
    public static float _chargeUpTime = 0f;
    /// <summary>
    /// 技能充能剩余时间。
    /// </summary>
    public static float _chargeUpTimeRemaining = 0f;
    /// <summary>
    /// 技能充能的冷却时间。
    /// </summary>
    public static float _chargeCooldownTime = 0f;

    static int _layer;
    static int _barNumber;
    static int _graphBarNumber;
    static float _horizontalBarHeaderFontSize;
    static float _windowOffset;
    static readonly Color _brightGold = new(1f, 0.8f, 0f, 1f);

    const float BAR_HEIGHT_SPACING = 0.075f;
    const float BAR_WIDTH_SPACING = 0.065f;

    static readonly Dictionary<UIElement, GameObject> _gameObjects = [];
    static readonly Dictionary<GameObject, bool> _objectStates = [];
    static readonly List<GameObject> _professionObjects = [];

    /*
    public static readonly Dictionary<PrefabGUID, Entity> PrefabEntityCache = [];
    public static readonly Dictionary<Entity, Dictionary<UnitStatType, float>> WeaponEntityStatCache = [];
    public static readonly Dictionary<PrefabGUID, Dictionary<UnitStatType, float>> WeaponStatCache = [];
    // static readonly Dictionary<PrefabGUID, Dictionary<UnitStatType, float>> _blacksmithingStatCache = []; // 暂时放弃这个功能 x_x
    public static readonly Dictionary<PrefabGUID, Dictionary<UnitStatType, float>> OriginalWeaponStatsCache = [];

    public static readonly Dictionary<PrefabGUID, Dictionary<UnitStatType, float>> GrimoireStatCache = [];
    public static readonly Dictionary<PrefabGUID, Dictionary<UnitStatType, float>> OriginalGrimoireStatsCache = [];

    public static readonly Dictionary<PrefabGUID, Dictionary<UnitStatType, float>> ArmorStatCache = [];
    public static readonly Dictionary<PrefabGUID, Dictionary<UnitStatType, float>> OriginalArmorStatsCache = [];
    */

    static readonly Dictionary<int, Action> _actionToggles = new()
    {
        {0, ExperienceToggle},
        {1, LegacyToggle},
        {2, ExpertiseToggle},
        {3, FamiliarToggle},
        {4, ProfessionToggle},
        {5, DailyQuestToggle},
        {6, WeeklyQuestToggle},
        {7, ShiftSlotToggle}
    };

    static readonly Dictionary<UIElement, string> _abilitySlotNamePaths = new()
    {
        { UIElement.Experience, "HUDCanvas(Clone)/BottomBarCanvas/BottomBar(Clone)/Content/Background/AbilityBar/AbilityBarEntry_Primary/" },
        { UIElement.Legacy, "HUDCanvas(Clone)/BottomBarCanvas/BottomBar(Clone)/Content/Background/AbilityBar/AbilityBarEntry_WeaponSkill1/" },
        { UIElement.Expertise, "HUDCanvas(Clone)/BottomBarCanvas/BottomBar(Clone)/Content/Background/AbilityBar/AbilityBarEntry_WeaponSkill2/" },
        { UIElement.Familiars, "HUDCanvas(Clone)/BottomBarCanvas/BottomBar(Clone)/Content/Background/AbilityBar/AbilityBarEntry_Travel/" },
        { UIElement.Professions, "HUDCanvas(Clone)/BottomBarCanvas/BottomBar(Clone)/Content/Background/AbilityBar/AbilityBarEntry_Spell1/" },
        { UIElement.Weekly, "HUDCanvas(Clone)/BottomBarCanvas/BottomBar(Clone)/Content/Background/AbilityBar/AbilityBarEntry_Spell2/" },
        { UIElement.Daily, "HUDCanvas(Clone)/BottomBarCanvas/BottomBar(Clone)/Content/Background/AbilityBar/AbilityBarEntry_Ultimate/" },
    };

    static readonly Dictionary<UIElement, bool> _uiElementsConfigured = new()
    {
        { UIElement.Experience, _experienceBar },
        { UIElement.Legacy, _legacyBar },
        { UIElement.Expertise, _expertiseBar },
        { UIElement.Familiars, _familiarBar },
        { UIElement.Professions, _professionBars },
        { UIElement.Daily, _questTracker },
        { UIElement.Weekly, _questTracker },
        { UIElement.ShiftSlot, _shiftSlot }
    };

    static readonly Dictionary<UIElement, int> _uiElementIndices = new()
    {
        { UIElement.Experience, 0 },
        { UIElement.Legacy, 1 },
        { UIElement.Expertise, 2 },
        { UIElement.Familiars, 3 },
        { UIElement.Professions, 4 },
        { UIElement.Daily, 5 },
        { UIElement.Weekly, 6 },
        { UIElement.ShiftSlot, 7 }
    };

    static readonly List<EquipmentType> _equipmentTypes =
    [
        EquipmentType.Chest,
        EquipmentType.Gloves,
        EquipmentType.Legs,
        EquipmentType.Footgear
    ];

    const int EXPERIENCE = 0;
    const int LEGACY = 1;
    const int EXPERTISE = 2;
    const int FAMILIARS = 3;
    const int PROFESSION = 4;
    const int DAILY = 5;
    const int WEEKLY = 6;
    const int SHIFT_SLOT = 7;

    const string SHIFT_SPRITE = "KeyboardGlyphs_Smaller_36";
    const string SHIFT_TEXTURE = "KeyboardGlyphs_Smaller";

    /// <summary>
    /// 指示UI是否已准备就绪。
    /// </summary>
    public static bool _ready = false;
    /// <summary>
    /// 指示UI元素是否处于活动状态。
    /// </summary>
    public static bool _active = false;
    /// <summary>
    /// 指示轮换栏位是否处于活动状态。
    /// </summary>
    public static bool _shiftActive = false;
    /// <summary>
    /// 用于停止所有协程的开关。
    /// </summary>
    public static bool _killSwitch = false;

    /// <summary>
    /// UI更新循环的协程。
    /// </summary>
    public static Coroutine _canvasRoutine;
    /// <summary>
    /// 轮换栏位更新循环的协程。
    /// </summary>
    public static Coroutine _shiftRoutine;

    static readonly PrefabGUID _statsBuff = PrefabGUIDs.SetBonus_AllLeech_T09;
    static readonly bool _statsBuffActive = _legacyBar || _expertiseBar; // 在循环中可以检查是否有职业来应用这些属性

    static readonly Dictionary<int, ModifyUnitStatBuff_DOTS> _weaponStats = [];
    static readonly Dictionary<int, ModifyUnitStatBuff_DOTS> _bloodStats = [];

    /// <summary>
    /// 初始化 <see cref="CanvasService"/> 类的新实例。
    /// </summary>
    /// <param name="canvas">UI画布基础组件。</param>
    public CanvasService(UICanvasBase canvas)
    {
        _canvasBase = canvas;

        _bottomBarCanvas = canvas.BottomBarParent.gameObject.GetComponent<Canvas>();
        _targetInfoPanelCanvas = canvas.TargetInfoPanelParent.gameObject.GetComponent<Canvas>();

        _layer = _bottomBarCanvas.gameObject.layer;
        _barNumber = 0;
        _graphBarNumber = 0;
        _windowOffset = 0f;

        FindSprites();
        InitializeBloodButton();


        try
        {
            InitializeUI();
            InitializeAbilitySlotButtons();
        }
        catch (Exception ex)
        {
            Core.Log.LogError($"初始化UI元素失败：{ex}");
        }
    }

    /// <summary>
    /// 初始化所有配置的UI元素。
    /// </summary>
    static void InitializeUI()
    {
        if (_experienceBar) ConfigureHorizontalProgressBar(ref _experienceBarGameObject, ref _experienceInformationPanel,
            ref _experienceFill, ref _experienceText, ref _experienceHeader, UIElement.Experience, Color.green,
            ref _experienceFirstText, ref _experienceClassText, ref _experienceSecondText);

        if (_legacyBar) ConfigureHorizontalProgressBar(ref _legacyBarGameObject, ref _legacyInformationPanel,
            ref _legacyFill, ref _legacyText, ref _legacyHeader, UIElement.Legacy, Color.red,
            ref _firstLegacyStat, ref _secondLegacyStat, ref _thirdLegacyStat);

        if (_expertiseBar) ConfigureHorizontalProgressBar(ref _expertiseBarGameObject, ref _expertiseInformationPanel,
            ref _expertiseFill, ref _expertiseText, ref _expertiseHeader, UIElement.Expertise, Color.grey,
            ref _firstExpertiseStat, ref _secondExpertiseStat, ref _thirdExpertiseStat);

        if (_familiarBar) ConfigureHorizontalProgressBar(ref _familiarBarGameObject, ref _familiarInformationPanel,
            ref _familiarFill, ref _familiarText, ref _familiarHeader, UIElement.Familiars, Color.yellow,
            ref _familiarMaxHealth, ref _familiarPhysicalPower, ref _familiarSpellPower);

        if (_questTracker)
        {
            ConfigureQuestWindow(ref _dailyQuestObject, UIElement.Daily, Color.green, ref _dailyQuestHeader, ref _dailyQuestSubHeader, ref _dailyQuestIcon);
            ConfigureQuestWindow(ref _weeklyQuestObject, UIElement.Weekly, Color.magenta, ref _weeklyQuestHeader, ref _weeklyQuestSubHeader, ref _weeklyQuestIcon);
        }

        if (_professionBars)
        {
            ConfigureVerticalProgressBar(ref _alchemyBarGameObject, ref _alchemyProgressFill, ref _alchemyFill, ref _alchemyLevelText, Profession.Alchemy);
            ConfigureVerticalProgressBar(ref _blacksmithingBarGameObject, ref _blacksmithingProgressFill, ref _blacksmithingFill, ref _blacksmithingLevelText, Profession.Blacksmithing);
            ConfigureVerticalProgressBar(ref _enchantingBarGameObject, ref _enchantingProgressFill, ref _enchantingFill, ref _enchantingLevelText, Profession.Enchanting);
            ConfigureVerticalProgressBar(ref _tailoringBarGameObject, ref _tailoringProgressFill, ref _tailoringFill, ref _tailoringLevelText, Profession.Tailoring);
            ConfigureVerticalProgressBar(ref _fishingBarGameObject, ref _fishingProgressFill, ref _fishingFill, ref _fishingLevelText, Profession.Fishing);
            ConfigureVerticalProgressBar(ref _harvestingGameObject, ref _harvestingProgressFill, ref _harvestingFill, ref _harvestingLevelText, Profession.Harvesting);
            ConfigureVerticalProgressBar(ref _miningBarGameObject, ref _miningProgressFill, ref _miningFill, ref _miningLevelText, Profession.Mining);
            ConfigureVerticalProgressBar(ref _woodcuttingBarGameObject, ref _woodcuttingProgressFill, ref _woodcuttingFill, ref _woodcuttingLevelText, Profession.Woodcutting);
        }

        if (_shiftSlot)
        {
            ConfigureShiftSlot(ref _abilityDummyObject, ref _abilityBarEntry, ref _uiState, ref _cooldownParentObject, ref _cooldownText,
                ref _chargesTextObject, ref _cooldownFillImage, ref _chargesText, ref _chargeCooldownFillImage, ref _chargeCooldownImageObject,
                ref _abilityEmptyIcon, ref _abilityIcon, ref _keybindObject);
        }

        _ready = true;
    }

    /// <summary>
    /// 初始化技能栏按钮，用于切换对应UI元素的显示状态。
    /// </summary>
    static void InitializeAbilitySlotButtons()
    {
        foreach (var keyValuePair in _uiElementsConfigured)
        {
            if (keyValuePair.Value && _abilitySlotNamePaths.ContainsKey(keyValuePair.Key))
            {
                int index = _uiElementIndices[keyValuePair.Key];
                GameObject abilitySlotObject = GameObject.Find(_abilitySlotNamePaths[keyValuePair.Key]);

                if (abilitySlotObject != null && _gameObjects.TryGetValue(keyValuePair.Key, out GameObject gameObject))
                {
                    SimpleStunButton stunButton = abilitySlotObject.AddComponent<SimpleStunButton>();
                    GameObject capturedObject = gameObject;

                    if (!keyValuePair.Key.Equals(UIElement.Professions)) stunButton.onClick.AddListener((UnityAction)(() => ToggleGameObject(capturedObject)));
                    else if (_actionToggles.TryGetValue(index, out var toggleAction))
                    {
                        stunButton.onClick.AddListener(new Action(toggleAction));
                    }
                }
            }
        }
    }

    /// <summary>
    /// 切换指定游戏对象的活动状态。
    /// </summary>
    /// <param name="gameObject">要切换状态的游戏对象。</param>
    static void ToggleGameObject(GameObject gameObject)
    {
        bool active = !gameObject.activeSelf;
        gameObject.SetActive(active);
        _objectStates[gameObject] = active;
    }

    /// <summary>
    /// 切换经验条的显示状态。
    /// </summary>
    static void ExperienceToggle()
    {
        bool active = !_experienceBarGameObject.activeSelf;

        _experienceBarGameObject.SetActive(active);
        _objectStates[_experienceBarGameObject] = active;
    }

    /// <summary>
    /// 切换传承条的显示状态。
    /// </summary>
    static void LegacyToggle()
    {
        bool active = !_legacyBarGameObject.activeSelf;

        _legacyBarGameObject.SetActive(active);
        _objectStates[_legacyBarGameObject] = active;
    }

    /// <summary>
    /// 切换专精条的显示状态。
    /// </summary>
    static void ExpertiseToggle()
    {
        bool active = !_expertiseBarGameObject.activeSelf;

        _expertiseBarGameObject.SetActive(active);
        _objectStates[_expertiseBarGameObject] = active;
    }

    /// <summary>
    /// 切换伙伴条的显示状态。
    /// </summary>
    static void FamiliarToggle()
    {
        bool active = !_familiarBarGameObject.activeSelf;

        _familiarBarGameObject.SetActive(active);
        _objectStates[_familiarBarGameObject] = active;
    }

    /// <summary>
    /// 切换所有专业条的显示状态。
    /// </summary>
    static void ProfessionToggle()
    {
        foreach (GameObject professionObject in _objectStates.Keys)
        {
            if (_professionObjects.Contains(professionObject))
            {
                bool active = !professionObject.activeSelf;

                professionObject.SetActive(active);
                _objectStates[professionObject] = active;
            }
            else
            {
                Core.Log.LogWarning($"专业对象未找到！");
            }
        }
    }

    /// <summary>
    /// 切换每日任务窗口的显示状态。
    /// </summary>
    static void DailyQuestToggle()
    {
        bool active = !_dailyQuestObject.activeSelf;

        _dailyQuestObject.SetActive(active);
        _objectStates[_dailyQuestObject] = active;
    }

    /// <summary>
    /// 切换每周任务窗口的显示状态。
    /// </summary>
    static void WeeklyQuestToggle()
    {
        bool active = !_weeklyQuestObject.activeSelf;

        _weeklyQuestObject.SetActive(active);
        _objectStates[_weeklyQuestObject] = active;
    }

    /// <summary>
    /// 切换轮换栏位的显示状态。
    /// </summary>
    static void ShiftSlotToggle()
    {
        bool active = !_abilityDummyObject.activeSelf;

        _abilityDummyObject.SetActive(active);
        _objectStates[_abilityDummyObject] = active;
    }

    /// <summary>
    /// 初始化血球按钮，用于切换所有UI元素的显示状态。
    /// </summary>
    static void InitializeBloodButton()
    {
        GameObject bloodObject = GameObject.Find("HUDCanvas(Clone)/BottomBarCanvas/BottomBar(Clone)/Content/BloodOrbParent/BloodOrb/BlackBackground/Blood");

        if (bloodObject != null)
        {
            SimpleStunButton stunButton = bloodObject.AddComponent<SimpleStunButton>();
            stunButton.onClick.AddListener(new Action(ToggleAllObjects));
        }
    }

    /// <summary>
    /// 切换所有已注册UI元素的活动状态。
    /// </summary>
    static void ToggleAllObjects()
    {
        _active = !_active;

        foreach (GameObject gameObject in _objectStates.Keys)
        {
            gameObject.active = _active;
            _objectStates[gameObject] = _active;
        }
    }

    /// <summary>
    /// UI更新循环，定期更新所有活动的UI元素。
    /// </summary>
    /// <returns>协程的 <see cref="IEnumerator"/>。</returns>
    public static IEnumerator CanvasUpdateLoop()
    {
        while (true)
        {
            if (_killSwitch)
            {
                yield break;
            }
            else if (!_ready || !_active)
            {
                yield return _delay;

                continue;
            }

            if (_experienceBar)
            {
                try
                {
                    UpdateBar(_experienceProgress, _experienceLevel, _experienceMaxLevel, _experiencePrestige, _experienceText, _experienceHeader, _experienceFill, UIElement.Experience);
                    UpdateClass(_classType, _experienceClassText);
                }
                catch (Exception e)
                {
                    Core.Log.LogError($"更新经验条时出错：{e}");
                }
            }

            if (_legacyBar)
            {
                try
                {
                    UpdateBar(_legacyProgress, _legacyLevel, _legacyMaxLevel, _legacyPrestige, _legacyText, _legacyHeader, _legacyFill, UIElement.Legacy, _legacyType);
                    UpdateBloodStats(_legacyBonusStats, [_firstLegacyStat, _secondLegacyStat, _thirdLegacyStat], GetBloodStatInfo);
                }
                catch (Exception e)
                {
                    Core.Log.LogError($"更新传承条时出错：{e}");
                }
            }

            if (_expertiseBar)
            {
                try
                {
                    UpdateBar(_expertiseProgress, _expertiseLevel, _expertiseMaxLevel, _expertisePrestige, _expertiseText, _expertiseHeader, _expertiseFill, UIElement.Expertise, _expertiseType);
                    UpdateWeaponStats(_expertiseBonusStats, [_firstExpertiseStat, _secondExpertiseStat, _thirdExpertiseStat], GetWeaponStatInfo);
                    GetAndUpdateWeaponStatBuffer(LocalCharacter);
                }
                catch (Exception e)
                {
                    Core.Log.LogError($"更新专精条时出错：{e}");
                }
            }

            if (_statsBuffActive)
            {
                try
                {
                    if (LocalCharacter.TryGetBuff(_statsBuff, out Entity buffEntity))
                    {
                        UpdateBuffStatBuffer(buffEntity);
                    }
                }
                catch (Exception e)
                {
                    Core.Log.LogError($"更新属性增益时出错：{e}");
                }
            }

            if (_familiarBar)
            {
                try
                {
                    UpdateBar(_familiarProgress, _familiarLevel, _familiarMaxLevel, _familiarPrestige, _familiarText, _familiarHeader, _familiarFill, UIElement.Familiars, _familiarName);
                    UpdateFamiliarStats(_familiarStats, [_familiarMaxHealth, _familiarPhysicalPower, _familiarSpellPower]);
                }
                catch (Exception e)
                {
                    Core.Log.LogError($"更新伙伴条时出错：{e}");
                }
            }

            if (_questTracker)
            {
                try
                {
                    UpdateQuests(_dailyQuestObject, _dailyQuestSubHeader, _dailyQuestIcon, _dailyTargetType, _dailyTarget, _dailyProgress, _dailyGoal, _dailyVBlood);
                    UpdateQuests(_weeklyQuestObject, _weeklyQuestSubHeader, _weeklyQuestIcon, _weeklyTargetType, _weeklyTarget, _weeklyProgress, _weeklyGoal, _weeklyVBlood);
                }
                catch (Exception e)
                {
                    Core.Log.LogError($"更新任务追踪器时出错：{e}");
                }
            }

            if (_professionBars)
            {
                try
                {
                    UpdateProfessions(_alchemyProgress, _alchemyLevel, _alchemyLevelText, _alchemyProgressFill, _alchemyFill, Profession.Alchemy);
                    UpdateProfessions(_blacksmithingProgress, _blacksmithingLevel, _blacksmithingLevelText, _blacksmithingProgressFill, _blacksmithingFill, Profession.Blacksmithing);
                    UpdateProfessions(_enchantingProgress, _enchantingLevel, _enchantingLevelText, _enchantingProgressFill, _enchantingFill, Profession.Enchanting);
                    UpdateProfessions(_tailoringProgress, _tailoringLevel, _tailoringLevelText, _tailoringProgressFill, _tailoringFill, Profession.Tailoring);
                }
                catch (Exception e)
                {
                    Core.Log.LogError($"更新专业(1)时出错：{e}");
                }

                try
                {
                    UpdateProfessions(_fishingProgress, _fishingLevel, _fishingLevelText, _fishingProgressFill, _fishingFill, Profession.Fishing);
                    UpdateProfessions(_harvestingProgress, _harvestingLevel, _harvestingLevelText, _harvestingProgressFill, _harvestingFill, Profession.Harvesting);
                    UpdateProfessions(_miningProgress, _miningLevel, _miningLevelText, _miningProgressFill, _miningFill, Profession.Mining);
                    UpdateProfessions(_woodcuttingProgress, _woodcuttingLevel, _woodcuttingLevelText, _woodcuttingProgressFill, _woodcuttingFill, Profession.Woodcutting);
                }
                catch (Exception e)
                {
                    Core.Log.LogError($"更新专业(2)时出错：{e}");
                }
            }

            if (_killSwitch) yield break;

            try
            {
                if (!_shiftActive && LocalCharacter.TryGetComponent(out AbilityBar_Shared abilityBar_Shared))
                {
                    Entity abilityGroupEntity = abilityBar_Shared.CastGroup.GetEntityOnServer();

                    if (abilityGroupEntity.TryGetComponent(out AbilityGroupState abilityGroupState) && abilityGroupState.SlotIndex == 3) // 如果在3号槽位找到技能，则激活轮换循环
                    {
                        if (_shiftRoutine == null)
                        {
                            _shiftRoutine = ShiftUpdateLoop().Start();
                            _shiftActive = true;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Core.Log.LogError($"更新技能栏时出错：{e}");
            }

            yield return _delay;
        }
    }

    /// <summary>
    /// 获取并更新玩家角色武器的属性增益缓冲区。
    /// </summary>
    /// <param name="playerCharacter">玩家角色实体。</param>
    static void GetAndUpdateWeaponStatBuffer(Entity playerCharacter)
    {
        if (!playerCharacter.TryGetComponent(out Equipment equipment)) return;

        Entity weaponEntity = equipment.GetEquipmentEntity(EquipmentType.Weapon).GetEntityOnServer();
        if (!weaponEntity.Exists()) return;

        Entity prefabEntity = weaponEntity.GetPrefabEntity();
        UpdateWeaponStatBuffer(prefabEntity);
    }

    /// <summary>
    /// 更新指定武器实体的属性增益缓冲区。
    /// </summary>
    /// <param name="weaponEntity">武器实体。</param>
    static void UpdateWeaponStatBuffer(Entity weaponEntity)
    {
        if (!weaponEntity.TryGetBuffer<ModifyUnitStatBuff_DOTS>(out var buffer)) return;

        List<int> existingIds = [];

        for (int i = buffer.Length - 1; i >= 0; i--)
        {
            int id = buffer[i].Id.Id;

            if (id != 0 && !_weaponStats.ContainsKey(id))
            {
                buffer.RemoveAt(i);
            }
            else
            {
                existingIds.Add(id);
            }
        }

        foreach (var keyValuePair in _weaponStats)
        {
            if (!existingIds.Contains(keyValuePair.Key))
            {
                buffer.Add(keyValuePair.Value);
            }
        }
    }

    /// <summary>
    /// 更新指定增益实体的属性增益缓冲区。
    /// </summary>
    /// <param name="buffEntity">增益实体。</param>
    static void UpdateBuffStatBuffer(Entity buffEntity)
    {
        if (!buffEntity.TryGetBuffer<ModifyUnitStatBuff_DOTS>(out var buffer)) return;

        List<int> existingIds = [];

        for (int i = buffer.Length - 1; i >= 0; i--)
        {
            int id = buffer[i].Id.Id;

            if (!_weaponStats.ContainsKey(id) && !_bloodStats.ContainsKey(id))
            {
                buffer.RemoveAt(i);
            }
            else
            {
                existingIds.Add(id);
            }
        }

        foreach (var keyValuePair in _weaponStats)
        {
            if (!existingIds.Contains(keyValuePair.Key))
            {
                buffer.Add(keyValuePair.Value);
            }
        }

        foreach (var keyValuePair in _bloodStats)
        {
            if (!existingIds.Contains(keyValuePair.Key))
            {
                buffer.Add(keyValuePair.Value);
            }
        }

        _weaponStats.Clear();
        _bloodStats.Clear();
    }

    /// <summary>
    /// 更新轮换栏位的技能数据。
    /// </summary>
    /// <param name="abilityTooltipData">技能工具提示数据。</param>
    /// <param name="abilityGroupEntity">技能组实体。</param>
    /// <param name="abilityCastEntity">技能施放实体。</param>
    /// <param name="abilityGroupPrefabGUID">技能组预制件GUID。</param>
    static void UpdateAbilityData(AbilityTooltipData abilityTooltipData, Entity abilityGroupEntity, Entity abilityCastEntity, PrefabGUID abilityGroupPrefabGUID)
    {
        if (!_abilityDummyObject.active)
        {
            _abilityDummyObject.SetActive(true);
            if (_uiState.CachedInputVersion != 3) _uiState.CachedInputVersion = 3;
        }

        if (!_keybindObject.active) _keybindObject.SetActive(true);

        _cooldownFillImage.fillAmount = 0f;
        _chargeCooldownFillImage.fillAmount = 0f;

        _abilityGroupPrefabGUID = abilityGroupPrefabGUID;

        _abilityBarEntry.AbilityEntity = abilityGroupEntity;
        _abilityBarEntry.AbilityId = abilityGroupPrefabGUID;
        _abilityBarEntry.AbilityIconImage.sprite = abilityTooltipData.Icon;

        _abilityBarEntry._CurrentUIState.AbilityIconImageActive = true;
        _abilityBarEntry._CurrentUIState.AbilityIconImageSprite = abilityTooltipData.Icon;

        if (abilityGroupEntity.TryGetComponent(out AbilityChargesData abilityChargesData))
        {
            _maxCharges = abilityChargesData.MaxCharges;
        }
        else
        {
            _maxCharges = 0;
            _currentCharges = 0;
            _chargesText.SetText("");
        }

        if (abilityCastEntity.TryGetComponent(out AbilityCooldownData abilityCooldownData))
        {
            _cooldownTime = _shiftSpellIndex.Equals(-1) ? abilityCooldownData.Cooldown._Value : _shiftSpellIndex * COOLDOWN_FACTOR + COOLDOWN_FACTOR;
            _cooldownEndTime = Core.ServerTime.TimeOnServer + _cooldownTime;

        }
    }

    /// <summary>
    /// 更新轮换栏位的技能状态（冷却、充能等）。
    /// </summary>
    /// <param name="abilityGroupEntity">技能组实体。</param>
    /// <param name="abilityCastEntity">技能施放实体。</param>
    static void UpdateAbilityState(Entity abilityGroupEntity, Entity abilityCastEntity)
    {
        PrefabGUID prefabGuid = abilityGroupEntity.GetPrefabGUID();
        if (prefabGuid.HasValue() && !prefabGuid.Equals(_abilityGroupPrefabGUID)) return;

        if (abilityCastEntity.TryGetComponent(out AbilityCooldownState abilityCooldownState))
        {
            _cooldownEndTime = _shiftSpellIndex.Equals(-1) ? abilityCooldownState.CooldownEndTime : _cooldownEndTime;
        }

        _chargeUpTimeRemaining = (float)(_chargeUpEndTime - Core.ServerTime.TimeOnServer);
        _cooldownRemaining = (float)(_cooldownEndTime - Core.ServerTime.TimeOnServer);

        if (abilityGroupEntity.TryGetComponent(out AbilityChargesState abilityChargesState))
        {
            _currentCharges = abilityChargesState.CurrentCharges;
            _chargeUpTime = abilityChargesState.ChargeTime;
            _chargeUpEndTime = Core.ServerTime.TimeOnServer + _chargeUpTime;

            if (_currentCharges == 0)
            {
                _abilityBarEntry._CurrentUIState.ChargesTextActive = false;
                _chargeCooldownFillImage.fillAmount = 0f;
                _chargeCooldownImageObject.SetActive(false);

                _chargesText.SetText("");
                _cooldownText.SetText($"{(int)_chargeUpTime}");

                _cooldownFillImage.fillAmount = _chargeUpTime / _cooldownTime;
            }
            else
            {
                _abilityBarEntry._CurrentUIState.ChargesTextActive = true;
                _cooldownFillImage.fillAmount = 0f;

                _chargesTextObject.SetActive(true);
                _chargeCooldownImageObject.SetActive(true);

                _cooldownText.SetText("");
                _chargesText.SetText($"{_currentCharges}");

                _chargeCooldownFillImage.fillAmount = 1 - (_cooldownRemaining / _cooldownTime);

                if (_currentCharges == _maxCharges) _chargeCooldownFillImage.fillAmount = 0f;
            }
        }
        else if (_maxCharges > 0)
        {
            if (_currentCharges == 0)
            {
                _abilityBarEntry._CurrentUIState.ChargesTextActive = true;
                _chargeCooldownFillImage.fillAmount = 0f;
                _chargeCooldownImageObject.SetActive(false);

                if (_chargeUpTimeRemaining < 0f)
                {
                    _cooldownText.SetText("");
                    _chargesText.SetText("1");
                }
                else
                {
                    _chargesText.SetText("");
                    _cooldownText.SetText($"{(int)_chargeUpTimeRemaining}");
                }

                _cooldownFillImage.fillAmount = _chargeUpTimeRemaining / _cooldownTime;

                if (_chargeUpTimeRemaining < 0f)
                {
                    ++_currentCharges;
                    _cooldownEndTime = Core.ServerTime.TimeOnServer + _cooldownTime;
                }
            }
            else if (_currentCharges < _maxCharges && _currentCharges > 0)
            {
                _cooldownText.SetText("");
                _abilityBarEntry._CurrentUIState.ChargesTextActive = true;
                _cooldownFillImage.fillAmount = 0f;

                _chargesTextObject.SetActive(true);
                _chargeCooldownImageObject.SetActive(true);

                _chargesText.SetText($"{_currentCharges}");

                _chargeCooldownFillImage.fillAmount = 1f - (_cooldownRemaining / _cooldownTime);

                if (_cooldownRemaining < 0f)
                {
                    ++_currentCharges;
                    _cooldownEndTime = Core.ServerTime.TimeOnServer + _cooldownTime;
                }
            }
            else if (_currentCharges == _maxCharges)
            {
                _chargeCooldownImageObject.SetActive(false);

                _cooldownText.SetText("");
                _abilityBarEntry._CurrentUIState.ChargesTextActive = true;

                _cooldownFillImage.fillAmount = 0f;
                _chargeCooldownFillImage.fillAmount = 0f;

                _chargesTextObject.SetActive(true);
                _chargesText.SetText($"{_currentCharges}");
            }
        }
        else
        {
            _currentCharges = 0;
            _abilityBarEntry._CurrentUIState.ChargesTextActive = false;

            _chargeCooldownImageObject.SetActive(false);
            _chargeCooldownFillImage.fillAmount = 0f;

            if (_cooldownRemaining < 0f)
            {
                _cooldownText.SetText($"");
            }
            else
            {
                _cooldownText.SetText($"{(int)_cooldownRemaining}");
            }

            _cooldownFillImage.fillAmount = _cooldownRemaining / _cooldownTime;
        }
    }

    /// <summary>
    /// 轮换栏位UI更新循环。
    /// </summary>
    /// <returns>协程的 <see cref="IEnumerator"/>。</returns>
    public static IEnumerator ShiftUpdateLoop()
    {
        while (true)
        {
            if (_killSwitch)
            {
                yield break;
            }
            else if (!_ready)
            {
                yield return _delay;
                continue;
            }
            else if (!_shiftActive)
            {
                yield return _delay;
                continue;
            }

            if (LocalCharacter.TryGetComponent(out AbilityBar_Shared abilityBar_Shared))
            {
                Entity abilityGroupEntity = abilityBar_Shared.CastGroup.GetEntityOnServer();
                Entity abilityCastEntity = abilityBar_Shared.CastAbility.GetEntityOnServer();

                if (abilityGroupEntity.TryGetComponent(out AbilityGroupState abilityGroupState) && abilityGroupState.SlotIndex == 3)
                {
                    PrefabGUID currentPrefabGUID = abilityGroupEntity.GetPrefabGUID();

                    if (TryUpdateTooltipData(abilityGroupEntity, currentPrefabGUID))
                    {
                        UpdateAbilityData(_abilityTooltipData, abilityGroupEntity, abilityCastEntity, currentPrefabGUID);
                    }
                    else if (_abilityTooltipData != null)
                    {
                        UpdateAbilityData(_abilityTooltipData, abilityGroupEntity, abilityCastEntity, currentPrefabGUID);
                    }
                }

                if (_abilityTooltipData != null)
                {
                    UpdateAbilityState(abilityGroupEntity, abilityCastEntity);
                }
            }

            yield return _shiftDelay;
        }
    }

    /// <summary>
    /// 尝试更新技能工具提示数据。
    /// </summary>
    /// <param name="abilityGroupEntity">技能组实体。</param>
    /// <param name="abilityGroupPrefabGUID">技能组预制件GUID。</param>
    /// <returns>如果成功更新数据则返回 <c>true</c>，否则返回 <c>false</c>。</returns>
    static bool TryUpdateTooltipData(Entity abilityGroupEntity, PrefabGUID abilityGroupPrefabGUID)
    {
        if (_abilityTooltipData == null || _abilityGroupPrefabGUID != abilityGroupPrefabGUID)
        {
            if (abilityGroupEntity.TryGetComponentObject(EntityManager, out _abilityTooltipData))
            {
                _abilityTooltipData ??= EntityManager.GetComponentObject<AbilityTooltipData>(abilityGroupEntity, _abilityTooltipDataComponent);
            }
        }

        return _abilityTooltipData != null;
    }

    /// <summary>
    /// 更新专业进度条。
    /// </summary>
    /// <param name="progress">当前进度 (0-1)。</param>
    /// <param name="level">当前等级。</param>
    /// <param name="levelText">显示等级的文本组件。</param>
    /// <param name="progressFill">显示当前等级进度的填充图像。</param>
    /// <param name="fill">显示总等级进度的填充图像。</param>
    /// <param name="profession">专业类型。</param>
    static void UpdateProfessions(float progress, int level, LocalizedText levelText,
        Image progressFill, Image fill, Profession profession)
    {
        if (_killSwitch) return;

        if (level == MAX_PROFESSION_LEVEL)
        {
            progressFill.fillAmount = 1f;
            fill.fillAmount = 1f;
        }
        else
        {
            progressFill.fillAmount = progress;
            fill.fillAmount = level / MAX_PROFESSION_LEVEL;
        }
    }

    /// <summary>
    /// 更新通用的水平进度条（如经验条、传承条等）。
    /// </summary>
    /// <param name="progress">当前进度 (0-1)。</param>
    /// <param name="level">当前等级。</param>
    /// <param name="maxLevel">最大等级。</param>
    /// <param name="prestiges">声望等级。</param>
    /// <param name="levelText">显示等级的文本组件。</param>
    /// <param name="barHeader">显示标题的文本组件。</param>
    /// <param name="fill">填充图像。</param>
    /// <param name="element">UI元素类型。</param>
    /// <param name="type">进度条的特定类型或名称（可选）。</param>
    static void UpdateBar(float progress, int level, int maxLevel,
        int prestiges, LocalizedText levelText, LocalizedText barHeader,
        Image fill, UIElement element, string type = "")
    {
        if (_killSwitch) return;

        string levelString = level.ToString();

        if (type == "Frailed" || type == "Familiar")
        {
            levelString = "不适用";
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

        if (element.Equals(UIElement.Familiars))
        {
            type = TrimToFirstWord(type);
        }

        if (barHeader.Text.fontSize != _horizontalBarHeaderFontSize)
        {
            barHeader.Text.fontSize = _horizontalBarHeaderFontSize;
        }

        if (_showPrestige && prestiges != 0)
        {
            string header = "";

            if (element.Equals(UIElement.Experience))
            {
                header = $"{element.GetDescription()} {IntegerToRoman(prestiges)}";
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

    /// <summary>
    /// 更新经验条上的职业文本。
    /// </summary>
    /// <param name="classType">玩家职业类型。</param>
    /// <param name="classText">显示职业名称的文本组件。</param>
    static void UpdateClass(PlayerClass classType, LocalizedText classText)
    {
        if (_killSwitch) return;

        if (classType != PlayerClass.None)
        {
            if (!classText.enabled) classText.enabled = true;
            if (!classText.gameObject.active) classText.gameObject.SetActive(true);

            string formattedClassName = FormatClassName(classType);
            classText.ForceSet(formattedClassName);

            if (_classColorHexMap.TryGetValue(classType, out Color classColor))
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

    /// <summary>
    /// 格式化职业名称以供显示。
    /// </summary>
    /// <param name="classType">玩家职业类型。</param>
    /// <returns>格式化后的职业名称字符串。</returns>
    static string FormatClassName(PlayerClass classType)
    {
        return _classNameRegex.Replace(classType.GetDescription(), " $1");
    }

    /// <summary>
    /// 更新武器属性文本显示。
    /// </summary>
    /// <param name="bonusStats">额外属性列表。</param>
    /// <param name="statTexts">显示属性的文本组件列表。</param>
    /// <param name="getStatInfo">获取属性信息的函数。</param>
    static void UpdateWeaponStats(List<string> bonusStats, List<LocalizedText> statTexts, Func<string, string> getStatInfo)
    {
        if (_killSwitch) return;

        for (int i = 0; i < 3; i++)
        {
            if (bonusStats[i] != "None")
            {
                if (!statTexts[i].enabled) statTexts[i].enabled = true;
                if (!statTexts[i].gameObject.active) statTexts[i].gameObject.SetActive(true);

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

    /// <summary>
    /// 更新血统属性文本显示。
    /// </summary>
    /// <param name="bonusStats">额外属性列表。</param>
    /// <param name="statTexts">显示属性的文本组件列表。</param>
    /// <param name="getStatInfo">获取属性信息的函数。</param>
    static void UpdateBloodStats(List<string> bonusStats, List<LocalizedText> statTexts, Func<string, string> getStatInfo)
    {
        if (_killSwitch) return;

        for (int i = 0; i < 3; i++)
        {
            if (bonusStats[i] != "None")
            {
                if (!statTexts[i].enabled) statTexts[i].enabled = true;
                if (!statTexts[i].gameObject.active) statTexts[i].gameObject.SetActive(true);

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

    /// <summary>
    /// 获取并格式化武器属性信息。
    /// </summary>
    /// <param name="statType">武器属性类型字符串。</param>
    /// <returns>格式化后的武器属性字符串。</returns>
    static string GetWeaponStatInfo(string statType)
    {
        if (Enum.TryParse(statType, out WeaponStatType weaponStat))
        {
            if (_weaponStatValues.TryGetValue(weaponStat, out float statValue))
            {
                float classMultiplier = ClassSynergy(weaponStat, _classType, _classStatSynergies);
                statValue *= (1 + (_prestigeStatMultiplier * _expertisePrestige)) * classMultiplier * ((float)_expertiseLevel / _expertiseMaxLevel);
                float displayStatValue = statValue;
                int statModificationId = ModificationIds.GenerateId(0, (int)weaponStat, statValue);

                if (weaponStat.Equals(WeaponStatType.MovementSpeed)
                    && LocalCharacter.TryGetComponent(out Movement movement))
                {
                    float movementSpeed = movement.Speed._Value;
                    statValue /= movementSpeed;
                }

                ModifyUnitStatBuff_DOTS unitStatBuff = new()
                {
                    StatType = (UnitStatType)Enum.Parse(typeof(UnitStatType), weaponStat.GetDescription()),
                    ModificationType = ModificationType.Add,
                    Value = statValue,
                    Modifier = 1,
                    IncreaseByStacks = false,
                    ValueByStacks = 0,
                    Priority = 0,
                    Id = new(statModificationId)
                };

                _weaponStats.TryAdd(statModificationId, unitStatBuff);
                return FormatWeaponStat(weaponStat, displayStatValue);
            }
        }
        return string.Empty;
    }

    /// <summary>
    /// 获取并格式化血统属性信息。
    /// </summary>
    /// <param name="statType">血统属性类型字符串。</param>
    /// <returns>格式化后的血统属性字符串。</returns>
    static string GetBloodStatInfo(string statType)
    {
        if (Enum.TryParse(statType, out BloodStatType bloodStat))
        {
            if (_bloodStatValues.TryGetValue(bloodStat, out float statValue))
            {
                float classMultiplier = ClassSynergy(bloodStat, _classType, _classStatSynergies);
                statValue *= ((1 + (_prestigeStatMultiplier * _legacyPrestige)) * classMultiplier * ((float)_legacyLevel / _legacyMaxLevel));
                string displayString = $"<color=#00FFFF>{BloodStatTypeAbbreviations[bloodStat]}</color>: <color=#90EE90>{(statValue * 100).ToString("F0") + "%"}</color>";

                int statModificationId = ModificationIds.GenerateId(1, (int)bloodStat, statValue);

                ModifyUnitStatBuff_DOTS unitStatBuff = new()
                {
                    StatType = (UnitStatType)Enum.Parse(typeof(UnitStatType), bloodStat.GetDescription()),
                    ModificationType = ModificationType.Add,
                    Value = statValue,
                    Modifier = 1,
                    IncreaseByStacks = false,
                    ValueByStacks = 0,
                    Priority = 0,
                    Id = new(statModificationId)
                };

                _bloodStats.TryAdd(statModificationId, unitStatBuff);

                return displayString;
            }
        }
        return "";
    }

    /// <summary>
    /// 更新伙伴属性文本显示。
    /// </summary>
    /// <param name="familiarStats">伙伴属性列表。</param>
    /// <param name="statTexts">显示属性的文本组件列表。</param>
    static void UpdateFamiliarStats(List<string> familiarStats, List<LocalizedText> statTexts)
    {
        if (_killSwitch) return;

        for (int i = 0; i < 3; i++)
        {
            if (!string.IsNullOrEmpty(familiarStats[i]))
            {
                if (!statTexts[i].enabled) statTexts[i].enabled = true;
                if (!statTexts[i].gameObject.active) statTexts[i].gameObject.SetActive(true);

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

    const string FISHING = "去钓鱼吧！";

    /// <summary>
    /// 更新任务追踪器UI。
    /// </summary>
    /// <param name="questObject">任务窗口的游戏对象。</param>
    /// <param name="questSubHeader">显示任务子标题的文本组件。</param>
    /// <param name="questIcon">显示任务图标的图像组件。</param>
    /// <param name="targetType">任务目标类型。</param>
    /// <param name="target">任务目标名称。</param>
    /// <param name="progress">当前任务进度。</param>
    /// <param name="goal">任务目标数量。</param>
    /// <param name="isVBlood">指示目标是否为VBlood单位。</param>
    static void UpdateQuests(GameObject questObject, LocalizedText questSubHeader, Image questIcon,
        TargetType targetType, string target, int progress, int goal, bool isVBlood)
    {
        if (_killSwitch) return;

        if (progress != goal && _objectStates[questObject])
        {
            if (!questObject.gameObject.active) questObject.gameObject.active = true;

            if (targetType.Equals(TargetType.Kill))
            {
                target = TrimToFirstWord(target);
            }
            else if (targetType.Equals(TargetType.Fish)) target = FISHING;

            questSubHeader.ForceSet($"<color=white>{target}</color>: {progress}/<color=yellow>{goal}</color>");

            switch (targetType)
            {
                case TargetType.Kill:
                    if (!questIcon.gameObject.active) questIcon.gameObject.active = true;
                    if (isVBlood && questIcon.sprite != _questKillVBloodUnit)
                    {
                        questIcon.sprite = _questKillVBloodUnit;
                    }
                    else if (!isVBlood && questIcon.sprite != _questKillStandardUnit)
                    {
                        questIcon.sprite = _questKillStandardUnit;
                    }
                    break;
                case TargetType.Craft:
                    if (!questIcon.gameObject.active) questIcon.gameObject.active = true;
                    PrefabGUID targetPrefabGUID = LocalizationService.GetPrefabGuidFromName(target);
                    ManagedItemData managedItemData = ManagedDataRegistry.GetOrDefault<ManagedItemData>(targetPrefabGUID);
                    if (managedItemData != null && questIcon.sprite != managedItemData.Icon)
                    {
                        questIcon.sprite = managedItemData.Icon;
                    }
                    break;
                case TargetType.Gather:
                    if (!questIcon.gameObject.active) questIcon.gameObject.active = true;
                    targetPrefabGUID = LocalizationService.GetPrefabGuidFromName(target);
                    if (target.Equals("Stone")) targetPrefabGUID = PrefabGUIDs.Item_Ingredient_Stone; // 不确定，暂时硬编码
                    managedItemData = ManagedDataRegistry.GetOrDefault<ManagedItemData>(targetPrefabGUID);
                    if (managedItemData != null && questIcon.sprite != managedItemData.Icon)
                    {
                        questIcon.sprite = managedItemData.Icon;
                    }
                    break;
                case TargetType.Fish:
                    if (!questIcon.gameObject.active) questIcon.gameObject.active = true;
                    managedItemData = ManagedDataRegistry.GetOrDefault<ManagedItemData>(PrefabGUIDs.FakeItem_AnyFish);
                    if (managedItemData != null && questIcon.sprite != managedItemData.Icon)
                    {
                        questIcon.sprite = managedItemData.Icon;
                    }
                    break;
                default:
                    break;
            }
        }
        else
        {
            questObject.gameObject.active = false;
            questIcon.gameObject.active = false;
        }
    }

    /// <summary>
    /// 配置轮换技能栏位UI。
    /// </summary>
    /// <param name="shiftSlotObject">轮换栏位游戏对象的引用。</param>
    /// <param name="shiftSlotEntry">轮换栏位条目组件的引用。</param>
    /// <param name="uiState">轮换栏位UI状态的引用。</param>
    /// <param name="cooldownObject">冷却时间父级游戏对象的引用。</param>
    /// <param name="cooldownText">冷却时间文本组件的引用。</param>
    /// <param name="chargeCooldownTextObject">充能冷却时间文本游戏对象的引用。</param>
    /// <param name="cooldownFill">冷却时间填充图像的引用。</param>
    /// <param name="chargeCooldownText">充能冷却时间文本组件的引用。</param>
    /// <param name="chargeCooldownFillImage">充能冷却时间填充图像的引用。</param>
    /// <param name="chargeCooldownFillObject">充能冷却时间填充游戏对象的引用。</param>
    /// <param name="abilityEmptyIcon">空技能图标游戏对象的引用。</param>
    /// <param name="abilityIcon">技能图标游戏对象的引用。</param>
    /// <param name="keybindObject">键位绑定游戏对象的引用。</param>
    static void ConfigureShiftSlot(ref GameObject shiftSlotObject, ref AbilityBarEntry shiftSlotEntry, ref AbilityBarEntry.UIState uiState, ref GameObject cooldownObject,
    ref TextMeshProUGUI cooldownText, ref GameObject chargeCooldownTextObject, ref Image cooldownFill,
    ref TextMeshProUGUI chargeCooldownText, ref Image chargeCooldownFillImage, ref GameObject chargeCooldownFillObject,
    ref GameObject abilityEmptyIcon, ref GameObject abilityIcon, ref GameObject keybindObject)
    {
        GameObject abilityDummyObject = GameObject.Find("HUDCanvas(Clone)/BottomBarCanvas/BottomBar(Clone)/Content/Background/AbilityBar/Abilities/AbilityBarEntry_Dummy/");

        if (abilityDummyObject != null)
        {
            shiftSlotObject = GameObject.Instantiate(abilityDummyObject);
            RectTransform rectTransform = shiftSlotObject.GetComponent<RectTransform>();

            RectTransform abilitiesTransform = GameObject.Find("HUDCanvas(Clone)/BottomBarCanvas/BottomBar(Clone)/Content/Background/AbilityBar/Abilities/").GetComponent<RectTransform>();

            GameObject.DontDestroyOnLoad(shiftSlotObject);
            SceneManager.MoveGameObjectToScene(shiftSlotObject, SceneManager.GetSceneByName("VRisingWorld"));

            shiftSlotObject.transform.SetParent(abilitiesTransform, false);
            shiftSlotObject.SetActive(false);

            shiftSlotEntry = shiftSlotObject.GetComponent<AbilityBarEntry>();
            shiftSlotEntry._CurrentUIState.CachedInputVersion = 3;
            uiState = shiftSlotEntry._CurrentUIState;

            cooldownObject = FindTargetUIObject(rectTransform, "CooldownParent").gameObject;
            cooldownText = FindTargetUIObject(rectTransform, "Cooldown").GetComponent<TextMeshProUGUI>();
            cooldownText.SetText("");
            cooldownText.alpha = 1f;
            cooldownText.color = Color.white;
            cooldownText.enabled = true;

            cooldownFill = FindTargetUIObject(rectTransform, "CooldownOverlayFill").GetComponent<Image>();
            cooldownFill.fillAmount = 0f;
            cooldownFill.enabled = true;

            chargeCooldownFillObject = FindTargetUIObject(rectTransform, "ChargeCooldownImage");
            chargeCooldownFillImage = chargeCooldownFillObject.GetComponent<Image>();
            chargeCooldownFillImage.fillOrigin = 2;
            chargeCooldownFillImage.fillAmount = 0f;
            chargeCooldownFillImage.fillMethod = Image.FillMethod.Radial360;
            chargeCooldownFillImage.fillClockwise = true;
            chargeCooldownFillImage.enabled = true;

            chargeCooldownTextObject = FindTargetUIObject(rectTransform, "ChargeCooldown");
            chargeCooldownText = chargeCooldownTextObject.GetComponent<TextMeshProUGUI>();
            chargeCooldownText.SetText("");
            chargeCooldownText.alpha = 1f;
            chargeCooldownText.color = Color.white;
            chargeCooldownText.enabled = true;

            abilityEmptyIcon = FindTargetUIObject(rectTransform, "EmptyIcon");
            abilityEmptyIcon.SetActive(false);

            abilityIcon = FindTargetUIObject(rectTransform, "Icon");
            abilityIcon.SetActive(true);

            keybindObject = GameObject.Find("HUDCanvas(Clone)/BottomBarCanvas/BottomBar(Clone)/Content/Background/AbilityBar/Abilities/AbilityBarEntry_Dummy(Clone)/KeybindBackground/Keybind/");
            TextMeshProUGUI keybindText = keybindObject.GetComponent<TextMeshProUGUI>();
            keybindText.SetText("Shift");
            keybindText.enabled = true;

            _objectStates.Add(shiftSlotObject, true);
            _gameObjects.Add(UIElement.ShiftSlot, shiftSlotObject);

            SimpleStunButton stunButton = shiftSlotObject.AddComponent<SimpleStunButton>();

            if (_actionToggles.TryGetValue(SHIFT_SLOT, out var toggleAction))
            {
                stunButton.onClick.AddListener(new Action(toggleAction));
            }
        }
        else
        {
            Core.Log.LogWarning("AbilityBarEntry_Dummy 为空！");
        }
    }

    /// <summary>
    /// 配置任务窗口UI。
    /// </summary>
    /// <param name="questObject">任务窗口游戏对象的引用。</param>
    /// <param name="questType">任务类型（每日/每周）。</param>
    /// <param name="headerColor">标题颜色。</param>
    /// <param name="header">标题文本组件的引用。</param>
    /// <param name="subHeader">子标题文本组件的引用。</param>
    /// <param name="questIcon">任务图标图像组件的引用。</param>
    static void ConfigureQuestWindow(ref GameObject questObject, UIElement questType, Color headerColor,
        ref LocalizedText header, ref LocalizedText subHeader, ref Image questIcon)
    {
        questObject = GameObject.Instantiate(_canvasBase.BottomBarParentPrefab.FakeTooltip.gameObject);
        RectTransform questTransform = questObject.GetComponent<RectTransform>();

        GameObject.DontDestroyOnLoad(questObject);
        SceneManager.MoveGameObjectToScene(questObject, SceneManager.GetSceneByName("VRisingWorld"));

        questTransform.SetParent(_bottomBarCanvas.transform, false);
        questTransform.gameObject.layer = _layer;
        questObject.SetActive(true);

        GameObject entries = FindTargetUIObject(questObject.transform, "InformationEntries");
        DeactivateChildrenExceptNamed(entries.transform, "TooltipHeader");

        GameObject tooltipHeader = FindTargetUIObject(questObject.transform, "TooltipHeader");
        tooltipHeader.SetActive(true);

        GameObject iconNameObject = FindTargetUIObject(tooltipHeader.transform, "Icon&Name");
        iconNameObject.SetActive(true);

        GameObject levelFrame = FindTargetUIObject(iconNameObject.transform, "LevelFrame");
        levelFrame.SetActive(false);
        GameObject reforgeCost = FindTargetUIObject(questObject.transform, "Tooltip_ReforgeCost");
        reforgeCost.SetActive(false);

        GameObject tooltipIcon = FindTargetUIObject(tooltipHeader.transform, "TooltipIcon");
        RectTransform tooltipIconTransform = tooltipIcon.GetComponent<RectTransform>();

        tooltipIconTransform.anchorMin = new Vector2(tooltipIconTransform.anchorMin.x, 0.55f);
        tooltipIconTransform.anchorMax = new Vector2(tooltipIconTransform.anchorMax.x, 0.55f);

        tooltipIconTransform.pivot = new Vector2(tooltipIconTransform.pivot.x, 0.55f);

        questIcon = tooltipIcon.GetComponent<Image>();
        if (questType.Equals(UIElement.Daily))
        {
            if (Sprites.ContainsKey("BloodIcon_Small_Warrior"))
            {
                questIcon.sprite = Sprites["BloodIcon_Small_Warrior"];
            }
        }
        else if (questType.Equals(UIElement.Weekly))
        {
            if (Sprites.ContainsKey("BloodIcon_Warrior"))
            {
                questIcon.sprite = Sprites["BloodIcon_Warrior"];
            }
        }

        tooltipIconTransform.sizeDelta = new Vector2(tooltipIconTransform.sizeDelta.x * 0.35f, tooltipIconTransform.sizeDelta.y * 0.35f);

        GameObject subHeaderObject = FindTargetUIObject(iconNameObject.transform, "TooltipSubHeader");
        header = FindTargetUIObject(iconNameObject.transform, "TooltipHeader").GetComponent<LocalizedText>();
        header.Text.fontSize *= 2f;
        header.Text.color = headerColor;
        subHeader = subHeaderObject.GetComponent<LocalizedText>();
        subHeader.Text.enableAutoSizing = false;
        subHeader.Text.autoSizeTextContainer = false;
        subHeader.Text.enableWordWrapping = false;

        ContentSizeFitter subHeaderFitter = subHeaderObject.GetComponent<ContentSizeFitter>();
        subHeaderFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        subHeaderFitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

        questTransform.sizeDelta = new Vector2(questTransform.sizeDelta.x * 0.65f, questTransform.sizeDelta.y);
        questTransform.anchorMin = new Vector2(1, _windowOffset);
        questTransform.anchorMax = new Vector2(1, _windowOffset);
        questTransform.pivot = new Vector2(1, _windowOffset);
        questTransform.anchoredPosition = new Vector2(0, _windowOffset);

        header.ForceSet(questType.GetDescription() + "任务");
        subHeader.ForceSet("目标: 0/0");

        _gameObjects.Add(questType, questObject);
        _objectStates.Add(questObject, true);
        _windowOffset += 0.075f;
    }

    /// <summary>
    /// 配置水平进度条UI（如经验条、传承条等）。
    /// </summary>
    /// <param name="barGameObject">进度条游戏对象的引用。</param>
    /// <param name="informationPanelObject">信息面板游戏对象的引用。</param>
    /// <param name="fill">填充图像的引用。</param>
    /// <param name="level">等级文本组件的引用。</param>
    /// <param name="header">标题文本组件的引用。</param>
    /// <param name="element">UI元素类型。</param>
    /// <param name="fillColor">填充颜色。</param>
    /// <param name="firstText">信息面板中第一个文本组件的引用。</param>
    /// <param name="secondText">信息面板中第二个文本组件的引用。</param>
    /// <param name="thirdText">信息面板中第三个文本组件的引用。</param>
    static void ConfigureHorizontalProgressBar(ref GameObject barGameObject, ref GameObject informationPanelObject, ref Image fill,
        ref LocalizedText level, ref LocalizedText header, UIElement element, Color fillColor,
        ref LocalizedText firstText, ref LocalizedText secondText, ref LocalizedText thirdText)
    {
        barGameObject = GameObject.Instantiate(_canvasBase.TargetInfoParent.gameObject);

        GameObject.DontDestroyOnLoad(barGameObject);
        SceneManager.MoveGameObjectToScene(barGameObject, SceneManager.GetSceneByName("VRisingWorld"));

        RectTransform barRectTransform = barGameObject.GetComponent<RectTransform>();
        barRectTransform.SetParent(_targetInfoPanelCanvas.transform, false);
        barRectTransform.gameObject.layer = _layer;

        float offsetY = BAR_HEIGHT_SPACING * _barNumber;
        float offsetX = 1f - BAR_WIDTH_SPACING;
        barRectTransform.anchorMin = new Vector2(offsetX, 0.6f - offsetY);
        barRectTransform.anchorMax = new Vector2(offsetX, 0.6f - offsetY);
        barRectTransform.pivot = new Vector2(offsetX, 0.6f - offsetY);

        barRectTransform.localScale = new Vector3(0.7f, 0.7f, 1f);

        fill = FindTargetUIObject(barRectTransform.transform, "Fill").GetComponent<Image>();
        level = FindTargetUIObject(barRectTransform.transform, "LevelText").GetComponent<LocalizedText>();
        header = FindTargetUIObject(barRectTransform.transform, "Name").GetComponent<LocalizedText>();

        fill.fillAmount = 0f;
        fill.color = fillColor;
        level.ForceSet("0");

        header.ForceSet(element.GetDescription());
        header.Text.fontSize *= 1.5f;
        _horizontalBarHeaderFontSize = header.Text.fontSize;

        FindTargetUIObject(barRectTransform.transform, "DamageTakenFill").GetComponent<Image>().fillAmount = 0f;
        FindTargetUIObject(barRectTransform.transform, "AbsorbFill").GetComponent<Image>().fillAmount = 0f;

        informationPanelObject = FindTargetUIObject(barRectTransform.transform, "InformationPanel");
        ConfigureInformationPanel(ref informationPanelObject, ref firstText, ref secondText, ref thirdText, element);

        _barNumber++;
        barGameObject.SetActive(true);

        _objectStates.Add(barGameObject, true);
        _gameObjects.Add(element, barGameObject);
    }

    /// <summary>
    /// 配置垂直进度条UI（用于专业）。
    /// </summary>
    /// <param name="barGameObject">进度条游戏对象的引用。</param>
    /// <param name="progressFill">当前等级进度填充图像的引用。</param>
    /// <param name="maxFill">总等级进度填充图像的引用。</param>
    /// <param name="level">等级文本组件的引用。</param>
    /// <param name="profession">专业类型。</param>
    static void ConfigureVerticalProgressBar(ref GameObject barGameObject, ref Image progressFill, ref Image maxFill,
        ref LocalizedText level, Profession profession)
    {
        barGameObject = GameObject.Instantiate(_canvasBase.TargetInfoParent.gameObject);

        GameObject.DontDestroyOnLoad(barGameObject);
        SceneManager.MoveGameObjectToScene(barGameObject, SceneManager.GetSceneByName("VRisingWorld"));

        RectTransform barRectTransform = barGameObject.GetComponent<RectTransform>();
        barRectTransform.SetParent(_targetInfoPanelCanvas.transform, false);
        barRectTransform.gameObject.layer = _layer;

        int totalBars = 8;
        float totalBarAreaWidth = 0.215f;
        float barWidth = totalBarAreaWidth / totalBars;

        float padding = 1f - (0.075f * 2.45f);
        float offsetX = padding + (barWidth * _graphBarNumber / 1.4f);

        Vector3 updatedScale = new(0.4f, 1f, 1f);
        barRectTransform.localScale = updatedScale;

        float offsetY = 0.24f;
        barRectTransform.anchorMin = new Vector2(offsetX, offsetY);
        barRectTransform.anchorMax = new Vector2(offsetX, offsetY);
        barRectTransform.pivot = new Vector2(offsetX, offsetY);

        progressFill = FindTargetUIObject(barRectTransform.transform, "Fill").GetComponent<Image>();
        progressFill.fillMethod = Image.FillMethod.Horizontal;
        progressFill.fillOrigin = 0;
        progressFill.fillAmount = 0f;
        progressFill.color = ProfessionColors[profession];

        barRectTransform.localRotation = Quaternion.Euler(0, 0, 90);

        level = FindTargetUIObject(barRectTransform.transform, "LevelText").GetComponent<LocalizedText>();

        GameObject levelBackgroundObject = FindTargetUIObject(barRectTransform.transform, "LevelBackground");

        Image levelBackgroundImage = levelBackgroundObject.GetComponent<Image>();
        Sprite professionIcon = _professionIcons.TryGetValue(profession, out string spriteName) && Sprites.TryGetValue(spriteName, out Sprite sprite) ? sprite : levelBackgroundImage.sprite;
        levelBackgroundImage.sprite = professionIcon ?? levelBackgroundImage.sprite;
        levelBackgroundImage.color = new(1f, 1f, 1f, 1f);
        levelBackgroundObject.transform.localRotation = Quaternion.Euler(0, 0, -90);
        levelBackgroundObject.transform.localScale = new(0.25f, 1f, 1f);

        var headerObject = FindTargetUIObject(barRectTransform.transform, "Name");
        headerObject?.SetActive(false);

        GameObject informationPanelObject = FindTargetUIObject(barRectTransform.transform, "InformationPanel");
        informationPanelObject?.SetActive(false);

        FindTargetUIObject(barRectTransform.transform, "DamageTakenFill").GetComponent<Image>().fillAmount = 0f;
        maxFill = FindTargetUIObject(barRectTransform.transform, "AbsorbFill").GetComponent<Image>();
        maxFill.fillAmount = 0f;
        maxFill.transform.localScale = new(1f, 0.25f, 1f);
        maxFill.color = _brightGold;

        _graphBarNumber++;

        barGameObject.SetActive(true);
        level.gameObject.SetActive(false);

        _objectStates.Add(barGameObject, true);
        _professionObjects.Add(barGameObject);
    }

    /// <summary>
    /// 配置信息面板UI。
    /// </summary>
    /// <param name="informationPanelObject">信息面板游戏对象的引用。</param>
    /// <param name="firstText">第一个文本组件的引用。</param>
    /// <param name="secondText">第二个文本组件的引用。</param>
    /// <param name="thirdText">第三个文本组件的引用。</param>
    /// <param name="element">UI元素类型。</param>
    static void ConfigureInformationPanel(ref GameObject informationPanelObject, ref LocalizedText firstText, ref LocalizedText secondText,
        ref LocalizedText thirdText, UIElement element)
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

    /// <summary>
    /// 配置经验信息面板UI。
    /// </summary>
    /// <param name="panel">面板游戏对象的引用。</param>
    /// <param name="firstText">第一个文本组件的引用（用于血统信息）。</param>
    /// <param name="secondText">第二个文本组件的引用（用于词缀/职业信息）。</param>
    /// <param name="thirdText">第三个文本组件的引用（用于平台用户名）。</param>
    static void ConfigureExperiencePanel(ref GameObject panel, ref LocalizedText firstText, ref LocalizedText secondText,
        ref LocalizedText thirdText)
    {
        RectTransform panelTransform = panel.GetComponent<RectTransform>();
        Vector2 panelAnchoredPosition = panelTransform.anchoredPosition;
        panelAnchoredPosition.x = -18f;

        firstText = FindTargetUIObject(panel.transform, "BloodInfo").GetComponent<LocalizedText>();
        firstText.ForceSet("");
        firstText.enabled = false;

        GameObject affixesObject = FindTargetUIObject(panel.transform, "Affixes");
        LayoutElement layoutElement = affixesObject.GetComponent<LayoutElement>();
        layoutElement.ignoreLayout = false;

        secondText = affixesObject.GetComponent<LocalizedText>();
        secondText.ForceSet("");
        secondText.Text.fontSize *= 1.2f;
        secondText.enabled = false;

        thirdText = FindTargetUIObject(panel.transform, "PlatformUserName").GetComponent<LocalizedText>();
        thirdText.ForceSet("");
        thirdText.enabled = false;
    }

    /// <summary>
    /// 配置默认的信息面板UI。
    /// </summary>
    /// <param name="panel">面板游戏对象的引用。</param>
    /// <param name="firstText">第一个文本组件的引用。</param>
    /// <param name="secondText">第二个文本组件的引用。</param>
    /// <param name="thirdText">第三个文本组件的引用。</param>
    static void ConfigureDefaultPanel(ref GameObject panel, ref LocalizedText firstText, ref LocalizedText secondText,
        ref LocalizedText thirdText)
    {
        RectTransform panelTransform = panel.GetComponent<RectTransform>();
        Vector2 panelAnchoredPosition = panelTransform.anchoredPosition;
        panelAnchoredPosition.x = -18f;

        firstText = FindTargetUIObject(panel.transform, "BloodInfo").GetComponent<LocalizedText>();
        firstText.ForceSet("");
        firstText.Text.fontSize *= 1.1f;
        firstText.enabled = false;

        GameObject affixesObject = FindTargetUIObject(panel.transform, "Affixes");
        LayoutElement layoutElement = affixesObject.GetComponent<LayoutElement>();
        layoutElement.ignoreLayout = false;

        secondText = affixesObject.GetComponent<LocalizedText>();
        secondText.ForceSet("");
        secondText.Text.fontSize *= 1.1f;
        secondText.enabled = false;

        thirdText = FindTargetUIObject(panel.transform, "PlatformUserName").GetComponent<LocalizedText>();
        thirdText.ForceSet("");
        thirdText.Text.fontSize *= 1.1f;
        thirdText.enabled = false;
    }

    /// <summary>
    /// 计算基于职业的属性协同加成。
    /// </summary>
    /// <typeparam name="T">属性类型（WeaponStatType 或 BloodStatType）。</typeparam>
    /// <param name="statType">具体的属性类型。</param>
    /// <param name="classType">玩家职业类型。</param>
    /// <param name="classStatSynergy">职业与属性协同的映射字典。</param>
    /// <returns>协同加成乘数。</returns>
    static float ClassSynergy<T>(T statType, PlayerClass classType, Dictionary<PlayerClass, (List<WeaponStatType> WeaponStatTypes, List<BloodStatType> BloodStatTypes)> classStatSynergy)
    {
        if (classType.Equals(PlayerClass.None))
            return 1f;

        if (typeof(T) == typeof(WeaponStatType) && classStatSynergy[classType].WeaponStatTypes.Contains((WeaponStatType)(object)statType))
        {
            return _classStatMultiplier;
        }
        else if (typeof(T) == typeof(BloodStatType) && classStatSynergy[classType].BloodStatTypes.Contains((BloodStatType)(object)statType))
        {
            return _classStatMultiplier;
        }

        return 1f;
    }

    /// <summary>
    /// 格式化武器属性值以供显示。
    /// </summary>
    /// <param name="weaponStat">武器属性类型。</param>
    /// <param name="statValue">属性值。</param>
    /// <returns>格式化后的武器属性字符串。</returns>
    static string FormatWeaponStat(WeaponStatType weaponStat, float statValue)
    {
        string statValueString = WeaponStatFormats[weaponStat] switch
        {
            "integer" => ((int)statValue).ToString(),
            "decimal" => statValue.ToString("F2"),
            "percentage" => (statValue * 100f).ToString("F1") + "%",
            _ => statValue.ToString(),
        };

        string displayString = $"<color=#00FFFF>{WeaponStatTypeAbbreviations[weaponStat]}</color>: <color=#90EE90>{statValueString}</color>";
        return displayString;
    }

    /// <summary>
    /// 将整数转换为罗马数字字符串。
    /// </summary>
    /// <param name="num">要转换的整数。</param>
    /// <returns>罗马数字字符串。</returns>
    static string IntegerToRoman(int num)
    {
        string result = string.Empty;

        foreach (var item in _romanNumerals)
        {
            while (num >= item.Key)
            {
                result += item.Value;
                num -= item.Key;
            }
        }

        return result;
    }

    /// <summary>
    /// 包含用于操作GameObject的实用方法。
    /// </summary>
    public static class GameObjectUtilities
    {
        /// <summary>
        /// 在指定的根变换下查找具有特定名称的目标UI对象。
        /// </summary>
        /// <param name="root">要搜索的根变换。</param>
        /// <param name="targetName">目标UI对象的名称。</param>
        /// <returns>找到的GameObject，如果未找到则返回 <c>null</c>。</returns>
        public static GameObject FindTargetUIObject(Transform root, string targetName)
        {
            Stack<(Transform transform, int indentLevel)> transformStack = new();
            transformStack.Push((root, 0));

            HashSet<Transform> visited = [];

            Il2CppArrayBase<Transform> children = root.GetComponentsInChildren<Transform>(true);

            List<Transform> transforms = [.. children];

            while (transformStack.Count > 0)
            {
                var (current, indentLevel) = transformStack.Pop();

                if (!visited.Add(current))
                {
                    continue;
                }

                if (current.gameObject.name.Equals(targetName, StringComparison.OrdinalIgnoreCase))
                {
                    return current.gameObject;
                }

                foreach (Transform child in transforms)
                {
                    if (child.parent == current)
                    {
                        transformStack.Push((child, indentLevel + 1));
                    }
                }
            }

            Core.Log.LogWarning($"名为 '{targetName}' 的GameObject未找到！");
            return null;
        }

        /// <summary>
        /// 查找并记录所有已加载的指定类型的Unity对象。
        /// </summary>
        /// <typeparam name="T">要查找的Unity对象类型。</typeparam>
        public static void FindLoadedObjects<T>() where T : UnityEngine.Object
        {
            Il2CppReferenceArray<UnityEngine.Object> resources = UnityEngine.Resources.FindObjectsOfTypeAll(Il2CppType.Of<T>());
            Core.Log.LogInfo($"找到 {resources.Length} 个 '{Il2CppType.Of<T>().FullName}'！");
            foreach (UnityEngine.Object resource in resources)
            {
                Core.Log.LogInfo($"精灵: {resource.name}");
            }
        }

        /// <summary>
        /// 停用指定根变换下除特定名称外的所有子对象。
        /// </summary>
        /// <param name="root">要操作的根变换。</param>
        /// <param name="targetName">要保留活动状态的子对象的名称。</param>
        public static void DeactivateChildrenExceptNamed(Transform root, string targetName)
        {
            Stack<(Transform transform, int indentLevel)> transformStack = new();
            transformStack.Push((root, 0));

            HashSet<Transform> visited = [];

            Il2CppArrayBase<Transform> children = root.GetComponentsInChildren<Transform>();
            List<Transform> transforms = [.. children];

            while (transformStack.Count > 0)
            {
                var (current, indentLevel) = transformStack.Pop();

                if (!visited.Add(current))
                {
                    continue;
                }

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

        /// <summary>
        /// 查找并记录（或写入文件）指定根变换下的所有GameObject及其组件。
        /// </summary>
        /// <param name="root">要搜索的根变换。</param>
        /// <param name="filePath">可选的文件路径，用于将结果写入文件。如果为空，则记录到控制台。</param>
        /// <param name="includeInactive">是否包含非活动的游戏对象。</param>
        public static void FindGameObjects(Transform root, string filePath = "", bool includeInactive = false)
        {
            Stack<(Transform transform, int indentLevel)> transformStack = new();
            transformStack.Push((root, 0));

            HashSet<Transform> visited = [];

            Il2CppArrayBase<Transform> children = root.GetComponentsInChildren<Transform>(includeInactive);
            List<Transform> transforms = [.. children];

            Core.Log.LogWarning($"找到 {transforms.Count} 个 GameObjects！");

            if (string.IsNullOrEmpty(filePath))
            {
                while (transformStack.Count > 0)
                {
                    var (current, indentLevel) = transformStack.Pop();

                    if (!visited.Add(current))
                    {
                        continue;
                    }

                    List<string> objectComponents = FindGameObjectComponents(current.gameObject);
                    string indent = new('|', indentLevel);
                    Core.Log.LogInfo($"{indent}{current.gameObject.name} | {string.Join(",", objectComponents)} | [{current.gameObject.scene.name}]");

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
                    continue;
                }

                List<string> objectComponents = FindGameObjectComponents(current.gameObject);
                string indent = new('|', indentLevel);
                writer.WriteLine($"{indent}{current.gameObject.name} | {string.Join(",", objectComponents)} | [{current.gameObject.scene.name}]");

                foreach (Transform child in transforms)
                {
                    if (child.parent == current)
                    {
                        transformStack.Push((child, indentLevel + 1));
                    }
                }
            }
        }

        /// <summary>
        /// 查找并返回指定GameObject上的所有组件名称。
        /// </summary>
        /// <param name="parentObject">要检查的GameObject。</param>
        /// <returns>组件名称列表。</returns>
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

        /// <summary>
        /// 查找并缓存所有在 <see cref="_spriteNames"/> 列表中定义的精灵。
        /// </summary>
        public static void FindSprites()
        {
            Il2CppArrayBase<Sprite> sprites = UnityEngine.Resources.FindObjectsOfTypeAll<Sprite>();

            foreach (Sprite sprite in sprites)
            {
                if (_spriteNames.Contains(sprite.name) && !Sprites.ContainsKey(sprite.name))
                {
                    _sprites[sprite.name] = sprite;

                    if (sprite.name.Equals("BloodIcon_Cursed") && _questKillVBloodUnit == null)
                    {
                        _questKillVBloodUnit = sprite;
                    }

                    if (sprite.name.Equals("BloodIcon_Warrior") && _questKillStandardUnit == null)
                    {
                        _questKillStandardUnit = sprite;
                    }
                }
            }
        }
    }

    /// <summary>
    /// 将字符串修剪为其第一个单词（如果包含多个单词）。
    /// </summary>
    /// <param name="name">要修剪的字符串。</param>
    /// <returns>修剪后的字符串，或原始字符串（如果只有一个单词或没有空格）。</returns>
    static string TrimToFirstWord(string name)
    {
        int firstSpaceIndex = name.IndexOf(' ');
        int secondSpaceIndex = name.IndexOf(' ', firstSpaceIndex + 1);

        if (firstSpaceIndex > 0 && secondSpaceIndex > 0)
        {
            return name[..firstSpaceIndex];
        }

        return name;
    }

    /// <summary>
    /// 重置CanvasService的状态，销毁所有创建的UI对象并清除缓存。
    /// </summary>
    public static void ResetState()
    {
        foreach (GameObject gameObject in CanvasService._objectStates.Keys)
        {
            if (gameObject != null)
            {
                GameObject.Destroy(gameObject);
            }
        }

        foreach (GameObject gameObject in CanvasService._professionObjects)
        {
            if (gameObject != null)
            {
                GameObject.Destroy(gameObject);
            }
        }

        _objectStates.Clear();
        _professionObjects.Clear();
        _gameObjects.Clear();

        _sprites.Clear();
    }
}