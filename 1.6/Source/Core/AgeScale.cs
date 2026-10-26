using System;

namespace RomanceAgeFix
{
    internal static class AgeScale
    {
        internal static float EquivalentHumanAge(float age, float lifespan, float lifespanFactor, float humanLifespan)
        {
            if (!IsPositiveFinite(age)) return 0f;
            if (!IsPositiveFinite(humanLifespan)) humanLifespan = 80f;
            if (!IsPositiveFinite(lifespan)) lifespan = humanLifespan;
            if (!IsPositiveFinite(lifespanFactor)) lifespanFactor = 1f;

            // Divide separately in double precision so invalid or extreme
            // modded lifespan values cannot introduce a NaN or infinity.
            double equivalent = (double)age / lifespan / lifespanFactor * humanLifespan;
            return (float)Math.Min(equivalent, float.MaxValue);
        }

        internal static bool NeedsAgeReversal(float equivalentHumanAge)
        {
            // The result is already measured in human years, not a 0..1 ratio.
            return equivalentHumanAge >= 25f;
        }

        private static bool IsPositiveFinite(float value)
        {
            return value > 0f && !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
