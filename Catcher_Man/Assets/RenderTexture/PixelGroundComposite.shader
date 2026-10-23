Shader "Custom/PixelGroundComposite"
{
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }
        Pass
        {
            Name "PixelGroundComposite"
            Cull Off
            ZWrite On
            ZTest LEqual
            Blend Off

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_PixelGroundColor);
            TEXTURE2D(_PixelGroundDepth);

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            struct FragmentOutput
            {
                half4 color : SV_Target;
                float depth : SV_Depth;
            };

            Varyings Vert(uint vertexID : SV_VertexID)
            {
                Varyings output;
                output.positionCS = GetFullScreenTriangleVertexPosition(vertexID);
                output.uv = GetFullScreenTriangleTexCoord(vertexID);
                return output;
            }

            FragmentOutput Frag(Varyings input)
            {
                float rawDepth = SAMPLE_TEXTURE2D_LOD(
                    _PixelGroundDepth, sampler_PointClamp, input.uv, 0).r;

                // Pixels without ground must leave scene color/depth untouched.
                #if UNITY_REVERSED_Z
                    clip(rawDepth - 0.000001);
                #else
                    clip(0.999999 - rawDepth);
                #endif

                FragmentOutput output;
                output.color = SAMPLE_TEXTURE2D_LOD(
                    _PixelGroundColor, sampler_PointClamp, input.uv, 0);
                output.color.a = 1;
                output.depth = rawDepth;
                return output;
            }
            ENDHLSL
        }
    }
}
