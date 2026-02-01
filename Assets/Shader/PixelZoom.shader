Shader "PixelZoom"
{

    Properties
    {
        _MainTex("Texture",2D) = "white" {}
        _PixelSize("Pixel Size",Range(32,2048))=32
    }
    SubShader
    {
      Tags{"RenderType"="Transparent" "Queue"="Transparent"}
      LOD 100

      Pass
      {
        Blend SrcAlpha OneMinusSrcAlpha

        CGPROGRAM
        #pragma vertex vert
        #pragma fragment frag

        #include "UnityCG.cginc"

        sampler2D _MainTex;
        float4 _MainTex_ST;
        float _PixelSize;

        struct appdata
        {
          float4 vertex : POSITION;
          float2 uv : TEXCOORD0;
        };
        struct v2f
        {
          float4 pos : SV_POSITION;
          float2 uv : TEXCOORD0;
        };
        v2f vert (appdata v)
        {
          v2f o;
          o.pos = UnityObjectToClipPos(v.vertex);
          o.uv = TRANSFORM_TEX(v.uv,_MainTex);
          return o;
        }
        fixed4 frag (v2f i) : SV_Target
        {
          float2 uv = i.uv;
          uv = floor(uv*_PixelSize)/_PixelSize;
          fixed4 col = tex2D(_MainTex,uv);
          return col;
        }
        ENDCG
      }
    }

}