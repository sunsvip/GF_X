// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>
//#define NEW_TEXTURE_3D_METHOD

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using UnityEditorInternal;
using System;
using System.IO;

namespace AmplifyShaderEditor
{
	[CustomEditor( typeof( TextureArrayCreatorAsset ) )]
	public class TextureArrayCreatorAssetEditor : Editor
	{
		private string[] m_sizesStr = { "32", "64", "128", "256", "512", "1024", "2048", "4096", "8192" };
		private int[] m_sizes = { 32, 64, 128, 256, 512, 1024, 2048, 4096, 8192 };

		private const string ArrayFilename = "NewTextureArray";
		private const string Texture3DFilename = "NewTexture3D";

		private GUIContent m_pathButtonContent = new GUIContent();
		private GUIStyle m_pathButtonStyle = null;

		public TextureArrayCreatorAsset Instance;

		private DragAndDropTool m_dragAndDropTool;

		SerializedObject m_so;
		SerializedProperty m_selectedSize;
		SerializedProperty m_lockRatio;
		SerializedProperty m_sizeX;
		SerializedProperty m_sizeY;
		SerializedProperty m_tex3DMode;
		SerializedProperty m_linearMode;
		SerializedProperty m_mipMaps;
		SerializedProperty m_wrapMode;
		SerializedProperty m_filterMode;
		SerializedProperty m_anisoLevel;
		SerializedProperty m_selectedFormatEnum;
		SerializedProperty m_quality;
		SerializedProperty m_folderPath;
		SerializedProperty m_fileName;
		SerializedProperty m_filenameChanged;
		SerializedProperty m_allTextures;

		[SerializeField]
		private ReorderableList m_listTextures = null;

		[SerializeField]
		private int m_previewSize;

		public void OnEnable()
		{
			Instance = (TextureArrayCreatorAsset)target;

			m_so = serializedObject;
			m_selectedSize = m_so.FindProperty( "m_selectedSize" );
			m_lockRatio = m_so.FindProperty( "m_lockRatio" );
			m_sizeX = m_so.FindProperty( "m_sizeX" );
			m_sizeY = m_so.FindProperty( "m_sizeY" );
			m_tex3DMode = m_so.FindProperty( "m_tex3DMode" );
			m_linearMode = m_so.FindProperty( "m_linearMode" );
			m_mipMaps = m_so.FindProperty( "m_mipMaps" );
			m_wrapMode = m_so.FindProperty( "m_wrapMode" );
			m_filterMode = m_so.FindProperty( "m_filterMode" );
			m_anisoLevel = m_so.FindProperty( "m_anisoLevel" );
			m_selectedFormatEnum = m_so.FindProperty( "m_selectedFormatEnum" );
			m_quality = m_so.FindProperty( "m_quality" );
			m_folderPath = m_so.FindProperty( "m_folderPath" );
			m_fileName = m_so.FindProperty( "m_fileName" );
			m_filenameChanged = m_so.FindProperty( "m_filenameChanged" );
			m_allTextures = m_so.FindProperty( "m_allTextures" );

			if( m_listTextures == null )
			{
				m_listTextures = new ReorderableList( m_so, m_allTextures, true, true, true, true );
				m_listTextures.elementHeight = 16;

				m_listTextures.drawElementCallback = ( Rect rect, int index, bool isActive, bool isFocused ) =>
				{
					m_allTextures.GetArrayElementAtIndex( index ).objectReferenceValue = (Texture2D)EditorGUI.ObjectField( rect, "Texture " + index, m_allTextures.GetArrayElementAtIndex( index ).objectReferenceValue, typeof( Texture2D ), false );
				};

				m_listTextures.drawHeaderCallback = ( Rect rect ) =>
				{
					m_previewSize = EditorGUI.IntSlider( rect, "Texture List", m_previewSize, 16, 64 );
					if( (float)m_previewSize != m_listTextures.elementHeight )
						m_listTextures.elementHeight = m_previewSize;
				};

				m_listTextures.onAddCallback = ( list ) =>
				{
					m_allTextures.InsertArrayElementAtIndex( m_allTextures.arraySize );
					m_allTextures.GetArrayElementAtIndex( m_allTextures.arraySize - 1 ).objectReferenceValue = null;
				};

				m_listTextures.onRemoveCallback = ( list ) =>
				{
					m_allTextures.GetArrayElementAtIndex( list.index ).objectReferenceValue = null;
					m_allTextures.DeleteArrayElementAtIndex( list.index );
				};
			}

			m_dragAndDropTool = new DragAndDropTool();
			m_dragAndDropTool.OnValidDropObjectEvt += OnValidObjectsDropped;
		}

