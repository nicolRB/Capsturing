Shader "Custom/WizardEyes"
{
    Properties
    {
        [NoScaleOffset] _LashL("Lash L", 2DArray) = "" {}
        [NoScaleOffset] _LashR("Lash R", 2DArray) = "" {}
        [NoScaleOffset] _ScleraL("Sclera L", 2DArray) = "" {}
        [NoScaleOffset] _ScleraR("Sclera R", 2DArray) = "" {}
        [NoScaleOffset] _IrisL("Iris L", 2DArray) = "" {}
        [NoScaleOffset] _IrisR("Iris R", 2DArray) = "" {}

        _LashIndexL("Lash idx L", Float) = 0
        _LashIndexR("Lash idx R", Float) = 0
        _ScleraIndexL("Sclera idx L", Float) = 0
        _ScleraIndexR("Sclera idx R", Float) = 0
        _IrisIndexL("Iris idx L", Float) = 0
        _IrisIndexR("Iris idx R", Float) = 0

        _IrisCenterL("Iris center L", Vector) = (0.78, 0.5, 0, 0)
        _IrisCenterR("Iris center R", Vector) = (0.22, 0.5, 0, 0)
        _IrisScaleL("Iris scale L", Vector) = (1, 1, 0, 0)
        _IrisScaleR("Iris scale R", Vector) = (1, 1, 0, 0)
        _IrisOffsetL("Iris offset L", Vector) = (0, 0, 0, 0)
        _IrisOffsetR("Iris offset R", Vector) = (0, 0, 0, 0)
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.5
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D_ARRAY(_LashL);   TEXTURE2D_ARRAY(_LashR);
            TEXTURE2D_ARRAY(_ScleraL); TEXTURE2D_ARRAY(_ScleraR);
            TEXTURE2D_ARRAY(_IrisL);   TEXTURE2D_ARRAY(_IrisR);
            SAMPLER(sampler_linear_clamp);

            CBUFFER_START(UnityPerMaterial)
                float _LashIndexL, _LashIndexR, _ScleraIndexL, _ScleraIndexR, _IrisIndexL, _IrisIndexR;
                float4 _IrisCenterL, _IrisCenterR, _IrisScaleL, _IrisScaleR, _IrisOffsetL, _IrisOffsetR;
            CBUFFER_END

            struct Attributes { float4 posOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings   { float4 posCS : SV_POSITION; float2 uv : TEXCOORD0; };

            Varyings vert(Attributes i)
            {
                Varyings o;
                o.posCS = TransformObjectToHClip(i.posOS.xyz);
                o.uv = i.uv;
                return o;
            }

            half4 SampleClip(TEXTURE2D_ARRAY_PARAM(tex, samp), float2 uv, float idx)
            {
                half4 c = SAMPLE_TEXTURE2D_ARRAY(tex, samp, uv, round(idx));
                return (all(uv >= 0) && all(uv <= 1)) ? c : half4(0, 0, 0, 0);
            }

            float2 IrisUV(float2 uv, float2 center, float2 scale, float2 offset)
            {
                return (uv - center) / max(scale, 0.001) + center + offset;
            }

            half4 frag(Varyings i) : SV_Target
            {
                half4 lash   = SampleClip(TEXTURE2D_ARRAY_ARGS(_LashL, sampler_linear_clamp), i.uv, _LashIndexL)
                             + SampleClip(TEXTURE2D_ARRAY_ARGS(_LashR, sampler_linear_clamp), i.uv, _LashIndexR);
                half4 sclera = SampleClip(TEXTURE2D_ARRAY_ARGS(_ScleraL, sampler_linear_clamp), i.uv, _ScleraIndexL)
                             + SampleClip(TEXTURE2D_ARRAY_ARGS(_ScleraR, sampler_linear_clamp), i.uv, _ScleraIndexR);
                half4 iris   = SampleClip(TEXTURE2D_ARRAY_ARGS(_IrisL, sampler_linear_clamp),
                                          IrisUV(i.uv, _IrisCenterL.xy, _IrisScaleL.xy, _IrisOffsetL.xy), 
                                          _IrisIndexL)
                             + SampleClip(TEXTURE2D_ARRAY_ARGS(_IrisR, sampler_linear_clamp),
                                          IrisUV(i.uv, _IrisCenterR.xy, _IrisScaleR.xy, _IrisOffsetR.xy), 
                                          _IrisIndexR);
                lash = saturate(lash); sclera = saturate(sclera); iris = saturate(iris);

                // íris só aparece dentro do alpha da esclera
                half3 col = lerp(sclera.rgb, iris.rgb, iris.a * sclera.a);
                col = lerp(col, lash.rgb, lash.a);
                return half4(col, saturate(sclera.a + lash.a));
            }
            ENDHLSL
        }
    }
}