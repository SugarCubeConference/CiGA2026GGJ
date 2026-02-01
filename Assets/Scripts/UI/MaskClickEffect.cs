using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MaskGame.UI
{
    /// <summary>
    /// 面具点击特效 - 点击时播放sprite动画
    /// </summary>
    public class MaskClickEffect : MonoBehaviour, IPointerClickHandler
    {
        [Header("特效设置")]
        [Tooltip("特效sprite序列（按顺序排列）")]
        [SerializeField]
        private Sprite[] effectSprites;

        [Tooltip("每帧持续时间（秒）")]
        [SerializeField]
        private float frameTime = 0.05f;

        [Tooltip("特效显示位置偏移")]
        [SerializeField]
        private Vector2 effectOffset = Vector2.zero;

        [Tooltip("特效缩放大小")]
        [SerializeField]
        private float effectScale = 1f;

        private GameObject effectObject;
        private Image effectImage;
        private Coroutine playCoroutine;

        private void Awake()
        {
            // 创建特效GameObject
            effectObject = new GameObject("ClickEffect");
            effectObject.transform.SetParent(transform, false);

            RectTransform effectRect = effectObject.AddComponent<RectTransform>();
            effectRect.anchoredPosition = effectOffset;
            effectRect.localScale = Vector3.one * effectScale;
            effectRect.sizeDelta = new Vector2(100, 100); // 默认大小，会根据sprite调整

            effectImage = effectObject.AddComponent<Image>();
            effectImage.raycastTarget = false; // 不阻挡点击
            effectObject.SetActive(false);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            PlayEffect();
        }

        /// <summary>
        /// 播放特效动画
        /// </summary>
        public void PlayEffect()
        {
            if (effectSprites == null || effectSprites.Length == 0)
            {
                Debug.LogWarning("MaskClickEffect: 没有设置特效sprite序列");
                return;
            }

            // 如果正在播放，先停止
            if (playCoroutine != null)
            {
                StopCoroutine(playCoroutine);
            }

            playCoroutine = StartCoroutine(PlayEffectCoroutine());
        }

        private IEnumerator PlayEffectCoroutine()
        {
            effectObject.SetActive(true);

            // 播放每一帧
            for (int i = 0; i < effectSprites.Length; i++)
            {
                effectImage.sprite = effectSprites[i];

                // 根据sprite调整大小保持长宽比
                if (effectSprites[i] != null)
                {
                    RectTransform rect = effectImage.rectTransform;
                    rect.sizeDelta = new Vector2(
                        effectSprites[i].rect.width,
                        effectSprites[i].rect.height
                    );
                }

                yield return new WaitForSeconds(frameTime);
            }

            // 播放完成后隐藏
            effectObject.SetActive(false);
            playCoroutine = null;
        }

        private void OnDestroy()
        {
            if (playCoroutine != null)
            {
                StopCoroutine(playCoroutine);
            }
        }
    }
}
