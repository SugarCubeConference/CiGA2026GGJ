using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MaskGame.UI
{
    /// <summary>
    /// 教程图控制器 - 点击后隐藏且不再显示
    /// </summary>
    public class TutorialImageController : MonoBehaviour, IPointerClickHandler
    {
        [Header("设置")]
        [SerializeField]
        [Tooltip("用于保存状态的PlayerPrefs键名")]
        private string saveKey = "Tutorial_Shown";

        [SerializeField]
        [Tooltip("是否允许重置（调试用）")]
        private bool allowReset = false;

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
            // 检查是否已经显示过教程
            if (HasShownTutorial())
            {
                // 已经显示过，直接隐藏并恢复计时
                gameObject.SetActive(false);
                canvasGroup.blocksRaycasts = false;
                
                var gameManager = MaskGame.Managers.GameManager.Instance;
                if (gameManager != null)
                {
                    gameManager.ResumeTimer();
                }
            }
            else
            {
                // 第一次显示，确保可见并阻挡交互
                gameObject.SetActive(true);
                canvasGroup.blocksRaycasts = true;
                canvasGroup.interactable = true;
            }
        }

        /// <summary>
        /// 点击时隐藏教程图并保存状态
        /// </summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            // 标记已显示
            MarkTutorialAsShown();
            
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

        /// <summary>
        /// 检查教程是否已显示过
        /// </summary>
        private bool HasShownTutorial()
        {
            return PlayerPrefs.GetInt(saveKey, 0) == 1;
        }

        /// <summary>
        /// 标记教程已显示
        /// </summary>
        private void MarkTutorialAsShown()
        {
            PlayerPrefs.SetInt(saveKey, 1);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// 重置教程状态（调试用）
        /// </summary>
        [ContextMenu("重置教程状态")]
        public void ResetTutorial()
        {
            if (allowReset)
            {
                PlayerPrefs.DeleteKey(saveKey);
                PlayerPrefs.Save();
                Debug.Log("教程状态已重置");
            }
            else
            {
                Debug.LogWarning("需要在Inspector中启用 'Allow Reset' 才能重置");
            }
        }

        private void OnValidate()
        {
            // 确保每个教程图有唯一的保存键
            if (string.IsNullOrEmpty(saveKey))
            {
                saveKey = "Tutorial_" + gameObject.name;
            }
        }
    }
}
