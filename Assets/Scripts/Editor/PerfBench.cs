using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MaskGame.Data;
using MaskGame.Simulation;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using Kernel = MaskGame.Simulation.Kernel;

namespace MaskGame.Editor
{
    public static class PerfBench
    {
        private const string MenuRoot = "Tools/Mask Game/Perf Bench/";
        private const string EncounterRes = "Encounters";
        private const int MaskCount = 4;
        private const uint BenchStream = 0x42454E43u; // "BENC"
        private const uint FuzzStream = 0x46555A5Au; // "FUZZ"
        private const uint ReportRoot = 0x52505430u; // "RPT0"
        private const int RepCount = 5;
        private const int RoundCount = 32;
        private const int WarmCap = 50;
        private const string CsvArg = "-exportCsv";
        private static readonly int[] ReportSizes = new[] { 500, 2000, 5000, 20000 };

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

        [MenuItem(MenuRoot + "Run (Report)")]
        public static void RunReport()
        {
            RunReport(RepCount);
        }

        public static void Run(int seeds)
        {
            if (!LoadRes(out EncounterData[] encData))
                return;

            Kernel.GameRules rules = LoadRules();
            Kernel.EncounterDefinition[] defs = BuildDefs(encData);

            uint[] seedList = BuildSeeds(seeds);

            BenchRes legacy = BenchLegacy(seedList, RoundCount, rules, encData);
            BenchRes kernel = BenchKernel(seedList, RoundCount, rules, defs);

            Debug.Log(Format(legacy, kernel));
        }

        private static void RunReport(int repeats)
        {
            if (!LoadRes(out EncounterData[] encData))
                return;

            Kernel.GameRules rules = LoadRules();
            Kernel.EncounterDefinition[] defs = BuildDefs(encData);

            int maxSeeds = ReportSizes[ReportSizes.Length - 1];
            uint[] seedAll = BuildSeeds(maxSeeds, ReportRoot);

            bool exportCsv = !Application.isBatchMode || HasArg(CsvArg);
            StringBuilder csv = exportCsv ? new StringBuilder(2048) : null;
            if (exportCsv)
            {
                csv.AppendLine(
                    "impl,seeds,rounds,repeat,games,steps,timeUs,avgMs,stepUs,usedDelta,heapDelta,usedBGame,usedBStep,gen0Gc"
                );
            }

            StringBuilder md = new StringBuilder(2048);
            md.AppendLine("### PerfBench Report (batchmode)");
            md.AppendLine(
                $"env: unity={Application.unityVersion} platform={Application.platform} cpu={SystemInfo.processorType} cores={SystemInfo.processorCount} memMB={SystemInfo.systemMemorySize}"
            );
            md.AppendLine(
                $"config: repeats={repeats} rounds={RoundCount} seedRoot=0x{ReportRoot:X8} sizes=[{string.Join(", ", ReportSizes)}] exportCsv={exportCsv}"
            );
            md.AppendLine(
                "note: usedDelta/heapDelta are Mono memory deltas (Profiler.GetMonoUsedSizeLong/GetMonoHeapSizeLong)."
            );
            md.AppendLine();
            md.AppendLine(
                "|seeds|metric|legacy p50|legacy p95|kernel p50|kernel p95|speedup(p50)|"
            );
            md.AppendLine("|---:|---|---:|---:|---:|---:|---:|");

            List<int> chartX = new List<int>(ReportSizes.Length);
            List<double> chartLegacy = new List<double>(ReportSizes.Length);
            List<double> chartKernel = new List<double>(ReportSizes.Length);
            for (int s = 0; s < ReportSizes.Length; s++)
            {
                int count = ReportSizes[s];
                uint[] seeds = new uint[count];
                Array.Copy(seedAll, seeds, count);

                double[] legacyAvg = new double[repeats];
                double[] legacyStep = new double[repeats];
                double[] legacyGc = new double[repeats];

                double[] kernelAvg = new double[repeats];
                double[] kernelStep = new double[repeats];
                double[] kernelGc = new double[repeats];

                for (int r = 0; r < repeats; r++)
                {
                    BenchRes legacy = BenchLegacy(seeds, RoundCount, rules, encData);
                    BenchRes kernel = BenchKernel(seeds, RoundCount, rules, defs);

                    if (exportCsv)
                    {
                        Append(csv, "Legacy", in legacy, r);
                        Append(csv, "Kernel", in kernel, r);
                    }

                    legacyAvg[r] = AvgMs(in legacy);
                    legacyStep[r] = StepUs(in legacy);
                    legacyGc[r] = legacy.Gen0Gc;

                    kernelAvg[r] = AvgMs(in kernel);
                    kernelStep[r] = StepUs(in kernel);
                    kernelGc[r] = kernel.Gen0Gc;
                }

                WriteRow(md, count, "avgMs", legacyAvg, kernelAvg);
                WriteRow(md, count, "stepUs", legacyStep, kernelStep);
                WriteRow(md, count, "gen0Gc", legacyGc, kernelGc, speedup: false);

                chartX.Add(count);
                chartLegacy.Add(Median(legacyAvg));
                chartKernel.Add(Median(kernelAvg));
            }

            md.AppendLine();
            if (exportCsv)
            {
                string csvPath = Path.Combine(Path.GetTempPath(), "mask_perf_report.csv");
                File.WriteAllText(csvPath, csv.ToString());
                md.AppendLine($"csv: {csvPath}");
            }
            else
            {
                md.AppendLine("csv: (disabled)");
            }
            md.AppendLine();
            md.AppendLine("```mermaid");
            md.AppendLine(BuildChart(chartX, chartLegacy, chartKernel));
            md.AppendLine("```");

            Debug.Log(md.ToString());
        }

