Shader "Hidden/PixelArt"
{
    Properties
    {
        _ColorLevels ("Color Levels", Float) = 8
        _EnableQuantization ("Enable Quantization", Float) = 0
        _EnableOutlines ("Enable Outlines", Float) = 0
        _OutlineThreshold ("Outline Threshold", Float) = 0.1
        _OutlineColor ("Outline Color", Color) = (0.05, 0.05, 0.05, 1)
        _EnableDithering ("Enable Dithering", Float) = 0
        _DitherStrength ("Dither Strength", Float) = 0.03
        _PixelSize ("Pixel Size", Float) = 4
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
        }

        ZWrite Off
        ZTest Always
        Cull Off

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

        float _ColorLevels;
        float _EnableQuantization;
        float _EnableOutlines;
        float _OutlineThreshold;
        float4 _OutlineColor;
        float _EnableDithering;
        float _DitherStrength;
        float _PixelSize;

        // 4x4 Bayer dithering matrix normalized to -0.5..0.5 range
        static const float BayerMatrix[16] =
        {
             0.0/16.0 - 0.5,  8.0/16.0 - 0.5,  2.0/16.0 - 0.5, 10.0/16.0 - 0.5,
            12.0/16.0 - 0.5,  4.0/16.0 - 0.5, 14.0/16.0 - 0.5,  6.0/16.0 - 0.5,
             3.0/16.0 - 0.5, 11.0/16.0 - 0.5,  1.0/16.0 - 0.5,  9.0/16.0 - 0.5,
            15.0/16.0 - 0.5,  7.0/16.0 - 0.5, 13.0/16.0 - 0.5,  5.0/16.0 - 0.5
        };

        float GetDither(float2 screenPos)
        {
            int2 pixel = int2(fmod(screenPos, 4.0));
            return BayerMatrix[pixel.y * 4 + pixel.x];
        }

        float3 QuantizeColor(float3 color, float levels)
        {
            return floor(color * levels + 0.5) / levels;
        }

        // Luminance for edge detection
        float Luminance3(float3 color)
        {
            return dot(color, float3(0.299, 0.587, 0.114));
        }
        ENDHLSL

        // Pass 0: Downscale with outlines, dithering, and color quantization
        Pass
        {
            Name "PixelArt_Downscale"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragDownscale

            half4 FragDownscale(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;
                half4 color = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_PointClamp, uv);

                // Edge detection using Sobel on the low-res grid
                if (_EnableOutlines > 0.5)
                {
                    float2 texel = _BlitTexture_TexelSize.xy;

                    float tl = Luminance3(SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_PointClamp, uv + float2(-texel.x, texel.y)).rgb);
                    float t  = Luminance3(SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_PointClamp, uv + float2(0, texel.y)).rgb);
                    float tr = Luminance3(SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_PointClamp, uv + float2(texel.x, texel.y)).rgb);
                    float l  = Luminance3(SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_PointClamp, uv + float2(-texel.x, 0)).rgb);
                    float r  = Luminance3(SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_PointClamp, uv + float2(texel.x, 0)).rgb);
                    float bl = Luminance3(SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_PointClamp, uv + float2(-texel.x, -texel.y)).rgb);
                    float b  = Luminance3(SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_PointClamp, uv + float2(0, -texel.y)).rgb);
                    float br = Luminance3(SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_PointClamp, uv + float2(texel.x, -texel.y)).rgb);

                    float sobelX = -tl - 2.0 * l - bl + tr + 2.0 * r + br;
                    float sobelY = -tl - 2.0 * t - tr + bl + 2.0 * b + br;
                    float edge = sqrt(sobelX * sobelX + sobelY * sobelY);

                    if (edge > _OutlineThreshold)
                        color.rgb = _OutlineColor.rgb;
                }

                // Ordered dithering before quantization to break up gradients
                if (_EnableDithering > 0.5)
                {
                    float2 screenPos = uv * _BlitTexture_TexelSize.zw;
                    float dither = GetDither(screenPos) * _DitherStrength;
                    color.rgb += dither;
                }

                // Color quantization
                if (_EnableQuantization > 0.5)
                    color.rgb = QuantizeColor(color.rgb, _ColorLevels);

                return color;
            }
            ENDHLSL
        }

        // Pass 1: Upscale (point-filtered blit)
        Pass
        {
            Name "PixelArt_Upscale"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragUpscale

            half4 FragUpscale(Varyings input) : SV_Target
            {
                return SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_PointClamp, input.texcoord);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
