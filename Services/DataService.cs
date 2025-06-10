using Stunlock.Core;
using System.ComponentModel;
using System.Globalization;
using UnityEngine;
using static Eclipse.Services.CanvasService;

namespace Eclipse.Services;
internal static class DataService
{
    public enum TargetType
    {
        [Description("击杀")]
        Kill,

        [Description("制作")]
        Craft,

        [Description("采集")]
        Gather,

        [Description("钓鱼")]
        Fish
    }
    public enum Profession
    {
        [Description("附魔")]
        Enchanting,

        [Description("炼金")]
        Alchemy,

        [Description("草药学")]
        Harvesting,

        [Description("锻造")]
        Blacksmithing,

        [Description("裁缝")]
        Tailoring,

        [Description("伐木")]
        Woodcutting,

        [Description("采矿")]
        Mining,

        [Description("钓鱼")]
        Fishing
    }

    public enum PlayerClass
    {
        [Description("无")]
        None,

        [Description("鲜血骑士")]
        BloodKnight,

        [Description("恶魔猎手")]
        DemonHunter,

        [Description("吸血鬼领主")]
        VampireLord,

        [Description("暗影之刃")]
        ShadowBlade,

        [Description("奥术巫师")]
        ArcaneSorcerer,

        [Description("死亡法师")]
        DeathMage
    }

    public enum BloodType
    {
        [Description("工人")]
        Worker,

        [Description("战士")]
        Warrior,

        [Description("学者")]
        Scholar,

        [Description("游荡者")]
        Rogue,

        [Description("变异体")]
        Mutant,

        [Description("V型血")]
        VBlood,

        [Description("孱弱")]
        Frailed,

        [Description("传送门首领")]
        GateBoss,

        [Description("德古拉血裔")]
        Draculin,

        [Description("不朽者")]
        Immortal,

        [Description("生物")]
        Creature,

        [Description("蛮徒")]
        Brute,

        [Description("腐化")]
        Corruption
    }
    public enum WeaponType
    {
        [Description("剑")]
        Sword,

        [Description("斧")]
        Axe,

        [Description("锤")]
        Mace,

        [Description("矛")]
        Spear,

        [Description("弩")]
        Crossbow,

        [Description("大剑")]
        GreatSword,

        [Description("双刃")]
        Slashers,

        [Description("手枪")]
        Pistols,

        [Description("镰刀")]
        Reaper,

        [Description("长弓")]
        Longbow,

        [Description("鞭")]
        Whip,

        [Description("徒手")]
        Unarmed,

        [Description("鱼竿")]
        FishingPole,

        [Description("双剑")]
        TwinBlades,

        [Description("匕首")]
        Daggers,

        [Description("爪")]
        Claws
    }

    public static Dictionary<WeaponStatType, float> _weaponStatValues = [];
    public enum WeaponStatType
    {
        [Description("无")]
        None,

        [Description("最大生命值")]
        MaxHealth,

        [Description("移动速度")]
        MovementSpeed,

        [Description("普通攻击速度")]
        PrimaryAttackSpeed,

        [Description("物理吸血")]
        PhysicalLifeLeech,

        [Description("法术吸血")]
        SpellLifeLeech,

        [Description("普攻吸血")]
        PrimaryLifeLeech,

        [Description("物理攻击力")]
        PhysicalPower,

        [Description("法术强度")]
        SpellPower,

        [Description("物理暴击率")]
        PhysicalCriticalStrikeChance,

        [Description("物理暴击伤害")]
        PhysicalCriticalStrikeDamage,

        [Description("法术暴击率")]
        SpellCriticalStrikeChance,

        [Description("法术暴击伤害")]
        SpellCriticalStrikeDamage
    }

