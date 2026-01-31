namespace MaskGame.Simulation.Kernel
{
    public readonly struct GameKernelRules
    {
        public readonly int TotalDays;
        public readonly int EncountersPerDay;
        public readonly int InitialHealth;
        public readonly int MaxHealth;
        public readonly int BatteryPenalty;

        public GameKernelRules(
            int totalDays,
            int encountersPerDay,
            int initialHealth,
            int maxHealth,
            int batteryPenalty
        )
        {
            TotalDays = totalDays;
            EncountersPerDay = encountersPerDay;
            InitialHealth = initialHealth;
            MaxHealth = maxHealth;
            BatteryPenalty = batteryPenalty;
        }
    }
}

