namespace MaskGame.Simulation.Kernel
{
    public readonly struct GameRules
    {
        public const int DefaultBossPenalty = 2;

        public readonly int TotalDays;
        public readonly int DayEnc;
        public readonly int InitialHealth;
        public readonly int MaxHealth;
        public readonly int BatteryPenalty;
        public readonly int BossPenalty;

        public GameRules(
            int totalDays,
            int dayEnc,
            int initialHealth,
            int maxHealth,
            int batteryPenalty,
            int bossPenalty = DefaultBossPenalty
        )
        {
            TotalDays = totalDays;
            DayEnc = dayEnc;
            InitialHealth = initialHealth;
            MaxHealth = maxHealth;
            BatteryPenalty = batteryPenalty;
            BossPenalty = bossPenalty;
        }
    }
}
