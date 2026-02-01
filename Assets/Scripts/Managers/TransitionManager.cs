using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class MosaicTransitionManager : MonoBehaviour
{
    [Header("UI 设置")]
    public RawImage transitionImage;      // 全屏RawImage
    public Material mosaicMaterial;       // 你的马赛克Material（需实例化）
    
    [Header("参数")]
    public float transitionDuration = 1.0f;
    public float pauseDuration = 0.3f;    // 完全模糊时的停顿时间
    public AnimationCurve easeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    private RenderTexture rt;
    private bool isTransitioning = false;
    
    public static MosaicTransitionManager Instance { get; private set; }
    
    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        SetupRenderTexture();
    }
    
    void SetupRenderTexture()
    {
        // 创建与屏幕匹配的RenderTexture
        rt = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.Default);
        mosaicMaterial = new Material(mosaicMaterial); // 实例化避免修改原材质
    }
    
    /// <summary>
    /// 调用此方法开始转场，传入目标场景名称
    /// </summary>
    public void TransitionTo(string sceneName)
    {
        if (isTransitioning) return;
        StartCoroutine(TransitionCoroutine(sceneName));
    }
    
    IEnumerator TransitionCoroutine(string targetScene)
    {
        isTransitioning = true;
        
        // ===== 阶段1：捕获当前场景画面 =====
        yield return new WaitForEndOfFrame();
        
        // 捕获当前屏幕到RenderTexture
        ScreenCapture.CaptureScreenshotIntoRenderTexture(rt);
        transitionImage.texture = rt;
        transitionImage.material = mosaicMaterial;
        transitionImage.enabled = true;
        mosaicMaterial.SetFloat("_Alpha", 1);
        
        // ===== 阶段2：旧场景马赛克化（100 -> 32）=====
        float timer = 0;
        while (timer < transitionDuration)
        {
            timer += Time.unscaledDeltaTime; // 使用unscaled避免Time.timeScale影响
            float t = timer / transitionDuration;
            float value = Mathf.Lerp(100f, 32f, easeCurve.Evaluate(t));
            mosaicMaterial.SetFloat("_PixelSize", value);
            yield return null;
        }
        mosaicMaterial.SetFloat("_PixelSize", 32f);
        
        // ===== 阶段3：异步加载新场景（在最模糊时切换）=====
        yield return new WaitForSecondsRealtime(pauseDuration);
        
        AsyncOperation loadOp = SceneManager.LoadSceneAsync(targetScene);
        loadOp.allowSceneActivation = false; // 不让场景立即显示
        
        while (loadOp.progress < 0.9f) yield return null; // 等待加载完成
        
        loadOp.allowSceneActivation = true; // 激活新场景
        yield return null; // 等待一帧让新场景渲染
        
        // 捕获新场景画面（此时还没变清晰）
        ScreenCapture.CaptureScreenshotIntoRenderTexture(rt);
        
        // ===== 阶段4：新场景马赛克化入场（32 -> 100）=====
        timer = 0;
        while (timer < transitionDuration)
        {
            timer += Time.unscaledDeltaTime;
            float t = timer / transitionDuration;
            float value = Mathf.Lerp(32f, 100f, easeCurve.Evaluate(t));
            mosaicMaterial.SetFloat("_PixelSize", value);
            yield return null;
        }
        mosaicMaterial.SetFloat("_PixelSize", 100f);
        
        // 清理
        transitionImage.enabled = false;
        transitionImage.texture = null;
        isTransitioning = false;
    }
}