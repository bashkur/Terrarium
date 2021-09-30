Shader "Custom/S_TesselationPhong"
{
    Properties
    {
            _NormalTex("Normals", 2D) = "white" {}
            _EdgeLength("Edge length", Range(2,50)) = 5
            _Phong("Phong Strengh", Range(0,1)) = 0.5
            _MainTex("Base (RGB)", 2D) = "white" {}
            _Color("Color", color) = (1,1,1,0)
            _Displacement("DisplacementAmount", float) = 0.5
            _SpecColor("Spec color", color) = (0.5,0.5,0.5,0.5)
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        LOD 300

        CGPROGRAM
        #pragma surface surf Lambert addshadow vertex:dispNone tessellate:tessEdge tessphong:_Phong nolightmap
        #include "Tessellation.cginc"

        struct appdata 
        {
            float4 vertex : POSITION;
            float3 normal : NORMAL;
            float2 texcoord : TEXCOORD0;
        };

        sampler2D _NormalTex;
        fixed4 _NormalTex_ST;
        fixed _Displacement;

        void dispNone(inout appdata v) 
        { 
            v.normal = tex2Dlod(_NormalTex, float4(v.texcoord.xy, 0, 0));
            v.vertex += float4((v.normal * _Displacement).xyz, 0);
        }

        float _Phong;
        float _EdgeLength;

        float4 tessEdge(appdata v0, appdata v1, appdata v2)
        {
            return UnityEdgeLengthBasedTess(v0.vertex, v1.vertex, v2.vertex, _EdgeLength);
        }

        struct Input {
            float2 uv_MainTex;
        };

        fixed4 _Color;
        sampler2D _MainTex;

        void surf(Input IN, inout SurfaceOutput o) {
            half4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
            o.Albedo = c.rgb;
            o.Alpha = c.a;
            o.Specular = 0.2;
        }

        ENDCG
    }
    FallBack "Diffuse"
}
