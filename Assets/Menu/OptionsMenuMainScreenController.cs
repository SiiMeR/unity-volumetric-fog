using System;
using Menu.Framework;

namespace Menu
{
    public class OptionsMenuMainScreenController : AbstractScreen<OptionsMenuMainScreenController>
    {
        private void Start()
        {
            var volumetricFog = FindObjectOfType<VolumetricFog>();
            if (!volumetricFog)
            {
                return;
            }
            
            // TODO: Bind correct configuration values on opening this menu
        }
    }
}
