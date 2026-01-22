
Shader "Effect/Distort Alpha Blend_1" {
Properties {
	[Enum(OFF,0,ON,1)] _ZWrite("Z Write", int) = 0  //OFF/ON
	[HDR]_TintColor ("Tint Color", Color) = (0.5,0.5,0.5,0.5)
	_MainTex ("Main Texture", 2D) = "white" {}
	_DistortTex ("Distort Texture (RG)", 2D) = "white" {}
	_Mask ("Mask ( R Channel )", 2D) = "white" {}
	_HeatTime  ("Heat Time", range (-1,1)) = 0
	_ForceX  ("Strength X", range (0,1)) = 0
	_ForceY  ("Strength Y", range (0,1)) = 0
	_Bright("Bright", range(1,5))=2
	_MainScrollUV_X("Main UV Scroll X", float)= 0
	_MainScrollUV_Y("Main UV Scroll Y", float)= 0
	_MaskScrollUV_X("Mask UV Scroll X", float)= 0
	_MaskScrollUV_Y("Mask UV Scroll Y", float)= 0
}

Category {
	Tags { "Queue"="Transparent" "RenderType"="Transparent" }
	Blend SrcAlpha OneMinusSrcAlpha
	Cull Off 
	Lighting Off 
	ZWrite [_ZWrite]
	Fog { Color (0,0,0,0) }
	BindChannels {
		Bind "Color", color
		Bind "Vertex", vertex
		Bind "TexCoord", texcoord
	}

	SubShader {
		Pass {
CGPROGRAM
#pragma vertex vert
#pragma fragment frag

#pragma fragmentoption ARB_precision_hint_fastest exclude_path:prepass nolightmap noforwardadd interpolateview
#pragma multi_compile_particles

#include "UnityCG.cginc"

struct appdata_t {
	float4 vertex : POSITION;
	fixed4 color : COLOR;
	float2 texcoord: TEXCOORD0;
};

struct v2f {
	float4 vertex : POSITION;
	fixed4 color : COLOR;
	float2 uvmain : TEXCOORD0;
	float2 uvnoise : TEXCOORD1;
	float2 uvmask : TEXCOORD2;
};

fixed4 _TintColor;
fixed _ForceX;
fixed _ForceY;
fixed _HeatTime;
float4 _MainTex_ST;
float4 _DistortTex_ST;
float4 _Mask_ST;
sampler2D _DistortTex;
sampler2D _MainTex;
sampler2D _Mask;
fixed _Bright;
float _MainScrollUV_X;
float _MainScrollUV_Y;
float _MaskScrollUV_X;
float _MaskScrollUV_Y;

v2f vert (appdata_t v)
{
	v2f o;
	o.vertex = UnityObjectToClipPos(v.vertex);
	o.color = v.color;
	o.uvmain = TRANSFORM_TEX( v.texcoord, _MainTex );
	o.uvnoise = TRANSFORM_TEX( v.texcoord, _DistortTex );
	o.uvmask = TRANSFORM_TEX( v.texcoord, _Mask );
	return o;
}

fixed4 frag( v2f i ) : COLOR
{
	//noise effect
	//i.uvnoise.x += _Time*_ScrollUV.z;
	//i.uvnoise.y += _Time*_ScrollUV.w;
	fixed4 offsetColor1 = tex2D(_DistortTex, i.uvnoise + _Time.xz*_HeatTime);
    fixed4 offsetColor2 = tex2D(_DistortTex, i.uvnoise + _Time.yx*_HeatTime);
	i.uvmain.x += ((offsetColor1.r + offsetColor2.r) - 1) * _ForceX;
	i.uvmain.y += ((offsetColor1.r + offsetColor2.r) - 1) * _ForceY;
	i.uvmain.x += _Time*_MainScrollUV_X;
	i.uvmain.y += _Time*_MainScrollUV_Y;
	
	fixed4 c=tex2D( _MainTex, i.uvmain);
	fixed2 pos;
	pos.x = i.uvmask.x + _Time*_MaskScrollUV_X;
	pos.y = i.uvmask.y + _Time*_MaskScrollUV_Y;
	c.a *= tex2D(_Mask, pos).r * 2.0f;
	fixed4 finalColor=_Bright * i.color * _TintColor * c;
	return finalColor;
}
ENDCG
		}
}
	// ------------------------------------------------------------------
	// Fallback for older cards and Unity non-Pro
	
	SubShader {
		Blend DstColor Zero
		Pass {
			Name "BASE"
			SetTexture [_MainTex] {	combine texture }
		}
	}
	
}
}
