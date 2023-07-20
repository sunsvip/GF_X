// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using System;
using System.Reflection;
using System.Globalization;
using UnityEngine;
using AmplifyShaderEditor;

namespace UnityEditor
{
	[CustomEditor( typeof( Shader ) )]
	internal class CustomShaderInspector : Editor
	{
		internal class Styles
		{
			public static Texture2D errorIcon = EditorGUIUtilityEx.LoadIcon( "console.erroricon.sml" );

			public static Texture2D warningIcon = EditorGUIUtilityEx.LoadIcon( "console.warnicon.sml" );
			#if UNITY_2020_1_OR_NEWER
			public static GUIContent togglePreprocess = EditorGUIUtilityEx.TextContent( "Preprocess only|Show preprocessor output instead of compiled shader code" );
			#if UNITY_2020_2_OR_NEWER
			public static GUIContent toggleStripLineDirective = EditorGUIUtility.TrTextContent( "Strip #line directives", "Strip #line directives from preprocessor output" );
			#endif
			#endif				
			public static GUIContent showSurface = EditorGUIUtilityEx.TextContent( "Show generated code|Show generated code of a surface shader" );

			public static GUIContent showFF = EditorGUIUtilityEx.TextContent( "Show generated code|Show generated code of a fixed function shader" );

			public static GUIContent showCurrent = new GUIContent( "Compile and show code | ▾" );

			public static GUIStyle messageStyle = "CN StatusInfo";

			public static GUIStyle evenBackground = "CN EntryBackEven";

			public static GUIContent no = EditorGUIUtilityEx.TextContent( "no" );

			public static GUIContent builtinShader = EditorGUIUtilityEx.TextContent( "Built-in shader" );

			public static GUIContent arrayValuePopupButton = EditorGUIUtilityEx.TextContent( "..." );
		}
#if UNITY_2020_1_OR_NEWER
		private static bool s_PreprocessOnly = false;
#if UNITY_2020_2_OR_NEWER
		private static bool s_StripLineDirectives = true;
#endif
#endif
		private const float kSpace = 5f;

		const float kValueFieldWidth = 200.0f;
		const float kArrayValuePopupBtnWidth = 25.0f;

		private static readonly string[] kPropertyTypes = new string[]
		{
			"Color: ",
			"Vector: ",
			"Float: ",
			"Range: ",
			"Texture: "
		};

		private static readonly string[] kTextureTypes = new string[]
		{
			"No Texture?: ",
			"1D?: ",
			"2D: ",
			"3D: ",
			"Cube: ",
			"2DArray: ",
			"Any texture: "
		};

		private static readonly int kErrorViewHash = "ShaderErrorView".GetHashCode();

		private Vector2 m_ScrollPosition = Vector2.zero;

		private PreviewRenderUtility m_previewRenderUtility;
		private Material m_material;
		private Mesh m_previewMesh;
		private Vector2 m_mouseDelta;
		private Transform m_cameraTransform;

		private static int m_sliderHashCode = -1;
		private const float MaxDeltaY = 90;
		private const int DefaultMouseSpeed = 1;
		private const int ShiftMouseSpeed = 3;
		private const float DeltaMultiplier = 135f;
		private void ValidateData()
		{
			if ( m_previewRenderUtility == null )
			{
				m_previewRenderUtility = new PreviewRenderUtility();
				m_cameraTransform = m_previewRenderUtility.camera.transform;
				m_cameraTransform.position = new Vector3( 0, 0, -4 );
				m_cameraTransform.rotation = Quaternion.identity;
			}

			if ( m_material == null )
			{
				m_material = new Material( target as Shader );
				m_material.hideFlags = HideFlags.DontSave;
			}

			if ( m_previewMesh == null )
			{
				m_previewMesh = Resources.GetBuiltinResource<Mesh>( "Sphere.fbx" );
			}

			if ( m_sliderHashCode < 0 )
			{
				"Slider".GetHashCode();
			}
		}

		public override bool HasPreviewGUI()
		{
			ValidateData();
			return true;
		}

		public static Vector2 CheckMouseMovement( Vector2 scrollPosition, Rect position )
		{
			int controlID = GUIUtility.GetControlID( m_sliderHashCode, FocusType.Passive );
			Event current = Event.current;
			switch ( current.GetTypeForControl( controlID ) )
			{
				case EventType.MouseDown:
				{
					if ( position.Contains( current.mousePosition ) && position.width > 50f )
					{
						GUIUtility.hotControl = controlID;
						current.Use();
						EditorGUIUtility.SetWantsMouseJumping( 1 );
					}
				}
				break;
				case EventType.MouseUp:
				{
					if ( GUIUtility.hotControl == controlID )
					{
						GUIUtility.hotControl = 0;
					}
					EditorGUIUtility.SetWantsMouseJumping( 0 );
				}
				break;
				case EventType.MouseDrag:
				{
					if ( GUIUtility.hotControl == controlID )
					{
						scrollPosition -= DeltaMultiplier * current.delta * ( float ) ( ( current.shift ) ? ShiftMouseSpeed : DefaultMouseSpeed ) / Mathf.Min( position.width, position.height );
						scrollPosition.y = Mathf.Clamp( scrollPosition.y, -MaxDeltaY, MaxDeltaY );
						current.Use();
					}
				}
				break;
			}
			return scrollPosition;
		}

