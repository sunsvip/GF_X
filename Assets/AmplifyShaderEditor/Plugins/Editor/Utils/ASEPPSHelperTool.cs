// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>
using System;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
//using UnityEngine.Rendering.PostProcessing;


namespace AmplifyShaderEditor
{
	public enum ASEPostProcessEvent
	{
		BeforeTransparent = 0,
		BeforeStack = 1,
		AfterStack = 2
	}

	[Serializable]
	public class ASEPPSHelperBuffer
	{
		public string Name;
		public string Tooltip;
	}

	[Serializable]
	public class ASEPPSHelperTool : EditorWindow
	{
		private const string PPSFullTemplate =
		"// Amplify Shader Editor - Visual Shader Editing Tool\n" +
		"// Copyright (c) Amplify Creations, Lda <info@amplify.pt>\n" +
		"#if UNITY_POST_PROCESSING_STACK_V2\n" +
		"using System;\n" +
		"using UnityEngine;\n" +
		"using UnityEngine.Rendering.PostProcessing;\n" +
		"\n" +
		"[Serializable]\n" +
		"[PostProcess( typeof( /*PPSRendererClass*/ ), PostProcessEvent./*PPSEventType*/, \"/*PPSMenuEntry*/\", /*AllowInSceneView*/ )]\n" +
		"public sealed class /*PPSSettingsClass*/ : PostProcessEffectSettings\n" +
		"{\n" +
		"/*PPSPropertiesDeclaration*/" +
		"}\n" +
		"\n" +
		"public sealed class /*PPSRendererClass*/ : PostProcessEffectRenderer</*PPSSettingsClass*/>\n" +
		"{\n" +
		"\tpublic override void Render( PostProcessRenderContext context )\n" +
		"\t{\n" +
		"\t\tvar sheet = context.propertySheets.Get( Shader.Find( \"/*PPSShader*/\" ) );\n" +
		"/*PPSPropertySet*/" +
		"\t\tcontext.command.BlitFullscreenTriangle( context.source, context.destination, sheet, 0 );\n" +
		"\t}\n" +
		"}\n" +
		"#endif\n";

		private const string PPSEventType = "/*PPSEventType*/";
		private const string PPSRendererClass = "/*PPSRendererClass*/";
		private const string PPSSettingsClass = "/*PPSSettingsClass*/";
		private const string PPSMenuEntry = "/*PPSMenuEntry*/";
		private const string PPSAllowInSceneView = "/*AllowInSceneView*/";
		private const string PPSShader = "/*PPSShader*/";
		private const string PPSPropertiesDecl = "/*PPSPropertiesDeclaration*/";
		private const string PPSPropertySet = "/*PPSPropertySet*/";

		public static readonly string PPSPropertySetFormat = "\t\tsheet.properties.{0}( \"{1}\", settings.{1} );\n";
		public static readonly string PPSPropertySetNullPointerCheckFormat = "\t\tif(settings.{1}.value != null) sheet.properties.{0}( \"{1}\", settings.{1} );\n";
		public static readonly string PPSPropertyDecFormat =
		"\t[{0}Tooltip( \"{1}\" )]\n" +
		"\tpublic {2} {3} = new {2} {{ {4} }};\n";
		public static readonly Dictionary<WirePortDataType, string> WireToPPSType = new Dictionary<WirePortDataType, string>()
		{
			{ WirePortDataType.FLOAT,"FloatParameter"},
			{ WirePortDataType.FLOAT2,"Vector4Parameter"},
			{ WirePortDataType.FLOAT3,"Vector4Parameter"},
			{ WirePortDataType.FLOAT4,"Vector4Parameter"},
			{ WirePortDataType.COLOR,"ColorParameter"},
			{ WirePortDataType.SAMPLER1D,"TextureParameter"},
			{ WirePortDataType.SAMPLER2D,"TextureParameter"},
			{ WirePortDataType.SAMPLER3D,"TextureParameter"},
			{ WirePortDataType.SAMPLERCUBE,"TextureParameter"},
			{ WirePortDataType.SAMPLER2DARRAY,"TextureParameter"}
		};

