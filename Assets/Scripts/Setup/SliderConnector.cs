using UnityEngine;
using UnityEngine.UI;

namespace MaskGame.Setup
{
    /// <summary>
    /// 滑块连接器 - 自动连接Slider的Fill和Handle引用
    /// </summary>
    [RequireComponent(typeof(Slider))]
    public class SliderConnector : MonoBehaviour
    {
        private void Awake()
        {
            ConnectSlider();
        }

        private void ConnectSlider()
        {
            var slider = GetComponent<Slider>();
            if (slider == null)
                return;

            // 查找Fill和Handle
            var fillObj = transform.Find("Fill Area/Fill");
            var handleObj = transform.Find("Handle Slide Area/Handle");

            // 设置fillRect
            if (fillObj != null)
            {
                var fillRect = fillObj.GetComponent<RectTransform>();
                if (fillRect != null)
                {
                    var fillField = typeof(Slider).GetField(
                        "m_FillRect",
                        System.Reflection.BindingFlags.NonPublic
                            | System.Reflection.BindingFlags.Instance
                    );
                    fillField?.SetValue(slider, fillRect);

                    // 配置Fill Image为Filled类型
                    var fillImage = fillObj.GetComponent<Image>();
                    if (fillImage != null)
                    {
                        fillImage.type = Image.Type.Filled;
                        fillImage.fillMethod = Image.FillMethod.Horizontal;
                        fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
                        fillImage.raycastTarget = false;
                    }
                }
            }

            // 设置handleRect
            if (handleObj != null)
            {
                var handleRect = handleObj.GetComponent<RectTransform>();
                if (handleRect != null)
                {
                    var handleField = typeof(Slider).GetField(
                        "m_HandleRect",
                        System.Reflection.BindingFlags.NonPublic
                            | System.Reflection.BindingFlags.Instance
                    );
                    handleField?.SetValue(slider, handleRect);

                    // 设置targetGraphic为Handle的Image
                    var handleImage = handleObj.GetComponent<Image>();
                    if (handleImage != null)
                    {
                        slider.targetGraphic = handleImage;
                        handleImage.raycastTarget = true;
                    }
                }
            }

            // 配置Slider基本属性
            slider.interactable = true;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.wholeNumbers = false;
            slider.direction = Slider.Direction.LeftToRight;
        }
    }
}
