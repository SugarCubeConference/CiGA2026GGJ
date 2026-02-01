using UnityEngine;
using UnityEngine.UI;

namespace MaskGame.UI
{
    /// <summary>
    /// 按钮点击音效 - 独立挂载版
    /// </summary>
    [RequireComponent(typeof(Button))] // 自动添加Button组件，避免遗漏
    public class UIButtonSound : MonoBehaviour
    {
        [Header("按钮音效设置")]
        [SerializeField] private AudioClip clickClip; // 点击音效资源
        [SerializeField] [Range(0, 1)] private float volume = 0.8f; // 音效音量

        private Button _button;
        private AudioSource _audioSource;

        private void Awake()
        {
            // 获取自身Button组件
            _button = GetComponent<Button>();
            // 查找全局的AudioManager（提前创建的空物体），获取其AudioSource
            _audioSource = GameObject.Find("AudioManager").GetComponent<AudioSource>();
            
            // 绑定按钮点击事件（无需在Inspector手动拖曳）
            _button.onClick.AddListener(PlayClickSound);
        }

        /// <summary>
        /// 播放点击音效
        /// </summary>
        private void PlayClickSound()
        {
            // 判空：避免无音效/无音频源时报错
            if (clickClip != null && _audioSource != null)
            {
                // PlayOneShot：播放短音效，不打断当前播放的其他音效（适合按钮、特效音）
                _audioSource.PlayOneShot(clickClip, volume);
            }
        }

        // 销毁时移除事件监听，避免内存泄漏
        private void OnDestroy()
        {
            if (_button != null)
            {
                _button.onClick.RemoveListener(PlayClickSound);
            }
        }
    }
}