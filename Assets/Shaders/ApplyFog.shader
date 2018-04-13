Shader "Hidden/ApplyFog"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
	}
	
	CGINCLUDE
            #include "UnityCG.cginc"      
             
            struct v2f 
            {
				float4 pos : SV_POSITION;
				float2 uv : TEXCOORD0;
				float3 ray : TEXCOORD1;
            };   
			

            uniform sampler2D FogRendertargetLinear,
                              FogRendertargetPoint,
                              _CameraDepthTexture,
                              _MainTex;
            
            uniform float4 _CameraDepthTexture_TexelSize,
                   _MainTex_TexelSize;
            
            float DepthThreshold = 1;
            
            uniform float4x4  InverseViewMatrix,                         
                              InverseProjectionMatrix;	                       
            
            v2f vert(appdata_img v ) 
            {
                v2f o; 
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord;
                
                return o;
            }
        /*
            void UpdateNearestSample(	inout float MinDist,
                                        inout float2 NearestUV,
                                        float Z,
                                        float2 UV,
                                        float ZFull
                                        )
            {
                float Dist = abs(Z - ZFull);
                if (Dist < MinDist)
                {
                    MinDist = Dist;
                    NearestUV = UV;
                }
            }
            */
			
			/*                
            float4 GetNearestDepthSample(float2 uv)
            {
                //read full resolution depth
                float ZFull = Linear01Depth( SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, uv) );
        
                //find depth texture texel size
                const float2 TexelSize = 2.0 * _CameraDepthTexture_TexelSize.xy;
                const float depthTreshold =  DepthThreshold ;
                
                float2 lowResUV = uv; 
                
                float MinDist = 1.e8f;
                
                float2 UV00 = lowResUV - 0.5 * TexelSize;
                float2 NearestUV = UV00;
                float Z00 = Linear01Depth( SAMPLE_DEPTH_TEXTURE( _CameraDepthTexture, UV00) );   
                UpdateNearestSample(MinDist, NearestUV, Z00, UV00, ZFull);
                
                float2 UV10 = float2(UV00.x+TexelSize.x, UV00.y);
                float Z10 = Linear01Depth( SAMPLE_DEPTH_TEXTURE( _CameraDepthTexture, UV10) );  
                UpdateNearestSample(MinDist, NearestUV, Z10, UV10, ZFull);
                
                float2 UV01 = float2(UV00.x, UV00.y+TexelSize.y);
                float Z01 = Linear01Depth( SAMPLE_DEPTH_TEXTURE( _CameraDepthTexture, UV01) );  
                UpdateNearestSample(MinDist, NearestUV, Z01, UV01, ZFull);
                
                float2 UV11 = UV00 + TexelSize;
                float Z11 = Linear01Depth( SAMPLE_DEPTH_TEXTURE( _CameraDepthTexture, UV11) );  
                UpdateNearestSample(MinDist, NearestUV, Z11, UV11, ZFull);
                
                float4 fogSample = float4(0,0,0,0);
                
		[branch]
		if (abs(Z00 - ZFull) < depthTreshold &&
		    abs(Z10 - ZFull) < depthTreshold &&
		    abs(Z01 - ZFull) < depthTreshold &&
		    abs(Z11 - ZFull) < depthTreshold )
		{
			fogSample = tex2Dlod( FogRendertargetLinear, float4(lowResUV,0,0)) ; 
		}
		else
		{
		    fogSample = tex2Dlod( FogRendertargetPoint, float4(lowResUV,0,0)) ; 
		}
                
            return fogSample;
        }
        */
            
            float4 frag(v2f input) : SV_Target 
            {			
              //  float4 fogSample = GetNearestDepthSample(input.uv);
              //  float4 linearDepthSample = Linear01Depth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, input.uv));
                
                float4 fogSample = tex2Dlod(FogRendertargetLinear, float4(input.uv,0,0));
               // float4 fogSample = tex2D(FogRendertargetLinear, input.uv);
                          
                float4 colorSample = tex2D(_MainTex, input.uv);
                
                float4 result = colorSample * fogSample.a + fogSample;
              //  float4 result = float4(colorSample.rgb * fogSample.a + fogSample,colorSample.a);
               // float4 result = colorSample * fogSample.a + fogSample * (1-fogSample.a);
                
                return result;
            }
	                              
                          
			    
	ENDCG
	SubShader
	{
		// No culling or depth
		Cull Off ZWrite Off ZTest Always

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			ENDCG
		}
	}
}
