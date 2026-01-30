using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

namespace MaskGame.UI
{
    public class GameWinUI : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private TextMeshProUGUI messageText;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button quitButton;

        private void Start()
        {
            if (restartButton != null)
            {
                restartButton.onClick.AddListener(OnRestart);
            }

            if (quitButton != null)
            {
                quitButton.onClick.AddListener(OnQuit);
            }

            DisplayWinInfo();
        }

        private void DisplayWinInfo()
        {
            if (titleText != null)
            {
                titleText.text = "WIN!";
            }

            int correct = PlayerPrefs.GetInt("CorrectAnswers", 0);
            int total = PlayerPrefs.GetInt("TotalAnswers", 0);

            if (scoreText != null)
            {
                scoreText.text = $"Score: {correct}/{total}";
            }

            if (messageText != null)
            {
                float accuracy = total > 0 ? (float)correct / total : 0;

                if (accuracy >= 0.9f)
                {
                    messageText.text = "PERFECT!";
                }
                else if (accuracy >= 0.7f)
                {
                    messageText.text = "GOOD!";
                }
                else if (accuracy >= 0.5f)
                {
                    messageText.text = "OK~";
                }
                else
                {
                    messageText.text = "...";
                }
            }
        }

            if (messageText != null)
            {
                float accuracy = total > 0 ? (float)correct / total : 0;

                if (accuracy >= 0.9f)
                {
                    messageText.text = "PERFECT!";
                }
                else if (accuracy >= 0.7f)
                {
                    messageText.text = "GOOD!";
                }
                else if (accuracy >= 0.5f)
                {
                    messageText.text = "OK~";
                }
                else
                {
                    messageText.text = "...";
                }
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
