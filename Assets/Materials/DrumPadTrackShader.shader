Shader "Unlit/DrumPadTrackShader"
{
    Properties
    {
        //_MainTex ("Texture", 2D) = "white" {}
        _ImageDimentions ("Image Dimentions", Vector) = (140.,300.,0.,0.)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            // cbuffer TrackGridBuffer : register(b0) {
            //     float2 TrackGridArray[66]; // 2D array of 6*10 + 6 of top side padding
            //     float3 TrackGridColorArray[66];            // 12 bytes + 4 bytes padding
            //     };
     

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

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }
            
            float4 _ImageDimentions;
            float mesureNumber;
            /// flattened 2D array of all possible pads
            /// packed into uint 192/32 = 6
            float FullPadChecks[6];

            int GetBitFromPadChecks(int index)
            {
                int floatIndex = index / 32;
                int bitIndex = index % 32;

                uint bits = asuint(FullPadChecks[floatIndex]);
                return int((bits >> bitIndex) & 1); // returns 0 or 1
            }
            static const float3 InstrumentColors[6] = {
                float3(1.0, 1.0, 1.0),  // Blue //fix
                float3(0.0, 1.0, 1.0),  // Cyan
                float3(1.0, 0.0, 1.0),  // Magenta
                float3(0.0, 1.0, 0.0),  // Green
                float3(1.0, 1.0, 0.0),  // Yellow
                float3(1.0, 0.0, 0.0),  // Red
            };


            fixed4 frag (v2f i) : SV_Target
            {

                float3 col = float3(0,0,0);

                float imageRatio = _ImageDimentions.x/_ImageDimentions.y;

                // 0 1 0
                float2 centeredUVs = float2(1-abs((i.uv.x-0.5)*2),1-abs((i.uv.y-0.5)*2));

                float VlinesMargin = 0.05;
                float HlinesMargin = 0.08;
                float RowNumber = 6;
                float CollumNumber = mesureNumber;
                
                float Vlimit = (sign(centeredUVs.y-HlinesMargin*1.75)*0.5+0.5);
                float Hlimit = (sign(centeredUVs.x-VlinesMargin*1.78)*0.5+0.5);

                
                float VerticalLineThickness = (((1/(0.09))*(1/imageRatio))/HlinesMargin)/CollumNumber;
                float HorizontalLineThickness = (((1/(0.16))*(imageRatio))/VlinesMargin)/RowNumber;

                //..0->1.. shrink uvs
                float VlinesUVx = abs(i.uv.x*(1+VlinesMargin*2)-VlinesMargin)%1;
                // 0->1 0->1 ... repeate motif
                float VbeatLinesUVx = (VlinesUVx*4*CollumNumber)%1;
                VlinesUVx = (VlinesUVx*CollumNumber)%1;
                // 0 1 0
                VlinesUVx = 1-abs((VlinesUVx-0.5)*2);
                VbeatLinesUVx = 1-abs((VbeatLinesUVx-0.5)*2);
                
                //..0->1.. shrink uvs
                float HlinesUVy = abs(i.uv.y*(1+HlinesMargin*2)-HlinesMargin)%1;
                // 0->1 0->1 ... repeate motif
                HlinesUVy = (HlinesUVy*RowNumber)%1;
                // 0 1 0
                HlinesUVy = 1-abs((HlinesUVy-0.5)*2);

                /// Vlines
                float Vlines = max(0,1-((VlinesUVx))*VerticalLineThickness)*2 * Vlimit;
                float VbeatLines = max(0,1-((VbeatLinesUVx))*VerticalLineThickness*0.35)*2 * Vlimit; // 0.35 * beat line adjustment
                /// Hlines
                float Hlines = max(0,1-(HlinesUVy)*HorizontalLineThickness)*2 * Hlimit;

                col = max(max(Vlines,VbeatLines),Hlines);

                // VlinesMargin+=0.01*imageRatio;
                // HlinesMargin+=0.01/imageRatio;
                Vlimit = (sign(centeredUVs.y-HlinesMargin*1.75)*0.5+0.5);
                Hlimit = (sign(centeredUVs.x-VlinesMargin*1.8)*0.5+0.5);

                float BoxInternalPaddingX = 0.02;
                float BoxInternalPaddingy = 0.04;

                //float PadBoxUVx = clamp(i.uv.x*1.5-0.25,0,1);
                //..0->1.. shrink uvs
                float PadBoxUVx = abs(i.uv.x*(1+VlinesMargin*2)-VlinesMargin)%1;
                // pad index x counting
                int PadXindex = floor(PadBoxUVx*16*mesureNumber);
                // 0->1 0->1 ... repeate motif
                PadBoxUVx = (PadBoxUVx*(16*mesureNumber))%1;
                // internal padding
                PadBoxUVx = clamp(PadBoxUVx*(1+BoxInternalPaddingX*2)-BoxInternalPaddingX,0,1);
                // 0 1 0
                PadBoxUVx = 1-abs((PadBoxUVx-0.5)*2);


                //..0->1.. shrink uvs
                float PadBoxUVy = abs(i.uv.y*(1+HlinesMargin*2)-HlinesMargin)%1;
                // pad index y counting
                int PadYindex = floor(PadBoxUVy*6);
                // 0->1 0->1 ... repeate motif
                PadBoxUVy = (PadBoxUVy*6)%1;
                // internal padding
                PadBoxUVy = clamp(PadBoxUVy*(1+BoxInternalPaddingy*2)-BoxInternalPaddingy,0,1);
                // 0 1 0
                PadBoxUVy = 1-abs((PadBoxUVy-0.5)*2);

                float BoxAlpha = min((PadBoxUVx*20)/imageRatio,PadBoxUVy*20*imageRatio)*Vlimit*Hlimit* GetBitFromPadChecks(PadXindex+(PadYindex*(16*mesureNumber))); //  * GetBitFromPadChecks() do masking by counting with uvs 

                col+= BoxAlpha*InstrumentColors[PadYindex];


                return float4(col,1);


            }
            ENDCG
        }
    }
}
