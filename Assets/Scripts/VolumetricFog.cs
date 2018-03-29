using System;
using UnityEngine;
using UnityEngine.Rendering;

[ExecuteInEditMode]
    [RequireComponent (typeof(Camera))]
	class VolumetricFog : SceneViewFilter
	{


		[HeaderAttribute("Required assets")]
		
		[SerializeField] private Shader _CalculateFogShader;
		[SerializeField] private Shader _ApplyBlurShader;
		[SerializeField] private Shader _ApplyFogShader;
		[SerializeField] private Transform SunLight;
		[SerializeField] private Texture2D _FogTexture2D;
		
		[HeaderAttribute("Position and size(in m³)")]
		
		[SerializeField] private Vector3 _FogWorldPosition;
		[SerializeField] private float _FogSize = 10.0f;

		[HeaderAttribute("Performance")]
		
		[SerializeField] [Range(1, 8)] private int _RenderTextureResDivision = 2;
		[SerializeField] [Range(16, 256)] private int _RayMarchSteps = 128;
		
		[Tooltip("Interleaved sampling square size")]
		[SerializeField] [Range(1, 16)] private int _SQRSize = 8;
		
		[HeaderAttribute("Physical coefficients")]
		
		[SerializeField] private float _FogDensityCoef = 0.3f;
		[SerializeField] private float _ScatteringCoef = 0.25f;
		[SerializeField] private float _ExtinctionCoef = 0.01f;
		[SerializeField] private float _Anisotropy = 0.5f;
		
		
		[HeaderAttribute("Color")]
		
		[SerializeField] private Color _ShadowColor = Color.black;
		[SerializeField] private Color _LightColor;
		
		[SerializeField] [Range(0,10)] private float _LightIntensity = 1;
		
		[HeaderAttribute("Debug")] 
		
		[SerializeField] private bool _BlurEnabled;
		[SerializeField] private bool _ShadowsEnabled;
		[SerializeField] private float _RaymarchDrawDistance = 40;
			
		
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
					_CurrentCamera = GetComponent<Camera>();
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

			Light light = null;
			if (SunLight != null)
			{
				light = SunLight.GetComponent<Light>();    
			}
        
			if (_AfterShadowPass != null && light)
			{
				light.RemoveCommandBuffer(LightEvent.AfterShadowMap, _AfterShadowPass);
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

		private Texture3D _FogTexture3D;
		
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

			if (!_FogTexture3D)
			{
				_FogTexture3D = TextureUtilities.CreateTexture3DFrom2DSlices(_FogTexture2D, 128);
			}

			if (_ShadowsEnabled)
			{
				Shader.EnableKeyword("SHADOWS_ON");
				Shader.DisableKeyword("SHADOWS_OFF");
			}
			else
			{
				Shader.DisableKeyword("SHADOWS_ON");
				Shader.EnableKeyword("SHADOWS_OFF");
			}
			
			SunLight.GetComponent<Light>().color = _LightColor;
			SunLight.GetComponent<Light>().intensity = _LightIntensity;
			
			
			RenderTextureFormat RTFogOutput = RenderTextureFormat.RFloat;
			int lowresDepthWidth= source.width;
			int lowresDepthHeight= source.height;

			RenderTexture lowresDepthRT = RenderTexture.GetTemporary (lowresDepthWidth, lowresDepthHeight, 0, RTFogOutput);
/*
			//downscale depth buffer to quarter resolution
			Graphics.Blit (source, lowresDepthRT, _DownscaleDepthMaterial);
			lowresDepthRT.filterMode = FilterMode.Point;
			*/
			RenderTextureFormat format = RenderTextureFormat.ARGBHalf;
			// float 4 : 1. A , 2: R, 3 : G, 4 : B
			
			int fogRTWidth= source.width / _RenderTextureResDivision;
			int fogRTHeight= source.height / _RenderTextureResDivision;


			RenderTexture fogRT1 = RenderTexture.GetTemporary (fogRTWidth, fogRTHeight, 0, format);
			RenderTexture fogRT2 = RenderTexture.GetTemporary (fogRTWidth, fogRTHeight, 0, format);

			fogRT1.filterMode = FilterMode.Bilinear;
			fogRT2.filterMode = FilterMode.Bilinear;

			Light light = SunLight.GetComponent<Light>();

			Shader.SetGlobalMatrix("InverseViewMatrix", CurrentCamera.cameraToWorldMatrix);
			Shader.SetGlobalMatrix("InverseProjectionMatrix", CurrentCamera.projectionMatrix.inverse);


			
		//	CalculateFogMaterial.SetTexture ("LowResDepth", lowresDepthRT); TODO
			CalculateFogMaterial.SetTexture ("_NoiseTexture", _FogTexture2D);
			CalculateFogMaterial.SetTexture("_NoiseTex3D", _FogTexture3D);
			
			CalculateFogMaterial.SetFloat("_RaymarchSteps", _RayMarchSteps);
			CalculateFogMaterial.SetFloat("_InterleavedSamplingSQRSize", _SQRSize);

			CalculateFogMaterial.SetVector("_LightData", new Vector4(light.transform.position.x, light.transform.position.y, light.transform.position.z,0));
			
			
			CalculateFogMaterial.SetFloat ("_FogDensity", _FogDensityCoef);
			CalculateFogMaterial.SetFloat ("_ScatteringCoef", _ScatteringCoef);
			CalculateFogMaterial.SetFloat ("_ExtinctionCoef", _ExtinctionCoef);
			CalculateFogMaterial.SetFloat("_Anisotropy", _Anisotropy);
			CalculateFogMaterial.SetFloat ("_ViewDistance", _RaymarchDrawDistance);
			CalculateFogMaterial.SetVector ("_FogWorldPosition", _FogWorldPosition);
			CalculateFogMaterial.SetFloat("_FogSize", _FogSize);
			CalculateFogMaterial.SetFloat ("_LightIntensity", _LightIntensity);
			CalculateFogMaterial.SetColor ("_ShadowColor", _ShadowColor);
			CalculateFogMaterial.SetVector ("_LightColor", _LightColor);



			//render fog
			Graphics.Blit (source, fogRT1, CalculateFogMaterial);

			if (true)
			{
				Graphics.Blit(fogRT1, destination);
				return;
			}


			if (_BlurEnabled)
			{
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
			}

	
		
			//apply fog to main scene
			
			ApplyFogMaterial.SetTexture ("FogRendertargetPoint", fogRT1);
			ApplyFogMaterial.SetTexture ("FogRendertargetLinear", fogRT1);
			
		//	ApplyFogMaterial.SetTexture ("LowResDepthTexture", lowresDepthRT);

			//apply to main rendertarget
			Graphics.Blit (source, destination, ApplyFogMaterial);
			
			RenderTexture.ReleaseTemporary(lowresDepthRT);
			RenderTexture.ReleaseTemporary(fogRT1);
			RenderTexture.ReleaseTemporary(fogRT2);

		}
		
	}

