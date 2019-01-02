using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
class VolumetricFog : MonoBehaviour
{
    private const RenderTextureFormat FORMAT_FOGRENDERTEXTURE = RenderTextureFormat.ARGBHalf;

    [Header("Required assets")] public Shader _CalculateFogShader;
    public Shader _ApplyBlurShader;
    public Shader _ApplyFogShader;
    public ComputeShader _Create3DLUTShader;
    public Transform SunLight;
    public List<Light> FogLightCasters;
    public Texture2D _FogTexture2D;

    [Header("Position and size(in m³)")] public bool _LimitFogInSize = true;
    public Vector3 _FogWorldPosition;
    public float _FogSize = 10.0f;

    [Header("Performance")] 
    [Range(0, 8)] public int _RenderTextureResDivision;
    [Range(16, 256)] public int _RayMarchSteps = 128;

    public bool _OptimizeSettingsFPS; // optimize raymarch steps according to fps
    public FPSTarget _FPSTarget = FPSTarget.MAX_60;

    [Header("Physical coefficients")]
    
    public bool _UseRayleighScattering = true;
    public float _RayleighScatteringCoef = 0.25f;

    public float _MieScatteringCoef = 0.25f;
    public MieScatteringApproximation _MieScatteringApproximation = MieScatteringApproximation.HenyeyGreenstein;

    public float _FogDensityCoef = 0.3f;
    public float _ExtinctionCoef = 0.01f;
    [Range(-1, 1)] public float _Anisotropy = 0.5f;
    public float _HeightDensityCoef = 0.5f;
    public float _BaseHeightDensity = 0.5f;

    [Header("Blur")] [Range(1, 8)] 
    
    public int _BlurIterations = 4;
    public float _BlurDepthFalloff = 0.5f;
    public Vector3 _BlurOffsets = new Vector3(1, 2, 3);
    public Vector3 _BlurWeights = new Vector3(0.213f, 0.17f, 0.036f);

    [Header("Color")] 
    public bool _UseLightColorForFog = false;
    public Color _FogInShadowColor = Color.black;
    public Color _FogInLightColor = Color.white;
    [Range(0, 1)] public float _AmbientFog;

    [Range(0, 10)] public float _LightIntensity = 1;

    [Header("Animation")]
    
    public Vector3 _WindDirection = Vector3.right;
    public float _Speed = 1f;
    
    [Header("Debug")] 
    
    public NoiseSource _NoiseSource = NoiseSource.Texture2D;

    public bool _AddSceneColor;
    public bool _BlurEnabled;
    public bool _ShadowsEnabled;
    public bool _HeightFogEnabled;

    [Range(-100, 100)] public float _NoiseScale = 0f;
   // [Range(1, 16)] public float _NoiseOctaves = 1f; TODO
    
    public Vector3Int _3DNoiseTextureDimensions = Vector3Int.one;


    private Material _ApplyBlurMaterial;
    private Material _CalculateFogMaterial;
    private Material _ApplyFogMaterial;

    private float _kFactor;

    private Texture3D _fogTexture3D;
    private RenderTexture _fogTexture3DCompute;
    private RenderTexture _fogTexture3DSimplex;
    private Benchmark _benchmark;

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

    private void Start()
    {
        _benchmark = FindObjectOfType<Benchmark>();
        FogLightCasters.ForEach(AddLightCommandBuffer);
        Regenerate3DTexture();
    }