        private static void Append(StringBuilder csv, string name, in BenchRes res, int repeat)
        {
            int games = res.Seeds * res.Rounds;
            csv.Append(name);
            csv.Append(',');
            csv.Append(res.Seeds);
            csv.Append(',');
            csv.Append(res.Rounds);
            csv.Append(',');
            csv.Append(repeat);
            csv.Append(',');
            csv.Append(games);
            csv.Append(',');
            csv.Append(res.Steps);
            csv.Append(',');
            csv.Append(res.TimeUs);
            csv.Append(',');
            csv.Append(AvgMs(in res).ToString("F6"));
            csv.Append(',');
            csv.Append(StepUs(in res).ToString("F6"));
            csv.Append(',');
            csv.Append(res.UsedDelta);
            csv.Append(',');
            csv.Append(res.HeapDelta);
            csv.Append(',');
            csv.Append(UsedGame(in res).ToString("F6"));
            csv.Append(',');
            csv.Append(UsedStep(in res).ToString("F6"));
            csv.Append(',');
            csv.Append(res.Gen0Gc);
            csv.AppendLine();
        }

        private static double AvgMs(in BenchRes res)
        {
            int games = res.Seeds * res.Rounds;
            return games > 0 ? (res.TimeUs / 1000.0) / games : 0;
        }

        private static double StepUs(in BenchRes res)
        {
            return res.Steps > 0 ? (double)res.TimeUs / res.Steps : 0;
        }

        private static double UsedGame(in BenchRes res)
        {
            int games = res.Seeds * res.Rounds;
            return games > 0 ? (double)res.UsedDelta / games : 0;
        }

        private static double UsedStep(in BenchRes res)
        {
            return res.Steps > 0 ? (double)res.UsedDelta / res.Steps : 0;
        }

        private static void WriteRow(
            StringBuilder md,
            int seeds,
            string name,
            double[] legacy,
            double[] kernel,
            bool speedup = true
        )
        {
            double l50 = Median(legacy);
            double l95 = Pct(legacy, 0.95);
            double k50 = Median(kernel);
            double k95 = Pct(kernel, 0.95);
            string up = speedup && k50 > 0 ? (l50 / k50).ToString("F2") + "x" : "-";
            md.AppendLine($"|{seeds}|{name}|{l50:F6}|{l95:F6}|{k50:F6}|{k95:F6}|{up}|");
        }

