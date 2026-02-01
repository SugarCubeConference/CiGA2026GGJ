using MaskGame.Simulation;

namespace MaskGame.Simulation.Kernel
{
    public enum GamePhase : byte
    {
        WaitAns = 0,
        WaitDay = 1,
        GameWon = 2,
        GameLost = 3,
    }

    public struct GameState
    {
        public uint Seed;
        public int CurrentDay;
        public int DayIdx;
        public int EncId;
        public int Health;
        public int TotalAnswers;
        public int CorrectAnswers;
        public DeterministicRng EncounterRng;
        public byte HasElo;
        public byte EloUsed;
        public GamePhase Phase;

        public int UsedCount;
        public uint[] UsedBits;

        public int[] DayDeck;
        public int DeckSize;
        public int DeckLeft;
        public int[] PoolScratch;
    }
}
