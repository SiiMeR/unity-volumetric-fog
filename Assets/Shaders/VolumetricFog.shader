// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Upgrade NOTE: replaced '_World2Object' with 'unity_WorldToObject'

// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Custom/VolumetricFog"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_Color("Color", Color) = (1,1,0,1)
		_NoiseTex("Fog Texture", 2D) = "black" {}
		
	}
	SubShader
	{
	
	//    Tags {"Queue" = "transparent" "RenderType"="transparent"}
		// No culling or depth buffer writes
		Cull Off ZWrite Off ZTest Always
	//	Blend SrcAlpha OneMinusSrcAlpha
		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			// compute shader support
			#pragma target 5.0

			#include "UnityCG.cginc"
			#include "CellNoise.cginc"
			
			uniform float3 _SunDirColor;
			uniform float3 _SunOppositeColor;
			
			sampler2D _MainTex;
			sampler2D _NoiseTex;
			uniform sampler3D _FogTex;
			
			#define STEPS 64
			#define STEP_SIZE 1 / STEPS
			#define MIN_DISTANCE 0.01
			
            fixed4 _Color;
            float3 viewDirection;
            
            
            
			uniform sampler2D _CameraDepthTexture;

            uniform float4 _MainTex_TexelSize;
            uniform float4 _FogTex_TexelSize;
            uniform float4 _Screen_TexelSize;
            
            uniform float _CameraFarOverMaxFar;
            uniform float _NearOverFarClip;
            

			struct VertIn
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				float4 ray : TEXCOORD1;
			};

			struct VertOut
			{
				float4 vertex : SV_POSITION;
				float2 uv : TEXCOORD0;
				float2 uv_depth : TEXCOORD1;
				float4 interpolatedRay : TEXCOORD2;
			};
			
			
			VertOut vert(VertIn v)
			{
				VertOut o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv.xy;
				o.uv_depth = v.uv.xy;

				#if UNITY_UV_STARTS_AT_TOP
				if (_MainTex_TexelSize.y < 0)
					o.uv.y = 1 - o.uv.y;
				#endif				

				o.interpolatedRay = v.ray;
				
				// Experimental
			//	o.localPosition = v.vertex.xyz;

				return o;
			}

			
		/*	struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				
			};
*/
	/*		struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 pos : SV_POSITION;
                float4 ray : TEXCOORD1;    			
				float4 vertex : SV_POSITION;
				float3 wPos : TEXCOORD0; // world pos
				float3 lPos : TEXCOORD1; // local pos
				float2 uv : TEXCOORD2;
			};*/
			
     /*       v2f vert (appdata_img v)
            {
                v2f o;
                o.pos = v.vertex;
                o.pos.xy = o.pos.xy * 2 - 1;
                o.uv = v.texcoord.xy;
                
                #if UNITY_UV_STARTS_AT_TOP
                if (_ProjectionParams.x < 0)
                    o.uv.y = 1-o.uv.y;
                #endif				
                
                return o;
            }	*/		
