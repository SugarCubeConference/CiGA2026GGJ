#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections.Generic;
using MaskGame.Data;
using MaskGame.Simulation;
using Kernel = MaskGame.Simulation.Kernel;
using UnityEngine;
using UnityEngine.Serialization;

namespace MaskGame.Managers
{
    public partial class GameManager
    {
        [FormerlySerializedAs("enableKernelShadow")]
        [SerializeField]
        private bool shadowOn = true;

        private bool shadowInit;
        private Kernel.GameRules shadowRules;
        private Kernel.EncounterDefinition[] shadowDefs;
        private Dictionary<EncounterData, int> shadowIds;
        private Kernel.GameState shadowState;

        private static bool encLog;
        private const uint Fnv32Offset = 2166136261u;
        private const uint Fnv32Prime = 16777619u;

        private static void LogEnc(EncounterData[] items, string tag)
        {
            if (encLog)
                return;

            encLog = true;
            uint hash = Fnv32Offset;
            for (int i = 0; i < items.Length; i++)
            {
                if (items[i] == null)
                    continue;

                string name = items[i].name;
                for (int j = 0; j < name.Length; j++)
                {
                    hash ^= name[j];
                    hash *= Fnv32Prime;
                }
            }

            string a = items.Length > 0 && items[0] != null ? items[0].name : "-";
            string b = items.Length > 1 && items[1] != null ? items[1].name : "-";
            string c = items.Length > 2 && items[2] != null ? items[2].name : "-";
            Debug.Log($"Encounter order ({tag}): count={items.Length} hash=0x{hash:X8} top3={a}|{b}|{c}");
        }

        private void InitShadow()
        {
            shadowInit = false;
            if (!shadowOn)
                return;

            List<EncounterData> pool = GetPool();
            int count = pool.Count;
            if (count <= 0)
                return;

            shadowIds = new Dictionary<EncounterData, int>(count);
            shadowDefs = new Kernel.EncounterDefinition[count];

            for (int i = 0; i < count; i++)
            {
                EncounterData encounter = pool[i];
                shadowIds[encounter] = i;

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

                shadowDefs[i] = new Kernel.EncounterDefinition(
                    (int)encounter.correctMask,
                    neutralBits
                );
            }

            shadowRules = new Kernel.GameRules(
                gameConfig.totalDays,
                gameConfig.encountersPerDay,
                gameConfig.initialHealth,
                gameConfig.maxHealth,
                gameConfig.batteryPenalty
            );

            shadowState = Kernel.GameKernel.NewGame(
                gameSeed,
                shadowRules,
                count
            );
            SyncElo();

            shadowInit = true;
        }

        private void SyncElo()
        {
            shadowState.HasElo =
                SkillManager.Instance != null && SkillManager.Instance.HasSkill(SkillType.Eloquence)
                    ? (byte)1
                    : (byte)0;
        }

        private void ShadowAnswer(MaskType selectedMask, bool isTimeout)
        {
            if (!shadowOn || !shadowInit)
                return;

            SyncElo();

            Kernel.SimulationCommand command = isTimeout
                ? Kernel.SimulationCommand.Timeout()
                : Kernel.SimulationCommand.SelectMask((int)selectedMask);

            Kernel.GameKernel.Apply(ref shadowState, command, shadowRules, shadowDefs);
        }

        private void ShadowDay()
        {
            if (!shadowOn || !shadowInit)
                return;

            SyncElo();

            Kernel.GameKernel.Apply(
                ref shadowState,
                Kernel.SimulationCommand.AdvanceDay(),
                shadowRules,
                shadowDefs
            );
        }

        private void ShadowHeal(int amount)
        {
            if (!shadowOn || !shadowInit)
                return;

            Kernel.GameKernel.Apply(
                ref shadowState,
                Kernel.SimulationCommand.Heal(amount),
                shadowRules,
                shadowDefs
            );
        }

        private void CheckShadow(string context)
        {
            if (!shadowOn || !shadowInit)
                return;

            Kernel.GamePhase expectedPhase;
            switch (state)
            {
                case GameState.Await:
                    expectedPhase = Kernel.GamePhase.WaitAns;
                    break;
                case GameState.DayEnd:
                    expectedPhase = Kernel.GamePhase.WaitDay;
                    break;
                case GameState.GameEnd:
                    expectedPhase =
                        socialBattery <= 0
                            ? Kernel.GamePhase.GameLost
                            : Kernel.GamePhase.GameWon;
                    break;
                default:
                    return;
            }

            bool mismatch = false;
            if (shadowState.CurrentDay != currentDay)
                mismatch = true;
            if (shadowState.DayIdx != currentEncounterIndex)
                mismatch = true;
            if (shadowState.Health != socialBattery)
                mismatch = true;
            if (shadowState.TotalAnswers != totalAnswers)
                mismatch = true;
            if (shadowState.CorrectAnswers != correctAnswers)
                mismatch = true;
            if (shadowState.Phase != expectedPhase)
                mismatch = true;

            if (!mismatch && expectedPhase == Kernel.GamePhase.WaitAns)
            {
                if (currentEncounter == null
                    || !shadowIds.TryGetValue(currentEncounter, out int encId)
                    || encId != shadowState.EncId)
                {
                    mismatch = true;
                }
            }

            if (!mismatch)
                return;

            ulong hash = Kernel.GameKernel.HashState(in shadowState);
            Debug.LogError(
                $"Kernel shadow mismatch ({context}) seed={gameSeed} hash=0x{hash:X16} " +
                $"gmDay={currentDay} kDay={shadowState.CurrentDay} " +
                $"gmIdx={currentEncounterIndex} kIdx={shadowState.DayIdx} " +
                $"gmHp={socialBattery} kHp={shadowState.Health} " +
                $"gmTotal={totalAnswers} kTotal={shadowState.TotalAnswers} " +
                $"gmCorrect={correctAnswers} kCorrect={shadowState.CorrectAnswers} " +
                $"gmState={state} kPhase={shadowState.Phase}"
            );
        }
    }
}
#endif

