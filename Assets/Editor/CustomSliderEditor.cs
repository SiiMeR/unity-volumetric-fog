using Menu;
using UnityEditor;
using UnityEditor.UI;

namespace Editor
{
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