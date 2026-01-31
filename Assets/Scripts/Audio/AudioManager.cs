using System.Collections.Generic;
using UnityEngine;

namespace MaskGame.Audio
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Sound Effects")]
        [SerializeField]
        private AudioClip[] soundClips;

        private AudioSource audioSource;
        private Dictionary<SoundType, AudioClip> soundLibrary;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;

            InitializeSoundLibrary();
        }

        private void InitializeSoundLibrary()
        {
            int enumLength = System.Enum.GetValues(typeof(SoundType)).Length;
            soundLibrary = new Dictionary<SoundType, AudioClip>(enumLength);

            if (soundClips == null || soundClips.Length == 0)
            {
                Debug.LogWarning(
                    $"[AudioManager] soundClips is not assigned on '{name}'. Sound effects will be unavailable. Assign clips in the Inspector.",
                    this
                );
                return;
            }

            if (soundClips.Length != enumLength)
            {
                int mappedCount = Mathf.Min(enumLength, soundClips.Length);
                Debug.LogWarning(
                    $"[AudioManager] soundClips count ({soundClips.Length}) does not match SoundType enum count ({enumLength}). Only the first {mappedCount} entries will be mapped.",
                    this
                );
            }

            int mappedLength = Mathf.Min(enumLength, soundClips.Length);
            for (int i = 0; i < mappedLength; i++)
            {
                SoundType soundType = (SoundType)i;
                AudioClip clip = soundClips[i];
                if (clip == null)
                {
                    Debug.LogWarning(
                        $"[AudioManager] soundClips[{i}] is null for SoundType.{soundType}. This sound will be unavailable.",
                        this
                    );
                    continue;
                }

                soundLibrary[soundType] = clip;
            }
        }

        public void Play(SoundType soundType)
        {
            if (soundLibrary.TryGetValue(soundType, out AudioClip clip) && clip != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }

        public void SetVolume(float volume)
        {
            if (audioSource != null)
            {
                audioSource.volume = Mathf.Clamp01(volume);
            }
        }
    }
}