		public override void OnPreviewGUI( Rect r, GUIStyle background )
		{
			m_mouseDelta = CheckMouseMovement( m_mouseDelta, r );

			if ( Event.current.type == EventType.Repaint )
			{
				m_previewRenderUtility.BeginPreview( r, background );

				Texture resultRender = m_previewRenderUtility.EndPreview();
				m_previewRenderUtility.DrawMesh( m_previewMesh, Matrix4x4.identity, m_material, 0 );
				m_cameraTransform.rotation = Quaternion.Euler( new Vector3( -m_mouseDelta.y, -m_mouseDelta.x, 0 ) );
				m_cameraTransform.position = m_cameraTransform.forward * -8f;
				m_previewRenderUtility.camera.Render();
				GUI.DrawTexture( r, resultRender, ScaleMode.StretchToFill, false );
			}
		}
		
		void OnDestroy()
		{
			CleanUp();
		}

		public void OnDisable()
		{
			CleanUp();
			if( m_SrpCompatibilityCheckMaterial != null )
			{
				GameObject.DestroyImmediate( m_SrpCompatibilityCheckMaterial );
			}
		}
		
		void CleanUp()
		{
			if( m_previewRenderUtility != null )
			{
				m_previewRenderUtility.Cleanup();
				m_previewRenderUtility = null;
			}

			if( m_previewMesh != null )
			{
				Resources.UnloadAsset( m_previewMesh );
				m_previewMesh = null;
			}

			if( m_previewRenderUtility != null )
			{
				m_previewRenderUtility.Cleanup();
				m_previewRenderUtility = null;
			}
			m_material = null;
		}

		private Material m_SrpCompatibilityCheckMaterial = null;
		public Material srpCompatibilityCheckMaterial
		{
			get
			{
				if( m_SrpCompatibilityCheckMaterial == null )
				{
					m_SrpCompatibilityCheckMaterial = new Material( target as Shader );
				}
				return m_SrpCompatibilityCheckMaterial;
			}
		}

		public virtual void OnEnable()
		{
			Shader s = this.target as Shader;
			if( s!= null )
				ShaderUtilEx.FetchCachedErrors( s );
		}
		
		private static string GetPropertyType( Shader s, int index )
		{
			UnityEditor.ShaderUtil.ShaderPropertyType propertyType = UnityEditor.ShaderUtil.GetPropertyType( s, index );
			if ( propertyType == UnityEditor.ShaderUtil.ShaderPropertyType.TexEnv )
			{
				return CustomShaderInspector.kTextureTypes[ ( int ) UnityEditor.ShaderUtil.GetTexDim( s, index ) ];
			}
			return CustomShaderInspector.kPropertyTypes[ ( int ) propertyType ];
		}

		public override void OnInspectorGUI()
		{
			Shader shader = this.target as Shader;
			if ( shader == null )
			{
				return;
			}

			GUI.enabled = true;

			GUILayout.Space( 3 );
			GUILayout.BeginHorizontal();
			{
				if ( GUILayout.Button( "Open in Shader Editor" ) )
				{
					ASEPackageManagerHelper.SetupLateShader( shader );
				}

				if ( GUILayout.Button( "Open in Text Editor" ) )
				{
					if( UIUtils.IsUnityNativeShader( shader ) )
					{
						Debug.LogWarningFormat( "Action not allowed. Attempting to load the native {0} shader into Text Editor", shader.name );
					}
					else
					{
						AssetDatabase.OpenAsset( shader, 1 );
					}
				}
			}
			GUILayout.EndHorizontal();

			GUILayout.Space( 5 );
			EditorGUI.indentLevel = 0;
			this.ShowShaderCodeArea( shader );
			if ( shader.isSupported )
			{
				EditorGUILayout.LabelField( "Cast shadows", ( !ShaderUtilEx.HasShadowCasterPass( shader ) ) ? "no" : "yes", new GUILayoutOption[ 0 ] );
				EditorGUILayout.LabelField( "Render queue", ShaderUtilEx.GetRenderQueue( shader ).ToString( System.Globalization.CultureInfo.InvariantCulture ), new GUILayoutOption[ 0 ] );
				EditorGUILayout.LabelField( "LOD", ShaderUtilEx.GetLOD( shader ).ToString( System.Globalization.CultureInfo.InvariantCulture ), new GUILayoutOption[ 0 ] );
				EditorGUILayout.LabelField( "Ignore projector", ( !ShaderUtilEx.DoesIgnoreProjector( shader ) ) ? "no" : "yes", new GUILayoutOption[ 0 ] );
				string label;
				switch ( ShaderEx.GetDisableBatching( shader ) )
				{
					case DisableBatchingType.False:
					label = "no";
					break;
					case DisableBatchingType.True:
					label = "yes";
					break;
					case DisableBatchingType.WhenLODFading:
					label = "when LOD fading is on";
					break;
					default:
					label = "unknown";
					break;
				}
				EditorGUILayout.LabelField( "Disable batching", label, new GUILayoutOption[ 0 ] );
				ShowKeywords( shader );
				srpCompatibilityCheckMaterial.SetPass( 0 );

				int shaderActiveSubshaderIndex = ShaderUtilEx.GetShaderActiveSubshaderIndex( shader );
				int sRPBatcherCompatibilityCode = ShaderUtilEx.GetSRPBatcherCompatibilityCode( shader, shaderActiveSubshaderIndex );
				string label2 = ( sRPBatcherCompatibilityCode != 0 ) ? "not compatible" : "compatible";
				EditorGUILayout.LabelField( "SRP Batcher", label2 );
				if( sRPBatcherCompatibilityCode != 0 )
				{
					EditorGUILayout.HelpBox( ShaderUtilEx.GetSRPBatcherCompatibilityIssueReason( shader, shaderActiveSubshaderIndex, sRPBatcherCompatibilityCode ), MessageType.Info );
				}

				CustomShaderInspector.ShowShaderProperties( shader );
			}
		}

