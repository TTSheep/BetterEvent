using BetterEvent.Utils;
using HarmonyLib;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BetterEvent.Patch
{
    /// <summary>
    /// 优化原版“反射”事件：在原有选项后追加“小心试探”和“离去”。
    /// </summary>
    [HarmonyPatch(typeof(Reflections), "GenerateInitialOptions")]
    public static class ReflectionsPatch
    {
        private const string CautiousProbeOptionKey =
            "REFLECTIONS.pages.INITIAL.options.CAUTIOUS_PROBE";

        private const string LeaveOptionKey =
            "REFLECTIONS.pages.INITIAL.options.LEAVE";

        private const string CautiousProbeDescriptionKey =
            "REFLECTIONS.pages.CAUTIOUS_PROBE.description";

        private const string LeaveDescriptionKey =
            "REFLECTIONS.pages.LEAVE.description";

        private const string SelectionPromptKey =
            "REFLECTIONS.selectionScreenPrompt";

        /// <summary>
        /// 保留两个原版选项，并在列表末尾追加本模组选项。
        /// </summary>
        [HarmonyPostfix]
        private static void Postfix(
            Reflections __instance,
            ref IReadOnlyList<EventOption> __result)
        {
            List<EventOption> options = new(__result)
            {
                new EventOption(
                    __instance,
                    () => CautiousProbe(__instance),
                    CautiousProbeOptionKey),
                new EventOption(
                    __instance,
                    () => Leave(__instance),
                    LeaveOptionKey)
            };

            __result = options;
        }

        /// <summary>
        /// 从牌组中选择一张非任务牌，复制其当前状态并将复制品加入牌组。
        /// </summary>
        private static async Task CautiousProbe(Reflections reflections)
        {
            CardSelectorPrefs prefs = new(
                new LocString("events", SelectionPromptKey),
                1);
            CardModel selectedCard = (await CardSelectCmd.FromDeckGeneric(
                reflections.Owner,
                prefs,
                IsCopyable)).FirstOrDefault();

            if (selectedCard is not null)
            {
                CardModel copiedCard = reflections.Owner.RunState.CloneCard(selectedCard);
                CardPileAddResult addResult = await CardPileCmd.Add(
                    copiedCard,
                    PileType.Deck);

                CardCmd.PreviewCardPileAdd(
                    addResult,
                    1.2f,
                    CardPreviewStyle.EventLayout);
            }

            EventAccess.Finish(reflections, CautiousProbeDescriptionKey);
        }

        /// <summary>
        /// 不承受原版选项的收益或代价，直接结束事件。
        /// </summary>
        private static Task Leave(Reflections reflections)
        {
            EventAccess.Finish(reflections, LeaveDescriptionKey);
            return Task.CompletedTask;
        }

        private static bool IsCopyable(CardModel card)
        {
            // 任务牌具有特殊流程，不允许通过事件复制。
            return card.Type != CardType.Quest;
        }

    }
}
