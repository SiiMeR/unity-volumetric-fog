using System;
using UnityEngine;
using UnityEngine.UI;

namespace Menu
{
    public class SliderOption : Option
    {
        private Slider _slider;
        
        public void OnValueChanged(float newValue)
        {
            var fieldInfo = CurrentOptions.GetType().GetField(targetOption);
            if (_slider.wholeNumbers) // ints
            {
                fieldInfo.SetValue(CurrentOptions, (int) newValue);
            }
            else // floats
            {
                fieldInfo.SetValue(CurrentOptions, newValue);
            }
        }

        public override void Awake()
        {
            base.Awake();

            _slider = GetComponentInChildren<Slider>();
            _slider.onValueChanged.AddListener(OnValueChanged);

            try
            {
                if (_slider.wholeNumbers) // ints
                {
                    _slider.value = (int) CurrentOptions.GetType().GetField(targetOption).GetValue(CurrentOptions);
                }
                else // floats
                {
                    _slider.value = (float) CurrentOptions.GetType().GetField(targetOption).GetValue(CurrentOptions);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Unable to set slider value for option: {targetOption}");
            }

        }
    }
}