    public void Regenerate3DTexture()
    {
        var is3DSource =
                _NoiseSource.HasFlag(NoiseSource.SimplexNoiseCompute) ||
                _NoiseSource.HasFlag(NoiseSource.Texture3DCompute) ||
                _NoiseSource.HasFlag(NoiseSource.Texture3D) 
            ;

        if (!is3DSource) return;
       
        
        switch (_NoiseSource)
        {
            case NoiseSource.Texture3D:
                _fogTexture3D = TextureUtilities.CreateFogLUT3DFrom2DSlices(_FogTexture2D, _3DNoiseTextureDimensions);
                break;
            case NoiseSource.Texture3DCompute:
                _fogTexture3DCompute = TextureUtilities.CreateFogLUT3DFrom2DSlicesCompute(_FogTexture2D, _3DNoiseTextureDimensions, _Create3DLUTShader);
                break;
            case NoiseSource.SimplexNoiseCompute:
                _fogTexture3DSimplex = TextureUtilities.CreateFogLUT3DFromSimplexNoise(_3DNoiseTextureDimensions, _Create3DLUTShader);
                break;

        }
 
    }

    private void CalculateKFactor()
    {
        _kFactor = 1.55f * _Anisotropy - (0.55f * Mathf.Pow(_Anisotropy, 3));
    }

    private void OnDestroy()
    {
        FogLightCasters.ForEach(RemoveLightCommandBuffer);
    }


    // based on https://interplayoflight.wordpress.com/2015/07/03/adventures-in-postprocessing-with-unity/    
    private void RemoveLightCommandBuffer(Light light)
    {
        if (_AfterShadowPass != null && light)
        {
            light.RemoveCommandBuffer(LightEvent.AfterShadowMap, _AfterShadowPass);
        }
    }

    // based on https://interplayoflight.wordpress.com/2015/07/03/adventures-in-postprocessing-with-unity/
    private void AddLightCommandBuffer(Light light)
    {
        _AfterShadowPass = new CommandBuffer {name = "Volumetric Fog ShadowMap"};

        _AfterShadowPass.SetGlobalTexture("ShadowMap",
            new RenderTargetIdentifier(BuiltinRenderTextureType.CurrentActive));

        if (light)
        {
            light.AddCommandBuffer(LightEvent.AfterShadowMap, _AfterShadowPass);
        }
    }


    private bool HasRequiredAssets()
    {
        return _FogTexture2D ||
               ApplyFogMaterial || _ApplyFogShader ||
               CalculateFogMaterial || _CalculateFogShader ||
               ApplyBlurMaterial || _ApplyBlurShader;
    }


    [ImageEffectOpaque]
    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (HasRequiredAssets() == false)
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

        var fogRTWidth = source.width >> _RenderTextureResDivision;
        var fogRTHeight = source.height >> _RenderTextureResDivision;

        // Get the rendertexture from the pool that fits the height, width and format. 
        // This increases performance, because Rendertextures do not need to be recreated when asking them again the next frame.
        // 2 Rendertextures are needed to iteratively blur an image
        var fogRT1 = RenderTexture.GetTemporary(fogRTWidth, fogRTHeight, 0, FORMAT_FOGRENDERTEXTURE);
        var fogRT2 = RenderTexture.GetTemporary(fogRTWidth, fogRTHeight, 0, FORMAT_FOGRENDERTEXTURE);

        fogRT1.filterMode = FilterMode.Bilinear;
        fogRT2.filterMode = FilterMode.Bilinear;


        SetMieScattering();
        SetNoiseSource();

        Shader.SetGlobalMatrix("InverseViewMatrix", CurrentCamera.cameraToWorldMatrix);
        Shader.SetGlobalMatrix("InverseProjectionMatrix", CurrentCamera.projectionMatrix.inverse);
    
        // render fog 
        RenderFog(fogRT1, source);
        // blur fog
        BlurFog(fogRT1, fogRT2);
        // blend fog 
        BlendWithScene(source, destination, fogRT1);

