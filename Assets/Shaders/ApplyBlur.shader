// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Hidden/ApplyBlur" 
{
	Properties 
	{ 
		_MainTex ("", any) = "" {} 
	}
	
	CGINCLUDE
	
	#pragma shader_feature __ BILATERAL_FILTERING
	
	#include "UnityCG.cginc"
	#include "AutoLight.cginc"
	
	struct v2f 
	{
		float4 pos : SV_POSITION;
		float2 uv : TEXCOORD0;
	};
	
	sampler2D _MainTex,
	          _CameraDepthTexture;
	          
	uniform float4 _MainTex_TexelSize,
	               _BlurOffsets,
	               _BlurWeights;

	uniform float _BlurDepthFalloff;
		
	uniform float2 BlurDir; 
	
	
	 
	
	v2f vert( appdata_img v ) 
	{
		v2f o; 
		o.pos = UnityObjectToClipPos(v.vertex);
		o.uv = v.texcoord;
		
		return o;
	}


	float4 frag(v2f input) : SV_Target 
	{		

        
        // depth of the center pixel
		float centralDepth = Linear01Depth(tex2D(_CameraDepthTexture, input.uv));
				
		float4 result = tex2D(_MainTex, input.uv) * _BlurWeights[0];
				
		float totalWeight = _BlurWeights[0];
		
		[unroll]
		for (int i = 1; i < 4; i++) 
		{
		       
		    // add pixel contribution of both sides of the main pixel
			float depth = Linear01Depth(tex2D(_CameraDepthTexture, (input.uv + BlurDir * _BlurOffsets[i] * _MainTex_TexelSize.xy )));	
					
			// calculate if the pixel is on edge or not
			float w = abs(depth-centralDepth)* _BlurDepthFalloff;					  	
            w = exp(-w*w);
			
			// add to result
			result += tex2D(_MainTex, ( input.uv + BlurDir * _BlurOffsets[i] * _MainTex_TexelSize.xy )) * w * _BlurWeights[i];
			
			
			totalWeight += w * _BlurWeights[i];
	 
            depth = Linear01Depth(tex2D(_CameraDepthTexture, (input.uv - BlurDir * _BlurOffsets[i] * _MainTex_TexelSize.xy )));
     		
            w = abs(depth-centralDepth)* _BlurDepthFalloff;			
            w = exp(-w*w);
     
            result += tex2D(_MainTex, ( input.uv - BlurDir * _BlurOffsets[i] * _MainTex_TexelSize.xy )) * w * _BlurWeights[i];
     
            totalWeight += w * _BlurWeights[i];		

		}
			
		// normalize the result according to the total weight
		return result / totalWeight;
	}
	
	ENDCG
	SubShader 
	{
		 Pass 
		 {
			  ZTest Always Cull Off ZWrite Off

			  CGPROGRAM
			  #pragma vertex vert
			  #pragma fragment frag
			  ENDCG
		  }
	}
	Fallback off
}
