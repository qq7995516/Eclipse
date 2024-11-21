using Stunlock.Core;
using System.Globalization;
using UnityEngine;
using UnityEngine.InputSystem.Utilities;

namespace Eclipse.Services;

internal static class DataService
{
    public enum TargetType
    {
        Kill,
        Craft,
        Gather
    }
    public enum Profession
    {
        Enchanting,
        Alchemy,
        Harvesting,
        Blacksmithing,
        Tailoring,
        Woodcutting,
        Mining,
        Fishing
    }
    public enum BloodType
    {
        Worker,
        Warrior,
        Scholar,
        Rogue,
        Mutant,
        VBlood,
        Frailed,
        GateBoss,
        Draculin,
        Immortal,
        Creature,
        Brute
    }
    public enum WeaponType
    {
        Sword,
        Axe,
        Mace,
        Spear,
        Crossbow,
        GreatSword,
        Slashers,
        Pistols,
        Reaper,
        Longbow,
        Whip,
        Unarmed,
        FishingPole
    }

    public static Dictionary<WeaponStatType, float> WeaponStatValues = [];
    public enum WeaponStatType
    {
        None, 
        MaxHealth,
        MovementSpeed,
        PrimaryAttackSpeed,
        PhysicalLifeLeech,
        SpellLifeLeech,
        PrimaryLifeLeech,
        PhysicalPower,
        SpellPower,
        PhysicalCritChance,
        PhysicalCritDamage,
        SpellCritChance, 
        SpellCritDamage 
    }

    public static readonly Dictionary<WeaponStatType, string> WeaponStatTypeAbbreviations = new()
    {
        { WeaponStatType.MaxHealth, "HP" },
        { WeaponStatType.MovementSpeed, "MS" },
        { WeaponStatType.PrimaryAttackSpeed, "PAS" },
        { WeaponStatType.PhysicalLifeLeech, "PLL" },
        { WeaponStatType.SpellLifeLeech, "SLL" },
        { WeaponStatType.PrimaryLifeLeech, "PLL" },
        { WeaponStatType.PhysicalPower, "PP" },
        { WeaponStatType.SpellPower, "SP" },
        { WeaponStatType.PhysicalCritChance, "PCC" },
        { WeaponStatType.PhysicalCritDamage, "PCD" },
        { WeaponStatType.SpellCritChance, "SCC" },
        { WeaponStatType.SpellCritDamage, "SCD" }
    };

    public static readonly Dictionary<string, string> WeaponStatStringAbbreviations = new()
    {
        { "MaxHealth", "HP" },
        { "MovementSpeed", "MS" },
        { "PrimaryAttackSpeed", "PAS" },
        { "PhysicalLifeLeech", "PLL" },
        { "SpellLifeLeech", "SLL" },
        { "PrimaryLifeLeech", "PLL" },
        { "PhysicalPower", "PP" },
        { "SpellPower", "SP" },
        { "PhysicalCritChance", "PCC" },
        { "PhysicalCritDamage", "PCD" },
        { "SpellCritChance", "SCC" },
        { "SpellCritDamage", "SCD" }
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
        { WeaponStatType.PhysicalCritChance, "percentage" },
        { WeaponStatType.PhysicalCritDamage, "percentage" },
        { WeaponStatType.SpellCritChance, "percentage" },
        { WeaponStatType.SpellCritDamage, "percentage" }
    };

    public static Dictionary<BloodStatType, float> BloodStatValues = [];
    public enum BloodStatType
    {
        None, // 0
        HealingReceived, // 0
        DamageReduction, // 1
        PhysicalResistance, // 2
        SpellResistance, // 3
        ResourceYield, // 4
        CCReduction, // 5
        SpellCooldownRecoveryRate, // 6
        WeaponCooldownRecoveryRate, // 7
        UltimateCooldownRecoveryRate, // 8
        MinionDamage, // 9
        ShieldAbsorb, // 10
        BloodEfficiency // 11
    }

