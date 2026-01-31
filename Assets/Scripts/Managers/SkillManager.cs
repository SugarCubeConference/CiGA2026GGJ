using System.Collections.Generic;
using MaskGame.Simulation;
using UnityEngine;
using UnityEngine.Events;
using MaskGame.Data;

namespace MaskGame.Managers
{
    /// <summary>
    /// 技能管理器 - 管理玩家获得的技能及其效果
    /// </summary>
    public class SkillManager : MonoBehaviour
    {
        public static SkillManager Instance { get; private set; }

        [Header("技能库")]
        [SerializeField]
        [Tooltip("所有可用的技能")]
        private List<SkillData> allSkills = new List<SkillData>();

        [Header("调试")]
        [SerializeField]
        private bool debugMode = false;

        // 玩家已获得的技能及叠加层数
        private Dictionary<SkillType, int> acquiredSkills = new Dictionary<SkillType, int>();
        private Dictionary<SkillType, SkillData> skillDataMap = new Dictionary<SkillType, SkillData>();

        // 一次性技能使用状态（每次遭遇重置）
        private bool eloquenceUsedThisEncounter = false;
        private bool innerDeductionUsedThisEncounter = false;
        private DeterministicRng skillRng;

        // 事件
        public UnityEvent<SkillData> OnSkillAcquired = new UnityEvent<SkillData>();
        public UnityEvent<List<SkillData>> OnSkillsUpdated = new UnityEvent<List<SkillData>>();

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                InitializeSkillDataMap();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void InitializeSkillDataMap()
        {
            skillDataMap.Clear();
            foreach (var skill in allSkills)
            {
                if (skill != null && !skillDataMap.ContainsKey(skill.skillType))
                {
                    skillDataMap[skill.skillType] = skill;
                }
            }
        }

        /// <summary>
        /// 获取随机的3个技能用于选择
        /// </summary>
        public List<SkillData> GetRandomSkills(int count = 3)
        {
            List<SkillData> availableSkills = new List<SkillData>();
            
            foreach (var skill in allSkills)
            {
                if (skill == null) continue;

                // 检查是否可以获得（未获得或可叠加且未达上限）
                if (!acquiredSkills.ContainsKey(skill.skillType))
                {
                    availableSkills.Add(skill);
                }
                else if (skill.stackable && acquiredSkills[skill.skillType] < skill.maxStacks)
                {
                    availableSkills.Add(skill);
                }
            }

            // 随机打乱
            for (int i = availableSkills.Count - 1; i > 0; i--)
            {
                int j = skillRng.NextInt(0, i + 1);
                var temp = availableSkills[i];
                availableSkills[i] = availableSkills[j];
                availableSkills[j] = temp;
            }

            // 返回指定数量
            int resultCount = Mathf.Min(count, availableSkills.Count);
            return availableSkills.GetRange(0, resultCount);
        }

        /// <summary>
        /// 玩家选择并获得技能
        /// </summary>
        public void AcquireSkill(SkillData skill)
        {
            if (skill == null) return;

            if (!acquiredSkills.ContainsKey(skill.skillType))
            {
                acquiredSkills[skill.skillType] = 1;
            }
            else if (skill.stackable)
            {
                acquiredSkills[skill.skillType]++;
            }

            // 立即应用某些技能效果
            ApplyImmediateSkillEffect(skill);

            OnSkillAcquired.Invoke(skill);
            OnSkillsUpdated.Invoke(GetAcquiredSkillList());
        }

        /// <summary>
        /// 立即应用技能效果（如回血）
        /// </summary>
        private void ApplyImmediateSkillEffect(SkillData skill)
        {
            switch (skill.skillType)
            {
                case SkillType.Meditation:
                    // 凝神定气 - 立即回复生命值
                    if (GameManager.Instance != null)
                    {
                        GameManager.Instance.RestoreHealth((int)skill.effectValue);
                    }
                    break;
            }
        }

        /// <summary>
        /// 检查是否拥有某技能
        /// </summary>
        public bool HasSkill(SkillType skillType)
        {
            return acquiredSkills.ContainsKey(skillType) && acquiredSkills[skillType] > 0;
        }

        /// <summary>
        /// 获取技能层数
        /// </summary>
        public int GetSkillStacks(SkillType skillType)
        {
            return acquiredSkills.ContainsKey(skillType) ? acquiredSkills[skillType] : 0;
        }

        /// <summary>
        /// 获取时间加成（电池技能）
        /// </summary>
        public float GetTimeBonus()
        {
            if (!HasSkill(SkillType.Battery)) return 1f;

            var skill = skillDataMap[SkillType.Battery];
            int stacks = GetSkillStacks(SkillType.Battery);
            // 每层增加effectValue%的时间
            return 1f + (skill.effectValue / 100f * stacks);
        }

        /// <summary>
        /// 是否应该显示关键词（思维敏捷）
        /// </summary>
        public bool ShouldShowKeywords()
        {
            return HasSkill(SkillType.QuickThinking);
        }

        /// <summary>
        /// 尝试使用妙语连珠（额外选择机会）
        /// </summary>
        public bool TryUseEloquence()
        {
            if (!HasSkill(SkillType.Eloquence)) return false;
            if (eloquenceUsedThisEncounter) return false;

            eloquenceUsedThisEncounter = true;
            return true;
        }

        /// <summary>
        /// 尝试使用内心推演（标红错误选项）
        /// </summary>
        public bool TryUseInnerDeduction()
        {
            if (!HasSkill(SkillType.InnerDeduction)) return false;
            if (innerDeductionUsedThisEncounter) return false;

            innerDeductionUsedThisEncounter = true;
            return true;
        }

        /// <summary>
        /// 重置遭遇相关的技能状态
        /// </summary>
        public void ResetEncounterSkillStates()
        {
            eloquenceUsedThisEncounter = false;
            innerDeductionUsedThisEncounter = false;
        }

        /// <summary>
        /// 获取已获得的技能列表
        /// </summary>
        public List<SkillData> GetAcquiredSkillList()
        {
            List<SkillData> result = new List<SkillData>();
            foreach (var kvp in acquiredSkills)
            {
                if (skillDataMap.ContainsKey(kvp.Key) && kvp.Value > 0)
                {
                    result.Add(skillDataMap[kvp.Key]);
                }
            }
            return result;
        }

        /// <summary>
        /// 获取已获得技能的名称列表（用于UI显示）
        /// </summary>
        public List<string> GetAcquiredSkillNames()
        {
            List<string> names = new List<string>();
            foreach (var kvp in acquiredSkills)
            {
                if (skillDataMap.ContainsKey(kvp.Key) && kvp.Value > 0)
                {
                    var skill = skillDataMap[kvp.Key];
                    if (skill.stackable && kvp.Value > 1)
                    {
                        names.Add($"{skill.skillName} x{kvp.Value}");
                    }
                    else
                    {
                        names.Add(skill.skillName);
                    }
                }
            }
            return names;
        }

        /// <summary>
        /// 重置所有技能（新游戏时调用）
        /// </summary>
        public void ResetAllSkills()
        {
            acquiredSkills.Clear();
            eloquenceUsedThisEncounter = false;
            innerDeductionUsedThisEncounter = false;
            OnSkillsUpdated.Invoke(new List<SkillData>());
        }

        public void SetDeterministicSeed(uint rootSeed)
        {
            skillRng = DeterministicRng.Create(rootSeed, DeterminismStreams.Skills);
        }
    }
}
