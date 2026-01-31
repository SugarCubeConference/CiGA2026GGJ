using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MaskGame.UI
{
    /// <summary>
    /// 退出按钮UI - 返回菜单场景
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class QuitButtonUI : MonoBehaviour
    {
        [Header("目标场景")]
        [SerializeField]
        private string menuSceneName = "Menu";

        private Button button;

        private void Awake()
        {
            button = GetComponent<Button>();
            if (button != null)
            {
                button.onClick.AddListener(QuitToMenu);
            }
        }

        private void OnDestroy()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(QuitToMenu);
            }
        }

        /// <summary>
        /// 返回菜单场景
        /// </summary>
        public void QuitToMenu()
        {
            if (MaskGame.UI.SceneTransition.Instance != null)
            {
                MaskGame.UI.SceneTransition.Instance.LoadSceneWithTransition(menuSceneName);
            }
            else
            {
                SceneManager.LoadScene(menuSceneName);
            }
        }
    }
}
