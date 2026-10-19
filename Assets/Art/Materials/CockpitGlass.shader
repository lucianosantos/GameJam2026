Shader "Ship/Cockpit Glass"
{
    Properties
    {
        _BaseColor ("Glass tint", Color) = (0.09, 0.16, 0.18, 0.018)
        _EdgeColor ("Grazing reflection", Color) = (0.32, 0.48, 0.54, 0.18)
        _Smoothness ("Smoothness", Range(0,1)) = 0.94
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _EdgeColor;
                half _Smoothness;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; half3 normalWS : TEXCOORD1; };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                half3 view = normalize(GetWorldSpaceViewDir(input.positionWS));
                half3 normal = normalize(input.normalWS);
                normal *= dot(normal, view) < 0 ? -1 : 1;
                half fresnel = pow(1 - saturate(dot(normal, view)), 5);
                Light light = GetMainLight();
                half3 halfway = normalize(light.direction + view);
                half glint = pow(saturate(dot(normal, halfway)), lerp(32, 256, _Smoothness));
                half3 reflection = GlossyEnvironmentReflection(reflect(-view, normal), input.positionWS, 1 - _Smoothness, 1);
                half3 color = lerp(_BaseColor.rgb, _EdgeColor.rgb + reflection, fresnel);
                color += light.color * glint * 0.7;
                return half4(color, saturate(_BaseColor.a + fresnel * _EdgeColor.a + glint * 0.2));
            }
            ENDHLSL
        }
    }
}
