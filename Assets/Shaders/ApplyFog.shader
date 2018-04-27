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
            
            float4 frag(v2f input) : SV_Target 
            {			

                float4 fogSample = tex2Dlod(FogRendertargetLinear, float4(input.uv,0,0));
                float4 colorSample = tex2D(_MainTex, input.uv);
                
         //       float4 result = colorSample * fogSample.a + fogSample;
                float4 result = float4(colorSample.rgb * fogSample.a + fogSample,colorSample.a);
               // float4 result = colorSample * fogSample.a + fogSample * (1-fogSample.a);
                
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