    public static readonly Dictionary<BloodStatType, string> BloodStatTypeAbbreviations = new()
    {
        { BloodStatType.HealingReceived, "HR" },
        { BloodStatType.DamageReduction, "DR" },
        { BloodStatType.PhysicalResistance, "PR" },
        { BloodStatType.SpellResistance, "SR" },
        { BloodStatType.ResourceYield, "RY" },
        { BloodStatType.CCReduction, "CCR" },
        { BloodStatType.SpellCooldownRecoveryRate, "SCR" },
        { BloodStatType.WeaponCooldownRecoveryRate, "WCR" },
        { BloodStatType.UltimateCooldownRecoveryRate, "UCR" },
        { BloodStatType.MinionDamage, "MD" },
        { BloodStatType.ShieldAbsorb, "SA" },
        { BloodStatType.BloodEfficiency, "BE" }
    };

    public static readonly Dictionary<string, string> BloodStatStringAbbreviations = new()
    {
        { "HealingReceived", "HR" },
        { "DamageReduction", "DR" },
        { "PhysicalResistance", "PR" },
        { "SpellResistance", "SR" },
        { "ResourceYield", "RY" },
        { "CCReduction", "CCR" },
        { "SpellCooldownRecoveryRate", "SCR" },
        { "WeaponCooldownRecoveryRate", "WCR" },
        { "UltimateCooldownRecoveryRate", "UCR" },
        { "MinionDamage", "MD" },
        { "ShieldAbsorb", "SA" },
        { "BloodEfficiency", "BE" }
    };

    public static Dictionary<FamiliarStatType, float> FamiliarStatValues = [];
    public enum FamiliarStatType
    {
        MaxHealth,
        PhysicalPower,
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
        { Profession.Enchanting,    new Color(0.494f, 0.133f, 0.808f) },
        { Profession.Alchemy,       new Color(0.071f, 0.831f, 0.635f) },
        { Profession.Harvesting,    new Color(0.0f,    0.502f, 0.0f) },
        { Profession.Blacksmithing, new Color(0.208f, 0.212f, 0.255f) },
        { Profession.Tailoring,     new Color(0.976f, 0.871f, 0.741f) },
        { Profession.Woodcutting,   new Color(0.647f, 0.165f, 0.165f) },
        { Profession.Mining,        new Color(0.502f, 0.502f, 0.502f) },
        { Profession.Fishing,       new Color(0.0f, 0.7f, 0.9f) }
    };
    internal class ProfessionData(string enchantingProgress, string enchantingLevel, string alchemyProgress, string alchemyLevel, 
        string harvestingProgress, string harvestingLevel, string blacksmithingProgress, string blacksmithingLevel, 
        string tailoringProgress, string tailoringLevel, string woodcuttingProgress, string woodcuttingLevel, string miningProgress, 
        string miningLevel, string fishingProgress, string fishingLevel)
    {
        public float EnchantingProgress { get; set; } = float.Parse(enchantingProgress, CultureInfo.InvariantCulture) / 100f;
        public int EnchantingLevel { get; set; } = int.Parse(enchantingLevel);
        public float AlchemyProgress { get; set; } = float.Parse(alchemyProgress, CultureInfo.InvariantCulture) / 100f;
        public int AlchemyLevel { get; set; } = int.Parse(alchemyLevel);
        public float HarvestingProgress { get; set; } = float.Parse(harvestingProgress, CultureInfo.InvariantCulture) / 100f;
        public int HarvestingLevel { get; set; } = int.Parse(harvestingLevel);
        public float BlacksmithingProgress { get; set; } = float.Parse(blacksmithingProgress, CultureInfo.InvariantCulture) / 100f;
        public int BlacksmithingLevel { get; set; } = int.Parse(blacksmithingLevel);
        public float TailoringProgress { get; set; } = float.Parse(tailoringProgress, CultureInfo.InvariantCulture) / 100f;
        public int TailoringLevel { get; set; } = int.Parse(tailoringLevel);
        public float WoodcuttingProgress { get; set; } = float.Parse(woodcuttingProgress, CultureInfo.InvariantCulture) / 100f;
        public int WoodcuttingLevel { get; set; } = int.Parse(woodcuttingLevel);
        public float MiningProgress { get; set; } = float.Parse(miningProgress, CultureInfo.InvariantCulture) / 100f;
        public int MiningLevel { get; set; } = int.Parse(miningLevel);
        public float FishingProgress { get; set; } = float.Parse(fishingProgress, CultureInfo.InvariantCulture) / 100f;
        public int FishingLevel { get; set; } = int.Parse(fishingLevel);
    }
    public enum PlayerClass
    {
        None,
        BloodKnight,
        DemonHunter,
        VampireLord,
        ShadowBlade,
        ArcaneSorcerer,
        DeathMage
    }

