using Verse;
using HarmonyLib;
using RimWorld;
using System;
using UnityEngine;

namespace RomanceAgeFix
{
    public static class AgeFix
    {
        public static float RomanceAgeOverride(Pawn pawn)
        {
            float expectancyLiftHuman = ThingDefOf.Human.race.lifeExpectancy;
            float expectancyLife = pawn.RaceProps.lifeExpectancy;
            // float expectancyLife2 = otherPawn.RaceProps.lifeExpectancy;

            float RomanceAge = pawn.ageTracker.AgeBiologicalYearsFloat;
            // float age2 = otherPawn.ageTracker.AgeBiologicalYearsFloat;

            if (expectancyLife > expectancyLiftHuman && RomanceAge > expectancyLiftHuman)
            {
                RomanceAge /= expectancyLife;
                if (RomanceAge < 16f)
                {
                    RomanceAge = 16f;
                }
            }
            return RomanceAge;
        }

    }
    public class Utils
    {
        public static float GetQuality(RitualOutcomeEffectWorker_ChildBirth __instance, LordJob_Ritual jobRitual, float progress,Pawn mother)
        {
            float quality = __instance.def.startingQuality;
            foreach (RitualOutcomeComp comp in __instance.def.comps)
            {
                if (comp is RitualOutcomeComp_PawnAge)
                {
                    float age = RomanceAgeFix.AgeFix.RomanceAgeOverride(mother);
                    Log.Warning($"Pawn {mother.Name} age: {age}");
                    quality += AgeFactor (age);
                    continue;
                }
                if (comp is RitualOutcomeComp_Quality && comp.Applies(jobRitual))
                {
                    Log.Warning("Applying RitualOutcomeComp_Quality"+ comp.GetType().Name);
                    quality += comp.QualityOffset(jobRitual, __instance.DataForComp(comp));
                }
            }

            if (jobRitual.repeatPenalty && jobRitual.Ritual != null)
            {
                quality += jobRitual.Ritual.RepeatQualityPenalty;
            }

            Tuple<ExpectationDef, float> expectationsOffset = RitualOutcomeEffectWorker_FromQuality.GetExpectationsOffset(jobRitual.Map, jobRitual.Ritual?.def);
            if (expectationsOffset != null)
            {
                quality += expectationsOffset.Item2;
            }

            return Mathf.Clamp(quality * Mathf.Lerp(
                RitualOutcomeEffectWorker_FromQuality.ProgressToQualityMapping.min,
                RitualOutcomeEffectWorker_FromQuality.ProgressToQualityMapping.max,
                progress),
                __instance.def.minQuality,
                __instance.def.maxQuality);
        }
        private static float AgeFactor(float age)
        {
            SimpleCurve ageCurve = new SimpleCurve
            {
                new CurvePoint(14f, 0.0f),
                new CurvePoint(15f, 0.3f),
                new CurvePoint(20f, 0.5f),
                new CurvePoint(30f, 0.5f),
                new CurvePoint(40f, 0.3f),
                new CurvePoint(65f, 0.0f)
            };
            return ageCurve.Evaluate(age);
        }
    }
}