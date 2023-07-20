// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using UnityEngine;
using UnityEditor;
using System;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;

namespace AmplifyShaderEditor
{
	public class ASEStartScreen : EditorWindow
	{
		[MenuItem( "Window/Amplify Shader Editor/Start Screen", false, 1999 )]
		public static void Init()
		{
			ASEStartScreen window = (ASEStartScreen)GetWindow( typeof( ASEStartScreen ), true, "Amplify Shader Editor Start Screen" );
			window.minSize = new Vector2( 650, 500 );
			window.maxSize = new Vector2( 650, 500 );
			window.Show();
		}

		private static readonly string ChangeLogGUID = "580cccd3e608b7f4cac35ea46d62d429";
		private static readonly string ResourcesGUID = "c0a0a980c9ba86345bc15411db88d34f";
		private static readonly string BuiltInGUID = "e00e6f90ab8233e46a41c5e33917c642";
		private static readonly string UniversalGUID = "a9d68dd8913f05d4d9ce75e7b40c6044";
		private static readonly string HighDefinitionGUID = "d1c0b77896049554fa4b635531caf741";

		private static readonly string IconGUID = "2c6536772776dd84f872779990273bfc";

		public static readonly string ChangelogURL = "http://amplify.pt/Banner/ASEchangelog.json";

		private static readonly string ManualURL = "http://wiki.amplify.pt/index.php?title=Unity_Products:Amplify_Shader_Editor/Manual";
		private static readonly string BasicURL = "http://wiki.amplify.pt/index.php?title=Unity_Products:Amplify_Shader_Editor/Tutorials#Official_-_Basics";
		private static readonly string BeginnerURL = "http://wiki.amplify.pt/index.php?title=Unity_Products:Amplify_Shader_Editor/Tutorials#Official_-_Beginner_Series";
		private static readonly string NodesURL = "http://wiki.amplify.pt/index.php?title=Unity_Products:Amplify_Shader_Editor/Nodes";
		private static readonly string SRPURL = "http://wiki.amplify.pt/index.php?title=Unity_Products:Amplify_Shader_Editor/Scriptable_Rendering_Pipeline";
		private static readonly string FunctionsURL = "http://wiki.amplify.pt/index.php?title=Unity_Products:Amplify_Shader_Editor/Manual#Shader_Functions";
		private static readonly string TemplatesURL = "http://wiki.amplify.pt/index.php?title=Unity_Products:Amplify_Shader_Editor/Templates";
		private static readonly string APIURL = "http://wiki.amplify.pt/index.php?title=Unity_Products:Amplify_Shader_Editor/API";

		private static readonly string DiscordURL = "https://discordapp.com/invite/EdrVAP5";
		private static readonly string ForumURL = "https://forum.unity.com/threads/best-tool-asset-store-award-amplify-shader-editor-node-based-shader-creation-tool.430959/";

		private static readonly string SiteURL = "http://amplify.pt/download/";
		private static readonly string StoreURL = "https://assetstore.unity.com/packages/tools/visual-scripting/amplify-shader-editor-68570";

		private static readonly GUIContent SamplesTitle = new GUIContent( "Shader Samples", "Import samples according to you project rendering pipeline" );
		private static readonly GUIContent ResourcesTitle = new GUIContent( "Learning Resources", "Check the online wiki for various topics about how to use ASE with node examples and explanations" );
		private static readonly GUIContent CommunityTitle = new GUIContent( "Community", "Need help? Reach us through our discord server or the official support Unity forum" );
		private static readonly GUIContent UpdateTitle = new GUIContent( "Latest Update", "Check the lastest additions, improvements and bug fixes done to ASE" );
		private static readonly GUIContent ASETitle = new GUIContent( "Amplify Shader Editor", "Are you using the latest version? Now you know" );

		private const string OnlineVersionWarning = "Please enable \"Allow downloads over HTTP*\" in Player Settings to access latest version information via Start Screen.";

		Vector2 m_scrollPosition = Vector2.zero;
		Preferences.ShowOption m_startup = Preferences.ShowOption.Never;

		[NonSerialized]
		Texture packageIcon = null;
		[NonSerialized]
		Texture textIcon = null;
		[NonSerialized]
		Texture webIcon = null;

		GUIContent HDRPbutton = null;
		GUIContent URPbutton = null;
		GUIContent BuiltInbutton = null;

