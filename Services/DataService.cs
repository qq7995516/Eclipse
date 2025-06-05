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
        public bool IsVBlood { get; set; } = bool.Parse(isVBlood);
    }
    public class FamiliarData(string percent, string level, string prestige, string familiarName, string familiarStats)
    {
        public float Progress { get; set; } = float.Parse(percent, CultureInfo.InvariantCulture) / 100f;
        public int Level { get; set; } = int.TryParse(level, out int parsedLevel) && parsedLevel > 0 ? parsedLevel : 1;
        public int Prestige { get; set; } = int.Parse(prestige, CultureInfo.InvariantCulture);
        public string FamiliarName { get; set; } = !string.IsNullOrEmpty(familiarName) ? familiarName : "Familiar";
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
                configData[index++], // maxProfessionLevel no longer used and merits getting cut but that necessitates enough other changes leaving alone for the moment
                configData[index++], // extraRecipes
                configData[index++], // primalCost
                string.Join(",", configData.Skip(index).Take(12)), // Combine the next 11 elements for weaponStatValues
                string.Join(",", configData.Skip(index += 12).Take(12)), // Combine the following 11 elements for bloodStatValues
                string.Join(",", configData.Skip(index += 12)) // Combine all remaining elements for classStatSynergies
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
                Core.Log.LogWarning($"Failed to modify recipes: {ex}");
            }
        }
        catch (Exception ex)
        {
            Core.Log.LogWarning($"Failed to parse config data: {ex}");
        }
    }
    public static void ParsePlayerData(List<string> playerData)
    {
        int index = 0;

        ExperienceData experienceData = new(playerData[index++], playerData[index++], playerData[index++], playerData[index++]);
        LegacyData legacyData = new(playerData[index++], playerData[index++], playerData[index++], playerData[index++], playerData[index++]);
        ExpertiseData expertiseData = new(playerData[index++], playerData[index++], playerData[index++], playerData[index++], playerData[index++]);
        FamiliarData familiarData = new(playerData[index++], playerData[index++], playerData[index++], playerData[index++], playerData[index++]);
        ProfessionData professionData = new(playerData[index++], playerData[index++], playerData[index++], playerData[index++], playerData[index++], playerData[index++], playerData[index++], playerData[index++], playerData[index++], playerData[index++], playerData[index++], playerData[index++], playerData[index++], playerData[index++], playerData[index++], playerData[index++]);
        QuestData dailyQuestData = new(playerData[index++], playerData[index++], playerData[index++], playerData[index++], playerData[index++]);
        QuestData weeklyQuestData = new(playerData[index++], playerData[index++], playerData[index++], playerData[index++], playerData[index++]);

        _experienceProgress = experienceData.Progress;
        _experienceLevel = experienceData.Level;
        _experiencePrestige = experienceData.Prestige;
        _classType = experienceData.Class;

        _legacyProgress = legacyData.Progress;
        _legacyLevel = legacyData.Level;
        _legacyPrestige = legacyData.Prestige;
        _legacyType = legacyData.LegacyType;
        _legacyBonusStats = legacyData.BonusStats;

        _expertiseProgress = expertiseData.Progress;
        _expertiseLevel = expertiseData.Level;
        _expertisePrestige = expertiseData.Prestige;
        _expertiseType = expertiseData.ExpertiseType;
        _expertiseBonusStats = expertiseData.BonusStats;

        _familiarProgress = familiarData.Progress;
        _familiarLevel = familiarData.Level;
        _familiarPrestige = familiarData.Prestige;
        _familiarName = familiarData.FamiliarName;
        _familiarStats = familiarData.FamiliarStats;

        _enchantingProgress = professionData.EnchantingProgress;
        _enchantingLevel = professionData.EnchantingLevel;
        _alchemyProgress = professionData.AlchemyProgress;
        _alchemyLevel = professionData.AlchemyLevel;
        _harvestingProgress = professionData.HarvestingProgress;
        _harvestingLevel = professionData.HarvestingLevel;
        _blacksmithingProgress = professionData.BlacksmithingProgress;
        _blacksmithingLevel = professionData.BlacksmithingLevel;
        _tailoringProgress = professionData.TailoringProgress;
        _tailoringLevel = professionData.TailoringLevel;
        _woodcuttingProgress = professionData.WoodcuttingProgress;
        _woodcuttingLevel = professionData.WoodcuttingLevel;
        _miningProgress = professionData.MiningProgress;
        _miningLevel = professionData.MiningLevel;
        _fishingProgress = professionData.FishingProgress;
        _fishingLevel = professionData.FishingLevel;

        _dailyTargetType = dailyQuestData.TargetType;
        _dailyProgress = dailyQuestData.Progress;
        _dailyGoal = dailyQuestData.Goal;
        _dailyTarget = dailyQuestData.Target;
        _dailyVBlood = dailyQuestData.IsVBlood;

        _weeklyTargetType = weeklyQuestData.TargetType;
        _weeklyProgress = weeklyQuestData.Progress;
        _weeklyGoal = weeklyQuestData.Goal;
        _weeklyTarget = weeklyQuestData.Target;
        _weeklyVBlood = weeklyQuestData.IsVBlood;

        ShiftSpellData shiftSpellData = new(playerData[index]);
        _shiftSpellIndex = shiftSpellData.ShiftSpellIndex;
    }
}