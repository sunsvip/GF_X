// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using UnityEngine;
using UnityEditor;
using UnityEditor.ProjectWindowCallback;
using System.IO;
namespace AmplifyShaderEditor
{
	public class DoCreateStandardShader : EndNameEditAction
	{
		public override void Action( int instanceId, string pathName, string resourceFile )
		{
			string uniquePath = AssetDatabase.GenerateUniqueAssetPath( pathName );
			string shaderName = Path.GetFileName( uniquePath );

			if( IOUtils.AllOpenedWindows.Count > 0 )
			{
				EditorWindow openedWindow = AmplifyShaderEditorWindow.GetWindow<AmplifyShaderEditorWindow>();
				AmplifyShaderEditorWindow currentWindow = AmplifyShaderEditorWindow.CreateTab();
				WindowHelper.AddTab( openedWindow, currentWindow );
				UIUtils.CurrentWindow = currentWindow;	
			}
			else
			{
				AmplifyShaderEditorWindow currentWindow = AmplifyShaderEditorWindow.OpenWindow( shaderName, UIUtils.ShaderIcon );
				UIUtils.CurrentWindow = currentWindow;
			}

			Shader shader = UIUtils.CreateNewEmpty( uniquePath, shaderName );
			ProjectWindowUtil.ShowCreatedAsset( shader );
		}
	}

	public class DoCreateTemplateShader : EndNameEditAction
	{
		public override void Action( int instanceId, string pathName, string resourceFile )
		{
			string uniquePath = AssetDatabase.GenerateUniqueAssetPath( pathName );
			string shaderName = Path.GetFileName( uniquePath );
			if( !string.IsNullOrEmpty( UIUtils.NewTemplateGUID ) )
			{
				Shader shader = AmplifyShaderEditorWindow.CreateNewTemplateShader( UIUtils.NewTemplateGUID, uniquePath, shaderName );
				ProjectWindowUtil.ShowCreatedAsset( shader );
			}
		}
	}
}
