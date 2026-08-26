using BetterEvent.Utils;
using HarmonyLib;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.ValueProps;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BetterEvent.Patch
{
    /// <summary>
    /// 优化原版“垃圾堆”事件：固定奖励由随机获得改为玩家自选，并增加离去选项。
    /// </summary>
    [HarmonyPatch(typeof(TrashHeap), "GenerateInitialOptions")]
    public static class TrashHeapPatch
    {
        private const int RelicsPerPage = 3;

        private const string ChooseRelicOptionKey =
            "TRASH_HEAP.pages.INITIAL.options.CHOOSE_RELIC";

        private const string ChooseCardOptionKey =
            "TRASH_HEAP.pages.INITIAL.options.CHOOSE_CARD";

        private const string LeaveOptionKey =
            "TRASH_HEAP.pages.INITIAL.options.LEAVE";

        private const string RelicSelectionPageKey =
            "TRASH_HEAP.pages.RELIC_SELECTION.description";

        private const string RelicSelectionPageId = "RELIC_SELECTION";

        private const string NextRelicPageOptionKey =
            "TRASH_HEAP.pages.RELIC_SELECTION.options.NEXT_PAGE";

        private const string PreviousRelicPageOptionKey =
            "TRASH_HEAP.pages.RELIC_SELECTION.options.PREVIOUS_PAGE";

        private const string RelicSelectedPageKey =
            "TRASH_HEAP.pages.RELIC_SELECTED.description";

        private const string CardSelectionPromptKey =
            "TRASH_HEAP.cardSelectionPrompt";

        private const string CardSelectedPageKey =
            "TRASH_HEAP.pages.CARD_SELECTED.description";

        private const string LeavePageKey =
            "TRASH_HEAP.pages.LEAVE.description";

        /// <summary>
        /// 用三个可控选项替换原版的两个随机奖励选项。
        /// </summary>
        [HarmonyPostfix]
        private static void Postfix(
            TrashHeap __instance,
            ref IReadOnlyList<EventOption> __result)
        {
            __result =
            [
                new EventOption(
                    __instance,
                    () => OpenRelicSelection(__instance),
                    ChooseRelicOptionKey)
                    .ThatDoesDamage(__instance.DynamicVars.HpLoss.IntValue),
                new EventOption(
                    __instance,
                    () => ChooseOldCard(__instance),
                    ChooseCardOptionKey),
                new EventOption(
                    __instance,
                    () => Leave(__instance),
                    LeaveOptionKey)
            ];
        }

        /// <summary>
        /// 支付原版生命代价后进入下一事件阶段，将五个原版遗物逐一列为选项。
        /// </summary>
        private static async Task OpenRelicSelection(TrashHeap trashHeap)
        {
            await CreatureCmd.Damage(
                new ThrowingPlayerChoiceContext(),
                trashHeap.Owner.Creature,
                trashHeap.DynamicVars.HpLoss.IntValue,
                ValueProp.Unblockable | ValueProp.Unpowered,
                null,
                null);

            await ShowRelicPage(trashHeap, 0);
        }

        /// <summary>
        /// 原版事件界面不滚动长选项列表，因此每页最多展示三个遗物和一个翻页选项。
        /// </summary>
        private static Task ShowRelicPage(
            TrashHeap trashHeap,
            int pageIndex)
        {
            IReadOnlyList<RelicModel> relics = GetTrashHeapRelics();
            List<EventOption> relicOptions = [];

            foreach (RelicModel canonicalRelic in relics
                .Skip(pageIndex * RelicsPerPage)
                .Take(RelicsPerPage))
            {
                RelicModel relic = canonicalRelic.ToMutable();
                relicOptions.Add(EventAccess.CreateRelicOption(
                    trashHeap,
                    relic,
                    () => ObtainRelic(trashHeap, relic),
                    RelicSelectionPageId));
            }

            if ((pageIndex + 1) * RelicsPerPage < relics.Count)
            {
                relicOptions.Add(new EventOption(
                    trashHeap,
                    () => ShowRelicPage(trashHeap, pageIndex + 1),
                    NextRelicPageOptionKey));
            }

            if (pageIndex > 0)
            {
                relicOptions.Add(new EventOption(
                    trashHeap,
                    () => ShowRelicPage(trashHeap, pageIndex - 1),
                    PreviousRelicPageOptionKey));
            }

            EventAccess.SetState(
                trashHeap,
                RelicSelectionPageKey,
                relicOptions);

            return Task.CompletedTask;
        }

        private static async Task ObtainRelic(
            TrashHeap trashHeap,
            RelicModel relic)
        {
            await RelicCmd.Obtain(relic, trashHeap.Owner);
            EventAccess.Finish(trashHeap, RelicSelectedPageKey);
        }

        /// <summary>
        /// 保留原版金币收益，并在网格中展示全部十张旧日卡牌供玩家选择一张。
        /// </summary>
        private static async Task ChooseOldCard(TrashHeap trashHeap)
        {
            await PlayerCmd.GainGold(
                trashHeap.DynamicVars.Gold.BaseValue,
                trashHeap.Owner,
                false);

            List<CardModel> cards = GetOldCards()
                .Select(card => trashHeap.Owner.RunState.CreateCard(
                    card,
                    trashHeap.Owner))
                .ToList();

            CardSelectorPrefs prefs = new(
                new LocString("events", CardSelectionPromptKey),
                1);

            CardModel selectedCard = (await CardSelectCmd.FromSimpleGrid(
                new BlockingPlayerChoiceContext(),
                cards,
                trashHeap.Owner,
                prefs)).FirstOrDefault();

            if (selectedCard is not null)
            {
                CardPileAddResult addResult = await CardPileCmd.Add(
                    selectedCard,
                    PileType.Deck);

                CardCmd.PreviewCardPileAdd(
                    addResult,
                    1.2f,
                    CardPreviewStyle.EventLayout);
            }

            EventAccess.Finish(trashHeap, CardSelectedPageKey);
        }

        private static Task Leave(TrashHeap trashHeap)
        {
            EventAccess.Finish(trashHeap, LeavePageKey);
            return Task.CompletedTask;
        }

        /// <summary>
        /// 原版垃圾堆事件的五种遗物候选。
        /// </summary>
        private static IReadOnlyList<RelicModel> GetTrashHeapRelics()
        {
            return
            [
                ModelDb.Relic<DarkstonePeriapt>(),
                ModelDb.Relic<DreamCatcher>(),
                ModelDb.Relic<HandDrill>(),
                ModelDb.Relic<MawBank>(),
                ModelDb.Relic<TheBoot>()
            ];
        }

        /// <summary>
        /// 原版垃圾堆事件收录的全部十张旧日卡牌。
        /// </summary>
        private static IReadOnlyList<CardModel> GetOldCards()
        {
            return
            [
                ModelDb.Card<Caltrops>(),
                ModelDb.Card<Clash>(),
                ModelDb.Card<Distraction>(),
                ModelDb.Card<DualWield>(),
                ModelDb.Card<Entrench>(),
                ModelDb.Card<HelloWorld>(),
                ModelDb.Card<Outmaneuver>(),
                ModelDb.Card<Rebound>(),
                ModelDb.Card<RipAndTear>(),
                ModelDb.Card<Stack>()
            ];
        }
    }
}
