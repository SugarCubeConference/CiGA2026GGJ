using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace MaskGame.UI
{
    /// <summary>
    /// 程序化生成红色涂抹线条效果
    /// </summary>
    public class ScribbleLineGenerator : MonoBehaviour
    {
        [Header("线条设置")]
        [SerializeField]
        private int lineCount = 15; // 线条数量
        [SerializeField]
        private float lineWidth = 3f; // 线条宽度
        [SerializeField]
        private Color lineColor = new Color(0.8f, 0.1f, 0.1f, 0.8f); // 红色

        [Header("生成范围")]
        [SerializeField]
        private float areaWidth = 300f;
        [SerializeField]
        private float areaHeight = 100f;

        [Header("线条复杂度")]
        [SerializeField]
        private int pointsPerLine = 8; // 每条线的点数
        [SerializeField]
        private float randomness = 30f; // 随机偏移程度

        [Header("自动生成")]
        [SerializeField]
        private int generationsPerSecond = 5; // 每秒生成次数

        private Texture2D[] lineTextures;
        private RectTransform[] lineRects;
        private Color32[] pixelBuffer;
        private Vector2[] pointBuffer;
        private Coroutine generationCoroutine;
        private int textureWidth;
        private int textureHeight;

        private void Start()
        {
            EnsureInitialized();
            GenerateScribbleLines();

            if (generationsPerSecond > 0)
            {
                generationCoroutine = StartCoroutine(AutoGenerateLines());
            }
        }

        private IEnumerator AutoGenerateLines()
        {
            float interval = 1f / generationsPerSecond;
            WaitForSeconds wait = new WaitForSeconds(interval);

            while (true)
            {
                GenerateScribbleLines();
                yield return wait;
            }
        }

        private void GenerateScribbleLines()
        {
            if (lineTextures == null)
                return;

            for (int i = 0; i < lineTextures.Length; i++)
            {
                GenerateLineTexture(lineTextures[i]);
            }
        }

        private void EnsureInitialized()
        {
            int desiredLineCount = Mathf.Max(0, lineCount);
            if (desiredLineCount == 0)
            {
                ReleaseGeneratedResources();
                return;
            }

            int desiredWidth = Mathf.Max(1, Mathf.RoundToInt(areaWidth));
            int desiredHeight = Mathf.Max(1, Mathf.RoundToInt(areaHeight));
            int desiredPoints = Mathf.Max(2, pointsPerLine);

            bool needsRebuild =
                lineTextures == null
                || lineTextures.Length != desiredLineCount
                || desiredWidth != textureWidth
                || desiredHeight != textureHeight;

            if (needsRebuild)
            {
                ReleaseGeneratedResources();

                textureWidth = desiredWidth;
                textureHeight = desiredHeight;
                pixelBuffer = new Color32[textureWidth * textureHeight];
                pointBuffer = new Vector2[desiredPoints];
                lineTextures = new Texture2D[desiredLineCount];
                lineRects = new RectTransform[desiredLineCount];

                for (int i = 0; i < desiredLineCount; i++)
                {
                    CreateLineObject(i);
                }
            }
            else
            {
                if (pointBuffer == null || pointBuffer.Length != desiredPoints)
                {
                    pointBuffer = new Vector2[desiredPoints];
                }

                Vector2 size = new Vector2(areaWidth, areaHeight);
                for (int i = 0; i < lineRects.Length; i++)
                {
                    if (lineRects[i] != null)
                    {
                        lineRects[i].sizeDelta = size;
                    }
                }
            }
        }

        private void CreateLineObject(int index)
        {
            GameObject lineObj = new GameObject($"ScribbleLine_{index}");
            lineObj.transform.SetParent(transform, false);

            RectTransform rectTransform = lineObj.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.sizeDelta = new Vector2(areaWidth, areaHeight);
            rectTransform.anchoredPosition = Vector2.zero;

            RawImage rawImage = lineObj.AddComponent<RawImage>();
            rawImage.color = Color.white;

            Texture2D texture = new Texture2D(
                textureWidth,
                textureHeight,
                TextureFormat.RGBA32,
                false
            );
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            rawImage.texture = texture;

            lineRects[index] = rectTransform;
            lineTextures[index] = texture;
        }

        private void GenerateLineTexture(Texture2D texture)
        {
            if (texture == null)
                return;

            System.Array.Clear(pixelBuffer, 0, pixelBuffer.Length);

            Vector2 startPos = new Vector2(
                Random.Range(0f, textureWidth * 0.3f),
                Random.Range(textureHeight * 0.3f, textureHeight * 0.7f)
            );

            Vector2 endPos = new Vector2(
                Random.Range(textureWidth * 0.7f, textureWidth),
                Random.Range(textureHeight * 0.3f, textureHeight * 0.7f)
            );

            pointBuffer[0] = startPos;
            pointBuffer[pointBuffer.Length - 1] = endPos;

            for (int i = 1; i < pointBuffer.Length - 1; i++)
            {
                float t = (float)i / (pointBuffer.Length - 1);
                Vector2 linearPos = Vector2.Lerp(startPos, endPos, t);
                linearPos += new Vector2(
                    Random.Range(-randomness, randomness),
                    Random.Range(-randomness, randomness)
                );
                pointBuffer[i] = linearPos;
            }

            DrawThickLine(pointBuffer, lineWidth, lineColor);

            texture.SetPixels32(pixelBuffer);
            texture.Apply(false);
        }

        private void DrawThickLine(Vector2[] points, float width, Color color)
        {
            for (int i = 0; i < points.Length - 1; i++)
            {
                DrawLineSegment(points[i], points[i + 1], width, color);
            }
        }

        private void DrawLineSegment(Vector2 start, Vector2 end, float width, Color color)
        {
            int steps = Mathf.CeilToInt(Vector2.Distance(start, end));
            if (steps <= 0)
            {
                DrawCircle((int)start.x, (int)start.y, width / 2f, color);
                return;
            }

            for (int i = 0; i <= steps; i++)
            {
                float t = i / (float)steps;
                Vector2 point = Vector2.Lerp(start, end, t);
                DrawCircle((int)point.x, (int)point.y, width / 2f, color);
            }
        }

        private void DrawCircle(int centerX, int centerY, float radius, Color color)
        {
            int radiusInt = Mathf.CeilToInt(radius);
            float radiusSqr = radius * radius;
            Color32 baseColor = color;
            byte alpha = (byte)(baseColor.a * Random.Range(0.7f, 1f));
            Color32 pixelColor = new Color32(baseColor.r, baseColor.g, baseColor.b, alpha);

            for (int y = -radiusInt; y <= radiusInt; y++)
            {
                for (int x = -radiusInt; x <= radiusInt; x++)
                {
                    if ((x * x) + (y * y) > radiusSqr)
                        continue;

                    int pixelX = centerX + x;
                    int pixelY = centerY + y;
                    if (
                        pixelX < 0
                        || pixelX >= textureWidth
                        || pixelY < 0
                        || pixelY >= textureHeight
                    )
                        continue;

                    int index = (pixelY * textureWidth) + pixelX;
                    pixelBuffer[index] = pixelColor;
                }
            }
        }

        private void ReleaseGeneratedResources()
        {
            if (generationCoroutine != null)
            {
                StopCoroutine(generationCoroutine);
                generationCoroutine = null;
            }

            if (lineTextures != null)
            {
                for (int i = 0; i < lineTextures.Length; i++)
                {
                    DestroyUnityObject(lineTextures[i]);
                }
            }

            if (lineRects != null)
            {
                for (int i = 0; i < lineRects.Length; i++)
                {
                    if (lineRects[i] != null)
                    {
                        DestroyUnityObject(lineRects[i].gameObject);
                    }
                }
            }

            lineTextures = null;
            lineRects = null;
            pixelBuffer = null;
            pointBuffer = null;
            textureWidth = 0;
            textureHeight = 0;
        }

        private void DestroyUnityObject(Object obj)
        {
            if (obj == null)
                return;

            if (Application.isPlaying)
            {
                Destroy(obj);
            }
            else
            {
                DestroyImmediate(obj);
            }
        }

        private void OnDestroy()
        {
            ReleaseGeneratedResources();
        }

        // 在编辑器中可以重新生成
        [ContextMenu("重新生成线条")]
        public void RegenerateLines()
        {
            EnsureInitialized();
            GenerateScribbleLines();
        }
    }
}
