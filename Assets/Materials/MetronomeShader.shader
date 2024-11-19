Shader "Unlit/MetronomeShader"
{
	  Properties
    {
        
        _MainTex ("Texture", 2D) = "white" {}
        _BPMnormalized ("BPMnormalized", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            //Blend SrcAlpha OneMinusSrcAlpha
            //ZWrite Off

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
            
            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _BPMnormalized;
           
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
                fixed3 color = float3(0,0,0);

                float centerLineImageYdeform = lerp(1.05, 0.64 , clamp( (abs(i.uv.x - 0.5)-0.08)*25,0,1));

                float centerLineThikness = 1;

                float centerLine =  1-smoothstep(0.0, 1, abs(i.uv.y - 0.5*centerLineImageYdeform)*100/centerLineThikness);

                float beatSpeed = _Time.x*2*_BPMnormalized;
                float beatLineThikness = 0.75;
                float beatLineHeight = 30/beatLineThikness;
                float beatSpacing = 10;
                float beatOffset = fmod(1-i.uv.x*(-sign(i.uv.x-0.5)) + beatSpeed,1);

                /// OPTI : use beat height instead of drawing beats independantly
                ///float beatUnitHeight = beatLineHeight * (0.5+floor((beatOffset)*2));

                /// Incorect, to do
                ///float beatLineImageYdeform = lerp(1.05, 0.64 , clamp( (abs((floor(i.uv.x*(beatSpacing)+0.5))/(beatSpacing) - 0.5)-0.08)*25,0,1));

                float beatLine = (1-smoothstep(0.0, 1, (fmod(beatOffset,1/beatSpacing)*50)/(beatLineThikness*0.34)))*(1-smoothstep(0.0, 1, abs(i.uv.y+(0.015 - 0.5)*centerLineImageYdeform)*100/beatLineThikness-beatLineHeight));
                beatLine += (1-smoothstep(0.0, 1, (fmod(1-beatOffset,1/beatSpacing)*50)/(beatLineThikness*0.34)))*(1-smoothstep(0.0, 1, abs(i.uv.y+(0.015 - 0.5)*centerLineImageYdeform)*100/beatLineThikness-beatLineHeight));
                
                beatLine += (1-smoothstep(0.0, 1, (fmod(beatOffset,1/(beatSpacing*4))*50)/(beatLineThikness*0.6*0.34)))*(1-smoothstep(0.0, 1, abs(i.uv.y+(0.015 - 0.5)*centerLineImageYdeform)*100/beatLineThikness-beatLineHeight*0.5));
                beatLine += (1-smoothstep(0.0, 1, (fmod(1-beatOffset,1/(beatSpacing*4))*50)/(beatLineThikness*0.6*0.34)))*(1-smoothstep(0.0, 1, abs(i.uv.y+(0.015 - 0.5)*centerLineImageYdeform)*100/beatLineThikness-beatLineHeight*0.5));
                


                color += float3(centerLine+beatLine,centerLine+beatLine,centerLine+beatLine);

                return float4(color,1.0);
            }
            ENDCG
        }
    }
}