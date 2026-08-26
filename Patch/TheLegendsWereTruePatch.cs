using BetterEvent.Utils;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models.Events;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BetterEvent.Patch
{
    /// <summary>
    /// 优化原版“传说是真的”事件：追加“顺走地图并卖了”选项。
    /// </summary>
    [HarmonyPatch(typeof(TheLegendsWereTrue), "GenerateInitialOptions")]
    public static class TheLegendsWereTruePatch
    {
        private const string StealAndSellOptionKey =
            "THE_LEGENDS_WERE_TRUE.pages.INITIAL.options.STEAL_AND_SELL";

        private const string StealAndSellDescriptionKey =
            "THE_LEGENDS_WERE_TRUE.pages.STEAL_AND_SELL.description";

        /// <summary>
        /// 保留原版选项，并在列表末尾追加出售地图的选项。
        /// </summary>
        [HarmonyPostfix]
        private static void Postfix(
            TheLegendsWereTrue __instance,
            ref IReadOnlyList<EventOption> __result)
        {
            List<EventOption> options = new(__result)
            {
                new EventOption(
                    __instance,
                    () => StealAndSell(__instance),
                    StealAndSellOptionKey)
            };

            __result = options;
        }

        /// <summary>
        /// 顺走藏宝图并出售，获得 100 金币后结束事件。
        /// </summary>
        private static async Task StealAndSell(
            TheLegendsWereTrue theLegendsWereTrue)
        {
            await PlayerCmd.GainGold(
                100m,
                theLegendsWereTrue.Owner,
                false);

            EventAccess.Finish(
                theLegendsWereTrue,
                StealAndSellDescriptionKey);
        }
    }
}
