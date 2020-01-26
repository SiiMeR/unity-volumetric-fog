using Menu;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.UI;
using UnityEngine;

namespace Editor
{
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