    public static Dictionary<PlayerClass, (List<WeaponStatType> WeaponStats, List<BloodStatType> BloodStats)> ClassStatSynergies = [];

    public static float PrestigeStatMultiplier;
    public static float ClassStatMultiplier;
    internal class ExperienceData(string percent, string level, string prestige, string playerClass)
    {
        public float Progress { get; set; } = float.Parse(percent, CultureInfo.InvariantCulture) / 100f;
        public int Level { get; set; } = int.Parse(level);
        public int Prestige { get; set; } = int.Parse(prestige);
        public PlayerClass Class { get; set; } = (PlayerClass)int.Parse(playerClass);
    }
    internal class LegacyData(string percent, string level, string prestige, string legacyType, string bonusStats) : ExperienceData(percent, level, prestige, legacyType)
    {
        public string LegacyType { get; set; } = ((BloodType)int.Parse(legacyType)).ToString();
        public List<string> BonusStats { get; set; } = Enumerable.Range(0, bonusStats.Length / 2).Select(i => ((BloodStatType)int.Parse(bonusStats.Substring(i * 2, 2))).ToString()).ToList();
    }
    internal class ExpertiseData(string percent, string level, string prestige, string expertiseType, string bonusStats) : ExperienceData(percent, level, prestige, expertiseType)
    {
        public string ExpertiseType { get; set; } = ((WeaponType)int.Parse(expertiseType)).ToString();
        public List<string> BonusStats { get; set; } = Enumerable.Range(0, bonusStats.Length / 2).Select(i => ((WeaponStatType)int.Parse(bonusStats.Substring(i * 2, 2))).ToString()).ToList();
    }
    internal class QuestData(string type, string progress, string goal, string target, string isVBlood)
    {
        public TargetType TargetType { get; set; } = (TargetType)int.Parse(type);
        public int Progress { get; set; } = int.Parse(progress);
        public int Goal { get; set; } = int.Parse(goal);
        public string Target { get; set; } = target;
        public bool IsVBlood { get; set; } = bool.Parse(isVBlood);
    }
    internal class FamiliarData(string percent, string level, string prestige, string familiarName, string familiarStats)
    {
        public float Progress { get; set; } = float.Parse(percent, CultureInfo.InvariantCulture) / 100f;
        public int Level { get; set; } = int.TryParse(level, out int parsedLevel) && parsedLevel > 0 ? parsedLevel : 1;
        public int Prestige { get; set; } = int.Parse(prestige);
        public string FamiliarName { get; set; } = !string.IsNullOrEmpty(familiarName) ? familiarName : "Familiar";
        public List<string> FamiliarStats { get; set; } = !string.IsNullOrEmpty(familiarStats) ? new List<string> { familiarStats[..4], familiarStats[4..7], familiarStats[7..] }
                .Select(stat => int.Parse(stat).ToString())
                .ToList() : ["", "", ""];
    }
    internal class AbilityData(string prefabHash, string cooldown, string charges, string chargesCooldown)
    {
        public PrefabGUID AbilityPrefabGUID { get; set; } = int.TryParse(prefabHash, out int parsedHash) ? new PrefabGUID(parsedHash) : PrefabGUID.Empty;
        public float Cooldown { get; set; } = float.TryParse(cooldown, NumberStyles.Integer, CultureInfo.InvariantCulture, out float parsedCooldown) ? parsedCooldown : 0f;
        public int Charges { get; set; } = int.TryParse(charges, out int parsedCharges) ? parsedCharges : 0;
        public float ChargesCooldown { get; set; } = float.TryParse(chargesCooldown, NumberStyles.Integer, CultureInfo.InvariantCulture, out float parsedChargesCooldown) ? parsedChargesCooldown : 0f;
    }
    internal class ConfigData
    {
        public float PrestigeStatMultiplier;

        public float ClassStatMultiplier;

        public int MaxPlayerLevel;

        public int MaxLegacyLevel;

        public int MaxExpertiseLevel;

        public int MaxFamiliarLevel;

        public int MaxProfessionLevel;

        public Dictionary<WeaponStatType, float> WeaponStatValues;

