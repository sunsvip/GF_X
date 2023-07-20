// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace AmplifyShaderEditor
{
	[Serializable]
	[NodeAttributes( "Sampler State", "Textures", "Creates a custom sampler state or returns the default one of a selected texture object" )]
	public class SamplerStateNode : ParentNode
	{
		private readonly string[] Dummy = { string.Empty };
		private const string WrapModeStr = "Wrap Mode";
		private const string UAxisStr = "U axis";
		private const string VAxisStr = "V axis";
		private const string FilterModeStr = "Filter Mode";
		private const string AnisotropicFilteringStr = "Aniso. Filtering";
		private const string MessageMacrosOFF = "Sampling Macros option is turned OFF, this node will not generate any sampler state";
		private const string MessageTextureObject = "Only Texture Objects that are actually being sampled within the shader generate valid sampler states.\n\nPlease make sure the referenced Texture Object is being sampled otherwise the shader will fail to compile.";
		private const string MessageUnitSuppport = "Unity support for sampler states in versions below Unity 2018.1 is limited.\n\nNotably, only vertex/frag shaders support it and not surfaces shaders and sampler states can only be reused and not created if the version is below 2017.1";

		[SerializeField]
		protected int m_wrapMode = 0;

		[SerializeField]
		protected TextureWrapMode m_wrapModeU = TextureWrapMode.Repeat;

		[SerializeField]
		protected TextureWrapMode m_wrapModeV = TextureWrapMode.Repeat;

		[SerializeField]
		protected FilterMode m_filterMode = FilterMode.Bilinear;


		public enum AnisoModes
		{
			None,
			X2,
			X4,
			X8,
			X16
		}

		[SerializeField]
		protected AnisoModes m_anisoMode = AnisoModes.None;

		[SerializeField]
		private int m_referenceSamplerId = -1;

		[SerializeField]
		private int m_referenceNodeId = -1;

		[SerializeField]
		private TexturePropertyNode m_inputReferenceNode = null;

		private TexturePropertyNode m_referenceNode = null;

		private UpperLeftWidgetHelper m_upperLeftWidget = new UpperLeftWidgetHelper();

		private InputPort m_texPort;

		private readonly string[] m_wrapModeStr = {
			"Repeat",
			"Clamp", 
			"Mirror",
			"Mirror Once",
			"Per-axis" 
		};

		protected override void CommonInit( int uniqueId )
		{
			base.CommonInit( uniqueId );
			AddInputPort( WirePortDataType.SAMPLER2D, false, "Tex" );
			m_inputPorts[ 0 ].CreatePortRestrictions( WirePortDataType.SAMPLER1D, WirePortDataType.SAMPLER2D, WirePortDataType.SAMPLER3D, WirePortDataType.SAMPLERCUBE, WirePortDataType.SAMPLER2DARRAY );
			AddOutputPort( WirePortDataType.SAMPLERSTATE, "Out" );
			m_texPort = m_inputPorts[ 0 ];
			m_hasLeftDropdown = true;
			m_autoWrapProperties = true;
			m_errorMessageTypeIsError = NodeMessageType.Warning;
			m_errorMessageTooltip = MessageMacrosOFF;
		}

		public override void OnInputPortConnected( int portId, int otherNodeId, int otherPortId, bool activateNode = true )
		{
			base.OnInputPortConnected( portId, otherNodeId, otherPortId, activateNode );
			m_inputPorts[ 0 ].MatchPortToConnection();
			m_inputReferenceNode = m_inputPorts[ 0 ].GetOutputNodeWhichIsNotRelay() as TexturePropertyNode;
			UpdateTitle();

		}

		public override void OnInputPortDisconnected( int portId )
		{
			base.OnInputPortDisconnected( portId );
			m_inputReferenceNode = null;
			UpdateTitle();
		}

		public override void OnConnectedOutputNodeChanges( int outputPortId, int otherNodeId, int otherPortId, string name, WirePortDataType type )
		{
			base.OnConnectedOutputNodeChanges( outputPortId, otherNodeId, otherPortId, name, type );
			m_inputPorts[ 0 ].MatchPortToConnection();
			UpdateTitle();
		}

		void UpdateTitle()
		{
			if( m_inputReferenceNode != null )
			{
				m_additionalContent.text = string.Format( Constants.PropertyValueLabel, m_inputReferenceNode.PropertyInspectorName );
			}
			else if( m_referenceSamplerId > -1 && m_referenceNode != null )
			{
				m_additionalContent.text = string.Format( Constants.PropertyValueLabel, m_referenceNode.PropertyInspectorName );
			}
			else
			{
				m_additionalContent.text = string.Empty;
			}
			m_sizeIsDirty = true;
		}

		public override void DrawProperties()
		{
			base.DrawProperties();
			bool guiEnabledBuffer = GUI.enabled;
			EditorGUI.BeginChangeCheck();
			List<string> arr = new List<string>( UIUtils.TexturePropertyNodeArr() );

			if( arr != null && arr.Count > 0 )
			{
				arr.Insert( 0, "None" );
				GUI.enabled = true && ( !m_inputPorts[ 0 ].IsConnected );
				m_referenceSamplerId = EditorGUILayoutPopup( Constants.AvailableReferenceStr, m_referenceSamplerId + 1, arr.ToArray() ) - 1;
			}
			else
			{
				m_referenceSamplerId = -1;
				GUI.enabled = false;
				EditorGUILayoutPopup( Constants.AvailableReferenceStr, m_referenceSamplerId, Dummy );
			}

			GUI.enabled = guiEnabledBuffer;
			if( EditorGUI.EndChangeCheck() )
			{
				m_referenceNode = UIUtils.GetTexturePropertyNode( m_referenceSamplerId );
				if( m_referenceNode != null )
				{
					m_referenceNodeId = m_referenceNode.UniqueId;
				}
				else
				{
					m_referenceNodeId = -1;
					m_referenceSamplerId = -1;
				}
				UpdateTitle();
			}

			EditorGUI.BeginDisabledGroup( m_texPort.IsConnected || m_referenceNodeId >= 0 );
			EditorGUI.BeginChangeCheck();
			m_wrapMode = EditorGUILayoutPopup( WrapModeStr, m_wrapMode, m_wrapModeStr );
			if( EditorGUI.EndChangeCheck() )
			{
				switch( m_wrapMode )
				{
					case 0:
					m_wrapModeU = TextureWrapMode.Repeat;
					m_wrapModeV = TextureWrapMode.Repeat;
					break;
					case 1:
					m_wrapModeU = TextureWrapMode.Clamp;
					m_wrapModeV = TextureWrapMode.Clamp;
					break;
					case 2:
					m_wrapModeU = TextureWrapMode.Mirror;
					m_wrapModeV = TextureWrapMode.Mirror;
					break;
					case 3:
					m_wrapModeU = TextureWrapMode.MirrorOnce;
					m_wrapModeV = TextureWrapMode.MirrorOnce;
					break;
				}
			}

			if( m_wrapMode == 4 )
			{
				EditorGUI.indentLevel++;
				m_wrapModeU = (TextureWrapMode)EditorGUILayoutEnumPopup( UAxisStr, m_wrapModeU );
				m_wrapModeV = (TextureWrapMode)EditorGUILayoutEnumPopup( VAxisStr, m_wrapModeV );
				EditorGUI.indentLevel--;
			}

			m_filterMode = (FilterMode)EditorGUILayoutEnumPopup( FilterModeStr, m_filterMode );

#if UNITY_2021_2_OR_NEWER
			m_anisoMode = (AnisoModes)EditorGUILayoutEnumPopup( AnisotropicFilteringStr , m_anisoMode );
#endif
			EditorGUI.EndDisabledGroup();

			if( !UIUtils.CurrentWindow.OutsideGraph.SamplingMacros )
				EditorGUILayout.HelpBox( MessageMacrosOFF, MessageType.Warning );

			if( m_texPort.IsConnected || m_referenceNodeId >= 0 )
				EditorGUILayout.HelpBox( MessageTextureObject, MessageType.Info );
		}

		public override void OnNodeLogicUpdate( DrawInfo drawInfo )
		{
			base.OnNodeLogicUpdate( drawInfo );
			if( !UIUtils.CurrentWindow.OutsideGraph.SamplingMacros && ContainerGraph.CurrentShaderFunction == null )
				m_showErrorMessage = true;
			else
				m_showErrorMessage = false;
		}

		public override void Draw( DrawInfo drawInfo )
		{
			base.Draw( drawInfo );
			EditorGUI.BeginChangeCheck();
			{
				List<string> arr = new List<string>( UIUtils.TexturePropertyNodeArr() );
				bool guiEnabledBuffer = GUI.enabled;

				if( arr != null && arr.Count > 0 )
				{
					arr.Insert( 0, "None" );
					GUI.enabled = true && ( !m_inputPorts[ 0 ].IsConnected );
					m_referenceSamplerId = m_upperLeftWidget.DrawWidget( this, m_referenceSamplerId + 1, arr.ToArray() ) - 1;
				}
				else
				{
					m_referenceSamplerId = -1;
					GUI.enabled = false;
					m_upperLeftWidget.DrawWidget( this, m_referenceSamplerId, Dummy );
				}
				GUI.enabled = guiEnabledBuffer;
			}
			if( EditorGUI.EndChangeCheck() )
			{
				m_referenceNode = UIUtils.GetTexturePropertyNode( m_referenceSamplerId );
				if( m_referenceNode != null )
				{
					m_referenceNodeId = m_referenceNode.UniqueId;
				}
				else
				{
					m_referenceNodeId = -1;
					m_referenceSamplerId = -1;
				}
				UpdateTitle();
			}
		}

		public string GenerateSamplerAttributes()
		{
			string result = string.Empty;
			switch( m_filterMode )
			{
				case FilterMode.Point:
				result += "_Point";
				break;
				default:
				case FilterMode.Bilinear:
				result += "_Linear";
				break;
				case FilterMode.Trilinear:
				result += "_Trilinear";
				break;
			}

			int finalWrap = m_wrapModeU == m_wrapModeV ? (int)m_wrapModeU : m_wrapMode;
			switch( finalWrap )
			{
				case 0:
				default:
				result += "_Repeat";
				break;
				case 1:
				result += "_Clamp";
				break;
				case 2:
				result += "_Mirror";
				break;
				case 3:
				result += "_MirrorOnce";
				break;
				case 4:
				{
					switch( m_wrapModeU )
					{
						default:
						case TextureWrapMode.Repeat:
						result += "_RepeatU";
						break;
						case TextureWrapMode.Clamp:
						result += "_ClampU";
						break;
						case TextureWrapMode.Mirror:
						result += "_MirrorU";
						break;
						case TextureWrapMode.MirrorOnce:
						result += "_MirrorOnceU";
						break;
					}
					switch( m_wrapModeV )
					{
						default:
						case TextureWrapMode.Repeat:
						result += "_RepeatV";
						break;
						case TextureWrapMode.Clamp:
						result += "_ClampV";
						break;
						case TextureWrapMode.Mirror:
						result += "_MirrorV";
						break;
						case TextureWrapMode.MirrorOnce:
						result += "_MirrorOnceV";
						break;
					}
				}
				break;
			}
#if UNITY_2021_2_OR_NEWER
			switch( m_anisoMode )
			{
				default:
				case AnisoModes.None:break;
				case AnisoModes.X2:	result += "_Aniso2";break;
				case AnisoModes.X4: result += "_Aniso4"; break;
				case AnisoModes.X8: result += "_Aniso8"; break;
				case AnisoModes.X16: result += "_Aniso16"; break;
			}
#endif

			return result;
		}

		public override string GenerateShaderForOutput( int outputId, ref MasterNodeDataCollector dataCollector, bool ignoreLocalvar )
		{
			if( !m_outputPorts[ 0 ].IsLocalValue( dataCollector.PortCategory ) )
			{
				string propertyOrOptions = string.Empty;
				if( m_texPort.IsConnected )
				{
					propertyOrOptions = m_texPort.GeneratePortInstructions( ref dataCollector );
				}
				else if( m_referenceNode != null )
				{
					m_referenceNode.BaseGenerateShaderForOutput( outputId, ref dataCollector, ignoreLocalvar );
					propertyOrOptions = m_referenceNode.PropertyName;
				}
				else
				{
					propertyOrOptions = GenerateSamplerAttributes();
				}

				string sampler = GeneratorUtils.GenerateSamplerState( ref dataCollector, UniqueId, propertyOrOptions , VariableMode.Create );

				m_outputPorts[ 0 ].SetLocalValue( sampler, dataCollector.PortCategory );
			}

			return m_outputPorts[ outputId ].LocalValue( dataCollector.PortCategory );
		}

		public override void RefreshExternalReferences()
		{
			base.RefreshExternalReferences();
			m_referenceNode = UIUtils.GetNode( m_referenceNodeId ) as TexturePropertyNode;
			m_referenceSamplerId = UIUtils.GetTexturePropertyNodeRegisterId( m_referenceNodeId );
			UpdateTitle();
		}

		public override void ReadFromString( ref string[] nodeParams )
		{
			base.ReadFromString( ref nodeParams );
			m_wrapMode = Convert.ToInt32( GetCurrentParam( ref nodeParams ) );
			m_wrapModeU = (TextureWrapMode)Convert.ToInt32( GetCurrentParam( ref nodeParams ) );
			m_wrapModeV = (TextureWrapMode)Convert.ToInt32( GetCurrentParam( ref nodeParams ) );
			m_filterMode = (FilterMode)Convert.ToInt32( GetCurrentParam( ref nodeParams ) );
			m_referenceNodeId = Convert.ToInt32( GetCurrentParam( ref nodeParams ) );
			if( UIUtils.CurrentShaderVersion() > 18926 )
			{
				m_anisoMode = (AnisoModes)Enum.Parse( typeof( AnisoModes ) , GetCurrentParam( ref nodeParams ) );
			}
		}

		public override void WriteToString( ref string nodeInfo, ref string connectionsInfo )
		{
			base.WriteToString( ref nodeInfo, ref connectionsInfo );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_wrapMode );
			IOUtils.AddFieldValueToString( ref nodeInfo, (int)m_wrapModeU );
			IOUtils.AddFieldValueToString( ref nodeInfo, (int)m_wrapModeV );
			IOUtils.AddFieldValueToString( ref nodeInfo, (int)m_filterMode );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_referenceNodeId );
			IOUtils.AddFieldValueToString( ref nodeInfo , m_anisoMode );
		}
	}
}
