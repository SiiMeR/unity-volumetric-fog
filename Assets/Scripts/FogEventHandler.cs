using System;
using Menu;

public class FogEventHandler : Singleton<FogEventHandler>
{
    private VolumetricFog _fog;

    private void Awake()
    {
        EventManager.onRaymarchStepsChanged += OnRaymarchStepsChanged;
    }

    private void OnDestroy()
    {
        EventManager.onRaymarchStepsChanged -= OnRaymarchStepsChanged;
    }

    private void Start()
    {
        _fog = FindObjectOfType<VolumetricFog>();
    }

    private void OnRaymarchStepsChanged(float newValue)
    {
        _fog._RayMarchSteps = (int) newValue;
    }
}