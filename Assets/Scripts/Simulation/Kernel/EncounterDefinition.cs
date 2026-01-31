namespace MaskGame.Simulation.Kernel
{
    public readonly struct EncounterDefinition
    {
        public readonly int CorrectMaskIndex;
        public readonly byte NeutralMaskBits;

        public EncounterDefinition(int correctMaskIndex, byte neutralMaskBits)
        {
            CorrectMaskIndex = correctMaskIndex;
            NeutralMaskBits = neutralMaskBits;
        }

        public bool IsNeutral(int maskIndex)
        {
            return (NeutralMaskBits & (1 << maskIndex)) != 0;
        }
    }
}

