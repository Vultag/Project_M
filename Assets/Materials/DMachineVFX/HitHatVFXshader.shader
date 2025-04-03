Shader "Unlit/HitHatVFXshader"
{
    Properties
    {
        /// Remplace with buffer array if playback ?
        _HitHatPressTime ("HitHat pressTime", float) = 0
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
            
            float _HitHatPressTime;

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            float Noise(float2 uv) {
                return frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453);
            }
            // Hash function for smooth randomness
            float Hash(float2 p) {
                return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453);
            }
            // Smooth interpolated noise that evolves uniformly over time
            float SmoothNoise(float x) {
                float2 p = float2(x, _Time.x*300); // Variation speed
                float2 i = floor(p);  // Integer part
                float2 f = frac(p);   // Fractional part

                // Four noise values for bilinear interpolation
                float v00 = Hash(i);
                float v10 = Hash(i + float2(1.0, 0.0));
                float v01 = Hash(i + float2(0.0, 1.0));
                float v11 = Hash(i + float2(1.0, 1.0));

                // Smooth interpolation factors
                float2 u = f * f * (3.0 - 2.0 * f);

                // Bilinear interpolation
                return lerp(lerp(v00, v10, u.x), lerp(v01, v11, u.x), u.y);
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
                
                /// could theoreticly OOB infinity<- ?
                float waveAlpha = 0.25+ _HitHatPressTime- _Time.y;

                // Center the UVs around (0,0)
                float2 centeredUV = uv * 2.0 - 1.0; // Shifts (0,1) UVs to (-1,1)
                float2 PolUvs;
                // Convert to polar coordinates
                PolUvs.x = atan2(centeredUV.y, centeredUV.x)/PI +1; // Angle in radians
                
                PolUvs.y = length(centeredUV); // Radius

                float lineSmoothness = 10;
                float lineSize = 1/ 0.01;

                float Line = max(0,(1-abs((PolUvs.y+SmoothNoise(PolUvs.x*500)*0.02)*lineSize-lineSize*0.5))*lineSmoothness-lineSmoothness*0.5);

                return fixed4(1,1,1,max(0,Line*waveAlpha*4));
            }
            ENDCG
        }
    }
}