    // 缩写保持英文，因为它们是通用的游戏术语
    public static readonly Dictionary<WeaponStatType, string> WeaponStatTypeAbbreviations = new()
    {
        { WeaponStatType.MaxHealth, "HP" },
        { WeaponStatType.MovementSpeed, "MS" },
        { WeaponStatType.PrimaryAttackSpeed, "PAS" },
        { WeaponStatType.PhysicalLifeLeech, "PLL" },
        { WeaponStatType.SpellLifeLeech, "SLL" },
        { WeaponStatType.PrimaryLifeLeech, "PAL" },
        { WeaponStatType.PhysicalPower, "PP" },
        { WeaponStatType.SpellPower, "SP" },
        { WeaponStatType.PhysicalCriticalStrikeChance, "PCC" },
        { WeaponStatType.PhysicalCriticalStrikeDamage, "PCD" },
        { WeaponStatType.SpellCriticalStrikeChance, "SCC" },
        { WeaponStatType.SpellCriticalStrikeDamage, "SCD" }
    };

    public static readonly Dictionary<string, string> WeaponStatStringAbbreviations = new()
    {
        { "MaxHealth", "HP" },
        { "MovementSpeed", "MS" },
        { "PrimaryAttackSpeed", "PAS" },
        { "PhysicalLifeLeech", "PLL" },
        { "SpellLifeLeech", "SLL" },
        { "PrimaryLifeLeech", "PAL" },
        { "PhysicalPower", "PP" },
        { "SpellPower", "SP" },
        { "PhysicalCriticalStrikeChance", "PCC" },
        { "PhysicalCriticalStrikeDamage", "PCD" },
        { "SpellCriticalStrikeChance", "SCC" },
        { "SpellCriticalStrikeDamage", "SCD" }
    };

    // 格式化关键字是逻辑的一部分，不能修改
    public static readonly Dictionary<WeaponStatType, string> WeaponStatFormats = new()
    {
        { WeaponStatType.MaxHealth, "integer" },
        { WeaponStatType.MovementSpeed, "decimal" },
        { WeaponStatType.PrimaryAttackSpeed, "percentage" },
        { WeaponStatType.PhysicalLifeLeech, "percentage" },
        { WeaponStatType.SpellLifeLeech, "percentage" },
        { WeaponStatType.PrimaryLifeLeech, "percentage" },
        { WeaponStatType.PhysicalPower, "integer" },
        { WeaponStatType.SpellPower, "integer" },
        { WeaponStatType.PhysicalCriticalStrikeChance, "percentage" },
        { WeaponStatType.PhysicalCriticalStrikeDamage, "percentage" },
        { WeaponStatType.SpellCriticalStrikeChance, "percentage" },
        { WeaponStatType.SpellCriticalStrikeDamage, "percentage" }
    };

    public static Dictionary<BloodStatType, float> _bloodStatValues = [];
    public enum BloodStatType
    {
        [Description("无")]
        None,

        [Description("受到的治疗效果")]
        HealingReceived,

        [Description("伤害减免")]
        DamageReduction,

        [Description("物理抗性")]
        PhysicalResistance,

        [Description("法术抗性")]
        SpellResistance,

        [Description("资源产出")]
        ResourceYield,

        [Description("降低鲜血流失率")]
        ReducedBloodDrain,

        [Description("法术冷却恢复速率")]
        SpellCooldownRecoveryRate,

        [Description("武器技能冷却恢复速率")]
        WeaponCooldownRecoveryRate,

        [Description("终极技能冷却恢复速率")]
        UltimateCooldownRecoveryRate,

        [Description("仆从伤害")]
        MinionDamage,

        [Description("技能攻击速度")]
        AbilityAttackSpeed,

        [Description("腐化伤害减免")]
        CorruptionDamageReduction
    }

