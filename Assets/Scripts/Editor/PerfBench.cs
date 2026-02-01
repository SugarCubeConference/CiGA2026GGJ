using System;
using System.Collections.Generic;
using MaskGame.Data;
using MaskGame.Simulation;
using Kernel = MaskGame.Simulation.Kernel;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;

namespace MaskGame.Editor
{
    public static class PerfBench
    {
        private const string MenuRoot = "Tools/Mask Game/Perf Bench/";
        private const string EncounterRes = "Encounters";
        private const int MaskCount = 4;
        private const uint BenchStream = 0x42454E43u; // "BENC"
        private const uint FuzzStream = 0x46555A5Au; // "FUZZ"

	        private readonly struct BenchRes
	        {
	            public readonly int Seeds;
	            public readonly int Rounds;
	            public readonly int Steps;
	            public readonly int Gen0Gc;
	            public readonly long TimeUs;
	            public readonly long UsedDelta;
	            public readonly long HeapDelta;

	            public BenchRes(
	                int seeds,
	                int rounds,
	                int steps,
	                int gen0Gc,
	                long timeUs,
	                long usedDelta,
	                long heapDelta
	            )
	            {
	                Seeds = seeds;
	                Rounds = rounds;
	                Steps = steps;
	                Gen0Gc = gen0Gc;
	                TimeUs = timeUs;
	                UsedDelta = usedDelta;
	                HeapDelta = heapDelta;
	            }
	        }

        [MenuItem(MenuRoot + "Run (5000)")]
        public static void Run5000()
        {
            Run(5000);
        }

        public static void Run(int seeds)
        {
            if (!LoadRes(out EncounterData[] encData))
                return;

            Kernel.GameRules rules = LoadRules();
            Kernel.EncounterDefinition[] defs = BuildDefs(encData);

            uint[] seedList = BuildSeeds(seeds);

            const int rounds = 32;
            BenchRes legacy = BenchLegacy(seedList, rounds, rules, encData);
            BenchRes kernel = BenchKernel(seedList, rounds, rules, defs);

            Debug.Log(Format(legacy, kernel));
        }

        private static Kernel.GameRules LoadRules()
        {
            GameConfig cfg = new GameConfig();
            return new Kernel.GameRules(
                cfg.totalDays,
                cfg.encountersPerDay,
                cfg.initialHealth,
                cfg.maxHealth,
                cfg.batteryPenalty
            );
        }

        private static bool LoadRes(out EncounterData[] items)
        {
            items = Resources.LoadAll<EncounterData>(EncounterRes);
            if (items.Length == 0)
            {
                Debug.LogError($"PerfBench: no EncounterData at Resources/{EncounterRes}");
                return false;
            }

            Array.Sort(items, (a, b) => string.CompareOrdinal(a.name, b.name));
            return true;
        }

        private static uint[] BuildSeeds(int count)
        {
            uint root = unchecked((uint)DateTime.UtcNow.Ticks);
            DeterministicRng rng = DeterministicRng.Create(root, BenchStream);
            uint[] list = new uint[count];
            for (int i = 0; i < count; i++)
            {
                list[i] = rng.NextUInt();
            }

            return list;
        }

        private static Kernel.EncounterDefinition[] BuildDefs(EncounterData[] src)
        {
            Kernel.EncounterDefinition[] defs = new Kernel.EncounterDefinition[src.Length];
            for (int i = 0; i < src.Length; i++)
            {
                EncounterData enc = src[i];

                byte neutralBits = 0;
                MaskType[] neutral = enc.neutralMasks;
                if (neutral != null)
                {
                    for (int n = 0; n < neutral.Length; n++)
                    {
                        neutralBits = (byte)(neutralBits | (1 << (int)neutral[n]));
                    }
                }

                defs[i] = new Kernel.EncounterDefinition((int)enc.correctMask, neutralBits);
            }

            return defs;
        }

