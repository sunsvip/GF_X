// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AmplifyShaderEditor
{
	public class Preferences
	{
		public enum ShowOption
		{
			Always = 0,
			OnNewVersion = 1,
			Never = 2
		}

		private static string ProductName = string.Empty;

		private static readonly GUIContent StartUp = new GUIContent( "Show start screen on Unity launch", "You can set if you want to see the start screen everytime Unity launchs, only just when there's a new version available or never." );
		public static readonly string PrefStartUp = "ASELastSession" + ProductName;
		public static ShowOption GlobalStartUp { get { return (ShowOption) EditorPrefs.GetInt( PrefStartUp, 0 ); } }

		private static readonly GUIContent EnableUndo = new GUIContent( "Enable Undo (unstable)", "Enables undo for actions within the shader graph canvas. Currently unstable, use with caution." );
		public static readonly string PrefEnableUndo = "ASEEnableUndo" + ProductName;
		public static bool GlobalEnableUndo { get { return EditorPrefs.GetBool( PrefEnableUndo, false ); } }

		private static readonly GUIContent AutoSRP = new GUIContent( "Auto import SRP shader templates", "By default Amplify Shader Editor checks for your SRP version and automatically imports the correct corresponding shader templates.\nTurn this OFF if you prefer to import them manually." );
		public static readonly string PrefAutoSRP = "ASEAutoSRP" + ProductName;
		public static bool GlobalAutoSRP { get { return EditorPrefs.GetBool( PrefAutoSRP, true ); } }

		private static readonly GUIContent DefineSymbol = new GUIContent( "Add Amplify Shader Editor define symbol", "Turning it OFF will disable the automatic insertion of the define symbol and remove it from the list while turning it ON will do the opposite.\nThis is used for compatibility with other plugins, if you are not sure if you need this leave it ON." );
		public static readonly string PrefDefineSymbol = "ASEDefineSymbol" + ProductName;
		public static bool GlobalDefineSymbol { get { return EditorPrefs.GetBool( PrefDefineSymbol, true ); } }

		private static readonly GUIContent ClearLog = new GUIContent( "Clear Log on Update", "Clears the previously generated log each time the Update button is pressed" );
		public static readonly string PrefClearLog = "ASEClearLog" + ProductName;
		public static bool GlobalClearLog { get { return EditorPrefs.GetBool( PrefClearLog, true ); } }

		private static readonly GUIContent LogShaderCompile = new GUIContent( "Log Shader Compile", "Log message to console when a shader compilation is finished" );
		public static readonly string PrefLogShaderCompile = "ASELogShaderCompile" + ProductName;
		public static bool GlobalLogShaderCompile { get { return EditorPrefs.GetBool( PrefLogShaderCompile, false ); } }

		private static readonly GUIContent LogBatchCompile = new GUIContent( "Log Batch Compile", "Log message to console when a batch compilation is finished" );
		public static readonly string PrefLogBatchCompile = "ASELogBatchCompile" + ProductName;
		public static bool GlobalLogBatchCompile { get { return EditorPrefs.GetBool( PrefLogBatchCompile, false ); } }

		private static readonly GUIContent UpdateOnSceneSave = new GUIContent( "Update on Scene save (Ctrl+S)", "ASE is aware of Ctrl+S and will use it to save shader" );
		public static readonly string PrefUpdateOnSceneSave = "ASEUpdateOnSceneSave" + ProductName;
		public static bool GlobalUpdateOnSceneSave { get { return EditorPrefs.GetBool( PrefUpdateOnSceneSave, true ); } }

		private static readonly GUIContent DisablePreviews = new GUIContent( "Disable Node Previews", "Disable preview on nodes from being updated to boost up performance on large graphs" );
		public static readonly string PrefDisablePreviews = "ASEActivatePreviews" + ProductName;
		public static bool GlobalDisablePreviews { get { return EditorPrefs.GetBool( PrefDisablePreviews, false ); } }

		private static readonly GUIContent ForceTemplateMinShaderModel = new GUIContent( "Force Template Min. Shader Model", "If active, when loading a shader its shader model will be replaced by the one specified in template if what is loaded is below the one set over the template." );
		public static readonly string PrefForceTemplateMinShaderModel = "ASEForceTemplateMinShaderModel" + ProductName;
		public static bool GlobalForceTemplateMinShaderModel { get { return EditorPrefs.GetBool( PrefForceTemplateMinShaderModel, true ); } }

		private static readonly GUIContent ForceTemplateInlineProperties = new GUIContent( "Force Template Inline Properties", "If active, defaults all inline properties to template values." );
		public static readonly string PrefForceTemplateInlineProperties = "ASEForceTemplateInlineProperties" + ProductName;
		public static bool GlobalForceTemplateInlineProperties { get { return EditorPrefs.GetBool( PrefForceTemplateInlineProperties, false ); } }

		[SettingsProvider]
		public static SettingsProvider ImpostorsSettings()
		{
			var provider = new SettingsProvider( "Preferences/Amplify Shader Editor", SettingsScope.User )
			{
				guiHandler = ( string searchContext ) =>
				{
					PreferencesGUI();
				},

				keywords = new HashSet<string>( new[] { "start", "screen", "import", "shader", "templates", "macros", "macros", "define", "symbol" } ),

			};
			return provider;
		}

		[InitializeOnLoadMethod]
		public static void Initialize()
		{
			ProductName = Application.productName;
		}

		public static void PreferencesGUI()
		{
			var cache = EditorGUIUtility.labelWidth;
			EditorGUIUtility.labelWidth = 250;
			{
				EditorGUI.BeginChangeCheck();
				ShowOption startUp = GlobalStartUp;
				startUp = ( ShowOption )EditorGUILayout.EnumPopup( StartUp, startUp );
				if( EditorGUI.EndChangeCheck() )
				{
					EditorPrefs.SetInt( PrefStartUp, ( int )startUp );
				}
			}

			{
				EditorGUI.BeginChangeCheck();
				bool enableUndo = EditorGUILayout.Toggle( EnableUndo, GlobalEnableUndo );
				if ( EditorGUI.EndChangeCheck() )
				{
					EditorPrefs.SetBool( PrefEnableUndo, enableUndo );
				}
			}

			{
				EditorGUI.BeginChangeCheck();
				bool autoSRP = EditorGUILayout.Toggle( AutoSRP, GlobalAutoSRP );
				if( EditorGUI.EndChangeCheck() )
				{
					EditorPrefs.SetBool( PrefAutoSRP, autoSRP );
				}
			}

			{
				EditorGUI.BeginChangeCheck();
				bool defineSymbol = EditorGUILayout.Toggle( DefineSymbol, GlobalDefineSymbol );
				if( EditorGUI.EndChangeCheck() )
				{
					EditorPrefs.SetBool( PrefDefineSymbol, defineSymbol );
					if( defineSymbol )
						IOUtils.SetAmplifyDefineSymbolOnBuildTargetGroup( EditorUserBuildSettings.selectedBuildTargetGroup );
					else
						IOUtils.RemoveAmplifyDefineSymbolOnBuildTargetGroup( EditorUserBuildSettings.selectedBuildTargetGroup );
				}
			}

			{
				EditorGUI.BeginChangeCheck();
				bool clearLog = EditorGUILayout.Toggle( ClearLog, GlobalClearLog );
				if( EditorGUI.EndChangeCheck() )
				{
					EditorPrefs.SetBool( PrefClearLog, clearLog );
				}
			}

			{
				EditorGUI.BeginChangeCheck();
				bool logShaderCompile = EditorGUILayout.Toggle( LogShaderCompile, GlobalLogShaderCompile );
				if( EditorGUI.EndChangeCheck() )
				{
					EditorPrefs.SetBool( PrefLogShaderCompile, logShaderCompile );
				}
			}

			{
				EditorGUI.BeginChangeCheck();
				bool logBatchCompile = EditorGUILayout.Toggle( LogBatchCompile, GlobalLogBatchCompile );
				if ( EditorGUI.EndChangeCheck() )
				{
					EditorPrefs.SetBool( PrefLogBatchCompile, logBatchCompile );
				}
			}

			{
				EditorGUI.BeginChangeCheck();
				bool updateOnSceneSave = EditorGUILayout.Toggle( UpdateOnSceneSave, GlobalUpdateOnSceneSave );
				if( EditorGUI.EndChangeCheck() )
				{
					EditorPrefs.SetBool( PrefUpdateOnSceneSave, updateOnSceneSave );
				}
			}

			{
				EditorGUI.BeginChangeCheck();
				bool disablePreviews = EditorGUILayout.Toggle( DisablePreviews, GlobalDisablePreviews );
				if( EditorGUI.EndChangeCheck() )
				{
					EditorPrefs.SetBool( PrefDisablePreviews, disablePreviews );
					UIUtils.ActivatePreviews( !disablePreviews );
				}
			}

			{
				EditorGUI.BeginChangeCheck();
				bool forceTemplateMinShaderModel = EditorGUILayout.Toggle( ForceTemplateMinShaderModel, GlobalForceTemplateMinShaderModel );
				if( EditorGUI.EndChangeCheck() )
				{
					EditorPrefs.SetBool( PrefForceTemplateMinShaderModel, forceTemplateMinShaderModel );
				}
			}

			{
				EditorGUI.BeginChangeCheck();
				bool forceTemplateInlineProperties = EditorGUILayout.Toggle( ForceTemplateInlineProperties, GlobalForceTemplateInlineProperties );
				if( EditorGUI.EndChangeCheck() )
				{
					EditorPrefs.SetBool( PrefForceTemplateInlineProperties, forceTemplateInlineProperties );
				}
			}

			EditorGUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
			if( GUILayout.Button( "Reset and Forget All" ) )
			{
				EditorPrefs.DeleteKey( PrefStartUp );
				EditorPrefs.DeleteKey( PrefEnableUndo );
				EditorPrefs.DeleteKey( PrefAutoSRP );

				EditorPrefs.DeleteKey( PrefDefineSymbol );
				IOUtils.SetAmplifyDefineSymbolOnBuildTargetGroup( EditorUserBuildSettings.selectedBuildTargetGroup );

				EditorPrefs.DeleteKey( PrefClearLog );
				EditorPrefs.DeleteKey( PrefLogShaderCompile);
				EditorPrefs.DeleteKey( PrefLogBatchCompile );
				EditorPrefs.DeleteKey( PrefUpdateOnSceneSave );
				EditorPrefs.DeleteKey( PrefDisablePreviews );
				EditorPrefs.DeleteKey( PrefForceTemplateMinShaderModel );
				EditorPrefs.DeleteKey( PrefForceTemplateInlineProperties );
			}
			EditorGUILayout.EndHorizontal();
			EditorGUIUtility.labelWidth = cache;
		}
	}
}