		public void OnValidObjectsDropped( UnityEngine.Object[] droppedObjs )
		{
			for( int objIdx = 0; objIdx < droppedObjs.Length; objIdx++ )
			{
				Texture2D tex = droppedObjs[ objIdx ] as Texture2D;
				if( tex != null )
				{
					m_allTextures.InsertArrayElementAtIndex( m_allTextures.arraySize );
					m_allTextures.GetArrayElementAtIndex( m_allTextures.arraySize - 1 ).objectReferenceValue = tex;
					m_so.ApplyModifiedProperties();
				}
				else
				{
					DefaultAsset asset = droppedObjs[ objIdx ] as DefaultAsset;
					if( asset != null )
					{
						string path = AssetDatabase.GetAssetPath( asset );
						if( AssetDatabase.IsValidFolder( path ) )
						{
							string[] pathArr = { path };
							string[] texInDir = AssetDatabase.FindAssets( "t:Texture2D", pathArr );
							for( int texIdx = 0; texIdx < texInDir.Length; texIdx++ )
							{
								Texture2D internalTex = AssetDatabase.LoadAssetAtPath<Texture2D>( AssetDatabase.GUIDToAssetPath( texInDir[ texIdx ] ) );
								if( internalTex != null )
								{
									m_allTextures.InsertArrayElementAtIndex( m_allTextures.arraySize );
									m_allTextures.GetArrayElementAtIndex( m_allTextures.arraySize - 1 ).objectReferenceValue = internalTex;
									m_so.ApplyModifiedProperties();
								}
							}
						}
					}
				}
			}

			Instance.AllTextures.Sort( ( x, y ) => string.Compare( x.name, y.name ) );
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

			if( m_pathButtonStyle == null )
				m_pathButtonStyle = "minibutton";

			EditorGUILayout.BeginHorizontal();
			var cache = EditorGUIUtility.labelWidth;
			EditorGUILayout.PrefixLabel( "Size" );
			EditorGUIUtility.labelWidth = 16;
			if( m_lockRatio.boolValue )
			{
				m_selectedSize.intValue = EditorGUILayout.Popup( "X", m_selectedSize.intValue, m_sizesStr );
				EditorGUI.BeginDisabledGroup( m_lockRatio.boolValue );
				EditorGUILayout.Popup( "Y", m_selectedSize.intValue, m_sizesStr );
				EditorGUI.EndDisabledGroup();
			}
			else
			{
				EditorGUILayout.PropertyField( m_sizeX, new GUIContent( "X" ) );
				EditorGUILayout.PropertyField( m_sizeY, new GUIContent( "Y" ) );
			}
			EditorGUIUtility.labelWidth = 100;
			m_lockRatio.boolValue = GUILayout.Toggle( m_lockRatio.boolValue, "L", "minibutton", GUILayout.Width( 18 ) );
			if( m_lockRatio.boolValue )
			{
				m_sizeX.intValue = m_sizes[ m_selectedSize.intValue ];
				m_sizeY.intValue = m_sizes[ m_selectedSize.intValue ];
			}
			EditorGUIUtility.labelWidth = cache;
			EditorGUILayout.EndHorizontal();
			EditorGUI.BeginChangeCheck();
			EditorGUILayout.PropertyField( m_tex3DMode, new GUIContent( "Texture 3D" ) );
			if( EditorGUI.EndChangeCheck() )
			{
				if( !m_filenameChanged.boolValue )
				{
					m_fileName.stringValue = m_tex3DMode.boolValue ? Texture3DFilename : ArrayFilename;
				}
			}
			EditorGUILayout.PropertyField( m_linearMode, new GUIContent( "Linear" ) );
			EditorGUILayout.PropertyField( m_mipMaps );
			EditorGUILayout.PropertyField( m_wrapMode );
			EditorGUILayout.PropertyField( m_filterMode );
			m_anisoLevel.intValue = EditorGUILayout.IntSlider( "Aniso Level", m_anisoLevel.intValue, 0, 16 );
			EditorGUILayout.PropertyField( m_selectedFormatEnum, new GUIContent( "Format" ) );

			if( m_selectedFormatEnum.intValue == (int)TextureFormat.DXT1Crunched )
			{
				m_selectedFormatEnum.intValue = (int)TextureFormat.DXT1;
				Debug.Log( "Texture Array does not support crunched DXT1 format. Changing to DXT1..." );
			}
			else if( m_selectedFormatEnum.intValue == (int)TextureFormat.DXT5Crunched )
			{
				m_selectedFormatEnum.intValue = (int)TextureFormat.DXT5;
				Debug.Log( "Texture Array does not support crunched DXT5 format. Changing to DXT5..." );
			}

			m_quality.intValue = EditorGUILayout.IntSlider( "Format Quality", m_quality.intValue, 0, 100 );
			EditorGUILayout.Separator();

			EditorGUILayout.LabelField( "Path and Name" );
			EditorGUILayout.BeginHorizontal();
			m_pathButtonContent.text = m_folderPath.stringValue;
			Vector2 buttonSize = m_pathButtonStyle.CalcSize( m_pathButtonContent );
			if( GUILayout.Button( m_pathButtonContent, m_pathButtonStyle, GUILayout.MaxWidth( Mathf.Min( Screen.width * 0.5f, buttonSize.x ) ) ) )
			{
				string folderpath = EditorUtility.OpenFolderPanel( "Save Texture Array to folder", "Assets/", "" );
				folderpath = FileUtil.GetProjectRelativePath( folderpath );
				if( string.IsNullOrEmpty( folderpath ) )
					m_folderPath.stringValue = "Assets/";
				else
					m_folderPath.stringValue = folderpath + "/";
			}
			EditorGUI.BeginChangeCheck();
			m_fileName.stringValue = EditorGUILayout.TextField( m_fileName.stringValue, GUILayout.ExpandWidth( true ) );
			if( EditorGUI.EndChangeCheck() )
			{
				m_filenameChanged.boolValue = m_fileName.stringValue == ArrayFilename ? false : true;
			}
			EditorGUILayout.LabelField( ".asset", GUILayout.MaxWidth( 40 ) );
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.Separator();

			if( GUILayout.Button( "Clear" ) )
			{
				m_allTextures.ClearArray();
			}

			if( m_listTextures != null )
				m_listTextures.DoLayoutList();

			EditorGUILayout.Separator();

			m_dragAndDropTool.TestDragAndDrop( new Rect( 0, 0, Screen.width, Screen.height ) );

			m_so.ApplyModifiedProperties();
		}
	}

