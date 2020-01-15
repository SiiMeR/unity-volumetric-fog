using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Menu
{
    public static class FogOptionsDrawerHelper
    {
#if UNITY_EDITOR
        public static string[] AllOptions()
        {
            var fogOptions = ScriptableObject.CreateInstance<VolumetricFogOptions>();
            return fogOptions.GetType()
                .GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Select(info => info.Name)
                .ToArray();
        }
#endif
    }
}