		private void ShowKeywords( Shader s )
		{
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.PrefixLabel( "Keywords", EditorStyles.miniButton );

			Rect buttonRect = GUILayoutUtility.GetRect( Styles.arrayValuePopupButton, GUI.skin.button, GUILayout.MinWidth( kValueFieldWidth ) );
			buttonRect.width = kArrayValuePopupBtnWidth;
			if( GUI.Button( buttonRect, Styles.arrayValuePopupButton, EditorStyles.miniButton ) )
			{
				var globalKeywords = ShaderUtilEx.GetShaderGlobalKeywords( s );
				var localKeywords = ShaderUtilEx.GetShaderLocalKeywords( s );
				PopupWindow.Show( buttonRect, new KeywordsPopup( globalKeywords, localKeywords, 150.0f ) );
			}

			EditorGUILayout.EndHorizontal();
		}

		private void ShowShaderCodeArea( Shader s )
		{
			CustomShaderInspector.ShowSurfaceShaderButton( s );
			CustomShaderInspector.ShowFixedFunctionShaderButton( s );
			this.ShowCompiledCodeButton( s );
			this.ShowShaderErrors( s );
		}

		private static void ShowShaderProperties( Shader s )
		{
			GUILayout.Space( 5f );
			GUILayout.Label( "Properties:", EditorStyles.boldLabel, new GUILayoutOption[ 0 ] );
			int propertyCount = UnityEditor.ShaderUtil.GetPropertyCount( s );
			for ( int i = 0; i < propertyCount; i++ )
			{
				string propertyName = UnityEditor.ShaderUtil.GetPropertyName( s, i );
				string label = CustomShaderInspector.GetPropertyType( s, i ) + UnityEditor.ShaderUtil.GetPropertyDescription( s, i );
				EditorGUILayout.LabelField( propertyName, label, new GUILayoutOption[ 0 ] );
			}
		}

