using BetterEvent.Utils;
using HarmonyLib;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BetterEvent.Patch
{
    /// <summary>
    /// 优化原版“滑脚木桥”事件：自由选择牌删除
    /// </summary>
    [HarmonyPatch]
    public static class SlipperyBridgePatch
    {
        [HarmonyPatch(typeof(SlipperyBridge), "GenerateInitialOptions")]
        [HarmonyPostfix]
        private static void Postfix(
            SlipperyBridge __instance,
            ref IReadOnlyList<EventOption> __result)
        {
            List<EventOption> options = new(__result);
            options.Clear();

            int removableCardCount = CardPile.Get(PileType.Deck, __instance.Owner)
                .Cards.Count(card => card.IsRemovable);

            // 原版移除选牌命令不会替事件决定选项是否可用，因此在生成选项时先做数量保护。
            options.Add(removableCardCount >= 1
                ? new EventOption(
                    __instance,
                    () => OvercomeFix(__instance),
                    "SLIPPERY_BRIDGE.pages.INITIAL.options.OVERCOMEFIX")
                : new EventOption(
                    __instance,
                    null,
                    "SLIPPERY_BRIDGE.pages.INITIAL.options.OVERCOMEFIX_LOCKED",
                    Array.Empty<IHoverTip>()));
            options.Add(removableCardCount >= 2
                ? new EventOption(
                    __instance,
                    () => HoldOnEndFix(__instance),
                    "SLIPPERY_BRIDGE.pages.INITIAL.options.HOLD_ON_ENDFIX")
                    // 使用原版危险提示 API：当前生命值不高于15时，选项显示致命红光。
                    .ThatDoesDamage(15m)
                : new EventOption(
                    __instance,
                    null,
                    "SLIPPERY_BRIDGE.pages.INITIAL.options.HOLD_ON_ENDFIX_LOCKED",
                    Array.Empty<IHoverTip>()));
            options.Add(new EventOption(
                __instance,
                () => ReturnNow(__instance),
                "SLIPPERY_BRIDGE.pages.INITIAL.options.RETURN_NOW"));
            __result = options;
        }
        private static async Task OvercomeFix(SlipperyBridge slipperyBridge)
        {
            List<CardModel> cards = (await CardSelectCmd.FromDeckForRemoval(prefs: new CardSelectorPrefs(CardSelectorPrefs.RemoveSelectionPrompt, 1), player: slipperyBridge.Owner)).ToList();
            await CardPileCmd.RemoveFromDeck(cards);
            EventAccess.Finish(slipperyBridge, "SLIPPERY_BRIDGE.pages.OVERCOMEFIX.description");
        }
        private static async Task HoldOnEndFix(SlipperyBridge slipperyBridge)
        {
            await CreatureCmd.Damage(new ThrowingPlayerChoiceContext(), slipperyBridge.Owner.Creature, 15, ValueProp.Unblockable | ValueProp.Unpowered, null, null);
            List<CardModel> cards = (await CardSelectCmd.FromDeckForRemoval(prefs: new CardSelectorPrefs(CardSelectorPrefs.RemoveSelectionPrompt, 2), player: slipperyBridge.Owner)).ToList();
            await CardPileCmd.RemoveFromDeck(cards);
            EventAccess.Finish(slipperyBridge, "SLIPPERY_BRIDGE.pages.HOLD_ON_ENDFIX.description");
        }
        private static async Task ReturnNow(SlipperyBridge slipperyBridge)
        {
            await CreatureCmd.Heal(slipperyBridge.Owner.Creature, 10);
            EventAccess.Finish(slipperyBridge, "SLIPPERY_BRIDGE.pages.RETURN_NOW.description");
        }
    }

}
