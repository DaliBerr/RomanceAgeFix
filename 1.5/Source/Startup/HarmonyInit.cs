using Verse;
using HarmonyLib;

namespace RomanceAgeFix.Startup
{
    [StaticConstructorOnStartup]
    public static class HarmonyInit
    {
        static HarmonyInit()
        {
            var harmony = new Harmony("com.romanceagefix");
            harmony.PatchAll();
            // Log.Message("[romanceagefix] Harmony patches applied");
        }
    }
}
