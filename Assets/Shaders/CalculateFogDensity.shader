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
            
            
            #define STEPS 128
            #define STEPSIZE 1/STEPS
            #define GRID_SIZE 4
            #define GRID_SIZE_SQR_RCP (1.0/(GRID_SIZE*GRID_SIZE))
            
            UNITY_DECLARE_SHADOWMAP(ShadowMap);
            
            uniform sampler2D _MainTex,
                              _CameraDepthTexture,
                              _NoiseTexture;
                              
            uniform float4    _MainTex_TexelSize,
                              _CameraDepthTexture_TexelSize;
                              
            uniform float3    _ShadowColor,
                              _LightColor,
                              _FogWorldPosition;
                              
            uniform float     _FogDensity,
                              _ScatteringCoef,
                              _ExtinctionCoef,
                              _ViewDistance,
                              _LightIntensity;
                              
            uniform float4x4  InverseViewMatrix,                   
                              InverseProjectionMatrix;
                              

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
				float2 d_sphere = float2(sdBox(p - float3(_FogWorldPosition), 20), 0.5);			
				return d_sphere;
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
            //    float3 rayDir = normalize(_WorldSpaceCameraPos.xyz-worldPos);
                float3 rayDir = normalize(worldPos-_WorldSpaceCameraPos.xyz);
              //  float rayDistance = length(_WorldSpaceCameraPos.xyz-worldPos);
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
                float4 viewZ = -viewPos.z; 
                float4 zNear = float4( viewZ >= _LightSplitsNear ); 
                float4 zFar = float4( viewZ < _LightSplitsFar ); 
                float4 weights = zNear * zFar; 
                        
                float3 litFogColour = _LightIntensity * _LightColor;
                
                float transmittance = 1;
                
                for(int i = 0 ; i < STEPS ; i++ )
                {	
                    			
                    if(transmittance < 0.01){
                        break;
                    }
                    
                    float2 distanceSample = map(currentPos); // sample distance field at current position
                    
                    if(distanceSample.x < 0.0001){ // we are inside the predefined cube
                    
                        
                      //  float2 noiseUV = currentPos.xz / TerrainSize.xz;
                        float2 noiseUV = currentPos.xz;
                        float noiseValue = saturate(2 * tex2Dlod(_NoiseTexture, float4(10*noiseUV + 0.5*_Time.xx, 0, 0)));
                        
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
    
                        float3 fColour = lerp(_ShadowColor, litFogColour, shadowTerm);
                        
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
