using BetterEvent.Utils;
using HarmonyLib;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Gold;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Runs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BetterEvent.Patch
{
    /// <summary>
    /// 优化原版“旺购百货”事件：将离开入口替换为包含额外交易、卡牌交换和离开的第二阶段。
    /// </summary>
    [HarmonyPatch(typeof(WelcomeToWongos), "GenerateInitialOptions")]
    public static class WelcomeToWongosPatch
    {
        private const string OriginalLeaveOptionKey =
            "WELCOME_TO_WONGOS.pages.INITIAL.options.LEAVE";

        private const string OtherChoicesOptionKey =
            "WELCOME_TO_WONGOS.pages.INITIAL.options.OTHER_CHOICES";

        private const string OtherChoicesDescriptionKey =
            "WELCOME_TO_WONGOS.pages.OTHER_CHOICES.description";

        private const string GiveGoldOptionKey =
            "WELCOME_TO_WONGOS.pages.OTHER_CHOICES.options.GIVE_GOLD";

        private const string GiveGoldLockedOptionKey =
            "WELCOME_TO_WONGOS.pages.OTHER_CHOICES.options.GIVE_GOLD_LOCKED";

        private const string ExchangeOptionKey =
            "WELCOME_TO_WONGOS.pages.OTHER_CHOICES.options.EXCHANGE";

        private const string ExchangeLockedOptionKey =
            "WELCOME_TO_WONGOS.pages.OTHER_CHOICES.options.EXCHANGE_LOCKED";

        private const string LeaveOptionKey =
            "WELCOME_TO_WONGOS.pages.OTHER_CHOICES.options.LEAVE";

        private const string ExchangeSelectionPromptKey =
            "WELCOME_TO_WONGOS.exchangeSelectionPrompt";

        private const string ExchangeDescriptionKey =
            "WELCOME_TO_WONGOS.pages.EXCHANGE.description";

        private const string GiveGoldDescriptionKey =
            "WELCOME_TO_WONGOS.pages.GIVE_GOLD.description";

        private const string LeaveDescriptionKey =
            "WELCOME_TO_WONGOS.pages.LEAVE.description";

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
                () => OpenOtherChoices(__instance),
                OtherChoicesOptionKey);

            __result = options;
        }

        /// <summary>
        /// 第二阶段保留交换选项的可见性；没有合规卡牌时使用原版锁定选项机制说明原因。
        /// </summary>
        private static Task OpenOtherChoices(WelcomeToWongos wongos)
        {
            bool canAffordGiveGold = wongos.Owner.Gold >= 50;
            bool hasExchangeableCard = CardPile
                .Get(PileType.Deck, wongos.Owner)
                .Cards.Any(CanExchange);

            List<EventOption> options =
            [
                canAffordGiveGold
                    ? new EventOption(
                        wongos,
                        () => GiveGold(wongos),
                        GiveGoldOptionKey)
                    : new EventOption(
                    wongos,
                    null,
                    GiveGoldLockedOptionKey)
            ];

            options.Add(hasExchangeableCard
                ? new EventOption(
                    wongos,
                    () => Exchange(wongos),
                    ExchangeOptionKey)
                : new EventOption(
                    wongos,
                    null,
                    ExchangeLockedOptionKey,
                    Array.Empty<IHoverTip>()));

            options.Add(new EventOption(
                wongos,
                () => Leave(wongos),
                LeaveOptionKey));

            EventAccess.SetState(
                wongos,
                OtherChoicesDescriptionKey,
                options);

            return Task.CompletedTask;
        }

        /// <summary>
        /// 使用原版金币支出与奖励界面：支付 50 金币，获得两瓶独立随机的药水。
        /// </summary>
        private static async Task GiveGold(WelcomeToWongos wongos)
        {
            // 二次检查余额，避免选项显示后金币状态被其他效果修改而产生负数或异常扣款。
            if (wongos.Owner.Gold < 50)
            {
                return;
            }

            await PlayerCmd.LoseGold(
                50m,
                wongos.Owner,
                GoldLossType.Spent);

            await RewardsCmd.OfferCustom(
                wongos.Owner,
                [
                    new PotionReward(wongos.Owner),
                    new PotionReward(wongos.Owner)
                ]);

            EventAccess.Finish(wongos, GiveGoldDescriptionKey);
        }

        /// <summary>
        /// 只有原版允许移除且奖励规则明确的四种稀有度能够用于交换；
        /// 带有“永恒”的牌会由 IsRemovable 自动排除。
        /// </summary>
        private static bool CanExchange(CardModel card)
        {
            return card.IsRemovable && card.Rarity is
                CardRarity.Basic or
                CardRarity.Curse or
                CardRarity.Common or
                CardRarity.Uncommon or
                CardRarity.Rare;
        }

        private static async Task Exchange(WelcomeToWongos wongos)
        {
            CardSelectorPrefs prefs = new(
                new LocString("events", ExchangeSelectionPromptKey),
                1);

            CardModel card = (await CardSelectCmd.FromDeckForRemoval(
                wongos.Owner,
                prefs,
                CanExchange)).FirstOrDefault();

            if (card is null)
            {
                return;
            }

            CardRarity rarity = card.Rarity;
            await CardPileCmd.RemoveFromDeck([card]);

            switch (rarity)
            {
                case CardRarity.Curse:
                    await PlayerCmd.GainGold(1m, wongos.Owner, false);
                    break;

                case CardRarity.Common or CardRarity.Basic:
                    await CreatureCmd.Heal(wongos.Owner.Creature, 5);
                    break;

                case CardRarity.Uncommon:
                    await RewardsCmd.OfferCustom(
                        wongos.Owner,
                        [
                            new PotionReward(wongos.Owner),
                            CreateCardReward(wongos)
                        ]);
                    break;

                case CardRarity.Rare:
                    await RewardsCmd.OfferCustom(
                        wongos.Owner,
                        [
                            new GoldReward(50, wongos.Owner, false),
                            new RelicReward(wongos.Owner)
                        ]);
                    break;
            }

            EventAccess.Finish(wongos, ExchangeDescriptionKey);
        }

        private static CardReward CreateCardReward(
            WelcomeToWongos wongos)
        {
            CardCreationOptions options =
                CardCreationOptions.ForNonCombatWithDefaultOdds(
                    [wongos.Owner.Character.CardPool],
                    null);

            return new CardReward(
                options,
                3,
                wongos.Owner,
                null);
        }

        private static Task Leave(WelcomeToWongos wongos)
        {
            EventAccess.Finish(wongos, LeaveDescriptionKey);
            return Task.CompletedTask;
        }
    }
}