		public static readonly Dictionary<WirePortDataType, string> WireToPPSValueSet = new Dictionary<WirePortDataType, string>()
		{
			{ WirePortDataType.FLOAT,"SetFloat"},
			{ WirePortDataType.FLOAT2,"SetVector"},
			{ WirePortDataType.FLOAT3,"SetVector"},
			{ WirePortDataType.FLOAT4,"SetVector"},
			{ WirePortDataType.COLOR,"SetColor"},
			{ WirePortDataType.SAMPLER1D,  "SetTexture"},
			{ WirePortDataType.SAMPLER2D,  "SetTexture"},
			{ WirePortDataType.SAMPLER3D,  "SetTexture"},
			{ WirePortDataType.SAMPLERCUBE,"SetTexture"},
			{ WirePortDataType.SAMPLER2DARRAY,"SetTexture"}
		};

		public static readonly Dictionary<UnityEditor.ShaderUtil.ShaderPropertyType, string> ShaderPropertyToPPSType = new Dictionary<UnityEditor.ShaderUtil.ShaderPropertyType, string>()
		{
			{ UnityEditor.ShaderUtil.ShaderPropertyType.Float,"FloatParameter"},
			{ UnityEditor.ShaderUtil.ShaderPropertyType.Range,"FloatParameter"},
			{ UnityEditor.ShaderUtil.ShaderPropertyType.Vector,"Vector4Parameter"},
			{ UnityEditor.ShaderUtil.ShaderPropertyType.Color,"ColorParameter"},
			{ UnityEditor.ShaderUtil.ShaderPropertyType.TexEnv,"TextureParameter"}
		};


		public static readonly Dictionary<UnityEditor.ShaderUtil.ShaderPropertyType, string> ShaderPropertyToPPSSet = new Dictionary<UnityEditor.ShaderUtil.ShaderPropertyType, string>()
		{
			{ UnityEditor.ShaderUtil.ShaderPropertyType.Float,"SetFloat"},
			{ UnityEditor.ShaderUtil.ShaderPropertyType.Range,"SetFloat"},
			{ UnityEditor.ShaderUtil.ShaderPropertyType.Vector,"SetVector"},
			{ UnityEditor.ShaderUtil.ShaderPropertyType.Color,"SetColor"},
			{ UnityEditor.ShaderUtil.ShaderPropertyType.TexEnv,"SetTexture"}
		};

		private Dictionary<string, bool> m_excludedProperties = new Dictionary<string, bool>
		{
			{ "_texcoord",true },
			{ "__dirty",true}
		};

		private Material m_dummyMaterial = null;

		private DragAndDropTool m_dragAndDropTool;
		private Rect m_draggableArea;

		[SerializeField]
		private string m_rendererClassName = "PPSRenderer";

		[SerializeField]
		private string m_settingsClassName = "PPSSettings";

		[SerializeField]
		private string m_folderPath = "Assets/";

		[SerializeField]
		private string m_menuEntry = string.Empty;

		[SerializeField]
		private bool m_allowInSceneView = true;

		[SerializeField]
		private ASEPostProcessEvent m_eventType = ASEPostProcessEvent.AfterStack;

		[SerializeField]
		private Shader m_currentShader = null;

		[SerializeField]
		private List<ASEPPSHelperBuffer> m_tooltips = new List<ASEPPSHelperBuffer>();

		[SerializeField]
		private bool m_tooltipsFoldout = true;

		private GUIStyle m_contentStyle = null;
		private GUIStyle m_pathButtonStyle = null;
		private GUIContent m_pathButtonContent = new GUIContent();
		private Vector2 m_scrollPos = Vector2.zero;

