using System;
using System.Collections.Generic;
using System.Linq;
using Enum;
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
    public List<Light> FogLightCasters;
    public Texture2D _FogTexture2D;
    public Texture2D _BlueNoiseTexture2D;

    [Header("Will be found automatically on startup")]
    public Light sunLight;

    [Header("Position and size(in m³)")] public bool _LimitFogInSize = true;
    public Vector3 _FogWorldPosition;
    public float _FogSize = 10.0f;

    [Header("Performance")] [Range(0, 8)] public int _RenderTextureResDivision;
    [Range(16, 256)] public int _RayMarchSteps = 128;

    public bool _OptimizeSettingsFPS; // optimize raymarch steps according to fps
    public FPSTarget _FPSTarget = FPSTarget.MAX_60;

    [Header("Physical coefficients")] public bool _UseRayleighScattering = true;
    public float _RayleighScatteringCoef = 0.25f;

    public float _MieScatteringCoef = 0.25f;
    public MieScatteringApproximation _MieScatteringApproximation = MieScatteringApproximation.HenyeyGreenstein;

    public float _FogDensityCoef = 0.3f;
    public float _ExtinctionCoef = 0.01f;
    [Range(-1, 1)] public float _Anisotropy = 0.5f;
    public float _HeightDensityCoef = 0.5f;
    public float _BaseHeightDensity = 0.5f;

    [Header("Blur")] [Range(1, 8)] public int _BlurIterations = 4;
    public float _BlurDepthFalloff = 0.5f;
    public Vector3 _BlurOffsets = new Vector3(1, 2, 3);
    public Vector3 _BlurWeights = new Vector3(0.213f, 0.17f, 0.036f);

    [Header("Color")] public bool _UseLightColorForFog = false;
    public Color _FogInShadowColor = Color.black;
    public Color _FogInLightColor = Color.white;
    [Range(0, 1)] public float _AmbientFog;

    [Range(0, 10)] public float _LightIntensity = 1;

    [Header("Animation")] public Vector3 _WindDirection = Vector3.right;
    public float _Speed = 1f;

    [Header("Debug")] public NoiseSource _NoiseSource = NoiseSource.Texture2D;

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
                _CalculateFogMaterial = new Material(_CalculateFogShader) {hideFlags = HideFlags.HideAndDontSave};
            }

            return _CalculateFogMaterial;
        }
    }

    private Camera _currentCamera;
    public Camera CurrentCamera
    {
        get
        {
            if (!_currentCamera)
                _currentCamera = GetComponent<Camera>();
            return _currentCamera;
        }
    }

    private CommandBuffer _afterShadowPass;

    #region Shader Properties

    // cached shader property references to save some performance
    private static readonly int BlueNoiseTexture = Shader.PropertyToID("_BlueNoiseTexture");
    private static readonly int FogSpeed = Shader.PropertyToID("_FogSpeed");
    private static readonly int FogDirection = Shader.PropertyToID("_FogDirection");
    private static readonly int AmbientFog = Shader.PropertyToID("_AmbientFog");
    private static readonly int LightDir = Shader.PropertyToID("_LightDir");
    private static readonly int FogColor = Shader.PropertyToID("_FogColor");
    private static readonly int ShadowColor = Shader.PropertyToID("_ShadowColor");
    private static readonly int LightColor = Shader.PropertyToID("LightColor");
    private static readonly int LightIntensity = Shader.PropertyToID("_LightIntensity");
    private static readonly int FogSize = Shader.PropertyToID("_FogSize");
    private static readonly int FogWorldPosition = Shader.PropertyToID("_FogWorldPosition");
    private static readonly int HeightDensityCoef = Shader.PropertyToID("_HeightDensityCoef");
    private static readonly int BaseHeightDensity = Shader.PropertyToID("_BaseHeightDensity");
    private static readonly int Anisotropy = Shader.PropertyToID("_Anisotropy");
    private static readonly int ExtinctionCoef = Shader.PropertyToID("_ExtinctionCoef");
    private static readonly int NoiseScale = Shader.PropertyToID("_NoiseScale");
    private static readonly int FogDensity = Shader.PropertyToID("_FogDensity");
    private static readonly int RaymarchSteps = Shader.PropertyToID("_RaymarchSteps");
    private static readonly int RayleighScatteringCoef = Shader.PropertyToID("_RayleighScatteringCoef");
    private static readonly int BlurDepthFalloff = Shader.PropertyToID("_BlurDepthFalloff");
    private static readonly int Offsets = Shader.PropertyToID("_BlurOffsets");
    private static readonly int BlurWeights = Shader.PropertyToID("_BlurWeights");
    private static readonly int BlurDir = Shader.PropertyToID("BlurDir");
    private static readonly int FogRendertargetLinear = Shader.PropertyToID("FogRendertargetLinear");
    private static readonly int MieScatteringCoef = Shader.PropertyToID("_MieScatteringCoef");
    private static readonly int KFactor = Shader.PropertyToID("_kFactor");
    private static readonly int NoiseTexture = Shader.PropertyToID("_NoiseTexture");
    private static readonly int NoiseTex3D = Shader.PropertyToID("_NoiseTex3D");
    private static readonly int InverseProjectionMatrix = Shader.PropertyToID("InverseProjectionMatrix");
    private static readonly int InverseViewMatrix = Shader.PropertyToID("InverseViewMatrix");

    #endregion

    private void OnEnable()
    {
        sunLight = FindObjectsOfType<Light>().First(l => l.type == LightType.Directional);
    }

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
                _fogTexture3DCompute =
                    TextureUtilities.CreateFogLUT3DFrom2DSlicesCompute(_FogTexture2D, _3DNoiseTextureDimensions,
                        _Create3DLUTShader);
                break;
            case NoiseSource.SimplexNoiseCompute:
                _fogTexture3DSimplex =
                    TextureUtilities.CreateFogLUT3DFromSimplexNoise(_3DNoiseTextureDimensions, _Create3DLUTShader);
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
        if (_afterShadowPass != null && light)
        {
            light.RemoveCommandBuffer(LightEvent.AfterShadowMap, _afterShadowPass);
        }
    }

    // based on https://interplayoflight.wordpress.com/2015/07/03/adventures-in-postprocessing-with-unity/
    private void AddLightCommandBuffer(Light light)
    {
        _afterShadowPass = new CommandBuffer {name = "Volumetric Fog ShadowMap"};

        _afterShadowPass.SetGlobalTexture("ShadowMap",
            new RenderTargetIdentifier(BuiltinRenderTextureType.CurrentActive));

        if (light)
        {
            light.AddCommandBuffer(LightEvent.AfterShadowMap, _afterShadowPass);
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

        sunLight.intensity = _LightIntensity;

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

        Shader.SetGlobalMatrix(InverseViewMatrix, CurrentCamera.cameraToWorldMatrix);
        Shader.SetGlobalMatrix(InverseProjectionMatrix, CurrentCamera.projectionMatrix.inverse);

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
            CalculateFogMaterial.SetFloat(RayleighScatteringCoef, _RayleighScatteringCoef);
        }
        else
        {
            CalculateFogMaterial.DisableKeyword("RAYLEIGH_SCATTERING");
        }

        ToggleShaderKeyword(CalculateFogMaterial, "LIMITFOGSIZE", _LimitFogInSize);
        ToggleShaderKeyword(CalculateFogMaterial, "HEIGHTFOG", _HeightFogEnabled);


        var performanceRatio = CalculateRaymarchStepRatio();
        CalculateFogMaterial.SetFloat(RaymarchSteps, _RayMarchSteps * Mathf.Pow(performanceRatio, 2));

        CalculateFogMaterial.SetFloat(FogDensity, _FogDensityCoef);
        CalculateFogMaterial.SetFloat(NoiseScale, _NoiseScale);


        CalculateFogMaterial.SetFloat(ExtinctionCoef, _ExtinctionCoef);
        CalculateFogMaterial.SetFloat(Anisotropy, _Anisotropy);
        CalculateFogMaterial.SetFloat(BaseHeightDensity, _BaseHeightDensity);
        CalculateFogMaterial.SetFloat(HeightDensityCoef, _HeightDensityCoef);

        CalculateFogMaterial.SetVector(FogWorldPosition, _FogWorldPosition);
        CalculateFogMaterial.SetFloat(FogSize, _FogSize);
        CalculateFogMaterial.SetFloat(LightIntensity, _LightIntensity);

        CalculateFogMaterial.SetColor(LightColor, sunLight.color);
        CalculateFogMaterial.SetColor(ShadowColor, _FogInShadowColor);
        CalculateFogMaterial.SetColor(FogColor,
            _UseLightColorForFog ? sunLight.color : _FogInLightColor);

        CalculateFogMaterial.SetVector(LightDir, sunLight.transform.forward);
        CalculateFogMaterial.SetFloat(AmbientFog, _AmbientFog);

        CalculateFogMaterial.SetVector(FogDirection, _WindDirection);
        CalculateFogMaterial.SetFloat(FogSpeed, _Speed);

        CalculateFogMaterial.SetTexture(BlueNoiseTexture, _BlueNoiseTexture2D);

        Graphics.Blit(source, fogRenderTexture, CalculateFogMaterial);
    }

    private float CalculateRaymarchStepRatio()
    {
        if (!_OptimizeSettingsFPS) return 1;

        var currentFps = 1.0f / _benchmark.TimeSpent;
        var targetFps = 30f;
        switch (_FPSTarget)
        {
            case FPSTarget.MAX_30:
                targetFps = 30;
                break;
            case FPSTarget.MAX_60:
                targetFps = 60;
                break;
            case FPSTarget.MAX_120:
                targetFps = 120;
                break;
            case FPSTarget.UNLIMITED:
                targetFps = currentFps; // do not optimize
                break;
            default:
                Debug.Log($"FPS Target not found");
                break;
        }

        return Mathf.Clamp01(currentFps / targetFps);
    }

    private void BlurFog(RenderTexture fogTarget1, RenderTexture fogTarget2)
    {
        if (!_BlurEnabled) return;


        ApplyBlurMaterial.SetFloat(BlurDepthFalloff, _BlurDepthFalloff);

        var blurOffsets = new Vector4(0, // initial sample is always at the center 
            _BlurOffsets.x,
            _BlurOffsets.y,
            _BlurOffsets.z);

        ApplyBlurMaterial.SetVector(Offsets, blurOffsets);

        // x is sum of all weights
        var blurWeightsWithTotal = new Vector4(_BlurWeights.x + _BlurWeights.y + _BlurWeights.z,
            _BlurWeights.x,
            _BlurWeights.y,
            _BlurWeights.z);

        ApplyBlurMaterial.SetVector(BlurWeights, blurWeightsWithTotal);

        for (var i = 0; i < _BlurIterations; i++)
        {
            // vertical blur 
            ApplyBlurMaterial.SetVector(BlurDir, new Vector2(0, 1));
            Graphics.Blit(fogTarget1, fogTarget2, ApplyBlurMaterial);

            // horizontal blur
            ApplyBlurMaterial.SetVector(BlurDir, new Vector2(1, 0));
            Graphics.Blit(fogTarget2, fogTarget1, ApplyBlurMaterial);
        }
    }

    private void BlendWithScene(RenderTexture source, RenderTexture destination, RenderTexture fogTarget)
    {
        if (!_AddSceneColor)
        {
            Graphics.Blit(fogTarget, destination);
            return;
        }

        ;

        //send fog texture
        ApplyFogMaterial.SetTexture(FogRendertargetLinear, fogTarget);

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
                CalculateFogMaterial.SetFloat(MieScatteringCoef, _MieScatteringCoef);
                break;

            case MieScatteringApproximation.CornetteShanks:

                ToggleShaderKeyword(CalculateFogMaterial, "CS_SCATTERING", true);
                CalculateFogMaterial.SetFloat(MieScatteringCoef, _MieScatteringCoef);
                break;

            case MieScatteringApproximation.Schlick:

                CalculateKFactor();

                ToggleShaderKeyword(CalculateFogMaterial, "SCHLICK_HG_SCATTERING", true);
                CalculateFogMaterial.SetFloat(KFactor, _kFactor);
                CalculateFogMaterial.SetFloat(MieScatteringCoef, _MieScatteringCoef);
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
                CalculateFogMaterial.SetTexture(NoiseTexture, _FogTexture2D);
                ToggleShaderKeyword(CalculateFogMaterial, "NOISE2D", true);
                break;
            case NoiseSource.Texture3D:
                CalculateFogMaterial.SetTexture(NoiseTex3D, _fogTexture3D);
                ToggleShaderKeyword(CalculateFogMaterial, "NOISE3D", true);
                break;
            case NoiseSource.SimplexNoiseCompute:
                CalculateFogMaterial.SetTexture(NoiseTex3D, _fogTexture3DSimplex);
                ToggleShaderKeyword(CalculateFogMaterial, "NOISE3D", true);
                break;
            case NoiseSource.Texture3DCompute:
                CalculateFogMaterial.SetTexture(NoiseTex3D, _fogTexture3DCompute);
                ToggleShaderKeyword(CalculateFogMaterial, "NOISE3D", true);
                break;
        }
    }

    private static void ToggleShaderKeyword(Material shaderMat, string keyword, bool shouldEnable)
    {
        if (shouldEnable)
        {
            shaderMat.EnableKeyword(keyword);
        }
        else
        {
            shaderMat.DisableKeyword(keyword);
        }
    }
}