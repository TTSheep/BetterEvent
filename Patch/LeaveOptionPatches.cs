using BetterEvent.Utils;
using HarmonyLib;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace BetterEvent.Patch
{
    /// <summary>
    /// 为指定原版事件统一追加可直接结束事件的离开选项。
    /// </summary>
    [HarmonyPatch]
    public static class LeaveOptionPatches
    {
        /// <summary>
        /// 仅扫描并注册指定事件各自重写的初始选项生成方法。
        /// </summary>
        [HarmonyTargetMethods]
        private static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(
                typeof(SpiritGrafter),
                "GenerateInitialOptions");

            yield return AccessTools.Method(
                typeof(RoundTeaParty),
                "GenerateInitialOptions");

            yield return AccessTools.Method(
                typeof(RanwidTheElder),
                "GenerateInitialOptions");

            yield return AccessTools.Method(
                typeof(DenseVegetation),
                "GenerateInitialOptions");

            yield return AccessTools.Method(
                typeof(TheFutureOfPotions),
                "GenerateInitialOptions");

            yield return AccessTools.Method(
                typeof(ThisOrThat),
                "GenerateInitialOptions");

            yield return AccessTools.Method(
                typeof(RoomFullOfCheese),
                "GenerateInitialOptions");

            yield return AccessTools.Method(
                typeof(BrainLeech),
                "GenerateInitialOptions");

            yield return AccessTools.Method(
                typeof(WhisperingHollow),
                "GenerateInitialOptions");

            yield return AccessTools.Method(
                typeof(LuminousChoir),
                "GenerateInitialOptions");

            yield return AccessTools.Method(
                typeof(MorphicGrove),
                "GenerateInitialOptions");

            yield return AccessTools.Method(
                typeof(UnrestSite),
                "GenerateInitialOptions");

            yield return AccessTools.Method(
                typeof(WoodCarvings),
                "GenerateInitialOptions");

            yield return AccessTools.Method(
                typeof(CrystalSphere),
                "GenerateInitialOptions");

            yield return AccessTools.Method(
                typeof(StoneOfAllTime),
                "GenerateInitialOptions");

            yield return AccessTools.Method(
                typeof(FieldOfManSizedHoles),
                "GenerateInitialOptions");
        }

        /// <summary>
        /// 保留原版全部选项及其顺序，只在列表末尾增加“离去”。
        /// </summary>
        [HarmonyPostfix]
        private static void Postfix(
            EventModel __instance,
            ref IReadOnlyList<EventOption> __result)
        {
            string localizationPrefix = GetLocalizationPrefix(__instance);
            List<EventOption> options = new(__result)
            {
                new EventOption(
                    __instance,
                    () => Leave(__instance, localizationPrefix),
                    $"{localizationPrefix}.pages.INITIAL.options.LEAVE")
            };

            __result = options;
        }

        /// <summary>
        /// 根据原版事件类型选择对应的原版本地化前缀。
        /// </summary>
        private static string GetLocalizationPrefix(EventModel eventModel)
        {
            return eventModel switch
            {
                SpiritGrafter => "SPIRIT_GRAFTER",
                RoundTeaParty => "ROUND_TEA_PARTY",
                RanwidTheElder => "RANWID_THE_ELDER",
                DenseVegetation => "DENSE_VEGETATION",
                TheFutureOfPotions => "THE_FUTURE_OF_POTIONS",
                ThisOrThat => "THIS_OR_THAT",
                RoomFullOfCheese => "ROOM_FULL_OF_CHEESE",
                BrainLeech => "BRAIN_LEECH",
                WhisperingHollow => "WHISPERING_HOLLOW",
                LuminousChoir => "LUMINOUS_CHOIR",
                MorphicGrove => "MORPHIC_GROVE",
                UnrestSite => "UNREST_SITE",
                WoodCarvings => "WOOD_CARVINGS",
                CrystalSphere => "CRYSTAL_SPHERE",
                StoneOfAllTime => "STONE_OF_ALL_TIME",
                FieldOfManSizedHoles => "FIELD_OF_MAN_SIZED_HOLES",
                _ => throw new System.ArgumentOutOfRangeException(
                    nameof(eventModel),
                    eventModel.GetType().FullName,
                    "不支持为该事件生成离去选项。")
            };
        }

        /// <summary>
        /// 使用原版 SetEventFinished 流程显示离去文本，并直接结束当前事件。
        /// </summary>
        private static Task Leave(
            EventModel eventModel,
            string localizationPrefix)
        {
            EventAccess.Finish(
                eventModel,
                $"{localizationPrefix}.pages.LEAVE.description");

            return Task.CompletedTask;
        }
    }
}
