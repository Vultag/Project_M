Shader "Unlit/FilterScreenShader"
{
	  Properties
    {
        
        _Frequency ("Cutoff", Float) = 0
        _Q ("Resonance", Float) = 0

        _MainTex ("Texture", 2D) = "white" {}
        //_EditorRes ("Editor Camera resolution", Vector) = (0, 0, 0, 0)
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


            // const float maxWavelength = 780;
            // const float minWavelength = 380;
            
            // float _Cutoff;
            // float _Resonance;

            
            // OPTI: branchless ?
            float3 WavelengthToRGB(float wavelength) 
            {
                //skiped violet
                //float3 violet = float3(0.56, 0.0, 1.0);
                float3 blue = float3(0.0, 0.0, 1.0);
                float3 cyan = float3(0.0, 1.0, 1.0);
                float3 green = float3(0.0, 1.0, 0.0);
                float3 yellow = float3(1.0, 1.0, 0.0);
                float3 orange = float3(1.0, 0.5, 0.0);
                float3 red = float3(1.0, 0.0, 0.0);
                float3 color = float3(0.0, 0.0, 0.0);

                // Violet to Blue
                //color = lerp(violet, blue, smoothstep(0.0, 100.0, wavelength));
                // Blue
                //color = lerp(blue, color, smoothstep(0.0, 100.0, wavelength));

                // Blue to Cyan
                color = lerp(blue, cyan, smoothstep(0.0, 100.0, wavelength));

                // Cyan to Green
                color = lerp(color, green, smoothstep(100.0, 200.0, wavelength));

                // Green to Yellow
                color = lerp(color, yellow, smoothstep(200, 300.0, wavelength));

                // Yellow to Orange
                color = lerp(color, orange, smoothstep(300.0, 400.0, wavelength));

                // Orange to Red
                color = lerp(color, red, smoothstep(400, 500.0, wavelength));

                // Red (extend to 780nm)
                color = lerp(color, red, smoothstep(500, 600.0, wavelength));

                // // Intensity scaling to simulate the human eye sensitivity
                // float intensity = 1.0;
                // intensity *= (wavelength > 700.0) ? 0.3 + 0.7 * (780.0 - wavelength) / (780.0 - 700.0) : 1.0;
                // intensity *= (wavelength < 420.0) ? 0.3 + 0.7 * (wavelength - 380.0) / (420.0 - 380.0) : 1.0;

                // // Apply intensity scaling
                // color *= intensity;

                return color;
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
            
            float4 _MainTex_ST;
            float _Frequency; // Center frequency in Hz
            float _Q; // Q Factor
            float4 _LineColor; // Color of the line
            float _LineWidth; // Width of the line

            // Function to calculate the magnitude response of the low-pass biquad filter
            void magnitudeResponse(float frequency, out float response, out float Derivative)
            {
                // Constants
                float fs = 48000.0; // Increased sampling rate in Hz
                float omega = 2.0 * 3.14159265 * (_Frequency) / fs; // Convert frequency to normalized radians

                float sn = sin(omega);
                float cs = cos(omega);

                float alpha = sn / (2.0 * _Q); // Calculate alpha

                // Biquad coefficients for low-pass filter
                float b0 = (1.0 - cs) / 2.0;
                float b1 = 1.0 - cs;
                float b2 = (1 - cs) / 2.0;
                float a0 = 1.0 + alpha;
                float a1 = -2.0 * cs;
                float a2 = 1.0 - alpha;

                // Normalize coefficients
                b0 /= a0;
                b1 /= a0;
                b2 /= a0;
                a1 /= a0;
                a2 /= a0;

                // Improved phi calculation for stability
                float phi = sin(3.14159265 * frequency / fs);
                phi = phi * phi;

                // Calculation of response magnitude squared
                float num = pow(b0 + b1 + b2, 2.0) - 4.0 * (b0 * b1 + 4.0 * b0 * b2 + b1 * b2) * phi + 16.0 * b0 * b2 * phi * phi;
                float den = pow(1.0 + a1 + a2, 2.0) - 4.0 * (a1 + 4.0 * a2 + a1 * a2) * phi + 16.0 * a2 * phi * phi;

                float r = num / den;
                r = max(0, r);

                response = sqrt(r);

                // Derivative of phi with respect to frequency
                float dphi_df = 2.0 * (3.14159265 / fs) * sin(2.0 * 3.14159265 * _Frequency / fs);
                //float dphi_df = (3.14159265 / fs) * sin(3.14159265 * frequency / fs) * cos(3.14159265 * frequency / fs) *2;

                // Derivative of num and den with respect to phi
                float dnum_dphi = -4.0 * (b0 * b1 + 4.0 * b0 * b2 + b1 * b2) + 32.0 * b0 * b2 * phi;
                float dden_dphi = -4.0 * (a1 + 4.0 * a2 + a1 * a2) + 32.0 * a2 * phi;

                // Derivative of num and den with respect to frequency
                float dnum_df = dnum_dphi * dphi_df;
                float dden_df = dden_dphi * dphi_df;

                // Derivative of response with respect to frequency
                Derivative = (0.5 / response) * (den * dnum_df - num * dden_df) / (den * den);
            }



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

                
                // _Frequency = 2400.0;
                //_Q = 1;

                _Frequency = lerp(10, 24000, pow(_Frequency,4));

                /// Scaled down to fit in the shape
                _Q = 0.707 + _Q*0.48;
                
                float scaledY = i.uv.y/0.75;
                    
                // Map UV.x to frequency scale from 10 Hz to 20,000 Hz using log scale
                float logMinFreq = log10(10.0);
                float logMaxFreq = log10(22000.0);
                float frequency = pow(10.0, lerp(logMinFreq, logMaxFreq, i.uv.x)); // Logarithmic mapping

                // // Map UV.x to frequency scale from 10 Hz to 20,000 Hz
                // float frequency = lerp(10, 22000.0, i.uv.x); // Linear mapping

                // Calculate the magnitude response
                float response,responseDeriv;
                magnitudeResponse(frequency, response,responseDeriv);
                
                /// Very scuffed approch but I could find a way to keep the line consistant like so :
                /// https://iquilezles.org/articles/distance/
                float lineThickness = 1+(1* ((pow(i.uv.x,6)+i.uv.x*0.01) * abs(responseDeriv)*50000)) ;
                //float lineThickness = 1;

                float responseResult = abs(response-scaledY);
                //float responseResult = abs(response - scaledY) / sqrt(1.0 + responseDeriv * responseDeriv);

                float lineColor = 1-smoothstep(0.0, lineThickness/100, responseResult);
                
                float wavelength = i.uv.x*0.83 * 600.0;
                float3 spectrumColor = WavelengthToRGB(wavelength);

                float3 finalColor = float3(0,0,0);

                //bot colors
                finalColor += float3(spectrumColor*(-sign(scaledY-response)*0.5 + 0.5));
                //top colors
                finalColor += float3((float3(0.2,0.2,0.2)+spectrumColor)*(sign(scaledY-response)*0.5 + 0.5)*0.25 );

                finalColor += float3(lineColor,lineColor,lineColor);


                return float4(finalColor, 1.0);

            }
            ENDCG
        }
    }
}