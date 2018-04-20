Shader "Hidden/CalculateFogDensity"
{

	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
	}
	CGINCLUDE
	        
            #include "UnityCG.cginc"
            #include "AutoLight.cginc"
            #include "DistanceFunc.cginc"
           // #include "UnityShadowLibrary.cginc"
            
            // compile multiple variants of shaders so switching at runtime is faster
            
            #pragma multi_compile SHADOWS_ON SHADOWS_OFF
            #pragma shader_feature __ HEIGHTFOG
            #pragma shader_feature __ RAYLEIGH_SCATTERING
            #pragma shader_feature __ HG_SCATTERING
            #pragma shader_feature __ CS_SCATTERING
            #pragma shader_feature __ LIMITFOGSIZE
            #pragma shader_feature __ INTERLEAVED_SAMPLING
            #pragma shader_feature __ NOISE2D
            #pragma shader_feature __ NOISE3D
            #pragma shader_feature __ SNOISE
            

            UNITY_DECLARE_SHADOWMAP(ShadowMap);
            
            uniform sampler2D _MainTex,
                              _CameraDepthTexture,
                              _NoiseTexture;
                              
            uniform sampler3D _NoiseTex3D;
                              
            uniform float4    _MainTex_TexelSize,
                              _CameraDepthTexture_TexelSize;
                              
            uniform float3    _ShadowColor,
                              _LightColor,
                              _FogWorldPosition;
                              
            uniform float     _FogDensity,
                              _RayleighScatteringCoef,
                              _MieScatteringCoef,
                              _ExtinctionCoef,
                              _Anisotropy,
                              _LightIntensity,
                              _FogSize,
                              _InterleavedSamplingSQRSize,
                              _RaymarchSteps,
                              _AmbientFog,
                              _BaseHeightDensity,
                              _HeightDensityCoef;
                              
            uniform float4x4  InverseViewMatrix,                   
                              InverseProjectionMatrix;
                              

            #define STEPS _RaymarchSteps
            #define STEPSIZE 1/STEPS
            #define GRID_SIZE _InterleavedSamplingSQRSize
            #define GRID_SIZE_SQR_RCP (1.0/(GRID_SIZE*GRID_SIZE))
            
            #define e 2.71828
            #define pi 3.1415
            
			struct v2f
			{
				float4 pos : SV_POSITION;
				float2 uv : TEXCOORD0;
				float3 ray : TEXCOORD1;
			};

			v2f vert (appdata_img v)
			{
                v2f o; 
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord;
                
                //transform clip pos to view space
                float4 clipPos = float4( v.texcoord * 2.0 - 1.0, 1.0, 1.0); 
               // float4 clipPos = float4( v.texcoord * 2.0 - 1.0, 1.0, 1.0); 
                float4 cameraRay = mul(InverseProjectionMatrix, clipPos);
                
                o.ray = cameraRay / cameraRay.w;
                
                return o; 
			}
			
			
				// http://byteblacksmith.com/improvements-to-the-canonical-one-liner-glsl-rand-for-opengl-es-2-0/
	        float rand(float2 co) {
                float a = 12.9898;
                float b = 78.233;
                float c = 43758.5453;
                float dt = dot(co.xy, float2(a, b));
                float sn = fmod(dt, 3.14);
        
                return 2.0 * frac(sin(sn) * c) - 1.0;
            }
			
			// This is the distance field function.  The distance field represents the closest distance to the surface
			// of any object we put in the scene.  If the given point (point p) is inside of an object, we return a
			// negative answer.
			// return.x: result of distance field
			// return.y: material data for closest object
			float2 map(float3 p) {
			                                                               
				float2 d_box = float2(sdBox(p - float3(_FogWorldPosition), _FogSize), 0.5);			
				return d_box;
			}		
			
	        
	        // https://docs.unity3d.com/Manual/DirLightShadows.html
	        // get the coefficients of each shadow cascade
			fixed4 getCascadeWeights(float z){
			
                float4 zNear = float4( z >= _LightSplitsNear ); 
                float4 zFar = float4( z < _LightSplitsFar ); 
                float4 weights = zNear * zFar; 
                
			    return weights;
			}
			
			// combines cascades to get shadowmap coordinate for later sampling of shadowmap
			fixed4 getShadowCoord(float4 worldPos, float4 weights){
			
			 
			    float3 shadowCoord = float3(0,0,0);
			    
			    // find which cascades need sampling and then take the positions to light space using worldtoshadow
			    
			    if(weights[0] == 1){
			        shadowCoord += mul(unity_WorldToShadow[0], worldPos).xyz; 
			    }
			    if(weights[1] == 1){
			        shadowCoord += mul(unity_WorldToShadow[1], worldPos).xyz; 
			    }
			    if(weights[2] == 1){
			        shadowCoord += mul(unity_WorldToShadow[2], worldPos).xyz; 
			    }
			    if(weights[3] == 1){
			        shadowCoord += mul(unity_WorldToShadow[3], worldPos).xyz; 
			    }
			    
			  
			
			     //calculate shadow at this sample position
			     /* ALTERNATE
                float3 shadowCoord0 = mul(unity_WorldToShadow[0], worldPos).xyz; 
                float3 shadowCoord1 = mul(unity_WorldToShadow[1], worldPos).xyz;                      
                float3 shadowCoord2 = mul(unity_WorldToShadow[2], worldPos).xyz; 
                float3 shadowCoord3 = mul(unity_WorldToShadow[3], worldPos).xyz;
                           
               
                float4 shadowCoord = float4(shadowCoord0 * weights[0] + 
                                            shadowCoord1 * weights[1] + 
                                            shadowCoord2 * weights[2] +
                                            shadowCoord3 * weights[3],
                                            1); 
                */
            //    shadowCoord = mul(unity_WorldToShadow[(int)dot(weights, float4(1,1,1,1))], worldPos);
                
                return float4(shadowCoord,1);            
			} 
			
			// from unity adam demo volumetric fog
			fixed4 getHenyeyGreenstein(float cosTheta){
                float g = _Anisotropy;
                float gsq = g*g;
                float denom = 1 + gsq - 2.0 * g * cosTheta;
                denom = denom * denom * denom;
                denom = sqrt(max(0, denom));
                return (1 - gsq) / denom;
			}
			
			// from https://github.com/Flafla2/Volumetrics-Unity/blob/master/Assets/Shader/VolumetricLight.shader
			fixed4 getFixedHenyeyGreenstein(float cosTheta){
			
				float n = 1 - _Anisotropy; // 1 - g
                float c = cosTheta; // cos(x)
                float d = 1 + _Anisotropy * _Anisotropy - 2 * _Anisotropy * c; // 1 + g^2 - 2g*cos(x)
                return n * n / (4 * pi * pow(d, 1.5));
                
			
			}
			
		    fixed4 getRayleighPhase(float cosTheta){
		        return (3.0 / (16.0 * pi)) * (1 + (cosTheta * cosTheta));
		    }
		    
		    
		    // as per https://developer.nvidia.com/sites/default/files/akamai/gameworks/downloads/papers/NVVL/Fast_Flexible_Physically-Based_Volumetric_Light_Scattering.pdf
		    fixed4 mieHazy(float cosTheta){
		    
		        float cosThetaPow = pow(((1 + cosTheta) / 2),8);
		        return (1 / 4 * pi) * (0.5 + ((9/2) * cosThetaPow));
		    }
		    
		    fixed4 mieMurky(float cosTheta){
		    
		        float cosThetaPow = pow(((1 + cosTheta) / 2),32);
		        
		        return (1 / 4 * pi) * (0.5 + ((33/2) * cosThetaPow));
		    }
			
			
			// gpu pro 6 p. 224
			fixed4 getHeightDensity(float height){
			
			    float ePow = pow(e, (-height * _HeightDensityCoef));
			    
			    return _BaseHeightDensity * ePow;
			}
			
			
			// http://publications.lib.chalmers.se/records/fulltext/203057/203057.pdf p. 12
			float getCornetteShanks(float costheta){
			     float g2 = _Anisotropy * _Anisotropy;
			     
			     float term1 = (3 * ( 1 -  g2)) / (2 * (2 + g2));
			     
			     float cos2 = costheta * costheta;
			     
			     float term2 = (1 + cos2) / (pow((1 + g2 - 2 * _Anisotropy * cos2),3/2));
			     
			     return term1 * term2;
			     
			     
			
			}
			
			float getBeerLaw(float density, float stepSize){
			    return saturate(exp( -density * stepSize)); //* (1.0 - exp(-density * 2.0));		
			}
			
            float3 ExpRL = float3(6.55e-6, 1.73e-5, 2.30e-5); 
            float3 ExpHG = float3(2e-6.xxx);
			
			#include "noiseSimplex.cginc"
			
			float _noiseStrength;
			fixed4 frag (v2f i) : SV_Target
			{


                
               // read depth and reconstruct world position
                float depth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.uv);
                
                //linearise depth		
                float lindepth = Linear01Depth (depth);
                
            
              //  float lindepth = LinearEyeDepth(depth);
                
                //get view and then world positions		
                float4 viewPos = float4(i.ray.xyz * lindepth,1);
                
                float3 worldPos = mul(InverseViewMatrix, viewPos).xyz;	
                    
             
                // ray direction in world space
                float3 rayDir = normalize(worldPos-_WorldSpaceCameraPos.xyz);
                
                
                float rayDistance = length(worldPos-_WorldSpaceCameraPos.xyz);
                
                //calculate step size for raymarching
                float stepSize = rayDistance / STEPS;
             //  float stepSize = rayDistance * STEPSIZE; 
              //  float3 currentPos = worldPos.xyz;
            //    float3 currentPos = _WorldSpaceCameraPos.xyz;
            
                float3 currentPos = _WorldSpaceCameraPos.xyz;
                        
                        
