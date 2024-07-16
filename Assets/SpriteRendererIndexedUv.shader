Shader "Instanced/SpriteRendererIndexedUv" {
    Properties {
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Cutoff ("Alpha cutoff", Range(0,1)) = 0.5
    }
     
    SubShader {
        Tags{
            "Queue"="AlphaTest"
            "IgnoreProjector"="True"
            "RenderType"="TransparentCutout"
        }
        Cull Back
        Lighting Off
        ZWrite On
        AlphaTest Greater 0
        Blend SrcAlpha OneMinusSrcAlpha
        Pass {
            CGPROGRAM
            // Upgrade NOTE: excluded shader from OpenGL ES 2.0 because it uses non-square matrices
            #pragma exclude_renderers gles
 
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 4.5
 
            #include "UnityCG.cginc"
 
            sampler2D _MainTex;
            fixed _Cutoff;
            
            struct SpriteRender
            {
                float4 UV;
                float4 Translation;
                float Scale;
                float4 Color;
            };

            struct AnimationData
            {
                int len;
                int index;
            };
            //StructuredBuffer<SpriteRender> SpriteRenders;
            // xyz is the position, w is the rotation
            StructuredBuffer<float4> translationAndRotationBuffer;
            
            StructuredBuffer<float> scaleBuffer;
            
            StructuredBuffer<float4> colorsBuffer;
            
            StructuredBuffer<float4> uvBuffer;
            
            StructuredBuffer<int> uvIndexBuffer;
            
            StructuredBuffer<int> flipBuffer;

            StructuredBuffer<int> animationIndexBuffer;

            StructuredBuffer<AnimationData> animationDataBuffer;
            struct v2f {
                float4 pos : SV_POSITION;
                float2 uv: TEXCOORD0;
                fixed4 color : COLOR0;
            };
 
            float4x4 rotationZMatrix(const float zRotRadians){
                float c = cos(zRotRadians);
                float s = sin(zRotRadians);
                float4x4 ZMatrix  = 
                    float4x4( 
                       c,  s, 0,  0,
                       -s, c, 0,  0,
                       0,  0, 1,  0,
                       0,  0, 0,  1);
                return ZMatrix;
            }
 
            v2f vert(appdata_full v, const uint instanceID : SV_InstanceID) {
                float4 translationAndRot = translationAndRotationBuffer[instanceID];
                const int uvIndex = uvIndexBuffer[instanceID];
                
                const int flip = flipBuffer[instanceID];
                const int animationIndex = animationIndexBuffer[instanceID];
                const AnimationData animationData = animationDataBuffer[instanceID];
                float4 uv = uvBuffer[uvIndex + animationIndex];
                uv.x *= flip;
                //rotate the vertex
                v.vertex = mul(v.vertex - float4(0.5, 0.5, 0,0), rotationZMatrix(translationAndRot.w));
                //scale it
                const float scale = scaleBuffer[instanceID];
               
                float3 worldPosition = translationAndRot.xyz + (v.vertex.xyz * scale);
                v2f o;
                o.pos = UnityObjectToClipPos(float4(worldPosition, 1.0f));
                // XY here is the dimension (width, height). 
                // ZW is the offset in the texture (the actual UV coordinates)
                o.uv =  v.texcoord * uv.xy + uv.zw;

                o.color = colorsBuffer[instanceID];
                return o;
            }
            
            float2 TilingAndOffset(float2 UV, float2 Tiling, float2 Offset)
            {
                return UV * Tiling + Offset;
            }
            fixed4 frag(v2f i, out float depth : SV_Depth) : SV_Target {
                fixed4 col = tex2D(_MainTex, i.uv) * i.color;
                clip(col.a - _Cutoff);
                col.rgb *= col.a;
 
                return col;
            }
 
            ENDCG
        }
    }
}