		[MenuItem( "Window/Amplify Shader Editor/Post-Processing Stack Tool", false, 1001 )]
		static void ShowWindow()
		{
			ASEPPSHelperTool window = EditorWindow.GetWindow<ASEPPSHelperTool>();
			window.titleContent.text = "Post-Processing Stack Tool";
			window.minSize = new Vector2( 302, 350 );
			window.Show();
		}

		void FetchTooltips()
		{
			m_tooltips.Clear();
			int propertyCount = UnityEditor.ShaderUtil.GetPropertyCount( m_currentShader );
			for( int i = 0; i < propertyCount; i++ )
			{
				//UnityEditor.ShaderUtil.ShaderPropertyType type = UnityEditor.ShaderUtil.GetPropertyType( m_currentShader, i );
				string name = UnityEditor.ShaderUtil.GetPropertyName( m_currentShader, i );
				string description = UnityEditor.ShaderUtil.GetPropertyDescription( m_currentShader, i );

				if( m_excludedProperties.ContainsKey( name ))
					continue;

				m_tooltips.Add( new ASEPPSHelperBuffer { Name = name, Tooltip = description } );
			}
		}

		void OnGUI()
		{
			if( m_pathButtonStyle == null )
				m_pathButtonStyle = "minibutton";

			m_scrollPos = EditorGUILayout.BeginScrollView( m_scrollPos, GUILayout.Height( position.height ) );

			EditorGUILayout.BeginVertical( m_contentStyle );
			EditorGUI.BeginChangeCheck();
			m_currentShader = EditorGUILayout.ObjectField( "Shader", m_currentShader, typeof( Shader ), false ) as Shader;
			if( EditorGUI.EndChangeCheck() )
			{
				GetInitialInfo( m_currentShader );
			}

			EditorGUILayout.Separator();
			EditorGUILayout.LabelField( "Path and Filename" );
			EditorGUILayout.BeginHorizontal();
			m_pathButtonContent.text = m_folderPath;
			Vector2 buttonSize = m_pathButtonStyle.CalcSize( m_pathButtonContent );
			if( GUILayout.Button( m_pathButtonContent, m_pathButtonStyle, GUILayout.MaxWidth( Mathf.Min( position.width * 0.5f, buttonSize.x ) ) ) )
			{
				string folderpath = EditorUtility.OpenFolderPanel( "Save Texture Array to folder", "Assets/", "" );
				folderpath = FileUtil.GetProjectRelativePath( folderpath );
				if( string.IsNullOrEmpty( folderpath ) )
					m_folderPath = "Assets/";
				else
					m_folderPath = folderpath + "/";
			}

			m_settingsClassName = EditorGUILayout.TextField( m_settingsClassName, GUILayout.ExpandWidth( true ) );

			EditorGUILayout.LabelField( ".cs", GUILayout.MaxWidth( 40 ) );
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.HelpBox( "The path for the generated script should be outside of Amplify Shader Editor folder structure due to use of Assembly Definition files which will conflict and prevent to compile correctly.", MessageType.Warning );

			EditorGUILayout.Separator();

			m_menuEntry = EditorGUILayout.TextField( "Name", m_menuEntry );

			EditorGUILayout.Separator();

			m_allowInSceneView = EditorGUILayout.Toggle( "Allow In Scene View", m_allowInSceneView );

			EditorGUILayout.Separator();

			m_eventType = (ASEPostProcessEvent)EditorGUILayout.EnumPopup( "Event Type", m_eventType );

			EditorGUILayout.Separator();

			m_tooltipsFoldout = EditorGUILayout.Foldout( m_tooltipsFoldout, "Tooltips" );
			if( m_tooltipsFoldout )
			{
				EditorGUI.indentLevel++;
				for( int i = 0; i < m_tooltips.Count; i++ )
				{
					m_tooltips[ i ].Tooltip = EditorGUILayout.TextField( m_tooltips[ i ].Name, m_tooltips[ i ].Tooltip );
				}
				EditorGUI.indentLevel--;
			}

			EditorGUILayout.Separator();

			if( GUILayout.Button( "Build" ) )
			{
				System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
				string propertiesDecl = string.Empty;
				string propertiesSet = string.Empty;
				GetShaderInfoFromShaderAsset( ref propertiesDecl, ref propertiesSet );
				string template = PPSFullTemplate;
				template = template.Replace( PPSRendererClass, m_rendererClassName );
				template = template.Replace( PPSSettingsClass, m_settingsClassName );
				template = template.Replace( PPSEventType, m_eventType.ToString() );
				template = template.Replace( PPSPropertiesDecl, propertiesDecl );
				template = template.Replace( PPSPropertySet, propertiesSet );
				template = template.Replace( PPSMenuEntry, m_menuEntry );
				template = template.Replace( PPSAllowInSceneView, m_allowInSceneView?"true":"false" );
				template = template.Replace( PPSShader, m_currentShader.name );
				string path = m_folderPath + m_settingsClassName + ".cs";
				IOUtils.SaveTextfileToDisk( template, path, false );
				System.Threading.Thread.CurrentThread.CurrentCulture = System.Threading.Thread.CurrentThread.CurrentUICulture;
				AssetDatabase.Refresh();
			}

			EditorGUILayout.EndVertical();
			EditorGUILayout.EndScrollView();
			m_draggableArea.size = position.size;
			m_dragAndDropTool.TestDragAndDrop( m_draggableArea );
		}

