using MaskGame.Managers;
using UnityEngine;
using UnityEngine.UI;

namespace MaskGame.UI
{
    /// <summary>
    /// 关闭设置面板按钮UI - 关闭设置面板并恢复倒计时
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class CloseSettingButtonUI : MonoBehaviour
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
                button.onClick.AddListener(CloseSettingPanel);
            }
        }

        private void OnDestroy()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(CloseSettingPanel);
            }
        }

        /// <summary>
        /// 关闭设置面板并恢复倒计时
        /// </summary>
        public void CloseSettingPanel()
        {
            if (settingCanvas != null)
            {
                settingCanvas.SetActive(false);

                // 恢复倒计时
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.ResumeTimer();
                }
            }
        }
    }
}
