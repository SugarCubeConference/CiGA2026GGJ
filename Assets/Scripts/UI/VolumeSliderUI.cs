using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MaskGame.UI
{
    using MaskGame.Managers;

    /// <summary>
    /// 音量滑块UI控制器
    /// </summary>
    [RequireComponent(typeof(Slider))]
    public class VolumeSliderUI : MonoBehaviour
    {
        [Header("音量类型")]
        [SerializeField]
        private VolumeType volumeType = VolumeType.Master;

        [Header("UI组件（可选）")]
        [SerializeField]
        [Tooltip("显示音量百分比的文本")]
        private TextMeshProUGUI percentText;

        private Slider slider;
        private bool isUpdatingValue = false;

        private void Awake()
        {
            slider = GetComponent<Slider>();
        }

        private void Start()
        {
            if (slider != null)
            {
                slider.onValueChanged.AddListener(OnSliderValueChanged);
            }
        }

        private void OnEnable()
        {
            // 每次激活时刷新滑块值
            Invoke(nameof(UpdateSliderFromAudioManager), 0.1f);
        }

        private void OnDestroy()
        {
            if (slider != null)
            {
                slider.onValueChanged.RemoveListener(OnSliderValueChanged);
            }
        }

        /// <summary>
        /// 从AudioManager更新滑块值
        /// </summary>
        private void UpdateSliderFromAudioManager()
        {
            if (AudioManager.Instance == null || slider == null)
                return;

            isUpdatingValue = true;

            float currentVolume = volumeType switch
            {
                VolumeType.Master => AudioManager.Instance.MasterVolume,
                VolumeType.Music => AudioManager.Instance.MusicVolume,
                VolumeType.SFX => AudioManager.Instance.SFXVolume,
                _ => 1f,
            };

            slider.value = currentVolume;
            UpdatePercentText(currentVolume);

            isUpdatingValue = false;
        }

        /// <summary>
        /// 滑块值改变时调用
        /// </summary>
        private void OnSliderValueChanged(float value)
        {
            if (isUpdatingValue || AudioManager.Instance == null)
                return;

            switch (volumeType)
            {
                case VolumeType.Master:
                    AudioManager.Instance.SetMasterVolume(value);
                    break;
                case VolumeType.Music:
                    AudioManager.Instance.SetMusicVolume(value);
                    break;
                case VolumeType.SFX:
                    AudioManager.Instance.SetSFXVolume(value);
                    break;
            }

            UpdatePercentText(value);
        }

        /// <summary>
        /// 更新百分比文本
        /// </summary>
        private void UpdatePercentText(float value)
        {
            if (percentText != null)
            {
                percentText.text = $"{Mathf.RoundToInt(value * 100)}%";
            }
        }

        /// <summary>
        /// 音量类型枚举
        /// </summary>
        public enum VolumeType
        {
            Master, // 主音量
            Music, // 音乐
            SFX, // 音效
        }
    }
}