		public void GetShaderInfoFromASE( ref string propertiesDecl, ref string propertiesSet )
		{
			List<PropertyNode> properties = UIUtils.CurrentWindow.OutsideGraph.PropertyNodes.NodesList;
			int propertyCount = properties.Count;
			for( int i = 0; i < propertyCount; i++ )
			{
				properties[ i ].GeneratePPSInfo( ref propertiesDecl, ref propertiesSet );
			}
		}

		public void GetShaderInfoFromShaderAsset( ref string propertiesDecl, ref string propertiesSet )
		{
			bool fetchInitialInfo = false;
			if( m_currentShader == null )
			{
				Material mat = Selection.activeObject as Material;
				if( mat != null )
				{
					m_currentShader = mat.shader;
				}
				else
				{
					m_currentShader = Selection.activeObject as Shader;
				}
				fetchInitialInfo = true;
			}

			if( m_currentShader != null )
			{
				if( fetchInitialInfo )
					GetInitialInfo( m_currentShader );

				if( m_dummyMaterial == null )
				{
					m_dummyMaterial = new Material( m_currentShader );
				}
				else
				{
					m_dummyMaterial.shader = m_currentShader;
				}

				int propertyCount = UnityEditor.ShaderUtil.GetPropertyCount( m_currentShader );
				//string allProperties = string.Empty;
				int validIds = 0;
				for( int i = 0; i < propertyCount; i++ )
				{
					UnityEditor.ShaderUtil.ShaderPropertyType type = UnityEditor.ShaderUtil.GetPropertyType( m_currentShader, i );
					string name = UnityEditor.ShaderUtil.GetPropertyName( m_currentShader, i );
					//string description = UnityEditor.ShaderUtil.GetPropertyDescription( m_currentShader, i );
					if( m_excludedProperties.ContainsKey( name ))
						continue;

					string defaultValue = string.Empty;
					bool nullPointerCheck = false;
					switch( type )
					{
						case UnityEditor.ShaderUtil.ShaderPropertyType.Color:
						{
							Color value = m_dummyMaterial.GetColor( name );
							defaultValue = string.Format( "value = new Color({0}f,{1}f,{2}f,{3}f)", value.r, value.g, value.b, value.a );
						}
						break;
						case UnityEditor.ShaderUtil.ShaderPropertyType.Vector:
						{
							Vector4 value = m_dummyMaterial.GetVector( name );
							defaultValue = string.Format( "value = new Vector4({0}f,{1}f,{2}f,{3}f)", value.x, value.y, value.z, value.w );
						}
						break;
						case UnityEditor.ShaderUtil.ShaderPropertyType.Float:
						{
							float value = m_dummyMaterial.GetFloat( name );
							defaultValue = "value = " + value + "f";
						}
						break;
						case UnityEditor.ShaderUtil.ShaderPropertyType.Range:
						{
							float value = m_dummyMaterial.GetFloat( name );
							defaultValue = "value = " + value + "f";
						}
						break;
						case UnityEditor.ShaderUtil.ShaderPropertyType.TexEnv:
						{
							nullPointerCheck = true;
						}
						break;
					}

					propertiesDecl += string.Format( PPSPropertyDecFormat, string.Empty, m_tooltips[ validIds ].Tooltip, ShaderPropertyToPPSType[ type ], name, defaultValue );
					propertiesSet += string.Format( nullPointerCheck ? PPSPropertySetNullPointerCheckFormat : PPSPropertySetFormat, ShaderPropertyToPPSSet[ type ], name );
					validIds++;
				}

			}
		}

