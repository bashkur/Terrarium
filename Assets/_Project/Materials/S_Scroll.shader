Shader "Unlit/M_Scroll"
{
    Properties
    {
        _MainTex ("Cloud Noise", 2D) = "white" {}
        _MainTex2("Cloud Noise", 2D) = "white" {}
        _VingeteTex("Mask", 2D) = "white" {}
        _UVScrollSpeedX("X Scroll Speed",Range(0,1)) = 0.1
        _UVScrollSpeedY("Y Scroll Speed",Range(0,1)) = 0.1

        //_Emission("Emission", float) = 0
        [HDR] _EmissionColor("Color", Color) = (0,0,0)

        _LineThiness("Higher = ThinnerLine",float) = 5
    }
    SubShader
    {
        //Tags { "RenderType"="Opaque" }
        //LOD 100
        
        Tags {"Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent"}
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        //Cull front
        LOD 100


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
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            sampler2D _MainTex2;
            float4 _MainTex2_ST;

            sampler2D _VingeteTex;
            float4 _VingeteTex_ST;

            float _UVScrollSpeedX;
            float _UVScrollSpeedY;
            float _LineThiness;

            fixed4 _EmissionColor;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                v.uv = v.uv ;
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            float map(float value, float min1, float max1, float min2, float max2)
            {
                // Convert the current value to a percentage
                // 0% - min1, 100% - max1
                float perc = (value - min1) / (max1 - min1);

                // Do the same operation backwards with min2 and max2
                return perc * (max2 - min2) + min2;
            }

            float4 map4(float4 value, float min1, float max1, float min2, float max2)
            {
                //ignore alpha...
                value.x = map(value.x, min1, max1, min2, max2);
                value.y = map(value.y, min1, max1, min2, max2);
                value.z = map(value.z, min1, max1, min2, max2);
                return value;
            }

            float4 oneMinus(float4 col)
            {
                return float4(1, 1, 1, 1) - col;
            }

            float4 power(float4 col, int num)
            {
                while (num)
                {
                    num--;
                    col.xyz *= col.xyz;
                }
                return col;
            }

            float4 Difference(float4 cBase, float4 cBlend)
            {
                return (abs(cBase - cBlend));
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // sample the texture
                //fixed4 col = tex2D(_MainTex, i.uv);

                float2 uv_scrolling = i.uv + frac(_Time.y * float2(_UVScrollSpeedX, _UVScrollSpeedY));

                fixed4 col = Difference(tex2D(_MainTex, i.uv), tex2D(_MainTex2, uv_scrolling));

                fixed4 mask = tex2D(_VingeteTex, i.uv);
                
                col.xyz *= _LineThiness;
                //clamp(col, fixed4(0, 0, 0, 0), fixed4(1, 1, 1, 1));

                col = oneMinus(col);

                col *= mask;

                //col = oneMinus(col);

                col.a = (col.x * col.z * col.y > 0.5f);

                clamp(col, fixed4(0, 0, 0, 0), fixed4(1, 1, 1, 1));

                col.xyz *= _EmissionColor;

                //o.Emission = c.rgb * tex2D(_MainTex, IN.uv_MainTex).a * _EmissionColor;

                //col = map4(col, 0.05f, 1.0f, 0.0f, 1.0f);

                //col = power(col, 2);

                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}
