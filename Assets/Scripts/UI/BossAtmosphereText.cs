using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MaskGame.UI
{
    /// <summary>
    /// BOSS战氛围文本 - 随机生成红色文本营造压迫感
    /// </summary>
    public class BossAtmosphereText : MonoBehaviour
    {
        [Header("文本内容")]
        [SerializeField]
        [TextArea(3, 10)]
        private string[] atmosphereTexts = new string[]
        {
            "句句都顺了人意，那真正的我去哪里了？",
            "这般违心，值得吗？",
            "用谎言抚平了风波，但心底的裂痕要如何愈合？",
            "脱口而出的那些话，到底处于谁的心意？",
            "再继续这样欺骗自己和他人真的可以吗？",
            "真正的我被安放到了哪里？"
        };

        [Header("生成设置")]
        [SerializeField]
        [Tooltip("生成间隔（秒）")]
        private float spawnInterval = 3f;

        [SerializeField]
        [Tooltip("每次最多同时存在的文本数")]
        private int maxConcurrentTexts = 3;

        [Header("文本样式")]
        [SerializeField]
        private Color textColor = new Color(1f, 0.2f, 0.2f, 1f); // 红色

        [SerializeField]
        private int fontSize = 24;

        [SerializeField]
        [Tooltip("打字机速度（秒/字）")]
        private float typewriterSpeed = 0.05f;

        [SerializeField]
        [Tooltip("文本显示完成后停留时间（秒）")]
        private float displayDuration = 2f;

        [SerializeField]
        [Tooltip("淡出时间（秒）")]
        private float fadeOutDuration = 0.5f;

        [Header("位置设置")]
        [SerializeField]
        [Tooltip("屏幕边缘留白比例（0-0.5）")]
        private float screenPadding = 0.1f;

        [SerializeField]
        [Tooltip("随机旋转角度范围")]
        private float maxRotation = 15f;

        private Canvas canvas;
        private RectTransform canvasRect;
        private Coroutine spawnCoroutine;
        private List<GameObject> activeTexts = new List<GameObject>();
        private bool isActive = false;

        private void Awake()
        {
            canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("BossAtmosphereText: 找不到Canvas组件！");
                return;
            }

            canvasRect = canvas.GetComponent<RectTransform>();
        }

        /// <summary>
        /// 开始生成氛围文本
        /// </summary>
        public void StartGenerating()
        {
            if (!enabled || !gameObject.activeInHierarchy)
            {
                enabled = true;
                gameObject.SetActive(true);
            }
            
            if (isActive)
            {
                return;
            }

            isActive = true;
            if (spawnCoroutine != null)
            {
                StopCoroutine(spawnCoroutine);
            }
            spawnCoroutine = StartCoroutine(SpawnTextCoroutine());
        }

        /// <summary>
        /// 停止生成氛围文本
        /// </summary>
        public void StopGenerating()
        {
            isActive = false;
            if (spawnCoroutine != null)
            {
                StopCoroutine(spawnCoroutine);
                spawnCoroutine = null;
            }

            // 清理所有现有文本
            foreach (GameObject textObj in activeTexts)
            {
                if (textObj != null)
                {
                    Destroy(textObj);
                }
            }
            activeTexts.Clear();
        }

        private IEnumerator SpawnTextCoroutine()
        {
            while (isActive)
            {
                // 如果当前文本数未超过限制，生成新文本
                if (activeTexts.Count < maxConcurrentTexts)
                {
                    SpawnRandomText();
                }

                yield return new WaitForSeconds(spawnInterval);
            }
        }

        private void SpawnRandomText()
        {
            if (atmosphereTexts == null || atmosphereTexts.Length == 0)
            {
                return;
            }

            // 随机选择文本
            string text = atmosphereTexts[Random.Range(0, atmosphereTexts.Length)];

            // 创建文本对象
            GameObject textObj = new GameObject("AtmosphereText");
            textObj.transform.SetParent(canvas.transform, false);

            RectTransform rectTransform = textObj.AddComponent<RectTransform>();
            rectTransform.pivot = new Vector2(0.5f, 0.5f);

            // 随机位置（屏幕内，考虑边距）
            float width = canvasRect.rect.width;
            float height = canvasRect.rect.height;
            float paddingX = width * screenPadding;
            float paddingY = height * screenPadding;

            float randomX = Random.Range(-width / 2 + paddingX, width / 2 - paddingX);
            float randomY = Random.Range(-height / 2 + paddingY, height / 2 - paddingY);
            rectTransform.anchoredPosition = new Vector2(randomX, randomY);

            // 随机旋转
            float randomRotation = Random.Range(-maxRotation, maxRotation);
            rectTransform.localRotation = Quaternion.Euler(0, 0, randomRotation);

            // 添加CanvasGroup用于淡入淡出
            CanvasGroup canvasGroup = textObj.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 1f;

            // 添加TextMeshProUGUI组件
            TextMeshProUGUI tmpText = textObj.AddComponent<TextMeshProUGUI>();
            tmpText.text = text;
            tmpText.fontSize = fontSize;
            tmpText.color = textColor;
            tmpText.fontStyle = FontStyles.Bold;
            tmpText.alignment = TextAlignmentOptions.Center;
            tmpText.enableWordWrapping = true;
            tmpText.raycastTarget = false; // 不阻挡点击

            // 设置宽度限制
            rectTransform.sizeDelta = new Vector2(width * 0.4f, 100);

            // 添加到活动列表
            activeTexts.Add(textObj);

            // 启动文本动画
            StartCoroutine(TextLifecycleCoroutine(textObj, tmpText, canvasGroup, text));
        }

        private IEnumerator TextLifecycleCoroutine(
            GameObject textObj,
            TextMeshProUGUI tmpText,
            CanvasGroup canvasGroup,
            string fullText
        )
        {
            // 打字机效果
            tmpText.maxVisibleCharacters = 0;
            int charCount = fullText.Length;

            for (int i = 0; i < charCount; i++)
            {
                tmpText.maxVisibleCharacters = i + 1;
                yield return new WaitForSeconds(typewriterSpeed);
            }

            // 显示完成，等待
            yield return new WaitForSeconds(displayDuration);

            // 淡出
            float elapsed = 0f;
            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeOutDuration);
                yield return null;
            }

            // 销毁对象
            activeTexts.Remove(textObj);
            Destroy(textObj);
        }

        private void OnDestroy()
        {
            StopGenerating();
        }
    }
}
