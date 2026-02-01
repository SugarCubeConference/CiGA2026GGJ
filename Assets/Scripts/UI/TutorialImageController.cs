using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MaskGame.UI
{
    /// <summary>
    /// 教程图控制器 - 点击后直接关闭
    /// </summary>
    public class TutorialImageController : MonoBehaviour, IPointerClickHandler
    {
        private CanvasGroup canvasGroup;

        private void Awake()
        {
            // 确保有CanvasGroup来控制交互阻挡
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

        private void Start()
        {
            // 每次都显示教程，确保可见并阻挡交互
            gameObject.SetActive(true);
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;
        }

        /// <summary>
        /// 点击时直接关闭教程图
        /// </summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            // 禁用交互阻挡
            if (canvasGroup != null)
            {
                canvasGroup.blocksRaycasts = false;
            }
            
            // 隐藏教程图
            gameObject.SetActive(false);
            
            // 恢复游戏计时
            var gameManager = MaskGame.Managers.GameManager.Instance;
            if (gameManager != null)
            {
                gameManager.ResumeTimer();
            }
        }
    }
}
