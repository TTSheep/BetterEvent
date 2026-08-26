using BetterEvent.Utils;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BetterEvent.Patch
{
    /// <summary>
    /// 优化原版“旺达百货商店”事件：将“离开”替换为免费获得两件普通遗物。
    /// </summary>
    [HarmonyPatch(typeof(WelcomeToWongos), "GenerateInitialOptions")]
    public static class WelcomeToWongosPatch
    {
        private const string OriginalLeaveOptionKey =
            "WELCOME_TO_WONGOS.pages.INITIAL.options.LEAVE";

        private const string FreeShoppingOptionKey =
            "WELCOME_TO_WONGOS.pages.INITIAL.options.FREE_SHOPPING";

        private const string FreeShoppingDescriptionKey =
            "WELCOME_TO_WONGOS.pages.FREE_SHOPPING.description";

        /// <summary>
        /// 仅替换原版离开选项，保持所有购买及锁定选项的原始顺序和逻辑。
        /// </summary>
        [HarmonyPostfix]
        private static void Postfix(
            WelcomeToWongos __instance,
            ref IReadOnlyList<EventOption> __result)
        {
            List<EventOption> options = new(__result);
            int leaveIndex = options.FindIndex(option =>
                option.TextKey == OriginalLeaveOptionKey);

            if (leaveIndex < 0)
            {
                return;
            }

            options[leaveIndex] = new EventOption(
                __instance,
                () => FreeShopping(__instance),
                FreeShoppingOptionKey);

            __result = options;
        }

        /// <summary>
        /// 沿用原版特价区的普通遗物抽取规则，连续从玩家遗物池抽取并获得两件遗物。
        /// </summary>
        private static async Task FreeShopping(WelcomeToWongos wongos)
        {
            for (int i = 0; i < 2; i++)
            {
                RelicModel relic = RelicFactory.PullNextRelicFromFront(
                    wongos.Owner,
                    RelicRarity.Common,
                    candidate => candidate.IsAllowedInShops);

                await RelicCmd.Obtain(
                    relic.ToMutable(),
                    wongos.Owner);
            }

            EventAccess.Finish(wongos, FreeShoppingDescriptionKey);
        }
    }
}
