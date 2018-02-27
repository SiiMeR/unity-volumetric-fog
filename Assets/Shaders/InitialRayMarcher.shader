
Shader "Hidden/InitialRayMarcher"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
	}
	SubShader
	{
		// No culling or depth
		Cull Off ZWrite Off ZTest Always

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

            // support for compute shaders
			#pragma target 5.0 

			#include "UnityCG.cginc"
			#include "DistanceFunc.cginc"
			#include "CellNoise.cginc"
			
			#define STEPS 64
			
			uniform sampler2D _CameraDepthTexture;
			uniform sampler2D _MainTex;
			uniform sampler3D _FogTex;
			
			uniform float _DrawDistance;
			
			uniform float3 _LightDir;
			
			uniform float4 _MainTex_TexelSize;
			uniform float4 _CameraWS;

			uniform float4x4 _CameraInvViewMatrix;
			uniform float4x4 _FrustumCornersES;

			struct appdata
			{
				// Remember, the z value here contains the index of _FrustumCornersES to use
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float4 pos : SV_POSITION;
				float2 uv : TEXCOORD0;
				float3 ray : TEXCOORD1;
			};

			v2f vert (appdata v)
			{
				v2f o;
				
				// Index passed via custom blit function in RaymarchGeneric.cs
				half index = v.vertex.z;
				v.vertex.z = 0.1;
				
				o.pos = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv.xy;
				
				#if UNITY_UV_STARTS_AT_TOP
				if (_MainTex_TexelSize.y < 0)
					o.uv.y = 1 - o.uv.y;
				#endif

				// Get the eyespace view ray (normalized)
				o.ray = _FrustumCornersES[(int)index].xyz;
				// Dividing by z "normalizes" it in the z axis
				// Therefore multiplying the ray by some number i gives the viewspace position
				// of the point on the ray with [viewspace z]=i
				o.ray /= abs(o.ray.z);
                
				// Transform the ray from eyespace to worldspace
				o.ray = mul(_CameraInvViewMatrix, o.ray);

				return o;
			}

			// This is the distance field function.  The distance field represents the closest distance to the surface
			// of any object we put in the scene.  If the given point (point p) is inside of an object, we return a
			// negative answer.
			// return.x: result of distance field
			// return.y: material data for closest object
			float2 map(float3 p) {                                                                   
				float2 d_sphere = float2(sdBox(p - float3(3,2,0), 10), 0.5);			
				return d_sphere;
			}
            
            float rand(float2 co){
            
				float a = 12.9898;
				float b = 78.233;
				float c = 43758.5453;
				float dt = dot(co.xy, float2(a, b));
				float sn = fmod(dt, 3.14);

				return 2.0 * frac(sin(sn) * c) - 1.0;
            }           
            float randomOffset(float uv){
                return abs(rand(_Time.zw + uv));
            }
            
			// Raymarch along given ray
			// ro: ray origin
			// rd: ray direction
			// s: unity depth buffer
			// color: original pixel color from _MainTex
			fixed4 raymarch(float3 ro, float3 rd, float s, float3 color) {
			
			    // http://www.iquilezles.org/www/articles/fog/fog.htm
			    fixed3 fogColor = fixed3(0.6,0.6,0.6);
			    float fogAmount = 1.0 - exp(-s*0.05);
			    
				fixed4 ret = fixed4(fogColor,0);
                
				float t = 0; // current distance traveled along ray
				float stepsTaken = 0;
				float experimentalStepSize = 1/STEPS;
				
				for (int i = 0; i < STEPS; ++i) {
				    stepsTaken += 1;
					// If we run past the depth buffer, or if we exceed the max draw distance,
					// stop and return nothing (transparent pixel).
					// this way raymarched objects and traditional meshes can coexist.
					if (t >= s || t > _DrawDistance) {
						ret = fixed4(0, 0, 0, 0);
						break;
					}

					float3 p = ro + rd * t; // World space position of sample
					float2 d = map(p);		// Sample of distance field (see map())

					// If the sample <= 0, we have hit something (see map()).
					if (d.x < 0.001) { // inside cube
					
						float4 particleSample = tex3D(_FogTex, p);
                        ret.a += 0.07;
                        
					    t += experimentalStepSize;
					}
					else { // not in volume yet, march forward the distance to closest object
					    t += d;
					}
				    
				               
                    if(ret.a > 0.99) break;
			
				}
				// now find color
				
				
				ret.rgb = lerp(color, ret.rgb,  fogAmount);
				

				return ret;
			}


			fixed4 frag (v2f i) : SV_Target
			{
				// ray direction
				float3 rd = normalize(i.ray.xyz);
				// ray origin (camera position)
				float3 ro = _CameraWS;
				
				float2 duv = i.uv;
				#if UNITY_UV_STARTS_AT_TOP
				if (_MainTex_TexelSize.y < 0)
					duv.y = 1 - duv.y;
				#endif

				// Convert from depth buffer (eye space) to true distance from camera
				// This is done by multiplying the eyespace depth by the length of the "z-normalized"
				// ray (see vert()).  Think of similar triangles: the view-space z-distance between a point
				// and the camera is proportional to the absolute distance.
				float nonlineardepth = tex2D(_CameraDepthTexture,duv).r;

				float depth = LinearEyeDepth(nonlineardepth);
				depth *= length(i.ray);

				fixed3 col = tex2D(_MainTex,i.uv);

				fixed4 add = raymarch(ro, rd, depth, col);

                // Returns final color using alpha blending
                fixed4 mixedcolor = fixed4(col*(1.0 - add.w) + add.xyz * add.w,1.0);
                 
				return mixedcolor;
			}
			ENDCG
		}
	}
}
