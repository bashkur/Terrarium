Shader "Hidden/Griffin/RampMaker"
{
    Properties
    {
		_HeightMap ("Height Map", 2D) = "black" {}
		_Falloff ("Falloff", 2D) = "white" {}
		_FalloffNoise ("Falloff Noise", 2D) = "white" {}
		_LowerHeight ("Lower Height", Int) = 1
		_RaiseHeight ("Raise Height", Int) = 1
		_AdditionalMeshResolution ("Additional Mesh Resolution", Float) = 0
		_TerrainMask ("TerrainMask", 2D) = "black"{}
		_StepCount ("Step Count", Int) = 1000
    }

	CGINCLUDE
	#pragma multi_compile _ FALLOFF
    #include "UnityCG.cginc"
	#include "SplineToolCommon.cginc"

	struct appdata
    {
		float4 vertex : POSITION;
		float2 uv : TEXCOORD0;
		float4 worldPos : TEXCOORD1;
		float3 vertexColor : COLOR;
    };

    struct v2f
    {
		float2 uv : TEXCOORD0;
		float4 vertex : SV_POSITION;
		float4 localPos : TEXCOORD1;
		float3 vertexColor : COLOR;
		float4 worldPos : TEXCOORD2;
    };

	sampler2D _HeightMap;
	sampler2D _Falloff;
	sampler2D _FalloffNoise;
	float4 _FalloffNoise_ST;
	int _LowerHeight;
	int _RaiseHeight;
	float _AdditionalMeshResolution;
	sampler2D _TerrainMask;
	int _StepCount;

	float stepValue(float v, int stepCount)
	{
		float step = 1.0 / stepCount;
		return v - v % step;
	}

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

	fixed4 fragRamp (v2f i) : SV_Target
    {
		float4 heightMapColor = tex2D(_HeightMap, i.uv);
		float currentHeight = GriffinDecodeFloatRG(heightMapColor.rg);
		float splineHeight = clamp(i.localPos.z,0,1);
		float delta = splineHeight - currentHeight;
		float targetHeight = currentHeight + (delta < 0)*_LowerHeight*delta + (delta >= 0)*_RaiseHeight*saturate(delta);

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
		float h = lerp(currentHeight, targetHeight, falloff);
		h = lerp(currentHeight, h, terrainMask);
		h = stepValue(h, _StepCount);
		h = max(0, min(0.999999, h));
		float2 encodedHeight = GriffinEncodeFloatRG(h);

		float addRes = lerp(0, _AdditionalMeshResolution, terrainMask);

		return saturate(float4(encodedHeight.rg, addRes, heightMapColor.a));
	}

	ENDCG

    SubShader
    {
        Tags { "RenderType"="Transparent" }
		Cull Off

        Pass
        {
			Name "Ramp"
			Blend One Zero
			BlendOp Add
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment fragRamp
            ENDCG
        }
    }
}