		GUIContent Manualbutton = null;
		GUIContent Basicbutton = null;
		GUIContent Beginnerbutton = null;
		GUIContent Nodesbutton = null;
		GUIContent SRPusebutton = null;
		GUIContent Functionsbutton = null;
		GUIContent Templatesbutton = null;
		GUIContent APIbutton = null;

		GUIContent DiscordButton = null;
		GUIContent ForumButton = null;

		GUIContent ASEIcon = null;
		RenderTexture rt;

		[NonSerialized]
		GUIStyle m_buttonStyle = null;
		[NonSerialized]
		GUIStyle m_buttonLeftStyle = null;
		[NonSerialized]
		GUIStyle m_buttonRightStyle = null;
		[NonSerialized]
		GUIStyle m_minibuttonStyle = null;
		[NonSerialized]
		GUIStyle m_labelStyle = null;
		[NonSerialized]
		GUIStyle m_linkStyle = null;

		private ChangeLogInfo m_changeLog;
		private bool m_infoDownloaded = false;
		private string m_newVersion = string.Empty;

		private static Dictionary<int, ASESRPPackageDesc> m_srpSamplePackages = new Dictionary<int, ASESRPPackageDesc>()
		{
			{ ( int )ASESRPBaseline.ASE_SRP_10, new ASESRPPackageDesc( ASESRPBaseline.ASE_SRP_10, "2edbf4a9b9544774bbef617e92429664", "9da5530d5ebfab24c8ecad68795e720f" ) },
			{ ( int )ASESRPBaseline.ASE_SRP_11, new ASESRPPackageDesc( ASESRPBaseline.ASE_SRP_11, "2edbf4a9b9544774bbef617e92429664", "9da5530d5ebfab24c8ecad68795e720f" ) },
			{ ( int )ASESRPBaseline.ASE_SRP_12, new ASESRPPackageDesc( ASESRPBaseline.ASE_SRP_12, "13ab599a7bda4e54fba3e92a13c9580a", "aa102d640b98b5d4781710a3a3dd6983" ) },
			{ ( int )ASESRPBaseline.ASE_SRP_13, new ASESRPPackageDesc( ASESRPBaseline.ASE_SRP_13, "13ab599a7bda4e54fba3e92a13c9580a", "aa102d640b98b5d4781710a3a3dd6983" ) },
			{ ( int )ASESRPBaseline.ASE_SRP_14, new ASESRPPackageDesc( ASESRPBaseline.ASE_SRP_14, "f6f268949ccf3f34fa4d18e92501ed82", "7a0bb33169d95ec499136d59cb25918b" ) },
		};

		private void OnEnable()
		{
			rt = new RenderTexture( 16, 16, 0 );
			rt.Create();

			m_startup = (Preferences.ShowOption)EditorPrefs.GetInt( Preferences.PrefStartUp, 0 );

			if( textIcon == null )
			{
				Texture icon = EditorGUIUtility.IconContent( "TextAsset Icon" ).image;
				var cache = RenderTexture.active;
				RenderTexture.active = rt;
				Graphics.Blit( icon, rt );
				RenderTexture.active = cache;
				textIcon = rt;

				Manualbutton = new GUIContent( " Manual", textIcon );
				Basicbutton = new GUIContent( " Basic use tutorials", textIcon );
				Beginnerbutton = new GUIContent( " Beginner Series", textIcon );
				Nodesbutton = new GUIContent( " Node List", textIcon );
				SRPusebutton = new GUIContent( " SRP HDRP/URP use", textIcon );
				Functionsbutton = new GUIContent( " Shader Functions", textIcon );
				Templatesbutton = new GUIContent( " Shader Templates", textIcon );
				APIbutton = new GUIContent( " Node API", textIcon );
			}

			if( packageIcon == null )
			{
				packageIcon = EditorGUIUtility.IconContent( "BuildSettings.Editor.Small" ).image;
				HDRPbutton = new GUIContent( " HDRP Samples", packageIcon );
				URPbutton = new GUIContent( " URP Samples", packageIcon );
				BuiltInbutton = new GUIContent( " Built-In Samples", packageIcon );
			}

			if( webIcon == null )
			{
				webIcon = EditorGUIUtility.IconContent( "BuildSettings.Web.Small" ).image;
				DiscordButton = new GUIContent( " Discord", webIcon );
				ForumButton = new GUIContent( " Unity Forum", webIcon );
			}

			if( m_changeLog == null )
			{
				var changelog = AssetDatabase.LoadAssetAtPath<TextAsset>( AssetDatabase.GUIDToAssetPath( ChangeLogGUID ) );
				string lastUpdate = string.Empty;
				if(changelog != null )
				{
					int oldestReleaseIndex = changelog.text.LastIndexOf( string.Format( "v{0}.{1}.{2}", VersionInfo.Major, VersionInfo.Minor, VersionInfo.Release ) );

					lastUpdate = changelog.text.Substring( 0, changelog.text.IndexOf( "\nv", oldestReleaseIndex + 25 ) );// + "\n...";
					lastUpdate = lastUpdate.Replace( "* ", "\u2022 " );
				}
				m_changeLog = new ChangeLogInfo( VersionInfo.FullNumber, lastUpdate );
			}

			if( ASEIcon == null )
			{
				ASEIcon = new GUIContent( AssetDatabase.LoadAssetAtPath<Texture2D>( AssetDatabase.GUIDToAssetPath( IconGUID ) ) );
			}
		}