		internal static void ShaderErrorListUI( UnityEngine.Object shader, ShaderError[] errors, ref Vector2 scrollPosition )
		{
			int num = errors.Length;
			GUILayout.Space( 5f );
			GUILayout.Label( string.Format( "Errors ({0}):", num ), EditorStyles.boldLabel, new GUILayoutOption[ 0 ] );
			int controlID = GUIUtility.GetControlID( CustomShaderInspector.kErrorViewHash, FocusType.Passive );
			float minHeight = Mathf.Min( ( float ) num * 20f + 40f, 150f );
			scrollPosition = GUILayout.BeginScrollView( scrollPosition, GUISkinEx.GetCurrentSkin().box, new GUILayoutOption[]
			{
				GUILayout.MinHeight(minHeight)
			} );
			EditorGUIUtility.SetIconSize( new Vector2( 16f, 16f ) );
			float height = CustomShaderInspector.Styles.messageStyle.CalcHeight( EditorGUIUtilityEx.TempContent( CustomShaderInspector.Styles.errorIcon ), 100f );
			Event current = Event.current;
			for ( int i = 0; i < num; i++ )
			{
				Rect controlRect = EditorGUILayout.GetControlRect( false, height, new GUILayoutOption[ 0 ] );
				string message = errors[ i ].message;
				string platform = errors[ i ].platform;
				bool flag = errors[ i ].warning != 0;
				string lastPathNameComponent = FileUtilEx.GetLastPathNameComponent( errors[ i ].file );
				int line = errors[ i ].line;
				if ( current.type == EventType.MouseDown && current.button == 0 && controlRect.Contains( current.mousePosition ) )
				{
					GUIUtility.keyboardControl = controlID;
					if ( current.clickCount == 2 )
					{
						string file = errors[ i ].file;
						UnityEngine.Object @object = ( !string.IsNullOrEmpty( file ) ) ? AssetDatabase.LoadMainAssetAtPath( file ) : null;
						AssetDatabase.OpenAsset( @object ?? shader, line );
						GUIUtility.ExitGUI();
					}
					current.Use();
				}
				if ( current.type == EventType.ContextClick && controlRect.Contains( current.mousePosition ) )
				{
					current.Use();
					GenericMenu genericMenu = new GenericMenu();
					int errorIndex = i;
					genericMenu.AddItem( new GUIContent( "Copy error text" ), false, delegate
					   {
						   string text = errors[ errorIndex ].message;
						   if ( !string.IsNullOrEmpty( errors[ errorIndex ].messageDetails ) )
						   {
							   text += '\n';
							   text += errors[ errorIndex ].messageDetails;
						   }
						   EditorGUIUtility.systemCopyBuffer = text;
					   } );
					genericMenu.ShowAsContext();
				}
				if ( current.type == EventType.Repaint && ( i & 1 ) == 0 )
				{
					GUIStyle evenBackground = CustomShaderInspector.Styles.evenBackground;
					evenBackground.Draw( controlRect, false, false, false, false );
				}
				Rect rect = controlRect;
				rect.xMin = rect.xMax;
				if ( line > 0 )
				{
					GUIContent content;
					if ( string.IsNullOrEmpty( lastPathNameComponent ) )
					{
						content = EditorGUIUtilityEx.TempContent( line.ToString( System.Globalization.CultureInfo.InvariantCulture ) );
					}
					else
					{
						content = EditorGUIUtilityEx.TempContent( lastPathNameComponent + ":" + line.ToString( System.Globalization.CultureInfo.InvariantCulture ) );
					}
					Vector2 vector = EditorStyles.miniLabel.CalcSize( content );
					rect.xMin -= vector.x;
					GUI.Label( rect, content, EditorStyles.miniLabel );
					rect.xMin -= 2f;
					if ( rect.width < 30f )
					{
						rect.xMin = rect.xMax - 30f;
					}
				}
				Rect position = rect;
				position.width = 0f;
				if ( platform.Length > 0 )
				{
					GUIContent content2 = EditorGUIUtilityEx.TempContent( platform );
					Vector2 vector2 = EditorStyles.miniLabel.CalcSize( content2 );
					position.xMin -= vector2.x;
					Color contentColor = GUI.contentColor;
					GUI.contentColor = new Color( 1f, 1f, 1f, 0.5f );
					GUI.Label( position, content2, EditorStyles.miniLabel );
					GUI.contentColor = contentColor;
					position.xMin -= 2f;
				}
				Rect position2 = controlRect;
				position2.xMax = position.xMin;
				GUI.Label( position2, EditorGUIUtilityEx.TempContent( message, ( !flag ) ? CustomShaderInspector.Styles.errorIcon : CustomShaderInspector.Styles.warningIcon ), CustomShaderInspector.Styles.messageStyle );
			}
			EditorGUIUtility.SetIconSize( Vector2.zero );
			GUILayout.EndScrollView();
		}

		ShaderMessage[] m_ShaderMessages;

		private void ShowShaderErrors( Shader s )
		{
			if( Event.current.type == EventType.Layout )
			{
				int n = ShaderUtil.GetShaderMessageCount( s );
				m_ShaderMessages = null;
				if( n >= 1 )
				{
					m_ShaderMessages = ShaderUtil.GetShaderMessages( s );
				}
			}

			if( m_ShaderMessages == null )
				return;

			ShaderInspectorEx.ShaderErrorListUI( s, m_ShaderMessages, ref this.m_ScrollPosition );
		}