    public static readonly Dictionary<BloodStatType, string> BloodStatTypeAbbreviations = new()
    {
        { BloodStatType.HealingReceived, "HR" },
        { BloodStatType.DamageReduction, "DR" },
        { BloodStatType.PhysicalResistance, "PR" },
        { BloodStatType.SpellResistance, "SR" },
        { BloodStatType.ResourceYield, "RY" },
        { BloodStatType.ReducedBloodDrain, "RBD" },
        { BloodStatType.SpellCooldownRecoveryRate, "SCR" },
        { BloodStatType.WeaponCooldownRecoveryRate, "WCR" },
        { BloodStatType.UltimateCooldownRecoveryRate, "UCR" },
        { BloodStatType.MinionDamage, "MD" },
        { BloodStatType.AbilityAttackSpeed, "AAS" },
        { BloodStatType.CorruptionDamageReduction, "CDR" }
    };

    public static readonly Dictionary<string, string> BloodStatStringAbbreviations = new()
    {
        { "HealingReceived", "HR" },
        { "DamageReduction", "DR" },
        { "PhysicalResistance", "PR" },
        { "SpellResistance", "SR" },
        { "ResourceYield", "RY" },
        { "ReducedBloodDrain", "RBD" },
        { "SpellCooldownRecoveryRate", "SCR" },
        { "WeaponCooldownRecoveryRate", "WCR" },
        { "UltimateCooldownRecoveryRate", "UCR" },
        { "MinionDamage", "MD" },
        { "AbilityAttackSpeed", "AAS" },
        { "CorruptionDamageReduction", "CDR" }
    };

    public static Dictionary<FamiliarStatType, float> _familiarStatValues = [];
    public enum FamiliarStatType
    {
        [Description("最大生命值")]
        MaxHealth,

        [Description("物理攻击力")]
        PhysicalPower,

        [Description("法术强度")]
        SpellPower
    }

    public static readonly Dictionary<FamiliarStatType, string> FamiliarStatTypeAbbreviations = new()
    {
        { FamiliarStatType.MaxHealth, "HP" },
        { FamiliarStatType.PhysicalPower, "PP" },
        { FamiliarStatType.SpellPower, "SP" }
    };

    public static readonly List<string> FamiliarStatStringAbbreviations = new()
    {
        { "HP" },
        { "PP" },
        { "SP" }
    };

    public static readonly Dictionary<Profession, Color> ProfessionColors = new()
    {
        { Profession.Enchanting,    new Color(0.5f, 0.1f, 0.8f, 0.5f) },
        { Profession.Alchemy,       new Color(0.1f, 0.9f, 0.7f, 0.5f) },
        { Profession.Harvesting,    new Color(0f, 0.5f, 0f, 0.5f) },
        { Profession.Blacksmithing, new Color(0.2f, 0.2f, 0.3f, 0.5f) },
        { Profession.Tailoring,     new Color(0.9f, 0.6f, 0.5f, 0.5f) },
        { Profession.Woodcutting,   new Color(0.5f, 0.3f, 0.1f, 0.5f) },
        { Profession.Mining,        new Color(0.5f, 0.5f, 0.5f, 0.5f) },
        { Profession.Fishing,       new Color(0f, 0.5f, 0.7f, 0.5f) }
    };

    public static Dictionary<PlayerClass, (List<WeaponStatType> WeaponStats, List<BloodStatType> BloodStats)> _classStatSynergies = [];