		private void GetInitialInfo()
		{
			MasterNode masterNode = UIUtils.CurrentWindow.OutsideGraph.CurrentMasterNode;
			m_menuEntry = masterNode.ShaderName.Replace( "Hidden/", string.Empty ).Replace( ".shader", string.Empty );
			string name = m_menuEntry;
			m_rendererClassName = name + "PPSRenderer";
			m_settingsClassName = name + "PPSSettings";
			m_folderPath = "Assets/";
		}

		private void GetInitialInfo( Shader shader )
		{
			if( shader == null )
			{
				m_scrollPos = Vector2.zero;
				m_menuEntry = string.Empty;
				m_rendererClassName = "PPSRenderer";
				m_settingsClassName = "PPSSettings";
				m_folderPath = "Assets/";
				m_tooltips.Clear();
				return;
			}

			m_menuEntry = shader.name.Replace( "Hidden/", string.Empty ).Replace( ".shader", string.Empty );
			m_menuEntry = UIUtils.RemoveInvalidCharacters( m_menuEntry );
			string name = m_menuEntry.Replace( "/", string.Empty );
			m_rendererClassName = name + "PPSRenderer";
			m_settingsClassName = name + "PPSSettings";
			m_folderPath = AssetDatabase.GetAssetPath( shader );
			m_folderPath = m_folderPath.Replace( System.IO.Path.GetFileName( m_folderPath ), string.Empty );

			FetchTooltips();
		}

		public void OnValidObjectsDropped( UnityEngine.Object[] droppedObjs )
		{
			for( int objIdx = 0; objIdx < droppedObjs.Length; objIdx++ )
			{
				Material mat = droppedObjs[ objIdx ] as Material;
				if( mat != null )
				{
					m_currentShader = mat.shader;
					GetInitialInfo( mat.shader );
					return;
				}
				else
				{
					Shader shader = droppedObjs[ objIdx ] as Shader;
					if( shader != null )
					{
						m_currentShader = shader;
						GetInitialInfo( shader );
						return;
					}
				}
			}
		}

		private void OnEnable()
		{
			m_draggableArea = new Rect( 0, 0, 1, 1 );
			m_dragAndDropTool = new DragAndDropTool();
			m_dragAndDropTool.OnValidDropObjectEvt += OnValidObjectsDropped;

			if( m_contentStyle == null )
			{
				m_contentStyle = new GUIStyle( GUIStyle.none );
				m_contentStyle.margin = new RectOffset( 6, 4, 5, 5 );
			}

			m_pathButtonStyle = null;

			//GetInitialInfo();
		}

		private void OnDestroy()
		{
			if( m_dummyMaterial != null )
			{
				GameObject.DestroyImmediate( m_dummyMaterial );
				m_dummyMaterial = null;
			}

			m_dragAndDropTool.Destroy();
			m_dragAndDropTool = null;

			m_tooltips.Clear();
			m_tooltips = null;

			m_contentStyle = null;
			m_pathButtonStyle = null;
			m_currentShader = null;
		}
	}
}
