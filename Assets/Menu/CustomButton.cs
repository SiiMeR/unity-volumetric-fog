using DG.Tweening;
using TMPro;
using UnityEditor;
using UnityEditor.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Menu
{
    public class CustomButton : Button
    {
        public Color onHoverColor = Color.black;
        private TextMeshProUGUI _text;
        
        protected override void Awake()
        {
            base.Awake();
            _text = GetComponentInChildren<TextMeshProUGUI>();
            targetGraphic.color = onHoverColor;
            onHoverColor.a = 0;
        }

        public override void OnPointerEnter(PointerEventData eventData)
        {
            targetGraphic.DOFade(1.0f, 0.15f);
            _text.DOColor(Color.white, .15f);
        }

        public override void OnPointerExit(PointerEventData eventData)    
        {
            targetGraphic.DOFade(0.0f, 0.15f);
            _text.DOColor(Color.black, .15f);
        }
    }

    [CustomEditor(typeof(CustomButton))]
    public class CustomButtonEditor : ButtonEditor
    {
        public override void OnInspectorGUI()
        {
            var targetMenuButton = (CustomButton)target;
            targetMenuButton.onHoverColor = EditorGUILayout.ColorField("On hover color", targetMenuButton.onHoverColor);
 
            base.OnInspectorGUI();
        }
    }
}
