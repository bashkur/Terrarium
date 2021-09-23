Shader "Hidden/Griffin/PathPainter"
{
    Properties
    {
		_Color ("Color", Color) = (1,1,1,1)
		_MainTex ("Main Tex", 2D) = "black" {}
		_Falloff ("Falloff", 2D) = "white" {}
		_FalloffNoise ("Falloff Noise", 2D) = "white" {}
		_Metallic ("Metallic", Float) = 0
		_Smoothness ("Smoothness", Float) = 0
		_ChannelIndex ("ChannelIndex", Int) = 0
		_TerrainMask("TerrainMask", 2D) = "black"{}
    }

	CGINCLUDE
    #include "UnityCG.cginc"
	#pragma multi_compile _ FALLOFF

	struct appdata
    {
		float4 vertex : POSITION;
		float2 uv : TEXCOORD0;
		float3 vertexColor : COLOR;
		float4 worldPos : TEXCOORD1;
    };

    struct v2f
    {
		float2 uv : TEXCOORD0;
		float4 vertex : SV_POSITION;
		float4 localPos : TEXCOORD1;
		float3 vertexColor : COLOR;
		float4 worldPos : TEXCOORD2;
    };

	float4 _Color;
	sampler2D _MainTex;
	sampler2D _Falloff;
	sampler2D _FalloffNoise;
	float4 _FalloffNoise_ST;
	float _Metallic;
	float _Smoothness;
	int _ChannelIndex;
	sampler2D _TerrainMask;

	v2f vert (appdata v)
    {
		v2f o;
		o.vertex = UnityObjectToClipPos(v.vertex);
		o.uv = v.uv;
		o.localPos = v.vertex;
		o.worldPos = v.worldPos;
		o.vertexColor = v.vertexColor;
		return o;
    }

	fixed4 fragAlbedo (v2f i) : SV_Target
    {
		float4 currentColor = tex2D(_MainTex, i.uv);

		#if FALLOFF
		float2 uv1 = float2(i.worldPos.x*_FalloffNoise_ST.x, i.worldPos.y*_FalloffNoise_ST.y);
		float4 falloffNoiseColor = tex2D(_FalloffNoise, uv1);
		float falloffNoiseValue = falloffNoiseColor.r;

		float f = i.vertexColor.r*falloffNoiseValue;
		float t = clamp(f, 0, 1);
		float falloff = tex2D(_Falloff, float2(t,0.5)).r;
		#else
		float falloff = 1;
		#endif

		float terrainMask = 1 - tex2D(_TerrainMask, i.localPos).r;
		float4 overlayColor = float4(_Color.rgb, _Color.a*falloff);
		overlayColor = lerp(currentColor, overlayColor, terrainMask);
		return overlayColor;
	}

	fixed4 fragMetallicSmoothness (v2f i) : SV_Target
    {
		float4 currentColor = tex2D(_MainTex, i.uv);

		#if FALLOFF
		float2 uv1 = float2(i.worldPos.x*_FalloffNoise_ST.x, i.worldPos.y*_FalloffNoise_ST.y);
		float4 falloffNoiseColor = tex2D(_FalloffNoise, uv1);
		float falloffNoiseValue = falloffNoiseColor.r;

		float f = i.vertexColor.r*falloffNoiseValue;
		float t = clamp(f, 0, 1);
		float falloff = tex2D(_Falloff, float2(t,0.5)).r;
		#else
		float falloff = 1;
		#endif

		float terrainMask = 1 - tex2D(_TerrainMask, i.localPos).r;
		float4 overlayColor = float4(_Metallic, 0, 0, _Smoothness);
		overlayColor = lerp(currentColor, overlayColor, terrainMask);
		float4 desColor = lerp(currentColor, overlayColor, falloff);

		return desColor;
	}

	fixed4 fragSplat (v2f i) : SV_Target
    {
		float4 currentColor = tex2D(_MainTex, i.uv);

		#if FALLOFF
		float2 uv1 = float2(i.worldPos.x*_FalloffNoise_ST.x, i.worldPos.y*_FalloffNoise_ST.y);
		float4 falloffNoiseColor = tex2D(_FalloffNoise, uv1);
		float falloffNoiseValue = falloffNoiseColor.r;

		float f = i.vertexColor.r*falloffNoiseValue;
		float t = clamp(f, 0, 1);
		float falloff = tex2D(_Falloff, float2(t,0.5)).r;
		#else
		float falloff = 1;
		#endif

		float terrainMask = 1 - tex2D(_TerrainMask, i.localPos).r;
		float4 channel = float4(_ChannelIndex==0, _ChannelIndex==1, _ChannelIndex==2, _ChannelIndex==3);
		float4 desColor = channel;
		float4 result = lerp(currentColor, desColor, falloff);
		result = lerp(currentColor, result, terrainMask);
		return result;
	}

	ENDCG

    SubShader
    {
        Tags { "RenderType"="Transparent" }
		Cull Off

        Pass
        {
			Name "Albedo"
			Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
			BlendOp Add
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment fragAlbedo
            ENDCG
        }

		Pass
        {
			Name "Metallic Smoothness"
			Blend One Zero
			BlendOp Add
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment fragMetallicSmoothness
            ENDCG
        }

		Pass
        {
			Name "Splat"
			Blend One Zero
			BlendOp Add
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment fragSplat
            ENDCG
        }
    }
}
