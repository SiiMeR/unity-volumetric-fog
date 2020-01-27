using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace Menu
{
    public class ResolutionDisplay : MonoBehaviour
    {
        private TMP_Dropdown _dropDown;

        private void Awake()
        {
            _dropDown = GetComponentInChildren<TMP_Dropdown>();
        }

        // Start is called before the first frame update
        void Start()
        {
            var resolutions = Screen.resolutions.Select(res => $"{res.width}x{res.height}").Distinct().ToList();
            var currentResolution = $"{Screen.currentResolution.width}x{Screen.currentResolution.height}";
            var index = resolutions.IndexOf(currentResolution);
            _dropDown.ClearOptions();
            _dropDown.AddOptions(new List<string>{"256x144", "426x240", "640x360"});
            _dropDown.AddOptions(resolutions);
            _dropDown.SetValueWithoutNotify(index);
            _dropDown.onValueChanged.AddListener(OnResolutionChanged);
        }

        public void OnResolutionChanged(int newRes)
        {
            var resolution = _dropDown.options[newRes].text.Split('x').Select(int.Parse).ToList();
            print($"{resolution[0]}x{resolution[1]}");
            var preferredRefreshRate = Screen.currentResolution.refreshRate;
            
            Screen.SetResolution(resolution[0], resolution[1], true, preferredRefreshRate);
            
        }
        
        private void OnDestroy()
        {
            _dropDown.onValueChanged.RemoveListener(OnResolutionChanged);
        }

        // Update is called once per frame
        void Update()
        {
        
        }
    }
}
