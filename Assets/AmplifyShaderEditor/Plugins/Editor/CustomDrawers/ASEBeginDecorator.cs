// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using UnityEngine;
using UnityEditor;
using System;
using AmplifyShaderEditor;

public class ASEBeginDecorator : MaterialPropertyDrawer
{
	const int Separator = 2;
	public override void OnGUI( Rect position, MaterialProperty prop, String label, MaterialEditor editor )
	{
		Rect button = position;
		button.height = EditorGUIUtility.singleLineHeight;

		if( GUI.Button( button, "Open in Shader Editor" ) )
		{
			Material mat = editor.target as Material;
			ASEPackageManagerHelper.SetupLateMaterial( mat );
		}
	}

	public override float GetPropertyHeight( MaterialProperty prop, string label, MaterialEditor editor )
	{
		return EditorGUIUtility.singleLineHeight + Separator;
	}
}