        private static BenchRes BenchKernel(
            uint[] seeds,
            int rounds,
            in Kernel.GameRules rules,
            Kernel.EncounterDefinition[] defs
        )
        {
            int warm = seeds.Length < 50 ? seeds.Length : 50;
            for (int i = 0; i < warm; i++)
            {
                SimKernel(seeds[i], in rules, defs, out _);
            }

	            ForceGc();
	            long used0 = Profiler.GetMonoUsedSizeLong();
	            long heap0 = Profiler.GetMonoHeapSizeLong();
	            int gen0Start = GC.CollectionCount(0);
	            long t0 = System.Diagnostics.Stopwatch.GetTimestamp();

            int steps = 0;
            for (int r = 0; r < rounds; r++)
            {
                for (int i = 0; i < seeds.Length; i++)
                {
                    uint seed = seeds[i];
                    SimKernel(seed, in rules, defs, out int stepCount);
                    steps += stepCount;
                }
            }

            long t1 = System.Diagnostics.Stopwatch.GetTimestamp();
            long us = (t1 - t0) * 1000000 / System.Diagnostics.Stopwatch.Frequency;
            long used1 = Profiler.GetMonoUsedSizeLong();
            long heap1 = Profiler.GetMonoHeapSizeLong();

	            return new BenchRes(
	                seeds: seeds.Length,
	                rounds: rounds,
	                steps: steps,
	                gen0Gc: GC.CollectionCount(0) - gen0Start,
	                timeUs: us,
	                usedDelta: used1 - used0,
	                heapDelta: heap1 - heap0
	            );
	        }

        private static void SimKernel(
            uint seed,
            in Kernel.GameRules rules,
            Kernel.EncounterDefinition[] defs,
            out int steps
        )
        {
            Kernel.GameState state = Kernel.GameKernel.NewGame(seed, in rules, defs.Length);
            DeterministicRng rng = DeterministicRng.Create(seed, FuzzStream);

            if ((rng.NextUInt() & 1u) != 0)
                state.HasElo = 1;

            steps = 0;
            for (int i = 0; i < 4096; i++)
            {
                Kernel.SimulationCommand cmd = GenCmd(ref rng, in state);
                Kernel.GameKernel.Apply(ref state, in cmd, in rules, defs);
                steps++;

                if (state.Phase == Kernel.GamePhase.GameWon || state.Phase == Kernel.GamePhase.GameLost)
                    break;
            }

            _ = Kernel.GameKernel.HashState(in state);
        }

        private static BenchRes BenchLegacy(
            uint[] seeds,
            int rounds,
            in Kernel.GameRules rules,
            EncounterData[] encData
        )
        {
            int warm = seeds.Length < 50 ? seeds.Length : 50;
            for (int i = 0; i < warm; i++)
            {
                SimLegacy(seeds[i], in rules, encData, out _, out _);
            }

	            ForceGc();
	            long used0 = Profiler.GetMonoUsedSizeLong();
	            long heap0 = Profiler.GetMonoHeapSizeLong();
	            int gen0Start = GC.CollectionCount(0);
	            long t0 = System.Diagnostics.Stopwatch.GetTimestamp();

            int steps = 0;
            for (int r = 0; r < rounds; r++)
            {
                for (int i = 0; i < seeds.Length; i++)
                {
                    SimLegacy(seeds[i], in rules, encData, out _, out int stepCount);
                    steps += stepCount;
                }
            }

            long t1 = System.Diagnostics.Stopwatch.GetTimestamp();
            long us = (t1 - t0) * 1000000 / System.Diagnostics.Stopwatch.Frequency;
            long used1 = Profiler.GetMonoUsedSizeLong();
            long heap1 = Profiler.GetMonoHeapSizeLong();

	            return new BenchRes(
	                seeds: seeds.Length,
	                rounds: rounds,
	                steps: steps,
	                gen0Gc: GC.CollectionCount(0) - gen0Start,
	                timeUs: us,
	                usedDelta: used1 - used0,
	                heapDelta: heap1 - heap0
	            );
	        }