#if defined(INTERLEAVED_SAMPLING)
                // Calculate the offsets on the ray according to the interleaved sampling pattern
                float2 interleavedPos = fmod( float2(i.pos.x, _CameraDepthTexture_TexelSize.w - i.pos.y), GRID_SIZE );		
              //  float2 interleavedPos = fmod(i.pos.xy, GRID_SIZE);		
                float rayStartOffset = ( interleavedPos.y * GRID_SIZE + interleavedPos.x ) * ( STEPSIZE * GRID_SIZE_SQR_RCP ) ;
                 
                currentPos += rayStartOffset * rayDir.xyz; // TODO : figure this out or remove

#else 

                currentPos +=  rayDir.xyz; 
               // if(true) return float4(currentPos + (rayDir * rayDistance) ,1);
                
#endif                
                
                
                
                float3 result = 0;
                
                //calculate weights for cascade split selection  
                float4 weights = getCascadeWeights(-viewPos.z);
                
               // if(true){ return  -viewPos.z < _LightSplitsFar;}
                float3 litFogColor = _LightIntensity * _LightColor;
             //   float3 litFogColor = _LightIntensity ;
                
                float transmittance = 1;
                
        
                for(int i = 0 ; i < STEPS ; i++ )
                {	
                    			
                    if(transmittance < 0.01){
                        break;
                    }
                    
                    float2 distanceSample = 0;
                    
#if defined(LIMITFOGSIZE)
                    distanceSample = map(currentPos); // sample distance field at current position
#endif                    

                    if(distanceSample.x < 0.0001){ // we are inside the predefined cube
                    

                        float2 noiseUV = currentPos.xz;
                        
                      //  float noiseValue = saturate(tex2Dlod(_NoiseTexture, 0.5 * float4(10*noiseUV + 0.5*rand(noiseUV), 0, 0)));
                      //  float noiseValue = saturate(tex2Dlod(_NoiseTexture, float4(0.5 * noiseUV, 0, 0)));
                     //   float noiseValue = saturate(tex3Dlod(_NoiseTex3D, float4(10 * currentPos.xyz, 0)));
                    //    float noiseValue = saturate(tex3Dlod(_NoiseTex3D, float4(snoise(currentPos), 0)));
                        float noiseValue = 0;
#if defined(SNOISE)
                        noiseValue = saturate(snoise(float4(currentPos, _Time.y)) * (i / STEPS) + 0.5);    
                        noiseValue += saturate(snoise(currentPos * 0.3) * (i / STEPS) + 0.5);
                        noiseValue *= 0.5;    
#elif defined(NOISE2D)
                        noiseValue = saturate(tex2Dlod(_NoiseTexture, float4(0.5 * noiseUV, 0, 0)));
#elif defined(NOISE3D)
                        
                        noiseValue = saturate(tex3Dlod(_NoiseTex3D, float4(10 * currentPos.xyz, 0)));
#endif
          
                        noiseValue = lerp(1, noiseValue, _noiseStrength);
                        //modulate fog density by a noise value to make it more interesting
                        float fogDensity = noiseValue * _FogDensity;
                        
#if defined(HEIGHTFOG)
                        float heightDensity = getHeightDensity(currentPos.y);  
                        fogDensity *= saturate(heightDensity);
#endif                        
                        
                        
                       // float scattering =  _ScatteringCoef * fogDensity;
                        float extinction = _ExtinctionCoef * fogDensity;
                        
                         //calculate transmittance by applying Beer law
                        transmittance *= getBeerLaw(extinction, stepSize);

                        // WSlightpos0 for directional light == light direction
                        float3 lightDir = normalize(_WorldSpaceLightPos0.xyz);
                        float3 cameraDir = normalize(_WorldSpaceCameraPos.xyz - currentPos);
                        
                        float cosTheta = dot(rayDir, lightDir);
                        
                        
                        // idea for inscattering : https://cboard.cprogramming.com/game-programming/116931-rayleigh-scattering-shader.html
                        float inScattering = 0; 
                        
#if defined(RAYLEIGH_SCATTERING)
                        float Rayleighscattering = getRayleighPhase(cosTheta) * _RayleighScatteringCoef * fogDensity;
                     //   float Rayleighscattering = getRayleighPhase(cosTheta) * ExpRL * fogDensity;
                        inScattering += Rayleighscattering;
#endif

#if defined(HG_SCATTERING)
                    //    float HGscattering = getFixedHenyeyGreenstein(cosTheta) * _MieScatteringCoef * fogDensity;
                        float HGscattering = getHenyeyGreenstein(cosTheta) * _MieScatteringCoef * fogDensity;
                       // float3 HGscattering = getHenyeyGreenstein(cosTheta) * ExpHG * fogDensity;
                
                   //    
                        inScattering += HGscattering;
                            
#elif defined(CS_SCATTERING) 
                        float CSscattering = getCornetteShanks(cosTheta) * _MieScatteringCoef * fogDensity;
                        inScattering += CSscattering;
#endif                    
                        

#if SHADOWS_ON
                        float4 shadowCoord = getShadowCoord(float4(currentPos,1), weights);
    
                        //do shadow test and store the result				
                        float shadowTerm = UNITY_SAMPLE_SHADOW(ShadowMap, shadowCoord);				

                        //use shadow term to lerp between shadowed and lit fog colour, so as to allow fog in shadowed areas
                        //add a bit of ambient fog so shadowed areas get some fog too
                        float3 fColor = lerp(_ShadowColor, litFogColor, shadowTerm + _AmbientFog);         
                                 
#endif

#if SHADOWS_OFF
                        float3 fColor = litFogColor;   
#endif
                        
                        //accumulate light
                        result += inScattering * transmittance * stepSize * fColor;
                        
                        
                    }
                    // TODO : STEP BY DISTANCE FIELD SAMPLE IF NOT IN CUBE

                    
                    //raymarch along the ray
                    currentPos += rayDir * stepSize;
                    

                }
                                
			    if(lindepth > 0.99){
					transmittance = lerp(transmittance, 1, 0.1);
				}
                return float4(result, transmittance);        

                } 
				
	
	ENDCG
	
	SubShader
	{
	    Tags {"RenderType"="Opaque"}

		// No culling or depth
		Cull Off ZWrite Off ZTest Off

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			ENDCG
		}
	}
}