		private void ShowCompiledCodeButton( Shader s )
		{
#if UNITY_2020_1_OR_NEWER
			using( new EditorGUI.DisabledScope( !EditorSettings.cachingShaderPreprocessor ) )
			{
				s_PreprocessOnly = EditorGUILayout.Toggle( Styles.togglePreprocess, s_PreprocessOnly );
#if UNITY_2020_2_OR_NEWER
				if( s_PreprocessOnly )
				{
					s_StripLineDirectives = EditorGUILayout.Toggle( Styles.toggleStripLineDirective, s_StripLineDirectives );
				}
#endif
			}
#endif
			EditorGUILayout.BeginHorizontal( new GUILayoutOption[ 0 ] );
			EditorGUILayout.PrefixLabel( "Compiled code", EditorStyles.miniButton );

			bool hasCode = ShaderUtilEx.HasShaderSnippets( s ) || ShaderUtilEx.HasSurfaceShaders( s ) || ShaderUtilEx.HasFixedFunctionShaders( s );
			if( hasCode )
			{
				GUIContent showCurrent = Styles.showCurrent;
				Rect rect = GUILayoutUtility.GetRect( showCurrent, EditorStyles.miniButton, new GUILayoutOption[]
				{
					GUILayout.ExpandWidth(false)
				} );
				Rect position = new Rect( rect.xMax - 16f, rect.y, 16f, rect.height );
				if( EditorGUIEx.ButtonMouseDown( position, GUIContent.none, FocusType.Passive, GUIStyle.none ) )
				{
					Rect last = GUILayoutUtilityEx.TopLevel_GetLast();
					PopupWindow.Show( last, (PopupWindowContent)Activator.CreateInstance( System.Type.GetType( "UnityEditor.ShaderInspectorPlatformsPopup, UnityEditor" ), new object[] { s } ) );
					GUIUtility.ExitGUI();
				}
				if( GUI.Button( rect, showCurrent, EditorStyles.miniButton ) )
				{
#if UNITY_2020_1
					ShaderUtilEx.OpenCompiledShader( s, ShaderInspectorPlatformsPopupEx.GetCurrentMode(), ShaderInspectorPlatformsPopupEx.GetCurrentPlatformMask(), ShaderInspectorPlatformsPopupEx.GetCurrentVariantStripping() == 0, s_PreprocessOnly );
#elif UNITY_2020_2_OR_NEWER
					ShaderUtilEx.OpenCompiledShader( s, ShaderInspectorPlatformsPopupEx.GetCurrentMode(), ShaderInspectorPlatformsPopupEx.GetCurrentPlatformMask(), ShaderInspectorPlatformsPopupEx.GetCurrentVariantStripping() == 0, s_PreprocessOnly, s_StripLineDirectives );
#else
					ShaderUtilEx.OpenCompiledShader( s, ShaderInspectorPlatformsPopupEx.GetCurrentMode(), ShaderInspectorPlatformsPopupEx.GetCurrentPlatformMask(), ShaderInspectorPlatformsPopupEx.GetCurrentVariantStripping() == 0 );
#endif
					GUIUtility.ExitGUI();
				}
			}
			else
			{
				GUILayout.Button( "none (precompiled shader)", GUI.skin.label, new GUILayoutOption[ 0 ] );
			}
			EditorGUILayout.EndHorizontal();
		}

		private static void ShowSurfaceShaderButton( Shader s )
		{
			bool flag = ShaderUtilEx.HasSurfaceShaders( s );
			EditorGUILayout.BeginHorizontal( new GUILayoutOption[ 0 ] );
			EditorGUILayout.PrefixLabel( "Surface shader", EditorStyles.miniButton );
			if ( flag )
			{
				if ( !( AssetImporter.GetAtPath( AssetDatabase.GetAssetPath( s ) ) == null ) )
				{
					if ( GUILayout.Button( CustomShaderInspector.Styles.showSurface, EditorStyles.miniButton, new GUILayoutOption[]
					{
						GUILayout.ExpandWidth(false)
					} ) )
					{
						ShaderUtilEx.OpenParsedSurfaceShader( s );
						GUIUtility.ExitGUI();
					}
				}
				else
				{
					GUILayout.Button( CustomShaderInspector.Styles.builtinShader, GUI.skin.label, new GUILayoutOption[ 0 ] );
				}
			}
			else
			{
				GUILayout.Button( CustomShaderInspector.Styles.no, GUI.skin.label, new GUILayoutOption[ 0 ] );
			}
			EditorGUILayout.EndHorizontal();
		}

		private static void ShowFixedFunctionShaderButton( Shader s )
		{
			bool flag = ShaderUtilEx.HasFixedFunctionShaders( s );
			EditorGUILayout.BeginHorizontal( new GUILayoutOption[ 0 ] );
			EditorGUILayout.PrefixLabel( "Fixed function", EditorStyles.miniButton );
			if ( flag )
			{
				if ( !( AssetImporter.GetAtPath( AssetDatabase.GetAssetPath( s ) ) == null ) )
				{
					if ( GUILayout.Button( CustomShaderInspector.Styles.showFF, EditorStyles.miniButton, new GUILayoutOption[]
					{
						GUILayout.ExpandWidth(false)
					} ) )
					{
						ShaderUtilEx.OpenGeneratedFixedFunctionShader( s );
						GUIUtility.ExitGUI();
					}
				}
				else
				{
					GUILayout.Button( CustomShaderInspector.Styles.builtinShader, GUI.skin.label, new GUILayoutOption[ 0 ] );
				}
			}
			else
			{
				GUILayout.Button( CustomShaderInspector.Styles.no, GUI.skin.label, new GUILayoutOption[ 0 ] );
			}
			EditorGUILayout.EndHorizontal();
		}
	}

	internal class KeywordsPopup : PopupWindowContent
	{
		private Vector2 m_ScrollPos = Vector2.zero;
		private string[] m_GlobalKeywords;
		private string[] m_LocalKeywords;
		private bool m_GlobalKeywordsExpended;
		private bool m_LocalKeywordsExpended;
		private float m_WindowWidth;

		private static readonly GUIStyle m_Style = EditorStyles.miniLabel;

