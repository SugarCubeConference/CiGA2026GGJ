namespace MaskGame.Simulation.Kernel
{
    public enum SimulationCommandType : byte
    {
        SelectMask = 0,
        Timeout = 1,
        AdvanceDay = 2,
    }

    public readonly struct SimulationCommand
    {
        public readonly SimulationCommandType Type;
        public readonly int Int0;

        public SimulationCommand(SimulationCommandType type, int int0 = 0)
        {
            Type = type;
            Int0 = int0;
        }

        public static SimulationCommand SelectMask(int maskIndex)
        {
            return new SimulationCommand(SimulationCommandType.SelectMask, maskIndex);
        }

        public static SimulationCommand Timeout()
        {
            return new SimulationCommand(SimulationCommandType.Timeout);
        }

        public static SimulationCommand AdvanceDay()
        {
            return new SimulationCommand(SimulationCommandType.AdvanceDay);
        }
    }
}

