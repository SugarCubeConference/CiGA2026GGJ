using UnityEngine;
using UnityEngine.UI;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;

namespace MaskGame.Editor
{
    /// <summary>
    /// UI连接器 - 自动连接UIManager的引用
    /// </summary>
    public class UIConnector : MonoBehaviour
    {
        [MenuItem("Mask Game/Connect UI References")]
        public static void ConnectReferences()
        {
            // 查找UIManager
            var uiManager = FindObjectOfType<MaskGame.UI.UIManager>();
            if (uiManager == null)
            {
                Debug.LogError("场景中没有找到UIManager！");
                return;
            }

            // 使用SerializedObject来设置私有字段
            SerializedObject so = new SerializedObject(uiManager);

            // 查找并连接UI元素
            // 顶部HUD
            ConnectComponent(so, "dayText", "DayText", typeof(TextMeshProUGUI));
            ConnectComponent(so, "progressBar", "ProgressBar", typeof(Slider));
            ConnectComponent(so, "batteryText", "BatteryText", typeof(TextMeshProUGUI));

            // 对话区域
            ConnectComponent(so, "dialogueText", "DialogueText", typeof(TextMeshProUGUI));
            ConnectComponent(so, "friendGroupText", "FriendGroupText", typeof(TextMeshProUGUI));
            ConnectGameObject(so, "dialoguePanel", "DialogueArea");
            
            // 倒计时
            ConnectComponent(so, "timeBar", "TimeBar", typeof(Slider));
            
            // TimeBar的Fill Image
            GameObject timeBarObj = GameObject.Find("TimeBar");
            if (timeBarObj != null)
            {
                Transform fillTransform = timeBarObj.transform.Find("Fill");
                if (fillTransform != null)
                {
                    Image fillImage = fillTransform.GetComponent<Image>();
                    if (fillImage != null)
                    {
                        so.FindProperty("timeBarFill").objectReferenceValue = fillImage;
                    }
                }
            }

            // 面具按钮
            Button[] maskButtons = new Button[4];
            TextMeshProUGUI[] maskTexts = new TextMeshProUGUI[4];
            
            for (int i = 0; i < 4; i++)
            {
                GameObject btnObj = GameObject.Find($"MaskButton{i + 1}");
                if (btnObj != null)
                {
                    maskButtons[i] = btnObj.GetComponent<Button>();
                    Transform textTransform = btnObj.transform.Find("Text");
                    if (textTransform != null)
                    {
                        maskTexts[i] = textTransform.GetComponent<TextMeshProUGUI>();
                    }
                }
            }

            var maskButtonsProp = so.FindProperty("maskButtons");
            maskButtonsProp.arraySize = 4;
            for (int i = 0; i < 4; i++)
            {
                maskButtonsProp.GetArrayElementAtIndex(i).objectReferenceValue = maskButtons[i];
            }

            var maskTextsProp = so.FindProperty("maskButtonTexts");
            maskTextsProp.arraySize = 4;
            for (int i = 0; i < 4; i++)
            {
                maskTextsProp.GetArrayElementAtIndex(i).objectReferenceValue = maskTexts[i];
            }

            // 游戏结束面板（如果存在）
            ConnectGameObject(so, "gameOverPanel", "GameOverPanel");
            ConnectComponent(so, "gameOverText", "GameOverText", typeof(TextMeshProUGUI));
            ConnectComponent(so, "restartButton", "RestartButton", typeof(Button));

            // 应用修改
            so.ApplyModifiedProperties();
            
            EditorUtility.SetDirty(uiManager);
            Debug.Log("UIManager引用连接完成！");
        }

        private static void ConnectComponent(SerializedObject so, string propertyName, string gameObjectName, System.Type componentType)
        {
            GameObject go = GameObject.Find(gameObjectName);
            if (go != null)
            {
                Component component = go.GetComponent(componentType);
                if (component != null)
                {
                    so.FindProperty(propertyName).objectReferenceValue = component;
                    Debug.Log($"✓ 已连接 {propertyName} -> {gameObjectName}");
                }
                else
                {
                    Debug.LogWarning($"✗ {gameObjectName} 上没有找到 {componentType.Name} 组件");
                }
            }
            else
            {
                Debug.LogWarning($"✗ 未找到GameObject: {gameObjectName}");
            }
        }

        private static void ConnectGameObject(SerializedObject so, string propertyName, string gameObjectName)
        {
            GameObject go = GameObject.Find(gameObjectName);
            if (go != null)
            {
                so.FindProperty(propertyName).objectReferenceValue = go;
                Debug.Log($"✓ 已连接 {propertyName} -> {gameObjectName}");
            }
            else
            {
                Debug.LogWarning($"✗ 未找到GameObject: {gameObjectName}");
            }
        }
    }
}
#endif