	public class ASETextureArrayCreator : EditorWindow
	{
		[MenuItem( "Window/Amplify Shader Editor/Texture Array Creator", false, 1001 )]
		static void ShowWindow()
		{
			ASETextureArrayCreator window = EditorWindow.GetWindow<ASETextureArrayCreator>();
			window.titleContent.text = "Texture Array";
			window.minSize = new Vector2( 302, 350 );
			window.Show();
		}

		private const string ClearButtonStr = "Clear";
		private const string TextureFilter = "t:Texture2D";
		private const string BuildArrayMessage = "Build Array";
		private const string BuildTexture3DMessage = "Build Texture 3D";
		private const string ArrayFilename = "NewTextureArray";
		private const string Texture3DFilename = "NewTexture3D";

		TextureArrayCreatorAssetEditor m_editor;

		[SerializeField]
		private TextureArrayCreatorAsset m_asset;

		[SerializeField]
		private TextureArrayCreatorAsset m_dummyAsset;

		private static List<TextureFormat> UncompressedFormats = new List<TextureFormat>() 
		{
			TextureFormat.RGBAFloat,
			TextureFormat.RGBAHalf,
			TextureFormat.ARGB32, 
			TextureFormat.RGBA32, 
			TextureFormat.RGB24, 
			TextureFormat.Alpha8 
		};

		private GUIStyle m_contentStyle = null;

		private Vector2 m_scrollPos;
		private Texture m_lastSaved;
		private string m_message = string.Empty;

		private void OnEnable()
		{
			if( m_contentStyle == null )
			{
				m_contentStyle = new GUIStyle( GUIStyle.none );
				m_contentStyle.margin = new RectOffset( 6, 4, 5, 5 );
			}
		}

		private void OnDestroy()
		{
			DestroyImmediate( m_editor );
			if( m_dummyAsset != null && m_dummyAsset != m_asset )
				DestroyImmediate( m_dummyAsset );
		}