        // release textures to avoid leaking memory
        RenderTexture.ReleaseTemporary(fogRT1);
        RenderTexture.ReleaseTemporary(fogRT2);
    }
    
    private void RenderFog(RenderTexture fogRenderTexture, RenderTexture source)
    {
        
        if (_UseRayleighScattering)
        {
            CalculateFogMaterial.EnableKeyword("RAYLEIGH_SCATTERING");
            CalculateFogMaterial.SetFloat("_RayleighScatteringCoef", _RayleighScatteringCoef);
        }
        else
        {
            CalculateFogMaterial.DisableKeyword("RAYLEIGH_SCATTERING");
        }
        
        ToggleShaderKeyword(CalculateFogMaterial, "LIMITFOGSIZE", _LimitFogInSize);
        ToggleShaderKeyword(CalculateFogMaterial, "HEIGHTFOG", _HeightFogEnabled);
        

        var performanceRatio = CalculateRaymarchStepRatio();
        CalculateFogMaterial.SetFloat("_RaymarchSteps", _RayMarchSteps * Mathf.Pow(performanceRatio,2));

        CalculateFogMaterial.SetFloat("_FogDensity", _FogDensityCoef);
        CalculateFogMaterial.SetFloat("_NoiseScale", _NoiseScale);


        CalculateFogMaterial.SetFloat("_ExtinctionCoef", _ExtinctionCoef);
        CalculateFogMaterial.SetFloat("_Anisotropy", _Anisotropy);
        CalculateFogMaterial.SetFloat("_BaseHeightDensity", _BaseHeightDensity);
        CalculateFogMaterial.SetFloat("_HeightDensityCoef", _HeightDensityCoef);

        CalculateFogMaterial.SetVector("_FogWorldPosition", _FogWorldPosition);
        CalculateFogMaterial.SetFloat("_FogSize", _FogSize);
        CalculateFogMaterial.SetFloat("_LightIntensity", _LightIntensity);

        CalculateFogMaterial.SetColor("LightColor", SunLight.GetComponent<Light>().color);
        CalculateFogMaterial.SetColor("_ShadowColor", _FogInShadowColor);
        CalculateFogMaterial.SetColor("_FogColor", _UseLightColorForFog? SunLight.GetComponent<Light>().color : _FogInLightColor);

        CalculateFogMaterial.SetVector("_LightDir", SunLight.GetComponent<Light>().transform.forward);
        CalculateFogMaterial.SetFloat("_AmbientFog", _AmbientFog);
        
        CalculateFogMaterial.SetVector("_FogDirection", _WindDirection);
        CalculateFogMaterial.SetFloat("_FogSpeed", _Speed);

        Graphics.Blit(source, fogRenderTexture, CalculateFogMaterial);
    }

    private float CalculateRaymarchStepRatio()
    {
        if (!_OptimizeSettingsFPS) return 1;
        
        var currentFPS = 1.0f / _benchmark.TimeSpent;
        var targetFPS = 30f;
        switch (_FPSTarget)
        {
            case FPSTarget.MAX_30:
                targetFPS = 30;
                break;
            case FPSTarget.MAX_60:
                targetFPS = 60;
                break;
            case FPSTarget.MAX_120:
                targetFPS = 120;
                break;
            case FPSTarget.UNLIMITED:
                targetFPS = currentFPS; // do not optimize
                break;
            default:
                Debug.Log($"FPS Target not found");
                break;
        }
        return Mathf.Clamp01(currentFPS / targetFPS);
    }

    private void BlurFog(RenderTexture fogTarget1, RenderTexture fogTarget2)
    {
        if (!_BlurEnabled) return;
        
        
        ApplyBlurMaterial.SetFloat("_BlurDepthFalloff", _BlurDepthFalloff);

        var BlurOffsets = new Vector4(0, // initial sample is always at the center 
            _BlurOffsets.x,
            _BlurOffsets.y,
            _BlurOffsets.z);

        ApplyBlurMaterial.SetVector("_BlurOffsets", BlurOffsets);

        // x is sum of all weights
        var BlurWeightsWithTotal = new Vector4(_BlurWeights.x + _BlurWeights.y + _BlurWeights.z,
            _BlurWeights.x,
            _BlurWeights.y,
            _BlurWeights.z);

        ApplyBlurMaterial.SetVector("_BlurWeights", BlurWeightsWithTotal);

        for (var i = 0; i < _BlurIterations; i++)
        {
            // vertical blur 
            ApplyBlurMaterial.SetVector("BlurDir", new Vector2(0, 1));
            Graphics.Blit(fogTarget1, fogTarget2, ApplyBlurMaterial);

            // horizontal blur
            ApplyBlurMaterial.SetVector("BlurDir", new Vector2(1, 0));
            Graphics.Blit(fogTarget2, fogTarget1, ApplyBlurMaterial);
        }
    }

    private void BlendWithScene(RenderTexture source, RenderTexture destination, RenderTexture fogTarget)
    {
        if (!_AddSceneColor) return;
        
        //send fog texture
        ApplyFogMaterial.SetTexture("FogRendertargetLinear", fogTarget);

        //apply to main rendertarget
        Graphics.Blit(source, destination, ApplyFogMaterial);
    }

    private void SetMieScattering()
    {
        ToggleShaderKeyword(CalculateFogMaterial, "HG_SCATTERING", false);
        ToggleShaderKeyword(CalculateFogMaterial, "CS_SCATTERING", false);
        ToggleShaderKeyword(CalculateFogMaterial, "SCHLICK_HG_SCATTERING", false);
        
        switch (_MieScatteringApproximation)
        {
            case MieScatteringApproximation.HenyeyGreenstein:
                ToggleShaderKeyword(CalculateFogMaterial, "HG_SCATTERING", true);
                CalculateFogMaterial.SetFloat("_MieScatteringCoef", _MieScatteringCoef);
                break;

            case MieScatteringApproximation.CornetteShanks:

                ToggleShaderKeyword(CalculateFogMaterial, "CS_SCATTERING", true);
                CalculateFogMaterial.SetFloat("_MieScatteringCoef", _MieScatteringCoef);
                break;

            case MieScatteringApproximation.Schlick:

                CalculateKFactor();

                ToggleShaderKeyword(CalculateFogMaterial, "SCHLICK_HG_SCATTERING", true);
                CalculateFogMaterial.SetFloat("_kFactor", _kFactor);
                CalculateFogMaterial.SetFloat("_MieScatteringCoef", _MieScatteringCoef);
                break;

            case MieScatteringApproximation.Off:
                break;

            default:
                Debug.LogWarning(
                    $"Mie scattering approximation {_MieScatteringApproximation} is not handled by SetMieScattering()");
                break;
        }
    }


    private void SetNoiseSource()
    {
        ToggleShaderKeyword(CalculateFogMaterial, "SNOISE", false);
        ToggleShaderKeyword(CalculateFogMaterial, "NOISE2D", false);
        ToggleShaderKeyword(CalculateFogMaterial, "NOISE3D", false);
        
        switch (_NoiseSource)
        {
            case NoiseSource.SimplexNoise:
                ToggleShaderKeyword(CalculateFogMaterial, "SNOISE", true);
                break;
            case NoiseSource.Texture2D:
                CalculateFogMaterial.SetTexture("_NoiseTexture", _FogTexture2D);
                ToggleShaderKeyword(CalculateFogMaterial, "NOISE2D", true);
                break;
            case NoiseSource.Texture3D:
                CalculateFogMaterial.SetTexture("_NoiseTex3D", _fogTexture3D);
                ToggleShaderKeyword(CalculateFogMaterial, "NOISE3D", true);
                break;
            case NoiseSource.SimplexNoiseCompute:
                CalculateFogMaterial.SetTexture("_NoiseTex3D", _fogTexture3DSimplex);
                ToggleShaderKeyword(CalculateFogMaterial, "NOISE3D", true);
                break;
            case NoiseSource.Texture3DCompute:
                CalculateFogMaterial.SetTexture("_NoiseTex3D", _fogTexture3DCompute);
                ToggleShaderKeyword(CalculateFogMaterial, "NOISE3D", true);
                break;
        }
    }

    private void ToggleShaderKeyword(Material shaderMat, string keyword, bool enabled)
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