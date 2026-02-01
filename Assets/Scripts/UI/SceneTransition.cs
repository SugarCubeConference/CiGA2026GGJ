using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MaskGame.UI
{
    /// <summary>
    /// 场景转场效果管理器 - 马赛克像素化转场
    /// </summary>
    public class SceneTransition : MonoBehaviour
    {
        public static SceneTransition Instance { get; private set; }

        [Header("转场设置")]
        [SerializeField]
        private float transitionDuration = 1f; // 转场持续时间

        [SerializeField]
        private int maxPixelSize = 50; // 最大像素块大小

        [SerializeField]
        private Color transitionColor = Color.black; // 转场颜色

        private GameObject transitionCanvas;
        private RawImage transitionImage;
        private Material pixelateMaterial;
        private bool isTransitioning = false;

        // 简单的像素化Shader代码
        private const string PixelateShader =
            @"
Shader ""Hidden/Pixelate""
{
    Properties
    {
        _MainTex (""Texture"", 2D) = ""white"" {}
        _PixelSize (""Pixel Size"", Float) = 1
        _Color (""Color"", Color) = (0,0,0,1)
    }
    SubShader
    {
        Tags { ""Queue""=""Overlay"" ""RenderType""=""Transparent"" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include ""UnityCG.cginc""

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float _PixelSize;
            float4 _Color;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // 马赛克像素化效果
                float2 pixelUV = floor(i.uv * _ScreenParams.xy / _PixelSize) * _PixelSize / _ScreenParams.xy;
                
                // 根据像素大小混合颜色和纹理
                float alpha = _PixelSize / 50.0;
                return lerp(tex2D(_MainTex, pixelUV), _Color, alpha);
            }
            ENDCG
        }
    }
}";

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeTransition();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// 初始化转场系统
        /// </summary>
        private void InitializeTransition()
        {
            // 创建转场Canvas
            transitionCanvas = new GameObject("TransitionCanvas");
            transitionCanvas.transform.SetParent(transform);
            DontDestroyOnLoad(transitionCanvas);

            Canvas canvas = transitionCanvas.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 9999; // 确保在最前面

            CanvasScaler scaler = transitionCanvas.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            // 创建Image用于转场效果
            GameObject imageObj = new GameObject("TransitionImage");
            imageObj.transform.SetParent(transitionCanvas.transform, false);

            RectTransform rect = imageObj.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;

            transitionImage = imageObj.AddComponent<RawImage>();
            transitionImage.color = transitionColor;

            // 创建像素化纹理
            CreatePixelTexture();

            transitionCanvas.SetActive(false);
        }

        /// <summary>
        /// 创建像素化纹理
        /// </summary>
        private void CreatePixelTexture()
        {
            // 创建一个简单的纹理用于马赛克效果
            Texture2D texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            transitionImage.texture = texture;
        }

        /// <summary>
        /// 加载场景并播放转场动画
        /// </summary>
        public void LoadSceneWithTransition(string sceneName)
        {
            if (!isTransitioning)
            {
                StartCoroutine(TransitionCoroutine(sceneName));
            }
        }

        /// <summary>
        /// 转场协程
        /// </summary>
        private IEnumerator TransitionCoroutine(string sceneName)
        {
            isTransitioning = true;
            transitionCanvas.SetActive(true);

            // 淡入阶段 - 马赛克从小到大
            yield return StartCoroutine(PixelateIn());

            // 加载场景
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
            while (!asyncLoad.isDone)
            {
                yield return null;
            }

            // 短暂停留
            yield return new WaitForSeconds(0.2f);

            // 淡出阶段 - 马赛克从大到小
            yield return StartCoroutine(PixelateOut());

            transitionCanvas.SetActive(false);
            isTransitioning = false;
        }

        /// <summary>
        /// 马赛克淡入效果
        /// </summary>
        private IEnumerator PixelateIn()
        {
            float elapsed = 0f;
            float halfDuration = transitionDuration / 2f;

            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / halfDuration;

                // 从透明到不透明，同时模拟像素化（通过缩放实现）
                Color color = transitionColor;
                color.a = progress;
                transitionImage.color = color;

                // 可以在这里调整RectTransform的scale来模拟像素化
                float pixelScale = Mathf.Lerp(1f, 0.95f, progress);
                transitionImage.transform.localScale = Vector3.one * pixelScale;

                yield return null;
            }

            transitionImage.color = transitionColor;
            transitionImage.transform.localScale = Vector3.one * 0.95f;
        }

        /// <summary>
        /// 马赛克淡出效果
        /// </summary>
        private IEnumerator PixelateOut()
        {
            float elapsed = 0f;
            float halfDuration = transitionDuration / 2f;

            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / halfDuration;

                // 从不透明到透明
                Color color = transitionColor;
                color.a = 1f - progress;
                transitionImage.color = color;

                // 恢复缩放
                float pixelScale = Mathf.Lerp(0.95f, 1f, progress);
                transitionImage.transform.localScale = Vector3.one * pixelScale;

                yield return null;
            }

            transitionImage.color = new Color(
                transitionColor.r,
                transitionColor.g,
                transitionColor.b,
                0f
            );
            transitionImage.transform.localScale = Vector3.one;
        }
    }
}
