// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

#if UNITY_2019_3_OR_NEWER
using UnityEditor;
using UnityEngine;
using System;
using System.Reflection;

namespace AmplifyShaderEditor
{
	public class ASEDiffusionProfile : MaterialPropertyDrawer
	{
		string m_hashField = string.Empty;

		public ASEDiffusionProfile( object guidField )
		{
			m_hashField = guidField.ToString();
		}

		public override void OnGUI( Rect position, MaterialProperty prop, String label, MaterialEditor editor )
		{
			var guid = HDUtilsEx.ConvertVector4ToGUID( prop.vectorValue );
			var profile = AssetDatabase.LoadAssetAtPath( AssetDatabase.GUIDToAssetPath( guid ), DiffusionProfileSettingsEx.Type );
			
			EditorGUI.BeginChangeCheck();
			profile = EditorGUI.ObjectField( position, new GUIContent( label ), profile, DiffusionProfileSettingsEx.Type, false );
			if( EditorGUI.EndChangeCheck() )
			{
				Vector4 newGuid = Vector4.zero;
				float hash = 0;
				if( profile != null )
				{
					var guid2 = AssetDatabase.AssetPathToGUID( AssetDatabase.GetAssetPath( profile ) );
					newGuid = HDUtilsEx.ConvertGUIDToVector4( guid2 );
					hash = HDShadowUtilsEx.Asfloat( DiffusionProfileSettingsEx.Hash( profile ) );
				}
				prop.vectorValue = newGuid;

				var hashField = MaterialEditor.GetMaterialProperty( new UnityEngine.Object[] { editor.target }, m_hashField );
				if( hashField != null )
				{
					hashField.floatValue = hash;
				}
			}

			if( profile == null )
				prop.vectorValue = Vector4.zero;

			DiffusionProfileMaterialUIEx.DrawDiffusionProfileWarning( profile );
		}

		private static class DiffusionProfileMaterialUIEx
		{
			private static System.Type type = null;
			public static System.Type Type { get { return ( type == null ) ? type = System.Type.GetType( "UnityEditor.Rendering.HighDefinition.DiffusionProfileMaterialUI, Unity.RenderPipelines.HighDefinition.Editor" ) : type; } }

			public static void DrawDiffusionProfileWarning( UnityEngine.Object obj )
			{
				object[] parameters = new object[] { obj };
				MethodInfo method = Type.GetMethod( "DrawDiffusionProfileWarning", BindingFlags.Static | BindingFlags.NonPublic );
				method.Invoke( null, parameters );
			}
		}

		private static class HDShadowUtilsEx
		{
			private static System.Type type = null;
			public static System.Type Type { get { return ( type == null ) ? type = System.Type.GetType( "UnityEngine.Rendering.HighDefinition.HDShadowUtils, Unity.RenderPipelines.HighDefinition.Runtime" ) : type; } }

			public static float Asfloat( uint val )
			{
				object[] parameters = new object[] { val };
				MethodInfo method = Type.GetMethod( "Asfloat", new Type[] { typeof( uint ) } );
				return (float)method.Invoke( null, parameters );
			}

			public static uint Asuint( float val )
			{
				object[] parameters = new object[] { val };
				MethodInfo method = Type.GetMethod( "Asuint", new Type[] { typeof( float ) } );
				return (uint)method.Invoke( null, parameters );
			}
		}
	}
}
#endif //UNITY_2019_3_OR_NEWER
