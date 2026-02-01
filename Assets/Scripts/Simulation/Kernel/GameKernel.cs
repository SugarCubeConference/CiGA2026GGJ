using System;
using MaskGame.Simulation;

namespace MaskGame.Simulation.Kernel
{
    public static class GameKernel
    {
        private const ulong Fnv1a64Offset = 14695981039346656037UL; // FNV-1a 64-bit
        private const ulong Fnv1a64Prime = 1099511628211UL; // FNV-1a 64-bit

        public static GameState NewGame(uint seed, in GameRules rules, int encounterCount)
        {
            return NewGame(seed, in rules, encounterCount, 0, false);
        }

        public static GameState NewGame(
            uint seed,
            in GameRules rules,
            int normalCount,
            int bossCount,
            bool startBoss
        )
        {
            int totalCount = normalCount + bossCount;
            int deckCap = Math.Min(rules.DayEnc, Math.Max(normalCount, bossCount));

            GameState state = new GameState
            {
                Seed = seed,
                Mode = GameMode.Normal,
                NormalCount = normalCount,
                BossCount = bossCount,
                CurrentDay = 1,
                DayIdx = 0,
                Health = rules.InitialHealth,
                TotalAnswers = 0,
                CorrectAnswers = 0,
                EncounterRng = DeterministicRng.Create(seed, DeterminismStreams.Encounters),
                HasElo = 0,
                EloUsed = 0,
                Phase = GamePhase.WaitAns,
                UsedCount = 0,
                UsedBits = new uint[(totalCount + 31) / 32],
                DayDeck = new int[deckCap],
                DeckSize = 0,
                PoolScratch = new int[totalCount],
            };

            if (bossCount > 0 && (startBoss || normalCount <= 0))
                state.Mode = GameMode.Boss;

            if (state.Mode == GameMode.Boss)
            {
                BeginBoss(ref state);
            }
            else
            {
                BeginNormal(ref state);
            }

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
            ulong hash = Fnv1a64Offset;
            Mix(ref hash, state.Seed);
            Mix(ref hash, (uint)state.Mode);
            Mix(ref hash, (uint)state.NormalCount);
            Mix(ref hash, (uint)state.BossCount);
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
            Mix(ref hash, (uint)state.UsedCount);
            Mix(ref hash, (uint)state.DeckSize);
            Mix(ref hash, (uint)state.DeckLeft);

            int[] deck = state.DayDeck;
            for (int i = 0; i < state.DeckSize; i++)
            {
                Mix(ref hash, (uint)deck[i]);
            }

            uint[] usedBits = state.UsedBits;
            for (int i = 0; i < usedBits.Length; i++)
            {
                Mix(ref hash, usedBits[i]);
            }

            return hash;
        }

        private static void ApplyDay(ref GameState state, in GameRules rules)
        {
            if (state.Phase != GamePhase.WaitDay)
                return;

            state.CurrentDay++;
            state.DayIdx = 0;
            BeginNormal(ref state);
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
                int penalty =
                    state.Mode == GameMode.Boss ? rules.BossPenalty : rules.BatteryPenalty;
                state.Health -= penalty;
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
                MarkUsed(ref state, state.EncId);
                state.EloUsed = 0;
                return;
            }

            if (state.Mode == GameMode.Boss)
            {
                BeginBoss(ref state);
                return;
            }

            if (state.DeckSize < rules.DayEnc)
            {
                if (state.BossCount > 0)
                {
                    state.Mode = GameMode.Boss;
                    state.DayIdx = 0;
                    BeginBoss(ref state);
                    return;
                }

                state.Phase = GamePhase.GameWon;
                return;
            }

            state.Phase = GamePhase.WaitDay;
        }

        private static bool BeginDeck(ref GameState state, int start, int count)
        {
            int[] scratch = state.PoolScratch;
            int availCount = 0;
            int end = start + count;

            for (int i = start; i < end; i++)
            {
                if (!IsUsed(in state, i))
                {
                    scratch[availCount] = i;
                    availCount++;
                }
            }

            state.DeckSize = 0;
            state.DeckLeft = 0;
            if (availCount <= 0)
                return false;

            for (int i = availCount - 1; i > 0; i--)
            {
                int j = state.EncounterRng.NextInt(0, i + 1);
                int tmp = scratch[i];
                scratch[i] = scratch[j];
                scratch[j] = tmp;
            }

            int deckSize = Math.Min(state.DayDeck.Length, availCount);
            for (int i = 0; i < deckSize; i++)
            {
                state.DayDeck[i] = scratch[i];
            }

            state.DeckSize = deckSize;
            state.DeckLeft = deckSize;
            state.Phase = GamePhase.WaitAns;
            state.EncId = state.DayDeck[deckSize - 1];
            MarkUsed(ref state, state.EncId);
            state.EloUsed = 0;
            return true;
        }

        private static void BeginNormal(ref GameState state)
        {
            if (BeginDeck(ref state, 0, state.NormalCount))
                return;

            if (state.BossCount > 0)
            {
                state.Mode = GameMode.Boss;
                state.DayIdx = 0;
                BeginBoss(ref state);
                return;
            }

            state.Phase = GamePhase.GameWon;
        }

        private static void BeginBoss(ref GameState state)
        {
            if (BeginDeck(ref state, state.NormalCount, state.BossCount))
                return;

            state.Phase = GamePhase.GameWon;
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
            hash *= Fnv1a64Prime;
        }

        private static bool IsUsed(in GameState state, int id)
        {
            uint[] bits = state.UsedBits;
            int word = id >> 5;
            uint mask = 1u << (id & 31);
            return (bits[word] & mask) != 0;
        }

        private static void MarkUsed(ref GameState state, int id)
        {
            uint[] bits = state.UsedBits;
            int word = id >> 5;
            uint mask = 1u << (id & 31);
            if ((bits[word] & mask) != 0)
                return;

            bits[word] |= mask;
            state.UsedCount++;
        }
    }
}
