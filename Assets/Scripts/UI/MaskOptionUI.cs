using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MaskGame.UI
{
    /// <summary>
    /// 面具选项UI - 鼠标悬停显示选项文本（程序化生成tooltip）
    /// </summary>
    public class MaskOptionUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("选项数据")]
        [SerializeField]
        private string optionText;

        [Header("设置")]
        [SerializeField]
        private Vector2 tooltipOffset = new Vector2(0, 50);

        [SerializeField]
        private Vector2 tooltipSize = new Vector2(300, 80);

        [SerializeField]
        private Color backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.95f);

        [SerializeField]
        private Color textColor = Color.white;

        [SerializeField]
        private int fontSize = 18;

        [Header("悬停文字缩放")]
        [SerializeField]
        private float textHoverScale = 1.5f; // 悬停时文字放大倍数

        [SerializeField]
        private float textScaleSpeed = 10f; // 文字缩放速度

        private GameObject tooltipPanel;
        private TextMeshProUGUI tooltipText;
        private RectTransform tooltipRect;
        private Canvas canvas;
        private bool isTextHovering = false;
        private float currentFontSize;

        private void Awake()
        {
            // 查找Canvas
            canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("MaskOptionUI: 无法找到Canvas组件！");
            }
        }

        private void Update()
        {
            // 平滑更新文字字号
            if (tooltipText != null && tooltipPanel != null && tooltipPanel.activeSelf)
            {
                float targetFontSize = isTextHovering ? (fontSize * textHoverScale) : fontSize;
                currentFontSize = Mathf.Lerp(
                    currentFontSize,
                    targetFontSize,
                    Time.deltaTime * textScaleSpeed
                );
                tooltipText.fontSize = currentFontSize;
            }
        }

        /// <summary>
        /// 创建tooltip UI
        /// </summary>
        private void CreateTooltip()
        {
            if (tooltipPanel != null || canvas == null)
                return;

            // 创建tooltip面板
            tooltipPanel = new GameObject("Tooltip_" + gameObject.name);
            tooltipPanel.transform.SetParent(canvas.transform, false);

            tooltipRect = tooltipPanel.AddComponent<RectTransform>();
            tooltipRect.sizeDelta = tooltipSize;
            tooltipRect.pivot = new Vector2(0.5f, 0);

            // 添加背景Image
            Image bgImage = tooltipPanel.AddComponent<Image>();
            bgImage.color = backgroundColor;

            // 添加ContentSizeFitter使tooltip自适应大小
            ContentSizeFitter sizeFitter = tooltipPanel.AddComponent<ContentSizeFitter>();
            sizeFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // 添加LayoutGroup以支持padding
            VerticalLayoutGroup layoutGroup = tooltipPanel.AddComponent<VerticalLayoutGroup>();
            layoutGroup.padding = new RectOffset(15, 15, 10, 10);
            layoutGroup.childAlignment = TextAnchor.MiddleCenter;
            layoutGroup.childControlWidth = true;
            layoutGroup.childControlHeight = true;
            layoutGroup.childForceExpandWidth = false;
            layoutGroup.childForceExpandHeight = false;

            // 创建文本对象
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(tooltipPanel.transform, false);

            RectTransform textRect = textObj.AddComponent<RectTransform>();

            tooltipText = textObj.AddComponent<TextMeshProUGUI>();
            tooltipText.fontSize = fontSize;
            tooltipText.color = textColor;
            tooltipText.alignment = TextAlignmentOptions.Center;
            tooltipText.enableWordWrapping = true;

            // 添加LayoutElement以支持自适应大小
            LayoutElement layoutElement = textObj.AddComponent<LayoutElement>();
            layoutElement.preferredWidth = 280; // 最大宽度
            layoutElement.flexibleWidth = 0;

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
            if (string.IsNullOrEmpty(optionText))
                return;

            // 启动文字缩放
            isTextHovering = true;

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
                    // 初始化字号
                    currentFontSize = fontSize;
                    tooltipText.fontSize = currentFontSize;
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
            // 停止文字缩放
            isTextHovering = false;

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
