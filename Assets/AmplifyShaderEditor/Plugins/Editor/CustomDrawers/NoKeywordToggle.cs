// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using UnityEngine;
using UnityEditor;
using System;

public class NoKeywordToggle : MaterialPropertyDrawer
{
    
    public override void OnGUI(Rect position, MaterialProperty prop, String label, MaterialEditor editor) {
        bool value = (prop.floatValue != 0.0f);

        EditorGUI.BeginChangeCheck();
		{
			EditorGUI.showMixedValue = prop.hasMixedValue;
			value = EditorGUI.Toggle( position, label, value );
			EditorGUI.showMixedValue = false;
		}
        if (EditorGUI.EndChangeCheck())
		{
            prop.floatValue = value ? 1.0f : 0.0f;
        }
    }
}
