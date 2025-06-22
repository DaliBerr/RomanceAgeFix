using Verse;
using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;

namespace RomanceAgeFix.HarmonyPatches
{
    [HarmonyPatch(typeof(Pawn_RelationsTracker), "SecondaryRomanceChanceFactor")]
    public static class Prefix_MinAgeForRomance
    {
        public static int requiredMinAge = 16;
        [HarmonyPostfix]
        public static void SecondaryRomanceChanceFactor_Postfix(Pawn ___pawn, Pawn otherPawn, ref float __result)
        {

            Pawn pawn = ___pawn;
            if (pawn.ageTracker.AgeBiologicalYears < requiredMinAge || otherPawn.ageTracker.AgeBiologicalYears < requiredMinAge)
            {
                __result = 0f;
                return;
            }
            float num = 1f;
            foreach (PawnRelationDef relation in pawn.GetRelations(otherPawn))
            {
                num *= relation.romanceChanceFactor;
            }
            float num2 = 1f;
            HediffWithTarget hediffWithTarget = (HediffWithTarget)pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.PsychicLove);
            if (hediffWithTarget != null && hediffWithTarget.target == otherPawn)
            {
                num2 = 10f;
            }
            float num3 = 1f;
            if (ModsConfig.BiotechActive && pawn.genes != null && (otherPawn.story?.traits == null || !otherPawn.story.traits.HasTrait(TraitDefOf.Kind)))
            {
                List<Gene> genesListForReading = pawn.genes.GenesListForReading;
                for (int i = 0; i < genesListForReading.Count; i++)
                {
                    if (genesListForReading[i].def.missingGeneRomanceChanceFactor != 1f &&
                        (otherPawn.genes == null || !otherPawn.genes.HasActiveGene(genesListForReading[i].def)))
                    {
                        num3 *= genesListForReading[i].def.missingGeneRomanceChanceFactor;
                    }
                }
            }
            float baseChance = SecondaryLovinChanceFactor(pawn, otherPawn, requiredMinAge);
            // Log.Warning("baseChance: " + baseChance);
            __result = baseChance * num * num2 * num3;
            return;
        }
        public static float SecondaryLovinChanceFactor(Pawn pawn, Pawn otherPawn, float minRequiredAge)
        {
            if (pawn == otherPawn)
            {
                // Log.Warning("pawn "+ pawn.Name + "with "+ otherPawn.Name +" def: " + pawn.def + " otherPawn: " + otherPawn.def);
                return 0f;
            }
            if (pawn.story != null && pawn.story.traits != null)
            {
                if (pawn.story.traits.HasTrait(TraitDefOf.Asexual))
                {
                    // Log.Warning("pawn "+ pawn.Name + "with "+ otherPawn.Name +" Asexual: " + pawn.story.traits.HasTrait(TraitDefOf.Asexual));
                    return 0f;
                }
                if (!pawn.story.traits.HasTrait(TraitDefOf.Bisexual))
                {
                    if (pawn.story.traits.HasTrait(TraitDefOf.Gay))
                    {
                        if (otherPawn.gender != pawn.gender)
                            return 0f;
                    }
                    else if (otherPawn.gender == pawn.gender)
                    {
                        return 0f;
                    }
                }
            }
            if (pawn.ageTracker.AgeBiologicalYearsFloat < minRequiredAge || otherPawn.ageTracker.AgeBiologicalYearsFloat < minRequiredAge)
                return 0f;
            return LovinAgeFactor(pawn, otherPawn) * PrettinessFactor(otherPawn);
        }
        private static float PrettinessFactor(Pawn otherPawn)
        {
            float beauty = 0f;
            if (otherPawn.RaceProps.Humanlike)
                beauty = otherPawn.GetStatValue(StatDefOf.PawnBeauty);

            if (beauty < 0f) return 0.3f;
            if (beauty > 0f) return 2.3f;
            return 1f;
        }
        private static float LovinAgeFactor(Pawn pawn, Pawn otherPawn)
        {
            float num = 1f;

            // float expectancyLiftHuman = ThingDefOf.Human.race.lifeExpectancy;
            float expectancyLife1 = pawn.RaceProps.lifeExpectancy;
            float expectancyLife2 = otherPawn.RaceProps.lifeExpectancy;
            float age1 = RomanceAgeFix.AgeFix.RomanceAgeOverride(pawn);
            float age2 = RomanceAgeFix.AgeFix.RomanceAgeOverride(otherPawn);

            float malemin = expectancyLife1 * .375f;
            float malelower = expectancyLife1 * .25f;
            float maleupper = expectancyLife1 * .075f;
            float malemax = expectancyLife1 * .25f;

            float femalemin = expectancyLife2 * .1875f;
            float femalelower = expectancyLife2 * .1f;
            float femaleupper = expectancyLife2 * .1875f;
            float femalemax = expectancyLife2 * .5f;

            if (pawn.gender == Gender.Male)
            {
                float min = age1 - malemin;
                float lower = age1 - malelower;
                float upper = age1 + maleupper;
                float max = age1 + malemax;
                num = GenMath.FlatHill(0.2f, min, lower, upper, max, 0.2f, age1);
            }
            else if (pawn.gender == Gender.Female)
            {
                float min2 = age1 - femalemin;
                float lower2 = age1 - femalelower;
                float upper2 = age1 + femaleupper;
                float max2 = age1 + femalemax;
                num = GenMath.FlatHill(0.2f, min2, lower2, upper2, max2, 0.2f, age2);
            }
            // Log.Warning( "pawn "+ pawn.Name + "with "+ otherPawn.Name +" AgeFactor: " + num);
            return num;
        }
    }

    [HarmonyPatch(typeof(LovePartnerRelationUtility), "GetLovinMtbHours")]
    public static class Postfix_GetLovinMtbHours
    {
        [HarmonyPostfix]
        public static void GetLovinMtbHours_Postfix(Pawn pawn, Pawn partner, ref float __result)
        {
            if (pawn.Dead || partner.Dead)
            {
                __result = -1f;
                return;
                // return -1f;
            }

            if (DebugSettings.alwaysDoLovin)
            {
                __result = 0.1f;
                return;
                // return 0.1f;
            }

            if (pawn.needs.food.Starving || partner.needs.food.Starving)
            {
                __result = -1f;
                return;
                // return -1f;
            }

            if (pawn.health.hediffSet.BleedRateTotal > 0f || partner.health.hediffSet.BleedRateTotal > 0f)
            {
                __result = -1f;
                return;
                // return -1f;
            }

            if (pawn.health.hediffSet.InLabor() || partner.health.hediffSet.InLabor())
            {
                __result = -1f;
                return;
                // return -1f;
            }
            float num = LovinMtbSinglePawnFactor(pawn);
            float num2 = LovinMtbSinglePawnFactor(partner);
            if (num < 0f || num2 < 0f)
            {
                __result = -1f;
                return;
            }
            float num3 = 12f;
            num3 *= num;
            num3 *= num2;
            num3 /= Mathf.Max(Prefix_MinAgeForRomance.SecondaryLovinChanceFactor(pawn, partner, Prefix_MinAgeForRomance.requiredMinAge), 0.1f);
            num3 *= GenMath.LerpDouble(-100f, 100f, 1.3f, 0.7f, pawn.relations.OpinionOf(partner));
            num3 *= GenMath.LerpDouble(-100f, 100f, 1.3f, 0.7f, partner.relations.OpinionOf(pawn));
            if (pawn.health.hediffSet.HasHediff(HediffDefOf.PsychicLove))
            {
                num3 /= 4f;
            }

            __result = num3;
            return;
        }

        private static float LovinMtbSinglePawnFactor(Pawn pawn)
        {
            float num = 1f;
            num /= 1f - pawn.health.hediffSet.PainTotal;
            float level = pawn.health.capacities.GetLevel(PawnCapacityDefOf.Consciousness);
            if (level < 0.5f)
            {
                num /= level * 2f;
            }
            return num / GenMath.FlatHill(0f, 14f, 16f, 25f, 80f, 0.2f, RomanceAgeFix.AgeFix.RomanceAgeOverride(pawn));
        }
    }

    // [HarmonyPatch(typeof(RitualOutcomeEffectWorker_ChildBirth), "Apply")]
    // public static class Prefix_RitualOutcomeEffectWorker_ChildBirth
    // {
    //     [HarmonyPrefix]
    //     public static bool Apply_Prefix(RitualOutcomeEffectWorker_ChildBirth __instance,
    //      float progress, Dictionary<Pawn, int> totalPresence, LordJob_Ritual jobRitual)
    //     {
    //         if (progress != 0f)
    //         {
    //             Pawn mother = jobRitual.assignments.FirstAssignedPawn("mother");
    //             // float quality = Utils.GetQuality(__instance, jobRitual, progress, mother);
    //             float quality = __instance.GetQuality(jobRitual, progress);
    //             RitualOutcomePossibility outcome = __instance.GetOutcome(quality, jobRitual);
    //             // Pawn pawn = jobRitual.assignments.FirstAssignedPawn("mother");
    //             Pawn doctor = jobRitual.assignments.FirstAssignedPawn("doctor");
    //             Hediff_LaborPushing hediff_LaborPushing = (Hediff_LaborPushing)mother.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.PregnancyLaborPushing);
    //             PregnancyUtility.ApplyBirthOutcome(outcome, quality, jobRitual.Ritual, hediff_LaborPushing.geneSet?.GenesListForReading, hediff_LaborPushing.Mother ?? mother, mother, hediff_LaborPushing.Father, doctor, jobRitual, jobRitual.assignments);
    //         }
    //         return false; // Skip original method execution
    //     }

    // }
    [HarmonyPatch(typeof(RitualOutcomeEffectWorker_FromQuality), "GetQuality")]
    public static class Prefix_RitualOutcomeEffectWorker_FromQuality_GetQuality
    {
        [HarmonyPrefix]
        public static bool GetQuality_Prefix(
        RitualOutcomeEffectWorker_FromQuality __instance,
        ref float __result,
        LordJob_Ritual jobRitual,
        float progress)
        {
            if (__instance is RitualOutcomeEffectWorker_ChildBirth && jobRitual.assignments.FirstAssignedPawn("mother") is Pawn mother)
            {
                __result = Utils.GetQuality((RitualOutcomeEffectWorker_ChildBirth)__instance, jobRitual, progress, mother);
                // Log.Warning("Skipping original GetQuality method ");
                return false; // Skip original method
            }
            // Log.Warning("Using original GetQuality method for other rituals");
            return true; // Use original for other rituals
        }
    }

    [HarmonyPatch(typeof(ThoughtWorker_AgeReversalDemanded))]
    public static class Prefix_ThoughtWorker_AgeReversalDemanded_ShouldHaveThought
    {
        public static MethodBase TargetMethod()
        {
            return typeof(ThoughtWorker_AgeReversalDemanded).GetMethod(
                "ShouldHaveThought",
                BindingFlags.Instance | BindingFlags.NonPublic
            );
        }
        [HarmonyPostfix]
        public static void ShouldHaveThought_Postfix(Pawn p, ref ThoughtState __result)
        {
            if (p.Faction != Faction.OfPlayer)
			{
				__result= ThoughtState.Inactive;
			}
			if (p.IsSlave)
			{
				__result= ThoughtState.Inactive;
			}
			if (p.ageTracker == null)
			{
				__result= ThoughtState.Inactive;
			}
			if (AgeFix.AgeReversalDemandedAgeOverride(p) < (25f/80f))
            {
                __result= ThoughtState.Inactive;
            }
			{
				__result= ThoughtState.Inactive;
			}
			long ageReversalDemandedDeadlineTicks = p.ageTracker.AgeReversalDemandedDeadlineTicks;
			if (ageReversalDemandedDeadlineTicks > 0L)
			{
				__result= ThoughtState.ActiveAtStage(3);
			}
			long num = -ageReversalDemandedDeadlineTicks / 60000L;
			int num2;
			if (num <= 15L)
			{
				num2 = 0;
			}
			else if (num <= 30L)
			{
				num2 = 1;
			}
			else
			{
				num2 = 2;
			}
			__result= ThoughtState.ActiveAtStage(num2);
        }
    }
}