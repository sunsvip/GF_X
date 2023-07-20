// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using UnityEngine;
using UnityEditor;
using System;

public class RemapSlidersFull : MaterialPropertyDrawer
{
	public override void OnGUI( Rect position, MaterialProperty prop, String label, MaterialEditor editor )
	{
		EditorGUI.BeginChangeCheck();
		Vector4 value = prop.vectorValue;

		EditorGUI.showMixedValue = prop.hasMixedValue;
		
		var cacheLabel = EditorGUIUtility.labelWidth;
		var cacheField = EditorGUIUtility.fieldWidth;
		if( cacheField <= 64 )
		{
			float total = position.width;
			EditorGUIUtility.labelWidth = Mathf.Ceil( 0.45f * total ) - 30;
			EditorGUIUtility.fieldWidth = Mathf.Ceil( 0.55f * total ) + 30;
		}

		EditorGUI.MinMaxSlider( position, label, ref value.x, ref value.y, value.z, value.w );

		EditorGUIUtility.labelWidth = cacheLabel;
		EditorGUIUtility.fieldWidth = cacheField;
		EditorGUI.showMixedValue = false;
		if( EditorGUI.EndChangeCheck() )
		{
			prop.vectorValue = value;
		}
	}
}
