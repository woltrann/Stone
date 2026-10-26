namespace TidyUpFramework.Demo.Editor.ContentKit
{
    internal struct TidyRandom
    {
        private const uint SeedMultiplier = 747796405u;
        private const uint SeedIncrement = 2891336453u;
        private const uint MantissaMask = 0xFFFFFF;
        private const float MantissaScale = 1f / 16777216f;

        private uint state;

        public TidyRandom(int seed)
        {
            unchecked
            {
                state = (uint)seed * SeedMultiplier + SeedIncrement;
            }

            if (state == 0u)
            {
                state = SeedIncrement;
            }
        }

        public float Next01()
        {
            state ^= state << 13;
            state ^= state >> 17;
            state ^= state << 5;
            return (state & MantissaMask) * MantissaScale;
        }

        public float Signed()
        {
            return Next01() * 2f - 1f;
        }

        public float Range(float min, float max)
        {
            return min + (max - min) * Next01();
        }

        public int Range(int minInclusive, int maxExclusive)
        {
            int span = maxExclusive - minInclusive;
            if (span <= 0)
            {
                return minInclusive;
            }

            int value = minInclusive + (int)(Next01() * span);
            return value >= maxExclusive ? maxExclusive - 1 : value;
        }

        public bool Chance(float probability)
        {
            return Next01() < probability;
        }
    }
}
