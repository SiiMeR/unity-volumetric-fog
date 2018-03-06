// Upgrade NOTE: replaced '_World2Object' with 'unity_WorldToObject'

// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'
// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'
// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Custom/ExperimentalFog" {
	Properties
	{
		_Radius("Radius", Range(0,3)) = 1
		_SpecularPower("Specular Power", Range(0,1)) = 0.5
		_Gloss("Gloss", Range(0,1)) = 0.5
		_Centre("Centre", float) = 0
		_Color("Color", Color) = (1,1,0,1)
	}
		SubShader
	{

		Tags {"Queue"="Transparent" "RenderType"="Transparent"}
		LOD 100

		ZWrite Off
		Blend SrcAlpha OneMinusSrcAlpha
		Pass
	{
		CGPROGRAM
	#pragma vertex vert
	#pragma fragment frag

	#include "UnityCG.cginc"
	#include "CellNoise.cginc"

    uniform sampler3D _FogTex;
    uniform sampler2D _CameraDepthTexture;
    float4 _MainTex_TexelSize;
	sampler2D _MainTex;
	float _Radius;
	float _Centre;
	fixed4 _Color;
	float _SpecularPower;
	float _Gloss;
	float3 viewDirection;

	#define STEPS 64
	#define STEP_SIZE 0.01
    #define MIN_DISTANCE 0.01

	struct appdata {
		float4 vertex : POSITION;
		float2 uv : TEXCOORD0;
		float4 ray : TEXCOORD1;
	};

	struct v2f {
		float4 vertex : SV_POSITION;
		float2 uv : TEXCOORD0;
		float3 wPos : TEXCOORD1; // World Position
	};

	v2f vert(appdata v) {
		v2f o;
		o.vertex = UnityObjectToClipPos(v.vertex);
		o.wPos = mul(unity_ObjectToWorld, v.vertex).xyz;
		
		o.uv = v.uv.xy;
		#if UNITY_UV_STARTS_AT_TOP
			if (_MainTex_TexelSize.y < 0)
				o.uv.y = 1 - o.uv.y;
		#endif		
		return o;
	}

/*    half4 Fog(half linear01Depth, half2 screenuv)
    {
                half z = linear01Depth * _CameraFarOverMaxFar;
                z = (z - _NearOverFarClip) / (1 -_NearOverFarClip);
                if (z < 0.0)
                    return half4(0, 0, 0, 1);
            
                half3 uvw = half3(screenuv.x, screenuv.y, z);
                uvw.xy += cellNoise(uvw.xy * _Screen_TexelSize.zw) * _FogTex_TexelSize.xy * 0.8;
                return tex3D(_FogTex, uvw);
    }  */
	float sphereDistance(float3 p)
	{
		return distance(p, _Centre) - _Radius;
	}
	float sdf_sphere(float3 p,float3 c, float r)
	{
		return distance(p, c) - r;
	}

	float map(float3 p)
	{
		return max
		(
			sdf_sphere(p, -float3 (1.5, 0, 0), 2), // Left sphere
			sdf_sphere(p, +float3 (1.5, 0, 0), 2)  // Right sphere
		);
	}

	#include "Lighting.cginc"

	fixed4 simpleLambert(fixed3 normal) {
		fixed3 lightDir = _WorldSpaceLightPos0.xyz;
		fixed3 lightCol = _LightColor0.rgb;
		fixed NdotL = max(dot(normal, lightDir), 0);

		fixed3 h = (lightDir - viewDirection) / 2;
		fixed s = pow(dot(normal, h), _SpecularPower) * _Gloss;
		fixed4 c;
		c.rgb = _Color * lightCol * NdotL + s;
		c.a = 0.5;
		return c;
	}

	float3 normal(float3 p) {
		const float eps = 0.01;
		return normalize(
			float3(
				sphereDistance(p + float3(eps, 0, 0)) - sphereDistance(p - float3(eps, 0, 0)),
				sphereDistance(p + float3(0, eps, 0)) - sphereDistance(p - float3(0, eps, 0)),
				sphereDistance(p + float3(0, 0, eps)) - sphereDistance(p - float3(0, 0, eps))
				)
		);
	}
	fixed4 renderSurface(float3 p) {
		float3 n = normal(p);
		return simpleLambert(n);
	}
	fixed4 raymarch(float3 position, float3 direction)
	{
		for (int i = 0; i < STEPS; i++)
		{
			float dist = sphereDistance(position);
			if (dist < MIN_DISTANCE)
			   // return fixed4(cellNoise(position.xz),0,1);

			//    return renderSurface(position);
			//    return fixed4(0,0,0,0); 
		//	    return tex3Dlod(_FogTex, fixed4(position,0));
				return renderSurface(position); 

			position += dist * direction;
		}
        
      //  return tex2D(_MainTex, position.xz);
		return fixed4(1, 1, 1, 1); // White
	}

	fixed4 frag(v2f i) : SV_Target
	{
		float3 worldPosition = _WorldSpaceCameraPos;
		viewDirection = normalize(i.wPos - _WorldSpaceCameraPos);
		
		
        float3 localPosition = mul(unity_WorldToObject, worldPosition); 
        
        float rawDepth = DecodeFloatRG(tex2D(_CameraDepthTexture, i.uv));
        half linearDepth = Linear01Depth(rawDepth);
                
        float4 wsDir = linearDepth * i.vertex;
        float3 wsPos = _WorldSpaceCameraPos + wsDir;
        
		return raymarch(wsPos, viewDirection);
	}
		ENDCG
	}
	}
}

