using UnityEngine;
using UnityEngine.UI;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;

namespace MaskGame.Editor
{
    /// <summary>
    /// UI构建器 - 自动创建游戏UI布局
    /// </summary>
    public class UIBuilder : MonoBehaviour
    {
        [MenuItem("Mask Game/Build UI Layout")]
        public static void BuildUI()
        {
            // 查找Canvas
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("场景中没有找到Canvas！");
                return;
            }

            RectTransform canvasRect = canvas.GetComponent<RectTransform>();

            // 清除现有子对象
            foreach (Transform child in canvas.transform)
            {
                DestroyImmediate(child.gameObject);
            }

            // === 1. 创建顶部HUD ===
            GameObject topHUD = CreatePanel("TopHUD", canvasRect, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), 
                new Vector2(0, -30), new Vector2(1000, 60));
            SetPanelColor(topHUD, new Color(0.1f, 0.1f, 0.1f, 0.8f));

            // Day文本
            CreateText("DayText", topHUD.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(-450, 0), new Vector2(150, 50), "Day 1", 24);

            // Progress Bar
            GameObject progressBarBg = CreatePanel("ProgressBar", topHUD.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-150, 0), new Vector2(400, 30));
            SetPanelColor(progressBarBg, new Color(0.2f, 0.2f, 0.2f));
            
            Slider progressSlider = progressBarBg.AddComponent<Slider>();
            GameObject progressFill = CreatePanel("Fill", progressBarBg.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                Vector2.zero, new Vector2(400, 30));
            SetPanelColor(progressFill, Color.green);
            progressSlider.fillRect = progressFill.GetComponent<RectTransform>();
            progressSlider.transition = Selectable.Transition.None;

            // Battery文本
            CreateText("BatteryText", topHUD.transform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(450, 0), new Vector2(150, 50), "❤❤❤", 24);

            // === 2. 创建对话区域 ===
            GameObject dialogueArea = CreatePanel("DialogueArea", canvasRect, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(350, 0), new Vector2(650, 700));
            SetPanelColor(dialogueArea, new Color(0.15f, 0.15f, 0.15f, 0.9f));

            // 好友分组标签
            CreateText("FriendGroupText", dialogueArea.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0, -30), new Vector2(600, 40), "Friend Group", 18, Color.red);

            // 对话文本框
            GameObject dialogueBox = CreatePanel("DialogueBox", dialogueArea.transform, new Vector2(0.5f, 0.6f), new Vector2(0.5f, 0.6f),
                new Vector2(0, 0), new Vector2(600, 400));
            SetPanelColor(dialogueBox, new Color(0.05f, 0.05f, 0.05f, 0.5f));
            
            CreateTextTMP("DialogueText", dialogueBox.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0, 0), new Vector2(560, 360), "Text with hints to the Mask", 32);

            // 倒计时条
            GameObject timeBarBg = CreatePanel("TimeBar", dialogueArea.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0, 30), new Vector2(600, 20));
            SetPanelColor(timeBarBg, new Color(0.2f, 0.2f, 0.2f));
            
            Slider timeSlider = timeBarBg.AddComponent<Slider>();
            GameObject timeFill = CreatePanel("Fill", timeBarBg.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                Vector2.zero, new Vector2(600, 20));
            SetPanelColor(timeFill, Color.white);
            timeSlider.fillRect = timeFill.GetComponent<RectTransform>();
            timeSlider.transition = Selectable.Transition.None;
            timeSlider.value = 1f;

            // === 3. 创建面具选择面板 ===
            GameObject maskPanel = CreatePanel("MaskPanel", canvasRect, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(-150, 0), new Vector2(250, 700));
            SetPanelColor(maskPanel, new Color(0.15f, 0.15f, 0.15f, 0.9f));

            // 创建4个面具按钮
            string[] maskNames = { "Mask ①", "Mask ②", "Mask ③", "Mask ④" };
            for (int i = 0; i < 4; i++)
            {
                float yPos = 250 - i * 150;
                GameObject maskBtn = CreateButton($"MaskButton{i + 1}", maskPanel.transform, 
                    new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(0, yPos), new Vector2(200, 120), maskNames[i]);
                
                // 设置按钮颜色为橙色
                var btnImage = maskBtn.GetComponent<Image>();
                if (btnImage != null)
                {
                    btnImage.color = new Color(1f, 0.6f, 0f);
                }
            }

            // === 4. 创建游戏结束面板 ===
            GameObject gameOverPanel = CreatePanel("GameOverPanel", canvasRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(600, 400));
            SetPanelColor(gameOverPanel, new Color(0f, 0f, 0f, 0.95f));
            gameOverPanel.SetActive(false);

            CreateTextTMP("GameOverText", gameOverPanel.transform, new Vector2(0.5f, 0.6f), new Vector2(0.5f, 0.6f),
                new Vector2(0, 0), new Vector2(500, 200), "GAME OVER\n社死！", 48);

            CreateButton("RestartButton", gameOverPanel.transform, new Vector2(0.5f, 0.3f), new Vector2(0.5f, 0.3f),
                new Vector2(0, 0), new Vector2(200, 60), "重新开始");

            // === 5. 添加UIManager组件 ===
            GameObject uiManagerObj = new GameObject("UIManager");
            uiManagerObj.transform.SetParent(canvas.transform);
            var uiManager = uiManagerObj.AddComponent<MaskGame.UI.UIManager>();

            Debug.Log("UI布局创建完成！请手动连接UIManager的引用。");

            // 保存场景
            EditorUtility.SetDirty(canvas.gameObject);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
        }

        private static GameObject CreatePanel(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 anchoredPos, Vector2 sizeDelta)
        {
            GameObject panel = new GameObject(name);
            panel.transform.SetParent(parent);
            RectTransform rect = panel.AddComponent<RectTransform>();
            panel.AddComponent<CanvasRenderer>();
            panel.AddComponent<Image>();

            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = sizeDelta;
            rect.localScale = Vector3.one;

            return panel;
        }

        private static void SetPanelColor(GameObject panel, Color color)
        {
            var image = panel.GetComponent<Image>();
            if (image != null)
            {
                image.color = color;
            }
        }

        private static GameObject CreateText(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 anchoredPos, Vector2 sizeDelta, string text, int fontSize, Color? color = null)
        {
            GameObject textObj = new GameObject(name);
            textObj.transform.SetParent(parent);
            RectTransform rect = textObj.AddComponent<RectTransform>();
            textObj.AddComponent<CanvasRenderer>();
            Text textComp = textObj.AddComponent<Text>();

            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = sizeDelta;
            rect.localScale = Vector3.one;

            textComp.text = text;
            textComp.fontSize = fontSize;
            textComp.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            textComp.alignment = TextAnchor.MiddleCenter;
            textComp.color = color ?? Color.white;

            return textObj;
        }

        private static GameObject CreateTextTMP(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 anchoredPos, Vector2 sizeDelta, string text, int fontSize, Color? color = null)
        {
            GameObject textObj = new GameObject(name);
            textObj.transform.SetParent(parent);
            RectTransform rect = textObj.AddComponent<RectTransform>();
            TextMeshProUGUI textComp = textObj.AddComponent<TextMeshProUGUI>();

            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = sizeDelta;
            rect.localScale = Vector3.one;

            textComp.text = text;
            textComp.fontSize = fontSize;
            textComp.alignment = TextAlignmentOptions.Center;
            textComp.color = color ?? Color.white;

            return textObj;
        }

        private static GameObject CreateButton(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 anchoredPos, Vector2 sizeDelta, string buttonText)
        {
            GameObject btnObj = new GameObject(name);
            btnObj.transform.SetParent(parent);
            RectTransform rect = btnObj.AddComponent<RectTransform>();
            btnObj.AddComponent<CanvasRenderer>();
            Image image = btnObj.AddComponent<Image>();
            Button button = btnObj.AddComponent<Button>();

            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = sizeDelta;
            rect.localScale = Vector3.one;

            image.color = new Color(0.8f, 0.8f, 0.8f);

            // 添加文本
            CreateTextTMP("Text", btnObj.transform, Vector2.zero, Vector2.one,
                Vector2.zero, Vector2.zero, buttonText, 24);

            return btnObj;
        }
    }
}
#endif