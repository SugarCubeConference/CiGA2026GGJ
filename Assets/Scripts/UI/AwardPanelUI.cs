using System.Collections.Generic;
using MaskGame.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MaskGame.UI
{
    // 前向声明引用
    using MaskGame.Managers;

    /// <summary>
    /// 技能奖励面板UI - 每天结束后显示技能选择
    /// </summary>
    public class AwardPanelUI : MonoBehaviour
    {
        [Header("面板引用")]
        [SerializeField]
        private GameObject panelRoot;

        [Header("技能选项按钮")]
        [SerializeField]
        [Tooltip("3个技能选项按钮")]
        private Button[] skillButtons = new Button[3];

        [SerializeField]
        [Tooltip("3个技能名称文本")]
        private TextMeshProUGUI[] skillNameTexts = new TextMeshProUGUI[3];

        [SerializeField]
        [Tooltip("3个技能描述文本")]
        private TextMeshProUGUI[] skillDescTexts = new TextMeshProUGUI[3];

        [SerializeField]
        [Tooltip("3个技能图标")]
        private Image[] skillIcons = new Image[3];

        [Header("已获得技能面板")]
        [SerializeField]
        [Tooltip("Panel_skill中显示已获得技能的TMP")]
        private TextMeshProUGUI acquiredSkillsText;

        private List<SkillData> currentSkillOptions = new List<SkillData>();
        private bool isWaitingForSelection = false;

        private void Start()
        {
            // 初始化按钮点击事件
            for (int i = 0; i < skillButtons.Length; i++)
            {
                int index = i; // 闭包捕获
                if (skillButtons[i] != null)
                {
                    skillButtons[i].onClick.AddListener(() => OnSkillSelected(index));
                }
            }

            // 监听技能更新事件
            if (SkillManager.Instance != null)
            {
                SkillManager.Instance.OnSkillsUpdated.AddListener(UpdateAcquiredSkillsDisplay);
            }

            // 初始隐藏面板
            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            if (SkillManager.Instance != null)
            {
                SkillManager.Instance.OnSkillsUpdated.RemoveListener(UpdateAcquiredSkillsDisplay);
            }
        }

        /// <summary>
        /// 显示技能选择面板
        /// </summary>
        public void ShowSkillSelection()
        {
            if (SkillManager.Instance == null)
            {
                return;
            }

            // 检查必要组件
            if (skillButtons == null || skillButtons.Length == 0)
            {
                return;
            }

            // 暂停游戏时间
            Time.timeScale = 0f;

            // 禁用设置按钉
            DisableSettingButton();

            // 获取随机技能
            currentSkillOptions = SkillManager.Instance.GetRandomSkills(3);

            // 更新UI显示
            for (int i = 0; i < skillButtons.Length; i++)
            {
                if (skillButtons[i] == null)
                {
                    continue;
                }

                if (i < currentSkillOptions.Count)
                {
                    SetupSkillButton(i, currentSkillOptions[i]);
                    skillButtons[i].gameObject.SetActive(true);
                }
                else
                {
                    skillButtons[i].gameObject.SetActive(false);
                }
            }

            // 显示面板并激活所有子元素
            if (panelRoot != null)
            {
                panelRoot.SetActive(true);

                // 递归激活所有子对象（包括嵌套的子对象）
                ActivateAllChildren(panelRoot.transform);
            }

            isWaitingForSelection = true;
        }

        /// <summary>
        /// 设置技能按钮显示
        /// </summary>
        private void SetupSkillButton(int index, SkillData skill)
        {
            if (
                skillNameTexts != null
                && index < skillNameTexts.Length
                && skillNameTexts[index] != null
            )
            {
                skillNameTexts[index].text = skill.skillName;
            }

            if (
                skillDescTexts != null
                && index < skillDescTexts.Length
                && skillDescTexts[index] != null
            )
            {
                skillDescTexts[index].text = skill.description;
            }

            if (
                skillIcons != null
                && index < skillIcons.Length
                && skillIcons[index] != null
                && skill.icon != null
            )
            {
                skillIcons[index].sprite = skill.icon;
                skillIcons[index].gameObject.SetActive(true);
            }
            else if (skillIcons != null && index < skillIcons.Length && skillIcons[index] != null)
            {
                skillIcons[index].gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// 玩家选择技能
        /// </summary>
        private void OnSkillSelected(int index)
        {
            if (!isWaitingForSelection)
                return;
            if (index < 0 || index >= currentSkillOptions.Count)
                return;

            SkillData selectedSkill = currentSkillOptions[index];

            // 获得技能
            if (SkillManager.Instance != null)
            {
                SkillManager.Instance.AcquireSkill(selectedSkill);
            }

            // 隐藏面板
            HidePanel();

            // 通知GameManager继续游戏
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnSkillSelectionComplete();
            }
        }

        /// <summary>
        /// 跳过技能选择
        /// </summary>
        public void SkipSkillSelection()
        {
            if (!isWaitingForSelection)
                return;

            // 隐藏面板（不选择任何技能）
            HidePanel();

            // 通知GameManager继续游戏
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnSkillSelectionComplete();
            }
        }

        /// <summary>
        /// 隐藏面板
        /// </summary>
        private void HidePanel()
        {
            isWaitingForSelection = false;

            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }

            // 恢复游戏时间
            Time.timeScale = 1f;

            // 启用设置按钮
            EnableSettingButton();
        }

        /// <summary>
        /// 更新已获得技能显示
        /// </summary>
        private void UpdateAcquiredSkillsDisplay(List<SkillData> skills)
        {
            if (acquiredSkillsText == null)
                return;

            if (SkillManager.Instance == null)
            {
                acquiredSkillsText.text = "";
                return;
            }

            var skillNames = SkillManager.Instance.GetAcquiredSkillNames();
            if (skillNames.Count == 0)
            {
                acquiredSkillsText.text = "暂无技能";
            }
            else
            {
                acquiredSkillsText.text = string.Join("\n", skillNames);
            }
        }

        /// <summary>
        /// 禁用设置按钮
        /// </summary>
        private void DisableSettingButton()
        {
            var settingButton = FindObjectOfType<SettingButtonUI>();
            if (settingButton != null)
            {
                settingButton.DisableButton();
            }
        }

        /// <summary>
        /// 启用设置按钮
        /// </summary>
        private void EnableSettingButton()
        {
            var settingButton = FindObjectOfType<SettingButtonUI>();
            if (settingButton != null)
            {
                settingButton.EnableButton();
            }
        }

        /// <summary>
        /// 递归激活所有子对象
        /// </summary>
        private void ActivateAllChildren(Transform parent)
        {
            foreach (Transform child in parent)
            {
                child.gameObject.SetActive(true);

                // 递归激活子对象的子对象
                if (child.childCount > 0)
                {
                    ActivateAllChildren(child);
                }
            }
        }
    }
}
