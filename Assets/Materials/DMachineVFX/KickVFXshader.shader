Shader "Unlit/KickVFXshader"
{
    Properties
    {
        /// Remplace with buffer array if playback ?
        _KickPressTime ("Kick pressTime", float) = 0
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
            
            float _KickPressTime;

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

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
                // Center the UVs around (0,0)
                float2 centeredUV = uv * 2.0 - 1.0; // Shifts (0,1) UVs to (-1,1)


                float circleSmoothness = 1;
                float circleEXPstart = 2;
                float circleHardWidth = 0.2;
                float circleEXPend = 5;
                float circleExpandSpeed = 20;
                float circleFadeSpeed = 3;
                float CircleAlpha = min(1,(_Time.y-_KickPressTime));
                float CircleExpantion = min(circleEXPend,(CircleAlpha*circleExpandSpeed)+circleEXPstart);

                float circle = 1-max(0,(abs(length(centeredUV)*10-CircleExpantion)*circleSmoothness)-circleHardWidth);

                return fixed4(1,1,1,max(0,circle)*(max(0,1-CircleAlpha*circleFadeSpeed)));
            }
            ENDCG
        }
    }
}
