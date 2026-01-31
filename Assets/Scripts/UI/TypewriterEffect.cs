using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace MaskGame.UI
{
    /// <summary>
    /// 打字机效果 - 让文本逐字显示，支持音效和富文本漂浮效果
    /// </summary>
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class TypewriterEffect : MonoBehaviour
    {
        [Header("打字机设置")]
        [Tooltip("每个字的显示间隔（秒）")]
        [SerializeField]
        private float charDelay = 0.05f;

        [Tooltip("标点符号的额外延迟（秒）")]
        [SerializeField]
        private float punctuationDelay = 0.2f;

        [Tooltip("是否在开始时自动播放")]
        [SerializeField]
        private bool playOnEnable = false;

        [Header("音效设置")]
        [Tooltip("打字音效（每个字播放一次）")]
        [SerializeField]
        private AudioClip typingSound;

        [Tooltip("音效音量")]
        [SerializeField]
        [Range(0f, 1f)]
        private float soundVolume = 0.5f;

        [Tooltip("每隔几个字播放一次音效（1=每个字，2=每两个字）")]
        [SerializeField]
        private int soundFrequency = 1;

        [Header("富文本漂浮效果")]
        [Tooltip("是否启用富文本标记内容的漂浮效果")]
        [SerializeField]
        private bool enableFloatingEffect = true;

        [Tooltip("漂浮幅度")]
        [SerializeField]
        private float floatAmplitude = 5f;

        [Tooltip("漂浮速度")]
        [SerializeField]
        private float floatSpeed = 2f;

        private TextMeshProUGUI textComponent;
        private AudioSource audioSource;
        private Coroutine typewriterCoroutine;
        private Coroutine floatingCoroutine;
        private string fullText;
        private bool isTyping = false;
        private readonly List<int> floatingCharacterIndices = new List<int>();
        private TMP_MeshInfo[] cachedMeshInfo;

        private void Awake()
        {
            textComponent = GetComponent<TextMeshProUGUI>();

            // 创建AudioSource组件用于播放音效
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.loop = false;
            audioSource.volume = soundVolume;
        }

        private void OnEnable()
        {
            if (playOnEnable && !string.IsNullOrEmpty(textComponent.text))
            {
                PlayTypewriter(textComponent.text);
            }
        }

        /// <summary>
        /// 播放打字机效果
        /// </summary>
        public void PlayTypewriter(string text)
        {
            StopAllEffectCoroutines();

            fullText = text ?? string.Empty;
            textComponent.text = fullText;
            textComponent.maxVisibleCharacters = int.MaxValue;
            RefreshFloatingCharactersAndMeshCache();

            textComponent.maxVisibleCharacters = 0;
            typewriterCoroutine = StartCoroutine(TypewriterCoroutine());

            if (enableFloatingEffect && floatingCharacterIndices.Count > 0)
            {
                floatingCoroutine = StartCoroutine(FloatingEffectCoroutine());
            }
        }

        private void StopAllEffectCoroutines()
        {
            if (typewriterCoroutine != null)
            {
                StopCoroutine(typewriterCoroutine);
                typewriterCoroutine = null;
            }

            if (floatingCoroutine != null)
            {
                StopCoroutine(floatingCoroutine);
                floatingCoroutine = null;
            }

            isTyping = false;
        }

        private void RefreshFloatingCharactersAndMeshCache()
        {
            floatingCharacterIndices.Clear();

            textComponent.ForceMeshUpdate();
            TMP_TextInfo textInfo = textComponent.textInfo;
            int characterCount = textInfo.characterCount;

            for (int i = 0; i < characterCount; i++)
            {
                TMP_CharacterInfo charInfo = textInfo.characterInfo[i];
                if (HasFloatingStyle(charInfo))
                {
                    floatingCharacterIndices.Add(i);
                }
            }

            cachedMeshInfo = textInfo.CopyMeshInfoVertexData();
        }

        private bool HasFloatingStyle(TMP_CharacterInfo charInfo)
        {
            bool hasBold = (charInfo.style & FontStyles.Bold) == FontStyles.Bold;
            bool hasItalic = (charInfo.style & FontStyles.Italic) == FontStyles.Italic;
            bool hasUnderline = (charInfo.style & FontStyles.Underline) == FontStyles.Underline;

            return hasBold || hasItalic || hasUnderline;
        }

        /// <summary>
        /// 立即显示全部文本（跳过动画）
        /// </summary>
        public void SkipToEnd()
        {
            if (typewriterCoroutine != null)
            {
                StopCoroutine(typewriterCoroutine);
                typewriterCoroutine = null;
            }

            textComponent.text = fullText;
            textComponent.maxVisibleCharacters = int.MaxValue;
            isTyping = false;

            if (enableFloatingEffect && floatingCoroutine == null && floatingCharacterIndices.Count > 0)
            {
                floatingCoroutine = StartCoroutine(FloatingEffectCoroutine());
            }
        }

        /// <summary>
        /// 打字机协程
        /// </summary>
        private IEnumerator TypewriterCoroutine()
        {
            isTyping = true;

            TMP_TextInfo textInfo = textComponent.textInfo;
            int characterCount = textInfo.characterCount;
            int frequency = soundFrequency <= 0 ? 1 : soundFrequency;

            for (int i = 0; i < characterCount; i++)
            {
                textComponent.maxVisibleCharacters = i + 1;

                if (typingSound != null && i % frequency == 0)
                {
                    audioSource.PlayOneShot(typingSound, soundVolume);
                }

                float delay = charDelay;
                char currentChar = textInfo.characterInfo[i].character;
                if (IsPunctuation(currentChar))
                {
                    delay += punctuationDelay;
                }

                float elapsed = 0f;
                while (elapsed < delay)
                {
                    elapsed += Time.deltaTime;
                    yield return null;
                }
            }

            isTyping = false;
            typewriterCoroutine = null;
        }

        /// <summary>
        /// 漂浮效果协程 - 对富文本标记的字符应用波浪动画
        /// </summary>
        private IEnumerator FloatingEffectCoroutine()
        {
            while (true)
            {
                TMP_TextInfo textInfo = textComponent.textInfo;
                int maxVisibleCharacters = textComponent.maxVisibleCharacters;

                for (int i = 0; i < floatingCharacterIndices.Count; i++)
                {
                    int charIndex = floatingCharacterIndices[i];
                    if (charIndex >= maxVisibleCharacters)
                        break;

                    TMP_CharacterInfo charInfo = textInfo.characterInfo[charIndex];
                    if (!charInfo.isVisible)
                        continue;

                    int materialIndex = charInfo.materialReferenceIndex;
                    int vertexIndex = charInfo.vertexIndex;

                    Vector3[] srcVertices = cachedMeshInfo[materialIndex].vertices;
                    Vector3[] dstVertices = textInfo.meshInfo[materialIndex].vertices;

                    float offset =
                        Mathf.Sin((Time.time * floatSpeed) + (charIndex * 0.5f)) * floatAmplitude;
                    Vector3 offsetVector = new Vector3(0f, offset, 0f);

                    dstVertices[vertexIndex + 0] = srcVertices[vertexIndex + 0] + offsetVector;
                    dstVertices[vertexIndex + 1] = srcVertices[vertexIndex + 1] + offsetVector;
                    dstVertices[vertexIndex + 2] = srcVertices[vertexIndex + 2] + offsetVector;
                    dstVertices[vertexIndex + 3] = srcVertices[vertexIndex + 3] + offsetVector;
                }

                textComponent.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices);
                yield return null;
            }
        }

        /// <summary>
        /// 判断是否为标点符号
        /// </summary>
        private bool IsPunctuation(char c)
        {
            return c == '。'
                || c == '！'
                || c == '？'
                || c == '，'
                || c == '、'
                || c == '.'
                || c == '!'
                || c == '?'
                || c == ','
                || c == ';'
                || c == ':';
        }

        /// <summary>
        /// 检查是否正在播放动画
        /// </summary>
        public bool IsTyping => isTyping;

        private void OnDisable()
        {
            StopAllEffectCoroutines();
        }
    }
}
