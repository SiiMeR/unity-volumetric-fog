using Menu;
using UnityEngine;

public class FogEventHandler : Singleton<FogEventHandler>
{
    private VolumetricFog _fog;

    private void Awake()
    {
        EventManager.onRaymarchStepsChanged += OnRaymarchStepsChanged;
        EventManager.onFogColorChanged += OnFogColorChanged;
    }

    private void OnDestroy()
    {
        EventManager.onRaymarchStepsChanged -= OnRaymarchStepsChanged;
        EventManager.onFogColorChanged += OnFogColorChanged;
    }

    private void Start()
    {
        _fog = FindObjectOfType<VolumetricFog>();
    }

    private void OnRaymarchStepsChanged(float newValue)
    {
        _fog.fogOptions.rayMarchSteps = (int) newValue;
    }  
    
    private void OnFogColorChanged(Color newColor)
    {
        _fog.fogOptions.fogInLightColor = newColor;
    }
}