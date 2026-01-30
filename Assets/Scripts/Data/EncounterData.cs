using UnityEngine;

namespace MaskGame.Data
{
    /// <summary>
    /// 对话遭遇数据 - ScriptableObject
    /// 存储朋友对话及所需的正确面具
    /// </summary>
    [CreateAssetMenu(fileName = "NewEncounter", menuName = "Mask Game/Encounter Data")]
    public class EncounterData : ScriptableObject
    {
        [Header("对话内容")]
        [Tooltip("朋友说的话（10-20字）")]
        [TextArea(2, 4)]
        public string dialogueText;

        [Header("选项文本")]
        [Tooltip("四个选项的文本描述")]
        public string[] optionTexts = new string[4];
        
        [Header("反馈文本")]
        [Tooltip("四个选项各自的反馈文本（选择后显示）")]
        [TextArea(1, 2)]
        public string[] optionFeedbacks = new string[4];

        [Header("朋友信息")]
        [Tooltip("朋友分组（如：亲密朋友、同事、长辈等）")]
        public string friendGroup;

        [Header("正确答案")]
        [Tooltip("此对话需要的正确面具")]
        public MaskType correctMask;
    }
}