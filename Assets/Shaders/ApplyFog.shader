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
                              _CameraDepthTexture,
                              _MainTex;
            
            uniform float4    _CameraDepthTexture_TexelSize,
                              _MainTex_TexelSize;
            
            
            uniform float4x4  InverseViewMatrix,                         
                              InverseProjectionMatrix;	                       
            
            v2f vert(appdata_img v ) 
            {
                v2f o; 
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord;
                
                return o;
            }
            
            float4 frag(v2f input) : SV_Target 
            {			
                float4 fogSample = tex2D(FogRendertargetLinear, input.uv);
                float4 colorSample = tex2D(_MainTex, input.uv);
                
               // float4 result = float4(colorSample.rgb * fogSample.a + fogSample,colorSample.a);
               // float4 result = fogSample.aaaa * fogSample + (float4(1.0, 1.0, 1.0, 1.0) - fogSample.aaaa) * colorSample;
                float4 result = float4(1.0, 1.0, 1.0, 1.0) * fogSample + float4(1.0, 1.0, 1.0, 1.0) * colorSample; //additive blending
                return result;
            }
                  
			    
	ENDCG
	SubShader
	{
		// No culling or depth writes
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