    public static float _prestigeStatMultiplier;
    public static float _classStatMultiplier;
    public static bool _extraRecipes;
    public static PrefabGUID _primalCost;
    public class ProfessionData(string enchantingProgress, string enchantingLevel, string alchemyProgress, string alchemyLevel,
        string harvestingProgress, string harvestingLevel, string blacksmithingProgress, string blacksmithingLevel,
        string tailoringProgress, string tailoringLevel, string woodcuttingProgress, string woodcuttingLevel, string miningProgress,
        string miningLevel, string fishingProgress, string fishingLevel)
    {
        public float EnchantingProgress { get; set; } = float.Parse(enchantingProgress, CultureInfo.InvariantCulture) / 100f;
        public int EnchantingLevel { get; set; } = int.Parse(enchantingLevel, CultureInfo.InvariantCulture);
        public float AlchemyProgress { get; set; } = float.Parse(alchemyProgress, CultureInfo.InvariantCulture) / 100f;
        public int AlchemyLevel { get; set; } = int.Parse(alchemyLevel, CultureInfo.InvariantCulture);
        public float HarvestingProgress { get; set; } = float.Parse(harvestingProgress, CultureInfo.InvariantCulture) / 100f;
        public int HarvestingLevel { get; set; } = int.Parse(harvestingLevel, CultureInfo.InvariantCulture);
        public float BlacksmithingProgress { get; set; } = float.Parse(blacksmithingProgress, CultureInfo.InvariantCulture) / 100f;
        public int BlacksmithingLevel { get; set; } = int.Parse(blacksmithingLevel, CultureInfo.InvariantCulture);
        public float TailoringProgress { get; set; } = float.Parse(tailoringProgress, CultureInfo.InvariantCulture) / 100f;
        public int TailoringLevel { get; set; } = int.Parse(tailoringLevel, CultureInfo.InvariantCulture);
        public float WoodcuttingProgress { get; set; } = float.Parse(woodcuttingProgress, CultureInfo.InvariantCulture) / 100f;
        public int WoodcuttingLevel { get; set; } = int.Parse(woodcuttingLevel, CultureInfo.InvariantCulture);
        public float MiningProgress { get; set; } = float.Parse(miningProgress, CultureInfo.InvariantCulture) / 100f;
        public int MiningLevel { get; set; } = int.Parse(miningLevel, CultureInfo.InvariantCulture);
        public float FishingProgress { get; set; } = float.Parse(fishingProgress, CultureInfo.InvariantCulture) / 100f;
        public int FishingLevel { get; set; } = int.Parse(fishingLevel, CultureInfo.InvariantCulture);
    }
    public class ExperienceData(string percent, string level, string prestige, string playerClass)
    {
        public float Progress { get; set; } = float.Parse(percent, CultureInfo.InvariantCulture) / 100f;
        public int Level { get; set; } = int.Parse(level, CultureInfo.InvariantCulture);
        public int Prestige { get; set; } = int.Parse(prestige, CultureInfo.InvariantCulture);
        public PlayerClass Class { get; set; } = (PlayerClass)int.Parse(playerClass, CultureInfo.InvariantCulture);
    }
    public class LegacyData(string percent, string level, string prestige, string legacyType, string bonusStats) : ExperienceData(percent, level, prestige, legacyType)
    {
        public string LegacyType { get; set; } = ((BloodType)int.Parse(legacyType, CultureInfo.InvariantCulture)).GetDescription();
        public List<string> BonusStats { get; set; } = [.. Enumerable.Range(0, bonusStats.Length / 2).Select(i => ((BloodStatType)int.Parse(bonusStats.Substring(i * 2, 2), CultureInfo.InvariantCulture)).GetDescription())];
    }
    public class ExpertiseData(string percent, string level, string prestige, string expertiseType, string bonusStats) : ExperienceData(percent, level, prestige, expertiseType)
    {
        public string ExpertiseType { get; set; } = ((WeaponType)int.Parse(expertiseType)).GetDescription();
        public List<string> BonusStats { get; set; } = [.. Enumerable.Range(0, bonusStats.Length / 2).Select(i => ((WeaponStatType)int.Parse(bonusStats.Substring(i * 2, 2), CultureInfo.InvariantCulture)).GetDescription())];
    }
    public class QuestData(string type, string progress, string goal, string target, string isVBlood)
    {
        public TargetType TargetType { get; set; } = (TargetType)int.Parse(type, CultureInfo.InvariantCulture);
        public int Progress { get; set; } = int.Parse(progress, CultureInfo.InvariantCulture);
        public int Goal { get; set; } = int.Parse(goal, CultureInfo.InvariantCulture);
        public string Target { get; set; } = target;
        //public bool IsVBlood { get; set; } = bool.Parse(isVBlood);
        // 或者更健壮的方式，考虑大小写和默认值
        public bool IsVBlood { get; set; } = ParseChineseBoolean(isVBlood);
    }
    /// <summary>
    /// 可以添加一个辅助方法
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    private static bool ParseChineseBoolean(string value)
    {
        if (string.Equals(value, "是", StringComparison.OrdinalIgnoreCase))
            return true;
        if (string.Equals(value, "否", StringComparison.OrdinalIgnoreCase))
            return false;
        return bool.Parse(value); // 如果不是中文，尝试标准解析
    }

