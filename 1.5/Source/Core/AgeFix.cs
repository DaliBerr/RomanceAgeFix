using Verse;
using HarmonyLib;
using RimWorld;
using System;

namespace RomanceAgeFix{
    public static class AgeFix
    {
        public static float RomanceAgeOverride(Pawn pawn){
            float expectancyLiftHuman = ThingDefOf.Human.race.lifeExpectancy;
            float expectancyLife = pawn.RaceProps.lifeExpectancy;
            // float expectancyLife2 = otherPawn.RaceProps.lifeExpectancy;

            float RomanceAge = pawn.ageTracker.AgeBiologicalYearsFloat;
            // float age2 = otherPawn.ageTracker.AgeBiologicalYearsFloat;

            if(expectancyLife > expectancyLiftHuman && RomanceAge > expectancyLiftHuman){
                RomanceAge /= expectancyLife;
                if(RomanceAge < 16f){
                    RomanceAge = 16f;
                }
            }         
            return RomanceAge;
        }
    }
}