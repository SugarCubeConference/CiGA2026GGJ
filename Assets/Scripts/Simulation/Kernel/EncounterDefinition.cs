namespace MaskGame.Simulation.Kernel
{
    public readonly struct EncounterDefinition
    {
        public readonly int CorrectMask;
        public readonly byte NeutralBits;

        public EncounterDefinition(int correctMask, byte neutralBits)
        {
            CorrectMask = correctMask;
            NeutralBits = neutralBits;
        }

        public bool IsNeutral(int maskIndex)
        {
            return (NeutralBits & (1 << maskIndex)) != 0;
        }
    }
}