		private void OnDisable()
		{
			if( rt != null )
			{
				rt.Release();
				DestroyImmediate( rt );
			}
		}

		public void OnGUI()
		{
			if( !m_infoDownloaded )
			{
				m_infoDownloaded = true;

#if UNITY_2022_1_OR_NEWER
				if ( PlayerSettings.insecureHttpOption == InsecureHttpOption.NotAllowed )
				{
					Debug.LogWarning( "[AmplifyShaderEditor] " + OnlineVersionWarning );
				}
				else
#endif
				{
					StartBackgroundTask( StartRequest( ChangelogURL, () =>
					{
						var temp = ChangeLogInfo.CreateFromJSON( www.downloadHandler.text );
						if( temp != null && temp.Version >= m_changeLog.Version )
						{
							m_changeLog = temp;
						}

						int version = m_changeLog.Version;
						int major = version / 10000;
						int minor = version / 1000 - major * 10;
						int release = version / 100 - ( version / 1000 ) * 10;
						int revision = version - ( version / 100 ) * 100;

						m_newVersion = major + "." + minor + "." + release + ( revision > 0 ? "." + revision : "" );

						Repaint();
					} ) );
				}
			}

			if( m_buttonStyle == null )
			{
				m_buttonStyle = new GUIStyle( GUI.skin.button );
				m_buttonStyle.alignment = TextAnchor.MiddleLeft;
			}

			if( m_buttonLeftStyle == null )
			{
				m_buttonLeftStyle = new GUIStyle( "ButtonLeft" );
				m_buttonLeftStyle.alignment = TextAnchor.MiddleLeft;
				m_buttonLeftStyle.margin = m_buttonStyle.margin;
				m_buttonLeftStyle.margin.right = 0;
			}

			if( m_buttonRightStyle == null )
			{
				m_buttonRightStyle = new GUIStyle( "ButtonRight" );
				m_buttonRightStyle.alignment = TextAnchor.MiddleLeft;
				m_buttonRightStyle.margin = m_buttonStyle.margin;
				m_buttonRightStyle.margin.left = 0;
			}

			if( m_minibuttonStyle == null )
			{
				m_minibuttonStyle = new GUIStyle( "MiniButton" );
				m_minibuttonStyle.alignment = TextAnchor.MiddleLeft;
				m_minibuttonStyle.margin = m_buttonStyle.margin;
				m_minibuttonStyle.margin.left = 20;
				m_minibuttonStyle.normal.textColor = m_buttonStyle.normal.textColor;
				m_minibuttonStyle.hover.textColor = m_buttonStyle.hover.textColor;
			}

			if( m_labelStyle == null )
			{
				m_labelStyle = new GUIStyle( "BoldLabel" );
				m_labelStyle.margin = new RectOffset( 4, 4, 4, 4 );
				m_labelStyle.padding = new RectOffset( 2, 2, 2, 2 );
				m_labelStyle.fontSize = 13;
			}

			if( m_linkStyle == null )
			{
				var inv = AssetDatabase.LoadAssetAtPath<Texture2D>( AssetDatabase.GUIDToAssetPath( "1004d06b4b28f5943abdf2313a22790a" ) ); // find a better solution for transparent buttons
				m_linkStyle = new GUIStyle();
				m_linkStyle.normal.textColor = new Color( 0.2980392f, 0.4901961f, 1f );
				m_linkStyle.hover.textColor = Color.white;
				m_linkStyle.active.textColor = Color.grey;
				m_linkStyle.margin.top = 3;
				m_linkStyle.margin.bottom = 2;
				m_linkStyle.hover.background = inv;
				m_linkStyle.active.background = inv;
			}

			EditorGUILayout.BeginHorizontal( GUIStyle.none, GUILayout.ExpandWidth( true ) );
			{
				// left column
				EditorGUILayout.BeginVertical( GUILayout.Width( 175 ) );
				{
					GUILayout.Label( SamplesTitle, m_labelStyle );
					EditorGUILayout.BeginHorizontal();
					if( GUILayout.Button( HDRPbutton, m_buttonLeftStyle ) )
						ImportSample( HDRPbutton.text, TemplateSRPType.HDRP );

					EditorGUILayout.EndHorizontal();

					EditorGUILayout.BeginHorizontal();
					if( GUILayout.Button( URPbutton, m_buttonLeftStyle ) )
						ImportSample( URPbutton.text, TemplateSRPType.URP );

					EditorGUILayout.EndHorizontal();
					if( GUILayout.Button( BuiltInbutton, m_buttonStyle ) )
						ImportSample( BuiltInbutton.text, TemplateSRPType.BiRP );

					GUILayout.Space( 10 );

					GUILayout.Label( ResourcesTitle, m_labelStyle );
					if( GUILayout.Button( Manualbutton, m_buttonStyle ) )
						Application.OpenURL( ManualURL );

					if( GUILayout.Button( Basicbutton, m_buttonStyle ) )
						Application.OpenURL( BasicURL );

					if( GUILayout.Button( Beginnerbutton, m_buttonStyle ) )
						Application.OpenURL( BeginnerURL );

					if( GUILayout.Button( Nodesbutton, m_buttonStyle ) )
						Application.OpenURL( NodesURL );

					if( GUILayout.Button( SRPusebutton, m_buttonStyle ) )
						Application.OpenURL( SRPURL );

					if( GUILayout.Button( Functionsbutton, m_buttonStyle ) )
						Application.OpenURL( FunctionsURL );

					if( GUILayout.Button( Templatesbutton, m_buttonStyle ) )
						Application.OpenURL( TemplatesURL );

					if( GUILayout.Button( APIbutton, m_buttonStyle ) )
						Application.OpenURL( APIURL );
				}
				EditorGUILayout.EndVertical();

				// right column
				EditorGUILayout.BeginVertical( GUILayout.Width( 650 - 175 - 9 ), GUILayout.ExpandHeight( true ) );
				{
					GUILayout.Label( CommunityTitle, m_labelStyle );
					EditorGUILayout.BeginHorizontal( GUILayout.ExpandWidth( true ) );
					{
						if( GUILayout.Button( DiscordButton, GUILayout.ExpandWidth( true ) ) )
						{
							Application.OpenURL( DiscordURL );
						}
						if( GUILayout.Button( ForumButton, GUILayout.ExpandWidth( true ) ) )
						{
							Application.OpenURL( ForumURL );
						}
					}
					EditorGUILayout.EndHorizontal();
					GUILayout.Label( UpdateTitle, m_labelStyle );
					m_scrollPosition = GUILayout.BeginScrollView( m_scrollPosition, "ProgressBarBack", GUILayout.ExpandHeight( true ), GUILayout.ExpandWidth( true ) );
					GUILayout.Label( m_changeLog.LastUpdate, "WordWrappedMiniLabel", GUILayout.ExpandHeight( true ) );
					GUILayout.EndScrollView();

					EditorGUILayout.BeginHorizontal( GUILayout.ExpandWidth( true ) );
					{
						EditorGUILayout.BeginVertical();
						GUILayout.Label( ASETitle, m_labelStyle );

						GUILayout.Label( "Installed Version: " + VersionInfo.StaticToString() );

						if( m_changeLog.Version > VersionInfo.FullNumber )
						{
							var cache = GUI.color;
							GUI.color = Color.red;
							GUILayout.Label( "New version available: " + m_newVersion, "BoldLabel" );
							GUI.color = cache;
						}
						else
						{
							var cache = GUI.color;
							GUI.color = Color.green;
							GUILayout.Label( "You are using the latest version", "BoldLabel" );
							GUI.color = cache;
						}

						EditorGUILayout.BeginHorizontal();
						GUILayout.Label( "Download links:" );
						if( GUILayout.Button( "Amplify", m_linkStyle ) )
							Application.OpenURL( SiteURL );
						GUILayout.Label( "-" );
						if( GUILayout.Button( "Asset Store", m_linkStyle ) )
							Application.OpenURL( StoreURL );
						EditorGUILayout.EndHorizontal();
						GUILayout.Space( 7 );
						EditorGUILayout.EndVertical();

						GUILayout.FlexibleSpace();
						EditorGUILayout.BeginVertical();
						GUILayout.Space( 7 );
						GUILayout.Label( ASEIcon );
						EditorGUILayout.EndVertical();
					}
					EditorGUILayout.EndHorizontal();
				}
				EditorGUILayout.EndVertical();
			}
			EditorGUILayout.EndHorizontal();


			EditorGUILayout.BeginHorizontal( "ProjectBrowserBottomBarBg", GUILayout.ExpandWidth( true ), GUILayout.Height(22) );
			{
				GUILayout.FlexibleSpace();
				EditorGUI.BeginChangeCheck();
				var cache = EditorGUIUtility.labelWidth;
				EditorGUIUtility.labelWidth = 100;
				m_startup = (Preferences.ShowOption)EditorGUILayout.EnumPopup( "Show At Startup", m_startup, GUILayout.Width( 220 ) );
				EditorGUIUtility.labelWidth = cache;
				if( EditorGUI.EndChangeCheck() )
				{
					EditorPrefs.SetInt( Preferences.PrefStartUp, (int)m_startup );
				}
			}
			EditorGUILayout.EndHorizontal();

			// Find a better way to update link buttons without repainting the window
			Repaint();
		}

