// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using UnityEditor;
using UnityEditor.ProjectWindowCallback;
namespace AmplifyShaderEditor
{
	public class DoCreateFunction : EndNameEditAction
	{
		public override void Action( int instanceId, string pathName, string resourceFile )
		{
			UnityEngine.Object obj = EditorUtility.InstanceIDToObject( instanceId );
			AssetDatabase.CreateAsset( obj, AssetDatabase.GenerateUniqueAssetPath( pathName ) );
			AmplifyShaderEditorWindow.LoadShaderFunctionToASE( (AmplifyShaderFunction)obj, false );
		}
	}
}