    public class FamiliarData(string percent, string level, string prestige, string familiarName, string familiarStats)
    {
        public float Progress { get; set; } = float.Parse(percent, CultureInfo.InvariantCulture) / 100f;
        public int Level { get; set; } = int.TryParse(level, out int parsedLevel) && parsedLevel > 0 ? parsedLevel : 1;
        public int Prestige { get; set; } = int.Parse(prestige, CultureInfo.InvariantCulture);
        public string FamiliarName { get; set; } = !string.IsNullOrEmpty(familiarName) ? familiarName : "伙伴";
        public List<string> FamiliarStats { get; set; } = !string.IsNullOrEmpty(familiarStats) ? [.. new List<string> { familiarStats[..4], familiarStats[4..7], familiarStats[7..] }.Select(stat => int.Parse(stat, CultureInfo.InvariantCulture).ToString())] : ["", "", ""];
    }
    public class ShiftSpellData(string index)
    {
        public int ShiftSpellIndex { get; set; } = int.Parse(index, CultureInfo.InvariantCulture);
    }
    public class ConfigDataV1_3
    {
        public float PrestigeStatMultiplier;

        public float ClassStatMultiplier;

        public int MaxPlayerLevel;

        public int MaxLegacyLevel;

        public int MaxExpertiseLevel;

        public int MaxFamiliarLevel;

        public int MaxProfessionLevel;

        public bool ExtraRecipes;

        public int PrimalCost;

        public Dictionary<WeaponStatType, float> WeaponStatValues;

        public Dictionary<BloodStatType, float> BloodStatValues;

