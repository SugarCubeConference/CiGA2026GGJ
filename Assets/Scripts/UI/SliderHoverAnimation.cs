using UnityEngine;
using UnityEngine.EventSystems;

namespace MaskGame.UI
{
    /// <summary>
    /// 滑块悬停动画 - 鼠标移上时放大，移开时缩小
    /// </summary>
    public class SliderHoverAnimation : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("动画设置")]
        [SerializeField]
        [Tooltip("悬停时的缩放比例")]
        private float hoverScale = 1.1f;

        [SerializeField]
        [Tooltip("动画持续时间（秒）")]
        private float animationDuration = 0.2f;

        [SerializeField]
        [Tooltip("缩放曲线")]
        private AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        private Vector3 originalScale;
        private Vector3 targetScale;
        private float currentTime = 0f;
        private bool isAnimating = false;
        private bool isHovering = false;

        private void Awake()
        {
            originalScale = transform.localScale;
            targetScale = originalScale;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            isHovering = true;
            targetScale = originalScale * hoverScale;
            StartAnimation();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            isHovering = false;
            targetScale = originalScale;
            StartAnimation();
        }

        private void StartAnimation()
        {
            currentTime = 0f;
            isAnimating = true;
        }

        private void Update()
        {
            if (!isAnimating)
                return;

            currentTime += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(currentTime / animationDuration);
            float curveValue = scaleCurve.Evaluate(progress);

            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, curveValue);

            if (progress >= 1f)
            {
                transform.localScale = targetScale;
                isAnimating = false;
            }
        }

        private void OnDisable()
        {
            // 禁用时重置缩放
            transform.localScale = originalScale;
            isAnimating = false;
        }
    }
}
