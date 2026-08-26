using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Runs;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;

namespace BetterEvent.Patch
{
    /// <summary>
    /// 优化原版“色彩哲学家”事件，选项扩展为所有角色
    /// </summary>
    [HarmonyPatch(typeof(ColorfulPhilosophers), "GenerateInitialOptions")]
    public static class ColorfulPhilosophersPatch
    {
        private static readonly MethodInfo MathfMin =
            AccessTools.Method(
                typeof(Mathf),
                nameof(Mathf.Min),
                [typeof(int), typeof(int)]);

        private static readonly MethodInfo OptionCountGetter =
            AccessTools.PropertyGetter(
                typeof(List<EventOption>),
                nameof(List<EventOption>.Count));

        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Transpiler(
            IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> codes = new(instructions);

            for (int i = 3; i < codes.Count; i++)
            {
                // 原版通过 Mathf.Min(3, options.Count) 将可选项限制为三个。
                // 当前游戏的 IL 在数字 3 和 Mathf.Min 之间还包含读取列表数量的指令，
                // 因此需要匹配完整指令序列，不能只检查 Mathf.Min 的前一条指令。
                if (codes[i].Calls(MathfMin) &&
                    codes[i - 1].Calls(OptionCountGetter) &&
                    codes[i - 2].opcode == OpCodes.Ldloc_0 &&
                    codes[i - 3].opcode == OpCodes.Ldc_I4_3)
                {
                    // 将上限 3 和 Mathf.Min 置空后，原位置会直接使用 options.Count，
                    // 从而保留全部角色选项。使用 Nop 可避免丢失指令上的标签信息。
                    codes[i - 3].opcode = OpCodes.Nop;
                    codes[i - 3].operand = null;
                    codes[i].opcode = OpCodes.Nop;
                    codes[i].operand = null;

                    break;
                }
            }

            return codes;
        }
    }
}
