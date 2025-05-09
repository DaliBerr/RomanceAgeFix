using Verse;
using HarmonyLib;
using RimWorld;
using System.Collections.Generic;

namespace RomanceAgeFix.HarmonyPatches
{
    [HarmonyPatch(typeof(Pawn_RelationsTracker), "SecondaryRomanceChanceFactor")]
    public static class Prefix_MinAgeForRomance
    {
        public static int requiredMinAge = 16;
        [HarmonyPostfix]
        public static void SecondaryRomanceChanceFactor_Prefix(Pawn ___pawn, Pawn otherPawn, ref float __result)
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
        private static float SecondaryLovinChanceFactor(Pawn pawn, Pawn otherPawn, float minRequiredAge)
        {
            if (pawn == otherPawn)
            {
                // Log.Warning("pawn "+ pawn.Name + "with "+ otherPawn.Name +" def: " + pawn.def + " otherPawn: " + otherPawn.def);
                return 0f;
            }
            if (pawn.story != null && pawn.story.traits != null)
            {
                if (pawn.story.traits.HasTrait(TraitDefOf.Asexual)){
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
            float expectancyLiftHuman = ThingDefOf.Human.race.lifeExpectancy;
            float expectancyLife1 = pawn.RaceProps.lifeExpectancy;
            float expectancyLife2 = otherPawn.RaceProps.lifeExpectancy;

            float age1 = pawn.ageTracker.AgeBiologicalYearsFloat;
            float age2 = otherPawn.ageTracker.AgeBiologicalYearsFloat;

            if(expectancyLife1 > expectancyLiftHuman && age1 > expectancyLiftHuman){
                age1 /= expectancyLife1;
                if(age1 < 16f){
                    age1 = 16f;
                }
            }
            if(expectancyLife2 > expectancyLiftHuman && age2 > expectancyLiftHuman){
                age2 /= expectancyLife2;
                if(age2 < 16f){
                    age2 = 16f;
                }
            }
            float malemin = expectancyLife1 * .375f;
            float malelower = expectancyLife1 * .125f;
            float maleupper = expectancyLife1 * .0375f;
            float malemax = expectancyLife1 * .125f;
            float femalemin = expectancyLife1 * .125f;
            float femalelower = expectancyLife1 * .0375f;
            float femaleupper = expectancyLife1 * .125f;
            float femalemax = expectancyLife1 * .375f;

            if (pawn.gender == Gender.Male)
            {
                float min = age1 - malemin;
                float lower = age1 - malelower;
                float upper = age1 + maleupper;
                float max = age1 + malemax;
                num = GenMath.FlatHill(0.2f, min, lower, upper, max, 0.2f, age2);
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
}