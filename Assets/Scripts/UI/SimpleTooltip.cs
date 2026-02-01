using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MaskGame.UI
{
    /// <summary>
    /// 简单的Tooltip组件 - 鼠标悬停显示提示文本
    /// </summary>
    public class SimpleTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("提示文本")]
        [SerializeField]
        [TextArea(2, 4)]
        private string tooltipText = "剩余耐心";

        [Header("样式设置")]
        [SerializeField]
        private Vector2 tooltipOffset = new Vector2(0, 30);

        [SerializeField]
        private Color backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.95f);

        [SerializeField]
        private Color textColor = Color.white;

        [SerializeField]
        private int fontSize = 16;

        [SerializeField]
        private int padding = 10;

        private GameObject tooltipPanel;
        private TextMeshProUGUI tooltipTextComponent;
        private RectTransform tooltipRect;
        private Canvas canvas;

        private void Awake()
        {
            // 查找Canvas
            canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("SimpleTooltip: 无法找到Canvas组件！");
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
            layoutGroup.padding = new RectOffset(padding, padding, padding, padding);
            layoutGroup.childAlignment = TextAnchor.MiddleCenter;
            layoutGroup.childControlWidth = true;
            layoutGroup.childControlHeight = true;
            layoutGroup.childForceExpandWidth = false;
            layoutGroup.childForceExpandHeight = false;

            // 创建文本对象
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(tooltipPanel.transform, false);

            RectTransform textRect = textObj.AddComponent<RectTransform>();

            tooltipTextComponent = textObj.AddComponent<TextMeshProUGUI>();
            tooltipTextComponent.fontSize = fontSize;
            tooltipTextComponent.color = textColor;
            tooltipTextComponent.alignment = TextAlignmentOptions.Center;
            tooltipTextComponent.enableWordWrapping = false;
            tooltipTextComponent.text = tooltipText;

            // 添加LayoutElement
            LayoutElement layoutElement = textObj.AddComponent<LayoutElement>();
            layoutElement.preferredWidth = -1; // 自动宽度
            layoutElement.flexibleWidth = 0;

            // 设置层级（确保显示在最前）
            tooltipPanel.transform.SetAsLastSibling();

            tooltipPanel.SetActive(false);
        }

        /// <summary>
        /// 设置提示文本
        /// </summary>
        public void SetTooltipText(string text)
        {
            tooltipText = text;
            if (tooltipTextComponent != null)
            {
                tooltipTextComponent.text = text;
            }
        }

        /// <summary>
        /// 鼠标进入时显示提示
        /// </summary>
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (string.IsNullOrEmpty(tooltipText))
                return;

            // 延迟创建tooltip
            if (tooltipPanel == null)
            {
                CreateTooltip();
            }

            if (tooltipPanel != null)
            {
                tooltipPanel.SetActive(true);

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
