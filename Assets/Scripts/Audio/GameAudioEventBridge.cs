using MaskGame.Managers;
using UnityEngine;

namespace MaskGame.Audio
{
    public class GameAudioEventBridge : MonoBehaviour
    {
        private void Start()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnAnswerResult.AddListener(OnAnswerResult);
                GameManager.Instance.OnGameOver.AddListener(OnGameOver);
                GameManager.Instance.OnDayComplete.AddListener(OnDayComplete);
            }
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnAnswerResult.RemoveListener(OnAnswerResult);
                GameManager.Instance.OnGameOver.RemoveListener(OnGameOver);
                GameManager.Instance.OnDayComplete.RemoveListener(OnDayComplete);
            }
        }

        private void OnAnswerResult(bool isCorrect, bool isTimeout, string feedback)
        {
            AudioManager audioManager = AudioManager.Instance;
            if (audioManager == null)
            {
                return;
            }

            if (isTimeout)
            {
                audioManager.Play(SoundType.Timeout);
            }
            else if (isCorrect)
            {
                audioManager.Play(SoundType.AnswerCorrect);
            }
            else
            {
                audioManager.Play(SoundType.AnswerWrong);
            }
        }

        private void OnGameOver()
        {
            AudioManager audioManager = AudioManager.Instance;
            if (audioManager != null)
            {
                audioManager.Play(SoundType.GameOver);
            }
        }

        private void OnDayComplete()
        {
            AudioManager audioManager = AudioManager.Instance;
            if (audioManager != null)
            {
                audioManager.Play(SoundType.DayComplete);
            }
        }
    }
}
