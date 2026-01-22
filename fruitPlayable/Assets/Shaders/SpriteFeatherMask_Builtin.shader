Shader "Custom/SpriteFeatherMask_Builtin"
{
    Properties
    {
        [PerRendererData]_MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        // 左右裁切位置（UV 0-1）
        _CutL ("Cut Left (0-1)", Range(0,1)) = 0.0
        _CutR ("Cut Right (0-1)", Range(0,1)) = 1.0

        // 羽化宽度（UV 0-0.5），越大边缘越柔
        _Feather ("Feather Width (UV)", Range(0,0.5)) = 0.05

        // 选择是否在极小 alpha 时 clip，提高过裁区域的剔除（0 关，1 开）
        _HardDiscard ("Hard Discard Outside (0/1)", Range(0,1)) = 0

        // 渲染排序相关（与默认 Sprites 相同）
        [MaterialToggle] PixelSnap ("Pixel snap", Float) = 0
        _RendererColor ("RendererColor", Color) = (1,1,1,1)
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ PIXELSNAP_ON
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA

            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;

            fixed4 _Color;
            fixed4 _RendererColor;

            float _CutL;
            float _CutR;
            float _Feather;
            float _HardDiscard;

            v2f vert (appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = v.texcoord;
                o.color = v.color * _Color * _RendererColor;

                #ifdef PIXELSNAP_ON
                o.vertex = UnityPixelSnap (o.vertex);
                #endif

                return o;
            }

            // 安全的 smoothstep（避免 Feather 为 0 导致除零/NaN）
            inline float safe_smoothstep(float edge0, float edge1, float x)
            {
                // 如果边界相等，退化为 step 行为
                float denom = max(1e-6, edge1 - edge0);
                float t = saturate((x - edge0) / denom);
                return t * t * (3.0 - 2.0 * t);
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 c = tex2D(_MainTex, i.texcoord) * i.color;

                // 计算左右羽化遮罩（基于 UV.x）
                // 左侧：从 _CutL 到 _CutL+_Feather 由 0 -> 1
                float leftMask  = safe_smoothstep(_CutL, _CutL + max(_Feather, 1e-6), i.texcoord.x);

                // 右侧：从 _CutR-_Feather 到 _CutR 由 1 -> 0（等价反向 smoothstep）
                // 先计算右边进入区域的增长，再取 1 - growth
                float rightGrowth = safe_smoothstep(_CutR - max(_Feather, 1e-6), _CutR, i.texcoord.x);
                float rightMask = 1.0 - rightGrowth;

                // 组合遮罩：取两者最小值，表示同时满足左右限制
                float baseMask = min(leftMask, rightMask);

                // 最终透明度
                c.a *= baseMask;

                // 可选：在极低 alpha 时丢弃像素（使被裁范围不写入颜色/深度）
                // WebGL 中 discard 会影响早期深度/alpha 剪裁，可按需开启
                if (_HardDiscard > 0.5 && c.a < 0.001)
                {
                    discard;
                }

                // 颜色预乘不需要，标准 alpha 混合
                return c;
            }
            ENDCG
        }
    }

    FallBack "Transparent/Diffuse"
}