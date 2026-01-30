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

        [Header("提示高亮")]
        [Tooltip("需要在对话中高亮的关键词（用于提示正确面具）")]
        public string[] highlightKeywords;

        [Header("朋友信息")]
        [Tooltip("朋友分组（如：亲密朋友、同事、长辈等）")]
        public string friendGroup;

        [Header("正确答案")]
        [Tooltip("此对话需要的正确面具")]
        public MaskType correctMask;

        [Header("反馈文本（可选）")]
        [Tooltip("选对后的短反馈")]
        public string successFeedback;
        
        [Tooltip("选错后的短反馈")]
        public string failureFeedback;
    }
}