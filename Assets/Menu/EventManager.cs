using UnityEngine;

namespace Menu
{
    public static class EventManager
    {
        public delegate void OnRaymarchStepsChanged(float newValue);

        public static event OnRaymarchStepsChanged onRaymarchStepsChanged;

        public static void RaymarchStepsChanged(float newValue)
        {
            onRaymarchStepsChanged?.Invoke(newValue);
        }
    }
}