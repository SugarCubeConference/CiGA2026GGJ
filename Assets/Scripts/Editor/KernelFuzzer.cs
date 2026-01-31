using System;
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
        private const uint FuzzStream = 0x46555A5Au; // "FUZZ"
        private const string FailKey = "MaskGame.KernelFuzzer.LastFailureSeed";
        private const uint Fnv32Offset = 2166136261u;
        private const uint Fnv32Prime = 16777619u;

        private readonly struct Settings
        {
            public readonly int Seeds;
            public readonly int MaxStep;
            public readonly bool FailFast;
            public readonly bool VerifyReplay;
            public readonly bool IncludeHeal;
            public readonly bool IncludeEloquence;

            public Settings(
                int seeds,
                int maxStep,
                bool failFast,
                bool verifyReplay,
                bool includeHeal,
                bool includeEloquence
            )
            {
                Seeds = seeds;
                MaxStep = maxStep;
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
                    maxStep: 512,
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
                    maxStep: 4096,
                    failFast: false,
                    verifyReplay: true,
                    includeHeal: true,
                    includeEloquence: true
                )
            );
        }

        [MenuItem(MenuRoot + "Repro Last Failure", true)]
        private static bool CanRepro()
        {
            return EditorPrefs.HasKey(FailKey);
        }

        [MenuItem(MenuRoot + "Repro Last Failure")]
        public static void ReproFail()
        {
            int stored = EditorPrefs.GetInt(FailKey, 0);
            if (stored <= 0)
            {
                Debug.LogError("No recorded failure seed found.");
                return;
            }

            RunSingle(unchecked((uint)stored));
        }

        private static void Run(Settings settings)
        {
            if (!BuildInput(out Kernel.GameRules rules, out Kernel.EncounterDefinition[] encounters))
                return;

            DeterministicRng seedRng = DeterministicRng.Create(unchecked((uint)DateTime.UtcNow.Ticks), FuzzStream);
            System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();
            bool showProgress = !Application.isBatchMode;

            int passed = 0;
            int failed = 0;

            try
            {
                for (int i = 0; i < settings.Seeds; i++)
                {
                    uint seed = seedRng.NextUInt();
                    if (showProgress && i % 64 == 0)
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
                    EditorPrefs.SetInt(FailKey, unchecked((int)seed));
                    Debug.LogError(FormatFailure(failure));

                    if (settings.FailFast)
                        break;
                }
            }
            finally
            {
                if (showProgress)
                    EditorUtility.ClearProgressBar();
            }

            stopwatch.Stop();
            Debug.Log(
                $"Kernel fuzzing complete: passed={passed} failed={failed} seeds={passed + failed} timeMs={stopwatch.ElapsedMilliseconds}"
            );
        }

        private static void RunSingle(uint seed)
        {
            if (!BuildInput(out Kernel.GameRules rules, out Kernel.EncounterDefinition[] encounters))
                return;

            Settings settings = new Settings(
                seeds: 1,
                maxStep: 16384,
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

        private static bool BuildInput(
            out Kernel.GameRules rules,
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

            rules = new Kernel.GameRules(
                config.totalDays,
                config.encountersPerDay,
                config.initialHealth,
                config.maxHealth,
                config.batteryPenalty
            );

            EncounterData[] loaded = Resources.LoadAll<EncounterData>(EncounterRes);
            if (loaded.Length == 0)
            {
                Debug.LogError($"No EncounterData found at Resources/{EncounterRes}.");
                encounters = Array.Empty<Kernel.EncounterDefinition>();
                return false;
            }

            Array.Sort(loaded, (a, b) => string.CompareOrdinal(a.name, b.name));
            LogEnc(loaded, "Resources");

            encounters = new Kernel.EncounterDefinition[loaded.Length];
            for (int i = 0; i < loaded.Length; i++)
            {
                EncounterData encounter = loaded[i];
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

                encounters[i] = new Kernel.EncounterDefinition((int)encounter.correctMask, neutralBits);
            }

            return true;
        }

        private static void LogEnc(EncounterData[] items, string tag)
        {
            uint hash = Fnv32Offset;
            for (int i = 0; i < items.Length; i++)
            {
                string name = items[i].name;
                for (int j = 0; j < name.Length; j++)
                {
                    hash ^= name[j];
                    hash *= Fnv32Prime;
                }
            }

            string a = items.Length > 0 ? items[0].name : "-";
            string b = items.Length > 1 ? items[1].name : "-";
            string c = items.Length > 2 ? items[2].name : "-";

            Debug.Log($"Encounter order ({tag}): count={items.Length} hash=0x{hash:X8} top3={a}|{b}|{c}");
        }

        private static bool TrySimulate(
            uint seed,
            in Kernel.GameRules rules,
            Kernel.EncounterDefinition[] encounters,
            in Settings settings,
            out Failure failure
        )
        {
            if (!TryEdge(seed, in rules, encounters, out failure))
                return false;

            const int TraceCapacity = 64;
            Kernel.SimulationCommand[] trace = new Kernel.SimulationCommand[TraceCapacity];
            int traceWrite = 0;
            int traceCount = 0;

            Kernel.GameState state = Kernel.GameKernel.NewGame(seed, in rules, encounters.Length);
            DeterministicRng driverRng = DeterministicRng.Create(seed, FuzzStream);

            if (settings.IncludeEloquence && (driverRng.NextUInt() & 1u) != 0)
                state.HasElo = 1;

            for (int step = 0; step < settings.MaxStep; step++)
            {
                Kernel.SimulationCommand command = GenerateCommand(ref driverRng, in state, in settings);

                trace[traceWrite] = command;
                traceWrite = (traceWrite + 1) % TraceCapacity;
                if (traceCount < TraceCapacity)
                    traceCount++;

                uint rngPrev = state.EncounterRng.State;
                Kernel.GameKernel.Apply(ref state, in command, in rules, encounters);

                if (command.Type == Kernel.CmdType.Heal
                    && state.EncounterRng.State != rngPrev)
                {
                    failure = new Failure(
                        seed,
                        step,
                        command,
                        Kernel.GameKernel.HashState(in state),
                        "Heal mutated EncounterRng state.",
                        FormatTrace(trace, traceWrite, traceCount)
                    );
                    return false;
                }

                if (!CheckInv(in state, in rules, encounters, out string message))
                {
                    failure = new Failure(
                        seed,
                        step,
                        command,
                        Kernel.GameKernel.HashState(in state),
                        message,
                        FormatTrace(trace, traceWrite, traceCount)
                    );
                    return false;
                }

                if (state.Phase == Kernel.GamePhase.GameWon
                    || state.Phase == Kernel.GamePhase.GameLost)
                {
                    break;
                }
            }

            if (settings.VerifyReplay)
            {
                ulong finalHash = Kernel.GameKernel.HashState(in state);
                if (!TryReplay(seed, in rules, encounters, in settings, finalHash, out Failure replayFailure))
                {
                    failure = replayFailure;
                    return false;
                }
            }

            failure = default;
            return true;
        }

        private static bool TryEdge(
            uint seed,
            in Kernel.GameRules rules,
            Kernel.EncounterDefinition[] encounters,
            out Failure failure
        )
        {
            if (!TryDeck(seed, in rules, encounters, out string message))
            {
                failure = new Failure(
                    seed,
                    -1,
                    default,
                    0,
                    message,
                    string.Empty
                );
                return false;
            }

            if (!TryWin(seed, encounters, out message))
            {
                failure = new Failure(
                    seed,
                    -1,
                    default,
                    0,
                    message,
                    string.Empty
                );
                return false;
            }

            if (!TryElo(seed, in rules, encounters, out message))
            {
                failure = new Failure(
                    seed,
                    -1,
                    default,
                    0,
                    message,
                    string.Empty
                );
                return false;
            }

            if (!TryDays(seed, in rules, encounters, out message))
            {
                failure = new Failure(
                    seed,
                    -1,
                    default,
                    0,
                    message,
                    string.Empty
                );
                return false;
            }

            if (!TryDie(seed, encounters, out message))
            {
                failure = new Failure(
                    seed,
                    -1,
                    default,
                    0,
                    message,
                    string.Empty
                );
                return false;
            }

            failure = default;
            return true;
        }

        private static bool TryWin(uint seed, Kernel.EncounterDefinition[] encounters, out string message)
        {
            Kernel.GameRules rules = new Kernel.GameRules(
                totalDays: 1,
                dayEnc: 1,
                initialHealth: 1,
                maxHealth: 1,
                batteryPenalty: 1
            );

            Kernel.GameState state = Kernel.GameKernel.NewGame(seed, in rules, encounters.Length);
            int mask = encounters[state.EncId].CorrectMask;
            Kernel.GameKernel.Apply(ref state, Kernel.SimulationCommand.SelectMask(mask), in rules, encounters);

            if (state.Phase != Kernel.GamePhase.GameWon)
            {
                message = $"Win failed: expected GameWon got={state.Phase}";
                return false;
            }

            if (state.Health != 1)
            {
                message = $"Win failed: expected health=1 got={state.Health}";
                return false;
            }

            message = string.Empty;
            return true;
        }

        private static bool TryDeck(
            uint seed,
            in Kernel.GameRules rules,
            Kernel.EncounterDefinition[] encounters,
            out string message
        )
        {
            if (rules.TotalDays <= 1)
            {
                message = string.Empty;
                return true;
            }

            Kernel.GameState a = Kernel.GameKernel.NewGame(seed, in rules, encounters.Length);
            Kernel.GameState b = Kernel.GameKernel.NewGame(seed, in rules, encounters.Length);

            if (!FinishDay(ref a, in rules, encounters, out message))
                return false;
            if (!FinishDay(ref b, in rules, encounters, out message))
                return false;

            if (a.Phase != Kernel.GamePhase.WaitDay || b.Phase != Kernel.GamePhase.WaitDay)
            {
                message = $"DeckInd precondition failed: a={a.Phase} b={b.Phase}";
                return false;
            }

            for (int i = 0; i < 3; i++)
            {
                Kernel.GameKernel.Apply(ref a, Kernel.SimulationCommand.Heal(1), in rules, encounters);
            }

            Kernel.GameKernel.Apply(ref a, Kernel.SimulationCommand.AdvanceDay(), in rules, encounters);
            Kernel.GameKernel.Apply(ref b, Kernel.SimulationCommand.AdvanceDay(), in rules, encounters);

            if (a.EncounterRng.State != b.EncounterRng.State)
            {
                message = "DeckInd failed: EncounterRng state diverged after Heal.";
                return false;
            }

            int[] deckA = a.DayDeck;
            int[] deckB = b.DayDeck;
            if (deckA.Length != deckB.Length)
            {
                message = $"DeckInd failed: deck size mismatch a={deckA.Length} b={deckB.Length}";
                return false;
            }

            for (int i = 0; i < deckA.Length; i++)
            {
                if (deckA[i] != deckB[i])
                {
                    message = $"DeckInd failed: deck mismatch at i={i} a={deckA[i]} b={deckB[i]}";
                    return false;
                }
            }

            message = string.Empty;
            return true;
        }

        private static bool TryElo(
            uint seed,
            in Kernel.GameRules rules,
            Kernel.EncounterDefinition[] encounters,
            out string message
        )
        {
            Kernel.GameState state = Kernel.GameKernel.NewGame(seed, in rules, encounters.Length);
            state.HasElo = 1;

            int hp0 = state.Health;
            int idx0 = state.DayIdx;
            int left0 = state.DeckLeft;
            int enc0 = state.EncId;

            int wrongMask = PickWrong(encounters[enc0]);
            Kernel.SimulationCommand cmd =
                wrongMask >= 0 ? Kernel.SimulationCommand.SelectMask(wrongMask) : Kernel.SimulationCommand.Timeout();

            Kernel.GameKernel.Apply(ref state, in cmd, in rules, encounters);
            if (state.Health != hp0)
            {
                message = $"Elo failed: first wrong changed health hp0={hp0} hp1={state.Health}";
                return false;
            }

            if (state.DayIdx != idx0 || state.DeckLeft != left0 || state.EncId != enc0)
            {
                message = "Elo failed: first wrong advanced state.";
                return false;
            }

            if (state.EloUsed == 0)
            {
                message = "Elo failed: EloUsed not set on first wrong.";
                return false;
            }

            Kernel.GameKernel.Apply(ref state, in cmd, in rules, encounters);
            if (state.Health != hp0 - rules.BatteryPenalty)
            {
                message = $"Elo failed: second wrong did not apply penalty exp={hp0 - rules.BatteryPenalty} got={state.Health}";
                return false;
            }

            if (state.Phase != Kernel.GamePhase.WaitAns && state.Phase != Kernel.GamePhase.GameLost)
            {
                message = $"Elo failed: unexpected phase after second wrong phase={state.Phase}";
                return false;
            }

            message = string.Empty;
            return true;
        }

        private static bool TryDays(
            uint seed,
            in Kernel.GameRules rules,
            Kernel.EncounterDefinition[] encounters,
            out string message
        )
        {
            Kernel.GameState state = Kernel.GameKernel.NewGame(seed, in rules, encounters.Length);
            state.HasElo = 0;

            int days = rules.TotalDays < 5 ? rules.TotalDays : 5;
            for (int d = 0; d < days; d++)
            {
                int guard = state.DayDeck.Length + 8;
                for (int i = 0; i < guard && state.Phase == Kernel.GamePhase.WaitAns; i++)
                {
                    int mask = encounters[state.EncId].CorrectMask;
                    Kernel.GameKernel.Apply(ref state, Kernel.SimulationCommand.SelectMask(mask), in rules, encounters);
                }

                if (state.Phase == Kernel.GamePhase.WaitAns)
                {
                    message = "Days failed: did not reach a non-WaitAns phase.";
                    return false;
                }

                if (state.Phase == Kernel.GamePhase.GameWon)
                {
                    message = string.Empty;
                    return true;
                }

                if (state.Phase != Kernel.GamePhase.WaitDay)
                {
                    message = $"Days failed: expected WaitDay got={state.Phase}";
                    return false;
                }

                Kernel.GameKernel.Apply(ref state, Kernel.SimulationCommand.AdvanceDay(), in rules, encounters);
                if (state.Phase != Kernel.GamePhase.WaitAns)
                {
                    message = $"Days failed: expected WaitAns after AdvanceDay got={state.Phase}";
                    return false;
                }
            }

            message = string.Empty;
            return true;
        }

        private static bool TryDie(uint seed, Kernel.EncounterDefinition[] encounters, out string message)
        {
            Kernel.GameRules dieRules = new Kernel.GameRules(
                totalDays: 999,
                dayEnc: 1,
                initialHealth: 1,
                maxHealth: 1,
                batteryPenalty: 1
            );

            Kernel.GameState state = Kernel.GameKernel.NewGame(seed, in dieRules, encounters.Length);
            state.HasElo = 0;

            Kernel.GameKernel.Apply(ref state, Kernel.SimulationCommand.Timeout(), in dieRules, encounters);

            if (state.Phase != Kernel.GamePhase.GameLost)
            {
                message = $"Die failed: expected GameLost got={state.Phase} hp={state.Health}";
                return false;
            }

            if (state.Health != 0)
            {
                message = $"Die failed: expected health=0 got={state.Health}";
                return false;
            }

            message = string.Empty;
            return true;
        }

        private static bool FinishDay(
            ref Kernel.GameState state,
            in Kernel.GameRules rules,
            Kernel.EncounterDefinition[] encounters,
            out string message
        )
        {
            int guard = state.DayDeck.Length + 8;
            for (int i = 0; i < guard && state.Phase == Kernel.GamePhase.WaitAns; i++)
            {
                int encId = state.EncId;
                int mask = encounters[encId].CorrectMask;
                Kernel.GameKernel.Apply(ref state, Kernel.SimulationCommand.SelectMask(mask), in rules, encounters);
            }

            if (state.Phase != Kernel.GamePhase.WaitDay)
            {
                message = $"FinishDay failed: phase={state.Phase} left={state.DeckLeft}";
                return false;
            }

            message = string.Empty;
            return true;
        }

        private static int PickWrong(in Kernel.EncounterDefinition encounter)
        {
            for (int i = 0; i < MaskCount; i++)
            {
                if (i == encounter.CorrectMask)
                    continue;
                if (encounter.IsNeutral(i))
                    continue;
                return i;
            }

            return -1;
        }

        private static bool TryReplay(
            uint seed,
            in Kernel.GameRules rules,
            Kernel.EncounterDefinition[] encounters,
            in Settings settings,
            ulong expHash,
            out Failure failure
        )
        {
            Kernel.GameState state = Kernel.GameKernel.NewGame(seed, in rules, encounters.Length);
            DeterministicRng driverRng = DeterministicRng.Create(seed, FuzzStream);

            if (settings.IncludeEloquence && (driverRng.NextUInt() & 1u) != 0)
                state.HasElo = 1;

            for (int step = 0; step < settings.MaxStep; step++)
            {
                Kernel.SimulationCommand command = GenerateCommand(ref driverRng, in state, in settings);
                Kernel.GameKernel.Apply(ref state, in command, in rules, encounters);

                if (state.Phase == Kernel.GamePhase.GameWon
                    || state.Phase == Kernel.GamePhase.GameLost)
                {
                    break;
                }
            }

            ulong finalHash = Kernel.GameKernel.HashState(in state);
            if (finalHash != expHash)
            {
                failure = new Failure(
                    seed,
                    -1,
                    default,
                    finalHash,
                    $"Replay hash mismatch. expected=0x{expHash:X16} actual=0x{finalHash:X16}",
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
                case Kernel.GamePhase.WaitAns:
                {
                    int roll = rng.NextInt(0, 100);
                    if (roll < 10)
                        return Kernel.SimulationCommand.Timeout();

                    int maskIndex = rng.NextInt(0, MaskCount);
                    return Kernel.SimulationCommand.SelectMask(maskIndex);
                }
                case Kernel.GamePhase.WaitDay:
                {
                    if (settings.IncludeHeal && rng.NextInt(0, 4) == 0)
                        return Kernel.SimulationCommand.Heal(1);

                    return Kernel.SimulationCommand.AdvanceDay();
                }
                default:
                    return Kernel.SimulationCommand.AdvanceDay();
            }
        }

        private static bool CheckInv(
            in Kernel.GameState state,
            in Kernel.GameRules rules,
            Kernel.EncounterDefinition[] encounters,
            out string message
        )
        {
            if (state.Health < 0 || state.Health > rules.MaxHealth)
            {
                message = $"Health out of range: health={state.Health} max={rules.MaxHealth}";
                return false;
            }

            if (state.Health == 0 && state.Phase != Kernel.GamePhase.GameLost)
            {
                message = $"Health=0 but phase != GameLost: phase={state.Phase}";
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
            if (state.DeckLeft < 0 || state.DeckLeft > deckSize)
            {
                message = $"Deck remaining out of range: remaining={state.DeckLeft} deckSize={deckSize}";
                return false;
            }

            if (state.DayIdx != deckSize - state.DeckLeft)
            {
                message =
                    $"DayIdx mismatch: idx={state.DayIdx} expected={deckSize - state.DeckLeft}";
                return false;
            }

            if (state.Phase == Kernel.GamePhase.GameLost)
            {
                if (state.Health > 0)
                {
                    message = $"GameLost but health>0: health={state.Health}";
                    return false;
                }

                message = string.Empty;
                return true;
            }

            if (state.Phase == Kernel.GamePhase.GameWon)
            {
                if (state.CurrentDay < rules.TotalDays)
                {
                    message = $"GameWon before reaching totalDays: day={state.CurrentDay} totalDays={rules.TotalDays}";
                    return false;
                }

                message = string.Empty;
                return true;
            }

            if (state.EncId < 0 || state.EncId >= encounters.Length)
            {
                message =
                    $"CurrentEncounterId out of range: id={state.EncId} count={encounters.Length}";
                return false;
            }

            if (state.Phase == Kernel.GamePhase.WaitAns)
            {
                if (state.DeckLeft <= 0)
                {
                    message = "WaitAns but DeckLeft <= 0.";
                    return false;
                }

                int expEnc = state.DayDeck[state.DeckLeft - 1];
                if (state.EncId != expEnc)
                {
                    message =
                        $"CurrentEncounterId mismatch: id={state.EncId} expected={expEnc}";
                    return false;
                }
            }
            else if (state.Phase == Kernel.GamePhase.WaitDay)
            {
                if (state.DeckLeft != 0)
                {
                    message =
                        $"WaitDay but DeckLeft != 0: remaining={state.DeckLeft}";
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
            int start = writeIndex - count;
            if (start < 0)
                start += ring.Length;
            for (int i = 0; i < count; i++)
            {
                Kernel.SimulationCommand command = ring[(start + i) % ring.Length];
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
