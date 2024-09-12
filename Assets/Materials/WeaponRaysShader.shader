Shader "Unlit/WeaponRaysShader"
{
    Properties
    {
        
        // _SignalData ("Signal Data", 2D) = "white" {}
        _Time ("Time", Float) = 0.0
        _SignalCount ("Signal Count", Float) = 0
        //_MousePos ("Mouse Position", Vector) = (0, 0, 0, 0)
        _WeaponPos ("Weapon Position", Vector) = (0, 0, 0, 0)

        _MainTex ("Texture", 2D) = "white" {}
        _EditorRes ("Editor Camera resolution", Vector) = (0, 0, 0, 0)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            //ZWrite Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag


            #include "UnityCG.cginc"

            // Returns the square and saw signals
            float2 funcs( in float x , in float fz)
            {
                x *= 0.6365*fz;
    
                float h = frac(x)-0.5;
    
                float square = -sign(h);
                float saw = -sign(h) + sign(h)*2.0*abs(h);
    
                return float2( square, saw );
            }
      

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
            struct SignalData
            {
                float3 SinSawSquareFactor;
                float2 direction;
                float frequency;
                float amplitude;
            };
            
            //sampler2D _SignalData;
            StructuredBuffer<SignalData> _SignalBuffer;
            float4 _MainTex_ST;
            //float4 _RayDirLenght;
            float4 _WeaponPos;
            float4 _EditorRes;
            float _SignalCount;

            float frequency;
            float amplitude;
            //float3 SinSawSquareFactor;
            ///builtin
            /// uniform float _Time;
            // Built-in Unity shader variable: x = width, y = height, z = 1 + 1/width, w = 1 + 1/height
            //float4 _ScreenParams; 

         

            PS_INPUT vert (VS_INPUT v)
            {
                PS_INPUT o;
                o.pos = UnityObjectToClipPos(v.pos);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.worldPos = mul(unity_ObjectToWorld, v.pos);

                return o;
            }

            fixed4 frag (PS_INPUT i) : SV_Target
            {

                //float2 resolution = float2(_EditorRes.x, _EditorRes.y); // Assume screen resolution
                float zoom = .55;
                float2 resolution = float2(_ScreenParams.x, _ScreenParams.y); // Assume screen resolution

                float2 p = i.worldPos.xy;
                fixed4 color = float4(0,0,0,0);

                float2 weaponPos = _WeaponPos.xy*zoom;

                float slideSpeed = 1.;
                // Zoom
                p*=zoom;

                for (int j = 0; j < int(_SignalCount); ++j)
                {
                    SignalData sd = _SignalBuffer[j];
                    
                    float2 rayEndPos = (_WeaponPos.xy+sd.direction.xy)*zoom;
                    //float2 rayEndPos = (0.0,0.0);
                    frequency = 8.; // Frequency of the sine wave
            
                    // Compute the direction and length of the segment
                    float2 ba = rayEndPos - weaponPos;
                    float2 direction = ba / length(ba);
                    float2 pa = p - weaponPos;

                    // Transform the point p to the segment's local space
                    float2 localP = float2(dot(pa, direction), dot(pa, float2(-direction.y, direction.x)));
                
                
                    float h = (localP.x)/length(ba);

                    float morphSpeedFactor = min(1.0,h*10) - max(0.0,-(1-h)*10+1);


                    float effectiveAmplitude = .5 * sd.amplitude;
                    float rayThikness = 3.0;
                

                    localP.y /= morphSpeedFactor * effectiveAmplitude;
                
                    // FIX SLIDE SPEED OFFSET FOR SIN WAVE
                    float sine = sin((localP.x-_Time.y*slideSpeed-(.5+slideSpeed))*frequency);
                    float sineDeriv = (frequency)*cos((localP.x-_Time.y*slideSpeed-(.5+slideSpeed))*frequency)* morphSpeedFactor * effectiveAmplitude;
                    float sineResult = abs(sine-localP.y)/sqrt( 1.+ sineDeriv*sineDeriv );



                    float2 f = funcs(localP.x-_Time.y*slideSpeed,frequency*0.25); 

                    float squareWave = min(sign(localP.x),1.0 - smoothstep( 0.0, (1/frequency)*0.48, (max(sign(localP.x-length(ba)),min(abs(localP.y-f.x), 1/effectiveAmplitude*length(float2(frac(localP.x*(frequency*0.31825)+0.5-_Time.y*slideSpeed*0.31825*(frequency))-0.5,min(1.0-abs(localP.y),0.0)))*(1/(frequency*0.31825)))))));
                    float sawWave = min(sign(localP.x),1.0 - smoothstep( 0.01, .05, (max(sign(localP.x-length(ba)),min(abs(localP.y-f.y)*(1/effectiveAmplitude), 1/effectiveAmplitude*length(float2((frac((localP.x*(frequency*0.31825)-_Time.y*slideSpeed*0.31825*(frequency))*0.5)-0.5)*4,min(1.0-abs(localP.y),0.0))))*(1/(frequency*0.31825))))));
                    float sinWave = min(sign(localP.x),1.0 - smoothstep( 0.0, (1/frequency)*0.32, max(sign(localP.x-length(ba)),sineResult)));


                    //color += fixed4(1.,0,0,max(0.,sinWave));
                    color += fixed4(max(0.,sinWave),max(0.,sawWave),max(0.,squareWave),max(0.,(squareWave*sd.SinSawSquareFactor.z)+(sawWave*sd.SinSawSquareFactor.y)+(sinWave*sd.SinSawSquareFactor.x)));
                    //color = fixed4(1.,1.,1.,1.);
                }


   
                
                
                //return fixed4(1.0,1.0,1.0,1.0);
                return color;
            }
            ENDCG
        }
    }
}
