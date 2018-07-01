using UnityEngine;
using UnityEngine.Rendering;

public enum MieScatteringApproximation
{
    HenyeyGreenstein,
    CornetteShanks,
    Schlick,
    Off
}

public enum NoiseSource
{
    Texture2D,
    Texture3D,
    SimplexNoise
}


[ExecuteInEditMode]
[RequireComponent (typeof(Camera))]
class VolumetricFog : MonoBehaviour
{
		
    [Header("Required assets")]
		
    public Shader _CalculateFogShader;
    public Shader _ApplyBlurShader;
    public Shader _ApplyFogShader;
    public Transform SunLight;
    public Texture2D _FogTexture2D;

    [Header("Position and size(in m³)")] 
		
    public bool _LimitFogInSize = true;
    public Vector3 _FogWorldPosition;
    public float _FogSize = 10.0f;

    [Header("Performance")]
		
    [Range(1, 8)] public int _RenderTextureResDivision = 2;
    [Range(16, 256)] public int _RayMarchSteps = 128;

    [Header("Physical coefficients")] 
		
    public bool _UseRayleighScattering = true;
    public float _RayleighScatteringCoef = 0.25f;

    public float _MieScatteringCoef = 0.25f;
    public MieScatteringApproximation _MieScatteringApproximation = MieScatteringApproximation.HenyeyGreenstein;
		
    public float _FogDensityCoef = 0.3f;
    public float _ExtinctionCoef = 0.01f;
    [Range(-1,1)]  public float _Anisotropy = 0.5f;
    public float _HeightDensityCoef = 0.5f;
    public float _BaseHeightDensity = 0.5f;
		
    [Header("Blur")]
		
    [Range(1, 8)] public int _BlurIterations = 4;
    public float _BlurDepthFalloff = 0.5f;
    public Vector3 _BlurOffsets = new Vector3(1, 2, 3);
    public Vector3 _BlurWeights = new Vector3(0.213f, 0.17f, 0.036f);
		
    [Header("Color")]
		
    public Color _FogInShadowColor = Color.black;
    public Color _FogInLightColor = Color.white;
    [Range(0,1)] public float _AmbientFog;
		
    [Range(0,10)] public float _LightIntensity = 1;
		

    [Header("Debug")] 
		
    public NoiseSource _NoiseSource = NoiseSource.Texture2D;

    public bool _AddSceneColor;
    public bool _BlurEnabled;
    public bool _ShadowsEnabled;
    public bool _HeightFogEnabled;
    
    [Range(-100,100)] public float _NoiseOctave = 0f;	
    
    
    
    private Material _ApplyBlurMaterial;
    private Material _CalculateFogMaterial;
    private Material _ApplyFogMaterial;

    private Texture3D _fogTexture3D;
    private float _kFactor;
    
    
    public Texture3D FogTexture3D
    {
        get
        {
            if (_fogTexture3D == null)
            {
                _fogTexture3D = TextureUtilities.CreateTexture3DFrom2DSlices(_FogTexture2D, 160, 90, 128);
            }
            
            return _fogTexture3D;
        }
        private set { _fogTexture3D = value; }
    }



