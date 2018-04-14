// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Hidden/ApplyBlur" 
{
	Properties 
	{ 
		_MainTex ("", any) = "" {} 
	}
	
	CGINCLUDE
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
	
	
//	const float _BlurOffsets[4] = { 0,1,2,3 };
//	const float _BlurWeights[4] = { 0.266, 0.213, 0.17, 0.036 };
	
	v2f vert( appdata_img v ) 
	{
		v2f o; 
		o.pos = UnityObjectToClipPos(v.vertex);
		o.uv = v.texcoord;
		
		return o;
	}


	float4 frag(v2f input) : SV_Target 
	{		


		float centralDepth = Linear01Depth(tex2D(_CameraDepthTexture, input.uv));
				
		float4 result = tex2D(_MainTex, input.uv) * _BlurWeights[0];
				
		float totalWeight = _BlurWeights[0];
		
		[unroll]
		for (int i = 1; i < 4; i++) 
		{
			float depth = Linear01Depth(tex2D(_CameraDepthTexture, (input.uv + BlurDir * _BlurOffsets[i] * _MainTex_TexelSize.xy )));	
			
			float w = abs(depth-centralDepth)* _BlurDepthFalloff;			
			w = exp(-w*w);
		
			result += tex2D(_MainTex, ( input.uv + BlurDir * _BlurOffsets[i] * _MainTex_TexelSize.xy )) * w * _BlurWeights[i];
			
			totalWeight += w * _BlurWeights[i];
	 
            depth = Linear01Depth(tex2D(_CameraDepthTexture, (input.uv - BlurDir * _BlurOffsets[i] * _MainTex_TexelSize.xy )));
     
            w = abs(depth-centralDepth)* _BlurDepthFalloff;
            w = exp(-w*w);
     
            result += tex2D(_MainTex, ( input.uv - BlurDir * _BlurOffsets[i] * _MainTex_TexelSize.xy )) * w * _BlurWeights[i];
     
            totalWeight += w * _BlurWeights[i];		

		}
			
		return result / totalWeight;
	}
	
	ENDCG
	SubShader 
	{
		 Pass 
		 {
			//  ZTest Always Cull Off ZWrite Off
			  ZTest Always Cull Off ZWrite Off

			  CGPROGRAM
			  #pragma vertex vert
			  #pragma fragment frag
			  ENDCG
		  }
	}
	Fallback off
}