        private static void ForceGc()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        private static void SimLegacy(
            uint seed,
            in Kernel.GameRules rules,
            EncounterData[] encData,
            out int phase,
            out int steps
        )
        {
            DeterministicRng encRng = DeterministicRng.Create(seed, DeterminismStreams.Encounters);
            DeterministicRng drvRng = DeterministicRng.Create(seed, FuzzStream);

            bool hasElo = (drvRng.NextUInt() & 1u) != 0;

            HashSet<int> used = new HashSet<int>(encData.Length);
            List<int> deck = new List<int>(rules.DayEnc);

            int day = 1;
            int hp = rules.InitialHealth;
            steps = 0;

            while (true)
            {
                if (hp <= 0)
                {
                    phase = 3;
                    return;
                }

                if (day > rules.TotalDays)
                {
                    phase = 2;
                    return;
                }

                deck.Clear();
                List<int> avail = new List<int>(encData.Length);
                for (int i = 0; i < encData.Length; i++)
                {
                    if (!used.Contains(i))
                        avail.Add(i);
                }

                if (avail.Count == 0)
                {
                    phase = 2;
                    return;
                }

                for (int i = avail.Count - 1; i > 0; i--)
                {
                    int j = encRng.NextInt(0, i + 1);
                    int tmp = avail[i];
                    avail[i] = avail[j];
                    avail[j] = tmp;
                }

                int take = avail.Count < rules.DayEnc ? avail.Count : rules.DayEnc;
                for (int i = 0; i < take; i++)
                {
                    deck.Add(avail[i]);
                }

                while (deck.Count > 0)
                {
                    int encId = deck[deck.Count - 1];
                    deck.RemoveAt(deck.Count - 1);

                    bool eloUsed = false;
                    while (true)
                    {
                        steps++;
                        bool timeout = drvRng.NextInt(0, 100) < 10;
                        int maskIndex = timeout ? 0 : drvRng.NextInt(0, MaskCount);

                        EncounterData enc = encData[encId];
                        bool isCorrect = !timeout && maskIndex == (int)enc.correctMask;
                        bool isNeutral = !timeout && IsNeutral(enc, maskIndex);

                        if (isCorrect || isNeutral)
                            break;

                        if (hasElo && !eloUsed)
                        {
                            eloUsed = true;
                            continue;
                        }

                        hp -= rules.BatteryPenalty;
                        break;
                    }

                    used.Add(encId);

                    if (hp <= 0)
                        break;
                }

                if (hp <= 0)
                    continue;

                day++;
            }
        }

        private static bool IsNeutral(EncounterData enc, int maskIndex)
        {
            MaskType[] neutral = enc.neutralMasks;
            if (neutral == null)
                return false;

            for (int i = 0; i < neutral.Length; i++)
            {
                if ((int)neutral[i] == maskIndex)
                    return true;
            }

            return false;
        }

        private static Kernel.SimulationCommand GenCmd(ref DeterministicRng rng, in Kernel.GameState state)
        {
            if (state.Phase == Kernel.GamePhase.WaitAns)
            {
                int roll = rng.NextInt(0, 100);
                if (roll < 10)
                    return Kernel.SimulationCommand.Timeout();

                int maskIndex = rng.NextInt(0, MaskCount);
                return Kernel.SimulationCommand.SelectMask(maskIndex);
            }

            if (state.Phase == Kernel.GamePhase.WaitDay)
            {
                if (rng.NextInt(0, 4) == 0)
                    return Kernel.SimulationCommand.Heal(1);

                return Kernel.SimulationCommand.AdvanceDay();
            }

            return Kernel.SimulationCommand.AdvanceDay();
        }

        private static string Format(in BenchRes legacy, in BenchRes kernel)
        {
            return
                "PERF Bench (batchmode)\n" +
                FormatOne("Legacy", in legacy) + "\n" +
                FormatOne("Kernel", in kernel);
        }

	        private static string FormatOne(string name, in BenchRes res)
	        {
	            double ms = res.TimeUs / 1000.0;
	            int games = res.Seeds * res.Rounds;
	            double avgMs = games > 0 ? ms / games : 0;
	            double stepUs = res.Steps > 0 ? (double)res.TimeUs / res.Steps : 0;
	            double gameBytes = games > 0 ? (double)res.UsedDelta / games : 0;
	            double stepBytes = res.Steps > 0 ? (double)res.UsedDelta / res.Steps : 0;

	            return
	                $"{name}: seeds={res.Seeds} rounds={res.Rounds} games={games} steps={res.Steps} timeUs={res.TimeUs} timeMs={ms:F3} " +
	                $"avgMs={avgMs:F4} stepUs={stepUs:F4} usedDelta={res.UsedDelta} heapDelta={res.HeapDelta} " +
	                $"gameBytes={gameBytes:F2} stepBytes={stepBytes:F4} gen0Gc={res.Gen0Gc}";
	        }
	    }
	}
