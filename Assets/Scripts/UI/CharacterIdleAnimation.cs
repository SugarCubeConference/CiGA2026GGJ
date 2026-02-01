using UnityEngine;

namespace MaskGame.UI
{
    /// <summary>
    /// 主角头像呼吸动画效果
    /// </summary>
    public class CharacterIdleAnimation : MonoBehaviour
    {
        [Header("呼吸缩放设置")]
        [SerializeField]
        private bool enableBreathing = true;

        [SerializeField]
        private float breathingScale = 0.03f; // 呼吸缩放幅度（3%）

        [SerializeField]
        private float breathingSpeed = 0.8f; // 呼吸速度

        [Header("轻微旋转设置")]
        [SerializeField]
        private bool enableTilt = false;

        [SerializeField]
        private float tiltAngle = 2f; // 旋转角度

        [SerializeField]
        private float tiltSpeed = 0.6f; // 旋转速度

        private RectTransform rectTransform;
        private Vector2 originalPosition;
        private Vector3 originalScale;
        private float timeOffset;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            originalPosition = rectTransform.anchoredPosition;
            originalScale = rectTransform.localScale;

            // 添加随机时间偏移，避免多个角色同步
            timeOffset = Random.Range(0f, 2f * Mathf.PI);
        }

        private void Update()
        {
            float time = Time.time + timeOffset;

            // 呼吸缩放效果
            if (enableBreathing)
            {
                float scaleOffset = Mathf.Sin(time * breathingSpeed) * breathingScale;
                float scale = 1f + scaleOffset;
                rectTransform.localScale = originalScale * scale;
            }

            // 轻微旋转效果
            if (enableTilt)
            {
                float angle = Mathf.Sin(time * tiltSpeed) * tiltAngle;
                rectTransform.localRotation = Quaternion.Euler(0, 0, angle);
            }
        }
    }
}
