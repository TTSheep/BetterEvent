using System.Collections.Generic;
using System.Threading.Tasks;
using BetterEvent.Utils;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Models.Relics;

namespace BetterEvent.Patch
{
    /// <summary>
    /// 优化原版“茶艺大师”事件：在原有三个选项之后追加一个同时获得两种茶的选项。
    /// </summary>
    [HarmonyPatch(typeof(TeaMaster), "GenerateInitialOptions")]
    public static class TeaMasterPatch
    {
        // 本地化暂定文本对应的键。之后修改文案时，只需编辑 zhs/eng 的 events.json。
        private const string BeatHisAssOptionKey =
            "TEA_MASTER.pages.INITIAL.options.BEAT_HIS_ASS";

        private const string BeatHisAssDescriptionKey =
            "TEA_MASTER.pages.BEAT_HIS_ASS.description";

        /// <summary>
        /// 保留所有原版选项，只在列表末尾添加“揍他一顿”。
        /// </summary>
        [HarmonyPostfix]
        private static void Postfix(
            TeaMaster __instance,
            ref IReadOnlyList<EventOption> __result)
        {
            List<EventOption> options = new(__result);
            options.Add(new EventOption(
                __instance,
                () => BeatHisAss(__instance),
                BeatHisAssOptionKey));

            __result = options;
        }

        /// <summary>
        /// 选中后不消耗金币，依次获得骨茶与余烬茶，然后显示专属后续文本。
        /// </summary>
        private static async Task BeatHisAss(TeaMaster teaMaster)
        {
            await RelicCmd.Obtain<BoneTea>(teaMaster.Owner);
            await RelicCmd.Obtain<EmberTea>(teaMaster.Owner);

            EventAccess.Finish(teaMaster, BeatHisAssDescriptionKey);
        }
    }
}
