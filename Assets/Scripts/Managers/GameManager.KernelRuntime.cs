using System.Collections.Generic;
using MaskGame.Data;
using MaskGame.Simulation;
using UnityEngine;
using Kernel = MaskGame.Simulation.Kernel;

namespace MaskGame.Managers
{
    public partial class GameManager
    {
        [Header("Kernel")]
        [SerializeField]
        private bool kernelOn = true;

        private bool kernelInit;
        private Kernel.GameRules kernelRules;
        private Kernel.EncounterDefinition[] kernelDefs;
        private List<EncounterData> kernelPool;
        private Kernel.GameState kernelState;

        private bool KernelInit()
        {
            kernelInit = false;
            kernelPool = GetPool();
            if (kernelPool == null || kernelPool.Count <= 0)
                return false;

            kernelDefs = new Kernel.EncounterDefinition[kernelPool.Count];
            for (int i = 0; i < kernelPool.Count; i++)
            {
                EncounterData encounter = kernelPool[i];

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

                kernelDefs[i] = new Kernel.EncounterDefinition(
                    (int)encounter.correctMask,
                    neutralBits
                );
            }

            kernelRules = new Kernel.GameRules(
                gameConfig.totalDays,
                gameConfig.encountersPerDay,
                gameConfig.initialHealth,
                gameConfig.maxHealth,
                gameConfig.batteryPenalty
            );

            kernelState = Kernel.GameKernel.NewGame(gameSeed, in kernelRules, kernelDefs.Length);
            KernelElo();
            KernelPull();
            kernelInit = true;
            return true;
        }

        private void KernelElo()
        {
            kernelState.HasElo =
                SkillManager.Instance != null && SkillManager.Instance.HasSkill(SkillType.Eloquence)
                    ? (byte)1
                    : (byte)0;
        }

        private void KernelPull()
        {
            currentDay = kernelState.CurrentDay;
            currentEncounterIndex = kernelState.DayIdx;
            socialBattery = kernelState.Health;
            totalAnswers = kernelState.TotalAnswers;
            correctAnswers = kernelState.CorrectAnswers;
        }

        private bool KernelLoad()
        {
            if (!kernelOn || !kernelInit)
                return false;
            if (kernelState.Phase != Kernel.GamePhase.WaitAns)
                return false;

            int encId = kernelState.EncId;
            if (encId < 0 || encId >= kernelPool.Count)
            {
                Debug.LogError(
                    $"GameManager: kernel EncId out of range: id={encId} count={kernelPool.Count}"
                );
                return false;
            }

            currentEncounter = kernelPool[encId];
            SpawnNPC(currentEncounter);

            float baseTime = gameConfig.GetDecisionTime(currentDay);
            float timeBonus =
                SkillManager.Instance != null ? SkillManager.Instance.GetTimeBonus() : 1f;
            remainingTime = baseTime * timeBonus;

            if (SkillManager.Instance != null)
            {
                SkillManager.Instance.ResetEncounterSkillStates();
            }

            OnNewEncounter.Invoke(currentEncounter);
            OnTimeChanged.Invoke(remainingTime);
            state = GameState.Await;
            return true;
        }

        private void KernelAnswer(MaskType selectedMask, bool isTimeout)
        {
            AnswerOutcome outcome;
            if (isTimeout)
            {
                outcome = AnswerOutcome.Timeout;
            }
            else if (selectedMask == currentEncounter.correctMask)
            {
                outcome = AnswerOutcome.Correct;
            }
            else if (IsNeutralMask(selectedMask))
            {
                outcome = AnswerOutcome.Neutral;
            }
            else
            {
                outcome = AnswerOutcome.Wrong;
            }

            string feedbackText = "";
            if (!isTimeout && currentEncounter.optionFeedbacks != null)
            {
                int selectedIndex = (int)selectedMask;
                if (selectedIndex >= 0 && selectedIndex < currentEncounter.optionFeedbacks.Length)
                {
                    feedbackText = currentEncounter.optionFeedbacks[selectedIndex];
                }
            }
            else if (isTimeout)
            {
                feedbackText = TimeoutFeedback;
            }

            OnAnswerResult.Invoke(outcome, feedbackText);

            if (outcome == AnswerOutcome.Correct)
                dailyCorrectAnswers++;

            int hpBefore = kernelState.Health;
            int idxBefore = kernelState.DayIdx;
            int encBefore = kernelState.EncId;
            byte eloBefore = kernelState.EloUsed;
            Kernel.GamePhase phaseBefore = kernelState.Phase;

            KernelElo();
            Kernel.SimulationCommand cmd = isTimeout
                ? Kernel.SimulationCommand.Timeout()
                : Kernel.SimulationCommand.SelectMask((int)selectedMask);
            Kernel.GameKernel.Apply(ref kernelState, in cmd, in kernelRules, kernelDefs);

            bool eloTriggered =
                (outcome == AnswerOutcome.Wrong || outcome == AnswerOutcome.Timeout)
                && phaseBefore == Kernel.GamePhase.WaitAns
                && kernelState.Phase == Kernel.GamePhase.WaitAns
                && hpBefore == kernelState.Health
                && idxBefore == kernelState.DayIdx
                && encBefore == kernelState.EncId
                && eloBefore == 0
                && kernelState.EloUsed == 1;

            if (eloTriggered)
            {
                if (SkillManager.Instance != null)
                    SkillManager.Instance.TryUseEloquence();

                OnAnswerResult.Invoke(AnswerOutcome.Wrong, feedbackText + EloSuffix);
                state = GameState.Await;
                return;
            }

            KernelPull();

            if (socialBattery != hpBefore)
                OnBatteryChanged.Invoke(socialBattery);

            if (kernelState.Phase == Kernel.GamePhase.GameLost)
            {
                GameOver();
                return;
            }

            if (currentEncounterIndex >= GetCurrentDayEncounters())
            {
                CompleteDay();
                return;
            }

            if (kernelState.Phase == Kernel.GamePhase.GameWon)
            {
                GameWin();
                return;
            }

            if (!KernelLoad())
            {
                GameWin();
            }
        }

        private void KernelHeal(int amount)
        {
            if (!kernelOn || !kernelInit)
                return;

            int hpBefore = kernelState.Health;
            Kernel.GameKernel.Apply(
                ref kernelState,
                Kernel.SimulationCommand.Heal(amount),
                in kernelRules,
                kernelDefs
            );

            KernelPull();
            if (socialBattery != hpBefore)
                OnBatteryChanged.Invoke(socialBattery);
        }

        private void KernelAdvance()
        {
            if (!kernelOn || !kernelInit)
                return;

            KernelElo();
            Kernel.GameKernel.Apply(
                ref kernelState,
                Kernel.SimulationCommand.AdvanceDay(),
                in kernelRules,
                kernelDefs
            );
            KernelPull();
        }
    }
}