    public Material ApplyFogMaterial
    {
        get
        {
            if (!_ApplyFogMaterial && _ApplyFogShader)
            {
                _ApplyFogMaterial = new Material(_ApplyFogShader) {hideFlags = HideFlags.HideAndDontSave};
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
                _ApplyBlurMaterial = new Material(_ApplyBlurShader) {hideFlags = HideFlags.HideAndDontSave};
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

    private void CalculateKFactor()
    {
        _kFactor = 1.55f * _Anisotropy - (0.55f * Mathf.Pow(_Anisotropy, 3));
    }

    void OnDestroy()
    {
        RemoveLightCommandBuffer();
    }

		
    // based on https://interplayoflight.wordpress.com/2015/07/03/adventures-in-postprocessing-with-unity/    
    private void RemoveLightCommandBuffer()
    {

        Light sunLight = null;
        if (SunLight)
        {
            sunLight = SunLight.GetComponent<Light>();
        }
       

        
        if (_AfterShadowPass != null && sunLight != null)
        {
            sunLight.RemoveCommandBuffer(LightEvent.AfterShadowMap, _AfterShadowPass);
        }
    }

    // based on https://interplayoflight.wordpress.com/2015/07/03/adventures-in-postprocessing-with-unity/
    void AddLightCommandBuffer()
    {
        _AfterShadowPass = new CommandBuffer {name = "Volumetric Fog ShadowMap"};
        
        _AfterShadowPass.SetGlobalTexture("ShadowMap", new RenderTargetIdentifier(BuiltinRenderTextureType.CurrentActive));

        Light sunLight = SunLight.GetComponent<Light>();
    
        if (sunLight)
        {
            sunLight.AddCommandBuffer(LightEvent.AfterShadowMap, _AfterShadowPass);
        }
    }


    bool CheckRequirements()
    {
        return !_FogTexture2D ||
               !ApplyFogMaterial || !_ApplyFogShader ||
               !CalculateFogMaterial || !_CalculateFogShader ||
               !ApplyBlurMaterial || !_ApplyBlurShader;
    }

    
    [ImageEffectOpaque]
    void OnRenderImage (RenderTexture source, RenderTexture destination)
    {


		if(CheckRequirements())	
        {
            Debug.Log("Not rendering image effect");
            Graphics.Blit(source, destination); // do nothing
            return;
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
			
        SunLight.GetComponent<Light>().intensity = _LightIntensity;

        RenderTextureFormat format = RenderTextureFormat.ARGBHalf;
        // float4 : 1: A , 2: R, 3: G, 4: B
			
        int fogRTWidth= source.width / _RenderTextureResDivision;
        int fogRTHeight= source.height / _RenderTextureResDivision;

        // Get the rendertexture from the pool that fits the height, width and format. 
        // This increases performance, because rendertextures do not need to be recreated when asking them again the next frame.
        // 2 rendertextures are needed to iteratively blur an image
        RenderTexture fogRT1 = RenderTexture.GetTemporary (fogRTWidth, fogRTHeight, 0, format);
        RenderTexture fogRT2 = RenderTexture.GetTemporary (fogRTWidth, fogRTHeight, 0, format);

        fogRT1.filterMode = FilterMode.Bilinear;
        fogRT2.filterMode = FilterMode.Bilinear;


        ToggleShaderKeyword(CalculateFogMaterial, "SHADOWS_ON", _ShadowsEnabled); // todo fix
        ToggleShaderKeyword(CalculateFogMaterial, "LIMITFOGSIZE", _LimitFogInSize);
        ToggleShaderKeyword(CalculateFogMaterial, "HEIGHTFOG", _HeightFogEnabled);

        
        if (_UseRayleighScattering)
        {
            CalculateFogMaterial.EnableKeyword("RAYLEIGH_SCATTERING");
            CalculateFogMaterial.SetFloat ("_RayleighScatteringCoef", _RayleighScatteringCoef);

        }
        else
        {
            CalculateFogMaterial.DisableKeyword("RAYLEIGH_SCATTERING");
        }


        SetMieScattering();
        SetNoiseSource();
			
        
        Shader.SetGlobalMatrix("InverseViewMatrix", CurrentCamera.cameraToWorldMatrix);
        Shader.SetGlobalMatrix("InverseProjectionMatrix", CurrentCamera.projectionMatrix.inverse);

        CalculateFogMaterial.SetFloat("_RaymarchSteps", _RayMarchSteps);

        CalculateFogMaterial.SetFloat ("_FogDensity", _FogDensityCoef);
        CalculateFogMaterial.SetFloat("_NoiseOctave", _NoiseOctave);
        
        
        CalculateFogMaterial.SetFloat ("_ExtinctionCoef", _ExtinctionCoef);
        CalculateFogMaterial.SetFloat("_Anisotropy", _Anisotropy);
        CalculateFogMaterial.SetFloat("_BaseHeightDensity", _BaseHeightDensity);
        CalculateFogMaterial.SetFloat("_HeightDensityCoef", _HeightDensityCoef);
			
        CalculateFogMaterial.SetVector ("_FogWorldPosition", _FogWorldPosition);
        CalculateFogMaterial.SetFloat("_FogSize", _FogSize);
        CalculateFogMaterial.SetFloat ("_LightIntensity", _LightIntensity);

        CalculateFogMaterial.SetColor("LightColor", SunLight.GetComponent<Light>().color);
        CalculateFogMaterial.SetColor ("_ShadowColor", _FogInShadowColor);
        CalculateFogMaterial.SetColor ("_FogColor", _FogInLightColor);
        
        CalculateFogMaterial.SetVector("_LightDir", SunLight.GetComponent<Light>().transform.forward);
        CalculateFogMaterial.SetFloat("_AmbientFog", _AmbientFog);



        //render fog
        Graphics.Blit (source, fogRT1, CalculateFogMaterial);

        // blur fog
        BlurFog(fogRT1, fogRT2);

        // blend fog 
        BlendWithScene(source, destination, fogRT1);


        // release textures to avoid high memory usage
        RenderTexture.ReleaseTemporary(fogRT1);
        RenderTexture.ReleaseTemporary(fogRT2);

        
    }

    private void BlurFog(RenderTexture fogTarget1, RenderTexture fogTarget2)
    {
        if (_BlurEnabled)
        {
				
            ApplyBlurMaterial.SetFloat("_BlurDepthFalloff", _BlurDepthFalloff);
				
            Vector4 BlurOffsets =   new Vector4(0, // initial sample is always at the center 
                _BlurOffsets.x,
                _BlurOffsets.y,
                _BlurOffsets.z);
				
            ApplyBlurMaterial.SetVector("_BlurOffsets", BlurOffsets);

            // x is sum of all weights
            Vector4 BlurWeightsWithTotal =  new Vector4(_BlurWeights.x + _BlurWeights.y + _BlurWeights.z,
                _BlurWeights.x,
                _BlurWeights.y,
                _BlurWeights.z);
				
            ApplyBlurMaterial.SetVector("_BlurWeights", BlurWeightsWithTotal);


            for (int i = 0; i < _BlurIterations; i++)
            {
					
                // vertical blur 
                ApplyBlurMaterial.SetVector ("BlurDir", new Vector2(0,1));
                Graphics.Blit (fogTarget1, fogTarget2, ApplyBlurMaterial);

                // horizontal blur
                ApplyBlurMaterial.SetVector ("BlurDir", new Vector2(1,0));
                Graphics.Blit (fogTarget2, fogTarget1, ApplyBlurMaterial);

            }
	
        }
    }
    
    private void BlendWithScene(RenderTexture source, RenderTexture destination, RenderTexture fogTarget)
    {
        if (_AddSceneColor)
        {
            //send fog texture
            ApplyFogMaterial.SetTexture ("FogRendertargetLinear", fogTarget);
			
            //apply to main rendertarget
            Graphics.Blit (source, destination, ApplyFogMaterial);
        }
    }
    
    private void SetMieScattering()
    {
        switch (_MieScatteringApproximation)
        {
            case MieScatteringApproximation.HenyeyGreenstein:
                ToggleShaderKeyword(CalculateFogMaterial, "HG_SCATTERING", true);
                ToggleShaderKeyword(CalculateFogMaterial, "CS_SCATTERING", false);
                ToggleShaderKeyword(CalculateFogMaterial, "SCHLICK_HG_SCATTERING", false);
                CalculateFogMaterial.SetFloat("_MieScatteringCoef", _MieScatteringCoef);
                break;
        
            case MieScatteringApproximation.CornetteShanks:
        
                ToggleShaderKeyword(CalculateFogMaterial, "CS_SCATTERING", true);
                ToggleShaderKeyword(CalculateFogMaterial, "SCHLICK_HG_SCATTERING", false);
                ToggleShaderKeyword(CalculateFogMaterial, "HG_SCATTERING", false);
        
                CalculateFogMaterial.SetFloat("_MieScatteringCoef", _MieScatteringCoef);
                break;
        
            case MieScatteringApproximation.Schlick:
                
                CalculateKFactor();
                
                ToggleShaderKeyword(CalculateFogMaterial, "SCHLICK_HG_SCATTERING", true);
                ToggleShaderKeyword(CalculateFogMaterial, "HG_SCATTERING", false);
                ToggleShaderKeyword(CalculateFogMaterial, "CS_SCATTERING", false);
        
                CalculateFogMaterial.SetFloat("_kFactor", _kFactor);
                CalculateFogMaterial.SetFloat("_MieScatteringCoef", _MieScatteringCoef);
                break;
            
            case MieScatteringApproximation.Off:
                ToggleShaderKeyword(CalculateFogMaterial, "SCHLICK_HG_SCATTERING", false);
                ToggleShaderKeyword(CalculateFogMaterial, "HG_SCATTERING", false);
                ToggleShaderKeyword(CalculateFogMaterial, "CS_SCATTERING", false);
                break;
            
            default:
                Debug.LogWarning($"Mie scattering approximation {_MieScatteringApproximation} is not handled by SetMieScattering()");
                break;
            
        }
    }


    void SetNoiseSource()
    {
        
        switch (_NoiseSource)
        {
            case NoiseSource.SimplexNoise:
                ToggleShaderKeyword(CalculateFogMaterial, "SNOISE", true);
                ToggleShaderKeyword(CalculateFogMaterial, "NOISE2D", false);
                ToggleShaderKeyword(CalculateFogMaterial, "NOISE3D", false);
                break;
					
            case NoiseSource.Texture2D:
	
                CalculateFogMaterial.SetTexture ("_NoiseTexture", _FogTexture2D);
                ToggleShaderKeyword(CalculateFogMaterial, "SNOISE", false);
                ToggleShaderKeyword(CalculateFogMaterial, "NOISE2D", true);
                ToggleShaderKeyword(CalculateFogMaterial, "NOISE3D", false);
                break;
					
            case NoiseSource.Texture3D:
                CalculateFogMaterial.SetTexture("_NoiseTex3D", FogTexture3D);
                ToggleShaderKeyword(CalculateFogMaterial, "SNOISE", false);
                ToggleShaderKeyword(CalculateFogMaterial, "NOISE2D", false);
                ToggleShaderKeyword(CalculateFogMaterial, "NOISE3D", true);
						
                break;
					
        }
    }
    void ToggleShaderKeyword(Material shaderMat, string keyword, bool enabled)
    {
        if (enabled)
        {
            shaderMat.EnableKeyword(keyword);
        }
        else
        {
            shaderMat.DisableKeyword(keyword);
        }
    }

}

