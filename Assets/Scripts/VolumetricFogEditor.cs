#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(VolumetricFog))]
public class VolumetricFogEditor : Editor {
	public override void OnInspectorGUI()
	{
		base.OnInspectorGUI();

		var fog = (VolumetricFog) target;
		
		if (GUILayout.Button("Set Noise Source and Regenerate 3D texture"))
		{
			fog.Regenerate3DTexture();
		}
	}
}
#endif
