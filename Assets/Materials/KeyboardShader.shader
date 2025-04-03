Shader "Unlit/KeyboardShader"
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


            float4 _MouseInfo;
            float _ModeKeysArray[12];

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float Box( in float2 uvs , in float2 dimentions, in float XedgeSmoothness,in float YedgeSmoothness)
            {
                /// snap to left for polar compute
                //uvs.x += 0.5-dimentions.x;
                float wx = abs(uvs.x-0.5)*XedgeSmoothness-dimentions.x*0.5*XedgeSmoothness;
                float wy = abs(uvs.y-0.5)*YedgeSmoothness-dimentions.y*0.5*YedgeSmoothness;
                float g = max(wx,wy);

                float result = length(max(float2(wx,wy),0.0));

                return result;
            }

            // Define section widths (OctaveRadianWeights, summing to 1)
            static const float4 SectionWidths1 = float4(4.0 / 43.0, 3.0 / 43.0, 4.0 / 43.0, 3.0 / 43.0);
            static const float4 SectionWidths2 = float4(4.0 / 43.0, 4.0 / 43.0, 3.0 / 43.0, 4.0 / 43.0);
            static const float4 SectionWidths3 = float4(3.0 / 43.0, 4.0 / 43.0, 3.0 / 43.0, 4.0 / 43.0);

            // Precomputed cumulative section starts (prefix sum)
            static const float4 SectionStarts1 = float4(0.0, 4.0 / 43.0, 7.0 / 43.0, 11.0 / 43.0);
            static const float4 SectionStarts2 = float4(14.0 / 43.0, 18.0 / 43.0, 22.0 / 43.0, 25.0 / 43.0);
            static const float4 SectionStarts3 = float4(29.0 / 43.0, 32.0 / 43.0, 36.0 / 43.0, 39.0 / 43.0);

            static const float4 SectionEnds1 = float4(4.0 / 43.0, 7.0 / 43.0, 11.0 / 43.0, 14.0 / 43.0);
            static const float4 SectionEnds2 = float4(18.0 / 43.0, 22.0 / 43.0, 25.0 / 43.0, 29.0 / 43.0);
            static const float4 SectionEnds3 = float4(32.0 / 43.0, 36.0 / 43.0, 39.0 / 43.0, 43.0 / 43.0);

            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = float2(i.uv.y,i.uv.x);
                
                // int numberOfSections = 2;
                // float offsetFromCenter = 0.5    *0.5;

                // Center the UVs around (0,0)
                float2 centeredUV = uv * 2.0 - 1.0; // Shifts (0,1) UVs to (-1,1)
                float2 PolUvs;
                // Convert to polar coordinates
                PolUvs.x = atan2(centeredUV.y, centeredUV.x)/PI; // Angle in radians
                PolUvs.y = length(centeredUV) *sign(centeredUV.y); // Radius

                int side = sign(centeredUV.y);

                    float x = abs(PolUvs.x*side -1*((-side+1)*0.5));

                    // Compute masks for each section group
                    float4 mask1 = step(SectionStarts1, x) * step(x, SectionEnds1);
                    float4 mask2 = step(SectionStarts2, x) * step(x, SectionEnds2);
                    float4 mask3 = step(SectionStarts3, x) * step(x, SectionEnds3);

                    // Compute normalized x within each section
                    float4 normX1 = (x - SectionStarts1) / SectionWidths1;
                    float4 normX2 = (x - SectionStarts2) / SectionWidths2;
                    float4 normX3 = (x - SectionStarts3) / SectionWidths3;

                    // Select the correct section using dot product
                    float firstPart  = dot(mask1, normX1);
                    float secondPart = dot(mask2, normX2);
                    float thirdPart  = dot(mask3, normX3);

                    float SectionnedUVs = firstPart + secondPart + thirdPart;
                   
                    int SectionIndex = mask1.x+mask1.y*2+mask1.z*3+mask1.w*4+mask2.x*5+mask2.y*6+mask2.z*7+mask2.w*8+mask3.x*9+mask3.y*10+mask3.z*11+mask3.w*12-1;
                    //int SectionIndex = mask1.x+mask1.y*2+mask1.z*3+mask1.w*4-1;
                    int ShouldAppear = _ModeKeysArray[SectionIndex];

                // normalized radian mouse dir
                float MouseX = abs(_MouseInfo.x+side*(-side*0.5+0.5));
                // Compute masks
                float4 mouseMask1 = step(SectionStarts1, MouseX) * step(MouseX, SectionEnds1);
                float4 mouseMask2 = step(SectionStarts2, MouseX) * step(MouseX, SectionEnds2);
                float4 mouseMask3 = step(SectionStarts3, MouseX) * step(MouseX, SectionEnds3);

                // Select the correct section using dot product
                float mousefirstPart  = dot(mask1, mouseMask1);
                float mousesecondPart = dot(mask2, mouseMask2);
                float mousethirdPart  = dot(mask3, mouseMask3);

                // 0 on passive , 1 on active sections
                float mouse = mousefirstPart + mousesecondPart + mousethirdPart;

                float StripesHeightRight = 0.03+ (0.04)*(mouse)*(_MouseInfo.y+1)*0.5;
                float StripesHeightLeft = 0.03+ (0.04)*(mouse)*(_MouseInfo.y-1)*(-0.5);
                
                // box stripes right
                float result = 1-min(1,Box(float2(SectionnedUVs,PolUvs.y),float2(0.95,StripesHeightRight),30,500));
                // box stripes left
                result += 1-min(1,Box(float2(SectionnedUVs,-PolUvs.y),float2(0.95,StripesHeightLeft),30,500));
                // mask boxes right
                result -= max(0,1-Box(float2(SectionnedUVs,PolUvs.y),float2(0.9,StripesHeightRight-0.01),50,500))*(1-mouse*_MouseInfo.y*(_MouseInfo.z));
                // mask boxes left
                result -= max(0,1-Box(float2(SectionnedUVs,-PolUvs.y),float2(0.9,StripesHeightLeft-0.01),50,500))*(1-mouse*(1-_MouseInfo.y)*(_MouseInfo.z));

                

                return fixed4(1, 1, 1, result* ShouldAppear); 
            }
            ENDCG
        }
    }
}
