namespace MaskGame.Simulation.Kernel
{
    public readonly struct GameRules
    {
        public readonly int TotalDays;
        public readonly int DayEnc;
        public readonly int InitialHealth;
        public readonly int MaxHealth;
        public readonly int BatteryPenalty;

        public GameRules(
            int totalDays,
            int dayEnc,
            int initialHealth,
            int maxHealth,
            int batteryPenalty
        )
        {
            TotalDays = totalDays;
            DayEnc = dayEnc;
            InitialHealth = initialHealth;
            MaxHealth = maxHealth;
            BatteryPenalty = batteryPenalty;
        }
    }
}
