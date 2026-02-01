using MaskGame.Managers;
using UnityEngine;

namespace MaskGame.UI
{
    /// <summary>
    /// 角色受伤音频播放器 - 监听生命值变化并播放受伤音效
    /// 支持多音效随机播放、音调变化、冷却时间等高级功能
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class PlayerHurtAudio : MonoBehaviour
    {
        [Header("音频设置")]
        [SerializeField]
        [Tooltip("受伤时播放的音频片段（支持多个随机播放）")]
        private AudioClip[] hurtSounds;

        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("受伤音效音量")]
        private float volume = 1f;

        [Header("音效变化")]
        [SerializeField]
        [Tooltip("启用音调随机变化")]
        private bool randomizePitch = true;

        [SerializeField]
        [Tooltip("音调变化范围")]
        private Vector2 pitchRange = new Vector2(0.9f, 1.1f);

        [Header("播放控制")]
        [SerializeField]
        [Tooltip("音效冷却时间（秒），防止短时间重复播放")]
        private float cooldownTime = 0.1f;

        [SerializeField]
        [Tooltip("根据剩余生命值调整音量")]
        private bool adjustVolumeByHealth = false;

        [SerializeField]
        [Tooltip("生命值越低音量越大的倍数")]
        private float lowHealthVolumeMultiplier = 1.5f;

        private AudioSource audioSource;
        private int previousHealth = -1;
        private float lastPlayTime = -999f;

        private void Awake()
        {
            InitializeAudioSource();
        }

        private void OnEnable()
        {
            // 不在OnEnable中注册，因为GameManager可能还未初始化
        }

        private void OnDisable()
        {
            UnregisterEvents();
        }

        private void Start()
        {
            // 在Start中注册事件，确保GameManager已初始化
            RegisterEvents();
        }

        private void InitializeAudioSource()
        {
            audioSource = GetComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.loop = false;
        }

        private void RegisterEvents()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnBatteryChanged.AddListener(OnHealthChanged);
                // 立即初始化健康值
                InitializePreviousHealth();
            }
        }

        private void InitializePreviousHealth()
        {
            if (GameManager.Instance != null && previousHealth == -1)
            {
                previousHealth = GameManager.Instance.SocialBattery;
            }
        }

        private void UnregisterEvents()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnBatteryChanged.RemoveListener(OnHealthChanged);
            }
        }

        private void OnHealthChanged(int currentHealth)
        {
            // 检测生命值是否降低（受伤）
            if (ShouldPlayHurtSound(currentHealth))
            {
                PlayHurtSound(currentHealth);
            }

            previousHealth = currentHealth;
        }

        private bool ShouldPlayHurtSound(int currentHealth)
        {
            // 未初始化
            if (previousHealth == -1)
                return false;

            // 生命值未降低
            if (currentHealth >= previousHealth)
                return false;

            // 冷却时间未结束
            if (Time.time - lastPlayTime < cooldownTime)
                return false;

            // 没有配置音效
            if (hurtSounds == null || hurtSounds.Length == 0)
                return false;

            return true;
        }

        private void PlayHurtSound(int currentHealth)
        {
            if (audioSource == null)
            {
                return;
            }

            // 随机选择一个受伤音效
            AudioClip selectedClip = GetRandomHurtSound();
            if (selectedClip == null)
            {
                return;
            }

            // 计算音量（根据健康值调整）
            float adjustedVolume = CalculateVolume(currentHealth);

            // 设置音调
            if (randomizePitch)
            {
                audioSource.pitch = Random.Range(pitchRange.x, pitchRange.y);
            }
            else
            {
                audioSource.pitch = 1f;
            }

            // 播放音效
            audioSource.PlayOneShot(selectedClip, adjustedVolume);
            lastPlayTime = Time.time;
        }

        private AudioClip GetRandomHurtSound()
        {
            // 过滤掉空引用
            var validClips = System.Array.FindAll(hurtSounds, clip => clip != null);

            if (validClips.Length == 0)
                return null;

            return validClips[Random.Range(0, validClips.Length)];
        }

        private float CalculateVolume(int currentHealth)
        {
            if (!adjustVolumeByHealth)
                return volume;

            // 获取最大生命值（初始值）
            int maxHealth = GameManager.Instance != null ? 4 : 4; // 默认4条血

            // 生命值比例 (0-1)
            float healthRatio = Mathf.Clamp01((float)currentHealth / maxHealth);

            // 生命值越低，音量越大
            float volumeMultiplier = Mathf.Lerp(lowHealthVolumeMultiplier, 1f, healthRatio);

            return volume * volumeMultiplier;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // 编辑器验证
            if (cooldownTime < 0f)
                cooldownTime = 0f;

            if (lowHealthVolumeMultiplier < 1f)
                lowHealthVolumeMultiplier = 1f;
        }
#endif
    }
}
