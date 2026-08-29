using System.Collections.Generic;
using System.Threading.Tasks;
using BetterEvent.Utils;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Models.Relics;
using System.Linq;

namespace BetterEvent.Patch
{
    /// <summary>
    /// 优化原版“茶艺大师”事件：新增“其他选择”入口及对应的第二事件阶段。
    /// </summary>
    [HarmonyPatch(typeof(TeaMaster), "GenerateInitialOptions")]
    public static class TeaMasterPatch
    {
        // 本地化文本均集中在 zhs/eng 的 events.json，后续可直接修改文案。
        private const string OtherChoicesOptionKey =
            "TEA_MASTER.pages.INITIAL.options.OTHER_CHOICES";

        private const string OtherChoicesDescriptionKey =
            "TEA_MASTER.pages.OTHER_CHOICES.description";

        private const string DrinkAllOptionKey =
            "TEA_MASTER.pages.OTHER_CHOICES.options.DRINK_ALL";

        private const string StealGoldOptionKey =
            "TEA_MASTER.pages.OTHER_CHOICES.options.STEAL_GOLD";

        private const string LeaveOptionKey =
            "TEA_MASTER.pages.OTHER_CHOICES.options.LEAVE";

        private const string DrinkAllDescriptionKey =
            "TEA_MASTER.pages.DRINK_ALL.description";

        private const string StealGoldDescriptionKey =
            "TEA_MASTER.pages.STEAL_GOLD.description";

        private const string LeaveDescriptionKey =
            "TEA_MASTER.pages.LEAVE.description";

        /// <summary>
        /// 保留所有原版选项，只在列表末尾添加“其他选择”。
        /// </summary>
        [HarmonyPostfix]
        private static void Postfix(
            TeaMaster __instance,
            ref IReadOnlyList<EventOption> __result)
        {
            List<EventOption> options = new(__result);
            options.Add(new EventOption(
                __instance,
                () => OpenOtherChoices(__instance),
                OtherChoicesOptionKey));

            __result = options;
        }

        /// <summary>
        /// 进入第二阶段，展示三项新增选择。
        /// </summary>
        private static Task OpenOtherChoices(TeaMaster teaMaster)
        {
            IReadOnlyList<EventOption> options =
            [
                new EventOption(
                    teaMaster,
                    () => DrinkAll(teaMaster),
                    DrinkAllOptionKey),
                new EventOption(
                    teaMaster,
                    () => StealGold(teaMaster),
                    StealGoldOptionKey),
                new EventOption(
                    teaMaster,
                    () => Leave(teaMaster),
                    LeaveOptionKey)
            ];

            EventAccess.SetState(
                teaMaster,
                OtherChoicesDescriptionKey,
                options);

            return Task.CompletedTask;
        }

        /// <summary>
        /// 使用原版遗物获得命令，依次获得茶艺大师事件中的全部三种茶。
        /// </summary>
        private static async Task DrinkAll(TeaMaster teaMaster)
        {
            await RelicCmd.Obtain<BoneTea>(teaMaster.Owner);
            await RelicCmd.Obtain<EmberTea>(teaMaster.Owner);
            await RelicCmd.Obtain<TeaOfDiscourtesy>(teaMaster.Owner);

            EventAccess.Finish(teaMaster, DrinkAllDescriptionKey);
        }

        private static async Task StealGold(TeaMaster teaMaster)
        {
            await PlayerCmd.GainGold(50m, teaMaster.Owner, false);

            EventAccess.Finish(teaMaster, StealGoldDescriptionKey);
        }

        private static Task Leave(TeaMaster teaMaster)
        {
            EventAccess.Finish(teaMaster, LeaveDescriptionKey);
            return Task.CompletedTask;
        }

    }
}
