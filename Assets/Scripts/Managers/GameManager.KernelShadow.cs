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
        private const uint Fnv32Offset = 2166136261u; // FNV-1a 32-bit
        private const uint Fnv32Prime = 16777619u; // FNV-1a 32-bit

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
            Debug.Log(
                $"Encounter order ({tag}): count={items.Length} hash=0x{hash:X8} top3={a}|{b}|{c}"
            );
        }

        private void InitShadow()
        {
            shadowInit = false;
            if (!shadowOn)
                return;

            // 在BOSS模式下使用BOSS池，否则使用普通池
            List<EncounterData> pool = bossMode ? GetBossPool() : GetPool();
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

            shadowState = Kernel.GameKernel.NewGame(gameSeed, shadowRules, count);
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
            
            // BOSS模式下需要额外扣血（kernel使用rules中的batteryPenalty=1，但BOSS模式实际-2）
            if (bossMode && currentEncounter != null)
            {
                bool isCorrect = selectedMask == currentEncounter.correctMask;
                bool isNeutral = false;
                
                if (currentEncounter.neutralMasks != null)
                {
                    foreach (var neutralMask in currentEncounter.neutralMasks)
                    {
                        if (neutralMask == selectedMask)
                        {
                            isNeutral = true;
                            break;
                        }
                    }
                }
                
                // 只有在答错或超时（且不是中立选项）时额外-1血
                if (!isCorrect && !isNeutral)
                {
                    shadowState.Health -= 1;
                }
            }
        }

        private void ShadowLoadEncounter()
        {
            if (!shadowOn || !shadowInit)
                return;

            // 加载encounter时需要同步encounterId到shadow kernel
            if (currentEncounter != null && shadowIds.TryGetValue(currentEncounter, out int encId))
            {
                // 如果shadow kernel还不在WaitAns状态，需要推进到该状态
                if (shadowState.Phase != Kernel.GamePhase.WaitAns)
                {
                    // kernel应该已经处于正确的初始状态，只需要更新EncId
                    shadowState.EncId = encId;
                }
                else
                {
                    shadowState.EncId = encId;
                }
            }
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
                        socialBattery <= 0 ? Kernel.GamePhase.GameLost : Kernel.GamePhase.GameWon;
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
                if (
                    currentEncounter == null
                    || !shadowIds.TryGetValue(currentEncounter, out int encId)
                    || encId != shadowState.EncId
                )
                {
                    mismatch = true;
                }
            }

            if (!mismatch)
                return;

            ulong hash = Kernel.GameKernel.HashState(in shadowState);
            Debug.LogError(
                $"Kernel shadow mismatch ({context}) seed={gameSeed} hash=0x{hash:X16} "
                    + $"gmDay={currentDay} kDay={shadowState.CurrentDay} "
                    + $"gmIdx={currentEncounterIndex} kIdx={shadowState.DayIdx} "
                    + $"gmHp={socialBattery} kHp={shadowState.Health} "
                    + $"gmTotal={totalAnswers} kTotal={shadowState.TotalAnswers} "
                    + $"gmCorrect={correctAnswers} kCorrect={shadowState.CorrectAnswers} "
                    + $"gmState={state} kPhase={shadowState.Phase}"
            );
        }
    }
}
#endif
