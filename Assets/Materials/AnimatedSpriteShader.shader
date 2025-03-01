Shader "Unlit/AnimatedSpriteShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        //_ImageTextureDimentions ("Image AND Texture Dimentions", Vector) = (140.,300.,384.,256.)
        /// Transfer data for rows and collums later
        _AnimationIndex ("Animation Index", float) = 0.
        _SpriteIndex ("Sprite Index", float) = 0.
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType"="Transparent" }
        LOD 100

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag


            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;

                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            //float4 _ImageTextureDimentions;
            float _AnimationIndex;
            float _SpriteIndex;
            

            /// Each texture will have a max of 8 animation
            cbuffer AnimationLenghtBuffer : register(b0) {
                float AnimationsLenghts[8];
                };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                ///test
                AnimationsLenghts[0] = 4;

                float animSpeed = 10;

                float EffectiveSpriteIndex = floor(fmod((_Time.y*animSpeed)+ _SpriteIndex, AnimationsLenghts[0]));

                float spriteSizeX = 1./AnimationsLenghts[0];
                float spriteCoordX = spriteSizeX*(EffectiveSpriteIndex);
                float2 textureUVs = float2(min(i.uv.x*spriteSizeX+spriteCoordX,1),i.uv.y);
                fixed4 col = tex2D(_MainTex, textureUVs);
                
                //return fixed4(col.a, col.a, col.a, 1.0);
                return col;
            }
            ENDCG
        }
    }
}
