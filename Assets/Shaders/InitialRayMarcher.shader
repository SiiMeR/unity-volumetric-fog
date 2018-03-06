
Shader "Hidden/InitialRayMarcher"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }

    	CGINCLUDE

		#include "UnityCG.cginc"
        #include "noiseSimplex.cginc"
		#include "DistanceFunc.cginc"
		#include "AutoLight.cginc" 
			  
		#define STEPS 128	        
			                    
		sampler2D _MainTex;	
	    uniform half4 _MainTex_TexelSize;
	    
		uniform sampler2D _CameraDepthTexture;
		uniform float4 _CameraDepthTexture_TexelSize;
		
		uniform float _FogDensity;
		uniform float _ExtinctionCoef;
		uniform float _ScatteringCoef;
		
		uniform sampler2D _BlurTex;
	    uniform sampler2D _NoiseTex;
	    uniform sampler2D FogRendertargetLinear;

		uniform sampler3D _FogTex;
			
		uniform float _DrawDistance;
			
		uniform float3 _LightDir;
			
		uniform float4 _CameraWS;
		    
		uniform float4x4 _CameraInvViewMatrix;
		uniform float4x4 _FrustumCornersES;
	    
        float3 LightColour;
        float  LightIntensity;	    
        
        
	    UNITY_DECLARE_SHADOWMAP(ShadowMap);
	
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
		
       		
	    ENDCG        
	
	SubShader
	{  
	    Cull Off ZWrite Off ZTest Always
        
        Pass
        {
            CGPROGRAM 
            #pragma vertex fogvert
            #pragma fragment fogfrag
            
            v2f fogvert(appdata_img v)
            {
                v2f o; 
                half index = v.vertex.z;
                
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord;
                
                //transform clip pos to view space
             //   float4 clipPos = float4( v.texcoord * 2.0 - 1.0, 1.0, 1.0);
             //   float4 cameraRay = mul(InverseProjectionMatrix, clipPos);
           //     o.ray = cameraRay / cameraRay.w;
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
            
            #define NUM_SAMPLES_RCP (1.0/STEPS)
            #define GRID_SIZE 8
            #define GRID_SIZE_SQR_RCP (1.0/(GRID_SIZE*GRID_SIZE))
            
            
            fixed4 fogfrag (v2f i) : SV_Target
            {
                // read low res depth and reconstruct world position
                float depth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.uv);
                
                //linearise depth		
                float lindepth = Linear01Depth (depth);
                
                //get view and then world positions		
                float4 viewPos = float4(i.ray.xyz * lindepth,1);
                
                float3 worldPos = mul(_CameraInvViewMatrix, viewPos).xyz;	
                            
                //get the ray direction in world space, raymarching is towards the camera
                float3 rayDir = normalize(_WorldSpaceCameraPos.xyz-worldPos);
                float rayDistance = length(_WorldSpaceCameraPos.xyz-worldPos);
                
                //calculate step size for raymarching
                float stepSize = rayDistance * NUM_SAMPLES_RCP;
                
                //raymarch from the world point to the camera
                float3 currentPos = worldPos.xyz;
                        
                // Calculate the offsets on the ray according to the interleaved sampling pattern
                float2 interleavedPos = fmod( float2(i.pos.x, _CameraDepthTexture_TexelSize.w - i.pos.y), GRID_SIZE );		
                float rayStartOffset = ( interleavedPos.y * GRID_SIZE + interleavedPos.x ) * ( stepSize * GRID_SIZE_SQR_RCP ) ;
                currentPos += rayStartOffset * rayDir.xyz;
                
                float3 result = 0;
                
                //calculate weights for cascade split selection
                float4 viewZ = -viewPos.z; 
                float4 zNear = float4( viewZ >= _LightSplitsNear ); 
                float4 zFar = float4( viewZ < _LightSplitsFar ); 
                float4 weights = zNear * zFar; 
                        
                float3 litFogColour = LightIntensity * LightColour;
                
                float transmittance = 1;
                
                for(int i = 0 ; i < STEPS ; i++ )
                {					
                  //  float2 noiseUV = currentPos.xz / TerrainSize.xz;
                    float2 noiseUV = currentPos.xz;
                    float noiseValue = saturate( 2 * tex2Dlod(_NoiseTex, float4(10*noiseUV + 0.5*_Time.xx, 0, 0)));
                    
                    //modulate fog density by a noise value to make it more interesting
                    float fogDensity = noiseValue * _FogDensity;
        
                    float scattering =  _ScatteringCoef * fogDensity;
                    float extinction = _ExtinctionCoef * fogDensity;
                        
                    //calculate shadow at this sample position
                    float3 shadowCoord0 = mul(unity_WorldToShadow[0], float4(currentPos,1)).xyz; 
                    float3 shadowCoord1 = mul(unity_WorldToShadow[1], float4(currentPos,1)).xyz; 
                    float3 shadowCoord2 = mul(unity_WorldToShadow[2], float4(currentPos,1)).xyz; 
                    float3 shadowCoord3 = mul(unity_WorldToShadow[3], float4(currentPos,1)).xyz;
                    
                    float4 shadowCoord = float4(shadowCoord0 * weights[0] + shadowCoord1 * weights[1] + shadowCoord2 * weights[2] + shadowCoord3 * weights[3],1); 
                    
                    //do shadow test and store the result				
                    float shadowTerm = UNITY_SAMPLE_SHADOW(ShadowMap, shadowCoord);				
                    
                    //calculate transmittance
                    transmittance *= exp( -extinction * stepSize);
                
                    //use shadow term to lerp between shadowed and lit fog colour, so as to allow fog in shadowed areas
                    float3 shadowColor = float3(0.2,0.2,0.2);
                    
                    float3 fColour = lerp(shadowColor, litFogColour, shadowTerm);
                    
                    //accumulate light
                    result += (scattering * transmittance * stepSize) * fColour;
        
                    //raymarch towards the camera
                    currentPos += rayDir * stepSize;	
                }
                                
                return float4(result, transmittance);        

            } 
            ENDCG
        }
        
		Pass
		{
		    // No culling or depth
		    
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

            // support for compute shaders
			#pragma target 5.0 
		
			
			
            #define HEIGHTDENSITYCOEF 2.0
            #define BASEDENSITYCOEF 1.0
            
            #define E 2.71828


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
				float2 d_sphere = float2(sdBox(p - float3(3,10,0), 10), 0.5);			
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
            
            float getHeightFogDensity(float3 worldPos){
                return BASEDENSITYCOEF * pow(E, (-worldPos.y *  HEIGHTDENSITYCOEF));
            }
            
			// Raymarch along given ray
			// ro: ray origin
			// rd: ray direction
			// s: unity depth buffer
			// color: original pixel color from _MainTex
			// blur: blurred pixel info from _BlurTex
			fixed4 raymarch(float3 ro, float3 rd, float s, float3 color, float3 blur) {
			    
			    fixed3 fogShadowColor = fixed3(0.2,0.5,0.5);
			    
			    // http://www.iquilezles.org/www/articles/fog/fog.htm
			    fixed3 fogColor = fixed3(0.6,0.6,0.6);
			    float fogAmount = 1.0 - exp(-s*0.05);
			    
			    float heightFogAmount = getHeightFogDensity(ro);

				fixed4 ret = fixed4(fogColor,0);
				
				float3 result = 0;
                
				float t = 0; // current distance traveled along ray
				
	            float transmittance = 1;
	            
				float experimentalStepSize = 1/STEPS;
				
				for (int i = 0; i < STEPS; ++i) {

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
                        float particleSample = saturate(2 * tex2Dlod(_NoiseTex, float4(10 + 0.5 * _Time.xx,0,0)));
                        
                        float fogDensityAtPoint = particleSample * _FogDensity;
                        
                        float scattering = _ScatteringCoef * fogDensityAtPoint;
                        float extinction = _ExtinctionCoef * fogDensityAtPoint;
                        
                        //calculate shadow at this sample position
                        float3 shadowCoord0 = mul(unity_WorldToShadow[0], float4(p,1)).xyz; 
                        float3 shadowCoord1 = mul(unity_WorldToShadow[1], float4(p,1)).xyz; 
                        float3 shadowCoord2 = mul(unity_WorldToShadow[2], float4(p,1)).xyz; 
                        float3 shadowCoord3 = mul(unity_WorldToShadow[3], float4(p,1)).xyz;
                        
                        float4 shadowCoord = float4(shadowCoord0  + shadowCoord1  + 
                                                    shadowCoord2  + shadowCoord3 ,1);
                        
                        
                        float shadowTerm = UNITY_SAMPLE_SHADOW(ShadowMap, shadowCoord);            
                        
                        transmittance *= exp( -extinction * experimentalStepSize);
                        
                        float3 fColor = lerp(fogShadowColor, fogColor, shadowTerm);
                        
                        result += (scattering * transmittance * experimentalStepSize) * fColor;
                        
                          
                        
						//float4 particleSample = tex3D(_FogTex, p);
					//	float particleSample = snoise(p) / 255.0;
                        ret.a += 0.07;
                      //  ret.a += particleSample;
                        
					    t += experimentalStepSize;
					}
					else { // not in volume yet, march forward the distance to closest object    
					    t += d;
					}
				    
				               
                    if(ret.a > 0.99) break;
			
				}
				// now find color

				ret.rgb = lerp(color, ret.rgb,  clamp(fogAmount, 0.0,1.0)); 
				
				
				fixed3 mixedcolor = color.rgb * (1.0 - ret.a) + ret.rgb;
				
             //   return float4(mixedcolor, transmittance);
				return fixed4(mixedcolor, 1.0);
			}
			
			
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
            
            
            float4 GetNearestDepthSample(float2 uv)
            {
                //read full resolution depth
                float ZFull = Linear01Depth( SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, uv) );
        
                //find low res depth texture texel size
                const float2 TexelSize = 2.0 * _CameraDepthTexture_TexelSize.xy;
                
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
                
                
                float depthTreshold = 0.01; //
                fogSample = tex2Dlod( FogRendertargetLinear, float4(lowResUV,0,0)) ; 
              /*  [branch]
                if (abs(Z00 - ZFull) < depthTreshold &&
                    abs(Z10 - ZFull) < depthTreshold &&
                    abs(Z01 - ZFull) < depthTreshold &&
                    abs(Z11 - ZFull) < depthTreshold )
                {
                    
                }
                else
                {
                    fogSample = tex2Dlod(FogRendertargetPoint, float4(NearestUV,0,0)) ;
                }*/
                
                return fogSample;
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
				float nonlineardepth = tex2D(_CameraDepthTexture, duv).r;
				
			//	float depth = LinearEyeDepth(nonlineardepth);
			//	float depth = LinearEyeDepth(nonlineardepth);
			    float depth = Linear01Depth(nonlineardepth);
				depth *= length(i.ray);

				fixed4 col = tex2D(_MainTex,duv);
				
                fixed4 blur = tex2D(_BlurTex, duv);
                
				fixed4 add = raymarch(ro, rd, depth, col, blur);
			    	
                // Returns final color using alpha blending
                fixed4 mixedcolor = fixed4(col*(1.0 - add.w) + add.xyz * add.w,1.0);
               // return lerp(mixedcolor, half4(blur,1) * 1/5, clamp(depth ,0,1.0));  
			//	return raymarch(ro, rd, depth, col, blur);
			    
			    float4 fogSample = GetNearestDepthSample(duv);
			    
			    float4 result = col * fogSample.a + fogSample;
			    
			   	return result;
				
			}
			ENDCG
		}
	}
}
