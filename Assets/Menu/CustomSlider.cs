using System.Globalization;
using TMPro;
using UnityEditor;
using UnityEditor.UI;
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
            _indicatorText.SetText(value.ToString(CultureInfo.InvariantCulture));
            onValueChanged.AddListener(UpdateIndicatorText);
        }

        public void UpdateIndicatorText(float newValue)
        {
            _indicatorText.SetText(newValue.ToString(CultureInfo.InvariantCulture));
            EventManager.RaymarchStepsChanged(newValue); // TODO: Does not belong here
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

    [CustomEditor(typeof(CustomSlider))]
    public class CustomSliderEditor : SliderEditor
    {
        public override void OnInspectorGUI()
        {
            var targetSlider = (CustomSlider)target;
            
            targetSlider.onHoverColor = EditorGUILayout.ColorField("On hover color", targetSlider.onHoverColor);
            targetSlider.onClickColor = EditorGUILayout.ColorField("On click color", targetSlider.onClickColor);
 
            base.OnInspectorGUI();
        }
    }
}