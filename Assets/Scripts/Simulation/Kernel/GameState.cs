using MaskGame.Simulation;

namespace MaskGame.Simulation.Kernel
{
    public enum GameKernelPhase : byte
    {
        AwaitingAnswer = 0,
        AwaitingDayAdvance = 1,
        GameWon = 2,
        GameLost = 3,
    }

    public struct GameState
    {
        public uint Seed;
        public int CurrentDay;
        public int EncounterIndexInDay;
        public int CurrentEncounterId;
        public int Health;
        public int TotalAnswers;
        public int CorrectAnswers;
        public DeterministicRng EncounterRng;
        public byte HasEloquence;
        public byte EloquenceUsedThisEncounter;
        public GameKernelPhase Phase;

        public int[] DayDeck;
        public int RemainingInDayDeck;
        public int[] PoolScratch;
    }
}
