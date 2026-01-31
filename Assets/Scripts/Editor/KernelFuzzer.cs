using System;
using System.Diagnostics;
using System.Text;
using MaskGame.Data;
using MaskGame.Simulation;
using Kernel = MaskGame.Simulation.Kernel;
using UnityEditor;
using UnityEngine;

namespace MaskGame.Editor
{
    public static class KernelFuzzer
    {
        private const string MenuRoot = "Tools/Mask Game/Kernel Fuzzer/";
        private const string EncounterRes = "Encounters";
        private const int MaskCount = 4;
        private const uint DriverStream = 0x46555A5Au; // "FUZZ"
        private const string LastFailureSeedKey = "MaskGame.KernelFuzzer.LastFailureSeed";

        private readonly struct Settings
        {
            public readonly int Seeds;
            public readonly int MaxStepsPerSeed;
            public readonly bool FailFast;
            public readonly bool VerifyReplay;
            public readonly bool IncludeHeal;
            public readonly bool IncludeEloquence;

            public Settings(
                int seeds,
                int maxStepsPerSeed,
                bool failFast,
                bool verifyReplay,
                bool includeHeal,
                bool includeEloquence
            )
            {
                Seeds = seeds;
                MaxStepsPerSeed = maxStepsPerSeed;
                FailFast = failFast;
                VerifyReplay = verifyReplay;
                IncludeHeal = includeHeal;
                IncludeEloquence = includeEloquence;
            }
        }

        private readonly struct Failure
        {
            public readonly uint Seed;
            public readonly int Step;
            public readonly Kernel.SimulationCommand Command;
            public readonly ulong StateHash;
            public readonly string Message;
            public readonly string Trace;

            public Failure(
                uint seed,
                int step,
                Kernel.SimulationCommand command,
                ulong stateHash,
                string message,
                string trace
            )
            {
                Seed = seed;
                Step = step;
                Command = command;
                StateHash = stateHash;
                Message = message;
                Trace = trace;
            }
        }

        [MenuItem(MenuRoot + "Run (Fast)")]
        public static void RunFast()
        {
            Run(
                new Settings(
                    seeds: 200,
                    maxStepsPerSeed: 512,
                    failFast: true,
                    verifyReplay: false,
                    includeHeal: true,
                    includeEloquence: true
                )
            );
        }

        [MenuItem(MenuRoot + "Run (Thorough)")]
        public static void RunThorough()
        {
            Run(
                new Settings(
                    seeds: 5000,
                    maxStepsPerSeed: 4096,
                    failFast: false,
                    verifyReplay: true,
                    includeHeal: true,
                    includeEloquence: true
                )
            );
        }

        [MenuItem(MenuRoot + "Repro Last Failure", true)]
        private static bool CanReproLastFailure()
        {
            return EditorPrefs.HasKey(LastFailureSeedKey);
        }

        [MenuItem(MenuRoot + "Repro Last Failure")]
        public static void ReproLastFailure()
        {
            int stored = EditorPrefs.GetInt(LastFailureSeedKey, 0);
            if (stored <= 0)
            {
                Debug.LogError("No recorded failure seed found.");
                return;
            }

            RunSingle(unchecked((uint)stored));
        }

