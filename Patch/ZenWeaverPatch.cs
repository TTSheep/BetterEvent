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
    /// 优化原版“修禅织网者”事件：增加扯碎蛛网并获得金币的选项。
    /// </summary>
    [HarmonyPatch(typeof(ZenWeaver), "GenerateInitialOptions")]
    public static class ZenWeaverPatch
    {
        private const string TearWebOptionKey =
            "ZEN_WEAVER.pages.INITIAL.options.TEAR_WEB";

        private const string TearWebDescriptionKey =
            "ZEN_WEAVER.pages.TEAR_WEB.description";

        /// <summary>
        /// 在原版三个修禅选项之后追加新选项，不改变金币检查与锁定逻辑。
        /// </summary>
        [HarmonyPostfix]
        private static void Postfix(
            ZenWeaver __instance,
            ref IReadOnlyList<EventOption> __result)
        {
            List<EventOption> options = new(__result)
            {
                new EventOption(
                    __instance,
                    () => TearWeb(__instance),
                    TearWebOptionKey)
            };

            __result = options;
        }

        /// <summary>
        /// 使用原版金币命令发放固定的 100 金币，随后正常结束事件。
        /// </summary>
        private static async Task TearWeb(ZenWeaver zenWeaver)
        {
            await PlayerCmd.GainGold(100m, zenWeaver.Owner, false);
            EventAccess.Finish(zenWeaver, TearWebDescriptionKey);
        }
    }
}
