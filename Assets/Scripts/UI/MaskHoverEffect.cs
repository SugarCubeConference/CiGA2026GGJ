using UnityEngine;
using UnityEngine.EventSystems;

namespace MaskGame.UI
{
    /// <summary>
    /// 面具悬停动画效果 - 鼠标悬停时放大并漂浮
    /// </summary>
    public class MaskHoverEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("缩放设置")]
        [SerializeField]
        private float hoverScale = 1.15f; // 悬停时的缩放比例

        [SerializeField]
        private float scaleSpeed = 8f; // 缩放过渡速度

        [Header("漂浮设置")]
        [SerializeField]
        private float floatAmplitude = 10f; // 漂浮幅度（像素）

        [SerializeField]
        private float floatSpeed = 2f; // 漂浮速度

        private bool isHovering = false;
        private RectTransform rectTransform;
        private Vector3 originalScale;
        private Vector2 originalPosition;
        private float floatTime = 0f;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            originalScale = rectTransform.localScale;
            originalPosition = rectTransform.anchoredPosition;
        }

        private void Update()
        {
            if (isHovering)
            {
                // 平滑缩放到悬停大小
                rectTransform.localScale = Vector3.Lerp(
                    rectTransform.localScale,
                    originalScale * hoverScale,
                    Time.deltaTime * scaleSpeed
                );

                // 上下漂浮动画
                floatTime += Time.deltaTime * floatSpeed;
                float yOffset = Mathf.Sin(floatTime) * floatAmplitude;
                rectTransform.anchoredPosition = new Vector2(
                    originalPosition.x,
                    originalPosition.y + yOffset
                );
            }
            else
            {
                // 平滑还原到原始大小
                rectTransform.localScale = Vector3.Lerp(
                    rectTransform.localScale,
                    originalScale,
                    Time.deltaTime * scaleSpeed
                );

                // 平滑还原到原始位置
                rectTransform.anchoredPosition = Vector2.Lerp(
                    rectTransform.anchoredPosition,
                    originalPosition,
                    Time.deltaTime * scaleSpeed
                );
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            isHovering = true;
            floatTime = 0f; // 重置漂浮时间，从底部开始
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            isHovering = false;
        }
    }
}