		public KeywordsPopup( string[] globalKeywords, string[] localKeywords, float windowWidth )
		{
			m_GlobalKeywords = globalKeywords;
			m_LocalKeywords = localKeywords;
			m_GlobalKeywordsExpended = true;
			m_LocalKeywordsExpended = true;
			m_WindowWidth = windowWidth;
		}

		public override Vector2 GetWindowSize()
		{
			var numValues = m_GlobalKeywords.Length + m_LocalKeywords.Length + 2;
			var lineHeight = m_Style.lineHeight + m_Style.padding.vertical + m_Style.margin.top;
			return new Vector2( m_WindowWidth, Math.Min( lineHeight * numValues, 250.0f ) );
		}

		public override void OnGUI( Rect rect )
		{
			m_ScrollPos = EditorGUILayout.BeginScrollView( m_ScrollPos );

			m_GlobalKeywordsExpended = KeywordsFoldout( m_GlobalKeywordsExpended, "Global Keywords", m_GlobalKeywords );
			m_LocalKeywordsExpended = KeywordsFoldout( m_LocalKeywordsExpended, "Local Keywords", m_LocalKeywords );

			EditorGUILayout.EndScrollView();
		}

		private bool KeywordsFoldout( bool expended, string name, string[] values )
		{
			expended = EditorGUILayout.Foldout( expended, name, true, m_Style );

			if( expended )
			{
				EditorGUI.indentLevel++;
				for( int i = 0; i < values.Length; ++i )
				{
					EditorGUILayout.LabelField( values[ i ], m_Style );
				}
				EditorGUI.indentLevel--;
			}

			return expended;
		}
	}

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	// UNITY EDITOR EXTENSIONS
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	public enum DisableBatchingType
	{
		False,
		True,
		WhenLODFading
	}

	public struct ShaderError
	{
		public string message;
		public string messageDetails;
		public string platform;
		public string file;
		public int line;
		public int warning;
	}

	public static class EditorGUIUtilityEx
	{
		private static System.Type type = null;
		public static System.Type Type { get { return ( type == null ) ? type = System.Type.GetType( "UnityEditor.EditorGUIUtility, UnityEditor" ) : type; } }

		public static Texture2D LoadIcon( string icon )
		{
			return ( Texture2D ) EditorGUIUtilityEx.Type.InvokeMember( "LoadIcon", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.InvokeMethod, null, null, new object[] { icon } );
		}

		public static GUIContent TextContent( string t )
		{
			return ( GUIContent ) EditorGUIUtilityEx.Type.InvokeMember( "TextContent", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.InvokeMethod, null, null, new object[] { t } );
		}

		internal static GUIContent TempContent( string t )
		{
			return ( GUIContent ) EditorGUIUtilityEx.Type.InvokeMember( "TempContent", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.InvokeMethod, null, null, new object[] { t } );
		}

		internal static GUIContent TempContent( Texture i )
		{
			return ( GUIContent ) EditorGUIUtilityEx.Type.InvokeMember( "TempContent", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.InvokeMethod, null, null, new object[] { i } );
		}

		internal static GUIContent TempContent( string t, Texture i )
		{
			return ( GUIContent ) EditorGUIUtilityEx.Type.InvokeMember( "TempContent", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.InvokeMethod, null, null, new object[] { t, i } );
		}
	}

	public static class GUILayoutUtilityEx
	{
		private static System.Type type = null;
		public static System.Type Type { get { return ( type == null ) ? type = System.Type.GetType( "UnityEngine.GUILayoutUtility, UnityEngine" ) : type; } }

		public static Rect TopLevel_GetLast()
		{
			System.Type guiLayoutGroup = System.Type.GetType( "UnityEngine.GUILayoutGroup, UnityEngine" );
			var topLevel = GUILayoutUtilityEx.Type.GetProperty( "topLevel", BindingFlags.NonPublic | BindingFlags.Static ).GetValue( null, null );
			return ( Rect ) guiLayoutGroup.InvokeMember( "GetLast", BindingFlags.Public | BindingFlags.Instance | BindingFlags.InvokeMethod, null, topLevel, new object[] { } );
		}
	}

	public static class ShaderEx
	{
		private static System.Type type = null;
		public static System.Type Type { get { return ( type == null ) ? type = System.Type.GetType( "UnityEngine.Shader, UnityEngine" ) : type; } }

		public static DisableBatchingType GetDisableBatching( Shader s )
		{
			return ( DisableBatchingType ) ShaderEx.Type.GetProperty( "disableBatching", BindingFlags.NonPublic | BindingFlags.Instance ).GetValue( s, new object[ 0 ] );
		}
	}

	public static class ShaderUtilEx
	{
		private static System.Type type = null;
		public static System.Type Type { get { return ( type == null ) ? type = System.Type.GetType( "UnityEditor.ShaderUtil, UnityEditor" ) : type; } }

		public static void OpenParsedSurfaceShader( Shader s )
		{
			ShaderUtilEx.Type.InvokeMember( "OpenParsedSurfaceShader", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.InvokeMethod, null, null, new object[] { s } );
		}

