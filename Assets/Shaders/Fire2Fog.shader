// Upgrade NOTE: replaced '_World2Object' with 'unity_WorldToObject'

Shader "Unlit/Fire2Fog"
{
	Properties
	{
		_Freq("Frequency", Range(0.0, 50.0)) = 4.4


		_Strength("Strength", Range(0, 1.0)) = 0.7
		_StrengthMultiplier("Strength Multiplier", Range(1.0, 30.0)) = 15.0
		_FogColor("Fog Color", Color) = (0.4, 0.4, 0.4, 1)
		[Toggle] _RandomOffset("Random Offset Toggle", Range(0.0, 1.0)) = 1.0
	}
	SubShader
	{
		Tags { "Queue" = "Transparent" }
		LOD 100
        
		Cull Back 
	//	Blend SrcAlpha One
		ZTest Always
		ZWrite Off

		Pass
		{
			CGPROGRAM
			
			// compute shader support
			#pragma target 5.0
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"

			// https://github.com/ashima/webgl-noise
			#include "noiseSimplex.cginc"
			#include "CellNoise.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				float4 ray : TEXCOORD1;
			};

			struct v2f {
				float4 pos : SV_POSITION;
				float4 interpolatedRay : TEXCOORD0;
				float3 wPos : TEXCOORD1;
				float3 localPos : TEXCOORD2;
				float2 uv : TEXCOORD3;
			};

			uniform float
				_Freq,
				_Strength,
				_RandomOffset,
				_StrengthMultiplier,
				_CameraFarOverMaxFar,
				_NearOverFarClip
			;

			uniform fixed4
                _FogColor
			;
			
			uniform sampler3D _FogTexture;
			
			uniform sampler2D _MainTex;
			uniform sampler2D _CameraDepthTexture;
			
            uniform float4 _Screen_TexelSize;
            uniform float4 _MainTex_TexelSize;
            uniform float4 _FogTexture_TexelSize;
            
            #define STEPS 64
			#define STEP_SIZE 1.73205 / STEPS
			
			half4 Fog(half linear01Depth, half2 screenuv)
            {
                half z = linear01Depth * _CameraFarOverMaxFar;
                z = (z - _NearOverFarClip) / (1 -_NearOverFarClip);
                if (z < 0.0)
                    return half4(0, 0, 0, 1);
            
                half3 uvw = half3(screenuv.x, screenuv.y, z);
                uvw.xy += cellNoise(uvw * _Screen_TexelSize) * _FogTexture_TexelSize * 0.8;
                return tex3D(_FogTexture, uvw);
            }    
			
			v2f vert (appdata v)
			{
				v2f o;

				o.localPos = v.vertex.xyz;
				o.pos = UnityObjectToClipPos(v.vertex);
				o.wPos = mul(unity_ObjectToWorld, v.vertex).xyz;

				o.uv = ComputeScreenPos(o.pos);
				
				#if UNITY_UV_STARTS_AT_TOP
                    if (_MainTex_TexelSize.y < 0)
                        o.uv.y = 1 - o.uv.y;
				#endif	
				
				o.interpolatedRay = v.ray;

				return o;
			}

			float rand(float2 co)
			{
				float a = 12.9898;
				float b = 78.233;
				float c = 43758.5453;
				float dt = dot(co.xy, float2(a, b));
				float sn = fmod(dt, 3.14);

				return 2.0 * frac(sin(sn) * c) - 1.0;
			}

			float getRandomOffsetAmount(float2 uv) {
				return abs(rand(_Time.zw + uv)) * _RandomOffset;
			}

			// Remaps value from one range to other range, e.g. 0.2 from range (0.0, 0.4) to range (0.0, 1.0) becomes 0.5 
			float remap(float value, float original_min, float original_max, float new_min, float new_max)
			{
				return new_min + (((value - original_min) / (original_max - original_min)) * (new_max - new_min));
			}

			float sampleFog(float4 pos, float height)
			{
			    float detailSample = abs(tex3D(_FogTexture, pos * _Freq)) / 3.0;
			//	float detailSample = abs(cellNoise(pos * _Freq) * 2.0 + cellNoise(pos * _Freq * 2.0)) / 3.0;

                float shapeSample = tex3D(_FogTexture, pos * _Freq * 0.7) + tex3D(_FogTexture, pos.xyz * _Freq * 0.3);// + float3(detailSample, detailSample, detailSample)); 
			//	float shapeSample = cellNoise(pos.xyz * _Freq * 0.7) + cellNoise(pos.xyz * _Freq * 0.3 + float3(detailSample, detailSample, detailSample) * _DistortWithDetailNoise);
			//	shapeSample *= _FireShapeMultiplier;

				//float fireSample = max(0.00, shapeSample) - max(detailSample - 0.8, 0.0);
				float fogSample = remap(shapeSample, detailSample, 1.0, 0.0, 1.0);

	//			float heightFadeOut = height * _FireFadeOutTreshold;
	//			float heightFadeIn =  pow(1.0 - height, _ThickFireHeight);

	//			fogSample -= heightFadeOut;
	//			fogSample += heightFadeIn;

				fogSample = max(0.0, fogSample) * pow(_Strength, 2);

				return fogSample;
			}



			fixed4 raymarch(float4 start, float4 direction, float randomOffsetAmount)
			{
				float stepSize = STEP_SIZE;
				fixed4 c = fixed4(_FogColor.rgb, 0);
				float4 p = start;
				float4 direcionStep = direction * stepSize;

				float4 randomOffset = direcionStep * randomOffsetAmount;
				p +=  randomOffset;
				
			//	float4 timeOffset = float4(cos(_Time.x * -_Speed * _WobbleSpeed), _Time.w * -_Speed * _UpwardsSpeed / _Freq, cos(_Time.x * -_Speed  * _WobbleSpeed * 0.9 + 1.0) + 2.0,  _Time.x * -_Speed * _DistortionSpeed);

	//			float smokeLerpConstant = _SmokeHeight * stepSize * _SmokeStrength * saturate((1.0 - dot(direction.xyz, float3(0.0, 1.0, 0.0)))); // the dot product makes it so when looking from bottom or top, it looks correct.

				for (int i = 0; i < STEPS; i++)
				{
					float height = max(p.y * 2 + 1, 0.0);
					float fogSample = sampleFog(p, height);

					float4 particle = float4((_FogColor).rgb, fogSample);

					particle.rgb *= particle.a;

					c = (1.0 - c.a)  * particle * min(1.0, stepSize * _StrengthMultiplier) + c;
				//	c.rgb = lerp(c.rgb, _SmokeColor, saturate(height *smokeLerpConstant)); // change color based on height, maybe even could try multi colored gradients

					// Old color way
					//c.a += fogSample * _ParticleAlpha * stepSize;
					//  //c.rgb += (_LightColor + _ThirdColor) * fogSample * stepSize; // this doesn't even seem to do anything

					// c.rgb = lerp(c.rgb, _SmokeColor, saturate(height * _SmokeHeight * stepSize * _SmokeStrength)); // change color based on height, maybe even could try multi colored gradients


				//	c.rgb += mad(_Time, 2.0, -0,5) / 255;
					if (c.a >= 0.99 || abs(p.x) > 0.5027 || abs(p.y) > 0.5027 || abs(p.z) > 0.5027) {
						break;
					}


					p += direcionStep;
				}
				return c;
			}
			
			fixed4 frag(v2f i) : SV_Target
			{
				//float ns = cellNoise(i.srcPos) / 2 + 0.5f;
				//return float4(ns, ns, ns, ns);
				//float ns = pow(1 - max(i.localPos.y * 2 + 1, 0.0), 5);
				//return float4(ns, ns, ns, 1.0);
				//_Time.y = 0;
				float randomOffsetAmount = getRandomOffsetAmount(i.uv);
				
				float4 color = raymarch(float4(i.localPos, 0.0), float4(normalize(i.wPos - _WorldSpaceCameraPos), 0.0), randomOffsetAmount);

             /*   float rawDepth = DecodeFloatRG(tex2D(_CameraDepthTexture, i.uv));
                half linearDepth = Linear01Depth(rawDepth);
                
                float4 wsDir = linearDepth * i.interpolatedRay;
                float3 wsPos = _WorldSpaceCameraPos + wsDir;
                
                half4 fog = Fog(linearDepth, i.uv);*/

				// https://stackoverflow.com/questions/944713/help-with-pixel-shader-effect-for-brightness-and-contrast
				//color.rgb /= color.a;
				// Apply contrast.
			//	color.rgb = ((color.rgb - 0.5f)) + 0.5f;

                return color;;
				// Return final pixel color.
				//color.rgb *= color.a;
                //+ tex2D(_MainTex, i.uv)
               // return tex2D(_MainTex, i.uv) * fog.a + fog;
				//return saturate(color) ;
			}
			ENDCG

		}

	}
}
