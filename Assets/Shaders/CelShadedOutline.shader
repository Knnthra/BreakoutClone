Shader "Custom/CelShadedOutline"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (1, 0.8, 0.6, 1)
        _BaseMap ("Base Texture", 2D) = "white" {}

        [Header(Cel Shading)]
        _CelBands ("Cel Bands", Range(2, 10)) = 3
        _CelSmoothness ("Band Smoothness", Range(0, 0.5)) = 0.05
        _ShadowColor ("Shadow Color", Color) = (0.6, 0.5, 0.4, 1)
        _ShadowThreshold ("Shadow Threshold", Range(0, 1)) = 0.5

        [Header(Rim Light)]
        _RimColor ("Rim Color", Color) = (1, 1, 1, 1)
        _RimPower ("Rim Power", Range(0.1, 8)) = 3
        _RimIntensity ("Rim Intensity", Range(0, 1)) = 0.3
        _RimThreshold ("Rim Threshold", Range(0, 1)) = 0.7

        [Header(Surface)]
        _AmbientIntensity ("Ambient Intensity", Range(0, 2)) = 1.2
        _Saturation ("Saturation", Range(0, 2)) = 1.0
        _Alpha ("Alpha", Range(0, 1)) = 1.0

        [Header(Circular Cutout)]
        _CutoutRadius ("Cutout Radius", Float) = 2.0
        _EdgeSoftness ("Edge Softness", Float) = 0.5
        _CutoutAspect ("Cutout Aspect Ratio", Vector) = (1, 1, 0, 0)

        [Header(Outline)]
        _OutlineWidth ("Outline Width", Range(0, 0.1)) = 0.01
        _OutlineColor ("Outline Color", Color) = (0, 0, 0, 1)
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }

        // PASS 1: Render main geometry first and write stencil
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite On
            ZTest LEqual
            Cull Off  // Render both front and back faces so we can discard both in cutout

            // Write to stencil to mark where the character mesh is rendered
            Stencil
            {
                Ref 1
                Comp Always
                Pass Replace
            }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile_instancing
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
                float3 viewDirWS : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor;
                float _CelBands;
                float _CelSmoothness;
                float4 _ShadowColor;
                float _ShadowThreshold;
                float4 _RimColor;
                float _RimPower;
                float _RimIntensity;
                float _RimThreshold;
                float _AmbientIntensity;
                float _Saturation;
                float _Alpha;
                float _CutoutRadius;
                float _EdgeSoftness;
                float4 _CutoutAspect;
                float _OutlineWidth;
                float4 _OutlineColor;
            CBUFFER_END

            // Cutout positions (support up to 4 simultaneous cutouts)
            float4 _CutoutPositions[4]; // xyz = position, w = active (1) or inactive (0)
            float _CutoutCount;
            float3 _CameraForward; // Camera forward direction for cylindrical cutout

            Varyings vert(Attributes input)
            {
                Varyings output;

                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS);

                output.positionCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.normalWS = normalInputs.normalWS;
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.viewDirWS = GetWorldSpaceViewDir(positionInputs.positionWS);

                return output;
            }

            // Cel shading function - creates distinct bands of color
            float CelShade(float value, float bands, float smoothness)
            {
                float stepped = floor(value * bands) / bands;
                return lerp(stepped, value, smoothness);
            }

            // Adjust saturation of a color
            float3 AdjustSaturation(float3 color, float saturation)
            {
                // Calculate luminance (grayscale value)
                float luminance = dot(color, float3(0.299, 0.587, 0.114));

                // Lerp between grayscale and original color based on saturation
                return lerp(float3(luminance, luminance, luminance), color, saturation);
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                // Sample base texture
                half4 baseMap = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                half4 albedo = baseMap * _BaseColor;

                // Normalize vectors
                float3 normalWS = normalize(input.normalWS);
                float3 viewDirWS = normalize(input.viewDirWS);

                // Get main light
                Light mainLight = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                float3 lightDir = normalize(mainLight.direction);

                // Basic Lambert lighting
                float NdotL = dot(normalWS, lightDir);
                float lightIntensity = saturate(NdotL);

                // Apply cel shading to lighting
                float celLighting = CelShade(lightIntensity, _CelBands, _CelSmoothness);

                // Shadow handling - binary shadow with smooth transition
                float shadowStep = step(_ShadowThreshold, mainLight.shadowAttenuation);
                shadowStep = lerp(shadowStep, mainLight.shadowAttenuation, _CelSmoothness * 2.0);

                // Choose between lit and shadow color
                float3 lightingColor = lerp(_ShadowColor.rgb, float3(1, 1, 1), celLighting);
                lightingColor = lerp(_ShadowColor.rgb, lightingColor, shadowStep);

                // Apply lighting to albedo
                float3 diffuse = albedo.rgb * lightingColor * mainLight.color;

                // Cel-shaded rim light
                float rimDot = 1.0 - saturate(dot(viewDirWS, normalWS));
                float rimIntensity = pow(rimDot, _RimPower);
                float rimStep = step(_RimThreshold, rimIntensity);
                rimStep = lerp(rimStep, rimIntensity, _CelSmoothness * 2.0);
                float3 rimLight = rimStep * _RimIntensity * _RimColor.rgb;

                // Ambient lighting
                float3 ambient = albedo.rgb * unity_SHAr.rgb * _AmbientIntensity;

                // Additional lights with cel shading
                #ifdef _ADDITIONAL_LIGHTS
                uint pixelLightCount = GetAdditionalLightsCount();
                for (uint lightIndex = 0u; lightIndex < pixelLightCount; ++lightIndex)
                {
                    Light light = GetAdditionalLight(lightIndex, input.positionWS);
                    float3 addLightDir = normalize(light.direction);
                    float addNdotL = dot(normalWS, addLightDir);
                    float addLightIntensity = saturate(addNdotL);

                    // Cel shade additional lights
                    float addCelLighting = CelShade(addLightIntensity, _CelBands, _CelSmoothness);

                    // Shadow step for additional lights
                    float addShadowStep = step(0.5, light.shadowAttenuation);
                    addShadowStep = lerp(addShadowStep, light.shadowAttenuation, _CelSmoothness * 2.0);

                    diffuse += albedo.rgb * light.color * addCelLighting * addShadowStep * light.distanceAttenuation;
                }
                #endif

                // Combine all lighting
                float3 finalColor = diffuse + ambient + rimLight;

                // Apply saturation adjustment
                finalColor = AdjustSaturation(finalColor, _Saturation);

                // Calculate circular cutout alpha
                float cutoutAlpha = 1.0;

                for(int i = 0; i < _CutoutCount && i < 4; i++)
                {
                    if(_CutoutPositions[i].w > 0.5) // Check if this cutout is active
                    {
                        float3 cutoutPos = _CutoutPositions[i].xyz;

                        // Calculate distance from pixel to the camera-player ray
                        float3 cameraForward = normalize(_CameraForward);
                        float3 toPixel = input.positionWS - cutoutPos;

                        // Project toPixel onto camera forward to get distance along ray
                        float alongRay = dot(toPixel, cameraForward);

                        // Get perpendicular distance from pixel to the ray (in XZ plane only)
                        float3 projectedOnRay = cutoutPos + cameraForward * alongRay;
                        float2 perpendicular = float2(input.positionWS.x - projectedOnRay.x, input.positionWS.z - projectedOnRay.z);

                        // Apply aspect ratio to make cutout adjustable (x = horizontal scale, y = vertical scale)
                        perpendicular *= _CutoutAspect.xy;
                        float dist = length(perpendicular);

                        // If within cutout radius, make fully transparent
                        if(dist < _CutoutRadius)
                        {
                            cutoutAlpha = 0.0;
                            break;
                        }
                        else if(dist < _CutoutRadius + _EdgeSoftness)
                        {
                            // Soft edge only at the border
                            float edgeFade = smoothstep(_CutoutRadius, _CutoutRadius + _EdgeSoftness, dist);
                            cutoutAlpha = min(cutoutAlpha, edgeFade);
                        }
                    }
                }

                // Combine cutout alpha with material alpha
                float finalAlpha = albedo.a * _Alpha * cutoutAlpha;

                // Discard fully transparent pixels
                if(finalAlpha < 0.01)
                {
                    discard;
                }

                return half4(finalColor, finalAlpha);
            }
            ENDHLSL
        }

        // Shadow casting pass
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            float3 _LightDirection;

            Varyings ShadowPassVertex(Attributes input)
            {
                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);

                output.positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, _LightDirection));
                return output;
            }

            half4 ShadowPassFragment(Varyings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }

        // Depth pass for depth prepass
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings DepthOnlyVertex(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            half4 DepthOnlyFragment(Varyings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }

        // DepthNormals pass for depth texture generation
        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }

            ZWrite On
            Cull Back

            HLSLPROGRAM
            #pragma vertex DepthNormalsVertex
            #pragma fragment DepthNormalsFragment
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings DepthNormalsVertex(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS);

                output.positionCS = positionInputs.positionCS;
                output.normalWS = normalInputs.normalWS;

                return output;
            }

            half4 DepthNormalsFragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                float3 normalWS = normalize(input.normalWS);
                return half4(normalWS, 0);
            }
            ENDHLSL
        }

        // PASS 2: Render outline AFTER main geometry, using stencil to avoid drawing on character
        Pass
        {
            Name "Outline"
            Tags { "LightMode" = "SRPDefaultUnlit" }

            Cull Front
            ZWrite On
            ZTest LEqual
            Blend SrcAlpha OneMinusSrcAlpha

            // Only render outline where stencil is NOT 1 (i.e., not on the character mesh)
            Stencil
            {
                Ref 1
                Comp NotEqual
            }

            HLSLPROGRAM
            #pragma vertex OutlineVertex
            #pragma fragment OutlineFragment
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 originalPositionWS : TEXCOORD1; // Original position before outline expansion
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            float _OutlineWidth;
            float4 _OutlineColor;
            float _Alpha;
            float _CutoutRadius;
            float _EdgeSoftness;
            float4 _CutoutAspect;

            float4 _CutoutPositions[4];
            float _CutoutCount;
            float3 _CameraForward;

            Varyings OutlineVertex(Attributes input)
            {
                Varyings output;

                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalOS = input.tangentOS.w == 0 ? input.tangentOS.xyz : input.normalOS;
                float3 normalWS = TransformObjectToWorldNormal(normalOS);

                // Store original position before expansion for cutout check
                output.originalPositionWS = positionWS;

                // Expand position for outline
                positionWS += normalWS * _OutlineWidth;
                output.positionWS = positionWS;
                output.positionCS = TransformWorldToHClip(positionWS);

                return output;
            }

            half4 OutlineFragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                float cutoutAlpha = 1.0;
                for(int i = 0; i < _CutoutCount && i < 4; i++)
                {
                    if(_CutoutPositions[i].w > 0.5)
                    {
                        float3 cutoutPos = _CutoutPositions[i].xyz;

                        // Calculate distance from pixel to the camera-player ray
                        float3 cameraForward = normalize(_CameraForward);
                        float3 toPixel = input.originalPositionWS - cutoutPos;

                        // Project toPixel onto camera forward to get distance along ray
                        float alongRay = dot(toPixel, cameraForward);

                        // Get perpendicular distance from pixel to the ray (in XZ plane only)
                        float3 projectedOnRay = cutoutPos + cameraForward * alongRay;
                        float2 perpendicular = float2(input.originalPositionWS.x - projectedOnRay.x, input.originalPositionWS.z - projectedOnRay.z);

                        // Apply aspect ratio
                        perpendicular *= _CutoutAspect.xy;
                        float dist = length(perpendicular);

                        if(dist < _CutoutRadius)
                        {
                            cutoutAlpha = 0.0;
                            break;
                        }
                        else if(dist < _CutoutRadius + _EdgeSoftness)
                        {
                            float edgeFade = smoothstep(_CutoutRadius, _CutoutRadius + _EdgeSoftness, dist);
                            cutoutAlpha = min(cutoutAlpha, edgeFade);
                        }
                    }
                }

                float finalAlpha = _OutlineColor.a * _Alpha * cutoutAlpha;
                if(finalAlpha < 0.01) discard;

                return half4(_OutlineColor.rgb, finalAlpha);
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Lit"
}
