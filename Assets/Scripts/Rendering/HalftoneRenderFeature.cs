using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace MaskGame.Rendering
{
    public class HalftoneRenderFeature : ScriptableRendererFeature
    {
        [System.Serializable]
        public class HalftoneSettings
        {
            public RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
            public Material material;

            [Range(1, 20)]
            public float dotSize = 5f;

            [Range(0, 1)]
            public float dotIntensity = 0.5f;
        }

        public HalftoneSettings settings = new HalftoneSettings();
        private HalftoneRenderPass renderPass;

        public override void Create()
        {
            if (settings.material == null)
            {
                Debug.LogWarning("HalftoneRenderFeature: Material is not assigned!");
                return;
            }

            renderPass = new HalftoneRenderPass(settings);
        }

        public override void AddRenderPasses(
            ScriptableRenderer renderer,
            ref RenderingData renderingData
        )
        {
            if (renderPass != null && settings.material != null)
            {
                renderer.EnqueuePass(renderPass);
            }
        }

        class HalftoneRenderPass : ScriptableRenderPass
        {
            private Material material;
            private HalftoneSettings settings;
            private RenderTargetIdentifier source;
            private RenderTargetHandle tempTexture;

            public HalftoneRenderPass(HalftoneSettings settings)
            {
                this.settings = settings;
                this.material = settings.material;
                this.renderPassEvent = settings.renderPassEvent;
                tempTexture.Init("_TempHalftoneTexture");
            }

            public override void Execute(
                ScriptableRenderContext context,
                ref RenderingData renderingData
            )
            {
                if (material == null)
                    return;

                CommandBuffer cmd = CommandBufferPool.Get("HalftoneEffect");

                RenderTextureDescriptor descriptor = renderingData
                    .cameraData
                    .cameraTargetDescriptor;
                descriptor.depthBufferBits = 0;

                // 更新shader参数
                material.SetFloat("_DotSize", settings.dotSize);
                material.SetFloat("_DotIntensity", settings.dotIntensity);

                // 获取相机渲染目标
                RenderTargetIdentifier cameraTarget = renderingData
                    .cameraData
                    .renderer
                    .cameraColorTarget;

                // 创建临时纹理
                cmd.GetTemporaryRT(tempTexture.id, descriptor);

                // 应用效果
                cmd.Blit(cameraTarget, tempTexture.Identifier(), material, 0);
                cmd.Blit(tempTexture.Identifier(), cameraTarget);

                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }

            public override void FrameCleanup(CommandBuffer cmd)
            {
                cmd.ReleaseTemporaryRT(tempTexture.id);
            }
        }
    }
}