        private static double Median(double[] values)
        {
            double[] copy = (double[])values.Clone();
            Array.Sort(copy);
            int mid = copy.Length / 2;
            if ((copy.Length & 1) != 0)
                return copy[mid];
            return (copy[mid - 1] + copy[mid]) * 0.5;
        }

        private static double Pct(double[] values, double pct)
        {
            double[] copy = (double[])values.Clone();
            Array.Sort(copy);
            double pos = (copy.Length - 1) * pct;
            int lo = (int)Math.Floor(pos);
            int hi = (int)Math.Ceiling(pos);
            if (lo == hi)
                return copy[lo];
            double t = pos - lo;
            return copy[lo] * (1 - t) + copy[hi] * t;
        }

        private static string BuildChart(List<int> xs, List<double> legacy, List<double> kernel)
        {
            double yMax = 0;
            for (int i = 0; i < legacy.Count; i++)
            {
                if (legacy[i] > yMax)
                    yMax = legacy[i];
            }
            for (int i = 0; i < kernel.Count; i++)
            {
                if (kernel[i] > yMax)
                    yMax = kernel[i];
            }
            yMax = yMax > 0 ? yMax * 1.2 : 1;

            StringBuilder sb = new StringBuilder(512);
            sb.AppendLine("xychart-beta");
            sb.AppendLine("  title \"avgMs per game (median over repeats)\"");
            sb.Append("  x-axis [");
            for (int i = 0; i < xs.Count; i++)
            {
                if (i != 0)
                    sb.Append(", ");
                sb.Append('"');
                sb.Append(xs[i]);
                sb.Append('"');
            }
            sb.AppendLine("]");
            sb.AppendLine($"  y-axis \"avgMs\" 0 --> {yMax:F4}");
            sb.Append("  line \"Legacy\" [");
            for (int i = 0; i < legacy.Count; i++)
            {
                if (i != 0)
                    sb.Append(", ");
                sb.Append(legacy[i].ToString("F6"));
            }
            sb.AppendLine("]");
            sb.Append("  line \"Kernel\" [");
            for (int i = 0; i < kernel.Count; i++)
            {
                if (i != 0)
                    sb.Append(", ");
                sb.Append(kernel[i].ToString("F6"));
            }
            sb.AppendLine("]");
            return sb.ToString();
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
            return BuildSeeds(count, unchecked((uint)DateTime.UtcNow.Ticks));
        }

        private static uint[] BuildSeeds(int count, uint root)
        {
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
            int warm = seeds.Length < WarmCap ? seeds.Length : WarmCap;
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

                if (
                    state.Phase == Kernel.GamePhase.GameWon
                    || state.Phase == Kernel.GamePhase.GameLost
                )
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
            int warm = seeds.Length < WarmCap ? seeds.Length : WarmCap;
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

        private static bool HasArg(string arg)
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                if (string.Equals(args[i], arg, StringComparison.Ordinal))
                    return true;
            }

            return false;
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

        private static Kernel.SimulationCommand GenCmd(
            ref DeterministicRng rng,
            in Kernel.GameState state
        )
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
            return "PERF Bench (batchmode)\n"
                + FormatOne("Legacy", in legacy)
                + "\n"
                + FormatOne("Kernel", in kernel);
        }

        private static string FormatOne(string name, in BenchRes res)
        {
            double ms = res.TimeUs / 1000.0;
            int games = res.Seeds * res.Rounds;
            double avgMs = games > 0 ? ms / games : 0;
            double stepUs = res.Steps > 0 ? (double)res.TimeUs / res.Steps : 0;
            double usedGame = games > 0 ? (double)res.UsedDelta / games : 0;
            double usedStep = res.Steps > 0 ? (double)res.UsedDelta / res.Steps : 0;

            return $"{name}: seeds={res.Seeds} rounds={res.Rounds} games={games} steps={res.Steps} timeUs={res.TimeUs} timeMs={ms:F3} "
                + $"avgMs={avgMs:F4} stepUs={stepUs:F4} gen0Gc={res.Gen0Gc} "
                + $"usedDelta={res.UsedDelta} heapDelta={res.HeapDelta} usedBGame={usedGame:F2} usedBStep={usedStep:F4}";
        }
    }
}
