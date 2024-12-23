Shader "Unlit/MusicSheetShader"
{
	  Properties
    {
        
        _MainTex ("Texture", 2D) = "white" {}
        _ImageTextureDimentions ("Image AND Texture Dimentions", Vector) = (486.,80.,590.,481.)

    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha

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
            

            // cbuffer MusicSheetData: register(b0)
            // {
            //     float mesureNumber =0;
            //     /// NECESSARY ?
            //     //float[] elementIndexingStart;
            //     int ElementsInMesure[4] = { 2, 2, 2, 2};
            //     // needs to be of size 64 ? -> 4element/beat,4beat/mesure,4mesure/sheet
            //     float NoteElements[9] = {4,4,4,4,4,4,4,4,4};
            //     float NotesSpriteIdx[9];
            //     float NotesHeight[9] = {99,99,99,99,99,99,99,99,99};
            //     //
            // };

            
            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _ImageTextureDimentions;

            float mesureNumber;
            float ElementsInMesure[4];
            float NoteElements[48];
            float NotesSpriteIdx[48];
            float NotesHeight[48] ;
           


            PS_INPUT vert (VS_INPUT v)
            {
                PS_INPUT o;
                o.pos = UnityObjectToClipPos(v.pos);
                //o.worldPos = mul(unity_ObjectToWorld, v.pos);
             
                o.uv = v.uv;//TRANSFORM_TEX(v.uv, _MainTex);

                return o;
            }

            fixed4 frag (PS_INPUT i) : SV_Target
            {

      
                ///
                
                fixed3 color = float3(1,1,1);
                //not needed anymore ?
                float staffLeftMargin = 0.;
                float staffHorizontalBorders = 1.;
                //
                float staffVerticalBorder = .25;
                float staffLinesThikness = 6;


                /// PACK THOSE INFORMATIONS
                _ImageTextureDimentions = fixed4(486.,80.,590.,481.);
                fixed4 _TextureInfo[21];
                
                /// DMSOUPIR
                /// position x/y, ElementBounds
                _TextureInfo[0] = fixed4(234.5,175,29.5,60);
                /// SOUPIR
                /// position x/y, ElementBounds
                _TextureInfo[1] = fixed4(233,284,28,49);
                ///
                /// [4] Soupir pointe ?
                ///
                /// SILENCE
                /// position x/y, ElementBounds
                _TextureInfo[3] = fixed4(234.5,407,29.5,74);

                ///...

                /// DBLCROCHE
                /// position x/y, ElementBounds
                _TextureInfo[10] = fixed4(503.5,301,86.5,180);
                /// CROCHE
                /// position x/y, ElementBounds
                _TextureInfo[11] = fixed4(340.5,305,76.5,176);
                ///
                /// [12] Soupir pointe ?
                ///
                /// NOIRE
                /// position x/y, ElementBounds
                _TextureInfo[13] = fixed4(169.5,300.5,35,180);
                
                ///...

                /// CLEF
                /// position x/y, ElementBounds
                _TextureInfo[20] = fixed4(67,240.5,67,240.5);

                ///

                
                mesureNumber = 2;
                // float ElementsInMesure[4] = { 2, 2, 2, 2};
                // float NoteElements[9] = {4,4,4,4,4,4,4,4,4};
                // float NotesHeight[9] = {99,99,99,99,99,99,99,99,99};

                /// Data for testing in the editor
                /// ACTIVATE FOR THE EDITOR NOT TO CRASH
                /// DEACTIVATE IT TO SEE THE RUNTIME EFFECT
                // float ElementsInMesure[4] = { 1, 3, 2, 1};
                // float testNotes[9] = {4,2,1,1,2,2,4,0,0};
                // int NotesSpriteIdx[9] = {0,1,1,1,1,1,1,1,1};
                // int NotesHeight[9] = {4,0,1,2,3,4,5,6,7};
                // for (int temp = 0; temp < 9; ++temp)
                // {
                //     NoteElements[temp] = testNotes[temp];
                //     NotesSpriteIdx[temp] = NotesSpriteIdx[temp];
                //     NotesHeight[temp] = NotesHeight[temp];
                // }



                // derived from ElementsInMesure
                float _MesuresInfo[4] = {ElementsInMesure[0]*40,
                    ElementsInMesure[1]*40,
                    ElementsInMesure[2]*40,
                    ElementsInMesure[3]*40};
                //float _MesuresInfo[4] = {50.,0,0,0};

                float cumulatedNormalizedPreviousMesures = 0;
                //float cumulatedNormalizedPreviousElements = 0;

                // Staff size when all mesures and margin added
                float staffTOTALSize = _MesuresInfo[0]+_MesuresInfo[1]+_MesuresInfo[2]+_MesuresInfo[3]; // + ; ADD HERE THE NOTE EXTENDING THE MESURE AND STAFF + the clef margin
                
                float staffXFactor = 1/(staffTOTALSize/_ImageTextureDimentions.x);
                float staffX = i.uv.x*staffXFactor - (staffXFactor)*0.5+0.5;

                float StaffXpos =  clamp(i.uv.x*staffXFactor - (staffXFactor)*0.5+0.5,0,1);

                int mesureIndex = 0;
                int elementIndex = 0;
                {
                    cumulatedNormalizedPreviousMesures = _MesuresInfo[0]/staffTOTALSize;
                    while(StaffXpos>cumulatedNormalizedPreviousMesures) 
                    {
                        elementIndex += ElementsInMesure[mesureIndex];
                        mesureIndex++;
                        cumulatedNormalizedPreviousMesures += _MesuresInfo[mesureIndex]/staffTOTALSize;
                    }
                    cumulatedNormalizedPreviousMesures -= _MesuresInfo[mesureIndex]/staffTOTALSize;
                }
                
                float LeftCutoff = (sign(staffX-(_MesuresInfo[0]/staffTOTALSize)*1.1)+1)*0.5;
                float RightCutoff = (sign(1-(staffX))+1)*0.5;

                /// NECESSARY ?
                //int elementIndexingStart = ElementIndexAtMesures[mesureIndex];
                int elementIndexingStart = 0;

                /// 0[ ... ](ElementsInMesure)
                float MesureXpos =  ((StaffXpos-cumulatedNormalizedPreviousMesures)/(_MesuresInfo[mesureIndex]/staffTOTALSize))*ElementsInMesure[mesureIndex];
                
                {
                    //cumulatedNormalizedPreviousElements = NoteElements[elementIndex];
                    float cumulatedNormalizedPreviousElements = 1;
                    while((MesureXpos)>cumulatedNormalizedPreviousElements)
                    {
                        elementIndex++;
                        cumulatedNormalizedPreviousElements += 1;
                    }
                    //cumulatedNormalizedPreviousElements -= NoteElements[elementIndex];
                }
             


                /// staff lines placement


                float staffY = fmod(((sign((1-i.uv.y*(staffVerticalBorder+1)))*i.uv.y)*(1+staffVerticalBorder*2)-staffVerticalBorder)*5,1);
               
                float staffLines =  (smoothstep(1, 0, abs(staffY - 0.5)*100/staffLinesThikness))*(sign((1-abs(staffX-0.5)*2)-(1-(1/staffHorizontalBorders)))+1)*0.5;


                /// Mesure lines placement

                float mesureLinesWidth = 0.1*mesureNumber*20*3 * (100/_MesuresInfo[mesureIndex]);
                float mesureLinesHeight = 53;

                float mesureLinesXpos = fmod((staffX- cumulatedNormalizedPreviousMesures)/(_MesuresInfo[mesureIndex]/staffTOTALSize),1); 
          
                // centering
                float mesureLinesXcentering = fmod(1-abs(mesureLinesXpos-0.5)*2,1)*2+0.5;
                

                float mesureLines = LeftCutoff*smoothstep(1, 0, abs(mesureLinesXcentering-0.5)*1000-mesureLinesWidth) * smoothstep(1, 0, (abs(i.uv.y-0.5)*200-mesureLinesHeight));
                //float mesureLines = smoothstep(1, 0, abs(mesureLinesXcentering-0.5)*1000-mesureLinesWidth) * smoothstep(1, 0, (abs(i.uv.y-0.5)*200-mesureLinesHeight));
                
                /// Beat placement

                float beatPerMesure = ElementsInMesure[mesureIndex];

                /// Helper beat reference
                
                // float beatWidth = 5*mesureNumber*(1+staffHorizontalBorders*0.1) * (100/_MesuresInfo[mesureIndex])*beatPerMesure;//*(1./NoteElements[elementIndex]);
                // float beatHeight = 10;
                // float beatStartDistance = 0.5;

                // float beatTightness = 1.;//0.75;

                // // centering + clef offset
                // float beatXcentering = staffX*staffHorizontalBorders-(staffHorizontalBorders-1)*0.5;
                // float beatXoffseted = beatXcentering*(staffLeftMargin+1)-staffLeftMargin;
                // // beat duplication over mesure + beat tightness
                // //float beatXpos = ((fmod(beatXoffseted*mesureNumber,1))*beatTightness-(beatTightness-1)*0.5);
                // float beatXpos = mesureLinesXpos*beatPerMesure;
                // // crop strokes 
                // //beatXpos = beatXpos*(-sign(beatXpos-1)+1)*0.5*(sign(beatXpos)+1)*0.5;
                // // layout over a mesure
                // //beatXpos = ((beatXpos-floor(beatXpos*beatPerMesure)*(1./beatPerMesure))*beatPerMesure);
                
                // beatXpos = fmod(beatXpos,1);

                // /// set first beat at fixed distance from start
                // //beatXpos = beatXpos+0.5 -(beatStartDistance*(100/_MesuresInfo[mesureIndex]));

                // float beatStrokes = smoothstep(1, 0, abs(beatXpos-0.5)*1000-beatWidth) * smoothstep(1, 0, (abs(i.uv.y-0.1)*200-beatHeight)) ;
                

                /// Notes elements placement
                
                fixed4 ElementColor = float4(0,0,0,0);


                float2 UVscallingFactor = float2((_ImageTextureDimentions.x/_ImageTextureDimentions.z),(_ImageTextureDimentions.y/_ImageTextureDimentions.w))*5.25;
                    
                float mesureStart = 0.5-((staffTOTALSize/_ImageTextureDimentions.x)*0.5)+(cumulatedNormalizedPreviousMesures/staffXFactor);

                float staffYstart = (0.5/(1+staffVerticalBorder*2));
                float staffYincrement = (0.5-staffYstart)*(0.2*(0.5/staffVerticalBorder));
                staffYstart = (0.5-staffYstart + (0.5-staffYstart)*(0.2*(0.5/staffVerticalBorder))) - staffYincrement*2;

                /// OPTI ?
                int j = 0;
                while (j<16)
                {
                    float elementXstart = (0.5/beatPerMesure)*((_MesuresInfo[mesureIndex]/staffTOTALSize)*(staffTOTALSize/_ImageTextureDimentions.w));
                    float elementX = j*(elementXstart*2);
                    float elementY = NotesHeight[elementIndex];

                    float2 TranscribedPosition = float2(mesureStart+elementXstart+elementX,staffYstart+staffYincrement*elementY);

                    float2 elementUV = i.uv*UVscallingFactor;
                    // Center note on arbitrary pivot
                    elementUV = elementUV + float2((_TextureInfo[NotesSpriteIdx[elementIndex]].x/_ImageTextureDimentions.z),(_TextureInfo[NotesSpriteIdx[elementIndex]].y/_ImageTextureDimentions.w));
                    // place note at target
                    elementUV = elementUV - TranscribedPosition*UVscallingFactor;
                
                    // sample texture and crop element from atlas
                    ElementColor += tex2D(_MainTex, float2(elementUV.x,elementUV.y))
                    *
                    (
                    (sign(-abs(_TextureInfo[NotesSpriteIdx[elementIndex]].x/_ImageTextureDimentions.z-elementUV.x) + _TextureInfo[NotesSpriteIdx[elementIndex]].z/_ImageTextureDimentions.z)+1)*0.5*
                    (sign(-abs(_TextureInfo[NotesSpriteIdx[elementIndex]].y/_ImageTextureDimentions.w-elementUV.y) + _TextureInfo[NotesSpriteIdx[elementIndex]].w/_ImageTextureDimentions.w)+1)*0.5
                    );

                    j++;
                    if(j>ElementsInMesure[mesureIndex])
                        break;

                }
                ElementColor*=RightCutoff;

                color -= RightCutoff*float3(staffLines+mesureLines,staffLines+mesureLines,staffLines+mesureLines);
                //color -= RightCutoff*float3(staffLines+mesureLines+beatStrokes,staffLines+mesureLines+beatStrokes,staffLines+mesureLines+beatStrokes);
                
                return float4((color*(1-ElementColor.w)+ElementColor.xyz*ElementColor.w),1.0) ;
                //return float4(color,1.0) ;
                //return ElementColor;
            }
            ENDCG
        }
    }
}