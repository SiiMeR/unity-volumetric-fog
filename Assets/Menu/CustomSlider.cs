using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Menu
{
    public class CustomSlider : Slider
    {
        public Color onHoverColor = Color.gray;
        public Color onClickColor = Color.black;

        private Color _originalColor;
        private TextMeshProUGUI _indicatorText;
        protected override void Awake()
        {
            base.Awake();

            _originalColor = targetGraphic.color;

            _indicatorText = GetComponentInChildren<TextMeshProUGUI>();
            
            UpdateIndicatorText(value);
            
            onValueChanged.AddListener(UpdateIndicatorText);
        }

        public void UpdateIndicatorText(float newValue)
        {
            if (wholeNumbers)
            {
                _indicatorText.SetText(newValue.ToString(CultureInfo.InvariantCulture));
            }
            else
            {
                _indicatorText.SetText(newValue.ToString("0.000"));
            }
        }
        
        public override void OnPointerDown(PointerEventData eventData)
        {
            base.OnPointerDown(eventData);
            // OptionsMenuMainScreenController.Instance.GetComponent<CanvasGroup>().alpha = .3f;
        }
        
        public override void OnPointerUp(PointerEventData eventData)
        {
            base.OnPointerUp(eventData);
            // OptionsMenuMainScreenController.Instance.GetComponent<CanvasGroup>().alpha = 1.0f;
        }

        // public override void OnPointerEnter(PointerEventData eventData)
        // {
        //     targetGraphic.DOColor(onHoverColor, .25f);
        // }
        //
        // public override void OnPointerExit(PointerEventData eventData)
        // {
        //     targetGraphic.DOColor(_originalColor, .25f);
        // }
    }
}