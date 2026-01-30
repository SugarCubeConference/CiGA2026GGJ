using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;

namespace MaskGame.Editor
{
    /// <summary>
    /// GameManager配置器 - 自动加载对话数据
    /// </summary>
    public class GameManagerConfigurator : MonoBehaviour
    {
        [MenuItem("Mask Game/Configure GameManager")]
        public static void ConfigureGameManager()
        {
            // 查找GameManager
            var gameManager = FindObjectOfType<MaskGame.Managers.GameManager>();
            if (gameManager == null)
            {
                Debug.LogError("场景中没有找到GameManager！");
                return;
            }

            // 加载所有对话数据
            string[] guids = AssetDatabase.FindAssets("t:EncounterData", new[] { "Assets/Data/Encounters" });
            
            if (guids.Length == 0)
            {
                Debug.LogWarning("未找到任何对话数据！");
                return;
            }

            List<MaskGame.Data.EncounterData> encounters = new List<MaskGame.Data.EncounterData>();
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var encounter = AssetDatabase.LoadAssetAtPath<MaskGame.Data.EncounterData>(path);
                if (encounter != null)
                {
                    encounters.Add(encounter);
                    Debug.Log($"✓ 加载对话: {encounter.name}");
                }
            }

            // 使用SerializedObject设置对话池
            SerializedObject so = new SerializedObject(gameManager);
            var encounterPoolProp = so.FindProperty("encounterPool");
            
            encounterPoolProp.arraySize = encounters.Count;
            for (int i = 0; i < encounters.Count; i++)
            {
                encounterPoolProp.GetArrayElementAtIndex(i).objectReferenceValue = encounters[i];
            }

            so.ApplyModifiedProperties();
            
            EditorUtility.SetDirty(gameManager);
            Debug.Log($"GameManager配置完成！已加载 {encounters.Count} 个对话数据。");
        }
    }
}
#endif