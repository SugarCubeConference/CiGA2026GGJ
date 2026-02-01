using UnityEngine;
using UnityEngine.Audio;

namespace MaskGame.Managers
{
    /// <summary>
    /// 音频管理器 - 全局音量控制
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("音频混合器")]
        [SerializeField]
        [Tooltip("AudioMixer资源，如果不使用可留空")]
        private AudioMixer audioMixer;

        [Header("BGM设置")]
        [SerializeField]
        private AudioSource bgmSource;

        [SerializeField]
        private AudioClip normalBGM;

        [SerializeField]
        private AudioClip bossBGM;

        [Header("音量设置")]
        [SerializeField]
        [Range(0f, 1f)]
        private float masterVolume = 1f;

        [SerializeField]
        [Range(0f, 1f)]
        private float musicVolume = 1f;

        [SerializeField]
        [Range(0f, 1f)]
        private float sfxVolume = 1f;

        // PlayerPrefs键名
        private const string MASTER_VOLUME_KEY = "MasterVolume";
        private const string MUSIC_VOLUME_KEY = "MusicVolume";
        private const string SFX_VOLUME_KEY = "SFXVolume";

        // AudioMixer参数名
        private const string MASTER_VOLUME_PARAM = "MasterVolume";
        private const string MUSIC_VOLUME_PARAM = "MusicVolume";
        private const string SFX_VOLUME_PARAM = "SFXVolume";

        public float MasterVolume => masterVolume;
        public float MusicVolume => musicVolume;
        public float SFXVolume => sfxVolume;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                LoadVolumeSettings();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// 加载保存的音量设置
        /// </summary>
        private void LoadVolumeSettings()
        {
            masterVolume = PlayerPrefs.GetFloat(MASTER_VOLUME_KEY, 1f);
            musicVolume = PlayerPrefs.GetFloat(MUSIC_VOLUME_KEY, 1f);
            sfxVolume = PlayerPrefs.GetFloat(SFX_VOLUME_KEY, 1f);

            ApplyAllVolumes();
        }

        /// <summary>
        /// 设置主音量
        /// </summary>
        public void SetMasterVolume(float volume)
        {
            masterVolume = Mathf.Clamp01(volume);
            PlayerPrefs.SetFloat(MASTER_VOLUME_KEY, masterVolume);
            PlayerPrefs.Save();

            ApplyMasterVolume();
        }

        /// <summary>
        /// 设置音乐音量
        /// </summary>
        public void SetMusicVolume(float volume)
        {
            musicVolume = Mathf.Clamp01(volume);
            PlayerPrefs.SetFloat(MUSIC_VOLUME_KEY, musicVolume);
            PlayerPrefs.Save();

            ApplyMusicVolume();
        }

        /// <summary>
        /// 设置音效音量
        /// </summary>
        public void SetSFXVolume(float volume)
        {
            sfxVolume = Mathf.Clamp01(volume);
            PlayerPrefs.SetFloat(SFX_VOLUME_KEY, sfxVolume);
            PlayerPrefs.Save();

            ApplySFXVolume();
        }

        /// <summary>
        /// 应用所有音量设置
        /// </summary>
        private void ApplyAllVolumes()
        {
            ApplyMasterVolume();
            ApplyMusicVolume();
            ApplySFXVolume();
        }

        /// <summary>
        /// 应用主音量
        /// </summary>
        private void ApplyMasterVolume()
        {
            if (audioMixer != null)
            {
                // 转换为分贝值：20 * log10(volume)
                // 0.0001f对应-80dB (静音)
                float db = masterVolume > 0.0001f ? 20f * Mathf.Log10(masterVolume) : -80f;
                audioMixer.SetFloat(MASTER_VOLUME_PARAM, db);
            }
            else
            {
                // 如果没有AudioMixer，直接设置AudioListener音量
                AudioListener.volume = masterVolume;
            }
        }

        /// <summary>
        /// 应用音乐音量
        /// </summary>
        private void ApplyMusicVolume()
        {
            if (audioMixer != null)
            {
                float db = musicVolume > 0.0001f ? 20f * Mathf.Log10(musicVolume) : -80f;
                audioMixer.SetFloat(MUSIC_VOLUME_PARAM, db);
            }
        }

        /// <summary>
        /// 应用音效音量
        /// </summary>
        private void ApplySFXVolume()
        {
            if (audioMixer != null)
            {
                float db = sfxVolume > 0.0001f ? 20f * Mathf.Log10(sfxVolume) : -80f;
                audioMixer.SetFloat(SFX_VOLUME_PARAM, db);
            }
        }

        /// <summary>
        /// 重置所有音量为默认值
        /// </summary>
        public void ResetToDefault()
        {
            SetMasterVolume(1f);
            SetMusicVolume(1f);
            SetSFXVolume(1f);
        }

        /// <summary>
        /// 播放BOSS战BGM
        /// </summary>
        public void PlayBossBGM()
        {
            if (bgmSource != null && bossBGM != null)
            {
                bgmSource.clip = bossBGM;
                bgmSource.loop = true;
                bgmSource.Play();
            }
        }

        /// <summary>
        /// 播放普通BGM
        /// </summary>
        public void PlayNormalBGM()
        {
            if (bgmSource != null && normalBGM != null)
            {
                bgmSource.clip = normalBGM;
                bgmSource.loop = true;
                bgmSource.Play();
            }
        }
    }
}