        private static void Run(Settings settings)
        {
            if (!TryBuildKernelInputs(out Kernel.GameKernelRules rules, out Kernel.EncounterDefinition[] encounters))
                return;

            DeterministicRng seedRng = DeterministicRng.Create(unchecked((uint)DateTime.UtcNow.Ticks), DriverStream);
            Stopwatch stopwatch = Stopwatch.StartNew();

            int passed = 0;
            int failed = 0;

            try
            {
                for (int i = 0; i < settings.Seeds; i++)
                {
                    uint seed = seedRng.NextUInt();
                    if ((i & 0x3F) == 0)
                    {
                        EditorUtility.DisplayProgressBar(
                            "Kernel Fuzzer",
                            $"Running seed {i + 1}/{settings.Seeds}",
                            (float)i / settings.Seeds
                        );
                    }

                    if (TrySimulate(seed, in rules, encounters, in settings, out Failure failure))
                    {
                        passed++;
                        continue;
                    }

                    failed++;
                    EditorPrefs.SetInt(LastFailureSeedKey, unchecked((int)seed));
                    Debug.LogError(FormatFailure(failure));

                    if (settings.FailFast)
                        break;
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            stopwatch.Stop();
            Debug.Log(
                $"Kernel fuzzing complete: passed={passed} failed={failed} seeds={passed + failed} timeMs={stopwatch.ElapsedMilliseconds}"
            );
        }

        private static void RunSingle(uint seed)
        {
            if (!TryBuildKernelInputs(out Kernel.GameKernelRules rules, out Kernel.EncounterDefinition[] encounters))
                return;

            Settings settings = new Settings(
                seeds: 1,
                maxStepsPerSeed: 16384,
                failFast: true,
                verifyReplay: true,
                includeHeal: true,
                includeEloquence: true
            );

            if (!TrySimulate(seed, in rules, encounters, in settings, out Failure failure))
            {
                Debug.LogError(FormatFailure(failure));
                return;
            }

            Debug.Log($"Repro seed passed: seed={seed}");
        }

        private static bool TryBuildKernelInputs(
            out Kernel.GameKernelRules rules,
            out Kernel.EncounterDefinition[] encounters
        )
        {
            GameConfig config;
            MaskGame.Managers.GameManager gameManager = UnityEngine.Object.FindObjectOfType<MaskGame.Managers.GameManager>();
            if (gameManager != null)
            {
                config = gameManager.Config;
            }
            else
            {
                config = new GameConfig();
            }

            rules = new Kernel.GameKernelRules(
                config.totalDays,
                config.encountersPerDay,
                config.initialHealth,
                config.maxHealth,
                config.batteryPenalty
            );

            EncounterData[] loaded = Resources.LoadAll<EncounterData>(EncounterRes);
            if (loaded == null || loaded.Length == 0)
            {
                Debug.LogError($"No EncounterData found at Resources/{EncounterRes}.");
                encounters = Array.Empty<Kernel.EncounterDefinition>();
                return false;
            }

            Array.Sort(
                loaded,
                (a, b) =>
                    string.CompareOrdinal(
                        a != null ? a.name : string.Empty,
                        b != null ? b.name : string.Empty
                    )
            );

            int count = 0;
            for (int i = 0; i < loaded.Length; i++)
            {
                if (loaded[i] != null)
                    count++;
            }

            if (count <= 0)
            {
                Debug.LogError($"No non-null EncounterData found at Resources/{EncounterRes}.");
                encounters = Array.Empty<Kernel.EncounterDefinition>();
                return false;
            }

            encounters = new Kernel.EncounterDefinition[count];
            int writeIndex = 0;
            for (int i = 0; i < loaded.Length; i++)
            {
                EncounterData encounter = loaded[i];
                if (encounter == null)
                    continue;

                byte neutralBits = 0;
                MaskType[] neutralMasks = encounter.neutralMasks;
                if (neutralMasks != null)
                {
                    for (int n = 0; n < neutralMasks.Length; n++)
                    {
                        int maskIndex = (int)neutralMasks[n];
                        neutralBits = (byte)(neutralBits | (1 << maskIndex));
                    }
                }

                encounters[writeIndex++] = new Kernel.EncounterDefinition((int)encounter.correctMask, neutralBits);
            }

            return true;
        }

        private static bool TrySimulate(
            uint seed,
            in Kernel.GameKernelRules rules,
            Kernel.EncounterDefinition[] encounters,
            in Settings settings,
            out Failure failure
        )
        {
            const int TraceCapacity = 64;
            Kernel.SimulationCommand[] trace = new Kernel.SimulationCommand[TraceCapacity];
            int traceWrite = 0;
            int traceCount = 0;

            Kernel.GameState state = Kernel.GameKernel.CreateNewGame(seed, in rules, encounters.Length);
            DeterministicRng driverRng = DeterministicRng.Create(seed, DriverStream);

            if (settings.IncludeEloquence && (driverRng.NextUInt() & 1u) != 0)
                state.HasEloquence = 1;

            Kernel.SimulationCommand lastCommand = default;

            for (int step = 0; step < settings.MaxStepsPerSeed; step++)
            {
                Kernel.SimulationCommand command = GenerateCommand(ref driverRng, in state, in settings);

                trace[traceWrite] = command;
                traceWrite = (traceWrite + 1) & (TraceCapacity - 1);
                if (traceCount < TraceCapacity)
                    traceCount++;

                uint encounterRngBefore = state.EncounterRng.State;
                Kernel.GameKernel.Apply(ref state, in command, in rules, encounters);
                lastCommand = command;

                if (command.Type == Kernel.SimulationCommandType.Heal
                    && state.EncounterRng.State != encounterRngBefore)
                {
                    failure = new Failure(
                        seed,
                        step,
                        command,
                        Kernel.GameKernel.ComputeStateHash(in state),
                        "Heal mutated EncounterRng state.",
                        FormatTrace(trace, traceWrite, traceCount)
                    );
                    return false;
                }

                if (!TryCheckInvariants(in state, in rules, encounters, out string message))
                {
                    failure = new Failure(
                        seed,
                        step,
                        command,
                        Kernel.GameKernel.ComputeStateHash(in state),
                        message,
                        FormatTrace(trace, traceWrite, traceCount)
                    );
                    return false;
                }

                if (state.Phase == Kernel.GameKernelPhase.GameWon
                    || state.Phase == Kernel.GameKernelPhase.GameLost)
                {
                    break;
                }
            }

            if (state.Phase != Kernel.GameKernelPhase.GameWon
                && state.Phase != Kernel.GameKernelPhase.GameLost)
            {
                failure = new Failure(
                    seed,
                    settings.MaxStepsPerSeed,
                    lastCommand,
                    Kernel.GameKernel.ComputeStateHash(in state),
                    "Simulation exceeded max steps without reaching a terminal state.",
                    FormatTrace(trace, traceWrite, traceCount)
                );
                return false;
            }

            if (settings.VerifyReplay)
            {
                ulong finalHash = Kernel.GameKernel.ComputeStateHash(in state);
                if (!TryReplay(seed, in rules, encounters, in settings, finalHash, out Failure replayFailure))
                {
                    failure = replayFailure;
                    return false;
                }
            }

            failure = default;
            return true;
        }

        private static bool TryReplay(
            uint seed,
            in Kernel.GameKernelRules rules,
            Kernel.EncounterDefinition[] encounters,
            in Settings settings,
            ulong expectedFinalHash,
            out Failure failure
        )
        {
            Kernel.GameState state = Kernel.GameKernel.CreateNewGame(seed, in rules, encounters.Length);
            DeterministicRng driverRng = DeterministicRng.Create(seed, DriverStream);

            if (settings.IncludeEloquence && (driverRng.NextUInt() & 1u) != 0)
                state.HasEloquence = 1;

            for (int step = 0; step < settings.MaxStepsPerSeed; step++)
            {
                Kernel.SimulationCommand command = GenerateCommand(ref driverRng, in state, in settings);
                Kernel.GameKernel.Apply(ref state, in command, in rules, encounters);

                if (state.Phase == Kernel.GameKernelPhase.GameWon
                    || state.Phase == Kernel.GameKernelPhase.GameLost)
                {
                    break;
                }
            }

            ulong finalHash = Kernel.GameKernel.ComputeStateHash(in state);
            if (finalHash != expectedFinalHash)
            {
                failure = new Failure(
                    seed,
                    -1,
                    default,
                    finalHash,
                    $"Replay hash mismatch. expected=0x{expectedFinalHash:X16} actual=0x{finalHash:X16}",
                    string.Empty
                );
                return false;
            }

            failure = default;
            return true;
        }

        private static Kernel.SimulationCommand GenerateCommand(
            ref DeterministicRng rng,
            in Kernel.GameState state,
            in Settings settings
        )
        {
            switch (state.Phase)
            {
                case Kernel.GameKernelPhase.AwaitingAnswer:
                {
                    int roll = rng.NextInt(0, 100);
                    if (roll < 10)
                        return Kernel.SimulationCommand.Timeout();

                    int maskIndex = rng.NextInt(0, MaskCount);
                    return Kernel.SimulationCommand.SelectMask(maskIndex);
                }
                case Kernel.GameKernelPhase.AwaitingDayAdvance:
                {
                    if (settings.IncludeHeal && rng.NextInt(0, 4) == 0)
                        return Kernel.SimulationCommand.Heal(1);

                    return Kernel.SimulationCommand.AdvanceDay();
                }
                default:
                    return Kernel.SimulationCommand.AdvanceDay();
            }
        }

        private static bool TryCheckInvariants(
            in Kernel.GameState state,
            in Kernel.GameKernelRules rules,
            Kernel.EncounterDefinition[] encounters,
            out string message
        )
        {
            if (state.Health < 0 || state.Health > rules.MaxHealth)
            {
                message = $"Health out of range: health={state.Health} max={rules.MaxHealth}";
                return false;
            }

            if (state.CurrentDay < 1 || state.CurrentDay > rules.TotalDays)
            {
                message = $"Day out of range: day={state.CurrentDay} totalDays={rules.TotalDays}";
                return false;
            }

            if (state.DayDeck == null || state.PoolScratch == null)
            {
                message = "Internal arrays are null.";
                return false;
            }

            int deckSize = state.DayDeck.Length;
            if (deckSize < 0 || state.RemainingInDayDeck < 0 || state.RemainingInDayDeck > deckSize)
            {
                message = $"Deck remaining out of range: remaining={state.RemainingInDayDeck} deckSize={deckSize}";
                return false;
            }

            if (state.EncounterIndexInDay != deckSize - state.RemainingInDayDeck)
            {
                message =
                    $"EncounterIndexInDay mismatch: idx={state.EncounterIndexInDay} expected={deckSize - state.RemainingInDayDeck}";
                return false;
            }

            if (state.Phase == Kernel.GameKernelPhase.GameLost)
            {
                if (state.Health > 0)
                {
                    message = $"GameLost but health>0: health={state.Health}";
                    return false;
                }

                message = string.Empty;
                return true;
            }

            if (state.Phase == Kernel.GameKernelPhase.GameWon)
            {
                if (state.CurrentDay < rules.TotalDays)
                {
                    message = $"GameWon before reaching totalDays: day={state.CurrentDay} totalDays={rules.TotalDays}";
                    return false;
                }

                message = string.Empty;
                return true;
            }

            if (state.CurrentEncounterId < 0 || state.CurrentEncounterId >= encounters.Length)
            {
                message =
                    $"CurrentEncounterId out of range: id={state.CurrentEncounterId} count={encounters.Length}";
                return false;
            }

            if (state.Phase == Kernel.GameKernelPhase.AwaitingAnswer)
            {
                if (state.RemainingInDayDeck <= 0)
                {
                    message = "AwaitingAnswer but RemainingInDayDeck <= 0.";
                    return false;
                }

                int expectedEncounterId = state.DayDeck[state.RemainingInDayDeck - 1];
                if (state.CurrentEncounterId != expectedEncounterId)
                {
                    message =
                        $"CurrentEncounterId mismatch: id={state.CurrentEncounterId} expected={expectedEncounterId}";
                    return false;
                }
            }
            else if (state.Phase == Kernel.GameKernelPhase.AwaitingDayAdvance)
            {
                if (state.RemainingInDayDeck != 0)
                {
                    message =
                        $"AwaitingDayAdvance but RemainingInDayDeck != 0: remaining={state.RemainingInDayDeck}";
                    return false;
                }
            }

            message = string.Empty;
            return true;
        }

        private static string FormatFailure(in Failure failure)
        {
            return
                $"Kernel fuzz failure: seed={failure.Seed} step={failure.Step} " +
                $"cmd={failure.Command.Type}({failure.Command.Int0}) " +
                $"hash=0x{failure.StateHash:X16} msg={failure.Message}\n" +
                $"trace(last)={failure.Trace}";
        }

        private static string FormatTrace(Kernel.SimulationCommand[] ring, int writeIndex, int count)
        {
            if (count <= 0)
                return string.Empty;

            StringBuilder sb = new StringBuilder(count * 10);
            int start = (writeIndex - count) & (ring.Length - 1);
            for (int i = 0; i < count; i++)
            {
                Kernel.SimulationCommand command = ring[(start + i) & (ring.Length - 1)];
                sb.Append(command.Type);
                sb.Append('(');
                sb.Append(command.Int0);
                sb.Append(')');
                if (i != count - 1)
                    sb.Append(' ');
            }
            return sb.ToString();
        }
    }
}

