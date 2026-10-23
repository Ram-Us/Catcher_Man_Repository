using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

// Unity 6 / URP 17 / Universal Renderer (Forward) / Render Graph enabled.
// Add to the Renderer Data asset, NOT to a GameObject.
public class PixelGroundRendererFeature : ScriptableRendererFeature
{
    [SerializeField] private LayerMask targetLayers;
    [SerializeField, Min(16)] private int resolutionHeight = 180;
    [SerializeField] private Material compositeMaterial;
    [SerializeField] private bool showInSceneView = true;

    private GroundPass groundPass;

    public override void Create()
    {
        groundPass = new GroundPass();
    }

    public override void AddRenderPasses(
        ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        CameraType type = renderingData.cameraData.cameraType;
        if (type != CameraType.Game &&
            !(showInSceneView && type == CameraType.SceneView))
        {
            return;
        }

        if (renderingData.cameraData.renderType != CameraRenderType.Base ||
            compositeMaterial == null || targetLayers.value == 0)
        {
            return;
        }

        groundPass.Setup(targetLayers, resolutionHeight, compositeMaterial);
        renderer.EnqueuePass(groundPass);
    }

    private sealed class GroundPass : ScriptableRenderPass
    {
        private static readonly int GroundColorId =
            Shader.PropertyToID("_PixelGroundColor");
        private static readonly int GroundDepthId =
            Shader.PropertyToID("_PixelGroundDepth");

        private readonly List<ShaderTagId> shaderTags = new()
        {
            new ShaderTagId("UniversalForward"),
            new ShaderTagId("UniversalForwardOnly"),
            new ShaderTagId("SRPDefaultUnlit")
        };

        private LayerMask layers;
        private int height;
        private Material material;

        private sealed class GroundData
        {
            public RendererListHandle renderers;
        }

        private sealed class CompositeData
        {
            public TextureHandle color;
            public TextureHandle depth;
            public Material material;
            public MaterialPropertyBlock properties;
        }

        public GroundPass()
        {
            // Opaque geometry and sky are ready. Transparent sprites/VFX follow.
            renderPassEvent = RenderPassEvent.BeforeRenderingTransparents;
        }

        public void Setup(LayerMask target, int targetHeight, Material composite)
        {
            layers = target;
            height = Mathf.Max(16, targetHeight);
            material = composite;
        }

        public override void RecordRenderGraph(
            RenderGraph renderGraph, ContextContainer frameContext)
        {
            UniversalResourceData resources =
                frameContext.Get<UniversalResourceData>();
            UniversalCameraData camera =
                frameContext.Get<UniversalCameraData>();
            UniversalRenderingData rendering =
                frameContext.Get<UniversalRenderingData>();
            UniversalLightData lighting =
                frameContext.Get<UniversalLightData>();

            if (!resources.activeColorTexture.IsValid() ||
                !resources.activeDepthTexture.IsValid())
            {
                return;
            }

            RenderTextureDescriptor colorDesc = camera.cameraTargetDescriptor;
            int fullWidth = Mathf.Max(1, colorDesc.width);
            int fullHeight = Mathf.Max(1, colorDesc.height);
            colorDesc.height = Mathf.Min(height, fullHeight);
            colorDesc.width = Mathf.Max(1, Mathf.RoundToInt(
                colorDesc.height * (float)fullWidth / fullHeight));
            colorDesc.msaaSamples = 1;
            colorDesc.bindMS = false;
            colorDesc.depthBufferBits = 0;
            colorDesc.depthStencilFormat = GraphicsFormat.None;
            colorDesc.useMipMap = false;
            colorDesc.autoGenerateMips = false;
            colorDesc.enableRandomWrite = false;
            colorDesc.useDynamicScale = false;

            TextureHandle groundColor = UniversalRenderer.CreateRenderGraphTexture(
                renderGraph, colorDesc, "PixelGround Color", true,
                FilterMode.Point);

            RenderTextureDescriptor depthDesc = colorDesc;
            depthDesc.graphicsFormat = GraphicsFormat.None;
            depthDesc.depthStencilFormat = GraphicsFormat.D32_SFloat;
            TextureHandle groundDepth = UniversalRenderer.CreateRenderGraphTexture(
                renderGraph, depthDesc, "PixelGround Depth", true,
                FilterMode.Point);

            DrawingSettings drawing = RenderingUtils.CreateDrawingSettings(
                shaderTags, rendering, camera, lighting,
                camera.defaultOpaqueSortFlags);
            FilteringSettings filtering = new(
                RenderQueueRange.opaque, layers.value);
            RendererListParams listParams = new(
                rendering.cullResults, drawing, filtering);

            // Draw selected objects with their existing opaque materials.
            using (var builder = renderGraph.AddRasterRenderPass<GroundData>(
                "PixelGround: low-resolution draw", out var data))
            {
                data.renderers = renderGraph.CreateRendererList(listParams);
                builder.UseRendererList(data.renderers);
                builder.UseAllGlobalTextures(true);
                builder.SetRenderAttachment(groundColor, 0, AccessFlags.Write);
                builder.SetRenderAttachmentDepth(groundDepth, AccessFlags.Write);
                builder.SetRenderFunc(static (GroundData pass, RasterGraphContext ctx) =>
                {
                    ctx.cmd.ClearRenderTarget(true, true, Color.clear);
                    ctx.cmd.DrawRendererList(pass.renderers);
                });
            }

            // Point-sample color AND depth. Hardware depth testing preserves
            // foreground opaque objects; writing depth lets later sprites test it.
            using (var builder = renderGraph.AddRasterRenderPass<CompositeData>(
                "PixelGround: depth composite", out var data))
            {
                data.color = groundColor;
                data.depth = groundDepth;
                data.material = material;
                data.properties = new MaterialPropertyBlock();
                builder.UseTexture(groundColor, AccessFlags.Read);
                builder.UseTexture(groundDepth, AccessFlags.Read);
                builder.SetRenderAttachment(
                    resources.activeColorTexture, 0, AccessFlags.ReadWrite);
                builder.SetRenderAttachmentDepth(
                    resources.activeDepthTexture, AccessFlags.ReadWrite);
                builder.SetRenderFunc(static (CompositeData pass, RasterGraphContext ctx) =>
                {
                    pass.properties.SetTexture(GroundColorId, (Texture)pass.color);
                    pass.properties.SetTexture(GroundDepthId, (Texture)pass.depth);
                    ctx.cmd.DrawProcedural(
                        Matrix4x4.identity, pass.material, 0,
                        MeshTopology.Triangles, 3, 1, pass.properties);
                });
            }
        }
    }
}
