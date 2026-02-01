namespace MaskGame.Simulation.Kernel
{
    public enum CmdType : byte
    {
        SelectMask = 0,
        Timeout = 1,
        AdvanceDay = 2,
        Heal = 3,
    }

    public readonly struct SimulationCommand
    {
        public readonly CmdType Type;
        public readonly int Int0;

        public SimulationCommand(CmdType type, int int0 = 0)
        {
            Type = type;
            Int0 = int0;
        }

        public static SimulationCommand SelectMask(int maskIndex)
        {
            return new SimulationCommand(CmdType.SelectMask, maskIndex);
        }

        public static SimulationCommand Timeout()
        {
            return new SimulationCommand(CmdType.Timeout);
        }

        public static SimulationCommand AdvanceDay()
        {
            return new SimulationCommand(CmdType.AdvanceDay);
        }

        public static SimulationCommand Heal(int amount)
        {
            return new SimulationCommand(CmdType.Heal, amount);
        }
    }
}
