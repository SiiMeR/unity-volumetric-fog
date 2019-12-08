using UnityEngine;

namespace Menu
{
    public class EventManager : Singleton<EventManager>
    {
        public delegate void OnRaymarchStepsChanged(float newValue);

        public static event OnRaymarchStepsChanged onRaymarchStepsChanged;

        public static void RaymarchStepsChanged(float newValue)
        {
            onRaymarchStepsChanged?.Invoke(newValue);
        }
    }
}