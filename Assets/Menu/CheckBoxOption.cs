using UnityEngine.UI;

namespace Menu
{
    public class CheckBoxOption : Option
    {
        private Toggle _toggle;
        
        public void OnValueChanged(bool newValue)
        {
            CurrentOptions.GetType().GetField(targetOption).SetValue(CurrentOptions, newValue);
        }

        public override void Awake()
        {
            base.Awake();

            _toggle = GetComponentInChildren<Toggle>();
            _toggle.onValueChanged.AddListener(OnValueChanged);

            _toggle.isOn = (bool) CurrentOptions.GetType().GetField(targetOption).GetValue(CurrentOptions);
        }
    }
}