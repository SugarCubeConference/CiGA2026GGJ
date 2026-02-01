using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace MaskGame.Editor
{
    /// <summary>
    /// 修复遭遇文件中的富文本格式
    /// 将HTML style标签转换为TextMeshPro支持的mark标签
    /// </summary>
    public class FixEncounterRichText : EditorWindow
    {
        [MenuItem("Tools/Mask Game/修复遭遇文件富文本格式")]
        public static void FixAllEncounters()
        {
            string encounterPath = "Assets/Resources/Encounters";
            string[] assetFiles = Directory.GetFiles(
                encounterPath,
                "*.asset",
                SearchOption.AllDirectories
            );

            int fixedCount = 0;

            foreach (string assetFile in assetFiles)
            {
                bool modified = false;
                string content = File.ReadAllText(assetFile);
                string originalContent = content;

                // 替换所有的 <b style=\"background-color: #XXXXXX;\">text</b>
                // 为 <b><color=#XXXXXX>text</color></b>
                // 注意：YAML文件中引号是转义的 \"
                content = Regex.Replace(
                    content,
                    @"<b\s+style=\\""background-color:\s*#?([0-9A-Fa-f]{6});?\\"">(.*?)</b>",
                    match =>
                    {
                        string color = match.Groups[1].Value.ToUpper();
                        string text = match.Groups[2].Value;
                        // 移除文本中的换行符和缩进空格
                        text = Regex.Replace(text, @"\r?\n\s*", "");
                        return $"<b><color=#{color}>{text}</color></b>";
                    },
                    RegexOptions.Singleline
                );

                if (content != originalContent)
                {
                    File.WriteAllText(assetFile, content);
                    modified = true;
                    fixedCount++;
                }

                if (modified)
                {
                    Debug.Log($"已修复: {assetFile}");
                }
            }

            AssetDatabase.Refresh();
            Debug.Log($"<color=green>修复完成！共处理 {fixedCount} 个文件。</color>");
        }
    }
}
