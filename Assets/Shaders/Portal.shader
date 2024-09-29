Shader "Custom/Portal"
{
    Properties
    {
        _InactiveColour ("Inactive Colour", Color) = (0, 0, 0, 0)
        _MainTex ("Portal Texture", 2D) = "white" {}
        _DisplayMask ("Display Mask", Float) = 1.0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalRenderPipeline" "RenderType"="Opaque" }
        LOD 100
        Cull Off

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // Declare per-material properties inside a constant buffer
            CBUFFER_START(UnityPerMaterial)
                float4 _InactiveColour;
                float  _DisplayMask;
            CBUFFER_END

            // Declare texture and sampler
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            struct Attributes
            {
                float3 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float4 screenPos   : TEXCOORD0;
            };

            Varyings vert (Attributes v)
            {
                Varyings o;
                o.positionHCS = TransformObjectToHClip(float4(v.positionOS, 1.0));
                o.screenPos = ComputeScreenPos(o.positionHCS);
                return o;
            }

            float4 frag (Varyings i) : SV_Target
            {
                // Compute UV coordinates
                float2 uv = i.screenPos.xy / i.screenPos.w;

                // **Apply a double inversion to uv.y**
                uv.y = 1.0 - (1.0 - uv.y);

                // Sample the portal texture
                float4 portalCol = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);

                // Blend between inactive color and portal texture
                return lerp(_InactiveColour, portalCol, _DisplayMask);
            }
            ENDHLSL
        }
    }
    Fallback Off
}