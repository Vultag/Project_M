Shader "Unlit/SnareVFXshader"
{
    Properties
    {
        /// Remplace with buffer array if playback ?
        _SnareInfo ("Snare radianNormalized_pressTime", Vector) = (0, 0, 0, 0)
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

            #define PI 3.14159265359

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };
            
            float4 _SnareInfo;

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            float Box( in float2 uvs , in float2 dimentions, in float XedgeSmoothness,in float YedgeSmoothness)
            {
                /// snap to the middle
                uvs.x += dimentions.x*0.5;
                float wx = abs(uvs.x-0.5)*XedgeSmoothness-dimentions.x*0.5*XedgeSmoothness;
                float wy = abs(uvs.y-0.5)*YedgeSmoothness-dimentions.y*0.5*YedgeSmoothness;
                float g = max(wx,wy);

                float result = length(max(float2(wx,wy),0.0));

                return result;
            }
            float Circle(in float2 uvs, in float2 position, in float size,in float edgeSmoothness)
            {
                uvs += position;
                return (1-length((uvs-float2(0.5,0.5))/size))*edgeSmoothness-edgeSmoothness*0.5;
            }
            float2 RotateUV(float2 uv, float angle)
            {
                // Translate UVs to the pivot
                uv -= float2(0.5,0.5);

                // Compute sine and cosine of the angle
                float s = sin(angle);
                float c = cos(angle);

                // Apply rotation matrix
                float2 rotatedUV;
                rotatedUV.x = uv.x * c - uv.y * s;
                rotatedUV.y = uv.x * s + uv.y * c;

                // Translate UVs back
                return rotatedUV + float2(0.5,0.5);
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = float2(i.uv.x,i.uv.y);
                
                float baguetteLenght = 0.35;
                float fadeDuration = 1;

                float2 rotatedUV = RotateUV(uv,PI*_SnareInfo.x*2);
                
                //_SnareInfo.y =  _SnareInfo.y*0.5f;

                /// could theoreticly OOB infinity<- ?
                float baguetteAlpha = 1+ _SnareInfo.y- _Time.y;

                float result;

                result = max(0,1-Box(rotatedUV,float2(baguetteLenght-0.05,0.01),500,500));
                result += max(0,Circle(rotatedUV,float2(0.5-baguetteLenght*0.5,0.),0.05,20));
                
                return fixed4(1,1,1,max(0,result*baguetteAlpha));
            }
            ENDCG
        }
    }
}
