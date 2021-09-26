Shader "Custom/S_StencilShader"
{
    Properties
    {
        _ColorTexture("Color Heightmap", 2D) = "white" {}
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0

        _StencilMask("Stencil Mask", Range(0, 255)) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue" = "Geometry"}
        LOD 200

        //Ztest always

        /*
        public enum ZTest
        {
            Always = 0,
            Less = 2,
            Equal = 3,
            LEqual = 4,
            GEqual = 5,
        }
        */

        //ZTest GEqual (portal stuff)
        ZTest Always //always draw regardless of depth test

        Stencil
        {
            Ref [_StencilMask]
            Comp GEqual
            Pass Replace
            ZFail Zero
            //ZFailFront Replace
        }

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        sampler2D _MainTex;
        sampler2D _ColorTexture;

        struct Input
        {
            float2 uv_MainTex;
            float3 localPos;
        };

        half _Glossiness;
        half _Metallic;

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

        #pragma surface surf Lambert vertex:vert

        void vert(inout appdata_full v, out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input, o);
            o.localPos = v.vertex.xyz;
        }

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Albedo comes from a texture tinted by color
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex);
            fixed4 color = tex2D(_ColorTexture, IN.uv_MainTex);

            c *= color;

            o.Albedo = c.rgb;
            // Metallic and smoothness come from slider variables
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = c.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