		void OnGUI()
		{
			TextureArrayCreatorAsset currentAsset = null;
			if( m_asset != null )
			{
				currentAsset = m_asset;
			}
			else
			{
				if( m_dummyAsset == null )
				{
					m_dummyAsset = ScriptableObject.CreateInstance<TextureArrayCreatorAsset>();
					m_dummyAsset.name = "Dummy";
				}
				currentAsset = m_dummyAsset;
			}

			m_scrollPos = EditorGUILayout.BeginScrollView( m_scrollPos, GUILayout.Height( position.height ) );
			float cachedWidth = EditorGUIUtility.labelWidth;
			EditorGUIUtility.labelWidth = 100;
			EditorGUILayout.BeginVertical( m_contentStyle );

			string buildButtonStr = currentAsset.Tex3DMode ? BuildTexture3DMessage : BuildArrayMessage;
			// build button
			EditorGUILayout.BeginHorizontal();
			EditorGUI.BeginDisabledGroup( currentAsset.AllTextures.Count <= 0 );
			if( GUILayout.Button( buildButtonStr, "prebutton", GUILayout.Height( 20 ) ) )
			{
				bool showWarning = false;
				for( int i = 0; i < currentAsset.AllTextures.Count; i++ )
				{
					if( currentAsset.AllTextures[ i ].width != currentAsset.SizeX || currentAsset.AllTextures[ i ].height != currentAsset.SizeY )
					{
						showWarning = true;
					}
				}

				if( !showWarning )
				{
					m_message = string.Empty;
					if( currentAsset.Tex3DMode )
						BuildTexture3D( currentAsset );
					else
						BuildArray( currentAsset );
				}
				else if( EditorUtility.DisplayDialog( "Warning!", "Some textures need to be resized to fit the selected size. Do you want to continue?", "Yes", "No" ) )
				{
					m_message = string.Empty;
					if( currentAsset.Tex3DMode )
						BuildTexture3D( currentAsset );
					else
						BuildArray( currentAsset );
				}
			}
			EditorGUI.EndDisabledGroup();
			EditorGUI.BeginDisabledGroup( m_lastSaved == null );
			GUIContent icon = EditorGUIUtility.IconContent( "icons/d_ViewToolZoom.png" );
			if( GUILayout.Button( icon, "prebutton", GUILayout.Width( 28 ), GUILayout.Height( 20 ) ) )
			{
				EditorGUIUtility.PingObject( m_lastSaved );
			}
			EditorGUI.EndDisabledGroup();
			EditorGUILayout.EndHorizontal();

			// message
			if( !string.IsNullOrEmpty( m_message ) )
				if( GUILayout.Button( "BUILD REPORT (click to hide):\n\n" + m_message, "helpbox" ) )
					m_message = string.Empty;

			// asset
			EditorGUILayout.BeginHorizontal();
			m_asset = EditorGUILayout.ObjectField( "Asset Preset", m_asset, typeof( TextureArrayCreatorAsset ), false ) as TextureArrayCreatorAsset;
			if( GUILayout.Button( m_asset != null ? "Save" : "Create", "minibutton", GUILayout.Width( 50 ) ) )
			{
				string defaultName = "ArrayPreset";
				if( m_asset != null )
					defaultName = m_asset.name;

				string path = EditorUtility.SaveFilePanelInProject( "Save as", defaultName, "asset", string.Empty );
				if( !string.IsNullOrEmpty( path ) )
				{
					TextureArrayCreatorAsset outfile = AssetDatabase.LoadMainAssetAtPath( path ) as TextureArrayCreatorAsset;
					if( outfile != null )
					{
						EditorUtility.CopySerialized( currentAsset, outfile );
						AssetDatabase.SaveAssets();
						Selection.activeObject = outfile;
						EditorGUIUtility.PingObject( outfile );
					}
					else
					{
						if( m_asset != null )
						{
							currentAsset = ScriptableObject.CreateInstance<TextureArrayCreatorAsset>();
							EditorUtility.CopySerialized( m_asset, currentAsset );
						}
						AssetDatabase.CreateAsset( currentAsset, path );
						Selection.activeObject = currentAsset;
						EditorGUIUtility.PingObject( currentAsset );
						m_asset = currentAsset;
					}
				}
			}
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.Separator();

			if( Event.current.type == EventType.Layout )
			{
				if( m_editor == null )
				{
					m_editor = Editor.CreateEditor( currentAsset, typeof( TextureArrayCreatorAssetEditor ) ) as TextureArrayCreatorAssetEditor;
				}
				else
				{
					if( m_editor.Instance != currentAsset )
					{
						DestroyImmediate( m_editor );
						m_editor = Editor.CreateEditor( currentAsset, typeof( TextureArrayCreatorAssetEditor ) ) as TextureArrayCreatorAssetEditor;
					}
				}
			}
			if( m_editor != null )
				m_editor.OnInspectorGUI();

			GUILayout.Space( 20 );
			EditorGUILayout.EndVertical();
			EditorGUIUtility.labelWidth = cachedWidth;
			EditorGUILayout.EndScrollView();
		}

