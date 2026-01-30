using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MaskGame.Data;
using MaskGame.Managers;

namespace MaskGame.UI
{
    /// <summary>
    /// UI管理器 - 控制所有UI元素的显示和更新
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        [Header("顶部HUD")]
        [SerializeField] private TextMeshProUGUI dayText;
        [SerializeField] private Slider progressBar;
        [SerializeField] private TextMeshProUGUI batteryText;
        [SerializeField] private Image[] batteryIcons;

        [Header("对话区域")]
        [SerializeField] private TextMeshProUGUI dialogueText;
        [SerializeField] private TextMeshProUGUI friendGroupText;
        [SerializeField] private GameObject dialoguePanel;

        [Header("倒计时")]
        [SerializeField] private Slider timeBar;
        [SerializeField] private TextMeshProUGUI timeText;
        [SerializeField] private Image timeBarFill;
        [SerializeField] private Color normalTimeColor = Color.white;
        [SerializeField] private Color warningTimeColor = Color.red;
        [SerializeField] private float warningThreshold = 2f;

        [Header("面具按钮")]
        [SerializeField] private Button[] maskButtons;
        [SerializeField] private TextMeshProUGUI[] maskButtonTexts;

        [Header("游戏结束面板")]
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private TextMeshProUGUI gameOverText;
        [SerializeField] private Button restartButton;

        [Header("高亮颜色")]
        [SerializeField] private Color highlightColor = Color.yellow;

        private GameManager gameManager;

        private void Awake()
        {
            gameManager = GameManager.Instance;
            if (gameManager == null)
            {
                Debug.LogError("GameManager未找到！");
                return;
            }

            // 订阅游戏事件
            gameManager.OnDayChanged.AddListener(UpdateDay);
            gameManager.OnProgressChanged.AddListener(UpdateProgress);
            gameManager.OnBatteryChanged.AddListener(UpdateBattery);
            gameManager.OnTimeChanged.AddListener(UpdateTime);
            gameManager.OnNewEncounter.AddListener(DisplayEncounter);
            gameManager.OnAnswerResult.AddListener(ShowAnswerFeedback);
            gameManager.OnGameOver.AddListener(ShowGameOver);

            // 设置面具按钮
            SetupMaskButtons();

            // 设置重启按钮
            if (restartButton != null)
            {
                restartButton.onClick.AddListener(OnRestartClicked);
            }

            // 初始化UI
            if (gameOverPanel != null)
                gameOverPanel.SetActive(false);
        }

        private void OnDestroy()
        {
            if (gameManager != null)
            {
                gameManager.OnDayChanged.RemoveListener(UpdateDay);
                gameManager.OnProgressChanged.RemoveListener(UpdateProgress);
                gameManager.OnBatteryChanged.RemoveListener(UpdateBattery);
                gameManager.OnTimeChanged.RemoveListener(UpdateTime);
                gameManager.OnNewEncounter.RemoveListener(DisplayEncounter);
                gameManager.OnAnswerResult.RemoveListener(ShowAnswerFeedback);
                gameManager.OnGameOver.RemoveListener(ShowGameOver);
            }
        }

        /// <summary>
        /// 设置面具按钮
        /// </summary>
        private void SetupMaskButtons()
        {
            for (int i = 0; i < maskButtons.Length; i++)
            {
                if (maskButtons[i] != null)
                {
                    MaskType maskType = (MaskType)i;
                    maskButtons[i].onClick.AddListener(() => OnMaskClicked(maskType));

                    // 设置按钮文本
                    if (maskButtonTexts[i] != null)
                    {
                        maskButtonTexts[i].text = $"Mask {(int)maskType + 1}";
                    }
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
        /// 更新进度条
        /// </summary>
        private void UpdateProgress(float progress)
        {
            if (progressBar != null)
            {
                progressBar.value = progress;
            }
        }

        /// <summary>
        /// 更新社交电池
        /// </summary>
        private void UpdateBattery(int battery)
        {
            if (batteryText != null)
            {
                batteryText.text = $"{battery}";
            }

            // 更新电池图标
            if (batteryIcons != null)
            {
                for (int i = 0; i < batteryIcons.Length; i++)
                {
                    if (batteryIcons[i] != null)
                    {
                        batteryIcons[i].enabled = i < battery;
                    }
                }
            }
        }

        /// <summary>
        /// 更新倒计时
        /// </summary>
        private void UpdateTime(float remainingTime)
        {
            if (timeBar != null)
            {
                float maxTime = gameManager.Config.GetDecisionTime(gameManager.CurrentDay);
                timeBar.value = remainingTime / maxTime;

                // 时间紧急时改变颜色
                if (timeBarFill != null)
                {
                    timeBarFill.color = remainingTime <= warningThreshold ? warningTimeColor : normalTimeColor;
                }
            }

            if (timeText != null)
            {
                timeText.text = $"{remainingTime:F1}s";
            }
        }

        /// <summary>
        /// 显示对话
        /// </summary>
        private void DisplayEncounter(EncounterData encounter)
        {
            if (encounter == null) return;

            // 显示朋友分组
            if (friendGroupText != null)
            {
                friendGroupText.text = encounter.friendGroup;
            }

            // 处理高亮文本
            if (dialogueText != null)
            {
                string displayText = encounter.dialogueText;

                // 替换关键词为高亮版本（使用TextMeshPro的富文本标记）
                if (encounter.highlightKeywords != null)
                {
                    foreach (string keyword in encounter.highlightKeywords)
                    {
                        if (!string.IsNullOrEmpty(keyword))
                        {
                            string colorHex = ColorUtility.ToHtmlStringRGB(highlightColor);
                            displayText = displayText.Replace(keyword, $"<color=#{colorHex}>{keyword}</color>");
                        }
                    }
                }

                dialogueText.text = displayText;
            }
        }

        /// <summary>
        /// 显示答案反馈
        /// </summary>
        private void ShowAnswerFeedback(bool isCorrect)
        {
            // 这里可以添加视觉反馈效果
            // 例如：屏幕闪烁、抖动等
            if (isCorrect)
            {
                // 正确反馈
                Debug.Log("选择正确！");
            }
            else
            {
                // 错误反馈（可以添加屏幕抖动效果）
                Debug.Log("选择错误！");
                StartCoroutine(ScreenShake());
            }
        }

        /// <summary>
        /// 屏幕抖动效果
        /// </summary>
        private System.Collections.IEnumerator ScreenShake()
        {
            if (dialoguePanel == null) yield break;

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
        /// 显示游戏结束面板
        /// </summary>
        private void ShowGameOver()
        {
            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(true);
            }

            if (gameOverText != null)
            {
                gameOverText.text = $"社死！\n坚持了 {gameManager.CurrentDay} 天";
            }
        }

        /// <summary>
        /// 面具按钮点击
        /// </summary>
        private void OnMaskClicked(MaskType maskType)
        {
            gameManager.SelectMask(maskType);
        }

        /// <summary>
        /// 重启按钮点击
        /// </summary>
        private void OnRestartClicked()
        {
            if (gameOverPanel != null)
                gameOverPanel.SetActive(false);

            gameManager.RestartGame();
        }
    }
}