using MaskGame.Managers;
using UnityEngine;
using UnityEngine.UI;

namespace MaskGame.UI
{
    /// <summary>
    /// 设置按钮UI - 打开设置面板并暂停倒计时
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class SettingButtonUI : MonoBehaviour
    {
        [Header("设置面板")]
        [SerializeField]
        private GameObject settingCanvas;

        private Button button;

        private void Awake()
        {
            button = GetComponent<Button>();
            if (button != null)
            {
                button.onClick.AddListener(OpenSettingPanel);
            }
        }

        private void OnDestroy()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(OpenSettingPanel);
            }
        }

        /// <summary>
        /// 打开设置面板并暂停倒计时
        /// </summary>
        public void OpenSettingPanel()
        {
            if (settingCanvas != null)
            {
                settingCanvas.SetActive(true);

                // 暂停倒计时
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.PauseTimer();
                }
            }
        }

        /// <summary>
        /// 禁用设置按针
        /// </summary>
        public void DisableButton()
        {
            if (button != null)
            {
                button.interactable = false;
            }
        }

        /// <summary>
        /// 启用设置按针
        /// </summary>
        public void EnableButton()
        {
            if (button != null)
            {
                button.interactable = true;
            }
        }
    }
}
