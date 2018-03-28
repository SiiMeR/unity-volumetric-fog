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
            #include "UnityShadowLibrary.cginc"
            
            #pragma multi_compile SHADOWS_ON SHADOWS_OFF 
            // compile 2 shaders so switching at runtime is faster
            
            UNITY_DECLARE_SHADOWMAP(ShadowMap);
            
            uniform sampler2D _MainTex,
                              _CameraDepthTexture,
                              _NoiseTexture;
                              
            uniform sampler3D _NoiseTex3D;
                              
            uniform float4    _MainTex_TexelSize,
                              _CameraDepthTexture_TexelSize,
                              _LightData;
                              
            uniform float3    _ShadowColor,
                              _LightColor,
                              _FogWorldPosition;
                              
            uniform float     _FogDensity,
                              _ScatteringCoef,
                              _ExtinctionCoef,
                              _Anisotropy,
                              _ViewDistance,
                              _LightIntensity,
                              _FogSize,
                              _InterleavedSamplingSQRSize,
                              _RaymarchSteps;
                              
            uniform float4x4  InverseViewMatrix,                   
                              InverseProjectionMatrix;
                              


            #define STEPS _RaymarchSteps
            #define STEPSIZE 1/STEPS
            #define GRID_SIZE _InterleavedSamplingSQRSize
            #define GRID_SIZE_SQR_RCP (1.0/(GRID_SIZE*GRID_SIZE))
            
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
                float4 cameraRay = mul(InverseProjectionMatrix, clipPos);
                o.ray = cameraRay / cameraRay.w;
                
                return o; 
			}
			
			
			// This is the distance field function.  The distance field represents the closest distance to the surface
			// of any object we put in the scene.  If the given point (point p) is inside of an object, we return a
			// negative answer.
			// return.x: result of distance field
			// return.y: material data for closest object
			float2 map(float3 p) {                                                                   
				float2 d_sphere = float2(sdBox(p - float3(_FogWorldPosition), _FogSize), 0.5);			
				return d_sphere;
			}		
			
	
	        // get the coefficients of each shadow cascade
			fixed4 getCascadeWeights(float z){
			
                float4 zNear = float4( z >= _LightSplitsNear ); 
                float4 zFar = float4( z < _LightSplitsFar ); 
                float4 weights = zNear * zFar; 
                
			    return weights;
			}
			
			// combines cascades to get shadowmap coordinate for later sampling of shadowmap
			fixed4 getShadowCoord(float4 worldPos, float4 weights){
			     //calculate shadow at this sample position
                float3 shadowCoord0 = mul(unity_WorldToShadow[0], worldPos).xyz; 
                float3 shadowCoord1 = mul(unity_WorldToShadow[1], worldPos).xyz;                      
                float3 shadowCoord2 = mul(unity_WorldToShadow[2], worldPos).xyz; 
                float3 shadowCoord3 = mul(unity_WorldToShadow[3], worldPos).xyz;
                        
                // float4 shadowCoord = float4(shadowCoord0 * weights[0] + shadowCoord1 * weights[1] + shadowCoord2 * weights[2] + shadowCoord3 * weights[3],1); 
                float4 shadowCoord = float4(shadowCoord0 * weights.x + shadowCoord1 * weights[1] + shadowCoord2 * weights[2] + shadowCoord3 * weights[3],1); 
                
                return shadowCoord;            
			} 
			
			// use Henyey-Greenstein phase function to approximate Mie scattering(GPU PRO 5)
			fixed4 getHenyeyGreenstein(float3 lightPos, float3 cameraPos){
			
			    float n = 1 - _Anisotropy;
			    float c = dot(lightPos,cameraPos);
			    float d = 1 + _Anisotropy * _Anisotropy - 2*_Anisotropy * c;
			    
			    return n * n / (4 * 3.1415 * pow(d, 1.5));
			}

			
			fixed4 frag (v2f i) : SV_Target
			{
               // read low res depth and reconstruct world position
                float depth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.uv);
                
                //linearise depth		
                float lindepth = Linear01Depth (depth);
                
                float linearEyeDepth = LinearEyeDepth(depth);
                
                //get view and then world positions		
                float4 viewPos = float4(i.ray.xyz * lindepth,1);
                
                float3 worldPos = mul(InverseViewMatrix, viewPos).xyz;	
                           
                // ray direction in world space
                float3 rayDir = normalize(worldPos-_WorldSpaceCameraPos.xyz);
                float rayDistance = length(worldPos-_WorldSpaceCameraPos.xyz);
                
                //calculate step size for raymarching
                float stepSize = rayDistance * STEPSIZE;
                
                //raymarch from the world point to the camera
              //  float3 currentPos = worldPos.xyz;
                float3 currentPos = _WorldSpaceCameraPos.xyz;
                        
                // Calculate the offsets on the ray according to the interleaved sampling pattern
                float2 interleavedPos = fmod( float2(i.pos.x, _CameraDepthTexture_TexelSize.w - i.pos.y), GRID_SIZE );		
                float rayStartOffset = ( interleavedPos.y * GRID_SIZE + interleavedPos.x ) * ( STEPSIZE * GRID_SIZE_SQR_RCP ) ;
                currentPos += rayStartOffset * rayDir.xyz;
                
                float3 result = 0;
                
                //calculate weights for cascade split selection             
                float4 weights = getCascadeWeights(-viewPos.z);
                
                float3 litFogColour = _LightIntensity * _LightColor;
                
                float transmittance = 1;
                
                for(int i = 0 ; i < STEPS ; i++ )
                {	
                    			
                    if(transmittance < 0.01){
                        break;
                    }
                    
                    float2 distanceSample = map(currentPos); // sample distance field at current position
                    
                    if(distanceSample.x < 0.0001){ // we are inside the predefined cube
                    

                        float2 noiseUV = currentPos.xz;
                     //   float3 noiseUV = currentPos.xyz;
                      //  float noiseValue = saturate(2 * tex3Dlod(_NoiseTex3D, float4(10 * noiseUV + 0.5 * _Time.xxx, 0)));
                     
                        float noiseValue = saturate(2 * tex2Dlod(_NoiseTexture, float4(10*noiseUV + 0.5*_Time.xx, 0, 0)));
                        
                        //modulate fog density by a noise value to make it more interesting
                        float fogDensity = noiseValue * _FogDensity;
            
                        float scattering =  _ScatteringCoef * fogDensity;
                        float extinction = _ExtinctionCoef * fogDensity;
                        
                         //calculate transmittance by applying Beer law
                        transmittance *= exp( -extinction * stepSize);
                        
                        
                              
#if SHADOWS_ON
                        float4 shadowCoord = getShadowCoord(float4(currentPos,1), weights);
                        
                        //do shadow test and store the result				
                        float shadowTerm = UNITY_SAMPLE_SHADOW(ShadowMap, shadowCoord);				
                    
                        //use shadow term to lerp between shadowed and lit fog colour, so as to allow fog in shadowed areas
                        float3 fColour = lerp(_ShadowColor, litFogColour, shadowTerm);                                            
#endif

#if SHADOWS_OFF
                        float3 fColour = litFogColour;   
#endif

                      //  float3 lightDir = normalize(currentPos - _LightData.xyz);
                      //  float HGfn = getHenyeyGreenstein(lightDir, currentPos) * _ScatteringCoef;
                        
                        
                        //accumulate light
                        result += (scattering * transmittance * stepSize) * fColour;
        
                    }
                    
                    //raymarch along the ray
                    currentPos += rayDir * stepSize;
                }
                                
                return float4(result, transmittance);        

                } 
				
	
	ENDCG
	
	SubShader
	{
		// No culling or depth
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
