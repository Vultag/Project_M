Shader "Unlit/TextureDisplayShader"
{
	  Properties
    {
        
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            //Blend SrcAlpha OneMinusSrcAlpha
            //ZWrite Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag


            #include "UnityCG.cginc"


            struct VS_INPUT
            {
                float4 pos : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct PS_INPUT
            {
                float2 uv : TEXCOORD0;
                float4 pos : SV_POSITION;
                float4 worldPos : TEXCOORD1;
            };
            
            sampler2D _MainTex;
            float4 _MainTex_ST;
           
            PS_INPUT vert (VS_INPUT v)
            {
                PS_INPUT o;
                o.pos = UnityObjectToClipPos(v.pos);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.worldPos = mul(unity_ObjectToWorld, v.pos);

                return o;
            }

            fixed4 frag (PS_INPUT i) : SV_Target
            {
                return tex2D(_MainTex, i.uv);
            }
            ENDCG
        }
    }
}