using UnityEngine;
using UnityEditor;

namespace MaskGame.Editor
{
    public class AudioManagerSetup
    {
        [MenuItem("Tools/Setup AudioManager")]
        public static void Setup()
        {
            var audioSystem = GameObject.Find("AudioSystem");
            if (audioSystem == null)
            {
                Debug.LogError("找不到AudioSystem GameObject");
                return;
            }

            var audioManager = audioSystem.GetComponent<MaskGame.Managers.AudioManager>();
            if (audioManager == null)
            {
                Debug.LogError("找不到AudioManager组件");
                return;
            }

            var bgmSourceObj = GameObject.Find("Audio Source BGM");
            if (bgmSourceObj != null)
            {
                var bgmSource = bgmSourceObj.GetComponent<AudioSource>();
                var so = new SerializedObject(audioManager);
                so.FindProperty("bgmSource").objectReferenceValue = bgmSource;
                
                var normalBGM = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/BGM/fight.mp3");
                so.FindProperty("normalBGM").objectReferenceValue = normalBGM;
                
                var bossBGM = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/BGM/boss.mp3");
                so.FindProperty("bossBGM").objectReferenceValue = bossBGM;
                
                var hitSoundsProp = so.FindProperty("hitSounds");
                hitSoundsProp.arraySize = 8;
                for (int i = 0; i < 8; i++)
                {
                    var clip = AssetDatabase.LoadAssetAtPath<AudioClip>($"Assets/Sounds/SE/hit_{i+1}.MP3");
                    hitSoundsProp.GetArrayElementAtIndex(i).objectReferenceValue = clip;
                }
                
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(audioManager);
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(audioSystem.scene);
                
                Debug.Log("AudioManager配置完成！");
            }
        }
    }
}
