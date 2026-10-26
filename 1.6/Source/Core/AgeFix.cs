using RimWorld;
using System;
using Verse;

namespace RomanceAgeFix
{
    public static class AgeFix
    {
        public static float RomanceAgeOverride(Pawn pawn)
        {
            return Math.Max(16f, GetEquivalentHumanAge(pawn));
        }

        public static float AgeReversalDemandedAgeOverride(Pawn pawn)
        {
            return GetEquivalentHumanAge(pawn);
        }

        public static float GetEquivalentHumanAge(Pawn pawn)
        {
            if (pawn?.ageTracker == null || pawn.RaceProps == null) return 0f;

            float humanLifespan = ThingDefOf.Human?.race?.lifeExpectancy ?? 80f;
            StatDef lifespanStat = DefDatabase<StatDef>.GetNamedSilentFail("LifespanFactor");
            float lifespanFactor = lifespanStat == null ? 1f : pawn.GetStatValue(lifespanStat);
            return AgeScale.EquivalentHumanAge(pawn.ageTracker.AgeBiologicalYearsFloat,
                pawn.RaceProps.lifeExpectancy, lifespanFactor, humanLifespan);
        }
    }
}
