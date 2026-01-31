using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MaskGame.Data;
using MaskGame.Managers;

namespace MaskGame.Setup
{
    /// <summary>
    /// 技能系统自动配置工具 - 运行时自动连接所有技能系统组件
    /// </summary>
    public class SkillSystemAutoSetup : MonoBehaviour
    {
        private void Awake()
        {
            SetupSkillManager();
            SetupAwardPanelUI();
            
            // 配置完成后销毁自己
            Destroy(this);
        }

        private void SetupSkillManager()
        {
            var skillManager = FindObjectOfType<SkillManager>();
            if (skillManager == null)
            {
                return;
            }

            // 从项目中加载所有SkillData资源
            #if UNITY_EDITOR
            string[] guids = UnityEditor.AssetDatabase.FindAssets("t:SkillData");
            var skillList = new System.Collections.Generic.List<SkillData>();
            
            foreach (string guid in guids)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                var skill = UnityEditor.AssetDatabase.LoadAssetAtPath<SkillData>(path);
                if (skill != null)
                {
                    skillList.Add(skill);
                }
            }

            // 使用反射设置私有字段
            var field = typeof(SkillManager).GetField("allSkills", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (field != null)
            {
                field.SetValue(skillManager, skillList);
            }
            #endif
        }

        private void SetupAwardPanelUI()
        {
            var awardPanel = GameObject.Find("Panel_award");
            if (awardPanel == null)
            {
                return;
            }

            var awardPanelUI = awardPanel.GetComponent<MaskGame.UI.AwardPanelUI>();
            if (awardPanelUI == null)
            {
                return;
            }

            // 查找Image_background
            var imageBackground = awardPanel.transform.Find("Image_background");
            if (imageBackground == null)
            {
                return;
            }

            // 从Image_background的直接子对象中查找所有带Button组件的对象
            var skillButtons = new System.Collections.Generic.List<Button>();
            var nameTexts = new System.Collections.Generic.List<TextMeshProUGUI>();
            var descTexts = new System.Collections.Generic.List<TextMeshProUGUI>();

            foreach (Transform child in imageBackground)
            {
                var button = child.GetComponent<Button>();
                if (button != null)
                {
                    skillButtons.Add(button);
                    
                    // 查找这个按钮下的文本组件
                    var texts = child.GetComponentsInChildren<TextMeshProUGUI>(true);
                    foreach (var text in texts)
                    {
                        if (text.name.Contains("Desc") || text.name.Contains("desc"))
                        {
                            descTexts.Add(text);
                        }
                        else
                        {
                            nameTexts.Add(text);
                        }
                    }
                }
            }

            if (skillButtons.Count < 3)
            {
                // 按钮数不足
            }

            // 获取已获得技能显示文本
            var panelSkill = GameObject.Find("Panel_skill");
            var acquiredSkillsText = panelSkill?.GetComponentInChildren<TextMeshProUGUI>(true);

            // 使用反射设置私有字段
            var type = typeof(MaskGame.UI.AwardPanelUI);
            
            // 设置panelRoot
            var panelRootField = type.GetField("panelRoot", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (panelRootField != null)
            {
                panelRootField.SetValue(awardPanelUI, awardPanel);
            }

            // 设置按钮数组
            var buttonsField = type.GetField("skillButtons", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (buttonsField != null)
            {
                buttonsField.SetValue(awardPanelUI, skillButtons.ToArray());
            }

            // 设置名称文本数组
            var nameTextsField = type.GetField("skillNameTexts", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (nameTextsField != null)
            {
                nameTextsField.SetValue(awardPanelUI, nameTexts.ToArray());
            }

            // 设置描述文本数组
            var descTextsField = type.GetField("skillDescTexts", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (descTextsField != null)
            {
                descTextsField.SetValue(awardPanelUI, descTexts.ToArray());
            }

            // 设置已获得技能文本
            var acquiredTextField = type.GetField("acquiredSkillsText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (acquiredTextField != null)
            {
                acquiredTextField.SetValue(awardPanelUI, acquiredSkillsText);
            }
        }
    }
}
