using BetterEvent.Utils;
using HarmonyLib;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models.Events;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BetterEvent.Patch
{
    /// <summary>
    /// 优化原版“审判”事件：允许玩家在第一阶段直接逃走。
    /// </summary>
    [HarmonyPatch(typeof(Trial), "GenerateInitialOptions")]
    public static class TrialPatch
    {
        private const string RunAwayOptionKey =
            "TRIAL.pages.INITIAL.options.RUN_AWAY";

        private const string RunAwayDescriptionKey =
            "TRIAL.pages.RUN_AWAY.description";

        /// <summary>
        /// 仅在原版初始选项末尾追加“逃走”，不修改审判事件后续阶段的原有逻辑。
        /// </summary>
        [HarmonyPostfix]
        private static void Postfix(
            Trial __instance,
            ref IReadOnlyList<EventOption> __result)
        {
            List<EventOption> options = new(__result)
            {
                new EventOption(
                    __instance,
                    () => RunAway(__instance),
                    RunAwayOptionKey)
            };

            __result = options;
        }

        /// <summary>
        /// 使用原版事件结束流程显示逃走后的文本，并直接结束事件。
        /// </summary>
        private static Task RunAway(Trial trial)
        {
            EventAccess.Finish(trial, RunAwayDescriptionKey);
            return Task.CompletedTask;
        }
    }
}
