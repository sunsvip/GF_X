using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace AmplifyShaderEditor
{

	public enum ASESaveBundleAssetAction
	{
		Update,
		Export,
		UpdateAndExport
	}

	[CustomEditor( typeof( ASESaveBundleAsset ) )]
	public class ASESaveBundleAssetEditor : Editor
	{
		public ASESaveBundleAsset Instance;
		private DragAndDropTool m_dragAndDropTool;

		private SerializedObject m_so;

		private SerializedProperty m_packageContentsOrigin;
		private GUIContent m_packageContentsOriginLabel = new GUIContent("Main Content");

		private SerializedProperty m_allExtras;

		private SerializedProperty m_packageTargetPath;
		private GUIContent m_packageTargetPathLabel = new GUIContent( "Target Path" );

		private SerializedProperty m_packageTargetName;
		private GUIContent m_packageTargetNameLabel = new GUIContent( "Target Name" );

		private SerializedProperty m_allShaders;

		[SerializeField]
		private ReorderableList m_listShaders = null;

		[SerializeField]
		private ReorderableList m_listExtras = null;

		public void OnEnable()
		{
			Instance = (ASESaveBundleAsset)target;

			m_so = serializedObject;
			m_packageContentsOrigin = m_so.FindProperty( "m_packageContentsOrigin" );
			m_packageTargetPath = m_so.FindProperty( "m_packageTargetPath" );
			m_packageTargetName = m_so.FindProperty( "m_packageTargetName" );
			m_allShaders = m_so.FindProperty( "m_allShaders" );

			if( m_listShaders == null )
			{
				m_listShaders = new ReorderableList( m_so , m_allShaders , true , true , true , true );
				m_listShaders.elementHeight = 16;

				m_listShaders.drawElementCallback = ( Rect rect , int index , bool isActive , bool isFocused ) =>
				{
					m_allShaders.GetArrayElementAtIndex( index ).objectReferenceValue = (Shader)EditorGUI.ObjectField( rect , "Shader " + index , m_allShaders.GetArrayElementAtIndex( index ).objectReferenceValue , typeof( Shader ) , false );
				};

				m_listShaders.drawHeaderCallback = ( Rect rect ) =>
				{
					EditorGUI.LabelField( rect , "Shader List" );
				};

				m_listShaders.onAddCallback = ( list ) =>
				{
					m_allShaders.InsertArrayElementAtIndex( m_allShaders.arraySize );
					m_allShaders.GetArrayElementAtIndex( m_allShaders.arraySize - 1 ).objectReferenceValue = null;
				};

				m_listShaders.onRemoveCallback = ( list ) =>
				{
					m_allShaders.GetArrayElementAtIndex( list.index ).objectReferenceValue = null;
					m_allShaders.DeleteArrayElementAtIndex( list.index );
				};
			}

			m_allExtras = m_so.FindProperty( "m_allExtras" );
			if( m_listExtras == null )
			{
				m_listExtras = new ReorderableList( m_so , m_allExtras , true , true , true , true );
				m_listExtras.elementHeight = 18;

				m_listExtras.drawElementCallback = ( Rect rect , int index , bool isActive , bool isFocused ) =>
				{
					rect.width -= 55;
					m_allExtras.GetArrayElementAtIndex( index ).stringValue = (string)EditorGUI.TextField( rect , "Path " + index , m_allExtras.GetArrayElementAtIndex( index ).stringValue );

					rect.x += rect.width;
					rect.width = 55;
					if( GUI.Button( rect, "Browse" ) )
						m_allExtras.GetArrayElementAtIndex( index ).stringValue = ASESaveBundleTool.FetchPath( "Folder Path" , m_allExtras.GetArrayElementAtIndex( index ).stringValue );
				};

				m_listExtras.drawHeaderCallback = ( Rect rect ) =>
				{
					EditorGUI.LabelField( rect , "Extra Paths" );
				};

				m_listExtras.onAddCallback = ( list ) =>
				{
					m_allExtras.InsertArrayElementAtIndex( m_allExtras.arraySize );
					m_allExtras.GetArrayElementAtIndex( m_allExtras.arraySize - 1 ).stringValue = string.Empty;
				};

				m_listExtras.onRemoveCallback = ( list ) =>
				{
					m_allExtras.GetArrayElementAtIndex( list.index ).stringValue = string.Empty;
					m_allExtras.DeleteArrayElementAtIndex( list.index );
				};
			}

			m_dragAndDropTool = new DragAndDropTool();
			m_dragAndDropTool.OnValidDropObjectEvt += OnValidObjectsDropped;
		}

		void FetchValidShadersFromPath( string path , bool updateProperty )
		{
			if( !path.StartsWith( "Assets" ) )
			{
				int idx = path.IndexOf( "Assets" );
				if( idx >= 0 )
				{
					path = path.Substring( idx );
				}
			}

			if( AssetDatabase.IsValidFolder( path ) )
			{
				if( updateProperty )
					m_packageContentsOrigin.stringValue = path;

				string[] pathArr = { path };
				string[] shaderInDir = AssetDatabase.FindAssets( "t:Shader" , pathArr );
				for( int shaderIdx = 0 ; shaderIdx < shaderInDir.Length ; shaderIdx++ )
				{
					Shader internalShader = AssetDatabase.LoadAssetAtPath<Shader>( AssetDatabase.GUIDToAssetPath( shaderInDir[ shaderIdx ] ) );
					if( internalShader != null && IOUtils.IsASEShader( internalShader ) )
					{
						m_allShaders.InsertArrayElementAtIndex( m_allShaders.arraySize );
						m_allShaders.GetArrayElementAtIndex( m_allShaders.arraySize - 1 ).objectReferenceValue = internalShader;
						m_so.ApplyModifiedProperties();
					}
				}
			}
		}

		public void OnValidObjectsDropped( UnityEngine.Object[] droppedObjs )
		{
			for( int objIdx = 0 ; objIdx < droppedObjs.Length ; objIdx++ )
			{
				Shader shader = droppedObjs[ objIdx ] as Shader;
				if( shader != null )
				{
					if( IOUtils.IsASEShader( shader ) )
					{
						m_allShaders.InsertArrayElementAtIndex( m_allShaders.arraySize );
						m_allShaders.GetArrayElementAtIndex( m_allShaders.arraySize - 1 ).objectReferenceValue = shader;
						m_so.ApplyModifiedProperties();
					}
				}
				else
				{
					DefaultAsset asset = droppedObjs[ objIdx ] as DefaultAsset;
					if( asset != null )
					{
						string path = AssetDatabase.GetAssetPath( asset );
						FetchValidShadersFromPath( path,true );
					}
				}
			}

			Instance.AllShaders.Sort( ( x , y ) => string.Compare( x.name , y.name ) );
			m_so.Update();
		}

		private void OnDestroy()
		{
			m_dragAndDropTool.Destroy();
			m_dragAndDropTool = null;
		}


		public override void OnInspectorGUI()
		{
			m_so.Update();
			EditorGUILayout.Separator();

			EditorGUILayout.BeginHorizontal();
			{
				EditorGUILayout.PropertyField( m_packageContentsOrigin, m_packageContentsOriginLabel );
				if( GUILayout.Button( "Browse", GUILayout.MaxWidth( 55 ) ) )
				{
					m_packageContentsOrigin.stringValue = ASESaveBundleTool.FetchPath( "Folder Path" , m_packageContentsOrigin.stringValue );
				}
				if( GUILayout.Button( "Fetch" , GUILayout.MaxWidth( 45 ) ) )
				{
					FetchValidShadersFromPath( m_packageContentsOrigin.stringValue, false );
				}
			}
			EditorGUILayout.EndHorizontal();

			if( m_listExtras != null )
				m_listExtras.DoLayoutList();

			EditorGUILayout.Separator();

			EditorGUILayout.BeginHorizontal();
			{
				EditorGUILayout.PropertyField( m_packageTargetPath , m_packageTargetPathLabel );
				if( GUILayout.Button( "Browse",GUILayout.MaxWidth(55) ))
					m_packageTargetPath.stringValue = EditorUtility.OpenFolderPanel( "Folder Path" , m_packageTargetPath.stringValue , "" );
			}
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.PropertyField( m_packageTargetName, m_packageTargetNameLabel );

			EditorGUILayout.Separator();
			if( GUILayout.Button( "Clear" ) )
			{
				m_allShaders.ClearArray();
			}

			if( m_listShaders != null )
				m_listShaders.DoLayoutList();

			EditorGUILayout.Separator();

			m_dragAndDropTool.TestDragAndDrop( new Rect( 0 , 0 , Screen.width , Screen.height ) );

			m_so.ApplyModifiedProperties();
		}
	}

	public class ASESaveBundleTool : EditorWindow
	{
		private const string UpdateAllStr = "Update All";
		private const string UpdateAllStyle = "prebutton";


		[SerializeField]
		private ASESaveBundleAsset m_asset;

		[SerializeField]
		private ASESaveBundleAsset m_dummyAsset;

		private GUIStyle m_contentStyle = null;

		private Vector2 m_scrollPos;
		private GUIContent m_ViewToolIcon;

		ASESaveBundleAssetEditor m_editor;

		private const string Title = "Batch Save and Pack";

		[NonSerialized]
		private GUIStyle m_titleStyle;

		[MenuItem( "Window/Amplify Shader Editor/"+ Title , false , 1001 )]
		static void ShowWindow()
		{
			ASESaveBundleTool window = EditorWindow.GetWindow<ASESaveBundleTool>();
			window.titleContent.text = "Batch Save...";
			window.titleContent.tooltip = Title;
			window.minSize = new Vector2( 302 , 350 );
			window.Show();
		}

		private void OnEnable()
		{
			if( m_contentStyle == null )
			{
				m_contentStyle = new GUIStyle( GUIStyle.none );
				m_contentStyle.margin = new RectOffset( 6 , 4 , 5 , 5 );
			}

			if( m_ViewToolIcon == null )
			{
				m_ViewToolIcon = EditorGUIUtility.IconContent( "icons/d_ViewToolZoom.png" );
			}
		}

		private void OnDestroy()
		{
			DestroyImmediate( m_editor );
			if( m_dummyAsset != null && m_dummyAsset != m_asset )
				DestroyImmediate( m_dummyAsset );
		}

		
		public static string FetchPath( string title, string folderpath )
		{
			folderpath = EditorUtility.OpenFolderPanel( title , folderpath , "" );
			folderpath = FileUtil.GetProjectRelativePath( folderpath );
			if( string.IsNullOrEmpty( folderpath ) )
				folderpath = "Assets";

			return folderpath;
		}

		private bool m_updatingShaders = false;

		private void ExportCurrent( ASESaveBundleAsset currentAsset )
		{
			List<string> pathsList = new List<string>();
			pathsList.Add( currentAsset.PackageContentsOrigin );
			for( int i = 0 ; i < currentAsset.AllExtras.Count ; i++ )
			{
				if( currentAsset.AllExtras[ i ].StartsWith( "Assets" ) )
				{
					pathsList.Add( currentAsset.AllExtras[ i ] );
				}
				else
				{
					int idx = currentAsset.AllExtras[ i ].IndexOf( "Assets" );
					if( idx >= 0 )
					{
						pathsList.Add( currentAsset.AllExtras[ i ].Substring( idx ) );
					}
				}
				
			}
			AssetDatabase.ExportPackage( pathsList.ToArray() , currentAsset.PackageTargetPath + "/" + currentAsset.PackageTargetName + ".unitypackage" , ExportPackageOptions.Recurse | ExportPackageOptions.Interactive );
		}

		private void OnGUI()
		{
			if( m_updatingShaders )
			{
				m_updatingShaders = EditorPrefs.HasKey( AmplifyShaderEditorWindow.ASEFileList );
			}


			if( m_titleStyle == null )
			{
				m_titleStyle = new GUIStyle( "BoldLabel" );
				m_titleStyle.fontSize = 13;
				m_titleStyle.alignment = TextAnchor.MiddleCenter;
			}


			EditorGUILayout.LabelField( Title , m_titleStyle );
			EditorGUI.BeginDisabledGroup( m_updatingShaders );
			{
				ASESaveBundleAsset currentAsset = null;
				if( m_asset != null )
				{
					currentAsset = m_asset;
				}
				else
				{
					if( m_dummyAsset == null )
					{
						m_dummyAsset = ScriptableObject.CreateInstance<ASESaveBundleAsset>();
						m_dummyAsset.name = "Dummy";
					}
					currentAsset = m_dummyAsset;
				}

				m_scrollPos = EditorGUILayout.BeginScrollView( m_scrollPos , GUILayout.Height( position.height ) );
				{
					float cachedWidth = EditorGUIUtility.labelWidth;
					EditorGUIUtility.labelWidth = 100;
					EditorGUILayout.BeginVertical( m_contentStyle );
					{
						EditorGUI.BeginDisabledGroup( currentAsset.AllShaders.Count <= 0 );
						{
							// Update all shaders
							if( GUILayout.Button( UpdateAllStr/* , UpdateAllStyle , GUILayout.Height( 20 )*/ ) )
							{
								m_updatingShaders = true;
								string[] assetPaths = new string[ currentAsset.AllShaders.Count ];
								for( int i = 0 ; i < assetPaths.Length ; i++ )
								{
									assetPaths[ i ] = AssetDatabase.GetAssetPath( currentAsset.AllShaders[ i ] );
								}
								AmplifyShaderEditorWindow.LoadAndSaveList( assetPaths );
							}

							if( GUILayout.Button( "Remove Custom Inspector" ) )
							{
								int count = currentAsset.AllShaders.Count;
								for( int i = 0 ; i < count ; i++ )
								{
									EditorUtility.DisplayProgressBar( "Removing custom inspector", currentAsset.AllShaders[i].name , i / ( count - 1 ) );
									string path = AssetDatabase.GetAssetPath( currentAsset.AllShaders[ i ] );
									string shaderBody = IOUtils.LoadTextFileFromDisk( path );
									shaderBody = Regex.Replace( shaderBody , TemplateHelperFunctions.CustomInspectorPattern , string.Empty ,RegexOptions.Multiline );
									shaderBody = UIUtils.ForceLFLineEnding( shaderBody );
									IOUtils.SaveTextfileToDisk( shaderBody , path , false );
								}
								AssetDatabase.Refresh();
								EditorUtility.ClearProgressBar();
							}
						}
						EditorGUI.EndDisabledGroup();


						EditorGUI.BeginDisabledGroup( string.IsNullOrEmpty( currentAsset.PackageTargetName ) || string.IsNullOrEmpty( currentAsset.PackageTargetPath ) );
						{
							if( GUILayout.Button( "Export Unity Package" ) )
							{
								ExportCurrent( currentAsset );
							}
						}
						EditorGUI.EndDisabledGroup();
						EditorGUILayout.Separator();
						// Asset creation/load
						EditorGUILayout.BeginHorizontal();
						m_asset = EditorGUILayout.ObjectField( "Asset Preset" , m_asset , typeof( ASESaveBundleAsset ) , false ) as ASESaveBundleAsset;
						if( GUILayout.Button( m_asset != null ? "Save" : "Create" , "minibutton" , GUILayout.Width( 50 ) ) )
						{
							string defaultName = "ShaderBundlePreset";
							string assetPath = string.Empty;
							if( m_asset != null )
							{
								defaultName = m_asset.name;
								assetPath = Application.dataPath.Substring(0, Application.dataPath.Length - 6 )+  AssetDatabase.GetAssetPath( m_asset );
							}
							string path = EditorUtility.SaveFilePanelInProject( "Save as" , defaultName , "asset" , string.Empty , assetPath );
							if( !string.IsNullOrEmpty( path ) )
							{
								ASESaveBundleAsset outfile = AssetDatabase.LoadMainAssetAtPath( path ) as ASESaveBundleAsset;
								if( outfile != null )
								{
									EditorUtility.CopySerialized( currentAsset , outfile );
									AssetDatabase.SaveAssets();
									Selection.activeObject = outfile;
									EditorGUIUtility.PingObject( outfile );
								}
								else
								{
									if( m_asset != null )
									{
										currentAsset = ScriptableObject.CreateInstance<ASESaveBundleAsset>();
										EditorUtility.CopySerialized( m_asset , currentAsset );
									}
									AssetDatabase.CreateAsset( currentAsset , path );
									Selection.activeObject = currentAsset;
									EditorGUIUtility.PingObject( currentAsset );
									m_asset = currentAsset;
								}
							}
						}
						EditorGUILayout.EndHorizontal();
						if( Event.current.type == EventType.Layout )
						{
							if( m_editor == null )
							{
								m_editor = Editor.CreateEditor( currentAsset , typeof( ASESaveBundleAssetEditor ) ) as ASESaveBundleAssetEditor;
							}
							else
							{
								if( m_editor.Instance != currentAsset )
								{
									DestroyImmediate( m_editor );
									m_editor = Editor.CreateEditor( currentAsset , typeof( ASESaveBundleAssetEditor ) ) as ASESaveBundleAssetEditor;
								}
							}
						}
						if( m_editor != null )
							m_editor.OnInspectorGUI();

					}
					EditorGUILayout.EndVertical();
				}
				EditorGUILayout.EndScrollView();
			}
			EditorGUI.EndDisabledGroup();
		}
	}
}
