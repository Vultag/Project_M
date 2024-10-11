Shader "Unlit/test"

{
    Properties
    {
        _LineColor ("Line Color", Color) = (1, 0, 0, 1) // Default line color (Red)
        _LineWidth ("Line Width", Range(0.0, 1.0)) = 0.01 // Default line width (1%)
        _BackgroundColor ("Background Color", Color) = (0, 0, 0, 0) // Default background color (Transparent)
        _Frequency ("Center Frequency", Range(19.5, 24000)) = 24000 // Default center frequency
        _Q ("Q Factor", Range(0, 1)) = 0 // Default quality factor
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            float _Frequency; // Center frequency in Hz
            float _Q; // Q Factor
            float4 _LineColor; // Color of the line
            float _LineWidth; // Width of the line
            float4 _BackgroundColor; // Background color

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

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

            // Function to calculate the slope of the response curve
            // float calculateDerivative(float response1,float frequency, float deltaFrequency)
            // {
            //     float response2;
            //     magnitudeResponse(frequency+deltaFrequency, response2);
            //     return abs(response2 - response1) / deltaFrequency;
            // }
         
            fixed4 frag(v2f i) : SV_Target
            {

                _Frequency = 24000.0;
                _Q = 1;


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


                float3 finalColor = float3(0,0,0);
                
                finalColor += float3(lineColor,lineColor,lineColor);

                return float4(finalColor, 1.0);

            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}