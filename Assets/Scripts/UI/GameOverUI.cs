using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

namespace MaskGame.UI
{
    /// <summary>
    /// 游戏结束UI管理器
    /// </summary>
    public class GameOverUI : MonoBehaviour
    {
        [Header("UI元素")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI messageText;
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button quitButton;

        private void Start()
        {
            // 设置按钮事件
            if (restartButton != null)
            {
                restartButton.onClick.AddListener(OnRestart);
            }

            if (quitButton != null)
            {
                quitButton.onClick.AddListener(OnQuit);
            }

            // 显示游戏结束信息
            DisplayGameOverInfo();
        }

        private void DisplayGameOverInfo()
        {
            if (titleText != null)
            {
                titleText.text = "GAME OVER";
            }

            if (messageText != null)
            {
                messageText.text = "QAQ";
            }

            if (scoreText != null)
            {
                int correctAnswers = PlayerPrefs.GetInt("CorrectAnswers", 0);
                int totalAnswers = PlayerPrefs.GetInt("TotalAnswers", 0);

                scoreText.text = $"Score: {correctAnswers}/{totalAnswers}";
            }
        }

        private void OnRestart()
        {
            PlayerPrefs.DeleteKey("CorrectAnswers");
            PlayerPrefs.DeleteKey("TotalAnswers");
            PlayerPrefs.DeleteKey("GameWon");

            SceneManager.LoadScene("Main");
        }

        private void OnQuit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
