using Verse;
using HarmonyLib;

namespace RomanceAgeFix.Startup
{
    [StaticConstructorOnStartup]
    public static class HarmonyInit
    {
        static HarmonyInit()
        {
            var harmony = new Harmony("com.phepholia");
            harmony.PatchAll();
            Log.Message("[phepholia] Harmony patches applied");
        }
    }
}
