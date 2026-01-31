namespace MaskGame.Simulation
{
    public struct DeterministicRng
    {
        private const uint DefaultSeed = 0x6D2B79F5u;

        private uint state;

        public DeterministicRng(uint seed)
        {
            state = seed == 0 ? DefaultSeed : seed;
        }

        public uint State => state;

        public static DeterministicRng Create(uint rootSeed, uint stream)
        {
            return new DeterministicRng(Mix(rootSeed, stream));
        }

        public uint NextUInt()
        {
            uint x = state;
            if (x == 0)
                x = DefaultSeed;

            x ^= x << 13;
            x ^= x >> 17;
            x ^= x << 5;
            state = x;
            return x;
        }

        public int NextInt(int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive)
                return minInclusive;

            uint range = (uint)(maxExclusive - minInclusive);
            return (int)(NextUInt() % range) + minInclusive;
        }

        public static uint Mix(uint a, uint b)
        {
            uint x = a;
            x ^= b + 0x9E3779B9u + (x << 6) + (x >> 2);
            x ^= x >> 16;
            x *= 0x7feb352du;
            x ^= x >> 15;
            x *= 0x846ca68bu;
            x ^= x >> 16;
            return x == 0 ? 1u : x;
        }
    }
}
