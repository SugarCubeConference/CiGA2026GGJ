using System.Collections;
using System.Collections.Generic;
using MaskGame.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StartButton : MonoBehaviour
{
    [SerializeField]
    private string targetSceneName; // 目标场景的名称

    public void SwitchScene()
    {
        if (!string.IsNullOrEmpty(targetSceneName))
        {
            // 使用转场效果
            if (SceneTransition.Instance != null)
            {
                SceneTransition.Instance.LoadSceneWithTransition(targetSceneName);
            }
            else
            {
                // 回退到直接加载
                SceneManager.LoadScene(targetSceneName);
            }
        }
    }
}
