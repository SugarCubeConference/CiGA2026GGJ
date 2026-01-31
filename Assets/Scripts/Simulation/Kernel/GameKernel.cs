using System;
using MaskGame.Simulation;

namespace MaskGame.Simulation.Kernel
{
    public static class GameKernel
    {
        private const ulong Fnv64Offset = 14695981039346656037UL;
        private const ulong Fnv64Prime = 1099511628211UL;

        public static GameState NewGame(uint seed, in GameRules rules, int encounterCount)
        {
            int deckSize = Math.Min(rules.DayEnc, encounterCount);
            GameState state = new GameState
            {
                Seed = seed,
                CurrentDay = 1,
                DayIdx = 0,
                Health = rules.InitialHealth,
                TotalAnswers = 0,
                CorrectAnswers = 0,
                EncounterRng = DeterministicRng.Create(seed, DeterminismStreams.Encounters),
                HasElo = 0,
                EloUsed = 0,
                Phase = GamePhase.WaitAns,
                DayDeck = new int[deckSize],
                PoolScratch = new int[encounterCount],
            };

            BeginDay(ref state, deckSize);
            return state;
        }

        public static void Apply(
            ref GameState state,
            in SimulationCommand command,
            in GameRules rules,
            EncounterDefinition[] encounters
        )
        {
            if (state.Phase == GamePhase.GameWon || state.Phase == GamePhase.GameLost)
                return;

            switch (command.Type)
            {
                case CmdType.SelectMask:
                    ApplyAnswer(ref state, command.Int0, false, rules, encounters);
                    break;
                case CmdType.Timeout:
                    ApplyAnswer(ref state, 0, true, rules, encounters);
                    break;
                case CmdType.AdvanceDay:
                    ApplyDay(ref state, rules);
                    break;
                case CmdType.Heal:
                    ApplyHeal(ref state, command.Int0, rules);
                    break;
            }
        }

        public static ulong HashState(in GameState state)
        {
            ulong hash = Fnv64Offset;
            Mix(ref hash, state.Seed);
            Mix(ref hash, (uint)state.CurrentDay);
            Mix(ref hash, (uint)state.DayIdx);
            Mix(ref hash, (uint)state.EncId);
            Mix(ref hash, (uint)state.Health);
            Mix(ref hash, (uint)state.TotalAnswers);
            Mix(ref hash, (uint)state.CorrectAnswers);
            Mix(ref hash, state.EncounterRng.State);
            Mix(ref hash, state.HasElo);
            Mix(ref hash, state.EloUsed);
            Mix(ref hash, (uint)state.Phase);
            Mix(ref hash, (uint)state.DeckLeft);

            int[] deck = state.DayDeck;
            for (int i = 0; i < deck.Length; i++)
            {
                Mix(ref hash, (uint)deck[i]);
            }

            return hash;
        }

        private static void ApplyDay(ref GameState state, in GameRules rules)
        {
            if (state.Phase != GamePhase.WaitDay)
                return;

            state.CurrentDay++;
            state.DayIdx = 0;
            state.Phase = GamePhase.WaitAns;
            BeginDay(ref state, state.DayDeck.Length);
        }

        private static void ApplyAnswer(
            ref GameState state,
            int maskIndex,
            bool isTimeout,
            in GameRules rules,
            EncounterDefinition[] encounters
        )
        {
            if (state.Phase != GamePhase.WaitAns)
                return;

            EncounterDefinition encounter = encounters[state.EncId];
            bool isCorrect = !isTimeout && maskIndex == encounter.CorrectMask;
            bool isNeutral = !isTimeout && encounter.IsNeutral(maskIndex);

            state.TotalAnswers++;
            if (!isCorrect && !isNeutral)
            {
                if (state.HasElo != 0 && state.EloUsed == 0)
                {
                    state.EloUsed = 1;
                    return;
                }
            }

            if (isCorrect)
            {
                state.CorrectAnswers++;
            }
            else if (!isNeutral)
            {
                state.Health -= rules.BatteryPenalty;
                if (state.Health <= 0)
                {
                    state.Phase = GamePhase.GameLost;
                    return;
                }
            }

            state.DayIdx++;
            state.DeckLeft--;
            if (state.DeckLeft > 0)
            {
                state.EncId = state.DayDeck[state.DeckLeft - 1];
                state.EloUsed = 0;
                return;
            }

            if (state.CurrentDay >= rules.TotalDays)
            {
                state.Phase = GamePhase.GameWon;
            }
            else
            {
                state.Phase = GamePhase.WaitDay;
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

            state.DeckLeft = deckSize;
            state.EncId = state.DayDeck[state.DeckLeft - 1];
            state.EloUsed = 0;
        }

        private static void ApplyHeal(ref GameState state, int amount, in GameRules rules)
        {
            if (amount <= 0)
                return;

            int value = state.Health + amount;
            if (value > rules.MaxHealth)
            {
                state.Health = rules.MaxHealth;
            }
            else
            {
                state.Health = value;
            }
        }

        private static void Mix(ref ulong hash, uint value)
        {
            hash ^= value;
            hash *= Fnv64Prime;
        }
    }
}
