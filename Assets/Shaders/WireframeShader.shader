Shader "Custom/WireframeFadeIn"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (1,1,1,1)
        _WireColor ("Wire Color", Color) = (0, 1, 1, 1)
        _WireThickness ("Wire Thickness", Range(0, 10)) = 2
        _WireSmoothing ("Wire Smoothing", Range(0, 10)) = 1
        _FadeAmount ("Fade Amount", Range(0, 1)) = 0
        _Alpha ("Alpha", Range(0, 1)) = 1
        _BuildUpProgress ("Build Up Progress", Range(0, 1)) = 0
        _BuildUpSoftness ("Build Up Edge Softness", Range(0, 1)) = 0.1
        _BuildUpEdgeGlow ("Build Up Edge Glow", Color) = (0, 2, 2, 1)
        _BuildUpEdgeWidth ("Build Up Edge Width", Range(0, 0.5)) = 0.05
        _WorldMinY ("World Min Y", Float) = 0
        _WorldMaxY ("World Max Y", Float) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Transparent"
            "Queue"="Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }

        LOD 100
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite On
        Cull Back

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma geometry geom

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
                float3 barycentric : TEXCOORD3;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _BaseColor;
                float4 _WireColor;
                float _WireThickness;
                float _WireSmoothing;
                float _FadeAmount;
                float _Alpha;
                float _BuildUpProgress;
                float _BuildUpSoftness;
                float4 _BuildUpEdgeGlow;
                float _BuildUpEdgeWidth;
                float _WorldMinY;
                float _WorldMaxY;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;

                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS);

                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                output.normalWS = normalInput.normalWS;
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.barycentric = float3(0, 0, 0);

                return output;
            }

            [maxvertexcount(3)]
            void geom(triangle Varyings input[3], inout TriangleStream<Varyings> outputStream)
            {
                Varyings output;

                // First vertex
                output = input[0];
                output.barycentric = float3(1, 0, 0);
                outputStream.Append(output);

                // Second vertex
                output = input[1];
                output.barycentric = float3(0, 1, 0);
                outputStream.Append(output);

                // Third vertex
                output = input[2];
                output.barycentric = float3(0, 0, 1);
                outputStream.Append(output);

                outputStream.RestartStrip();
            }

            half4 frag(Varyings input) : SV_Target
            {
                // Calculate world Y position of this fragment
                float worldY = input.positionWS.y;

                // Normalize based on the GLOBAL object bounds
                float worldRange = _WorldMaxY - _WorldMinY;
                if (worldRange < 0.001) worldRange = 1.0;

                float heightFactor = (worldY - _WorldMinY) / worldRange;
                heightFactor = saturate(heightFactor);

                // Calculate the distance from the build-up edge
                float distanceFromEdge = heightFactor - _BuildUpProgress;

                // Hard cutoff - discard pixels above the build line
                if (distanceFromEdge > _BuildUpSoftness)
                    discard;

                // Calculate build-up mask
                float buildUpMask = 1.0 - smoothstep(-_BuildUpSoftness, _BuildUpSoftness, distanceFromEdge);

                // Calculate edge glow
                float edgeGlow = 0;
                if (distanceFromEdge < _BuildUpEdgeWidth && distanceFromEdge > -_BuildUpEdgeWidth)
                {
                    float edgeDistance = abs(distanceFromEdge);
                    edgeGlow = 1.0 - smoothstep(0, _BuildUpEdgeWidth, edgeDistance);
                    edgeGlow = pow(edgeGlow, 2.0);
                }

                // Calculate wireframe
                float3 barys = input.barycentric;
                float3 deltas = fwidth(barys);
                float3 smoothing = deltas * _WireSmoothing;
                float3 thickness = deltas * _WireThickness;
                barys = smoothstep(thickness, thickness + smoothing, barys);
                float minBary = min(barys.x, min(barys.y, barys.z));

                // Calculate wireframe line strength (1 = on edge, 0 = on face)
                float wireStrength = 1.0 - minBary;

                // Create wireframe-only effect
                half4 finalColor = _WireColor;
                finalColor.a = wireStrength * _Alpha;

                // Add edge glow effect
                if (edgeGlow > 0 && _BuildUpProgress < 1.0)
                {
                    finalColor.rgb += _BuildUpEdgeGlow.rgb * edgeGlow;
                    finalColor.a = saturate(finalColor.a + edgeGlow);
                }

                // Apply build-up mask for smooth fade-in
                finalColor.a *= buildUpMask;

                return finalColor;
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Lit"
}
