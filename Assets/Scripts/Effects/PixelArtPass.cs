using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Render pass that downscales the camera image to a low-resolution texture and
/// upscales it back with point filtering to create a pixel art look.
/// </summary>
public class PixelArtPass : ScriptableRenderPass, IDisposable
{
    private static readonly int EnableQuantizationID = Shader.PropertyToID("_EnableQuantization");
    private static readonly int ColorLevelsID = Shader.PropertyToID("_ColorLevels");
    private static readonly int EnableOutlinesID = Shader.PropertyToID("_EnableOutlines");
    private static readonly int OutlineThresholdID = Shader.PropertyToID("_OutlineThreshold");
    private static readonly int OutlineColorID = Shader.PropertyToID("_OutlineColor");
    private static readonly int EnableDitheringID = Shader.PropertyToID("_EnableDithering");
    private static readonly int DitherStrengthID = Shader.PropertyToID("_DitherStrength");
    private static readonly int PixelSizeID = Shader.PropertyToID("_PixelSize");

    private static readonly ProfilingSampler profilingSampler = new ProfilingSampler("PixelArtPass");

    private Material material;
    private int pixelSize;
    private int colorLevels;
    private bool enableColorQuantization;
    private bool enableOutlines;
    private float outlineThreshold;
    private Color outlineColor;
    private bool enableDithering;
    private float ditherStrength;

    public PixelArtPass(Material material, PixelArtFeature.PixelArtSettings settings)
    {
        this.material = material;
        UpdateSettings(settings);
        requiresIntermediateTexture = true;
    }

    /// <summary>
    /// Updates pass settings from the feature each frame.
    /// </summary>
    public void UpdateSettings(PixelArtFeature.PixelArtSettings settings)
    {
        pixelSize = Mathf.Max(1, settings.pixelSize);
        colorLevels = settings.colorLevels;
        enableColorQuantization = settings.enableColorQuantization;
        enableOutlines = settings.enableOutlines;
        outlineThreshold = settings.outlineThreshold;
        outlineColor = settings.outlineColor;
        enableDithering = settings.enableDithering;
        ditherStrength = settings.ditherStrength;
        renderPassEvent = settings.renderPassEvent;
    }

    private void SetMaterialProperties()
    {
        material.SetFloat(EnableQuantizationID, enableColorQuantization ? 1f : 0f);
        material.SetFloat(ColorLevelsID, colorLevels);
        material.SetFloat(EnableOutlinesID, enableOutlines ? 1f : 0f);
        material.SetFloat(OutlineThresholdID, outlineThreshold);
        material.SetColor(OutlineColorID, outlineColor);
        material.SetFloat(EnableDitheringID, enableDithering ? 1f : 0f);
        material.SetFloat(DitherStrengthID, ditherStrength);
        material.SetFloat(PixelSizeID, pixelSize);
    }

    private class PassData
    {
        public TextureHandle source;
        public TextureHandle lowResTarget;
        public Material material;
    }

    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        if (material == null || pixelSize <= 1)
            return;

        UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
        UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

        TextureHandle source = resourceData.activeColorTexture;

        int screenWidth = cameraData.cameraTargetDescriptor.width;
        int screenHeight = cameraData.cameraTargetDescriptor.height;
        int lowResWidth = Mathf.Max(1, screenWidth / pixelSize);
        int lowResHeight = Mathf.Max(1, screenHeight / pixelSize);

        RenderTextureDescriptor lowResDesc = cameraData.cameraTargetDescriptor;
        lowResDesc.width = lowResWidth;
        lowResDesc.height = lowResHeight;
        lowResDesc.depthBufferBits = 0;
        lowResDesc.msaaSamples = 1;

        TextureHandle lowResTexture = UniversalRenderer.CreateRenderGraphTexture(
            renderGraph, lowResDesc, "_PixelArtLowRes", false, FilterMode.Point);

        SetMaterialProperties();

        // Pass 1: Downscale source to low-res with outlines, dithering, and quantization
        using (var builder = renderGraph.AddRasterRenderPass<PassData>(
            "PixelArt_Downscale", out var passData, profilingSampler))
        {
            passData.source = source;
            passData.material = material;

            builder.UseTexture(source, AccessFlags.Read);
            builder.SetRenderAttachment(lowResTexture, 0, AccessFlags.Write);

            builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
            {
                Blitter.BlitTexture(context.cmd, data.source, new Vector4(1, 1, 0, 0), data.material, 0);
            });
        }

        // Pass 2: Upscale low-res back to screen with point filtering
        using (var builder = renderGraph.AddRasterRenderPass<PassData>(
            "PixelArt_Upscale", out var passData2, profilingSampler))
        {
            passData2.lowResTarget = lowResTexture;
            passData2.material = material;

            builder.UseTexture(lowResTexture, AccessFlags.Read);
            builder.SetRenderAttachment(resourceData.activeColorTexture, 0, AccessFlags.Write);

            builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
            {
                Blitter.BlitTexture(context.cmd, data.lowResTarget, new Vector4(1, 1, 0, 0), data.material, 1);
            });
        }
    }

    public void Dispose() { }
}
