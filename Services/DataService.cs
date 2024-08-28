namespace Eclipse.Services;

internal static class DataService
{
    public enum BloodType
    {
        Worker,
        Warrior,
        Scholar,
        Rogue,
        Mutant,
        VBlood,
        None,
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
        None, // 0 not shifting the rest right now :p
        MaxHealth, // 0
        MovementSpeed, // 1
        PrimaryAttackSpeed, // 2
        PhysicalLifeLeech, // 3
        SpellLifeLeech, // 4
        PrimaryLifeLeech, // 5
        PhysicalPower, // 6
        SpellPower, // 7
        PhysicalCritChance, // 8
        PhysicalCritDamage, // 9
        SpellCritChance, // 10
        SpellCritDamage // 11
    }

    public static readonly Dictionary<WeaponStatType, string> WeaponStatAbbreviations = new()
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

    public static readonly Dictionary<BloodStatType, string> BloodStatAbbreviations = new()
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
    public enum PlayerClass // for now subtract 1 when processing this enum, if 0 no class (otherwise 0 would be bloodknight. should probably add a None enum)
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
        public float Progress { get; set; } = float.Parse(percent) / 100f;
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
    internal class QuestData(string progress, string goal, string target)
    {
        public int Progress { get; set; } = int.Parse(progress);
        public int Goal { get; set; } = int.Parse(goal);
        public string Target { get; set; } = target;
    }
    internal class  ConfigData
    {
        public float PrestigeStatMultiplier;

        public float ClassStatMultiplier;

        public Dictionary<WeaponStatType, float> WeaponStatValues;

        public Dictionary<BloodStatType, float> BloodStatValues;

        public Dictionary<PlayerClass, (List<WeaponStatType> WeaponStats, List<BloodStatType> bloodStats)> ClassStatSynergies;

        public ConfigData(string prestigeMultiplier, string statSynergyMultiplier, string weaponStatValues, string bloodStatValues, string classStatSynergies)
        {
            //Core.Log.LogInfo($"ConfigData: {prestigeMultiplier}, {statSynergyMultiplier}, {weaponStatValues}, {bloodStatValues}, {classStatSynergies}");

            PrestigeStatMultiplier = float.Parse(prestigeMultiplier);
            ClassStatMultiplier = float.Parse(statSynergyMultiplier);

            WeaponStatValues = weaponStatValues.Split(',')
            .Select((value, index) => new { Index = index + 1, Value = float.Parse(value) })
            .ToDictionary(x => (WeaponStatType)x.Index, x => x.Value);

            BloodStatValues = bloodStatValues.Split(',')
            .Select((value, index) => new { Index = index + 1, Value = float.Parse(value) })
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
}
