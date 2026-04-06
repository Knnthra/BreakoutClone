using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// URP Renderer Feature that pixelates the rendered image to create a retro pixel art look.
/// Add this to the Universal Renderer asset via the Inspector.
/// </summary>
public class PixelArtFeature : ScriptableRendererFeature
{
    /// <summary>
    /// User-configurable settings for the pixel art effect.
    /// </summary>
    [System.Serializable]
    public class PixelArtSettings
    {
        [Header("Pixelation")]

        /// <summary>
        /// Size of each visible pixel block. Higher values produce chunkier pixels.
        /// A value of 1 disables the effect.
        /// </summary>
        [Range(1, 20)]
        public int pixelSize = 4;

        [Header("Color")]

        /// <summary>
        /// Reduces the number of distinct colors to simulate a retro palette.
        /// </summary>
        public bool enableColorQuantization;

        /// <summary>
        /// Number of distinct levels per color channel (R, G, B). Lower values
        /// produce a more posterized, retro look.
        /// </summary>
        [Range(2, 32)]
        public int colorLevels = 8;

        [Header("Outlines")]

        /// <summary>
        /// Adds dark outlines at edges to give a hand-drawn pixel art feel.
        /// </summary>
        public bool enableOutlines;

        /// <summary>
        /// Sensitivity for edge detection. Lower values detect more edges.
        /// </summary>
        [Range(0.05f, 1.0f)]
        public float outlineThreshold = 0.3f;

        /// <summary>
        /// Color used for detected edges.
        /// </summary>
        public Color outlineColor = new Color(0.05f, 0.05f, 0.05f, 1f);

        [Header("Dithering")]

        /// <summary>
        /// Applies ordered dithering to break up color banding.
        /// </summary>
        public bool enableDithering;

        /// <summary>
        /// How strong the dithering pattern is. Higher values are more visible.
        /// </summary>
        [Range(0.01f, 0.15f)]
        public float ditherStrength = 0.03f;

        [Header("Injection")]

        /// <summary>
        /// When to inject the pixel art pass in the rendering pipeline.
        /// </summary>
        public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;
    }

    /// <summary>
    /// Settings for the pixel art effect.
    /// </summary>
    [SerializeField] private PixelArtSettings settings = new PixelArtSettings();

    /// <summary>
    /// Direct reference to the PixelArt shader. Ensures it is included in builds.
    /// </summary>
    [SerializeField] private Shader pixelArtShader;

    private Material pixelArtMaterial;
    private PixelArtPass pixelArtPass;

    public override void Create()
    {
        if (pixelArtShader == null)
        {
            Debug.LogWarning("PixelArtFeature: No shader assigned.");
            return;
        }

        pixelArtMaterial = CoreUtils.CreateEngineMaterial(pixelArtShader);
        pixelArtPass = new PixelArtPass(pixelArtMaterial, settings);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (pixelArtMaterial == null || pixelArtPass == null)
            return;

        if (renderingData.cameraData.cameraType == CameraType.Preview)
            return;

        pixelArtPass.UpdateSettings(settings);
        renderer.EnqueuePass(pixelArtPass);
    }

    protected override void Dispose(bool disposing)
    {
        pixelArtPass?.Dispose();
        if (pixelArtMaterial != null)
            CoreUtils.Destroy(pixelArtMaterial);
    }
}
