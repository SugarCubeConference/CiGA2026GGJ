using System;
using MaskGame.Simulation;

namespace MaskGame.Simulation.Kernel
{
    public static class GameKernel
    {
        public static GameState CreateNewGame(uint seed, in GameKernelRules rules, int encounterCount)
        {
            int deckSize = Math.Min(rules.EncountersPerDay, encounterCount);
            GameState state = new GameState
            {
                Seed = seed,
                CurrentDay = 1,
                EncounterIndexInDay = 0,
                Health = rules.InitialHealth,
                TotalAnswers = 0,
                CorrectAnswers = 0,
                EncounterRng = DeterministicRng.Create(seed, DeterminismStreams.Encounters),
                Phase = GameKernelPhase.AwaitingAnswer,
                DayDeck = new int[deckSize],
                PoolScratch = new int[encounterCount],
            };

            BeginDay(ref state, deckSize);
            return state;
        }

        public static void Apply(
            ref GameState state,
            in SimulationCommand command,
            in GameKernelRules rules,
            EncounterDefinition[] encounters
        )
        {
            if (state.Phase == GameKernelPhase.GameWon || state.Phase == GameKernelPhase.GameLost)
                return;

            switch (command.Type)
            {
                case SimulationCommandType.SelectMask:
                    ApplyAnswer(ref state, command.Int0, false, rules, encounters);
                    break;
                case SimulationCommandType.Timeout:
                    ApplyAnswer(ref state, 0, true, rules, encounters);
                    break;
                case SimulationCommandType.AdvanceDay:
                    ApplyAdvanceDay(ref state, rules);
                    break;
            }
        }

        public static ulong ComputeStateHash(in GameState state)
        {
            ulong hash = 14695981039346656037UL;
            Mix(ref hash, (uint)state.Seed);
            Mix(ref hash, (uint)state.CurrentDay);
            Mix(ref hash, (uint)state.EncounterIndexInDay);
            Mix(ref hash, (uint)state.CurrentEncounterId);
            Mix(ref hash, (uint)state.Health);
            Mix(ref hash, (uint)state.TotalAnswers);
            Mix(ref hash, (uint)state.CorrectAnswers);
            Mix(ref hash, (uint)state.EncounterRng.State);
            Mix(ref hash, (uint)state.Phase);
            Mix(ref hash, (uint)state.RemainingInDayDeck);

            int[] deck = state.DayDeck;
            for (int i = 0; i < deck.Length; i++)
            {
                Mix(ref hash, (uint)deck[i]);
            }

            return hash;
        }

        private static void ApplyAdvanceDay(ref GameState state, in GameKernelRules rules)
        {
            if (state.Phase != GameKernelPhase.AwaitingDayAdvance)
                return;

            state.CurrentDay++;
            state.EncounterIndexInDay = 0;
            state.Phase = GameKernelPhase.AwaitingAnswer;
            BeginDay(ref state, state.DayDeck.Length);
        }

        private static void ApplyAnswer(
            ref GameState state,
            int maskIndex,
            bool isTimeout,
            in GameKernelRules rules,
            EncounterDefinition[] encounters
        )
        {
            if (state.Phase != GameKernelPhase.AwaitingAnswer)
                return;

            EncounterDefinition encounter = encounters[state.CurrentEncounterId];
            bool isCorrect = !isTimeout && maskIndex == encounter.CorrectMaskIndex;
            bool isNeutral = !isTimeout && encounter.IsNeutral(maskIndex);

            state.TotalAnswers++;
            if (isCorrect)
            {
                state.CorrectAnswers++;
                if (state.Health < rules.MaxHealth)
                {
                    state.Health++;
                }
            }
            else if (!isNeutral)
            {
                state.Health -= rules.BatteryPenalty;
                if (state.Health <= 0)
                {
                    state.Phase = GameKernelPhase.GameLost;
                    return;
                }
            }

            state.EncounterIndexInDay++;
            state.RemainingInDayDeck--;
            if (state.RemainingInDayDeck > 0)
            {
                state.CurrentEncounterId = state.DayDeck[state.RemainingInDayDeck - 1];
                return;
            }

            if (state.CurrentDay >= rules.TotalDays)
            {
                state.Phase = GameKernelPhase.GameWon;
            }
            else
            {
                state.Phase = GameKernelPhase.AwaitingDayAdvance;
            }
        }

        private static void BeginDay(ref GameState state, int deckSize)
        {
            int[] pool = state.PoolScratch;
            for (int i = 0; i < pool.Length; i++)
            {
                pool[i] = i;
            }

            for (int i = pool.Length - 1; i > 0; i--)
            {
                int j = state.EncounterRng.NextInt(0, i + 1);
                int tmp = pool[i];
                pool[i] = pool[j];
                pool[j] = tmp;
            }

            for (int i = 0; i < deckSize; i++)
            {
                state.DayDeck[i] = pool[i];
            }

            state.RemainingInDayDeck = deckSize;
            state.CurrentEncounterId = state.DayDeck[state.RemainingInDayDeck - 1];
        }

        private static void Mix(ref ulong hash, uint value)
        {
            hash ^= value;
            hash *= 1099511628211UL;
        }
    }
}
