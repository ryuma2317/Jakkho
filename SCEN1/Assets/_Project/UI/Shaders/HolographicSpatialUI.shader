Shader "CADT/VR/HolographicSpatialUI"
{
    Properties
    {
        [Header(Base Hologram Settings)]
        _MainColor ("Panel Tint Color", Color) = (0.0588, 0.0902, 0.1647, 0.55) // #0F172A (45% transparency -> 0.55 alpha)
        _BorderColor ("Glow Border Color", Color) = (0.0, 0.941, 1.0, 1.0)      // #00F0FF Glowing Cyan
        _BorderThickness ("Border Thickness", Range(0.001, 0.2)) = 0.035
        _BorderSoftness ("Border Softness", Range(0.001, 0.1)) = 0.015
        _GlowIntensity ("Glow Emission Multiplier", Range(1.0, 5.0)) = 2.2

        [Header(Holographic Scanlines & Grid)]
        _ScanlineDensity ("Scanline Density", Float) = 120.0
        _ScanlineSpeed ("Scanline Scroll Speed", Float) = 1.5
        _ScanlineAlpha ("Scanline Intensity", Range(0.0, 0.5)) = 0.08
        _GridDensity ("Grid Density", Float) = 24.0
        _GridAlpha ("Grid Line Intensity", Range(0.0, 0.5)) = 0.06

        [Header(Fresnel Edge Glow)]
        _FresnelPower ("Fresnel Rim Power", Range(0.5, 6.0)) = 2.5
        _FresnelIntensity ("Fresnel Rim Intensity", Range(0.0, 3.0)) = 1.4

        [Header(Rendering State)]
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull Mode", Float) = 0 // Double-sided
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src Blend", Float) = 5 // SrcAlpha
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend", Float) = 10 // OneMinusSrcAlpha
        [Enum(Off, 0, On, 1)] _ZWrite ("Z Write", Float) = 0
    }

    SubShader
    {
        Tags 
        { 
            "Queue"="Transparent+50" 
            "IgnoreProjector"="True" 
            "RenderType"="Transparent" 
            "PreviewType"="Plane"
            "RenderPipeline"="UniversalPipeline"
        }

        Cull [_Cull]
        Lighting Off
        ZWrite [_ZWrite]
        Blend [_SrcBlend] [_DstBlend]

        Pass
        {
            Name "ForwardLit"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                float3 worldNormal : TEXCOORD1;
                float3 viewDir : TEXCOORD3;
            };

            float4 _MainColor;
            float4 _BorderColor;
            float _BorderThickness;
            float _BorderSoftness;
            float _GlowIntensity;
            float _ScanlineDensity;
            float _ScanlineSpeed;
            float _ScanlineAlpha;
            float _GridDensity;
            float _GridAlpha;
            float _FresnelPower;
            float _FresnelIntensity;

            v2f vert (appdata_t v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                
                float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.viewDir = normalize(_WorldSpaceCameraPos - worldPos);
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                // 1. Calculate Rectangular Edge Border SDF
                float2 distFromEdge = min(i.uv, 1.0 - i.uv);
                float edgeDist = min(distFromEdge.x, distFromEdge.y);
                
                // Anti-aliased border glow factor
                float borderFactor = 1.0 - smoothstep(_BorderThickness, _BorderThickness + _BorderSoftness, edgeDist);

                // 2. Holographic Scanlines & Subtle Grid
                float scanline = sin((i.uv.y + _Time.y * _ScanlineSpeed) * _ScanlineDensity) * 0.5 + 0.5;
                float scanlineEffect = scanline * _ScanlineAlpha;

                float2 gridUV = frac(i.uv * _GridDensity);
                float gridLine = (gridUV.x < 0.05 || gridUV.y < 0.05) ? _GridAlpha : 0.0;

                // 3. Fresnel Rim Glow for VR immersion
                float NdotV = max(0.0, dot(normalize(i.worldNormal), normalize(i.viewDir)));
                float fresnel = pow(1.0 - NdotV, _FresnelPower) * _FresnelIntensity;

                // 4. Composite Base Panel Color with Border Glow and Hologram FX
                float4 baseCol = _MainColor * i.color;
                float4 borderCol = _BorderColor * _GlowIntensity;

                // Mix border glow
                float4 finalColor = lerp(baseCol, borderCol, borderFactor);

                // Add scanlines, grid, and Fresnel rim
                finalColor.rgb += _BorderColor.rgb * (scanlineEffect + gridLine + fresnel * 0.3);
                finalColor.a = saturate(finalColor.a + (borderFactor * 0.45) + fresnel * 0.2 + scanlineEffect);

                return finalColor;
            }
            ENDHLSL
        }
    }
    FallBack "UI/Default"
}
