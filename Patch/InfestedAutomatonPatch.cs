using BetterEvent.Utils;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Runs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BetterEvent.Patch
{
    /// <summary>
    /// 优化原版“被寄生的自动机械”事件：
    /// “学习”和“触碰核心”均改为提供一组符合条件的卡牌奖励。
    /// </summary>
    [HarmonyPatch(typeof(InfestedAutomaton), "GenerateInitialOptions")]
    public static class InfestedAutomatonPatch
    {
        private const int RewardOptionCount = 3;

        private const string StudyOptionKey =
            "INFESTED_AUTOMATON.pages.INITIAL.options.STUDY";

        private const string TouchCoreOptionKey =
            "INFESTED_AUTOMATON.pages.INITIAL.options.TOUCH_CORE";

        private const string StudyDescriptionKey =
            "INFESTED_AUTOMATON.pages.STUDY.description";

        private const string TouchCoreDescriptionKey =
            "INFESTED_AUTOMATON.pages.TOUCH_CORE.description";

        /// <summary>
        /// 替换原版两个选项的回调，保留原有标题和后续事件文本。
        /// </summary>
        [HarmonyPostfix]
        private static void Postfix(
            InfestedAutomaton __instance,
            ref IReadOnlyList<EventOption> __result)
        {
            __result =
            [
                new EventOption(
                    __instance,
                    () => Study(__instance),
                    StudyOptionKey),
                new EventOption(
                    __instance,
                    () => TouchCore(__instance),
                    TouchCoreOptionKey)
            ];
        }

        /// <summary>
        /// 提供三张能力牌，玩家从中选择一张作为奖励。
        /// </summary>
        private static async Task Study(InfestedAutomaton infestedAutomaton)
        {
            CardCreationOptions options =
                CardCreationOptions.ForNonCombatWithDefaultOdds(
                    [infestedAutomaton.Owner.Character.CardPool],
                    card => card.Type == CardType.Power);

            await OfferCardReward(infestedAutomaton, options);
            EventAccess.Finish(infestedAutomaton, StudyDescriptionKey);
        }

        /// <summary>
        /// 提供三张基础耗能为零且非 X 费的牌，玩家从中选择一张作为奖励。
        /// </summary>
        private static async Task TouchCore(InfestedAutomaton infestedAutomaton)
        {
            CardCreationOptions options =
                CardCreationOptions.ForNonCombatWithDefaultOdds(
                    [infestedAutomaton.Owner.Character.CardPool],
                    card => card.EnergyCost is not null &&
                            card.EnergyCost.Canonical == 0 &&
                            !card.EnergyCost.CostsX)
                .WithFlags(CardCreationFlags.NoCardPoolModifications);

            await OfferCardReward(infestedAutomaton, options);
            EventAccess.Finish(infestedAutomaton, TouchCoreDescriptionKey);
        }

        /// <summary>
        /// 使用游戏原生卡牌奖励界面展示三张候选牌，并处理选牌与入牌组流程。
        /// </summary>
        private static Task OfferCardReward(
            InfestedAutomaton infestedAutomaton,
            CardCreationOptions options)
        {
            CardReward reward = new(
                options,
                RewardOptionCount,
                infestedAutomaton.Owner,
                null);

            return RewardsCmd.OfferCustom(
                infestedAutomaton.Owner,
                new List<Reward> { reward });
        }

    }
}
