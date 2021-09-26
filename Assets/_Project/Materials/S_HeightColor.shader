// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "Unlit/S_HeightColor"
{
    Properties
    {
        _MainTexTop ("top texture", 2D) = "white" {}
        _MainTexBottom("bottom texture", 2D) = "white" {}
        
        //_Mask("Mask", 2D) = "white" {}
        _BottomColor("bottom color", Color) = (1,1,1,1)
        _TopColor("top color", Color) = (1,1,1,1)

        _MinHeight("min height", float) = 0
        _MaxHeight("max height", float) = 1

        _StencilMask("Stencil Mask", Range(0, 255)) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        ZTest Always //always draw regardless of depth test

        Stencil
        {
            Ref[_StencilMask]
            Comp GEqual
            Pass Replace
            ZFail Zero
        //ZFailFront Replace
        }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"
            
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
                float4 worldPos : TEXCOORD1;
            };

            sampler2D _MainTexTop;
            float4 _MainTexTop_ST;

            sampler2D _MainTexBottom;
            float4 _MainTexBottom_ST;


            fixed4 _BottomColor;
            fixed4 _TopColor;

            float _MinHeight;
            float _MaxHeight;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTexTop);
                UNITY_TRANSFER_FOG(o,o.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float h = (_MaxHeight - i.worldPos.y) / (_MaxHeight - _MinHeight);
                fixed4 col;
                fixed4 otherCol;

                col = tex2D(_MainTexTop, i.uv);
                otherCol = tex2D(_MainTexBottom, i.uv);

                col = lerp(col.rgba, otherCol.rgba, h);

                fixed mask = col.x;

                h *= mask;

                fixed4 tintColor = lerp(_TopColor.rgba, _BottomColor.rgba, h);

                col *= tintColor;

                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}
