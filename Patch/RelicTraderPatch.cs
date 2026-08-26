using BetterEvent.Utils;
using HarmonyLib;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models.Events;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace BetterEvent.Patch
{
    /// <summary>
    /// 优化原版“遗物交换商”事件：增加交易入口，并允许无限重新随机当前交易货物。
    /// </summary>
    [HarmonyPatch(typeof(RelicTrader), "GenerateInitialOptions")]
    public static class RelicTraderPatch
    {
        private const string TradeOptionKey =
            "RELIC_TRADER.pages.INITIAL.options.TRADE";

        private const string LeaveOptionKey =
            "RELIC_TRADER.pages.INITIAL.options.LEAVE";

        private const string RerollOptionKey =
            "RELIC_TRADER.pages.INITIAL.options.REROLL";

        private const string TradingDescriptionKey =
            "RELIC_TRADER.pages.TRADING.description";

        private const string LeaveDescriptionKey =
            "RELIC_TRADER.pages.LEAVE.description";

        // 通过原版方法重新生成交易选项，保留 Top/Middle/Bottom、悬浮提示和 Trade(index) 回调。
        private static readonly MethodInfo GenerateInitialOptionsMethod =
            AccessTools.Method(typeof(RelicTrader), "GenerateInitialOptions");

        [ThreadStatic]
        private static bool _isGeneratingTradeOptions;

        // 原版将双方的三件候选遗物分别缓存在这两个字段中。
        private static readonly FieldInfo OwnedRelicsField = AccessTools.Field(
            typeof(RelicTrader),
            "_ownedRelics");

        private static readonly FieldInfo NewRelicsField = AccessTools.Field(
            typeof(RelicTrader),
            "_newRelics");

        /// <summary>
        /// 首次进入事件时用“交易 / 离去”替换原版交易列表；
        /// 由补丁主动重新调用原版生成方法时，则保留原版交易列表并加入刷新选项。
        /// </summary>
        [HarmonyPostfix]
        private static void Postfix(
            RelicTrader __instance,
            ref IReadOnlyList<EventOption> __result)
        {
            if (!_isGeneratingTradeOptions)
            {
                __result =
                [
                    new EventOption(
                        __instance,
                        () => EnterTrade(__instance),
                        TradeOptionKey),
                    new EventOption(
                        __instance,
                        () => Leave(__instance),
                        LeaveOptionKey)
                ];
                return;
            }

            List<EventOption> options = new(__result);
            EventOption rerollOption = new(
                __instance,
                () => Reroll(__instance),
                RerollOptionKey);

            // 无限刷新不应在事件历史中重复堆积同一选项记录。
            rerollOption.ThatWontSaveToChoiceHistory();

            int proceedIndex = options.FindIndex(option =>
                option.TextKey == "PROCEED");

            if (proceedIndex >= 0)
            {
                options.Insert(proceedIndex, rerollOption);
            }
            else
            {
                options.Add(rerollOption);
            }

            __result = options;
        }

        /// <summary>
        /// 进入新增的第二阶段，并展示原版生成的全部交易选项。
        /// </summary>
        private static Task EnterTrade(RelicTrader relicTrader)
        {
            EventAccess.SetState(
                relicTrader,
                TradingDescriptionKey,
                GenerateTradeOptions(relicTrader));

            return Task.CompletedTask;
        }

        /// <summary>
        /// 调用原版 GenerateInitialOptions；线程标记只用于阻止本补丁再次生成入口阶段。
        /// </summary>
        private static IReadOnlyList<EventOption> GenerateTradeOptions(
            RelicTrader relicTrader)
        {
            _isGeneratingTradeOptions = true;

            try
            {
                return (IReadOnlyList<EventOption>)GenerateInitialOptionsMethod.Invoke(
                    relicTrader,
                    null);
            }
            finally
            {
                _isGeneratingTradeOptions = false;
            }
        }

        /// <summary>
        /// 清除原版缓存后重新执行原版计算：
        /// 待交换遗物会再次使用事件 Rng 稳定洗牌，换入遗物会继续从原版遗物池抽取。
        /// </summary>
        private static Task Reroll(RelicTrader relicTrader)
        {
            OwnedRelicsField.SetValue(relicTrader, null);
            NewRelicsField.SetValue(relicTrader, null);

            relicTrader.CalculateVars();
            EventAccess.SetState(
                relicTrader,
                TradingDescriptionKey,
                GenerateTradeOptions(relicTrader));

            return Task.CompletedTask;
        }

        /// <summary>
        /// 从入口阶段直接结束事件，不生成或执行任何交易回调。
        /// </summary>
        private static Task Leave(RelicTrader relicTrader)
        {
            EventAccess.Finish(relicTrader, LeaveDescriptionKey);
            return Task.CompletedTask;
        }
    }
}
