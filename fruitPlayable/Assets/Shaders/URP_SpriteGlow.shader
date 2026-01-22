Shader "Custom/URP_SpriteGlow"
{
    Properties
    {
        _MainTex("Sprite Texture", 2D) = "white" {}
        _Color("Tint Color", Color) = (1,1,1,1)
        
        [Header(Glow Settings)]
        _GlowColor("Glow Color", Color) = (1,1,0,1)
        _GlowLeft("Glow Left Expand", Range(0,1)) = 0.05
        _GlowRight("Glow Right Expand", Range(0,1)) = 0.05
        _GlowTop("Glow Top Expand", Range(0,1)) = 0.05
        _GlowBottom("Glow Bottom Expand", Range(0,1)) = 0.05
        _GlowThickness("Glow Thickness", Range(0,1)) = 0.5
        _GlowIntensity("Glow Intensity", Range(0,10)) = 2.0
        _GlowSoftness("Glow Softness", Range(0,1)) = 0.5
    }

    SubShader
    {
        Tags 
        {
            "RenderType"="Transparent"
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderPipeline"="UniversalPipeline"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            Name "SpriteGlow"
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Color;
                float4 _GlowColor;
                float _GlowLeft;
                float _GlowRight;
                float _GlowTop;
                float _GlowBottom;
                float _GlowThickness;
                float _GlowIntensity;
                float _GlowSoftness;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                
                // 直接使用原始顶点位置和UV,不扩展mesh
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.color = input.color;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // 采样原图
                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, TRANSFORM_TEX(input.uv, _MainTex));
                half4 spriteColor = texColor * _Color * input.color;
                
                // 计算到sprite边界的距离(在UV空间)
                float2 distToEdge = float2(0, 0);
                float glowDist = 0;
                
                // 计算水平方向距离
                if (input.uv.x < 0.5)
                {
                    distToEdge.x = input.uv.x; // 到左边界距离
                    if (distToEdge.x < _GlowLeft)
                        glowDist = max(glowDist, (_GlowLeft - distToEdge.x) / _GlowLeft);
                }
                else
                {
                    distToEdge.x = 1.0 - input.uv.x; // 到右边界距离
                    if (distToEdge.x < _GlowRight)
                        glowDist = max(glowDist, (_GlowRight - distToEdge.x) / _GlowRight);
                }
                
                // 计算垂直方向距离
                if (input.uv.y < 0.5)
                {
                    distToEdge.y = input.uv.y; // 到下边界距离
                    if (distToEdge.y < _GlowBottom)
                        glowDist = max(glowDist, (_GlowBottom - distToEdge.y) / _GlowBottom);
                }
                else
                {
                    distToEdge.y = 1.0 - input.uv.y; // 到上边界距离
                    if (distToEdge.y < _GlowTop)
                        glowDist = max(glowDist, (_GlowTop - distToEdge.y) / _GlowTop);
                }
                
                // 计算发光效果
                if (glowDist > 0)
                {
                    float glowThreshold = 1.0 - _GlowThickness;
                    float glowMask = smoothstep(glowThreshold, 1.0, glowDist);
                    glowMask = pow(glowMask, 1.0 / max(_GlowSoftness, 0.01));
                    
                    half4 glowColor = _GlowColor * glowMask * _GlowIntensity;
                    glowColor.a *= glowMask;
                    
                    // 混合原图和发光
                    return spriteColor + glowColor;
                }
                
                return spriteColor;
            }
            ENDHLSL
        }
    }
    
    FallBack "Sprites/Default"
}