using HarmonyLib;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace BetterEvent.Utils
{
    /// <summary>
    /// 集中封装 EventModel 中补丁需要访问的非公开方法。
    /// </summary>
    internal static class EventAccess
    {
        private static readonly MethodInfo SetEventFinishedMethod = AccessTools.Method(
            typeof(EventModel),
            "SetEventFinished",
            [typeof(LocString)]);

        private static readonly MethodInfo SetEventStateMethod = AccessTools.Method(
            typeof(EventModel),
            "SetEventState",
            [typeof(LocString), typeof(IEnumerable<EventOption>)]);

        private static readonly MethodInfo RelicOptionMethod = AccessTools.Method(
            typeof(EventModel),
            "RelicOption",
            [typeof(RelicModel), typeof(Func<Task>), typeof(string)]);

        private static readonly MethodInfo SetInitialEventStateMethod =
            AccessTools.Method(
                typeof(EventModel),
                "SetInitialEventState",
                [typeof(bool)]);

        /// <summary>
        /// 使用 events 本地化表中的后续文本结束指定事件。
        /// </summary>
        internal static void Finish(EventModel eventModel, string descriptionKey)
        {
            SetEventFinishedMethod.Invoke(
                eventModel,
                [new LocString("events", descriptionKey)]);
        }

        /// <summary>
        /// 切换到事件的下一阶段，并替换当前描述和选项。
        /// </summary>
        internal static void SetState(
            EventModel eventModel,
            string descriptionKey,
            IEnumerable<EventOption> options)
        {
            SetEventStateMethod.Invoke(
                eventModel,
                [new LocString("events", descriptionKey), options]);
        }

        /// <summary>
        /// 创建与原版事件一致的遗物选项，并为遗物绑定当前事件玩家。
        /// </summary>
        internal static EventOption CreateRelicOption(
            EventModel eventModel,
            RelicModel relic,
            Func<Task> onChosen,
            string pageId)
        {
            return (EventOption)RelicOptionMethod.Invoke(
                eventModel,
                [relic, onChosen, pageId]);
        }

        /// <summary>
        /// 重新调用事件的原版初始选项生成流程，并刷新当前页面。
        /// </summary>
        internal static void ResetToInitialState(EventModel eventModel)
        {
            SetInitialEventStateMethod.Invoke(eventModel, [false]);
        }
    }
}
