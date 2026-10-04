Shader "Rice Foliage/URP Lit Cutout"
{
    Properties
    {
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}
        [MainColor] _BaseColor("Base Color", Color) = (1,1,1,1)
        _Cutoff("Alpha Cutoff", Range(0,1)) = 0.35
        _Smoothness("Smoothness", Range(0,1)) = 0.12
        _WindStrength("Wind Strength", Range(0,0.5)) = 0.08
        _WindSpeed("Wind Speed", Range(0,8)) = 1.5
        _WindScale("Wind Scale", Range(0.01,8)) = 1.2
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "TransparentCutout"
            "Queue" = "AlphaTest"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Cull Off
        ZWrite On
        AlphaToMask On

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half _Cutoff;
                half _Smoothness;
                float _WindStrength;
                float _WindSpeed;
                float _WindScale;
            CBUFFER_END

            float4 _RiceGlobalWindDirection;
            float4 _RiceGlobalWindParameters;

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
                half3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float3 ApplyWind(float3 positionOS)
            {
                float3 world = TransformObjectToWorld(positionOS);
                float useGlobal = step(0.5, _RiceGlobalWindParameters.w);
                float strength = lerp(_WindStrength, _RiceGlobalWindParameters.x, useGlobal);
                float speed = lerp(_WindSpeed, _RiceGlobalWindParameters.y, useGlobal);
                float scale = lerp(_WindScale, _RiceGlobalWindParameters.z, useGlobal);
                float2 direction = normalize(lerp(float2(1.0, 0.35), _RiceGlobalWindDirection.xy, useGlobal) + 0.0001);
                float anchor = saturate(positionOS.y);
                float phase = dot(world.xz, float2(0.37, 0.61)) * scale + _Time.y * speed;
                float sway = (sin(phase) + sin(phase * 0.47 + 1.7) * 0.35) * strength * anchor * anchor;
                positionOS.xz += direction * sway;
                return positionOS;
            }

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                float3 positionOS = ApplyWind(input.positionOS.xyz);
                VertexPositionInputs positionInputs = GetVertexPositionInputs(positionOS);
                output.positionCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }

            half4 Frag(Varyings input, FRONT_FACE_TYPE face : FRONT_FACE_SEMANTIC) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                half4 sample = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;
                clip(sample.a - _Cutoff);

                half3 normalWS = normalize(input.normalWS);
                normalWS *= IS_FRONT_VFACE(face, 1.0h, -1.0h);
                Light mainLight = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                half diffuse = saturate(dot(normalWS, mainLight.direction));
                half backLight = saturate(dot(-normalWS, mainLight.direction)) * 0.35h;
                half3 ambient = SampleSH(normalWS);
                half3 lighting = ambient + mainLight.color * (diffuse + backLight) * mainLight.shadowAttenuation;
                sample.rgb *= max(lighting, 0.18h);
                return sample;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ColorMask 0

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex DepthVert
            #pragma fragment DepthFrag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half _Cutoff;
                half _Smoothness;
                float _WindStrength;
                float _WindSpeed;
                float _WindScale;
            CBUFFER_END
            float4 _RiceGlobalWindDirection;
            float4 _RiceGlobalWindParameters;

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };

            Varyings DepthVert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                float3 positionOS = input.positionOS.xyz;
                float3 world = TransformObjectToWorld(positionOS);
                float useGlobal = step(0.5, _RiceGlobalWindParameters.w);
                float strength = lerp(_WindStrength, _RiceGlobalWindParameters.x, useGlobal);
                float speed = lerp(_WindSpeed, _RiceGlobalWindParameters.y, useGlobal);
                float scale = lerp(_WindScale, _RiceGlobalWindParameters.z, useGlobal);
                float2 direction = normalize(lerp(float2(1.0, 0.35), _RiceGlobalWindDirection.xy, useGlobal) + 0.0001);
                float phase = dot(world.xz, float2(0.37, 0.61)) * scale + _Time.y * speed;
                float sway = (sin(phase) + sin(phase * 0.47 + 1.7) * 0.35) * strength * saturate(positionOS.y) * saturate(positionOS.y);
                positionOS.xz += direction * sway;
                output.positionCS = TransformObjectToHClip(positionOS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }

            half4 DepthFrag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                half alpha = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).a * _BaseColor.a;
                clip(alpha - _Cutoff);
                return 0;
            }
            ENDHLSL
        }
    }
}
