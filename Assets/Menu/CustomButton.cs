using DG.Tweening;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Menu
{
    public class CustomButton : Button
    {
        [HideInInspector] public Color onHoverColor;
        private TextMeshProUGUI _text;
        
        protected override void Awake()
        {
            base.Awake();
            _text = GetComponentInChildren<TextMeshProUGUI>();
        }

        protected override void OnEnable()
        {
            targetGraphic.color = onHoverColor;
        }

        public override void OnPointerEnter(PointerEventData eventData)
        {
            targetGraphic.DOFade(onHoverColor.a, 0.15f);
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

            if (GUI.changed)
            {
                EditorUtility.SetDirty(targetMenuButton);
                EditorSceneManager.MarkSceneDirty(targetMenuButton.gameObject.scene);
            }
        }
    }
}
