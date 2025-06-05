using HarmonyLib;
using ProjectM;
using ProjectM.UI;
using Stunlock.Core;
using Unity.Entities;
using UnityEngine;

namespace Eclipse.Patches;

[HarmonyPatch]
internal static class ServantMenuMapperPatch
{
    // 这些是UI上显示的文本，可以安全地汉化
    const string SPELL_POWER = "法术强度";
    const string HUNT_PROFICIENCY = "狩猎专精";

    // 字典的值是富文本字符串，只翻译其中的文本部分，保留<color>标签
    public static readonly Dictionary<PrefabGUID, string> ShinyBuffColorHexMap = new()
    {
        { new(348724578), "<color=#A020F0>混乱</color>" },   // 点燃 紫色
        { new(-1576512627), "<color=#FFD700>风暴</color>" },  // 静电 黄色
        { new(-1246704569), "<color=#FF0000>鲜血</color>" },  // 汲取 红色
        { new(1723455773), "<color=#008080>幻术</color>" },   // 削弱 青色
        { new(27300215), "<color=#00FFFF>冰霜</color>" },     // 冰冷 青色
        { new(-325758519), "<color=#00FF00>邪能</color>" }    // 谴责 绿色
    };

    static ServantInventorySubMenu _familiarServantMenu;
    static Entity _familiarServant;
    static Entity _familiar;

    [HarmonyPatch(typeof(ServantInventorySubMenuMapper), nameof(ServantInventorySubMenuMapper.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(ServantInventorySubMenuMapper __instance)
    {
        if (!Core._initialized) return;
        else if (__instance._Target.HasValue())
        {
            Entity servant = __instance._Target;

            if (servant.Equals(_familiarServant))
            {
                // _familiarServantMenu?.Expertise.ForceSet(SPELL_POWER);
                // _familiarServantMenu?.HuntProficiency.ForceSet(shinyBuff);
            }
            else if (!servant.HasConnectedCoffin())
            {
                _familiarServant = servant;
                _familiarServantMenu = __instance._Menu;

                Entity playerCharacter = __instance.GetLocalPlayer();

                /*
                [警告:   Eclipse] 追随者 - CHAR_ChurchOfLight_Paladin_VBlood PrefabGuid(-740796338)
                [警告:   Eclipse] 追随者增益 - Buff_General_Wounded_Tracker PrefabGuid(224060472)
                [警告:   Eclipse] 追随者增益 - Storm_Vampire_Buff_Static PrefabGuid(-1576512627)
                [警告:   Eclipse] 追随者增益 - AB_Militia_HoundMaster_QuickShot_Buff PrefabGuid(1520432556)
                */

                if (playerCharacter.TryGetBuffer<FollowerBuffer>(out var buffer))
                {
                    foreach (FollowerBuffer follower in buffer)
                    {
                        Entity familiar = follower.Entity.GetEntityOnServer();

                        if (familiar.Exists())
                        {
                            _familiar = familiar;
                            var matchingBuff = ShinyBuffColorHexMap.FirstOrDefault(buff => _familiar.HasBuff(buff.Key));
                            string shinyBuff = matchingBuff.Value ?? "?";
                            // Core.Log.LogWarning($"仆人闪亮增益 - {shinyBuff}|{matchingBuff.Key.GetPrefabName()}|{(_familiar.TryGetBuff(matchingBuff.Key, out Entity buffEntity) ? buffEntity.Read<Buff>().Stacks : -1)}");
                        }
                    }
                }

                // ModifyFamiliarServantMenu(_familiarServantMenu.gameObject);
            }
            else if (servant.Exists())
            {
                _familiarServant = Entity.Null;
                _familiar = Entity.Null;
                // RestoreServantMenu(_familiarServantMenu.gameObject);
            }
        }
    }
    static void ModifyFamiliarServantMenu(GameObject menu)
    {

    }
    static void RestoreServantMenu(GameObject menu)
    {

    }
}