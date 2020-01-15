using System;
using System.Linq;

namespace Menu
{
    public class CarouselOption : Option
    {
        private UICarousel _carousel;
        
        public void OnValueChanged(string newValue)
        {
            var fieldInfo = CurrentOptions.GetType().GetField(targetOption);
            var value = System.Enum.Parse(fieldInfo.FieldType, newValue);
            fieldInfo.SetValue(CurrentOptions, value);
        }

        public override void Awake()
        {
            base.Awake();

            _carousel = GetComponentInChildren<UICarousel>();
            _carousel.onValueChanged += OnValueChanged;

            var fieldInfo = CurrentOptions.GetType().GetField(targetOption);
            var defaultValue = fieldInfo.GetValue(CurrentOptions);
            var allValues = System.Enum.GetNames(fieldInfo.FieldType).ToList();
            
            print(string.Join(" ,", allValues));
            
            _carousel.SetOptions(allValues);
            _carousel.SetCurrentText(defaultValue.ToString());
        }

        private void OnDestroy()
        {
            _carousel.onValueChanged -= OnValueChanged;

        }
    }
}