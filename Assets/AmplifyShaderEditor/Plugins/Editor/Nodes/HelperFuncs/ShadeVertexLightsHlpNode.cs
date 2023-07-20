// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using System;
using UnityEngine;
using UnityEditor;
namespace AmplifyShaderEditor
{
	[Serializable]
	[NodeAttributes( "Shade Vertex Lights", "Light", "Computes illumination from four per-vertex lights and ambient, given object space position & normal" )]
	public sealed class ShadeVertexLightsHlpNode : ParentNode
	{
		private const string HelperMessage = "Shade Vertex Lights node only outputs correct results on\nTemplate Vertex/Frag shaders with their LightMode set to Vertex.";
		private const string ShadeVertexLightFunc = "ShadeVertexLightsFull({0},{1},{2},{3})";
		private const string LightCount = "Light Count";
		private const string IsSpotlight = "Is Spotlight";
		private const int MinLightCount = 0;
		private const int MaxLightCount = 8;
		[SerializeField]
		private int m_lightCount = 4;

		[SerializeField]
		private bool m_enableSpotlight = false;

		private int _LightCountId;
		private int _IsSpotlightId;

		protected override void CommonInit( int uniqueId )
		{
			base.CommonInit( uniqueId );
			AddInputPort( WirePortDataType.FLOAT4, false, "Vertex Position" );
			AddInputPort( WirePortDataType.FLOAT3, false, "Vertex Normal" );
			AddOutputPort( WirePortDataType.FLOAT3, Constants.EmptyPortValue );
			m_useInternalPortData = true;
			//m_autoWrapProperties = true;
			m_textLabelWidth = 90;
			m_previewShaderGUID = "3b6075034a85ad047be2d31dd213fb4f";
		}

		public override void OnEnable()
		{
			base.OnEnable();
			_LightCountId = Shader.PropertyToID( "_LightCount" );
			_IsSpotlightId = Shader.PropertyToID( "_IsSpotlight" );
		}

		public override void DrawProperties()
		{
			base.DrawProperties();
			NodeUtils.DrawPropertyGroup( ref m_propertiesFoldout, Constants.ParameterLabelStr, DrawGeneralProperties );
			EditorGUILayout.HelpBox( HelperMessage, MessageType.Info );
		}

		public override void SetPreviewInputs()
		{
			base.SetPreviewInputs();
			PreviewMaterial.SetInt( _LightCountId, m_lightCount );
			PreviewMaterial.SetInt( _IsSpotlightId, ( m_enableSpotlight ? 1 : 0 ) );

		}

		void DrawGeneralProperties()
		{
			m_lightCount = EditorGUILayoutIntSlider( LightCount, m_lightCount, MinLightCount, MaxLightCount );
			m_enableSpotlight = EditorGUILayoutToggle( IsSpotlight, m_enableSpotlight );
		}

		public override string GenerateShaderForOutput( int outputId, ref MasterNodeDataCollector dataCollector, bool ignoreLocalvar )
		{
			if( dataCollector.MasterNodeCategory == AvailableShaderTypes.SurfaceShader )
				UIUtils.ShowMessage( UniqueId, HelperMessage, MessageSeverity.Warning );

			if( m_outputPorts[ 0 ].IsLocalValue( dataCollector.PortCategory ) )
				return m_outputPorts[ 0 ].LocalValue( dataCollector.PortCategory );

			dataCollector.AddToIncludes( UniqueId, Constants.UnityCgLibFuncs );

			string vertexPosition = m_inputPorts[ 0 ].GeneratePortInstructions( ref dataCollector );
			string vertexNormal = m_inputPorts[ 1 ].GeneratePortInstructions( ref dataCollector );

			string value = string.Format( ShadeVertexLightFunc, vertexPosition, vertexNormal, m_lightCount, m_enableSpotlight.ToString().ToLower() );

			RegisterLocalVariable( 0, value, ref dataCollector, "shadeVertexLight" + OutputId );

			return m_outputPorts[ 0 ].LocalValue( dataCollector.PortCategory );
		}

		public override void ReadFromString( ref string[] nodeParams )
		{
			base.ReadFromString( ref nodeParams );
			if( UIUtils.CurrentShaderVersion() > 14301 )
			{
				m_lightCount = Convert.ToInt32( GetCurrentParam( ref nodeParams ) );
				m_enableSpotlight = Convert.ToBoolean( GetCurrentParam( ref nodeParams ) );
			}
		}

		public override void WriteToString( ref string nodeInfo, ref string connectionsInfo )
		{
			base.WriteToString( ref nodeInfo, ref connectionsInfo );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_lightCount );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_enableSpotlight );
		}
	}
}