		public static void OpenGeneratedFixedFunctionShader( Shader s )
		{
			ShaderUtilEx.Type.InvokeMember( "OpenGeneratedFixedFunctionShader", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.InvokeMethod, null, null, new object[] { s } );
		}

#if UNITY_2020_1
		public static void OpenCompiledShader( Shader shader, int mode, int customPlatformsMask, bool includeAllVariants, bool preprocessOnly )
		{
			ShaderUtilEx.Type.InvokeMember( "OpenCompiledShader", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.InvokeMethod, null, null, new object[] { shader, mode, customPlatformsMask, includeAllVariants, preprocessOnly } );
		}
#elif UNITY_2020_2_OR_NEWER
		public static void OpenCompiledShader( Shader shader, int mode, int customPlatformsMask, bool includeAllVariants, bool preprocessOnly, bool stripLineDirectives )
		{
			ShaderUtilEx.Type.InvokeMember( "OpenCompiledShader", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.InvokeMethod, null, null, new object[] { shader, mode, customPlatformsMask, includeAllVariants, preprocessOnly, stripLineDirectives } );
		}
#else
		public static void OpenCompiledShader( Shader shader, int mode, int customPlatformsMask, bool includeAllVariants )
		{
			ShaderUtilEx.Type.InvokeMember( "OpenCompiledShader", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.InvokeMethod, null, null, new object[] { shader, mode, customPlatformsMask, includeAllVariants } );
		}
#endif
		public static void FetchCachedErrors( Shader s )
		{
			ShaderUtilEx.Type.InvokeMember( "FetchCachedMessages", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.InvokeMethod, null, null, new object[] { s } );
		}

		public static string[] GetShaderGlobalKeywords( Shader s )
		{
			return ShaderUtilEx.Type.InvokeMember( "GetShaderGlobalKeywords", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.InvokeMethod, null, null, new object[] { s } ) as string[];
		}

		public static string[] GetShaderLocalKeywords( Shader s )
		{
			return ShaderUtilEx.Type.InvokeMember( "GetShaderLocalKeywords", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.InvokeMethod, null, null, new object[] { s } ) as string[];
		}

		public static int GetShaderErrorCount( Shader s )
		{
			return ShaderUtil.GetShaderMessageCount( s );
		}

		public static int GetAvailableShaderCompilerPlatforms()
		{
			return (int)ShaderUtilEx.Type.InvokeMember( "GetAvailableShaderCompilerPlatforms", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.InvokeMethod, null, null, new object[] { } );
		}

		public static ShaderError[] GetShaderErrors( Shader s )
		{
			System.Type shaderErrorType = System.Type.GetType( "UnityEditor.ShaderError, UnityEditor" );
			var errorList = ( System.Collections.IList ) ShaderUtilEx.Type.InvokeMember( "GetShaderErrors", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.InvokeMethod, null, null, new object[] { s } );

			FieldInfo messageField = shaderErrorType.GetField( "message", BindingFlags.Public | BindingFlags.Instance );
			FieldInfo messageDetailsField = shaderErrorType.GetField( "messageDetails", BindingFlags.Public | BindingFlags.Instance );
			FieldInfo platformField = shaderErrorType.GetField( "platform", BindingFlags.Public | BindingFlags.Instance );
			FieldInfo fileField = shaderErrorType.GetField( "file", BindingFlags.Public | BindingFlags.Instance );
			FieldInfo lineField = shaderErrorType.GetField( "line", BindingFlags.Public | BindingFlags.Instance );
			FieldInfo warningField = shaderErrorType.GetField( "warning", BindingFlags.Public | BindingFlags.Instance );

			ShaderError[] errors = new ShaderError[ errorList.Count ];
			for ( int i = 0; i < errorList.Count; i++ )
			{
				errors[ i ].message = ( string ) messageField.GetValue( errorList[ i ] );
				errors[ i ].messageDetails = ( string ) messageDetailsField.GetValue( errorList[ i ] );
				errors[ i ].platform = ( string ) platformField.GetValue( errorList[ i ] );
				errors[ i ].file = ( string ) fileField.GetValue( errorList[ i ] );
				errors[ i ].line = ( int ) lineField.GetValue( errorList[ i ] );
				errors[ i ].warning = ( int ) warningField.GetValue( errorList[ i ] );
			}
			return errors;
		}

		public static bool HasShaderSnippets( Shader s )
		{
			return ( bool ) ShaderUtilEx.Type.InvokeMember( "HasShaderSnippets", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.InvokeMethod, null, null, new object[] { s } );
		}

		public static bool HasSurfaceShaders( Shader s )
		{
			return ( bool ) ShaderUtilEx.Type.InvokeMember( "HasSurfaceShaders", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.InvokeMethod, null, null, new object[] { s } );
		}

		public static bool HasFixedFunctionShaders( Shader s )
		{
			return ( bool ) ShaderUtilEx.Type.InvokeMember( "HasFixedFunctionShaders", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.InvokeMethod, null, null, new object[] { s } );
		}