		void ImportSample( string pipeline, TemplateSRPType srpType )
		{
			if( EditorUtility.DisplayDialog( "Import Sample", "This will import the samples for" + pipeline.Replace( " Samples", "" ) + ", please make sure the pipeline is properly installed and/or selected before importing the samples.\n\nContinue?", "Yes", "No" ) )
			{
				AssetDatabase.ImportPackage( AssetDatabase.GUIDToAssetPath( ResourcesGUID ), false );

				switch ( srpType )
				{
					case TemplateSRPType.BiRP:
					{
						AssetDatabase.ImportPackage( AssetDatabase.GUIDToAssetPath( BuiltInGUID ), false );
						break;
					}
					case TemplateSRPType.URP:
					{
						if ( m_srpSamplePackages.TryGetValue( ( int )ASEPackageManagerHelper.CurrentURPBaseline, out ASESRPPackageDesc desc ) )
						{
							string path = AssetDatabase.GUIDToAssetPath( desc.guidURP );
							if ( !string.IsNullOrEmpty( path ) )
							{
								AssetDatabase.ImportPackage( AssetDatabase.GUIDToAssetPath( UniversalGUID ), false );
								AssetDatabase.ImportPackage( path, false );
							}
						}
						break;
					}
					case TemplateSRPType.HDRP:
					{
						if ( m_srpSamplePackages.TryGetValue( ( int )ASEPackageManagerHelper.CurrentHDRPBaseline, out ASESRPPackageDesc desc ) )
						{
							string path = AssetDatabase.GUIDToAssetPath( desc.guidHDRP );
							if ( !string.IsNullOrEmpty( path ) )
							{
								AssetDatabase.ImportPackage( AssetDatabase.GUIDToAssetPath( HighDefinitionGUID ), false );
								AssetDatabase.ImportPackage( path, false );
							}
						}
						break;
					}
					default:
					{
						// no action
						break;
					}

				}
			}
		}

		UnityWebRequest www;

		IEnumerator StartRequest( string url, Action success = null )
		{
			using( www = UnityWebRequest.Get( url ) )
			{
				yield return www.SendWebRequest();

				while( www.isDone == false )
					yield return null;

				if( success != null )
					success();
			}
		}

		public static void StartBackgroundTask( IEnumerator update, Action end = null )
		{
			EditorApplication.CallbackFunction closureCallback = null;

			closureCallback = () =>
			{
				try
				{
					if( update.MoveNext() == false )
					{
						if( end != null )
							end();
						EditorApplication.update -= closureCallback;
					}
				}
				catch( Exception ex )
				{
					if( end != null )
						end();
					Debug.LogException( ex );
					EditorApplication.update -= closureCallback;
				}
			};

			EditorApplication.update += closureCallback;
		}
	}

	[Serializable]
	internal class ChangeLogInfo
	{
		public int Version;
		public string LastUpdate;

		public static ChangeLogInfo CreateFromJSON( string jsonString )
		{
			return JsonUtility.FromJson<ChangeLogInfo>( jsonString );
		}

		public ChangeLogInfo( int version, string lastUpdate )
		{
			Version = version;
			LastUpdate = lastUpdate;
		}
	}
}
