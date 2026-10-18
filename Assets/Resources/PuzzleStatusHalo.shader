Shader "GameJam2026/Puzzle Status Halo"
{
    Properties { _Tint ("Light color", Color) = (1, 0.05, 0.01, 1) }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend SrcAlpha One
        ZWrite Off
        Cull Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _Tint;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                float radius = length(input.uv * 2 - 1);
                float falloff = pow(saturate(1 - radius), 2.5);
                // A soft colored halo and a small pale hotspot, with no hard outline.
                half3 color = lerp(_Tint.rgb, half3(1, 1, 1), .3 * pow(saturate(1 - radius), 6));
                return half4(color, falloff * .85);
            }
            ENDHLSL
        }
    }
}
