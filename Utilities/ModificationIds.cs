using System.ComponentModel;
using UnityEngine;
using static Eclipse.Services.DataService;

namespace Eclipse.Utilities;
internal static class ModificationIds
{
    // 位运算相关的常量是核心逻辑，不能修改
    const int SOURCE_SHIFT = 30;
    const int STAT_SHIFT = 20;
    const int MAX_SOURCE = 0b11;
    const int MAX_STAT = 0x3FF;
    const int MAX_VALUE = 0xFFFFF;

    public enum StatSourceType
    {
        [Description("武器")]
        Weapon = 0,

        [Description("血质")]
        Blood = 1,

        [Description("职业")]
        Class = 2
    }

    public static int GenerateId(int sourceType, int statType, float value)
    {
        int quantizedValue = Mathf.RoundToInt(value * 1000f);
        quantizedValue = Mathf.Clamp(quantizedValue, 0, MAX_VALUE);

        return (sourceType & MAX_SOURCE) << SOURCE_SHIFT |
               (statType & MAX_STAT) << STAT_SHIFT |
               (quantizedValue & MAX_VALUE);
    }

    public static bool TryParseId(int id, out string description)
    {
        int source = (id >> SOURCE_SHIFT) & MAX_SOURCE;
        int stat = (id >> STAT_SHIFT) & MAX_STAT;
        int quantizedValue = id & MAX_VALUE;

        float value = quantizedValue / 1000f;
        // 默认的错误/未知描述
        description = $"未知 (原始ID: {id})";

        switch ((StatSourceType)source)
        {
            case StatSourceType.Weapon:
                if (Enum.IsDefined(typeof(WeaponStatType), stat))
                {
                    // 使用 GetDescription() 来获取枚举的中文描述
                    description = $"武器属性: {((WeaponStatType)stat).GetDescription()}, 值: {value:F3}";
                    return true;
                }
                break;
            case StatSourceType.Blood:
                if (Enum.IsDefined(typeof(BloodStatType), stat))
                {
                    description = $"血质属性: {((BloodStatType)stat).GetDescription()}, 值: {value:F3}";
                    return true;
                }
                break;
            case StatSourceType.Class:
                if (Enum.IsDefined(typeof(WeaponStatType), stat)) // 假设职业使用相同的属性
                {
                    description = $"职业属性: {((WeaponStatType)stat).GetDescription()}, 值: {value:F3}";
                    return true;
                }
                break;
        }

        return false;
    }
}