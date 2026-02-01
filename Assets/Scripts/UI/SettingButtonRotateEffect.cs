using UnityEngine;
using UnityEngine.EventSystems;

namespace MaskGame.UI
{
    /// <summary>
    /// 设置按钮旋转动画 - 鼠标悬停时持续旋转
    /// </summary>
    public class SettingButtonRotateEffect
        : MonoBehaviour,
            IPointerEnterHandler,
            IPointerExitHandler
    {
        [Header("旋转设置")]
        [SerializeField]
        private float rotateSpeed = 90f; // 旋转速度（度/秒）

        private bool isHovering = false;
        private RectTransform rectTransform;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
        }

        private void Update()
        {
            if (isHovering)
            {
                rectTransform.Rotate(0, 0, -rotateSpeed * Time.deltaTime);
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            isHovering = true;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            isHovering = false;
        }
    }
}