		private void CopyToArray( ref Texture2D from, ref Texture2DArray to, int arrayIndex, int mipLevel, bool compressed = true )
		{
			if( compressed )
			{
				Graphics.CopyTexture( from, 0, mipLevel, to, arrayIndex, mipLevel );
			}
			else
			{
				to.SetPixels( from.GetPixels(), arrayIndex, mipLevel );
				to.Apply();
			}
		}

#if NEW_TEXTURE_3D_METHOD
		private void BuildTexture3D( TextureArrayCreatorAsset asset )
		{
			int sizeX = asset.SizeX;
			int sizeY = asset.SizeY;

			Texture3D texture3D = new Texture3D( sizeX, sizeY, asset.AllTextures.Count, asset.SelectedFormatEnum, asset.MipMaps );
			texture3D.wrapMode = asset.WrapMode;
			texture3D.filterMode = asset.FilterMode;
			texture3D.anisoLevel = asset.AnisoLevel;
			//texture3D.Apply( false );
			RenderTexture cache = RenderTexture.active;
			RenderTexture rt = new RenderTexture( sizeX, sizeY, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default );
			rt.Create();
			List<Texture2D> textures = new List<Texture2D>( asset.AllTextures.Count );

			for( int i = 0; i < asset.AllTextures.Count; i++ )
			{
				// build report
				int widthChanges = asset.AllTextures[ i ].width < sizeX ? -1 : asset.AllTextures[ i ].width > sizeX ? 1 : 0;
				int heightChanges = asset.AllTextures[ i ].height < sizeY ? -1 : asset.AllTextures[ i ].height > sizeY ? 1 : 0;
				if( ( widthChanges < 0 && heightChanges <= 0 ) || ( widthChanges <= 0 && heightChanges < 0 ) )
					m_message += asset.AllTextures[ i ].name + " was upscaled\n";
				else if( ( widthChanges > 0 && heightChanges >= 0 ) || ( widthChanges >= 0 && heightChanges > 0 ) )
					m_message += asset.AllTextures[ i ].name + " was downscaled\n";
				else if( ( widthChanges > 0 && heightChanges < 0 ) || ( widthChanges < 0 && heightChanges > 0 ) )
					m_message += asset.AllTextures[ i ].name + " changed dimensions\n";

				// blit image to upscale or downscale the image to any size
				RenderTexture.active = rt;

				bool cachedsrgb = GL.sRGBWrite;
				GL.sRGBWrite = !asset.LinearMode;
				Graphics.Blit( asset.AllTextures[ i ], rt );
				GL.sRGBWrite = cachedsrgb;

				textures.Add( new Texture2D( sizeX, sizeY, TextureFormat.ARGB32, asset.MipMaps, asset.LinearMode ) );
				textures[ i ].ReadPixels( new Rect( 0, 0, sizeX, sizeY ), 0, 0, asset.MipMaps );
				RenderTexture.active = null;

				bool isCompressed = UncompressedFormats.FindIndex( x => x.Equals( asset.SelectedFormatEnum ) ) < 0;
				if( isCompressed )
				{
					EditorUtility.CompressTexture( textures[ i ], asset.SelectedFormatEnum, asset.Quality );
					//	t2d.Apply( false );
				}
				textures[ i ].Apply( false );
			}

			rt.Release();
			RenderTexture.active = cache;

			if( m_message.Length > 0 )
				m_message = m_message.Substring( 0, m_message.Length - 1 );

			int sizeZ = textures.Count;
			Color[] colors = new Color[ sizeX * sizeY * sizeZ ];
			int idx = 0;
			for( int z = 0; z < sizeZ; z++ )
			{
				for( int y = 0; y < sizeY; y++ )
				{
					for( int x = 0; x < sizeX; x++, idx++ )
					{
						colors[ idx ] = textures[ z ].GetPixel(x,y);
					}
				}
			}

			texture3D.SetPixels( colors );
			texture3D.Apply();

			string path = asset.FolderPath + asset.FileName + ".asset";
			Texture3D outfile = AssetDatabase.LoadMainAssetAtPath( path ) as Texture3D;
			if( outfile != null )
			{
				EditorUtility.CopySerialized( texture3D, outfile );
				AssetDatabase.SaveAssets();
				EditorGUIUtility.PingObject( outfile );
				m_lastSaved = outfile;
			}
			else
			{
				AssetDatabase.CreateAsset( texture3D, path );
				EditorGUIUtility.PingObject( texture3D );
				m_lastSaved = texture3D;
			}
		}
#else
		private void BuildTexture3D( TextureArrayCreatorAsset asset )
		{
			int sizeX = asset.SizeX;
			int sizeY = asset.SizeY;
			int numLevels = 1 + (int)Mathf.Floor( Mathf.Log( Mathf.Max( sizeX, sizeY ), 2 ) );
			int mipCount = asset.MipMaps ? numLevels : 1;

			Texture3D texture3D = new Texture3D( sizeX, sizeY, asset.AllTextures.Count, asset.SelectedFormatEnum, asset.MipMaps );
			texture3D.wrapMode = asset.WrapMode;
			texture3D.filterMode = asset.FilterMode;
			texture3D.anisoLevel = asset.AnisoLevel;
			texture3D.Apply( false );
			RenderTexture cache = RenderTexture.active;
			RenderTexture rt = new RenderTexture( sizeX, sizeY, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Default );
			rt.Create();
			List<List<Color>> mipColor = new List<List<Color>>();
			if( asset.MipMaps )
			{
				for( int i = 0; i < mipCount; i++ )
				{
					mipColor.Add( new List<Color>() );
				}
			}
			else
			{
				mipColor.Add( new List<Color>() );
			}

			for( int i = 0; i < asset.AllTextures.Count; i++ )
			{
				// build report
				int widthChanges = asset.AllTextures[ i ].width < sizeX ? -1 : asset.AllTextures[ i ].width > sizeX ? 1 : 0;
				int heightChanges = asset.AllTextures[ i ].height < sizeY ? -1 : asset.AllTextures[ i ].height > sizeY ? 1 : 0;
				if( ( widthChanges < 0 && heightChanges <= 0 ) || ( widthChanges <= 0 && heightChanges < 0 ) )
					m_message += asset.AllTextures[ i ].name + " was upscaled\n";
				else if( ( widthChanges > 0 && heightChanges >= 0 ) || ( widthChanges >= 0 && heightChanges > 0 ) )
					m_message += asset.AllTextures[ i ].name + " was downscaled\n";
				else if( ( widthChanges > 0 && heightChanges < 0 ) || ( widthChanges < 0 && heightChanges > 0 ) )
					m_message += asset.AllTextures[ i ].name + " changed dimensions\n";

				// blit image to upscale or downscale the image to any size
				RenderTexture.active = rt;

				bool cachedsrgb = GL.sRGBWrite;
				GL.sRGBWrite = !asset.LinearMode;
				Graphics.Blit( asset.AllTextures[ i ], rt );
				GL.sRGBWrite = cachedsrgb;

				bool isCompressed = UncompressedFormats.FindIndex( x => x.Equals( asset.SelectedFormatEnum ) ) < 0;
				TextureFormat validReadPixelsFormat = isCompressed ? TextureFormat.RGBAFloat : asset.SelectedFormatEnum;
				Texture2D t2d = new Texture2D( sizeX, sizeY, validReadPixelsFormat, asset.MipMaps, asset.LinearMode );
				t2d.ReadPixels( new Rect( 0, 0, sizeX, sizeY ), 0, 0, asset.MipMaps );
				RenderTexture.active = null;

				if( isCompressed )
				{
					EditorUtility.CompressTexture( t2d, asset.SelectedFormatEnum, asset.Quality );
					//	t2d.Apply( false );
				}
				t2d.Apply( false );

				if( asset.MipMaps )
				{
					for( int mip = 0; mip < mipCount; mip++ )
					{
						mipColor[ mip ].AddRange( t2d.GetPixels( mip ) );
					}
				}
				else
				{
					mipColor[ 0 ].AddRange( t2d.GetPixels( 0 ) );
				}
			}

			rt.Release();
			RenderTexture.active = cache;

			if( m_message.Length > 0 )
				m_message = m_message.Substring( 0, m_message.Length - 1 );

			for( int i = 0; i < mipCount; i++ )
			{
				texture3D.SetPixels( mipColor[ i ].ToArray(), i );
			}

			texture3D.Apply( false );

			string path = asset.FolderPath + asset.FileName + ".asset";
			Texture3D outfile = AssetDatabase.LoadMainAssetAtPath( path ) as Texture3D;
			if( outfile != null )
			{
				EditorUtility.CopySerialized( texture3D, outfile );
				AssetDatabase.SaveAssets();
				EditorGUIUtility.PingObject( outfile );
				m_lastSaved = outfile;
			}
			else
			{
				AssetDatabase.CreateAsset( texture3D, path );
				EditorGUIUtility.PingObject( texture3D );
				m_lastSaved = texture3D;
			}
		}
#endif
		private void BuildTexture3DAutoMips( TextureArrayCreatorAsset asset )
		{
			int sizeX = asset.SizeX;
			int sizeY = asset.SizeY;

			Texture3D texture3D = new Texture3D( sizeX, sizeY, asset.AllTextures.Count, asset.SelectedFormatEnum, asset.MipMaps );
			texture3D.wrapMode = asset.WrapMode;
			texture3D.filterMode = asset.FilterMode;
			texture3D.anisoLevel = asset.AnisoLevel;
			texture3D.Apply( false );
			RenderTexture cache = RenderTexture.active;
			RenderTexture rt = new RenderTexture( sizeX, sizeY, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Default );
			rt.Create();
			List<Color> texColors = new List<Color>();

			for( int i = 0; i < asset.AllTextures.Count; i++ )
			{
				// build report
				int widthChanges = asset.AllTextures[ i ].width < sizeX ? -1 : asset.AllTextures[ i ].width > sizeX ? 1 : 0;
				int heightChanges = asset.AllTextures[ i ].height < sizeY ? -1 : asset.AllTextures[ i ].height > sizeY ? 1 : 0;
				if( ( widthChanges < 0 && heightChanges <= 0 ) || ( widthChanges <= 0 && heightChanges < 0 ) )
					m_message += asset.AllTextures[ i ].name + " was upscaled\n";
				else if( ( widthChanges > 0 && heightChanges >= 0 ) || ( widthChanges >= 0 && heightChanges > 0 ) )
					m_message += asset.AllTextures[ i ].name + " was downscaled\n";
				else if( ( widthChanges > 0 && heightChanges < 0 ) || ( widthChanges < 0 && heightChanges > 0 ) )
					m_message += asset.AllTextures[ i ].name + " changed dimensions\n";

				// blit image to upscale or downscale the image to any size
				RenderTexture.active = rt;

				bool cachedsrgb = GL.sRGBWrite;
				GL.sRGBWrite = !asset.LinearMode;
				Graphics.Blit( asset.AllTextures[ i ], rt );
				GL.sRGBWrite = cachedsrgb;

				bool isCompressed = UncompressedFormats.FindIndex( x => x.Equals( asset.SelectedFormatEnum ) ) < 0;
				TextureFormat validReadPixelsFormat = isCompressed ? TextureFormat.RGBAFloat : asset.SelectedFormatEnum;
				Texture2D t2d = new Texture2D( sizeX, sizeY, validReadPixelsFormat, asset.MipMaps, asset.LinearMode );
				t2d.ReadPixels( new Rect( 0, 0, sizeX, sizeY ), 0, 0, asset.MipMaps );
				RenderTexture.active = null;

				if( isCompressed )
				{
					EditorUtility.CompressTexture( t2d, asset.SelectedFormatEnum, asset.Quality );
					t2d.Apply( false );
				}
				texColors.AddRange( t2d.GetPixels() );
			}

			rt.Release();
			RenderTexture.active = cache;

			if( m_message.Length > 0 )
				m_message = m_message.Substring( 0, m_message.Length - 1 );

			texture3D.SetPixels( texColors.ToArray() );
			texture3D.Apply();

			string path = asset.FolderPath + asset.FileName + ".asset";
			Texture3D outfile = AssetDatabase.LoadMainAssetAtPath( path ) as Texture3D;
			if( outfile != null )
			{
				EditorUtility.CopySerialized( texture3D, outfile );
				AssetDatabase.SaveAssets();
				EditorGUIUtility.PingObject( outfile );
				m_lastSaved = outfile;
			}
			else
			{
				AssetDatabase.CreateAsset( texture3D, path );
				EditorGUIUtility.PingObject( texture3D );
				m_lastSaved = texture3D;
			}
		}