        public Dictionary<PlayerClass, (List<WeaponStatType> WeaponStats, List<BloodStatType> bloodStats)> ClassStatSynergies;
        public ConfigDataV1_3(string prestigeMultiplier, string statSynergyMultiplier, string maxPlayerLevel, string maxLegacyLevel, string maxExpertiseLevel, string maxFamiliarLevel, string maxProfessionLevel, string extraRecipes, string primalCost, string weaponStatValues, string bloodStatValues, string classStatSynergies)
        {
            PrestigeStatMultiplier = float.Parse(prestigeMultiplier, CultureInfo.InvariantCulture);
            ClassStatMultiplier = float.Parse(statSynergyMultiplier, CultureInfo.InvariantCulture);

            MaxPlayerLevel = int.Parse(maxPlayerLevel, CultureInfo.InvariantCulture);
            MaxLegacyLevel = int.Parse(maxLegacyLevel, CultureInfo.InvariantCulture);
            MaxExpertiseLevel = int.Parse(maxExpertiseLevel, CultureInfo.InvariantCulture);
            MaxFamiliarLevel = int.Parse(maxFamiliarLevel, CultureInfo.InvariantCulture);
            MaxProfessionLevel = int.Parse(maxProfessionLevel, CultureInfo.InvariantCulture);

            ExtraRecipes = bool.Parse(extraRecipes);
            PrimalCost = int.Parse(primalCost, CultureInfo.InvariantCulture);

            WeaponStatValues = weaponStatValues.Split(',')
            .Select((value, index) => new { Index = index + 1, Value = float.Parse(value, CultureInfo.InvariantCulture) })
            .ToDictionary(x => (WeaponStatType)x.Index, x => x.Value);

            BloodStatValues = bloodStatValues.Split(',')
            .Select((value, index) => new { Index = index + 1, Value = float.Parse(value, CultureInfo.InvariantCulture) })
            .ToDictionary(x => (BloodStatType)x.Index, x => x.Value);

            ClassStatSynergies = classStatSynergies
            .Split(',')
            .Select((value, index) => new { Value = value, Index = index })
            .GroupBy(x => x.Index / 3)
            .ToDictionary(
                g => (PlayerClass)int.Parse(g.ElementAt(0).Value, CultureInfo.InvariantCulture),
                g => (
                    Enumerable.Range(0, g.ElementAt(1).Value.Length / 2)
                        .Select(j => (WeaponStatType)int.Parse(g.ElementAt(1).Value.Substring(j * 2, 2), CultureInfo.InvariantCulture))
                        .ToList(),
                    Enumerable.Range(0, g.ElementAt(2).Value.Length / 2)
                        .Select(j => (BloodStatType)int.Parse(g.ElementAt(2).Value.Substring(j * 2, 2), CultureInfo.InvariantCulture))
                        .ToList()
                )
            );
        }
    }
    public static List<string> ParseMessageString(string serverMessage)
    {
        if (string.IsNullOrEmpty(serverMessage))
        {
            return [];
        }

        return [.. serverMessage.Split(',')];
    }
    public static void ParseConfigData(List<string> configData)
    {
        int index = 0;

        try
        {
            ConfigDataV1_3 parsedConfigData = new(
                configData[index++], // prestigeMultiplier
                configData[index++], // statSynergyMultiplier
                configData[index++], // maxPlayerLevel
                configData[index++], // maxLegacyLevel
                configData[index++], // maxExpertiseLevel
                configData[index++], // maxFamiliarLevel
                configData[index++], // maxProfessionLevel (不再使用，但暂时保留以避免其他修改)
                configData[index++], // extraRecipes
                configData[index++], // primalCost
                string.Join(",", configData.Skip(index).Take(12)), // 合并接下来的12个元素作为 weaponStatValues
                string.Join(",", configData.Skip(index += 12).Take(12)), // 合并再接下来的12个元素作为 bloodStatValues
                string.Join(",", configData.Skip(index += 12)) // 合并所有剩余元素作为 classStatSynergies
            );

            _prestigeStatMultiplier = parsedConfigData.PrestigeStatMultiplier;
            _classStatMultiplier = parsedConfigData.ClassStatMultiplier;

            _experienceMaxLevel = parsedConfigData.MaxPlayerLevel;
            _legacyMaxLevel = parsedConfigData.MaxLegacyLevel;
            _expertiseMaxLevel = parsedConfigData.MaxExpertiseLevel;
            _familiarMaxLevel = parsedConfigData.MaxFamiliarLevel;
            _extraRecipes = parsedConfigData.ExtraRecipes;
            _primalCost = new PrefabGUID(parsedConfigData.PrimalCost);

            _weaponStatValues = parsedConfigData.WeaponStatValues;
            _bloodStatValues = parsedConfigData.BloodStatValues;

            _classStatSynergies = parsedConfigData.ClassStatSynergies;

            try
            {
                if (_extraRecipes) Recipes.ModifyRecipes();
            }
            catch (Exception ex)
            {
                Core.Log.LogWarning($"修改配方失败：{ex}");
            }
        }
        catch (Exception ex)
        {
            Core.Log.LogWarning($"解析配置数据失败：{ex}");
        }
    }
    /// <summary>
    /// 解析来自服务器的玩家数据字符串列表，并更新相关的静态字段。
    /// 此方法按顺序从输入列表中提取数据，实例化相应的数据类，
    /// 然后使用这些实例中的值更新存储玩家状态的静态字段。
    /// </summary>
    /// <param name="playerData">
    /// 包含玩家数据的字符串列表。列表中的每个元素或元素序列
    /// 对应一个特定的玩家数据点（例如，经验值、传承、专精等）。
    /// 数据的顺序至关重要，必须与解析逻辑的预期顺序一致。
    /// </param>
    public static void ParsePlayerData(List<string> playerData)
    {
        int index = 0; // 初始化索引，用于追踪 playerData 列表中的当前位置。

        // 解析经验数据
        // playerData[index++] -> 经验百分比
        // playerData[index++] -> 等级
        // playerData[index++] -> 声望
        // playerData[index++] -> 玩家职业
        ExperienceData experienceData = new(playerData[index++], playerData[index++], playerData[index++], playerData[index++]);

        // 解析传承数据
        // playerData[index++] -> 传承进度百分比
        // playerData[index++] -> 传承等级
        // playerData[index++] -> 传承声望
        // playerData[index++] -> 传承类型
        // playerData[index++] -> 传承奖励属性
        LegacyData legacyData = new(playerData[index++], playerData[index++], playerData[index++], playerData[index++], playerData[index++]);

        // 解析专精数据
        // playerData[index++] -> 专精进度百分比
        // playerData[index++] -> 专精等级
        // playerData[index++] -> 专精声望
        // playerData[index++] -> 专精类型
        // playerData[index++] -> 专精奖励属性
        ExpertiseData expertiseData = new(playerData[index++], playerData[index++], playerData[index++], playerData[index++], playerData[index++]);

        // 解析伙伴数据
        // playerData[index++] -> 伙伴进度百分比
        // playerData[index++] -> 伙伴等级
        // playerData[index++] -> 伙伴声望
        // playerData[index++] -> 伙伴名称
        // playerData[index++] -> 伙伴属性
        FamiliarData familiarData = new(playerData[index++], playerData[index++], playerData[index++], playerData[index++], playerData[index++]);

        // 解析专业数据
        // 传入16个参数，每2个参数代表一个专业的进度和等级
        ProfessionData professionData = new(
            playerData[index++], playerData[index++], // 附魔进度, 附魔等级
            playerData[index++], playerData[index++], // 炼金进度, 炼金等级
            playerData[index++], playerData[index++], // 草药学进度, 草药学等级
            playerData[index++], playerData[index++], // 锻造进度, 锻造等级
            playerData[index++], playerData[index++], // 裁缝进度, 裁缝等级
            playerData[index++], playerData[index++], // 伐木进度, 伐木等级
            playerData[index++], playerData[index++], // 采矿进度, 采矿等级
            playerData[index++], playerData[index++]  // 钓鱼进度, 钓鱼等级
        );

        // 解析每日任务数据
        // playerData[index++] -> 任务目标类型
        // playerData[index++] -> 任务进度
        // playerData[index++] -> 任务目标数量
        // playerData[index++] -> 任务目标描述
        // playerData[index++] -> 是否为V型血目标
        QuestData dailyQuestData = new(playerData[index++], playerData[index++], playerData[index++], playerData[index++], playerData[index++]);

        // 解析每周任务数据
        // playerData[index++] -> 任务目标类型
        // playerData[index++] -> 任务进度
        // playerData[index++] -> 任务目标数量
        // playerData[index++] -> 任务目标描述
        // playerData[index++] -> 是否为V型血目标
        QuestData weeklyQuestData = new(playerData[index++], playerData[index++], playerData[index++], playerData[index++], playerData[index++]);

        // 更新经验相关的静态字段
        _experienceProgress = experienceData.Progress; // 玩家当前等级的经验进度
        _experienceLevel = experienceData.Level;       // 玩家当前等级
        _experiencePrestige = experienceData.Prestige; // 玩家声望等级
        _classType = experienceData.Class;             // 玩家职业

        // 更新传承相关的静态字段
        _legacyProgress = legacyData.Progress;         // 传承当前等级的进度
        _legacyLevel = legacyData.Level;               // 传承等级
        _legacyPrestige = legacyData.Prestige;         // 传承声望
        _legacyType = legacyData.LegacyType;           // 传承类型
        _legacyBonusStats = legacyData.BonusStats;     // 传承提供的额外属性

        // 更新专精相关的静态字段
        _expertiseProgress = expertiseData.Progress;     // 专精当前等级的进度
        _expertiseLevel = expertiseData.Level;           // 专精等级
        _expertisePrestige = expertiseData.Prestige;     // 专精声望
        _expertiseType = expertiseData.ExpertiseType;    // 专精类型
        _expertiseBonusStats = expertiseData.BonusStats; // 专精提供的额外属性

        // 更新伙伴相关的静态字段
        _familiarProgress = familiarData.Progress;     // 伙伴当前等级的进度
        _familiarLevel = familiarData.Level;           // 伙伴等级
        _familiarPrestige = familiarData.Prestige;     // 伙伴声望
        _familiarName = familiarData.FamiliarName;     // 伙伴名称
        _familiarStats = familiarData.FamiliarStats;   // 伙伴属性

        // 更新专业相关的静态字段
        _enchantingProgress = professionData.EnchantingProgress; // 附魔进度
        _enchantingLevel = professionData.EnchantingLevel;       // 附魔等级
        _alchemyProgress = professionData.AlchemyProgress;       // 炼金进度
        _alchemyLevel = professionData.AlchemyLevel;             // 炼金等级
        _harvestingProgress = professionData.HarvestingProgress; // 草药学进度
        _harvestingLevel = professionData.HarvestingLevel;       // 草药学等级
        _blacksmithingProgress = professionData.BlacksmithingProgress; // 锻造进度
        _blacksmithingLevel = professionData.BlacksmithingLevel;       // 锻造等级
        _tailoringProgress = professionData.TailoringProgress;   // 裁缝进度
        _tailoringLevel = professionData.TailoringLevel;         // 裁缝等级
        _woodcuttingProgress = professionData.WoodcuttingProgress; // 伐木进度
        _woodcuttingLevel = professionData.WoodcuttingLevel;       // 伐木等级
        _miningProgress = professionData.MiningProgress;         // 采矿进度
        _miningLevel = professionData.MiningLevel;               // 采矿等级
        _fishingProgress = professionData.FishingProgress;       // 钓鱼进度
        _fishingLevel = professionData.FishingLevel;             // 钓鱼等级

        // 更新每日任务相关的静态字段
        _dailyTargetType = dailyQuestData.TargetType; // 每日任务的目标类型
        _dailyProgress = dailyQuestData.Progress;     // 每日任务的当前进度
        _dailyGoal = dailyQuestData.Goal;             // 每日任务的目标数量
        _dailyTarget = dailyQuestData.Target;         // 每日任务的目标描述
        _dailyVBlood = dailyQuestData.IsVBlood;       // 每日任务目标是否为V型血

        // 更新每周任务相关的静态字段
        _weeklyTargetType = weeklyQuestData.TargetType; // 每周任务的目标类型
        _weeklyProgress = weeklyQuestData.Progress;     // 每周任务的当前进度
        _weeklyGoal = weeklyQuestData.Goal;             // 每周任务的目标数量
        _weeklyTarget = weeklyQuestData.Target;         // 每周任务的目标描述
        _weeklyVBlood = weeklyQuestData.IsVBlood;       // 每周任务目标是否为V型血

        // 解析并更新位移技能数据
        // playerData[index] -> 位移技能索引 (注意：这里没有 index++，因为它是列表中的最后一个元素)
        ShiftSpellData shiftSpellData = new(playerData[index]);
        _shiftSpellIndex = shiftSpellData.ShiftSpellIndex; // 玩家选择的位移技能索引
    }

}