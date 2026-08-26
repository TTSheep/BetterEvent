using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;

namespace BetterEvent
{
    [ModInitializer(nameof(Initialize))]
    public static class BetterEventInitializer
    {
        public static void Initialize()
        {
            // 扫描当前程序集中的 HarmonyPatch 特性并统一注册补丁。
            new Harmony("yy.better-event").PatchAll();

            Log.Info("BetterEvent - 加载成功!");
        }
    }
}
