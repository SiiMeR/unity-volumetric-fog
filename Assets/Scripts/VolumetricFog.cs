using System.Collections.Generic;
using System.Linq;
using Attributes;
using Enum;
using UnityEngine;
using UnityEngine.Rendering;

[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
public class VolumetricFog : MonoBehaviour
{
    private const RenderTextureFormat FormatFogrendertexture = RenderTextureFormat.ARGBHalf;

    [Expandable]
    public VolumetricFogOptions fogOptions;
    
    
    [Header("Required assets")] public Shader calculateFogShader;
    public Shader applyBlurShader;
    public Shader applyFogShader;
    public ComputeShader create3DLutShader;
    public List<Light> fogLightCasters;
    public Texture2D fogTexture2D;
    public Texture2D blueNoiseTexture2D;

    [Header("Will be found automatically on startup")]
    public Light sunLight;

    
    private Material _applyBlurMaterial;
    private Material _calculateFogMaterial;
    private Material _applyFogMaterial;

    private float _kFactor;

    private Texture3D _fogTexture3D;
    private RenderTexture _fogTexture3DCompute;
    private RenderTexture _fogTexture3DSimplex;
    private Benchmark _benchmark;

    public Material ApplyFogMaterial
    {
        get
        {
            if (!_applyFogMaterial && applyFogShader)
            {
                _applyFogMaterial = new Material(applyFogShader) {hideFlags = HideFlags.HideAndDontSave};
            }

            return _applyFogMaterial;
        }
    }


    public Material ApplyBlurMaterial
    {
        get
        {
            if (!_applyBlurMaterial && applyBlurShader)
            {
                _applyBlurMaterial = new Material(applyBlurShader) {hideFlags = HideFlags.HideAndDontSave};
            }

            return _applyBlurMaterial;
        }
    }

    public Material CalculateFogMaterial
    {
        get
        {
            if (!_calculateFogMaterial && calculateFogShader)
            {
                _calculateFogMaterial = new Material(calculateFogShader) {hideFlags = HideFlags.HideAndDontSave};
            }

            return _calculateFogMaterial;
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
        sunLight = FindObjectsOfType<Light>().FirstOrDefault(l => l.type == LightType.Directional);

        if (!sunLight)
        {
            sunLight = FindObjectOfType<Light>();
        }
        
        if (!fogLightCasters.Contains(sunLight))
        {
            fogLightCasters.Add(sunLight);
        }
    }

    private void Start()
    {
        _benchmark = FindObjectOfType<Benchmark>();
        fogLightCasters.ForEach(AddLightCommandBuffer);
        Regenerate3DTexture();
    }

    public void Regenerate3DTexture()
    {
        var is3DSource =
                fogOptions.noiseSource.HasFlag(NoiseSource.SimplexNoiseCompute) ||
                fogOptions.noiseSource.HasFlag(NoiseSource.Texture3DCompute) ||
                fogOptions.noiseSource.HasFlag(NoiseSource.Texture3D);

        if (!is3DSource) return;
        
        switch (fogOptions.noiseSource)
        {
            case NoiseSource.Texture3D:
                _fogTexture3D = TextureUtilities.CreateFogLUT3DFrom2DSlices(fogTexture2D, fogOptions.noiseTexture3DDimensions);
                break;
            case NoiseSource.Texture3DCompute:
                _fogTexture3DCompute =
                    TextureUtilities.CreateFogLUT3DFrom2DSlicesCompute(fogTexture2D, fogOptions.noiseTexture3DDimensions,
                        create3DLutShader);
                break;
            case NoiseSource.SimplexNoiseCompute:
                _fogTexture3DSimplex =
                    TextureUtilities.CreateFogLUT3DFromSimplexNoise(fogOptions.noiseTexture3DDimensions, create3DLutShader);
                break;
        }
    }

    private void CalculateKFactor()
    {
        _kFactor = 1.55f * fogOptions.anisotropy - (0.55f * Mathf.Pow(fogOptions.anisotropy, 3));
    }

    private void OnDestroy()
    {
        fogLightCasters.ForEach(RemoveLightCommandBuffer);
    }


    // based on https://interplayoflight.wordpress.com/2015/07/03/adventures-in-postprocessing-with-unity/    
    private void RemoveLightCommandBuffer(Light lightComponent)
    {
        if (_afterShadowPass != null && lightComponent)
        {
            lightComponent.RemoveCommandBuffer(LightEvent.AfterShadowMap, _afterShadowPass);
        }
    }

    // based on https://interplayoflight.wordpress.com/2015/07/03/adventures-in-postprocessing-with-unity/
    private void AddLightCommandBuffer(Light lightComponent)
    {
        _afterShadowPass = new CommandBuffer {name = "Volumetric Fog ShadowMap"};

        _afterShadowPass.SetGlobalTexture("ShadowMap",
            new RenderTargetIdentifier(BuiltinRenderTextureType.CurrentActive));

        if (lightComponent)
        {
            lightComponent.AddCommandBuffer(LightEvent.AfterShadowMap, _afterShadowPass);
        }
    }


    private bool HasRequiredAssets()
    {
        return fogTexture2D &&
               ApplyFogMaterial && applyFogShader &&
               CalculateFogMaterial && calculateFogShader &&
               ApplyBlurMaterial && applyBlurShader;
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

        if (fogOptions.shadowsEnabled)
        {
            Shader.EnableKeyword("SHADOWS_ON");
            Shader.DisableKeyword("SHADOWS_OFF");
        }
        else
        {
            Shader.DisableKeyword("SHADOWS_ON");
            Shader.EnableKeyword("SHADOWS_OFF");
        }

        if (sunLight)
        {
            sunLight.intensity = fogOptions.lightIntensity;
        }

        var fogRtWidth = source.width >> fogOptions.renderTextureResDivision;
        var fogRtHeight = source.height >> fogOptions.renderTextureResDivision;

        // Get the rendertexture from the pool that fits the height, width and format. 
        // This increases performance, because Rendertextures do not need to be recreated when asking them again the next frame.
        // 2 Rendertextures are needed to iteratively blur an image
        var fogRt1 = RenderTexture.GetTemporary(fogRtWidth, fogRtHeight, 0, FormatFogrendertexture);
        var fogRt2 = RenderTexture.GetTemporary(fogRtWidth, fogRtHeight, 0, FormatFogrendertexture);

        fogRt1.filterMode = FilterMode.Bilinear;
        fogRt2.filterMode = FilterMode.Bilinear;


        SetMieScattering();
        SetNoiseSource();

        Shader.SetGlobalMatrix(InverseViewMatrix, CurrentCamera.cameraToWorldMatrix);
        Shader.SetGlobalMatrix(InverseProjectionMatrix, CurrentCamera.projectionMatrix.inverse);

        // render fog 
        RenderFog(fogRt1, source);
        // blur fog
        BlurFog(fogRt1, fogRt2);
        // blend fog 
        BlendWithScene(source, destination, fogRt1);

        // release textures to avoid leaking memory
        RenderTexture.ReleaseTemporary(fogRt1);
        RenderTexture.ReleaseTemporary(fogRt2);
    }

    private void RenderFog(RenderTexture fogRenderTexture, RenderTexture source)
    {
        if (fogOptions.useRayleighScattering)
        {
            CalculateFogMaterial.EnableKeyword("RAYLEIGH_SCATTERING");
            CalculateFogMaterial.SetFloat(RayleighScatteringCoef, fogOptions.rayleighScatteringCoef);
        }
        else
        {
            CalculateFogMaterial.DisableKeyword("RAYLEIGH_SCATTERING");
        }

        ToggleShaderKeyword(CalculateFogMaterial, "LIMITFOGSIZE", fogOptions.limitFogInSize);
        ToggleShaderKeyword(CalculateFogMaterial, "HEIGHTFOG", fogOptions.heightFogEnabled);


        var performanceRatio = CalculateRaymarchStepRatio();
        CalculateFogMaterial.SetFloat(RaymarchSteps, fogOptions.rayMarchSteps * Mathf.Pow(performanceRatio, 2));

        CalculateFogMaterial.SetFloat(FogDensity, fogOptions.fogDensityCoef);
        CalculateFogMaterial.SetFloat(NoiseScale, fogOptions.noiseScale);


        CalculateFogMaterial.SetFloat(ExtinctionCoef, fogOptions.extinctionCoef);
        CalculateFogMaterial.SetFloat(Anisotropy, fogOptions.anisotropy);
        CalculateFogMaterial.SetFloat(BaseHeightDensity, fogOptions.baseHeightDensity);
        CalculateFogMaterial.SetFloat(HeightDensityCoef, fogOptions.heightDensityCoef);

        CalculateFogMaterial.SetVector(FogWorldPosition, fogOptions.fogWorldPosition);
        CalculateFogMaterial.SetFloat(FogSize, fogOptions.fogSize);
        CalculateFogMaterial.SetFloat(LightIntensity, fogOptions.lightIntensity);

        CalculateFogMaterial.SetColor(LightColor, sunLight.color);
        CalculateFogMaterial.SetColor(ShadowColor, fogOptions.fogInShadowColor);
        CalculateFogMaterial.SetColor(FogColor,
            fogOptions.useLightColorForFog ? sunLight.color : fogOptions.fogInLightColor);

        CalculateFogMaterial.SetVector(LightDir, sunLight.transform.forward);
        CalculateFogMaterial.SetFloat(AmbientFog, fogOptions.ambientFog);

        CalculateFogMaterial.SetVector(FogDirection, fogOptions.windDirection);
        CalculateFogMaterial.SetFloat(FogSpeed, fogOptions.speed);

        CalculateFogMaterial.SetTexture(BlueNoiseTexture, blueNoiseTexture2D);

        Graphics.Blit(source, fogRenderTexture, CalculateFogMaterial);
    }

    private float CalculateRaymarchStepRatio()
    {
        if (!fogOptions.optimizeSettingsFps) return 1;

        var currentFps = 1.0f / _benchmark.TimeSpent;
        var targetFps = 30f;
        switch (fogOptions.fpsTarget)
        {
            case FPSTarget.Max30:
                targetFps = 30;
                break;
            case FPSTarget.Max60:
                targetFps = 60;
                break;
            case FPSTarget.Max120:
                targetFps = 120;
                break;
            case FPSTarget.Unlimited:
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
        if (!fogOptions.blurEnabled) return;


        ApplyBlurMaterial.SetFloat(BlurDepthFalloff, fogOptions.blurDepthFalloff);

        var blurOffsets = new Vector4(0, // initial sample is always at the center 
            fogOptions.blurOffsets.x,
            fogOptions.blurOffsets.y,
            fogOptions.blurOffsets.z);

        ApplyBlurMaterial.SetVector(Offsets, blurOffsets);

        // x is sum of all weights
        var blurWeightsWithTotal = new Vector4(fogOptions.blurWeights.x + fogOptions.blurWeights.y + fogOptions.blurWeights.z,
            fogOptions.blurWeights.x,
            fogOptions.blurWeights.y,
            fogOptions.blurWeights.z);

        ApplyBlurMaterial.SetVector(BlurWeights, blurWeightsWithTotal);

        for (var i = 0; i < fogOptions.blurIterations; i++)
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
        if (!fogOptions.addSceneColor)
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

        switch (fogOptions.mieScatteringApproximation)
        {
            case MieScatteringApproximation.HenyeyGreenstein:
                ToggleShaderKeyword(CalculateFogMaterial, "HG_SCATTERING", true);
                CalculateFogMaterial.SetFloat(MieScatteringCoef, fogOptions.mieScatteringCoef);
                break;

            case MieScatteringApproximation.CornetteShanks:

                ToggleShaderKeyword(CalculateFogMaterial, "CS_SCATTERING", true);
                CalculateFogMaterial.SetFloat(MieScatteringCoef, fogOptions.mieScatteringCoef);
                break;

            case MieScatteringApproximation.Schlick:

                CalculateKFactor();

                ToggleShaderKeyword(CalculateFogMaterial, "SCHLICK_HG_SCATTERING", true);
                CalculateFogMaterial.SetFloat(KFactor, _kFactor);
                CalculateFogMaterial.SetFloat(MieScatteringCoef, fogOptions.mieScatteringCoef);
                break;

            case MieScatteringApproximation.Off:
                break;

            default:
                Debug.LogWarning(
                    $"Mie scattering approximation {fogOptions.mieScatteringApproximation} is not handled by SetMieScattering()");
                break;
        }
    }


    private void SetNoiseSource()
    {
        ToggleShaderKeyword(CalculateFogMaterial, "SNOISE", false);
        ToggleShaderKeyword(CalculateFogMaterial, "NOISE2D", false);
        ToggleShaderKeyword(CalculateFogMaterial, "NOISE3D", false);

        switch (fogOptions.noiseSource)
        {
            case NoiseSource.SimplexNoise:
                ToggleShaderKeyword(CalculateFogMaterial, "SNOISE", true);
                break;
            case NoiseSource.Texture2D:
                CalculateFogMaterial.SetTexture(NoiseTexture, fogTexture2D);
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