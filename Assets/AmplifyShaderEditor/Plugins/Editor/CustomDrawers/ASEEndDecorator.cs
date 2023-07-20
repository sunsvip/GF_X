// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using UnityEngine;
using UnityEditor;
using System;
using AmplifyShaderEditor;

public class ASEEndDecorator : MaterialPropertyDrawer
{
	bool m_applyNext = false;

	public override void OnGUI( Rect position, MaterialProperty prop, String label, MaterialEditor editor )
	{
		if( prop.applyPropertyCallback == null )
			prop.applyPropertyCallback = Testc;

		if( GUI.changed || m_applyNext )
		{
			m_applyNext = false;
			Material mat = editor.target as Material;
			UIUtils.CopyValuesFromMaterial( mat );
		}
	}

	bool Testc( MaterialProperty prop, int changeMask, object previousValue )
	{
		m_applyNext = true;
		return false;
	}

	public override float GetPropertyHeight( MaterialProperty prop, string label, MaterialEditor editor )
	{
		return 0;
	}
}
