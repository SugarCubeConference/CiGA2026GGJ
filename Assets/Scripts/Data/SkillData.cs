using UnityEngine;

namespace MaskGame.Data
{
    /// <summary>
    /// 技能类型枚举
    /// </summary>
    public enum SkillType
    {
        Battery, // 电池 - 增加20%回答时长
        Meditation, // 凝神定气 - 回复一条命
        QuickThinking, // 思维敏捷 - 关键词再次显示
        Eloquence, // 妙语连珠 - 选错时获得额外机会
        InnerDeduction, // 内心推演 - 将一个错误选项标红
    }

    /// <summary>
    /// 技能数据 - ScriptableObject
    /// </summary>
    [CreateAssetMenu(fileName = "NewSkill", menuName = "Mask Game/Skill Data")]
    public class SkillData : ScriptableObject
    {
        [Header("基本信息")]
        [Tooltip("技能类型")]
        public SkillType skillType;

        [Tooltip("技能名称")]
        public string skillName;

        [Tooltip("技能描述")]
        [TextArea(2, 4)]
        public string description;

        [Tooltip("技能图标")]
        public Sprite icon;

        [Header("技能参数")]
        [Tooltip("技能效果数值（如增加时间百分比、回复血量等）")]
        public float effectValue = 1f;

        [Tooltip("技能是否可叠加")]
        public bool stackable = false;

        [Tooltip("最大叠加层数")]
        public int maxStacks = 1;
    }
}
