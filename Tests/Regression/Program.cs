using RomanceAgeFix;
using RimWorld;
using Verse;

int checks = 0;
void Check(bool condition, string message)
{
    if (!condition) throw new Exception(message);
    checks++;
}
Pawn PawnAt(float age, float life = 80f, float factor = 1f, float humanLife = 80f)
{
    ThingDefOf.Human = new ThingDef { race = new RaceProperties { lifeExpectancy = humanLife } };
    DefDatabase<StatDef>.Value = new StatDef();
    return new Pawn { ageTracker = new Pawn_AgeTracker { AgeBiologicalYearsFloat = age },
        RaceProps = new RaceProperties { lifeExpectancy = life }, LifespanFactor = factor };
}

(float Age, float Life, float Factor, float HumanLife, float Equivalent, bool Reversal)[] cases =
{
    (20f, 80f, 1f, 80f, 20f, false),
    (24f, 80f, 1f, 80f, 24f, false),
    (25f, 80f, 1f, 80f, 25f, true),
    (40f, 80f, 1f, 80f, 40f, true),
    (300f, 1000f, 1f, 80f, 24f, false),
    (312.5f, 1000f, 1f, 80f, 25f, true),
    (400f, 1000f, 1f, 80f, 32f, true),
    (49f, 80f, 2f, 80f, 24.5f, false),
    (50f, 80f, 2f, 80f, 25f, true),
    (200f, 1000f, 0.5f, 80f, 32f, true),
    (320f, 1000f, 1f, 100f, 32f, true),
    (30f, 0f, 1f, 80f, 30f, true),
    (30f, -10f, 1f, 80f, 30f, true),
    (30f, 80f, 0f, 80f, 30f, true),
    (30f, 80f, -2f, 80f, 30f, true),
    (30f, float.NaN, 1f, 80f, 30f, true),
    (30f, 80f, float.PositiveInfinity, 80f, 30f, true),
    (30f, 80f, 1f, float.NaN, 30f, true),
    (float.NaN, 80f, 1f, 80f, 0f, false),
    (float.PositiveInfinity, 80f, 1f, 80f, 0f, false),
    (-10f, 80f, 1f, 80f, 0f, false)
};
foreach (var test in cases)
{
    Pawn pawn = PawnAt(test.Age, test.Life, test.Factor, test.HumanLife);
    float actual = AgeFix.GetEquivalentHumanAge(pawn);
    Check(Math.Abs(actual - test.Equivalent) < 0.0001f, $"Wrong equivalent age for {test}");
    Check(AgeScale.NeedsAgeReversal(AgeFix.AgeReversalDemandedAgeOverride(pawn)) == test.Reversal,
        $"Age-reversal years/ratio regression for {test}");
    Check(AgeFix.RomanceAgeOverride(pawn) >= 16f, "Existing equivalent-age floor changed");
}

Pawn missingStat = PawnAt(40f, 80f, 20f);
DefDatabase<StatDef>.Value = null;
Check(AgeFix.GetEquivalentHumanAge(missingStat) == 40f && missingStat.StatQueries == 0, "Missing stat was queried instead of using a neutral multiplier");
Check(AgeFix.GetEquivalentHumanAge(null) == 0f, "Null pawn was not handled");
Pawn missingTracker = PawnAt(40f);
missingTracker.ageTracker = null;
Check(AgeFix.GetEquivalentHumanAge(missingTracker) == 0f, "Missing age tracker was not handled");
Pawn missingRace = PawnAt(40f);
missingRace.RaceProps = null;
Check(AgeFix.GetEquivalentHumanAge(missingRace) == 0f, "Missing race properties were not handled");
Pawn extreme = PawnAt(float.MaxValue, float.Epsilon, float.Epsilon);
Check(float.IsFinite(AgeFix.GetEquivalentHumanAge(extreme)), "Extreme modded values overflowed to infinity");
Console.WriteLine($"PASS: {checks} production age-scaling, threshold, lifespan and invalid-data checks.");

namespace Verse
{
    public class Pawn
    {
        public Pawn_AgeTracker ageTracker;
        public RaceProperties RaceProps;
        public float LifespanFactor;
        public int StatQueries;
        public float GetStatValue(StatDef stat) { StatQueries++; return LifespanFactor; }
    }
    public class Pawn_AgeTracker { public float AgeBiologicalYearsFloat; }
    public class RaceProperties { public float lifeExpectancy; }
    public class ThingDef { public RaceProperties race; }
    public static class DefDatabase<T> { public static T Value; public static T GetNamedSilentFail(string name) => Value; }
}
namespace RimWorld
{
    public class StatDef { }
    public static class ThingDefOf { public static ThingDef Human; }
}