/*
            v2f vert(appdata v) {
            	v2f o;

				half index = v.vertex.z;
				v.vertex.z = 0.1;

				o.pos = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv.xy;

				#if UNITY_UV_STARTS_AT_TOP
				if (_MainTex_TexelSize.y < 0)
					o.uv.y = 1 - o.uv.y;
				#endif

				// Get the eyespace view ray (normalized)
				o.ray = _FrustumCornersES[(int)index];
				// Dividing by z "normalizes" it in the z axis
				// Therefore multiplying the ray by some number i gives the viewspace position
				// of the point on the ray with [viewspace z]=i
				o.ray /= abs(o.ray.z);

				// Transform the ray from eyespace to worldspace
				o.ray = mul(_CameraInvViewMatrix, o.ray);


// older
                return o;
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.lPos = v.vertex.xyz;
                o.wPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
                
            }
            */
            
            float rand(float2 co) {
				float a = 12.9898;
				float b = 78.233;
				float c = 43758.5453;
				float dt = dot(co.xy, float2(a, b));
				float sn = fmod(dt, 3.14);

				return 2.0 * frac(sin(sn) * c) - 1.0;
			}
			
		/*	fixed4 raymarch(float3 ro, float3 rd, float s,fixed4 colorz) {
				fixed4 ret = fixed4(0,0,0,0);

				const int maxstep = 64;
				float t = 0; // current distance traveled along ray
				for (int i = 0; i < maxstep; ++i) {
					// If we run past the depth buffer, or if we exceed the max draw distance,
					// stop and return nothing (transparent pixel).
					// this way raymarched objects and traditional meshes can coexist.
		//			if (t >= s) {
	//					ret = fixed4(0, 0, 1, 0);
//						break;
					

					float3 p = ro + rd * t; // World space position of sample
				//	float2 d = tex3Dlod(_FogTex, float4(p,0));
				
				    //float2 d = cellNoise(p.xz);
					// If the sample > 0, we haven't hit anything yet so we should march forward
					// We step forward by distance d, because d is the minimum distance possible to intersect
					// an object (see map()).
					t += d;
				}
                
                fixed3 color = lerp(colorz.rgb, ret.rgb, ret.a);
				return ret;
}
        */
        
        /*	fixed4 raymarch(float3 position, float3 direction, float3 depth)
            {
            
                fixed4 ret = fixed4(0,0,0,0);
                float3 pos = position;
                
                for (int i = 0; i < STEPS; i++)
                {
                
                    if(ret.a > 1){
                        break;
                    }
                    
                    float s =  cellNoise(pos.xy);
                    
                    ret.a += s;
                    pos += STEP_SIZE * direction;
                }
                
                ret = ret + tex2D(_MainTex, position.xy);
                return ret; // White
            }*/
            half4 Fog(half linear01Depth, half2 screenuv)
            {
                half z = linear01Depth * _CameraFarOverMaxFar;
                z = (z - _NearOverFarClip) / (1 -_NearOverFarClip);
                if (z < 0.0)
                    return half4(0, 0, 0, 1);
            
                half3 uvw = half3(screenuv.x, screenuv.y, z);
                uvw.xy += cellNoise(uvw * _Screen_TexelSize) * _FogTex_TexelSize * 0.8;
                return tex3D(_FogTex, uvw);
            }         
                   
            fixed4 raymarch(float3 position, float3 direction, float3 depth, float4 col)
            {   
            
                //fixed4 c = color;
                fixed4 c = fixed4(0,0,0,0);
                
                float3 stepInDirection = direction * STEP_SIZE;  
                float textureSample;
                float distanceAlongRay = 0;
                float noisesample = 0;
                
                float3 pos = position;
  
                

                for (int i = 0; i < STEPS; ++i)
                {
                    if (c.a >= 0.99) {
						break;
                    }
                    
                    float3 p = pos + direction * distanceAlongRay;
                    textureSample = cellNoise(p);
                 //   textureSample = Fog(depth, p.xy);
                 //   textureSample = tex3D(_FogTex, p);
                 //   float w1 = pow(1 - textureSample, 2);              
                    
                    c.a += textureSample;
                  //  c.rgb += textureSample;
                    
                    distanceAlongRay += stepInDirection;
                }
                
             //   fixed3 color = lerp(col.rgb, c.rgb, c.a);
            //    noisesample = pow(clamp(noisesample / 2 + 0.5f, 0.0, 1.0), 2);

                
            //    return fixed4(color, 1);
                return c + col; 
            }  
            

            fixed4 frag(VertOut i) : SV_Target
            {
                half4 col = tex2D(_MainTex, i.uv);
                float rawDepth = DecodeFloatRG(tex2D(_CameraDepthTexture, i.uv_depth));
                half linearDepth = Linear01Depth(rawDepth);
                
                float4 wsDir = linearDepth * i.interpolatedRay;
                float3 wsPos = _WorldSpaceCameraPos + wsDir;
                
                half4 fog = Fog(linearDepth, i.uv);
                return raymarch(wsPos, wsDir, linearDepth, col);
             //   return raymarch(wsPos, wsDir, linearDepth);
          //      float noisesample = tex3Dlod(_FogTex, i.vertex);
           //     return tex2D(_MainTex, i.uv) + noisesample;
               // return tex3Dlod(_FogTex, i.vertex);
               // return tex2D(_NoiseTex, i.uv);
               // fixed4 rm = raymarch(wsPos, wsDir, linearDepth);
                
             //   return rm + fog;
       //         return col + fog;
             //   return raymarch(wsPos, wsDir, linearDepth);
              //  return tex2D(_MainTex, i.uv) * fog.a + fog;
                
              //  return i.pos;
            
           /*     fixed4 color = tex2D(_MainTex, i.uv);
                float3 worldPosition = i.uv;
                float3 localPosition = i.lPos;
                float3 localPosition = float4(i.lPos.x + cos(_Time.y) * 0.1,
									   i.lPos.y + (_Time.y  * 2.5),
									   i.lPos.z + cos(_Time.y  / 1.1 + 1) * 0.1 + 2,
									   _Time.y * 0.15);
									   
									   
                
                viewDirection = normalize(worldPosition - _WorldSpaceCameraPos);
                
                return raymarch(i.lPos, viewDirection, localPosition,color);*/
            }
    
		/*	fixed4 frag (v2f i) : SV_Target
			{
			    fixed4 col = tex2D(_MainTex, i.uv);
				// ray direction
				float3 rd = normalize(i.ray.xyz);
				// ray origin (camera position)
				float3 ro = i.pos;

				float2 duv = i.uv;
				#if UNITY_UV_STARTS_AT_TOP
				if (_MainTex_TexelSize.y < 0)
					duv.y = 1 - duv.y;
				#endif

				// Convert from depth buffer (eye space) to true distance from camera
				// This is done by multiplying the eyespace depth by the length of the "z-normalized"
				// ray (see vert()).  Think of similar triangles: the view-space z-distance between a point
				// and the camera is proportional to the absolute distance.
				float depth = LinearEyeDepth(tex2D(_CameraDepthTexture, duv).r);
				depth *= length(i.ray);
			//	depth *= length(i.ray);
    
            //    float3 viewDirection = normalize(i.wPos - _WorldSpaceCameraPos);
			//	fixed3 col = tex2D(_NoiseTex,i.uv);
              //  fixed4 add = raymarch(ro, rd, depth);
                return raymarch(_CameraWS,viewDirection,depth,col);
				// Returns final color using alpha blending
		//		return fixed4((1.0 - add.w) + add.xyz * add.w,1.0);
			}*/
			ENDCG
		}
	}
}
