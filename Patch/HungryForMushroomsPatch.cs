using BetterEvent.Utils;
using HarmonyLib;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Runs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BetterEvent.Patch
{

    /// <summary>
    /// 原版事件“蘑菇饥渴”优化:新增选项“强忍饥饿”与“全吃了”
    /// </summary>
    [HarmonyPatch(typeof(HungryForMushrooms), "GenerateInitialOptions")]
    public static class HungryForMushroomsPatch
    {
        [HarmonyPostfix]
        private static void Postfix(
            HungryForMushrooms __instance,
            ref IReadOnlyList<EventOption> __result)
        {
            List<EventOption> _options = new(__result);
            _options.Add(new EventOption(
                __instance,
                () => EatAll(__instance),
                "HUNGRY_FOR_MUSHROOMS.pages.INITIAL.options.EAT_ALL"));

            // 与原版事件一致：没有可升级牌时保留选项，但将其显示为不可点击并说明原因。
            if (CardPile.Get(PileType.Deck, __instance.Owner).UpgradableCardCount > 0)
            {
                _options.Add(new EventOption(
                    __instance,
                    () => EndureHunger(__instance),
                    "HUNGRY_FOR_MUSHROOMS.pages.INITIAL.options.ENDURE_HUNGER"));
            }
            else
            {
                _options.Add(new EventOption(
                    __instance,
                    null,
                    "HUNGRY_FOR_MUSHROOMS.pages.INITIAL.options.ENDURE_HUNGER_LOCKED",
                    Array.Empty<IHoverTip>()));
            }

            __result = _options;
        }
        private static async Task EatAll(HungryForMushrooms hungryForMushrooms)
        {
            await RelicCmd.Obtain<BigMushroom>(hungryForMushrooms.Owner);
            await RelicCmd.Obtain<FragrantMushroom>(hungryForMushrooms.Owner);
            EventAccess.Finish(
                hungryForMushrooms,
                "HUNGRY_FOR_MUSHROOMS.pages.EAT_ALL.description");
        }
        private static async Task EndureHunger(HungryForMushrooms hungryForMushrooms)
        {
            CardModel cardModel = (await CardSelectCmd.FromDeckForUpgrade(hungryForMushrooms.Owner, new CardSelectorPrefs(CardSelectorPrefs.UpgradeSelectionPrompt, 1))).FirstOrDefault();
            if (cardModel != null)
            {
                CardCmd.Upgrade(cardModel);
            }
            EventAccess.Finish(
                hungryForMushrooms,
                "HUNGRY_FOR_MUSHROOMS.pages.ENDURE_HUNGER.description");
        }
    }
}
