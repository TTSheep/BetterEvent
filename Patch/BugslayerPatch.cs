using BetterEvent.Utils;
using HarmonyLib;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BetterEvent.Patch
{
    /// <summary>
    /// 优化原版“害虫杀手”事件：玩家可选择手中的牌变化为“杀虫剂”或“压扁”；新增选项
    /// </summary>
    [HarmonyPatch]
    public static class BugslayerPatch
    {
        [HarmonyPatch(typeof(Bugslayer), "GenerateInitialOptions")]
        [HarmonyPostfix]
        private static void EventOptionPostfix(Bugslayer __instance, ref IReadOnlyList<EventOption> __result)
        {
            List<EventOption> options = new(__result);
            options.Clear();

            bool hasTransformableCard = CardPile.Get(PileType.Deck, __instance.Owner)
                .Cards.Any(card => card.IsTransformable);

            // 原版通过 OnChosen 为 null 的 EventOption 显示锁定选项及其原因。
            if (hasTransformableCard)
            {
                options.Add(new EventOption(
                    __instance,
                    () => ExterminationFix(__instance),
                    "BUGSLAYER.pages.INITIAL.options.EXTERMINATION", HoverTipFactory.FromCardWithCardHoverTips<Exterminate>()));
                options.Add(new EventOption(
                    __instance,
                    () => SquashFix(__instance),
                    "BUGSLAYER.pages.INITIAL.options.SQUASH", HoverTipFactory.FromCardWithCardHoverTips<Squash>()));
            }
            else
            {
                options.Add(new EventOption(
                    __instance,
                    null,
                    "BUGSLAYER.pages.INITIAL.options.EXTERMINATION_LOCKED",
                    Array.Empty<IHoverTip>()));
                options.Add(new EventOption(
                    __instance,
                    null,
                    "BUGSLAYER.pages.INITIAL.options.SQUASH_LOCKED",
                    Array.Empty<IHoverTip>()));
            }

            options.Add(new EventOption(
                __instance,
                () => Refuse(__instance),
                "BUGSLAYER.pages.INITIAL.options.REFUSE"));
            __result = options;

        }

        private static async Task ExterminationFix(Bugslayer bugslayer)
        {
            CardModel cardModel = (await CardSelectCmd.FromDeckGeneric(bugslayer.Owner, new CardSelectorPrefs(CardSelectorPrefs.TransformSelectionPrompt, 1), (CardModel c) => c.IsTransformable)).FirstOrDefault();
            if (cardModel != null)
            {
                await CardCmd.TransformTo<Exterminate>(cardModel, CardPreviewStyle.EventLayout);
            }
            EventAccess.Finish(
                bugslayer,
                "BUGSLAYER.pages.EXTERMINATION.description");

        }
        private static async Task SquashFix(Bugslayer bugslayer)
        {
            CardModel cardModel = (await CardSelectCmd.FromDeckGeneric(bugslayer.Owner, new CardSelectorPrefs(CardSelectorPrefs.TransformSelectionPrompt, 1), (CardModel c) => c.IsTransformable)).FirstOrDefault();
            if (cardModel != null)
            {
                await CardCmd.TransformTo<Squash>(cardModel, CardPreviewStyle.EventLayout);
            }
            EventAccess.Finish(
                bugslayer,
                "BUGSLAYER.pages.SQUASH.description");
        }
        private static async Task Refuse(Bugslayer bugslayer)
        {
            await PlayerCmd.GainGold(50m, bugslayer.Owner, false);

            EventAccess.Finish(
                bugslayer,
                "BUGSLAYER.pages.REFUSE.description");
        }
    }
}
