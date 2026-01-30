using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace MaskGame.UI
{
    /// <summary>
    /// 面具选项UI - 鼠标悬停显示选项文本（程序化生成tooltip）
    /// </summary>
    public class MaskOptionUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("选项数据")]
        [SerializeField] private string optionText;
        
        [Header("设置")]
        [SerializeField] private Vector2 tooltipOffset = new Vector2(0, 50);
        [SerializeField] private Vector2 tooltipSize = new Vector2(300, 80);
        [SerializeField] private Color backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.95f);
        [SerializeField] private Color textColor = Color.white;
        [SerializeField] private int fontSize = 18;

        private GameObject tooltipPanel;
        private TextMeshProUGUI tooltipText;
        private RectTransform tooltipRect;
        private Canvas canvas;

        private void Awake()
        {
            // 查找Canvas
            canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("MaskOptionUI: 无法找到Canvas组件！");
            }
        }

        /// <summary>
        /// 创建tooltip UI
        /// </summary>
        private void CreateTooltip()
        {
            if (tooltipPanel != null || canvas == null) return;

            // 创建tooltip面板
            tooltipPanel = new GameObject("Tooltip_" + gameObject.name);
            tooltipPanel.transform.SetParent(canvas.transform, false);
            
            tooltipRect = tooltipPanel.AddComponent<RectTransform>();
            tooltipRect.sizeDelta = tooltipSize;
            tooltipRect.pivot = new Vector2(0.5f, 0);
            
            // 添加背景Image
            Image bgImage = tooltipPanel.AddComponent<Image>();
            bgImage.color = backgroundColor;
            
            // 创建文本对象
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(tooltipPanel.transform, false);
            
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(10, 10);
            textRect.offsetMax = new Vector2(-10, -10);
            
            tooltipText = textObj.AddComponent<TextMeshProUGUI>();
            tooltipText.fontSize = fontSize;
            tooltipText.color = textColor;
            tooltipText.alignment = TextAlignmentOptions.Center;
            tooltipText.enableWordWrapping = true;
            
            // 设置层级（确保显示在最前）
            tooltipPanel.transform.SetAsLastSibling();
            
            tooltipPanel.SetActive(false);
        }

        /// <summary>
        /// 设置选项文本
        /// </summary>
        public void SetOptionText(string text)
        {
            optionText = text;
        }

        /// <summary>
        /// 鼠标进入时显示提示
        /// </summary>
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (string.IsNullOrEmpty(optionText)) return;
            
            // 延迟创建tooltip
            if (tooltipPanel == null)
            {
                CreateTooltip();
            }
            
            if (tooltipPanel != null)
            {
                tooltipPanel.SetActive(true);
                
                if (tooltipText != null)
                {
                    tooltipText.text = optionText;
                }

                // 更新提示框位置（在鼠标上方）
                if (tooltipRect != null)
                {
                    Vector2 localPoint;
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        canvas.transform as RectTransform,
                        eventData.position,
                        eventData.pressEventCamera,
                        out localPoint
                    );
                    tooltipRect.localPosition = localPoint + tooltipOffset;
                }
                
                // 确保显示在最前
                tooltipPanel.transform.SetAsLastSibling();
            }
        }

        /// <summary>
        /// 鼠标离开时隐藏提示
        /// </summary>
        public void OnPointerExit(PointerEventData eventData)
        {
            if (tooltipPanel != null)
            {
                tooltipPanel.SetActive(false);
            }
        }
        
        private void OnDestroy()
        {
            // 清理创建的tooltip
            if (tooltipPanel != null)
            {
                Destroy(tooltipPanel);
            }
        }
    }
}
