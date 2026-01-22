Shader "Custom/UI/TextBold"
{
    Properties
    {
        _MainTex ("Font Texture", 2D) = "white" {}
        _Color ("Text Color", Color) = (1,1,1,1)
        _Thickness ("Bold Thickness", Range(0, 5)) = 1.5
        
        // UI相关属性
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }
    
    SubShader
    {
        Tags 
        { 
            "Queue"="Transparent" 
            "IgnoreProjector"="True" 
            "RenderType"="Transparent" 
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }
        
        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp] 
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }
        
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]
        
        Pass
        {
            Name "Default"
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            
            #pragma multi_compile _ UNITY_UI_CLIP_RECT
            #pragma multi_compile _ UNITY_UI_ALPHACLIP
            
            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            
            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord  : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            
            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _MainTex_TexelSize;
            fixed4 _Color;
            float4 _ClipRect;
            float _Thickness;
            
            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
                OUT.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                
                // 确保颜色正确传递
                OUT.color = v.color * _Color;
                
                return OUT;
            }
            
            fixed4 frag(v2f IN) : SV_Target
            {
                // 计算采样偏移
                float2 texelSize = _MainTex_TexelSize.xy;
                float thickness = _Thickness;
                
                // 获取中心点的alpha值
                float centerAlpha = tex2D(_MainTex, IN.texcoord).a;
                
                // 8方向采样获取最大alpha值（实现加粗效果）
                float maxAlpha = centerAlpha;
                
                // 正交方向
                maxAlpha = max(maxAlpha, tex2D(_MainTex, IN.texcoord + float2(texelSize.x * thickness, 0)).a);
                maxAlpha = max(maxAlpha, tex2D(_MainTex, IN.texcoord + float2(-texelSize.x * thickness, 0)).a);
                maxAlpha = max(maxAlpha, tex2D(_MainTex, IN.texcoord + float2(0, texelSize.y * thickness)).a);
                maxAlpha = max(maxAlpha, tex2D(_MainTex, IN.texcoord + float2(0, -texelSize.y * thickness)).a);
                
                // 对角方向
                maxAlpha = max(maxAlpha, tex2D(_MainTex, IN.texcoord + float2(texelSize.x * thickness, texelSize.y * thickness)).a);
                maxAlpha = max(maxAlpha, tex2D(_MainTex, IN.texcoord + float2(-texelSize.x * thickness, texelSize.y * thickness)).a);
                maxAlpha = max(maxAlpha, tex2D(_MainTex, IN.texcoord + float2(texelSize.x * thickness, -texelSize.y * thickness)).a);
                maxAlpha = max(maxAlpha, tex2D(_MainTex, IN.texcoord + float2(-texelSize.x * thickness, -texelSize.y * thickness)).a);
                
                // 使用顶点颜色（包含Text组件的颜色）
                fixed4 color = IN.color;
                
                // 应用alpha值
                color.a *= maxAlpha;
                
                // UI裁剪
                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif
                
                #ifdef UNITY_UI_ALPHACLIP
                clip (color.a - 0.001);
                #endif
                
                return color;
            }
            ENDCG
        }
    }
}