		private void BuildArray( TextureArrayCreatorAsset asset )
		{
			int sizeX = asset.SizeX;
			int sizeY = asset.SizeY;

			Texture2DArray textureArray = new Texture2DArray( sizeX, sizeY, asset.AllTextures.Count, asset.SelectedFormatEnum, asset.MipMaps, asset.LinearMode );
			textureArray.wrapMode = asset.WrapMode;
			textureArray.filterMode = asset.FilterMode;
			textureArray.anisoLevel = asset.AnisoLevel;
			textureArray.Apply( false );
			RenderTexture cache = RenderTexture.active;
			RenderTexture rt = new RenderTexture( sizeX, sizeY, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Default );
			rt.Create();
			for( int i = 0; i < asset.AllTextures.Count; i++ )
			{
				// build report
				int widthChanges = asset.AllTextures[ i ].width < sizeX ? -1 : asset.AllTextures[ i ].width > sizeX ? 1 : 0;
				int heightChanges = asset.AllTextures[ i ].height < sizeY ? -1 : asset.AllTextures[ i ].height > sizeY ? 1 : 0;
				if( ( widthChanges < 0 && heightChanges <= 0 ) || ( widthChanges <= 0 && heightChanges < 0 ) )
					m_message += asset.AllTextures[ i ].name + " was upscaled\n";
				else if( ( widthChanges > 0 && heightChanges >= 0 ) || ( widthChanges >= 0 && heightChanges > 0 ) )
					m_message += asset.AllTextures[ i ].name + " was downscaled\n";
				else if( ( widthChanges > 0 && heightChanges < 0 ) || ( widthChanges < 0 && heightChanges > 0 ) )
					m_message += asset.AllTextures[ i ].name + " changed dimensions\n";

				// blit image to upscale or downscale the image to any size
				RenderTexture.active = rt;

				bool cachedsrgb = GL.sRGBWrite;
				GL.sRGBWrite = !asset.LinearMode;
				Graphics.Blit( asset.AllTextures[ i ], rt );
				GL.sRGBWrite = cachedsrgb;

				bool isCompressed = UncompressedFormats.FindIndex( x => x.Equals( asset.SelectedFormatEnum ) ) < 0;
				TextureFormat validReadPixelsFormat = isCompressed ? TextureFormat.RGBAFloat : asset.SelectedFormatEnum;
				Texture2D t2d = new Texture2D( sizeX, sizeY, validReadPixelsFormat, asset.MipMaps, asset.LinearMode );
				t2d.ReadPixels( new Rect( 0, 0, sizeX, sizeY ), 0, 0, asset.MipMaps );
				RenderTexture.active = null;

				if( isCompressed )
				{
					EditorUtility.CompressTexture( t2d, asset.SelectedFormatEnum, asset.Quality );
					t2d.Apply( false );
				}

				if( asset.MipMaps )
				{
					int maxSize = Mathf.Max( sizeX, sizeY );
					int numLevels = 1 + (int)Mathf.Floor( Mathf.Log( maxSize, 2 ) );
					for( int mip = 0; mip < numLevels; mip++ )
					{
						CopyToArray( ref t2d, ref textureArray, i, mip, isCompressed );
					}
				}
				else
				{
					CopyToArray( ref t2d, ref textureArray, i, 0, isCompressed );
				}
			}

			rt.Release();
			RenderTexture.active = cache;
			if( m_message.Length > 0 )
				m_message = m_message.Substring( 0, m_message.Length - 1 );

			string path = asset.FolderPath + asset.FileName + ".asset";
			Texture2DArray outfile = AssetDatabase.LoadMainAssetAtPath( path ) as Texture2DArray;
			if( outfile != null )
			{
				EditorUtility.CopySerialized( textureArray, outfile );
				AssetDatabase.SaveAssets();
				EditorGUIUtility.PingObject( outfile );
				m_lastSaved = outfile;
			}
			else
			{
				AssetDatabase.CreateAsset( textureArray, path );
				EditorGUIUtility.PingObject( textureArray );
				m_lastSaved = textureArray;
			}
		}
	}
}
