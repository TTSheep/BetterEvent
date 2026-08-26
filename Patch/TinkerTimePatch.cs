using BetterEvent.Utils;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Events;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace BetterEvent.Patch
{
    /// <summary>
    /// 优化原版“打造时间”事件：卡牌类型和对应的附加效果均展示完整的三个选项。
    /// </summary>
    [HarmonyPatch]
    public static class TinkerTimePatch
    {
        private const string CardTypeDescriptionKey =
            "TINKER_TIME.pages.CHOOSE_CARD_TYPE.description";

        private const string RiderEffectDescriptionKey =
            "TINKER_TIME.pages.CHOOSE_RIDER_EFFECT.description";

        private static readonly MethodInfo AttackMethod =
            AccessTools.Method(typeof(TinkerTime), "Attack");

        private static readonly MethodInfo SkillMethod =
            AccessTools.Method(typeof(TinkerTime), "Skill");

        private static readonly MethodInfo PowerMethod =
            AccessTools.Method(typeof(TinkerTime), "Power");

        private static readonly MethodInfo RiderChosenMethod =
            AccessTools.Method(typeof(TinkerTime), "RiderChosen");

        private static readonly MethodInfo GetCardTypeHoverTipMethod =
            AccessTools.Method(typeof(TinkerTime), "GetCardTypeHoverTip");

        private static readonly MethodInfo GetRiderLocKeyMethod =
            AccessTools.Method(typeof(TinkerTime), "GetRiderLocKey");

        private static readonly FieldInfo ChosenCardTypeField =
            AccessTools.Field(typeof(TinkerTime), "_chosenCardType");

        /// <summary>
        /// 原版会将三个卡牌类型随机截取为两个；这里保留原版选项内容和回调，直接展示全部类型。
        /// </summary>
        [HarmonyPatch(typeof(TinkerTime), "ChooseCardType")]
        [HarmonyPrefix]
        private static bool ChooseCardTypePrefix(
            TinkerTime __instance,
            ref Task __result)
        {
            List<EventOption> options =
            [
                CreateCardTypeOption(
                    __instance,
                    AttackMethod,
                    CardType.Attack,
                    "TINKER_TIME.pages.CHOOSE_CARD_TYPE.options.ATTACK"),
                CreateCardTypeOption(
                    __instance,
                    SkillMethod,
                    CardType.Skill,
                    "TINKER_TIME.pages.CHOOSE_CARD_TYPE.options.SKILL"),
                CreateCardTypeOption(
                    __instance,
                    PowerMethod,
                    CardType.Power,
                    "TINKER_TIME.pages.CHOOSE_CARD_TYPE.options.POWER")
            ];

            EventAccess.SetState(__instance, CardTypeDescriptionKey, options);
            __result = Task.CompletedTask;
            return false;
        }

        /// <summary>
        /// 原版每种卡牌类型都有三种效果，但只随机展示其中两个；这里仍使用事件 Rng
        /// 随机排列原版三种效果，同时把三项全部转换为事件选项。
        /// </summary>
        [HarmonyPatch(typeof(TinkerTime), "ChooseRiderEffect")]
        [HarmonyPrefix]
        private static bool ChooseRiderEffectPrefix(
            TinkerTime __instance,
            ref Task __result)
        {
            CardType chosenCardType =
                (CardType)ChosenCardTypeField.GetValue(__instance);

            IReadOnlyList<TinkerTime.RiderEffect> riderEffects =
                GetRiderEffects(chosenCardType);

            List<EventOption> options = riderEffects
                .TakeRandom(3, __instance.Rng)
                .Select(effect => CreateRiderOption(__instance, effect))
                .ToList();

            EventAccess.SetState(__instance, RiderEffectDescriptionKey, options);
            __result = Task.CompletedTask;
            return false;
        }

        private static EventOption CreateCardTypeOption(
            TinkerTime tinkerTime,
            MethodInfo callback,
            CardType cardType,
            string optionKey)
        {
            // CardHoverTip 与文本 HoverTip 是并列的悬浮提示类型，不能互相强制转换。
            CardHoverTip hoverTip = (CardHoverTip)GetCardTypeHoverTipMethod.Invoke(
                tinkerTime,
                [cardType]);

            return new EventOption(
                tinkerTime,
                () => InvokeTask(callback, tinkerTime),
                optionKey,
                [hoverTip]);
        }

        private static EventOption CreateRiderOption(
            TinkerTime tinkerTime,
            TinkerTime.RiderEffect riderEffect)
        {
            string optionKey = (string)GetRiderLocKeyMethod.Invoke(
                tinkerTime,
                [riderEffect]);

            return new EventOption(
                tinkerTime,
                () => InvokeTask(RiderChosenMethod, tinkerTime, riderEffect),
                optionKey,
                TinkerTime.GetRiderHoverTips(riderEffect));
        }

        /// <summary>
        /// 三组效果及其顺序均取自当前版本原版 TinkerTime 的 ChooseRiderEffect。
        /// </summary>
        private static IReadOnlyList<TinkerTime.RiderEffect> GetRiderEffects(
            CardType cardType)
        {
            return cardType switch
            {
                CardType.Attack =>
                [
                    TinkerTime.RiderEffect.Sapping,
                    TinkerTime.RiderEffect.Violence,
                    TinkerTime.RiderEffect.Choking
                ],
                CardType.Skill =>
                [
                    TinkerTime.RiderEffect.Energized,
                    TinkerTime.RiderEffect.Wisdom,
                    TinkerTime.RiderEffect.Chaos
                ],
                CardType.Power =>
                [
                    TinkerTime.RiderEffect.Expertise,
                    TinkerTime.RiderEffect.Curious,
                    TinkerTime.RiderEffect.Improvement
                ],
                _ => []
            };
        }

        private static Task InvokeTask(
            MethodInfo method,
            TinkerTime tinkerTime,
            params object[] arguments)
        {
            return (Task)method.Invoke(tinkerTime, arguments);
        }
    }
}
