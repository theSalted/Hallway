Shader "Custom/Portal"
{
    Properties
    {
        _InactiveColour ("Inactive Colour", Color) = (1, 1, 1, 1)
        _MainTex ("Portal Texture", 2D) = "white" {}
        _DisplayMask ("Display Mask", Range(0,1)) = 1.0
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
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            float4 _InactiveColour;
            float  _DisplayMask;

            struct Attributes
            {
                float3 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float4 screenPos   : TEXCOORD0;
            };

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                float4 positionCS = TransformObjectToHClip(float4(IN.positionOS, 1.0));
                OUT.positionHCS = positionCS;
                OUT.screenPos = ComputeScreenPos(positionCS);
                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                // Avoid division by zero
                float w = max(IN.screenPos.w, 0.0001);
                float2 uv = IN.screenPos.xy / w;

                // Adjust UV coordinates if necessary
                uv = uv * float2(0.5, 0.5) + float2(0.0, 0.0);

                float4 portalCol = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
                return lerp(_InactiveColour, portalCol, _DisplayMask);
            }
            ENDHLSL
        }
    }
    FallBack Off
}