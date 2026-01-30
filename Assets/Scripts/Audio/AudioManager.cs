using System.Collections.Generic;
using UnityEngine;

namespace MaskGame.Audio
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Sound Effects")]
        [SerializeField] private AudioClip[] soundClips;

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
            soundLibrary = new Dictionary<SoundType, AudioClip>();

            if (soundClips == null || soundClips.Length == 0)
            {
                return;
            }

            int enumLength = System.Enum.GetValues(typeof(SoundType)).Length;
            for (int i = 0; i < enumLength && i < soundClips.Length; i++)
            {
                SoundType soundType = (SoundType)i;
                soundLibrary[soundType] = soundClips[i];
            }
        }

        public void Play(SoundType soundType)
        {
            if (soundLibrary.TryGetValue(soundType, out AudioClip clip))
            {
                if (clip != null)
                {
                    audioSource.PlayOneShot(clip);
                }
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
