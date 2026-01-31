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

        private void Start()
        {
            StartCoroutine(AutoGenerateLines());
        }

        private IEnumerator AutoGenerateLines()
        {
            float interval = 1f / generationsPerSecond;
            
            while (true)
            {
                ClearAllLines();
                GenerateScribbleLines();
                yield return new WaitForSeconds(interval);
            }
        }

        private void ClearAllLines()
        {
            foreach (Transform child in transform)
            {
                Destroy(child.gameObject);
            }
        }

        private void GenerateScribbleLines()
        {
            for (int i = 0; i < lineCount; i++)
            {
                CreateSingleLine();
            }
        }

        private void CreateSingleLine()
        {
            // 创建线条GameObject
            GameObject lineObj = new GameObject("ScribbleLine");
            lineObj.transform.SetParent(transform, false);

            // 添加RectTransform
            RectTransform rectTransform = lineObj.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.sizeDelta = new Vector2(areaWidth, areaHeight);
            rectTransform.anchoredPosition = Vector2.zero;

            // 添加CanvasRenderer和RawImage用于显示
            CanvasRenderer canvasRenderer = lineObj.AddComponent<CanvasRenderer>();
            RawImage rawImage = lineObj.AddComponent<RawImage>();

            // 创建纹理
            Texture2D texture = CreateLineTexture();
            rawImage.texture = texture;
            rawImage.color = Color.white;
        }

        private Texture2D CreateLineTexture()
        {
            int texWidth = (int)areaWidth;
            int texHeight = (int)areaHeight;
            Texture2D texture = new Texture2D(texWidth, texHeight, TextureFormat.RGBA32, false);
            
            // 清空纹理（透明）
            Color[] clearColors = new Color[texWidth * texHeight];
            for (int i = 0; i < clearColors.Length; i++)
            {
                clearColors[i] = Color.clear;
            }
            texture.SetPixels(clearColors);

            // 生成随机线条路径
            Vector2 startPos = new Vector2(
                Random.Range(0f, texWidth * 0.3f),
                Random.Range(texHeight * 0.3f, texHeight * 0.7f)
            );

            Vector2 endPos = new Vector2(
                Random.Range(texWidth * 0.7f, texWidth),
                Random.Range(texHeight * 0.3f, texHeight * 0.7f)
            );

            // 生成中间点
            Vector2[] points = new Vector2[pointsPerLine];
            points[0] = startPos;
            points[pointsPerLine - 1] = endPos;

            for (int i = 1; i < pointsPerLine - 1; i++)
            {
                float t = (float)i / (pointsPerLine - 1);
                Vector2 linearPos = Vector2.Lerp(startPos, endPos, t);
                
                // 添加随机偏移
                linearPos += new Vector2(
                    Random.Range(-randomness, randomness),
                    Random.Range(-randomness, randomness)
                );
                
                points[i] = linearPos;
            }

            // 绘制线条
            DrawThickLine(texture, points, lineWidth, lineColor);

            texture.Apply();
            texture.filterMode = FilterMode.Bilinear;
            return texture;
        }

        private void DrawThickLine(Texture2D texture, Vector2[] points, float width, Color color)
        {
            for (int i = 0; i < points.Length - 1; i++)
            {
                DrawLineSegment(texture, points[i], points[i + 1], width, color);
            }
        }

        private void DrawLineSegment(Texture2D texture, Vector2 start, Vector2 end, float width, Color color)
        {
            int steps = Mathf.CeilToInt(Vector2.Distance(start, end));
            
            for (int i = 0; i <= steps; i++)
            {
                float t = i / (float)steps;
                Vector2 point = Vector2.Lerp(start, end, t);
                
                // 绘制圆形点来形成粗线
                DrawCircle(texture, (int)point.x, (int)point.y, width / 2f, color);
            }
        }

        private void DrawCircle(Texture2D texture, int centerX, int centerY, float radius, Color color)
        {
            int radiusInt = Mathf.CeilToInt(radius);
            
            for (int y = -radiusInt; y <= radiusInt; y++)
            {
                for (int x = -radiusInt; x <= radiusInt; x++)
                {
                    if (x * x + y * y <= radius * radius)
                    {
                        int pixelX = centerX + x;
                        int pixelY = centerY + y;
                        
                        if (pixelX >= 0 && pixelX < texture.width && 
                            pixelY >= 0 && pixelY < texture.height)
                        {
                            // 添加一些透明度变化
                            float alpha = color.a * Random.Range(0.7f, 1f);
                            Color pixelColor = new Color(color.r, color.g, color.b, alpha);
                            texture.SetPixel(pixelX, pixelY, pixelColor);
                        }
                    }
                }
            }
        }

        // 在编辑器中可以重新生成
        [ContextMenu("重新生成线条")]
        public void RegenerateLines()
        {
            ClearAllLines();
            GenerateScribbleLines();
        }
    }
}
