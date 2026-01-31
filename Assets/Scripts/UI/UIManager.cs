using MaskGame.Data;
using MaskGame.Managers;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MaskGame.UI
{
    /// <summary>
    /// UI管理器 - 控制所有UI元素的显示和更新
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        [Header("天数显示")]
        [SerializeField]
        private TextMeshProUGUI dayText;

        [Header("生命UI")]
        [SerializeField]
        private Image healthImage; // 显示健康值的图片

        [SerializeField]
        private Sprite[] healthSprites; // 0-4条血的sprite数组（长度5）

        [Header("对话区域")]
        [SerializeField]
        private TextMeshProUGUI dialogueText;

        [SerializeField]
        private TextMeshProUGUI friendGroupText;

        [SerializeField]
        private GameObject dialoguePanel;

        [Header("时间显示(斜杠)")]
        [SerializeField]
        private TextMeshProUGUI timeSlashText;

        [SerializeField]
        private float timePerSlash = 2f; // 每个斜杠代表2秒

        [SerializeField]
        private Color normalSlashColor = Color.white;

        [SerializeField]
        private Color warningSlashColor = Color.red;

        [SerializeField]
        private float warningThreshold = 2f; // 秒

        [Header("面具选项")]
        [SerializeField]
        private Image[] maskImages; // 4个面具Image

        [Header("打字机效果")]
        [SerializeField]
        private bool enableTypewriter = true; // 是否启用打字机效果

        private GameManager gameManager;
        private TypewriterEffect typewriterEffect;

        private void Awake()
        {
            gameManager = GameManager.Instance;
            if (gameManager == null)
            {
                UnityEngine.Debug.LogError(
                    "UIManager: GameManager.Instance为null！请确保场景中有GameManager对象。"
                );
                return;
            }

            // 订阅游戏事件
            gameManager.OnDayChanged.AddListener(UpdateDay);
            gameManager.OnBatteryChanged.AddListener(UpdateBattery);
            gameManager.OnTimeChanged.AddListener(UpdateTime);
            gameManager.OnNewEncounter.AddListener(DisplayEncounter);
            gameManager.OnAnswerResult.AddListener(ShowAnswerFeedback);

            // 设置面具按钮
            SetupMaskButtons();
            // 获取或添加TypewriterEffect组件
            if (enableTypewriter && dialogueText != null)
            {
                typewriterEffect = dialogueText.GetComponent<TypewriterEffect>();
                if (typewriterEffect == null)
                {
                    typewriterEffect = dialogueText.gameObject.AddComponent<TypewriterEffect>();
                }
            }
        }

        private void OnDestroy()
        {
            if (gameManager != null)
            {
                gameManager.OnDayChanged.RemoveListener(UpdateDay);
                gameManager.OnBatteryChanged.RemoveListener(UpdateBattery);
                gameManager.OnTimeChanged.RemoveListener(UpdateTime);
                gameManager.OnNewEncounter.RemoveListener(DisplayEncounter);
                gameManager.OnAnswerResult.RemoveListener(ShowAnswerFeedback);
            }
        }

        /// <summary>
        /// 设置面具图片点击事件
        /// </summary>
        private void SetupMaskButtons()
        {
            for (int i = 0; i < maskImages.Length; i++)
            {
                if (maskImages[i] != null)
                {
                    MaskType maskType = (MaskType)i;

                    // 添加或获取MaskOptionUI组件
                    MaskOptionUI optionUI = maskImages[i].GetComponent<MaskOptionUI>();
                    if (optionUI == null)
                    {
                        optionUI = maskImages[i].gameObject.AddComponent<MaskOptionUI>();
                    }

                    // 添加EventTrigger组件处理点击
                    EventTrigger trigger = maskImages[i].GetComponent<EventTrigger>();
                    if (trigger == null)
                    {
                        trigger = maskImages[i].gameObject.AddComponent<EventTrigger>();
                    }

                    // 清除旧事件
                    trigger.triggers.Clear();

                    // 添加点击事件
                    EventTrigger.Entry entry = new EventTrigger.Entry();
                    entry.eventID = EventTriggerType.PointerClick;
                    entry.callback.AddListener(
                        (data) =>
                        {
                            OnMaskClicked(maskType);
                        }
                    );
                    trigger.triggers.Add(entry);
                }
            }
        }

        /// <summary>
        /// 更新天数显示
        /// </summary>
        private void UpdateDay(int day)
        {
            if (dayText != null)
            {
                dayText.text = $"Day {day}";
            }
        }

        /// <summary>
        /// 更新社交电池/健康值
        /// </summary>
        private void UpdateBattery(int battery)
        {
            if (healthImage == null || healthSprites == null)
                return;

            // 限制battery在0-4范围内
            battery = Mathf.Clamp(battery, 0, 4);

            // 检查healthSprites数组长度
            if (battery >= 0 && battery < healthSprites.Length && healthSprites[battery] != null)
            {
                healthImage.sprite = healthSprites[battery];
            }
        }

        /// <summary>
        /// 更新倒计时 - 使用斜杠显示
        /// </summary>
        private void UpdateTime(float remainingTime)
        {
            if (timeSlashText == null)
            {
                UnityEngine.Debug.LogWarning(
                    "UIManager: timeSlashText未null，请在Inspector中连接Text_deadline (TMP)组件！"
                );
                return;
            }

            // 根据剩余时间计算斜杠数（每个斜杠代表2秒）
            int currentSlashes = Mathf.CeilToInt(remainingTime / timePerSlash);
            currentSlashes = Mathf.Max(0, currentSlashes); // 确保不为负数

            // 生成斜杠字符串
            string slashString = new string('/', currentSlashes);

            // 时间紧急时变红
            Color slashColor =
                remainingTime <= warningThreshold ? warningSlashColor : normalSlashColor;
            string colorHex = ColorUtility.ToHtmlStringRGB(slashColor);

            timeSlashText.text = $"<color=#{colorHex}>{slashString}</color>";
        }

        /// <summary>
        /// 显示对话
        /// </summary>
        private void DisplayEncounter(EncounterData encounter)
        {
            if (encounter == null)
                return;

            // 显示朋友分组
            if (friendGroupText != null)
            {
                friendGroupText.text = encounter.friendGroup;
            }

            // 显示对话文本（使用打字机效果）
            if (dialogueText != null)
            {
                if (enableTypewriter && typewriterEffect != null)
                {
                    // 使用打字机效果
                    typewriterEffect.PlayTypewriter(encounter.dialogueText);
                }
                else
                {
                    // 直接显示
                    dialogueText.text = encounter.dialogueText;
                }
            }

            // 更新面具选项文本到MaskOptionUI（鼠标悬停提示）
            if (encounter.optionTexts != null && encounter.optionTexts.Length >= 4)
            {
                for (int i = 0; i < maskImages.Length && i < encounter.optionTexts.Length; i++)
                {
                    if (maskImages[i] != null)
                    {
                        MaskOptionUI optionUI = maskImages[i].GetComponent<MaskOptionUI>();
                        if (optionUI != null)
                        {
                            optionUI.SetOptionText(encounter.optionTexts[i]);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 显示答案反馈 - 从鼠标位置飘出文本
        /// </summary>
        private void ShowAnswerFeedback(bool isCorrect, bool isTimeout, string feedbackText)
        {
            // 深绿色 (0, 128, 0) 和 深红色 (139, 0, 0)
            Color feedbackColor = isCorrect ? new Color(0f, 0.5f, 0f) : new Color(0.545f, 0f, 0f);

            // 创建飘浮反馈文本
            CreateFloatingText(feedbackText, Input.mousePosition, feedbackColor);

            if (!isCorrect)
            {
                // 错误反馈 - 屏幕抖动效果
                StartCoroutine(ScreenShake());
            }
        }

        /// <summary>
        /// 创建飘浮文本
        /// </summary>
        private void CreateFloatingText(string text, Vector3 screenPosition, Color color)
        {
            if (string.IsNullOrEmpty(text))
                return;

            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
                return;

            // 创建文本对象
            GameObject floatingObj = new GameObject("FloatingFeedback");
            floatingObj.transform.SetParent(canvas.transform, false);

            RectTransform rectTransform = floatingObj.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(400, 100);

            // 将屏幕坐标转为画布坐标
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas.transform as RectTransform,
                screenPosition,
                canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
                out localPoint
            );
            rectTransform.localPosition = localPoint;

            // 添加文本组件
            TextMeshProUGUI tmpText = floatingObj.AddComponent<TextMeshProUGUI>();
            tmpText.text = text;
            tmpText.fontSize = 32;
            tmpText.color = color;
            tmpText.alignment = TextAlignmentOptions.Center;
            tmpText.fontStyle = FontStyles.Bold;

            // 设置层级（最前）
            floatingObj.transform.SetAsLastSibling();

            // 启动飘浮动画
            StartCoroutine(FloatingAnimation(floatingObj, rectTransform, tmpText));
        }

        /// <summary>
        /// 飘浮动画 - 向上移动并淡出
        /// </summary>
        private System.Collections.IEnumerator FloatingAnimation(
            GameObject obj,
            RectTransform rectTransform,
            TextMeshProUGUI tmpText
        )
        {
            float duration = 1.5f; // 动画总时长
            float moveDistance = 100f; // 向上移动距离
            float elapsed = 0f;

            Vector3 startPos = rectTransform.localPosition;
            Vector3 targetPos = startPos + new Vector3(0, moveDistance, 0);
            Color startColor = tmpText.color;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / duration;

                // 向上移动（缓动）
                rectTransform.localPosition = Vector3.Lerp(startPos, targetPos, progress);

                // 淡出（后半段加速）
                float alpha = Mathf.Lerp(1f, 0f, Mathf.Pow(progress, 2));
                tmpText.color = new Color(startColor.r, startColor.g, startColor.b, alpha);

                yield return null;
            }

            // 销毁对象
            Destroy(obj);
        }

        /// <summary>
        /// 屏幕抖动效果
        /// </summary>
        private System.Collections.IEnumerator ScreenShake()
        {
            if (dialoguePanel == null)
                yield break;

            Vector3 originalPos = dialoguePanel.transform.localPosition;
            float duration = 0.3f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                float x = Random.Range(-10f, 10f);
                float y = Random.Range(-10f, 10f);
                dialoguePanel.transform.localPosition = originalPos + new Vector3(x, y, 0);

                elapsed += Time.deltaTime;
                yield return null;
            }

            dialoguePanel.transform.localPosition = originalPos;
        }

        /// <summary>
        /// 面具按钮点击
        /// </summary>
        private void OnMaskClicked(MaskType maskType)
        {
            gameManager.SelectMask(maskType);
        }
    }
}
