// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace AmplifyShaderEditor
{
	[Serializable]
	public class TemplateInputData
	{
		public string PortName;
		public WirePortDataType DataType;
		public MasterNodePortCategory PortCategory;
		public int PortUniqueId;
		public int OrderId;
		public int TagGlobalStartIdx;
		public int TagLocalStartIdx;
		public string TagId;
		public string DefaultValue;
		public string LinkId;

		public TemplateInputData( int tagLocalStartIdx, int tagGlobalStartIdx, string tagId, string portName, string defaultValue, WirePortDataType dataType, MasterNodePortCategory portCategory, int portUniqueId, int orderId, string linkId )
		{
			DefaultValue = defaultValue;
			PortName = portName;
			DataType = dataType;
			PortCategory = portCategory;
			PortUniqueId = portUniqueId;
			OrderId = orderId;
			TagId = tagId;
			TagGlobalStartIdx = tagGlobalStartIdx;
			TagLocalStartIdx = tagLocalStartIdx;
			LinkId = linkId;
		}

		public TemplateInputData( TemplateInputData other )
		{
			DefaultValue = other.DefaultValue;
			PortName = other.PortName;
			DataType = other.DataType;
			PortCategory = other.PortCategory;
			PortUniqueId = other.PortUniqueId;
			OrderId = other.OrderId;
			TagId = other.TagId;
			TagGlobalStartIdx = other.TagGlobalStartIdx;
			LinkId = other.LinkId;
		}
	}



	[Serializable]
	public class TemplatePropertyContainer
	{
		[SerializeField]
		private List<TemplateProperty> m_propertyList = new List<TemplateProperty>();
		private Dictionary<string, TemplateProperty> m_propertyDict = new Dictionary<string, TemplateProperty>();


		public void AddId( TemplateProperty templateProperty )
		{
			BuildInfo();
			m_propertyList.Add( templateProperty );
			m_propertyDict.Add( templateProperty.Id, templateProperty );
		}

		public void AddId( string body, string ID, bool searchIndentation = true )
		{
			AddId( body, ID, searchIndentation, string.Empty );
		}

		public void AddId( string body, string ID, bool searchIndentation, string customIndentation )
		{
			BuildInfo();

			int propertyIndex = body.IndexOf( ID );
			if( propertyIndex > -1 )
			{
				if( searchIndentation )
				{
					int identationIndex = -1;
					for( int i = propertyIndex; i >= 0; i-- )
					{
						if( body[ i ] == TemplatesManager.TemplateNewLine )
						{
							identationIndex = i + 1;
							break;
						}

						if( i == 0 )
						{
							identationIndex = 0;
						}
					}
					if( identationIndex > -1 )
					{
						int length = propertyIndex - identationIndex;
						string indentation = ( length > 0 ) ? body.Substring( identationIndex, length ) : string.Empty;
						TemplateProperty templateProperty = new TemplateProperty( ID, indentation, false );
						m_propertyList.Add( templateProperty );
						m_propertyDict.Add( templateProperty.Id, templateProperty );
					}
					else
					{
						TemplateProperty templateProperty = new TemplateProperty( ID, string.Empty, false );
						m_propertyList.Add( templateProperty );
						m_propertyDict.Add( templateProperty.Id, templateProperty );
					}
				}
				else
				{
					TemplateProperty templateProperty = new TemplateProperty( ID, customIndentation, true );
					m_propertyList.Add( templateProperty );
					m_propertyDict.Add( templateProperty.Id, templateProperty );
				}
			}
		}


		public void AddId( string body, string ID, int propertyIndex, bool searchIndentation )
		{
			AddId( body, ID, propertyIndex, searchIndentation, string.Empty );
		}

		public void AddId( string body, string ID, int propertyIndex, bool searchIndentation, string customIndentation )
		{
			if( body == null || string.IsNullOrEmpty( body ) )
				return;

			BuildInfo();
			if( searchIndentation && propertyIndex > -1 && propertyIndex < body.Length )
			{
				int indentationIndex = -1;
				for( int i = propertyIndex; i > 0; i-- )
				{
					if( body[ i ] == TemplatesManager.TemplateNewLine )
					{
						indentationIndex = i + 1;
						break;
					}
				}

				if( indentationIndex > -1 )
				{
					int length = propertyIndex - indentationIndex;
					string indentation = ( length > 0 ) ? body.Substring( indentationIndex, length ) : string.Empty;
					TemplateProperty templateProperty = new TemplateProperty( ID, indentation, false );
					m_propertyList.Add( templateProperty );
					m_propertyDict.Add( templateProperty.Id, templateProperty );
				}
			}
			else
			{
				TemplateProperty templateProperty = new TemplateProperty( ID, customIndentation, true );
				m_propertyList.Add( templateProperty );
				m_propertyDict.Add( templateProperty.Id, templateProperty );
			}

		}
		public void BuildInfo()
		{
			if( m_propertyDict == null )
			{
				m_propertyDict = new Dictionary<string, TemplateProperty>();
			}

			if( m_propertyList.Count != m_propertyDict.Count )
			{
				m_propertyDict.Clear();
				for( int i = 0; i < m_propertyList.Count; i++ )
				{
					m_propertyDict.Add( m_propertyList[ i ].Id, m_propertyList[ i ] );
				}
			}
		}

		public void ResetTemplateUsageData()
		{
			BuildInfo();
			for( int i = 0; i < m_propertyList.Count; i++ )
			{
				m_propertyList[ i ].Used = false;
			}
		}

		public void Reset()
		{
			m_propertyList.Clear();
			m_propertyDict.Clear();
		}

		public void Destroy()
		{
			m_propertyList.Clear();
			m_propertyList = null;
			m_propertyDict.Clear();
			m_propertyDict = null;
		}


		public Dictionary<string, TemplateProperty> PropertyDict
		{
			get
			{
				BuildInfo();
				return m_propertyDict;
			}
		}
		public List<TemplateProperty> PropertyList { get { return m_propertyList; } }
	}

	[Serializable]
	public class TemplateProperty
	{
		public bool UseIndentationAtStart = false;
		public string Indentation;
		public bool UseCustomIndentation;
		public string Id;
		public bool AutoLineFeed;
		public bool Used;

		public TemplateProperty( string id, string indentation, bool useCustomIndentation )
		{
			Id = id;
			Indentation = indentation;
			UseCustomIndentation = useCustomIndentation;
			AutoLineFeed = !string.IsNullOrEmpty( indentation );
			Used = false;
		}
	}

	[Serializable]
	public class TemplateTessVControlTag
	{
		public string Id;
		public int StartIdx;

		public TemplateTessVControlTag()
		{
			StartIdx = -1;
		}

		public bool IsValid { get { return StartIdx >= 0; } }
	}

	[Serializable]
	public class TemplateTessControlData
	{
		public string Id;
		public int StartIdx;
		public string InVarType;
		public string InVarName;
		public string OutVarType;
		public string OutVarName;

		public bool IsValid { get { return StartIdx >= 0; } }

		public TemplateTessControlData()
		{
			StartIdx = -1;
		}

		public TemplateTessControlData( int startIdx, string id, string inVarInfo, string outVarInfo )
		{
			StartIdx = startIdx;
			Id = id;
			string[] inVarInfoArr = inVarInfo.Split( IOUtils.VALUE_SEPARATOR );
			if( inVarInfoArr.Length > 1 )
			{
				InVarType = inVarInfoArr[ 1 ];
				InVarName = inVarInfoArr[ 0 ];
			}

			string[] outVarInfoArr = outVarInfo.Split( IOUtils.VALUE_SEPARATOR );
			if( outVarInfoArr.Length > 1 )
			{
				OutVarType = outVarInfoArr[ 1 ];
				OutVarName = outVarInfoArr[ 0 ];
			}
		}

		public string[] GenerateControl( Dictionary<TemplateSemantics, TemplateVertexData> vertexData, List<string> inputList )
		{
			List<string> value = new List<string>();
			if( vertexData != null && vertexData.Count > 0 )
			{
				foreach( var item in vertexData )
				{
					if( inputList.FindIndex( x => { return x.Contains( item.Value.VarName ); } ) > -1 )
						value.Add( string.Format( "{0}.{1} = {2}.{1};", OutVarName, item.Value.VarName, InVarName ) );
				}
			}
			return value.ToArray();
		}
	}

	[Serializable]
	public class TemplateTessDomainData
	{
		public string Id;
		public int StartIdx;
		public string InVarType;
		public string InVarName;
		public string OutVarType;
		public string OutVarName;
		public string BaryVarType;
		public string BaryVarName;

		public bool IsValid { get { return StartIdx >= 0; } }

		public TemplateTessDomainData()
		{
			StartIdx = -1;
		}

		public TemplateTessDomainData( int startIdx, string id, string inVarInfo, string outVarInfo, string baryVarInfo )
		{
			StartIdx = startIdx;
			Id = id;
			string[] inVarInfoArr = inVarInfo.Split( IOUtils.VALUE_SEPARATOR );
			if( inVarInfoArr.Length > 1 )
			{
				InVarType = inVarInfoArr[ 1 ];
				InVarName = inVarInfoArr[ 0 ];
			}

			string[] outVarInfoArr = outVarInfo.Split( IOUtils.VALUE_SEPARATOR );
			if( outVarInfoArr.Length > 1 )
			{
				OutVarType = outVarInfoArr[ 1 ];
				OutVarName = outVarInfoArr[ 0 ];
			}

			string[] baryVarInfoArr = baryVarInfo.Split( IOUtils.VALUE_SEPARATOR );
			if( baryVarInfoArr.Length > 1 )
			{
				BaryVarType = baryVarInfoArr[ 1 ];
				BaryVarName = baryVarInfoArr[ 0 ];
			}
		}

		public string[] GenerateDomain( Dictionary<TemplateSemantics, TemplateVertexData> vertexData, List<string> inputList )
		{
			List<string> value = new List<string>();
			if( vertexData != null && vertexData.Count > 0 )
			{
				foreach( var item in vertexData )
				{
					//o.ase_normal = patch[0].ase_normal * bary.x + patch[1].ase_normal * bary.y + patch[2].ase_normal * bary.z;
					if( inputList.FindIndex( x => { return x.Contains( item.Value.VarName ); } ) > -1 )
						value.Add( string.Format( "{0}.{1} = {2}[0].{1} * {3}.x + {2}[1].{1} * {3}.y + {2}[2].{1} * {3}.z;", OutVarName, item.Value.VarName, InVarName, BaryVarName ) );
				}
			}
			return value.ToArray();
		}
	}

	[Serializable]
	public class TemplateFunctionData
	{
		public int MainBodyLocalIdx;
		public string MainBodyName;

		public string Id;
		public int Position;
		public string InVarType;
		public string InVarName;
		public string OutVarType;
		public string OutVarName;
		public MasterNodePortCategory Category;
		public TemplateFunctionData( int mainBodyLocalIdx, string mainBodyName, string id, int position, string inVarInfo, string outVarInfo, MasterNodePortCategory category )
		{
			MainBodyLocalIdx = mainBodyLocalIdx;
			MainBodyName = mainBodyName;
			Id = id;
			Position = position;
			{
				string[] inVarInfoArr = inVarInfo.Split( IOUtils.VALUE_SEPARATOR );
				if( inVarInfoArr.Length > 1 )
				{
					InVarType = inVarInfoArr[ 1 ];
					InVarName = inVarInfoArr[ 0 ];
				}
			}
			{
				string[] outVarInfoArr = outVarInfo.Split( IOUtils.VALUE_SEPARATOR );
				if( outVarInfoArr.Length > 1 )
				{
					OutVarType = outVarInfoArr[ 1 ];
					OutVarName = outVarInfoArr[ 0 ];
				}
			}
			Category = category;
		}
	}

	[Serializable]
	public class TemplateTagData
	{
		public int StartIdx = -1;
		public string Id;
		public bool SearchIndentation;
		public string CustomIndentation;


		public TemplateTagData( int startIdx, string id, bool searchIndentation )
		{
			StartIdx = startIdx;
			Id = id;
			SearchIndentation = searchIndentation;
			CustomIndentation = string.Empty;
		}

		public TemplateTagData( string id, bool searchIndentation )
		{
			Id = id;
			SearchIndentation = searchIndentation;
			CustomIndentation = string.Empty;
		}

		public TemplateTagData( string id, bool searchIndentation, string customIndentation )
		{
			Id = id;
			SearchIndentation = searchIndentation;
			CustomIndentation = customIndentation;
		}

		public bool IsValid { get { return StartIdx >= 0; } }
	}

	public enum TemplatePortIds
	{
		Name = 0,
		DataType,
		UniqueId,
		OrderId,
		Link
	}

	public enum TemplateCommonTagId
	{
		Property = 0,
		Global = 1,
		Function = 2,
		Tag = 3,
		Pragmas = 4,
		Pass = 5,
		Params_Vert = 6,
		Params_Frag = 7
		//CullMode	= 8,
		//BlendMode   = 9,
		//BlendOp		= 10,
		//ColorMask	= 11,
		//StencilOp	= 12
	}

	[Serializable]
	public class TemplatesManager : ScriptableObject
	{
		public static int MPShaderVersion = 14503;
		
		public static readonly string TemplateShaderNameBeginTag = "/*ase_name*/";
		public static readonly string TemplateStencilTag = "/*ase_stencil*/\n";
		public static readonly string TemplateRenderPlatformsTag = "/*ase_render_platforms*/";
		public static readonly string TemplateAllModulesTag = "/*ase_all_modules*/\n";
		public static readonly string TemplateMPSubShaderTag = "\\bSubShader\\b\\s*{";
		//public static readonly string TemplateMPPassTag = "^\\s*Pass\b\\s*{";//"\\bPass\\b\\s*{";
		public static readonly string TemplateMPPassTag = "\\bPass\\b\\s*{";
		public static readonly string TemplateLocalVarTag = "/*ase_local_var*/";
		public static readonly string TemplateDependenciesListTag = "/*ase_dependencies_list*/";
		public static readonly string TemplatePragmaBeforeTag = "/*ase_pragma_before*/";
		public static readonly string TemplatePragmaTag = "/*ase_pragma*/";
		public static readonly string TemplatePassTag = "/*ase_pass*/";
		public static readonly string TemplatePassesEndTag = "/*ase_pass_end*/";
		public static readonly string TemplateLODsTag = "/*ase_lod*/";
		//public static readonly string TemplatePassTagPattern = @"\s\/\*ase_pass\*\/";
		public static readonly string TemplatePassTagPattern = @"\s\/\*ase_pass[:\*]+";
		public static readonly string TemplatePropertyTag = "/*ase_props*/";
		public static readonly string TemplateGlobalsTag = "/*ase_globals*/";
		public static readonly string TemplateSRPBatcherTag = "/*ase_srp_batcher*/\n";
		public static readonly string TemplateInterpolatorBeginTag = "/*ase_interp(";
		public static readonly string TemplateVertexDataTag = "/*ase_vdata:";

		public static readonly string TemplateTessVControlTag = "/*ase_vcontrol*/";
		public static readonly string TemplateTessControlCodeArea = "/*ase_control_code:";
		public static readonly string TemplateTessDomainCodeArea = "/*ase_domain_code:";

		//public static readonly string TemplateExcludeFromGraphTag = "/*ase_hide_pass*/";
		public static readonly string TemplateMainPassTag = "/*ase_main_pass*/";

		public static readonly string TemplateFunctionsTag = "/*ase_funcs*/\n";
		//public static readonly string TemplateTagsTag = "/*ase_tags*/";

		//public static readonly string TemplateCullModeTag = "/*ase_cull_mode*/";
		//public static readonly string TemplateBlendModeTag = "/*ase_blend_mode*/";
		//public static readonly string TemplateBlendOpTag = "/*ase_blend_op*/";
		//public static readonly string TemplateColorMaskTag = "/*ase_color_mask*/";
		//public static readonly string TemplateStencilOpTag = "/*ase_stencil*/";

		public static readonly string TemplateCodeSnippetAttribBegin = "#CODE_SNIPPET_ATTRIBS_BEGIN#";
		public static readonly string TemplateCodeSnippetAttribEnd = "#CODE_SNIPPET_ATTRIBS_END#\n";
		public static readonly string TemplateCodeSnippetEnd = "#CODE_SNIPPET_END#\n";

		public static readonly char TemplateNewLine = '\n';

		// INPUTS AREA
		public static readonly string TemplateInputsVertBeginTag = "/*ase_vert_out:";
		public static readonly string TemplateInputsFragBeginTag = "/*ase_frag_out:";
		public static readonly string TemplateInputsVertParamsTag = "/*ase_vert_input*/";
		public static readonly string TemplateInputsFragParamsTag = "/*ase_frag_input*/";


		// CODE AREA
		public static readonly string TemplateVertexCodeBeginArea = "/*ase_vert_code:";
		public static readonly string TemplateFragmentCodeBeginArea = "/*ase_frag_code:";


		public static readonly string TemplateEndOfLine = "*/\n";
		public static readonly string TemplateEndSectionTag = "*/";
		public static readonly string TemplateFullEndTag = "/*end*/";

		public static readonly string NameFormatter = "\"{0}\"";

		public static readonly TemplateTagData[] CommonTags = { new TemplateTagData( TemplatePropertyTag,true),
																new TemplateTagData( TemplateGlobalsTag,true),
																new TemplateTagData( TemplateSRPBatcherTag,true),
																new TemplateTagData( TemplateFunctionsTag,true),
																//new TemplateTagData( TemplateTagsTag,false," "),
																new TemplateTagData( TemplatePragmaBeforeTag,true),
																new TemplateTagData( TemplatePragmaTag,true),
																new TemplateTagData( TemplatePassTag,true),
																new TemplateTagData( TemplateInputsVertParamsTag,false),
																new TemplateTagData( TemplateInputsFragParamsTag,false),
																new TemplateTagData( TemplateLODsTag,true)
																//new TemplateTagData( TemplateCullModeTag,false),
																//new TemplateTagData( TemplateBlendModeTag,false),
																//new TemplateTagData( TemplateBlendOpTag,false),
																//new TemplateTagData( TemplateColorMaskTag,false),
																//new TemplateTagData( TemplateStencilOpTag,true),
																};
		public static string URPLitGUID = "94348b07e5e8bab40bd6c8a1e3df54cd";
		public static string URPUnlitGUID = "2992e84f91cbeb14eab234972e07ea9d";

		public static string HDRPLitGUID = "53b46d85872c5b24c8f4f0a1c3fe4c87";
		public static string HDRPUnlitGUID = "7f5cb9c3ea6481f469fdd856555439ef";

		public static Dictionary<string, string> DeprecatedTemplates = new Dictionary<string, string>()
		{
		};

		public static Dictionary<string, string> OfficialTemplates = new Dictionary<string, string>()
		{
			{ "6ce779933eb99f049b78d6163735e06f","Custom Render Texture/Initialize"},
			{ "32120270d1b3a8746af2aca8bc749736","Custom Render Texture/Update"},

			{ "5056123faa0c79b47ab6ad7e8bf059a4","UI/Default" },

			{ "ed95fe726fd7b4644bb42f4d1ddd2bcd","Legacy/Lit"},
			{ "0770190933193b94aaa3065e307002fa","Legacy/Unlit"},
			{ "899e609c083c74c4ca567477c39edef0","Legacy/Unlit Lightmap" },
			{ "e1de45c0d41f68c41b2cc20c8b9c05ef","Legacy/Multi Pass Unlit" },

			{ "32139be9c1eb75640a847f011acf3bcf","Legacy/Post-Processing Stack"},
			{ "c71b220b631b6344493ea3cf87110c93","Legacy/Image Effect" },

			{ "0f8ba0101102bb14ebf021ddadce9b49","Legacy/Default Sprites" },
			{ "0b6a9f8b4f707c74ca64c0be8e590de0","Legacy/Particles Alpha Blended" },
			

			{ URPLitGUID,"Universal/Lit"},
			{ URPUnlitGUID,"Universal/Unlit"},

			{ HDRPLitGUID,"HDRP/Lit"},
			{ HDRPUnlitGUID,"HDRP/Unlit"},
		};

		public static readonly string TemplateMenuItemsFileGUID = "da0b931bd234a1e43b65f684d4b59bfb";

		private Dictionary<string, TemplateDataParent> m_availableTemplates = new Dictionary<string, TemplateDataParent>();

		[SerializeField]
		private List<TemplateDataParent> m_sortedTemplates = new List<TemplateDataParent>();

		[SerializeField]
		public string[] AvailableTemplateNames;

		[SerializeField]
		public bool Initialized = false;

		private Dictionary<string, bool> m_optionsInitialSetup = new Dictionary<string, bool>();

		public static string CurrTemplateGUIDLoaded = string.Empty;

		public static bool IsTestTemplate { get { return CurrTemplateGUIDLoaded.Equals( "a95a019bbc760714bb8228af04c291d1" ); } }
		public static bool ShowDebugMessages = false;
		public void RefreshAvailableTemplates()
		{
			if( m_availableTemplates.Count != m_sortedTemplates.Count )
			{
				m_availableTemplates.Clear();
				int count = m_sortedTemplates.Count;
				for( int i = 0; i < count; i++ )
				{
					m_availableTemplates.Add( m_sortedTemplates[ i ].GUID, m_sortedTemplates[ i ] );
				}
			}
		}

		struct TemplateDescriptor
		{
			public TemplateDataParent template;
			public string name;
			public string guid;
			public string path;
			public bool isCommunity;
		}

		public void Init()
		{
			if( !Initialized )
			{
				if( ShowDebugMessages )
					Debug.Log( "Initialize" );

				string templateMenuItems = IOUtils.LoadTextFileFromDisk( AssetDatabase.GUIDToAssetPath( TemplateMenuItemsFileGUID ) );
				bool refreshTemplateMenuItems = false;

				string[] allShaders = AssetDatabase.FindAssets( "t:shader" );
				var templates = new Dictionary<string,TemplateDescriptor>();				
				
				// Add official templates first
				foreach ( KeyValuePair<string, string> kvp in OfficialTemplates )
				{
					string guid = kvp.Key;
					string path = AssetDatabase.GUIDToAssetPath( guid );
					if ( !string.IsNullOrEmpty( path ) && !templates.ContainsKey( guid ) )
					{												
						var desc = new TemplateDescriptor();
						desc.template = ScriptableObject.CreateInstance<TemplateMultiPass>();
						desc.name = kvp.Value;
						desc.guid = guid;
						desc.path = path;
						desc.isCommunity = false;
						templates.Add( desc.guid, desc );
					}
				}

				// Search for other possible templates on the project
				var candidates = new List<KeyValuePair<string, string>>( allShaders.Length );
				var candidateBag = new ConcurrentBag<string>();

				for ( int i = 0; i < allShaders.Length; i++ )
				{
					if ( !templates.ContainsKey( allShaders[ i ] ) )
					{
						candidates.Add( new KeyValuePair<string, string>( allShaders[ i ], AssetDatabase.GUIDToAssetPath( allShaders[ i ] ) ) );
					}
				}

				Parallel.For( 0, candidates.Count, i =>				
				{
					string body = File.ReadAllText( candidates[ i ].Value ); ;
					if ( body.IndexOf( TemplatesManager.TemplateShaderNameBeginTag ) > -1 )
					{
						candidateBag.Add( candidates[ i ].Key );
					}						
				} );
				
				foreach ( var guid in candidateBag )
				{
					TemplateDataParent template = GetTemplate( guid );
					if ( template == null && !templates.ContainsKey( guid ) )
					{
						var desc = new TemplateDescriptor();
						desc.template = ScriptableObject.CreateInstance<TemplateMultiPass>();
						desc.name = string.Empty;
						desc.guid = guid;
						desc.path = AssetDatabase.GUIDToAssetPath( guid );
						desc.isCommunity = true;
						templates.Add( desc.guid, desc );					
					}				
				}

				var templateList = templates.Values.ToArray();
				Parallel.For( 0, templateList.Length, i =>				
				{
					TemplateDescriptor desc = templateList[ i ];
					desc.template.Init( desc.name, desc.guid, desc.path, desc.isCommunity );
				} );
				
				foreach ( var pair in templates )
				{
					TemplateDescriptor desc = pair.Value;
					
					if ( desc.template.IsValid )
					{
						AddTemplate( desc.template );
					}					
				
					if ( !desc.isCommunity && !refreshTemplateMenuItems && templateMenuItems.IndexOf( name ) < 0 )
					{
						refreshTemplateMenuItems = true;
					}
				}

				AvailableTemplateNames = new string[ m_sortedTemplates.Count + 1 ];
				AvailableTemplateNames[ 0 ] = "Custom";
				for( int i = 0; i < m_sortedTemplates.Count; i++ )
				{
					m_sortedTemplates[ i ].OrderId = i;
					AvailableTemplateNames[ i + 1 ] = m_sortedTemplates[ i ].Name;
				}

				if( refreshTemplateMenuItems )
					CreateTemplateMenuItems();

				Initialized = true;
			}
		}

		//[MenuItem( "Window/Amplify Shader Editor/Create Menu Items", false, 1000 )]
		//public static void ForceCreateTemplateMenuItems()
		//{
		//	UIUtils.CurrentWindow.TemplatesManagerInstance.CreateTemplateMenuItems();
		//}

		public void CreateTemplateMenuItems()
		{
			if( m_sortedTemplates == null || m_sortedTemplates.Count == 0 )
				return;

			// change names for duplicates
			for( int i = 0; i < m_sortedTemplates.Count; i++ )
			{
				for( int j = 0; j < i; j++ )
				{
					if( m_sortedTemplates[ i ].Name == m_sortedTemplates[ j ].Name )
					{
						var match = Regex.Match( m_sortedTemplates[ i ].Name, @"^.*?(\d+(?:[.,]\d+)?)\s*$" );
						if( match.Success )
						{
							string strNumber = match.Groups[ 1 ].Value;
							int number = int.Parse( strNumber ) + 1;
							string firstPart = m_sortedTemplates[ i ].Name.Substring( 0, match.Groups[ 1 ].Index );
							string secondPart = m_sortedTemplates[ i ].Name.Substring( match.Groups[ 1 ].Index + strNumber.Length );
							m_sortedTemplates[ i ].Name = firstPart + number + secondPart;
						}
						else
						{
							m_sortedTemplates[ i ].Name += " 1";
						}
					}
				}
			}

			// Sort templates by name
			var sorted = new SortedDictionary<string, string>();
			for ( int i = 0; i < m_sortedTemplates.Count; i++ )
			{
				sorted.Add( m_sortedTemplates[ i ].Name, m_sortedTemplates[ i ].GUID );
			}			

			System.Text.StringBuilder fileContents = new System.Text.StringBuilder();
			fileContents.Append( "// Amplify Shader Editor - Visual Shader Editing Tool\n" );
			fileContents.Append( "// Copyright (c) Amplify Creations, Lda <info@amplify.pt>\n" );
			fileContents.Append( "using UnityEditor;\n" );
			fileContents.Append( "\n" );
			fileContents.Append( "namespace AmplifyShaderEditor\n" );
			fileContents.Append( "{\n" );
			fileContents.Append( "\tpublic class TemplateMenuItems\n" );
			fileContents.Append( "\t{\n" );
			int fixedPriority = 85;
			foreach ( var pair in sorted )
			{
				fileContents.AppendFormat( "\t\t[MenuItem( \"Assets/Create/Amplify Shader/{0}\", false, {1} )]\n", pair.Key, fixedPriority );
				string itemName = UIUtils.RemoveInvalidCharacters( pair.Key );
				fileContents.AppendFormat( "\t\tpublic static void ApplyTemplate{0}()\n", itemName/*i*/ );
				fileContents.Append( "\t\t{\n" );
				//fileContents.AppendFormat( "\t\t\tAmplifyShaderEditorWindow.CreateNewTemplateShader( \"{0}\" );\n", m_sortedTemplates[ i ].GUID );
				fileContents.AppendFormat( "\t\t\tAmplifyShaderEditorWindow.CreateConfirmationTemplateShader( \"{0}\" );\n", pair.Value );
				fileContents.Append( "\t\t}\n" );
			}
			fileContents.Append( "\t}\n" );
			fileContents.Append( "}\n" );
			string filePath = AssetDatabase.GUIDToAssetPath( TemplateMenuItemsFileGUID );
			IOUtils.SaveTextfileToDisk( fileContents.ToString(), filePath, false );
			m_filepath = filePath;
			//AssetDatabase.ImportAsset( filePath );
		}

		string m_filepath = string.Empty;

		public void ReimportMenuItems()
		{
			if( !string.IsNullOrEmpty( m_filepath ) )
			{
				AssetDatabase.ImportAsset( m_filepath );
				m_filepath = string.Empty;
			}
		}

		public int GetIdForTemplate( TemplateData templateData )
		{
			if( templateData == null )
				return -1;

			for( int i = 0; i < m_sortedTemplates.Count; i++ )
			{
				if( m_sortedTemplates[ i ].GUID.Equals( templateData.GUID ) )
					return m_sortedTemplates[ i ].OrderId;
			}
			return -1;
		}



		public void AddTemplate( TemplateDataParent templateData )
		{
			if( templateData == null || !templateData.IsValid )
				return;
			RefreshAvailableTemplates();
			if( !m_availableTemplates.ContainsKey( templateData.GUID ) )
			{
				m_sortedTemplates.Add( templateData );
				m_availableTemplates.Add( templateData.GUID, templateData );
			}
		}

		public void RemoveTemplate( string guid )
		{
			TemplateDataParent templateData = GetTemplate( guid );
			if( templateData != null )
			{
				RemoveTemplate( templateData );
			}
		}

		public void RemoveTemplate( TemplateDataParent templateData )
		{
			RefreshAvailableTemplates();

			if( m_availableTemplates != null )
				m_availableTemplates.Remove( templateData.GUID );

			m_sortedTemplates.Remove( templateData );
			templateData.Destroy();
		}

		public void Destroy()
		{
			if( TemplatesManager.ShowDebugMessages )
				Debug.Log( "Destroy Manager" );
			if( m_availableTemplates != null )
			{
				foreach( KeyValuePair<string, TemplateDataParent> kvp in m_availableTemplates )
				{
					kvp.Value.Destroy();
				}
				m_availableTemplates.Clear();
				m_availableTemplates = null;
			}
			int count = m_sortedTemplates.Count;

			for( int i = 0; i < count; i++ )
			{
				ScriptableObject.DestroyImmediate( m_sortedTemplates[ i ] );
			}

			m_sortedTemplates.Clear();
			m_sortedTemplates = null;

			AvailableTemplateNames = null;
			Initialized = false;
		}

		public TemplateDataParent GetTemplate( int id )
		{
			if( id < m_sortedTemplates.Count )
				return m_sortedTemplates[ id ];

			return null;
		}

		public TemplateDataParent GetTemplate( string guid )
		{
			RefreshAvailableTemplates();
			if( m_availableTemplates == null && m_sortedTemplates != null )
			{
				m_availableTemplates = new Dictionary<string, TemplateDataParent>();
				for( int i = 0; i < m_sortedTemplates.Count; i++ )
				{
					m_availableTemplates.Add( m_sortedTemplates[ i ].GUID, m_sortedTemplates[ i ] );
				}
			}

			if( m_availableTemplates.ContainsKey( guid ) )
				return m_availableTemplates[ guid ];

			return null;
		}


		public TemplateDataParent GetTemplateByName( string name )
		{
			RefreshAvailableTemplates();
			if( m_availableTemplates == null && m_sortedTemplates != null )
			{
				m_availableTemplates = new Dictionary<string, TemplateDataParent>();
				for( int i = 0; i < m_sortedTemplates.Count; i++ )
				{
					m_availableTemplates.Add( m_sortedTemplates[ i ].GUID, m_sortedTemplates[ i ] );
				}
			}

			foreach( KeyValuePair<string, TemplateDataParent> kvp in m_availableTemplates )
			{
				if( kvp.Value.DefaultShaderName.Equals( name ) )
				{
					return kvp.Value;
				}
			}
			return null;
		}

		public TemplateDataParent CheckAndLoadTemplate( string guid )
		{
			TemplateDataParent templateData = GetTemplate( guid );
			if( templateData == null )
			{
				string datapath = AssetDatabase.GUIDToAssetPath( guid );
				string body = IOUtils.LoadTextFileFromDisk( datapath );

				if( body.IndexOf( TemplatesManager.TemplateShaderNameBeginTag ) > -1 )
				{
					templateData = ScriptableObject.CreateInstance<TemplateMultiPass>();
					templateData.Init( string.Empty, guid, datapath, true );
					if( templateData.IsValid )
					{
						AddTemplate( templateData );
						return templateData;
					}
				}
			}

			return null;
		}

		private void OnEnable()
		{
			if( !Initialized )
			{
				Init();
			}
			else
			{
				RefreshAvailableTemplates();
			}
			hideFlags = HideFlags.HideAndDontSave;
			if( ShowDebugMessages )
				Debug.Log( "On Enable Manager: " + this.GetInstanceID() );
		}

		public void ResetOptionsSetupData()
		{
			if( ShowDebugMessages )
			Debug.Log( "Reseting options setup data" );
			m_optionsInitialSetup.Clear();
		}

		public bool SetOptionsValue( string optionId, bool value )
		{
			if( m_optionsInitialSetup.ContainsKey( optionId ) )
			{
				m_optionsInitialSetup[ optionId ] = m_optionsInitialSetup[ optionId ] || value;
			}
			else
			{
				m_optionsInitialSetup.Add( optionId, value );
			}
			return m_optionsInitialSetup[ optionId ];
		}

		public bool CheckIfDeprecated( string guid , out string newGUID )
		{
			if( DeprecatedTemplates.ContainsKey( guid ) )
			{
				UIUtils.ShowMessage( "Shader using deprecated template which no longer exists on ASE. Pointing to new correct one, options and connections to master node were reset." );
				newGUID =  DeprecatedTemplates[ guid ];
				return true;
			}
			newGUID = string.Empty;
			return false;
		}

		public int TemplateCount { get { return m_sortedTemplates.Count; } }
	}
}
