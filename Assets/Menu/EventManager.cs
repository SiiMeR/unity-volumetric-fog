using UnityEngine;

namespace Menu
{
    public static class EventManager
    {
        public delegate void OnRaymarchStepsChanged(float newValue);
        
        public delegate void OnFogColorChanged(Color newColor);

        public static event OnRaymarchStepsChanged onRaymarchStepsChanged;
        
        public static event OnFogColorChanged onFogColorChanged;

        public static void RaymarchStepsChanged(float newValue)
        {
            onRaymarchStepsChanged?.Invoke(newValue);
        }

        public static void FogColorChanged(Color newColor)
        {
            onFogColorChanged?.Invoke(newColor);
        }
    }
}