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
	
	sampler2D _MainTex;
	uniform float4 _MainTex_TexelSize;

	sampler2D _CameraDepthTexture;
	float BlurDepthFalloff = 0.01;
		
	float2 BlurDir;
	
	v2f vert( appdata_img v ) 
	{
		v2f o; 
		o.pos = UnityObjectToClipPos(v.vertex);
		o.uv = v.texcoord;
		
		return o;
	}


	float4 frag(v2f input) : SV_Target 
	{		
		const float offset[4] = { 0, 1, 2, 3 };
		const float weight[4] = { 0.266, 0.213, 0.1, 0.036 };

		float centralDepth = Linear01Depth(tex2D(_CameraDepthTexture, input.uv));
				
		float4 result = tex2D(_MainTex, input.uv) * weight[0];		
		float totalWeight = weight[0];
		
		[unroll]
		for (int i = 1; i < 4; i++) 
		{
			float depth = Linear01Depth(tex2D(_CameraDepthTexture, (input.uv + BlurDir * offset[i] * _MainTex_TexelSize.xy )));	
			
			float w = abs(depth-centralDepth)* BlurDepthFalloff;			
			w = exp(-w*w);
		
			result += tex2D(_MainTex, ( input.uv + BlurDir * offset[i] * _MainTex_TexelSize.xy )) * w * weight[i];
			
			totalWeight += w * weight[i];
	 
          depth = Linear01Depth(tex2D(_CameraDepthTexture, (input.uv - BlurDir * offset[i] * _MainTex_TexelSize.xy )));
 
          w = abs(depth-centralDepth)* BlurDepthFalloff;
          w = exp(-w*w);
 
          result += tex2D(_MainTex, ( input.uv - BlurDir * offset[i] * _MainTex_TexelSize.xy )) * w* weight[i];
 
          totalWeight += w * weight[i];		

		}
			
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
