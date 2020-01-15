using System;
using System.Collections.Generic;
using UnityEngine;

namespace Menu
{
    public class Option : MonoBehaviour
    {
        [DropdownDisplay(typeof(FogOptionsDrawerHelper), "AllOptions")]
        public string targetOption;

        protected VolumetricFogOptions CurrentOptions;
        public virtual void Awake()
        {
            CurrentOptions = FindObjectOfType<VolumetricFog>().fogOptions;
        }
    }
}