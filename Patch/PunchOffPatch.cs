using BetterEvent.Utils;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Runs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BetterEvent.Patch
{
    /// <summary>
    /// 优化原版“重拳出击”事件：增加一次性夺取药水、金币和卡牌奖励的偷袭选项。
    /// </summary>
    [HarmonyPatch(typeof(PunchOff), "GenerateInitialOptions")]
    public static class PunchOffPatch
    {
        private const string SneakAttackOptionKey =
            "PUNCH_OFF.pages.INITIAL.options.SNEAK_ATTACK";

        private const string SneakAttackDescriptionKey =
            "PUNCH_OFF.pages.SNEAK_ATTACK.description";

        /// <summary>
        /// 保留原版“顺走”和“我能打赢”选项，仅在末尾追加“偷袭”。
        /// </summary>
        [HarmonyPostfix]
        private static void Postfix(
            PunchOff __instance,
            ref IReadOnlyList<EventOption> __result)
        {
            List<EventOption> options = new(__result)
            {
                new EventOption(
                    __instance,
                    () => SneakAttack(__instance),
                    SneakAttackOptionKey)
            };

            __result = options;
        }

        /// <summary>
        /// 按原版事件奖励 API 创建同一组奖励：
        /// 一瓶随机药水、普通战斗标准金币，以及一次当前角色卡池的三选一卡牌奖励。
        /// </summary>
        private static async Task SneakAttack(PunchOff punchOff)
        {
            CardCreationOptions cardOptions =
                CardCreationOptions.ForNonCombatWithDefaultOdds(
                    [punchOff.Owner.Character.CardPool],
                    null);

            List<Reward> rewards =
            [
                new PotionReward(punchOff.Owner),
                CreateNormalCombatGoldReward(punchOff),
                new CardReward(
                    cardOptions,
                    3,
                    punchOff.Owner,
                    null)
            ];

            await RewardsCmd.OfferCustom(punchOff.Owner, rewards);
            EventAccess.Finish(punchOff, SneakAttackDescriptionKey);
        }

        /// <summary>
        /// 复刻 EncounterModel 对普通战斗金币的原版计算：基础区间为 10–20，
        /// “贫困”进阶启用时对上下限应用相同的金币倍率。
        /// </summary>
        private static GoldReward CreateNormalCombatGoldReward(
            PunchOff punchOff)
        {
            int minGold = 10;
            int maxGold = 20;

            if (AscensionHelper.HasAscension(AscensionLevel.Poverty))
            {
                double multiplier =
                    AscensionHelper.PovertyAscensionGoldMultiplier;

                minGold = (int)(minGold * multiplier);
                maxGold = (int)(maxGold * multiplier);
            }

            // 使用区间构造函数，让 GoldReward 沿用原版奖励随机数决定最终数量。
            return new GoldReward(
                minGold,
                maxGold,
                punchOff.Owner,
                false);
        }
    }
}
