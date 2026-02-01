using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace MaskGame.UI
{
    /// <summary>
    /// NPC入场动画效果
    /// </summary>
    public class NPCEntranceAnimation : MonoBehaviour
    {
        [Header("入场设置")]
        [SerializeField]
        private float startOffsetX = -100f; // 起始X偏移（左侧）

        [SerializeField]
        private float moveDuration = 0.8f; // 移动持续时间

        [SerializeField]
        private AnimationCurve moveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("抖动设置")]
        [SerializeField]
        private float shakeAmplitude = 15f; // 抖动幅度

        [SerializeField]
        private float shakeDuration = 0.15f; // 单次抖动时间

        [SerializeField]
        private int shakeCount = 2; // 抖动次数

        private RectTransform rectTransform;
        private CanvasGroup canvasGroup;
        private Vector2 targetPosition;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();

            // 添加CanvasGroup用于淡入效果
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

        private void Start()
        {
            PlayEntranceAnimation();
        }

        public void PlayEntranceAnimation()
        {
            StartCoroutine(EntranceSequence());
        }

        private IEnumerator EntranceSequence()
        {
            // 保存目标位置
            targetPosition = rectTransform.anchoredPosition;

            // 设置起始位置和透明度
            rectTransform.anchoredPosition = new Vector2(
                targetPosition.x + startOffsetX,
                targetPosition.y
            );
            canvasGroup.alpha = 0f;

            // 淡入并平移到目标位置
            float elapsed = 0f;
            Vector2 startPosition = rectTransform.anchoredPosition;

            while (elapsed < moveDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / moveDuration;
                float curveT = moveCurve.Evaluate(t);

                // 平移
                rectTransform.anchoredPosition = Vector2.Lerp(
                    startPosition,
                    targetPosition,
                    curveT
                );

                // 淡入
                canvasGroup.alpha = Mathf.Lerp(0f, 1f, t);

                yield return null;
            }

            // 确保到达目标位置
            rectTransform.anchoredPosition = targetPosition;
            canvasGroup.alpha = 1f;

            // 等待短暂时间
            yield return new WaitForSeconds(0.1f);

            // 上下抖动表示说话
            for (int i = 0; i < shakeCount; i++)
            {
                yield return StartCoroutine(ShakeOnce());
            }
        }

        private IEnumerator ShakeOnce()
        {
            // 向上
            float elapsed = 0f;
            Vector2 startPos = rectTransform.anchoredPosition;
            Vector2 upPos = startPos + new Vector2(0, shakeAmplitude);

            while (elapsed < shakeDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / shakeDuration;
                rectTransform.anchoredPosition = Vector2.Lerp(startPos, upPos, t);
                yield return null;
            }

            // 向下回到原位
            elapsed = 0f;
            while (elapsed < shakeDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / shakeDuration;
                rectTransform.anchoredPosition = Vector2.Lerp(upPos, targetPosition, t);
                yield return null;
            }

            // 确保回到目标位置
            rectTransform.anchoredPosition = targetPosition;
        }
    }
}
