using UnityEngine;

namespace Menu
{
    public static class EventManager
    {
        public delegate void OnFogInitialized(VolumetricFogOptions options);

        public static event OnFogInitialized onFogInitialized;
        
        public static void FogOptionsChanged(VolumetricFogOptions options)
        {
            onFogInitialized?.Invoke(options);
        }
    }
}