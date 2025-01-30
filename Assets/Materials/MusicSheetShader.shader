Shader "Unlit/MusicSheetShader"
{
	  Properties
    {
        
        _MainTex ("Texture", 2D) = "white" {}
        _ImageTextureDimentions ("Image AND Texture Dimentions", Vector) = (486.,80.,590.,506.)

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
            
            // draw line segment from A to B
            float segment(float2 P, float2 A, float2 B, float r) 
            {
                float2 g = B - A;
                float2 h = P - A;
                float d = length(h - g * clamp(dot(g, h) / dot(g,g), 0.0, 1.0));
	            return smoothstep(r, 0.5*r, d);
            }

            ///
            /// PUT FUNC IN SEPERATE FILE
            ///

            // Inverse hyperbolic cosine (acosh) function for HLSL
            float acosh(float x) {
                return log(x + sqrt(x * x - 1.0));
            }
            // Inverse hyperbolic sine (asinh) function for HLSL
            float asinh(float x) {
                return log(x + sqrt(x * x + 1.0));
            }
            // SDFs
            float sdBezier(in float2 p, in float2 v1, in float2 v2, in float2 v3) {
                float2 c1 = p - v1;
                float2 c2 = 2.0 * v2 - v3 - v1;
                float2 c3 = v1 - v2;

                float t3 = dot(c2, c2);
                float t2 = dot(c3, c2) * 3.0 / t3;
                float t1 = (dot(c1, c2) + 2.0 * dot(c3, c3)) / t3;
                float t0 = dot(c1, c3) / t3;

                float t22 = t2 * t2;
                float2 pq = float2(t1 - t22 / 3.0, t22 * t2 / 13.5 - t2 * t1 / 3.0 + t0);
                float ppp = pq.x * pq.x * pq.x, qq = pq.y * pq.y;

                float p2 = abs(pq.x);
                float r1 = 1.5 / pq.x * pq.y;

                if (qq * 0.25 + ppp / 27.0 > 0.0) {
                    float r2 = r1 * sqrt(3.0 / p2), root;
                    if (pq.x < 0.0) root = sign(pq.y) * cosh(acosh(r2 * -sign(pq.y)) / 3.0);
                    else root = sinh(asinh(r2) / 3.0);
                    root = clamp(-2.0 * sqrt(p2 / 3.0) * root - t2 / 3.0, 0.0, 1.0);
                    return 1.0-length(p - lerp(lerp(v1, v2, root), lerp(v2, v3, root), root));
                }

                else {
                    float ac = acos(r1 * sqrt(-3.0 / pq.x)) / 3.0;
                    float2 roots = clamp(2.0 * sqrt(-pq.x / 3.0) * cos(float2(ac, ac - 4.18879020479)) - t2 / 3.0, 0.0, 1.0);
                    float2 p1 = p - lerp(lerp(v1, v2, roots.x), lerp(v2, v3, roots.x), roots.x);
                    float2 p2 = p - lerp(lerp(v1, v2, roots.y), lerp(v2, v3, roots.y), roots.y);
                    return 1.0-sqrt(min(dot(p1, p1), dot(p2, p2)));
                }
}


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
            //float NoteElements[48];
            float NotesSpriteIdx[48];
            float NotesHeight[48] ;
            //uint NotesIsPointed[48] ;
           


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
                
                fixed3 color = float3(0.1,0.1,0.1);
                //not needed anymore ?
                float staffLeftMargin = 0.;
                float staffHorizontalBorders = 1.;
                //
                float staffVerticalBorder = .25;
                float staffLinesThikness = 6;


                /// PACK THOSE INFORMATIONS
                _ImageTextureDimentions = fixed4(486.,80.,590.,506.);
                fixed4 _TextureInfo[25];
                
                /// DMSOUPIR
                /// position x/y, ElementBounds
                _TextureInfo[0] = fixed4(234.5,200,29.5,60);
                /// SOUPIR
                /// position x/y, ElementBounds
                _TextureInfo[1] = fixed4(233,309,28,49);
                ///
                /// [4] Soupir pointe ?
                ///
                /// SILENCE
                /// position x/y, ElementBounds
                _TextureInfo[3] = fixed4(234.5,432,29.5,74);

                ///...

                /// DBLCROCHE
                /// position x/y, ElementBounds
                _TextureInfo[10] = fixed4(503.5,326,86.5,180);
                /// CROCHE
                /// position x/y, ElementBounds
                _TextureInfo[11] = fixed4(340.5,330,76.5,176);
                ///
                /// [12] Soupir pointe ?
                ///
                /// NOIRE
                /// position x/y, ElementBounds
                _TextureInfo[13] = fixed4(169.5,325.5,35,180);
                
                ///...

                /// CLEF SOL
                /// position x/y, ElementBounds
                _TextureInfo[20] = fixed4(67,265.5,67,240.5);
                /// CLEF FA
                /// position x/y, ElementBounds
                // TO DO  _TextureInfo[21] =
                /// Sharp
                /// position x/y, ElementBounds
                _TextureInfo[22] = fixed4(158.5,81.5,24.5,64.5);
                /// Flat
                /// position x/y, ElementBounds
                _TextureInfo[23] = fixed4(204,70,21,70);
                /// Natural
                /// position x/y, ElementBounds
                _TextureInfo[24] = fixed4(242,77,17,63);
                



                
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
                float _MesuresInfo[4] = {1+ElementsInMesure[0]*35,
                    1+ElementsInMesure[1]*35,
                    1+ElementsInMesure[2]*35,
                    1+ElementsInMesure[3]*35};
                //float _MesuresInfo[4] = {50.,0,0,0};

                float cumulatedNormalizedPreviousMesures = 0;
                //float cumulatedNormalizedPreviousElements = 0;

                // Staff size when all mesures and margin added
                float staffTOTALSize = _MesuresInfo[0]+_MesuresInfo[1]+_MesuresInfo[2]+_MesuresInfo[3]; // + ; ADD HERE THE NOTE EXTENDING THE MESURE AND STAFF + the clef margin
                
                float staffXFactor = 1/(staffTOTALSize/_ImageTextureDimentions.x);
                float staffX = i.uv.x*staffXFactor - (staffXFactor)*0.5+0.5;

                /// Lim to 1 to prevent crash on elementIndex while loop computation due to faulty mesureIndex?
                float StaffXpos =  clamp(i.uv.x*staffXFactor - (staffXFactor)*0.5+0.5,0,0.999999);

                int mesureIndex = 0;
                int elementIndex = 0;
                
                cumulatedNormalizedPreviousMesures = _MesuresInfo[0]/staffTOTALSize;
                while(StaffXpos>cumulatedNormalizedPreviousMesures) 
                {
                    elementIndex += ElementsInMesure[mesureIndex];
                    mesureIndex++;
                    cumulatedNormalizedPreviousMesures += _MesuresInfo[mesureIndex]/staffTOTALSize;
                }
                cumulatedNormalizedPreviousMesures -= _MesuresInfo[mesureIndex]/staffTOTALSize;
                
                
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

                //float beatPerMesure = ElementsInMesure[mesureIndex];

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
                    /// NotesSpriteIdx contains spriteIdx(integer), isDotted(decimal) and isLinked information(sign)
                    int NoteEffectiveSpriteIdx = abs(NotesSpriteIdx[elementIndex]);
                    uint NoteIsDotted = ceil(frac(NotesSpriteIdx[elementIndex]));
                    NoteEffectiveSpriteIdx = floor(NoteEffectiveSpriteIdx);

                    float elementXstart = (0.5/(ElementsInMesure[mesureIndex]))*((_MesuresInfo[mesureIndex]/staffTOTALSize)*(staffTOTALSize/_ImageTextureDimentions.x));
                    float elementX = j*(elementXstart*2);


                    float elementY = floor(NotesHeight[elementIndex]);

                    float2 TranscribedPosition = float2(mesureStart+elementXstart+elementX,staffYstart+staffYincrement*elementY);

                    float2 elementUV = i.uv*UVscallingFactor;
                    // Center note on arbitrary pivot
                    elementUV = elementUV + float2((_TextureInfo[NoteEffectiveSpriteIdx].x/_ImageTextureDimentions.z),(_TextureInfo[NoteEffectiveSpriteIdx].y/_ImageTextureDimentions.w));
                    // place note at target
                    elementUV = elementUV - TranscribedPosition*UVscallingFactor;

                    // sample texture and crop element from atlas for element
                    ElementColor += tex2D(_MainTex, float2(elementUV.x,elementUV.y))
                    *
                    (
                    (sign(-abs(_TextureInfo[NoteEffectiveSpriteIdx].x/_ImageTextureDimentions.z-elementUV.x) + _TextureInfo[NoteEffectiveSpriteIdx].z/_ImageTextureDimentions.z)+1)*0.5*
                    (sign(-abs(_TextureInfo[NoteEffectiveSpriteIdx].y/_ImageTextureDimentions.w-elementUV.y) + _TextureInfo[NoteEffectiveSpriteIdx].w/_ImageTextureDimentions.w)+1)*0.5
                    );

                    /// Add dot if the element is indeed dotted
                    // OPTI
                    if(NoteIsDotted>0)
                    {
                        float dotRadius = 0.035;
                        float imageSquishFactor = _ImageTextureDimentions.x/_ImageTextureDimentions.y;
                        float distance = length(float2(i.uv.x*imageSquishFactor,i.uv.y) - float2(0.13+TranscribedPosition.x*imageSquishFactor,TranscribedPosition.y));
                        float dotAlpha = smoothstep(dotRadius, dotRadius * 0.6, distance);
                        /// erase ElementColor behind dot for proper transparency and display it
                        ElementColor = ElementColor* (sign(distance-dotRadius)+1)*0.5 + (float4(float3(1,1,1),1 )*dotAlpha);
                    }
                
                    /// Add a accidental if the element has one
                    float NoteRemainder = frac(NotesHeight[elementIndex]);
                    if(ceil(NoteRemainder)>0)
                    {
                        float alterationXOffset = .025;
                        int alterationIdx = 22+round(NoteRemainder);
                        float alterationX = elementX-alterationXOffset;
                        float2 alterationTranscribedPosition = float2(mesureStart+elementXstart+alterationX,staffYstart+staffYincrement*elementY);
                        float2 alterationelementUV = i.uv*UVscallingFactor;
                        // Center note on arbitrary pivot
                        alterationelementUV = alterationelementUV + float2((_TextureInfo[alterationIdx].x/_ImageTextureDimentions.z),(_TextureInfo[alterationIdx].y/_ImageTextureDimentions.w));
                        // place note at target
                        alterationelementUV = alterationelementUV - alterationTranscribedPosition*UVscallingFactor;

                        float alterationCrop =  (
                        (sign(-abs(_TextureInfo[alterationIdx].x/_ImageTextureDimentions.z-alterationelementUV.x) + _TextureInfo[alterationIdx].z/_ImageTextureDimentions.z)+1)*0.5*
                        (sign(-abs(_TextureInfo[alterationIdx].y/_ImageTextureDimentions.w-alterationelementUV.y) + _TextureInfo[alterationIdx].w/_ImageTextureDimentions.w)+1)*0.5
                        );
                        /// clear ElementColor it overlaps
                        ElementColor *= -(alterationCrop-1);
                        // sample texture and crop element from atlas for alteration
                        ElementColor += tex2D(_MainTex, float2(alterationelementUV.x,alterationelementUV.y))*alterationCrop;

                    }
                  

                    j++;
                    if(j>ElementsInMesure[mesureIndex]-1)
                        break;

                }
                ElementColor*=RightCutoff;


                /// liaison loop over all elements OPTI
                float LiaisonLayerAlpha = 0;
                {
                    int l = 1;
                    while (l<17)
                    {
                        /// liaison draw if the element is linked (NotesSpriteIdx[x]<0)
                        if(NotesSpriteIdx[l]<0)
                        {
                            float liaisonMesureStart = 0.5-((staffTOTALSize/_ImageTextureDimentions.x)*0.5);

                            /// Upcomming Error here for mesure indexing
                            float elementXstart = (0.5/(ElementsInMesure[1]))*((_MesuresInfo[1]/staffTOTALSize)*(staffTOTALSize/_ImageTextureDimentions.x));
                            float elementX = l*(elementXstart*2);;
                            float elementY = floor(NotesHeight[l]);
                            float2 TranscribedPosition = float2(liaisonMesureStart+elementXstart+elementX,staffYstart+staffYincrement*elementY)*UVscallingFactor;

                            float PreviousElementX = (l-1)*(elementXstart*2);
                            float PreviousElementY = floor(NotesHeight[l-1]);
                            float2 PreviousTranscribedPosition = float2(liaisonMesureStart+elementXstart+PreviousElementX,staffYstart+staffYincrement*PreviousElementY)*UVscallingFactor;

                            /// offset slitly bezier curve start and end form element
                            PreviousTranscribedPosition+= float2(0.045,-0.04);
                            TranscribedPosition+= float2(-0.055,-0.04);

                            float2 start = PreviousTranscribedPosition;
                            float2 mid = float2((TranscribedPosition.x+PreviousTranscribedPosition.x)*0.5,abs(TranscribedPosition.y+PreviousTranscribedPosition.y)*0.5-0.1);
                            float2 end = TranscribedPosition;
                            // Calculate the distance to the Bezier curve
                            float curveWidth = 150;
                            float dist = sdBezier(i.uv*UVscallingFactor, start, mid, end)*(curveWidth+1)-curveWidth;
                            LiaisonLayerAlpha += clamp(dist,0,1);
                        }
                        l++;
                    }
                }
             
                

                color += RightCutoff*float3(staffLines+mesureLines,staffLines+mesureLines,staffLines+mesureLines);
                
                float4 finalColor = float4((color*(1-ElementColor.w)+ElementColor.xyz*ElementColor.w),1.0);

                return finalColor+float4(1,1,1,1)*(LiaisonLayerAlpha);

            }
            ENDCG
        }
    }
}