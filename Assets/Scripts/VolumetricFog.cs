using System;
using UnityEngine;
using UnityEngine.Rendering;


[ExecuteInEditMode]
//	[RequireComponent (typeof(Camera))]
	class VolumetricFog : MonoBehaviour
	{
		[SerializeField] private Shader _ApplyFogShader;
		[SerializeField] private Shader _CalculateFogShader;
		[SerializeField] private Shader _ApplyBlurShader;
		
		
		[SerializeField] private float _RaymarchDrawDistance = 40;
		[SerializeField] private Texture2D _FogTexture2D;
    
		[SerializeField] private float _FogDensityCoef = 0.3f;
		[SerializeField] private float _ScatteringCoef = 0.25f;
		[SerializeField] private float _ExtinctionCoef = 0.01f;
		[SerializeField] private Color _ShadowColor = Color.black;
		[SerializeField] private Transform SunLight;

		private Material _DownscaleDepthMaterial;//TODO
		
		private Material _ApplyBlurMaterial;
		private Material _CalculateFogMaterial;
		private Material _ApplyFogMaterial;
    
		public Material ApplyFogMaterial
		{
			get
			{
				if (!_ApplyFogMaterial && _ApplyFogShader)
				{
					_ApplyFogMaterial = new Material(_ApplyFogShader);
					_ApplyFogMaterial.hideFlags = HideFlags.HideAndDontSave;
				}

				return _ApplyFogMaterial;
			}
		}


		public Material ApplyBlurMaterial
		{
			get
			{
				if (!_ApplyBlurMaterial && _ApplyBlurShader)
				{
					_ApplyBlurMaterial = new Material(_ApplyBlurShader);
					_ApplyBlurMaterial.hideFlags = HideFlags.HideAndDontSave;
				}

				return _ApplyBlurMaterial;
			}
		}		
		
		public Material CalculateFogMaterial
		{
			get
			{
				if (!_CalculateFogMaterial && _CalculateFogShader)
				{
					_CalculateFogMaterial = new Material(_CalculateFogShader);
					_CalculateFogMaterial.hideFlags = HideFlags.HideAndDontSave;
				}

				return _CalculateFogMaterial;
			}
		}

		private Camera _CurrentCamera;
    
		public Camera CurrentCamera
		{
			get
			{
				if (!_CurrentCamera)
					_CurrentCamera = Camera.main;
				return _CurrentCamera;
			}
		}

		private CommandBuffer _AfterShadowPass;
    
		void Start()
		{
			AddLightCommandBuffer();
		}
    
		void OnDestroy()
		{
			RemoveLightCommandBuffer();
		}
		
		// based on https://interplayoflight.wordpress.com/2015/07/03/adventures-in-postprocessing-with-unity/    
		private void RemoveLightCommandBuffer()
		{
			// TODO : SUPPORT MULTIPLE LIGHTS 
			if (SunLight != null)
			{
				Light light = SunLight.GetComponent<Light>();    
			}
        
			if (_AfterShadowPass != null && GetComponent<Light>())
			{
				GetComponent<Light>().RemoveCommandBuffer(LightEvent.AfterShadowMap, _AfterShadowPass);
			}
		}

		// based on https://interplayoflight.wordpress.com/2015/07/03/adventures-in-postprocessing-with-unity/
		void AddLightCommandBuffer()
		{
			_AfterShadowPass = new CommandBuffer();
			_AfterShadowPass.name = "Volumetric Fog ShadowMap";
			_AfterShadowPass.SetGlobalTexture("ShadowMap", new RenderTargetIdentifier(BuiltinRenderTextureType.CurrentActive));

			Light light = SunLight.GetComponent<Light>();
    
			if (light)
			{
				light.AddCommandBuffer(LightEvent.AfterShadowMap, _AfterShadowPass);
			}

		}
		
		void OnRenderImage (RenderTexture source, RenderTexture destination)
		{
			if (!_FogTexture2D || 
			    !ApplyFogMaterial || !_ApplyFogShader ||
			    !CalculateFogMaterial || !_CalculateFogShader ||
			    !ApplyBlurMaterial || !_ApplyBlurShader)
			{
				Debug.Log("Not rendering image effect");
				Graphics.Blit(source, destination); // do nothing
				return;
			}

			RenderTextureFormat formatRF32 = RenderTextureFormat.RFloat;
			int lowresDepthWidth= source.width/2;
			int lowresDepthHeight= source.height/2;

			RenderTexture lowresDepthRT = RenderTexture.GetTemporary (lowresDepthWidth, lowresDepthHeight, 0, formatRF32);

		/*	//downscale depth buffer to quarter resolution
			Graphics.Blit (source, lowresDepthRT, _DownscaleDepthMaterial);

			lowresDepthRT.filterMode = FilterMode.Point;*/

			RenderTextureFormat format = RenderTextureFormat.ARGBHalf;
			int fogRTWidth= source.width;
			int fogRTHeight= source.height;

			RenderTexture fogRT1 = RenderTexture.GetTemporary (fogRTWidth, fogRTHeight, 0, format);
			RenderTexture fogRT2 = RenderTexture.GetTemporary (fogRTWidth, fogRTHeight, 0, format);

			fogRT1.filterMode = FilterMode.Bilinear;
			fogRT2.filterMode = FilterMode.Bilinear;

			Light light = GameObject.Find("Directional Light").GetComponent<Light>();


			Matrix4x4 worldViewProjection = CurrentCamera.worldToCameraMatrix * CurrentCamera.projectionMatrix;
			Matrix4x4 invWorldViewProjection = worldViewProjection.inverse;

			Shader.SetGlobalMatrix("InverseViewMatrix", CurrentCamera.cameraToWorldMatrix);
			Shader.SetGlobalMatrix("InverseProjectionMatrix", CurrentCamera.projectionMatrix.inverse);

		//	CalculateFogMaterial.SetTexture ("LowResDepth", lowresDepthRT); TODO
			CalculateFogMaterial.SetTexture ("_NoiseTexture", _FogTexture2D);
			CalculateFogMaterial.SetFloat ("_FogDensity", _FogDensityCoef);
			CalculateFogMaterial.SetFloat ("_ScatteringCoef", _ScatteringCoef);
			CalculateFogMaterial.SetFloat ("_ExtinctionCoef", _ExtinctionCoef);
			CalculateFogMaterial.SetFloat ("_ViewDistance", _RaymarchDrawDistance);
			CalculateFogMaterial.SetVector ("_LightColor", light.color.linear);
			CalculateFogMaterial.SetFloat ("_LightIntensity", light.intensity);
			CalculateFogMaterial.SetColor ("_ShadowColor", _ShadowColor);


			//render fog
			Graphics.Blit (source, fogRT1, CalculateFogMaterial);
			
			
			//blur fog, quarter resolution
			ApplyBlurMaterial.SetFloat ("BlurDepthFalloff", 0.01f);
		//	ApplyBlurMaterial.SetTexture ("LowresDepthSampler", lowresDepthRT);

			ApplyBlurMaterial.SetVector ("BlurDir", new Vector2(0,1));
			Graphics.Blit (fogRT1, fogRT2, ApplyBlurMaterial);

			//blur fog, quarter resolution
			ApplyBlurMaterial.SetVector ("BlurDir", new Vector2(1,0));
			Graphics.Blit (fogRT2, fogRT1, ApplyBlurMaterial);

			//blur fog, quarter resolution
			ApplyBlurMaterial.SetVector ("BlurDir", new Vector2(0,1));
			Graphics.Blit (fogRT1, fogRT2, ApplyBlurMaterial);
			
			//blur fog, quarter resolution
			ApplyBlurMaterial.SetVector ("BlurDir", new Vector2(1,0));
			Graphics.Blit (fogRT2, fogRT1, ApplyBlurMaterial);			
			
			//apply fog to main scene
			fogRT1.filterMode = FilterMode.Bilinear;
			ApplyFogMaterial.SetTexture ("FogRendertargetPoint", fogRT1);

			fogRT2.filterMode = FilterMode.Bilinear;
			
			ApplyFogMaterial.SetTexture ("FogRendertargetLinear", fogRT1);
			
		//	ApplyFogMaterial.SetTexture ("LowResDepthTexture", lowresDepthRT);

			//upscale fog and apply to main rendertarget
			Graphics.Blit (source, destination, ApplyFogMaterial);
			
			RenderTexture.ReleaseTemporary(lowresDepthRT);
			RenderTexture.ReleaseTemporary(fogRT1);
			RenderTexture.ReleaseTemporary(fogRT2);

		}
		
	}

