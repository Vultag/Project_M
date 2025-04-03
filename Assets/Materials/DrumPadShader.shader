Shader "Unlit/DrumPadShader"
{
    Properties
    {
        _MouseInfo ("Mouse radianNormalized_Sign_pressInertia", Vector) = (0, 0, 0, 0)
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

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            float Box( in float2 uvs , in float2 dimentions, in float XedgeSmoothness,in float YedgeSmoothness)
            {
                /// snap to left for polar compute
                uvs.x += 0.5-dimentions.x*0.5;
                float wx = abs(uvs.x-0.5)*XedgeSmoothness-dimentions.x*0.5*XedgeSmoothness;
                float wy = abs(uvs.y-0.5)*YedgeSmoothness-dimentions.y*0.5*YedgeSmoothness;
                float g = max(wx,wy);

                float result = length(max(float2(wx,wy),0.0));

                return result;
            }


            float4 _MouseInfo;


            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = float2(i.uv.y,i.uv.x);
                
                float testNumberOfInstruments = 3;

                float InstrumentProportion = (2/testNumberOfInstruments);
                float stripeEdgeThickness = 0.005 * sign(testNumberOfInstruments-1);

                // Center the UVs around (0,0)
                float2 centeredUV = uv * 2.0 - 1.0; // Shifts (0,1) UVs to (-1,1)
                float2 PolUvs;
                // Convert to polar coordinates
                PolUvs.x = atan2(centeredUV.y, centeredUV.x)/PI +1; // Angle in radians
                
                // int side = sign(centeredUV.y);
                // PolUvs.x = abs(PolUvs.x*side -1*((-side+1)*0.5));

                PolUvs.y = length(centeredUV); // Radius
                //PolUvs.y = length(centeredUV) *sign(centeredUV.y); // Radius
                
                // normalized radian mouse dir
                float MouseX = _MouseInfo.x;
                float MouseXzoneIdx = floor(MouseX*testNumberOfInstruments);
                float UVXzoneIdx = floor(PolUvs.x*0.5*testNumberOfInstruments);
                // 1 in mouse zone, 0 on others
                float mouse = 1-ceil(abs((UVXzoneIdx/testNumberOfInstruments)-(MouseXzoneIdx/testNumberOfInstruments)));

                //min useless ?
                float result = max(0,1-Box(PolUvs,float2(2,0.23),500,500));
                
                PolUvs.x = PolUvs.x%InstrumentProportion;
                result -= max(0,1-Box(float2(PolUvs.x-stripeEdgeThickness,PolUvs.y),float2(InstrumentProportion-stripeEdgeThickness*2,0.2),300,500))*(1-_MouseInfo.z*mouse);

                

                return fixed4(1, 1, 1, result*(0.5+mouse*0.5)); 

            }
            ENDCG
        }
    }
}