		public static bool HasShadowCasterPass( Shader s )
		{
			return ( bool ) ShaderUtilEx.Type.InvokeMember( "HasShadowCasterPass", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.InvokeMethod, null, null, new object[] { s } );
		}

		public static int GetRenderQueue( Shader s )
		{
			return ( int ) ShaderUtilEx.Type.InvokeMember( "GetRenderQueue", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.InvokeMethod, null, null, new object[] { s } );
		}

		public static int GetLOD( Shader s )
		{
			return ( int ) ShaderUtilEx.Type.InvokeMember( "GetLOD", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.InvokeMethod, null, null, new object[] { s } );
		}

		public static bool DoesIgnoreProjector( Shader s )
		{
			return ( bool ) ShaderUtilEx.Type.InvokeMember( "DoesIgnoreProjector", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.InvokeMethod, null, null, new object[] { s } );
		}

		public static int GetShaderActiveSubshaderIndex( Shader s )
		{
			return (int)ShaderUtilEx.Type.InvokeMember( "GetShaderActiveSubshaderIndex", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.InvokeMethod, null, null, new object[] { s } );
		}

		public static int GetSRPBatcherCompatibilityCode( Shader s, int subShaderIdx )
		{
			return (int)ShaderUtilEx.Type.InvokeMember( "GetSRPBatcherCompatibilityCode", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.InvokeMethod, null, null, new object[] { s, subShaderIdx } );
		}

		public static string GetSRPBatcherCompatibilityIssueReason( Shader s, int subShaderIdx, int err )
		{
			return (string)ShaderUtilEx.Type.InvokeMember( "GetSRPBatcherCompatibilityIssueReason", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.InvokeMethod, null, null, new object[] { s, subShaderIdx, err } );
		}
	}

	public static class FileUtilEx
	{
		private static System.Type type = null;
		public static  System.Type Type { get { return ( type == null ) ? type = System.Type.GetType( "UnityEditor.FileUtil, UnityEditor" ) : type; } }

		public static string GetLastPathNameComponent( string path )
		{
			return ( string ) FileUtilEx.Type.InvokeMember( "GetLastPathNameComponent", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.InvokeMethod, null, null, new object[] { path } );
		}
	}

	public static class ShaderInspectorEx
	{
		private static System.Type type = null;
		public static  System.Type Type { get { return ( type == null ) ? type = System.Type.GetType( "UnityEditor.ShaderInspector, UnityEditor" ) : type; } }

		public static void ShaderErrorListUI( UnityEngine.Object shader, ShaderMessage[] messages, ref Vector2 scrollPosition )
		{
			Type.InvokeMember( "ShaderErrorListUI", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.InvokeMethod, null, null, new object[] { shader, messages, scrollPosition } );
		}
	}

	public static class GUISkinEx
	{
		private static System.Type type = null;
		public static System.Type Type { get { return ( type == null ) ? type = System.Type.GetType( "UnityEngine.GUISkin, UnityEngine" ) : type; } }

		public static GUISkin GetCurrentSkin()
		{
			return ( GUISkin ) GUISkinEx.Type.GetField( "current", BindingFlags.NonPublic | BindingFlags.Static ).GetValue( null );
		}
	}

	public static class EditorGUIEx
	{
		public static System.Type Type = typeof( EditorGUI );

		public static bool ButtonMouseDown( Rect position, GUIContent content, FocusType focusType, GUIStyle style )
		{
			return EditorGUI.DropdownButton( position, content, focusType, style );
		}

		public static float kObjectFieldMiniThumbnailHeight
		{
			get
			{
				return (float)EditorGUIEx.Type.InvokeMember( "kObjectFieldMiniThumbnailHeight", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.GetField, null, null, new object[] {} );
			}
		}

		public static float kSingleLineHeight
		{
			get
			{
				return (float)EditorGUIEx.Type.InvokeMember( "kSingleLineHeight", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.GetField, null, null, new object[] { } );
			}
		}

	}

	public static class ShaderInspectorPlatformsPopupEx
	{
		private static System.Type type = null;
		public static  System.Type Type { get { return ( type == null ) ? type = System.Type.GetType( "UnityEditor.ShaderInspectorPlatformsPopup, UnityEditor" ) : type; } }

		public static int GetCurrentMode()
		{
			return ( int ) ShaderInspectorPlatformsPopupEx.Type.GetProperty( "currentMode", BindingFlags.Public | BindingFlags.Static ).GetValue( null, null );
		}

		public static int GetCurrentPlatformMask()
		{
			return ( int ) ShaderInspectorPlatformsPopupEx.Type.GetProperty( "currentPlatformMask", BindingFlags.Public | BindingFlags.Static ).GetValue( null, null );
		}

		public static int GetCurrentVariantStripping()
		{
			return ( int ) ShaderInspectorPlatformsPopupEx.Type.GetProperty( "currentVariantStripping", BindingFlags.Public | BindingFlags.Static ).GetValue( null, null );
		}
	}
}