        public Dictionary<BloodStatType, float> BloodStatValues;

        public Dictionary<PlayerClass, (List<WeaponStatType> WeaponStats, List<BloodStatType> bloodStats)> ClassStatSynergies;
        public ConfigData(string prestigeMultiplier, string statSynergyMultiplier, string maxPlayerLevel, string maxLegacyLevel, string maxExpertiseLevel, string maxFamiliarLevel, string maxProfessionLevel, string weaponStatValues, string bloodStatValues, string classStatSynergies)
        {
            //Core.Log.LogInfo($"ConfigData: {prestigeMultiplier}, {statSynergyMultiplier}, {weaponStatValues}, {bloodStatValues}, {classStatSynergies}");

            PrestigeStatMultiplier = float.Parse(prestigeMultiplier, CultureInfo.InvariantCulture);
            ClassStatMultiplier = float.Parse(statSynergyMultiplier, CultureInfo.InvariantCulture);

            MaxPlayerLevel = int.Parse(maxPlayerLevel);
            MaxLegacyLevel = int.Parse(maxLegacyLevel);
            MaxExpertiseLevel = int.Parse(maxExpertiseLevel);
            MaxFamiliarLevel = int.Parse(maxFamiliarLevel);
            MaxProfessionLevel = int.Parse(maxProfessionLevel);

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
                g => (PlayerClass)int.Parse(g.ElementAt(0).Value),
                g => (
                    Enumerable.Range(0, g.ElementAt(1).Value.Length / 2)
                        .Select(j => (WeaponStatType)int.Parse(g.ElementAt(1).Value.Substring(j * 2, 2)))
                        .ToList(),
                    Enumerable.Range(0, g.ElementAt(2).Value.Length / 2)
                        .Select(j => (BloodStatType)int.Parse(g.ElementAt(2).Value.Substring(j * 2, 2)))
                        .ToList()
                )
            );
        }
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
            configData[index++], // maxPlayerLevel
            configData[index++], // maxLegacyLevel
            configData[index++], // maxExpertiseLevel
            configData[index++], // maxFamiliarLevel
            configData[index++], // maxProfessionLevel
            string.Join(",", configData.Skip(index).Take(12)), // Combine the next 11 elements for weaponStatValues
            string.Join(",", configData.Skip(index += 12).Take(12)), // Combine the following 11 elements for bloodStatValues
            string.Join(",", configData.Skip(index += 12)) // Combine all remaining elements for classStatSynergies
        );

        PrestigeStatMultiplier = parsedConfigData.PrestigeStatMultiplier;
        ClassStatMultiplier = parsedConfigData.ClassStatMultiplier;

        CanvasService.ExperienceMaxLevel = parsedConfigData.MaxPlayerLevel;
        CanvasService.LegacyMaxLevel = parsedConfigData.MaxLegacyLevel;
        CanvasService.ExpertiseMaxLevel = parsedConfigData.MaxExpertiseLevel;
        CanvasService.FamiliarMaxLevel = parsedConfigData.MaxFamiliarLevel;
        CanvasService.ProfessionMaxLevel = parsedConfigData.MaxProfessionLevel;

        WeaponStatValues = parsedConfigData.WeaponStatValues;

        BloodStatValues = parsedConfigData.BloodStatValues;

        ClassStatSynergies = parsedConfigData.ClassStatSynergies;
    }
    public static void ParsePlayerData(List<string> playerData)
    {
        int index = 0;

        //Core.Log.LogInfo($"PlayerData: {string.Join(",", playerData)}");

        ExperienceData experienceData = new(playerData[index++], playerData[index++], playerData[index++], playerData[index++]);
        LegacyData legacyData = new(playerData[index++], playerData[index++], playerData[index++], playerData[index++], playerData[index++]);
        ExpertiseData expertiseData = new(playerData[index++], playerData[index++], playerData[index++], playerData[index++], playerData[index++]);
        FamiliarData familiarData = new(playerData[index++], playerData[index++], playerData[index++], playerData[index++], playerData[index++]);
        ProfessionData professionData = new(playerData[index++], playerData[index++], playerData[index++], playerData[index++], playerData[index++], playerData[index++], playerData[index++], playerData[index++], playerData[index++], playerData[index++], playerData[index++], playerData[index++], playerData[index++], playerData[index++], playerData[index++], playerData[index++]);
        QuestData dailyQuestData = new(playerData[index++], playerData[index++], playerData[index++], playerData[index++], playerData[index++]);
        QuestData weeklyQuestData = new(playerData[index++], playerData[index++], playerData[index++], playerData[index++], playerData[index]);

        CanvasService.ExperienceProgress = experienceData.Progress;
        CanvasService.ExperienceLevel = experienceData.Level;
        CanvasService.ExperiencePrestige = experienceData.Prestige;
        CanvasService.ClassType = experienceData.Class;

        CanvasService.LegacyProgress = legacyData.Progress;
        CanvasService.LegacyLevel = legacyData.Level;
        CanvasService.LegacyPrestige = legacyData.Prestige;
        CanvasService.LegacyType = legacyData.LegacyType;
        CanvasService.LegacyBonusStats = legacyData.BonusStats;

        CanvasService.ExpertiseProgress = expertiseData.Progress;
        CanvasService.ExpertiseLevel = expertiseData.Level;
        CanvasService.ExpertisePrestige = expertiseData.Prestige;
        CanvasService.ExpertiseType = expertiseData.ExpertiseType;
        CanvasService.ExpertiseBonusStats = expertiseData.BonusStats;

        CanvasService.FamiliarProgress = familiarData.Progress;
        CanvasService.FamiliarLevel = familiarData.Level;
        CanvasService.FamiliarPrestige = familiarData.Prestige;
        CanvasService.FamiliarName = familiarData.FamiliarName;
        CanvasService.FamiliarStats = familiarData.FamiliarStats;

        CanvasService.EnchantingProgress = professionData.EnchantingProgress;
        CanvasService.EnchantingLevel = professionData.EnchantingLevel;
        CanvasService.AlchemyProgress = professionData.AlchemyProgress;
        CanvasService.AlchemyLevel = professionData.AlchemyLevel;
        CanvasService.HarvestingProgress = professionData.HarvestingProgress;
        CanvasService.HarvestingLevel = professionData.HarvestingLevel;
        CanvasService.BlacksmithingProgress = professionData.BlacksmithingProgress;
        CanvasService.BlacksmithingLevel = professionData.BlacksmithingLevel;
        CanvasService.TailoringProgress = professionData.TailoringProgress;
        CanvasService.TailoringLevel = professionData.TailoringLevel;
        CanvasService.WoodcuttingProgress = professionData.WoodcuttingProgress;
        CanvasService.WoodcuttingLevel = professionData.WoodcuttingLevel;
        CanvasService.MiningProgress = professionData.MiningProgress;
        CanvasService.MiningLevel = professionData.MiningLevel;
        CanvasService.FishingProgress = professionData.FishingProgress;
        CanvasService.FishingLevel = professionData.FishingLevel;

        CanvasService.DailyTargetType = dailyQuestData.TargetType;
        CanvasService.DailyProgress = dailyQuestData.Progress;
        CanvasService.DailyGoal = dailyQuestData.Goal;
        CanvasService.DailyTarget = dailyQuestData.Target;
        CanvasService.DailyVBlood = dailyQuestData.IsVBlood;

        CanvasService.WeeklyTargetType = weeklyQuestData.TargetType;
        CanvasService.WeeklyProgress = weeklyQuestData.Progress;
        CanvasService.WeeklyGoal = weeklyQuestData.Goal;
        CanvasService.WeeklyTarget = weeklyQuestData.Target;
        CanvasService.WeeklyVBlood = weeklyQuestData.IsVBlood;
    }
    public static void ParseAbilityData(List<string> playerData)
    {
        int index = 0;

        AbilityData abilityData = new(playerData[index++], playerData[index++], playerData[index++], playerData[index]);

        CanvasService.AbilityGroupPrefabGUID = abilityData.AbilityPrefabGUID;
        CanvasService.AbilityGroupPrefabName = abilityData.AbilityPrefabGUID.IsEmpty() ? "" : abilityData.AbilityPrefabGUID.LookupName();
        CanvasService.Cooldown = abilityData.Cooldown;
        CanvasService.Charges = abilityData.Charges;
        CanvasService.ChargesCooldown = abilityData.ChargesCooldown;
    }
}
