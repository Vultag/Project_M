Shader "Unlit/FlowfieldShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma require 2darray
            #pragma multi_compile _ DOTS_INSTANCING_ON
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"
            
            #define PI 3.14159265359

            struct VertexInput
            {
                float4 pos : POSITION0;
                float4 tex : TEXCOORD0;
                #if UNITY_ANY_INSTANCING_ENABLED
                uint instanceID : INSTANCEID_SEMANTIC;
                #endif
            };

            struct v2f
            {
                float3 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                #if UNITY_ANY_INSTANCING_ENABLED
                uint instanceID : CUSTOM_INSTANCE_ID;
                #endif
            };

            #ifdef DOTS_INSTANCING_ON
                UNITY_DOTS_INSTANCING_START(MaterialPropertyMetadata)
                UNITY_DOTS_INSTANCING_END(MaterialPropertyMetadata)
            #endif

            v2f vert (VertexInput v)
            {
                v2f o;

                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);

                o.vertex = mul(UNITY_MATRIX_MVP, v.pos);
               
                // Texture uvs
                o.uv.xy = v.tex.xy;

                return o;
            }

            sampler2D _MainTex;
            struct FlowfieldCellGPU
            {
                float ArrowRotation;
                float Cost;
                float IsBlocked;
                float InLineOfSight;
            };
            StructuredBuffer<FlowfieldCellGPU> _FlowfieldCellBuffer;
            float4 _FlowfieldPosSize;

            float2 RotateUV(float2 uv, float radian)
            {
                float s = sin(radian);
                float c = cos(radian);

                // Rotate from center
                uv -= float2(0.5,0.5);

                // Rotate
                float2 rotatedUV;
                rotatedUV.x = uv.x * c - uv.y * s;
                rotatedUV.y = uv.x * s + uv.y * c;

                // Translate back
                rotatedUV += float2(0.5,0.5);

                return rotatedUV;
            }

            half4 frag (v2f i) : SV_Target
            {
                float gridSizeX = _FlowfieldPosSize.z;
                float gridSizeY = _FlowfieldPosSize.w;

                float cellBorder = 0.1;

                
                int gridX = floor(i.uv.x*gridSizeX);
                int gridY = floor(i.uv.y*gridSizeY);
                FlowfieldCellGPU flowfieldcell = _FlowfieldCellBuffer[gridX + gridY*gridSizeX];

                float2 gridUVs = float2((i.uv.x*gridSizeX)%1,(i.uv.y*gridSizeY)%1);
                float2 cell = float2(abs(gridUVs.x-0.5)*2,abs(gridUVs.y-0.5)*2);
                cell = float2((cell.x-(1-cellBorder))/cellBorder,(cell.y-(1-cellBorder))/cellBorder);

                float2 arrowRotatedUVs = RotateUV(gridUVs,flowfieldcell.ArrowRotation+PI*0.5f);

                float grid = max(0,max(cell.x,cell.y))+(tex2D(_MainTex, arrowRotatedUVs)*(1-flowfieldcell.InLineOfSight));

                //grid = grid * (1-flowfieldcell.IsBlocked);

                return float4((flowfieldcell.Cost/gridSizeX),1-(flowfieldcell.Cost/gridSizeX),0,grid);
                //return tex2D(_MainTex, i.uv);

            }
            ENDHLSL
        }
    }
}