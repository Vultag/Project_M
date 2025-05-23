Shader "Unlit/MusicTrackShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _ImageTextureDimentions ("Image AND Texture Dimentions", Vector) = (140.,300.,384.,256.)
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

            cbuffer TrackGridBuffer : register(b0) {
                float2 TrackGridArray[66]; // 2D array of 6*10 + 6 of top side padding
                float3 TrackGridColorArray[66];            // 12 bytes + 4 bytes padding
                };
     

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
            float4 _ImageTextureDimentions;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                //fixed4 col = tex2D(_MainTex, i.uv);

                fixed4 col = float4(0,0,0,1);

                int spritePsize = 128;
                //float spriteProportion = (spritePsize/_ImageTextureDimentions.w);
                float2 spriteProportion = float2(spritePsize/_ImageTextureDimentions.z,spritePsize/_ImageTextureDimentions.w);

                int numOfLines = 6.;
                int numOfmesureOnScreen = 10;
                
                float trackLinesThikness = 1.2*numOfLines;
                float mesureLinesThikness = 0.4*numOfmesureOnScreen;
                float mesureLineLenght = 1.5;
                float trackFlowSpeed = 0.25 *(1./numOfmesureOnScreen);
                float sideMargin = 0;
                //float sideMargin = 0.12;


                float trackX  = clamp((i.uv.x-sideMargin)*(1./(1-sideMargin*2)),0,1);
                /// offset the trackY with the time
                float trackY = fmod(i.uv.y+(_Time.y*trackFlowSpeed),1);
                //float trackY = fmod(i.uv.y,1);

                float trackLineX = fmod(trackX*numOfLines,1);
                float trackMesureY = fmod(trackY*numOfmesureOnScreen,1);

                float mirroredXuv = (1-abs(trackLineX*2-1))*0.5;
                float mirroredYuv = (1-abs(trackMesureY*2-1))*0.5;
               
                float trackLine =  (smoothstep(1, 0, abs(mirroredXuv - 0.5)*100./trackLinesThikness));
                float mesureLine =  (smoothstep(1, 0, mirroredYuv*100./mesureLinesThikness));
                /// mask mesureLine for trackLine only
                mesureLine *= ceil(mirroredXuv-(1/(mesureLineLenght+2)));

                /// Color the running mesure in green
                float coloredPlayingMesures = (-ceil((i.uv.y-1)-(1./numOfmesureOnScreen * (1-fmod(_Time.y*trackFlowSpeed*numOfmesureOnScreen,1)))));
                float3 trackBackground = (trackLine+mesureLine) * (float3(0,1,0)*coloredPlayingMesures) + (trackLine+mesureLine)*(1-coloredPlayingMesures);

                
                float atlasSpriteSize = (1./6.);

                // float2 UVscallingFactor = float2(
                //     (_ImageTextureDimentions.x/_ImageTextureDimentions.z)*12,
                //     (_ImageTextureDimentions.y/_ImageTextureDimentions.w)*(_ImageTextureDimentions.z/_ImageTextureDimentions.x)*(0.5)/(atlasSpriteSize))
                //     ;
                float2 UVscallingFactor = float2(
                    (_ImageTextureDimentions.x/_ImageTextureDimentions.z),
                    (_ImageTextureDimentions.y/_ImageTextureDimentions.w))/atlasSpriteSize
                    ;



                //float gridY = fmod(i.uv.y+(_Time.y*trackFlowSpeed),1.);
                float gridY = i.uv.y+fmod(_Time.y*0.25,1)*0.1;
                /// do margin here
                float2 elementUV =  float2(
                    ((i.uv.x-(1./numOfLines)*0.5)*UVscallingFactor.x+(spritePsize/_ImageTextureDimentions.z)*0.5),
                    ((gridY-(1./numOfmesureOnScreen)*0.5)*UVscallingFactor.y+(spritePsize/_ImageTextureDimentions.w)*0.5)
                    );

                /// offset tiles
                elementUV = float2(elementUV.x,elementUV.y);
                //float Xoffset = (floor(UVscallingFactor.x)*0.75*spriteProportion);
                //float Xoffset = (floor(UVscallingFactor.x)*(0.4175)*spriteProportion);
                //float Xoffset = (floor(UVscallingFactor.x)*(0.25)*spriteProportion);
                //float Xoffset = (floor(UVscallingFactor.x)*(0.15)*spriteProportion);
                // float Xoffset = (floor(UVscallingFactor.x)*(0.104));
                // float Yoffset = (floor(UVscallingFactor.x)*0.12);


                int2 currentElementIdx = int2(
                    floor(elementUV.x/((1./numOfLines)*UVscallingFactor.x)),
                    floor(elementUV.y/((1./numOfmesureOnScreen)*UVscallingFactor.y))
                    );
                    
                /// OPTI -> deduct from currentElementIdx no need to recalculate all
                float2 unmaskedElementUV = float2(
                    //fmod(elementUV.x,(2.95/numOfLines)),
                    fmod(elementUV.x,(1./numOfLines)*UVscallingFactor.x),
                    //fmod(elementUV.y,(12./numOfmesureOnScreen)));
                    fmod(elementUV.y,(1./numOfmesureOnScreen)*UVscallingFactor.y));


                // /// mask the space created by the tile offset
                // /// !! artefacts on sides -> find other way to mask?
                // elementUV = float2(
                //     //unmaskedElementUV.x*(1-ceil((unmaskedElementUV.x)-(spritePsize/_ImageTextureDimentions.z)))*floor(unmaskedElementUV.x+1),
                //     unmaskedElementUV.x*(1-ceil(unmaskedElementUV.x-(spritePsize/_ImageTextureDimentions.z))),
                //     unmaskedElementUV.y*(1-ceil(unmaskedElementUV.y-(spritePsize/_ImageTextureDimentions.w)))*floor(unmaskedElementUV.y+1));
                //     //unmaskedElementUV.y);
           

                /// flattened indexing
                /// *6 as maxLinesNum is 6 for flattened array indexing
                int flattenedIdx = currentElementIdx.x+(currentElementIdx.y*6);
                    
                //float2 test = TrackGridArray[flattenedIdx];
                // if(flattenedIdx==33)
                //     test = float2(1,1);
                // else
                //     test = float2(99,99);

                // if(flattenedIdx==11)
                //     test = float2(1,0);

                /// calibrate uvs for target atlas sprite
                elementUV = float2(
                    unmaskedElementUV.x+((TrackGridArray[flattenedIdx].x)*spriteProportion.x),
                    unmaskedElementUV.y+((TrackGridArray[flattenedIdx].y)*spriteProportion.y)
                    );
                
                float4 itemCol = tex2D(_MainTex, float2(elementUV.x,elementUV.y));

                /// mask the space created by the tile offset
                itemCol *= 
                    (1-ceil(unmaskedElementUV.x-(spritePsize/_ImageTextureDimentions.z)))*floor(unmaskedElementUV.y+1)*
                    (1-ceil(unmaskedElementUV.y-(spritePsize/_ImageTextureDimentions.w)))*floor(unmaskedElementUV.x+1);


                float4 itemColoring = float4(TrackGridColorArray[flattenedIdx],1);
                
                col+= float4(trackBackground,1) - (float4(1,1,1,0)-itemColoring)*itemCol.z;
                col+= itemCol * itemColoring;

                return col;
            }
            ENDCG
        }
    }
}
