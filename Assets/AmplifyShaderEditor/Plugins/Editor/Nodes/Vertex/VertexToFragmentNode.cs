// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>
//
// Custom Node Vertex To Fragment
// Donated by Jason Booth - http://u3d.as/DND

using UnityEngine;
using UnityEditor;
using System;

namespace AmplifyShaderEditor
{
	[System.Serializable]
	[NodeAttributes( "Vertex To Fragment", "Miscellaneous", "Pass vertex data to the pixel shader", null, KeyCode.None, true, false, null, null, "Jason Booth - http://u3d.as/DND" )]
	public sealed class VertexToFragmentNode : SingleInputOp
	{
		private const string DisabledInterpolatorMsg = "No Interpolation option cannot be used over Standard Surface type as we must be able to directly control interpolators registry, which does't happen over this shader type. Please disable it.";
		private const string NoInterpolationUsageMsg = "No interpolation is performed when passing value from vertex to fragment during rasterization. Please note this option will not work across all API's and can even throw compilation errors on some of them ( p.e. Metal and GLES 2.0 )";

		private const string SampleInfoMessage = "Interpolate at sample location rather than at the pixel center. This causes the pixel shader to execute per-sample rather than per-pixel. Only available in shader model 4.1 or higher";

		[SerializeField]
		private bool m_noInterpolation;

		[SerializeField]
		private bool m_sample;

		protected override void CommonInit( int uniqueId )
		{
			base.CommonInit( uniqueId );
			m_inputPorts[ 0 ].AddPortForbiddenTypes(	WirePortDataType.FLOAT3x3,
														WirePortDataType.FLOAT4x4,
														WirePortDataType.SAMPLER1D,
														WirePortDataType.SAMPLER2D,
														WirePortDataType.SAMPLER3D,
														WirePortDataType.SAMPLERCUBE,
														WirePortDataType.SAMPLER2DARRAY,
														WirePortDataType.SAMPLERSTATE );
			m_inputPorts[ 0 ].Name = "(VS) In";
			m_outputPorts[ 0 ].Name = "Out";
			m_useInternalPortData = false;
			m_autoWrapProperties = true;
			m_errorMessageTypeIsError = NodeMessageType.Warning;
			m_previewShaderGUID = "74e4d859fbdb2c0468de3612145f4929";
		}

		public override void DrawProperties()
		{
			base.DrawProperties();
			bool isSurface = ContainerGraph.IsStandardSurface;
			EditorGUI.BeginDisabledGroup( isSurface && !m_noInterpolation );
			m_noInterpolation = EditorGUILayoutToggle( "No Interpolation" , m_noInterpolation );
			EditorGUI.EndDisabledGroup();
			if( m_noInterpolation  )
			{
				if( isSurface )
				{
					EditorGUILayout.HelpBox( DisabledInterpolatorMsg, MessageType.Warning );
				} else
				{
					EditorGUILayout.HelpBox( NoInterpolationUsageMsg, MessageType.Info );
				}
			}

			EditorGUI.BeginDisabledGroup( isSurface && !m_sample );
			m_sample = EditorGUILayoutToggle( "Sample" , m_sample );
			EditorGUI.EndDisabledGroup();
			if( m_sample )
				EditorGUILayout.HelpBox( SampleInfoMessage , MessageType.Info ); 
		}

		public override void Draw( DrawInfo drawInfo )
		{
			base.Draw( drawInfo );
			m_showErrorMessage =	ContainerGraph.IsStandardSurface && m_noInterpolation ||
									ContainerGraph.IsStandardSurface && m_sample;
		}


		public override string GenerateShaderForOutput( int outputId, ref MasterNodeDataCollector dataCollector, bool ignoreLocalVar )
		{
			if( m_outputPorts[ 0 ].IsLocalValue( dataCollector.PortCategory ) )
				return m_outputPorts[ 0 ].LocalValue( dataCollector.PortCategory );

			bool noInterpolationFlag = dataCollector.IsTemplate ? m_noInterpolation : false;
			bool sampleFlag = dataCollector.IsTemplate ? m_sample : false;
			string varName = GenerateInputInVertex( ref dataCollector, 0, "vertexToFrag" + OutputId,true, noInterpolationFlag, sampleFlag );
			m_outputPorts[ 0 ].SetLocalValue( varName, dataCollector.PortCategory );

			return varName;

			////TEMPLATES
			//if( dataCollector.IsTemplate )
			//{
			//	if( !dataCollector.IsFragmentCategory )
			//		return m_inputPorts[ 0 ].GeneratePortInstructions( ref dataCollector );

			//	string varName = "vertexToFrag" + OutputId;
			//	if( dataCollector.TemplateDataCollectorInstance.HasCustomInterpolatedData( varName ) )
			//		return varName;

			//	MasterNodePortCategory category = dataCollector.PortCategory;
			//	dataCollector.PortCategory = MasterNodePortCategory.Vertex;
			//	bool dirtyVertexVarsBefore = dataCollector.DirtyVertexVariables;
			//	ContainerGraph.ResetNodesLocalVariablesIfNot( this, MasterNodePortCategory.Vertex );

			//	string data = m_inputPorts[ 0 ].GeneratePortInstructions( ref dataCollector );

			//	dataCollector.PortCategory = category;
			//	if( !dirtyVertexVarsBefore && dataCollector.DirtyVertexVariables )
			//	{
			//		dataCollector.AddVertexInstruction( dataCollector.VertexLocalVariables, UniqueId, false );
			//		dataCollector.ClearVertexLocalVariables();
			//		ContainerGraph.ResetNodesLocalVariablesIfNot( this, MasterNodePortCategory.Vertex );
			//	}

			//	ContainerGraph.ResetNodesLocalVariablesIfNot( this, MasterNodePortCategory.Fragment );

			//	dataCollector.TemplateDataCollectorInstance.RegisterCustomInterpolatedData( varName, m_inputPorts[ 0 ].DataType, m_currentPrecisionType, data );
			//	//return varName;

			//	m_outputPorts[ 0 ].SetLocalValue( varName );
			//	return m_outputPorts[ 0 ].LocalValue;
			//}

			////SURFACE 
			//{
			//	if( !dataCollector.IsFragmentCategory )
			//	{
			//		return m_inputPorts[ 0 ].GeneratePortInstructions( ref dataCollector );
			//	}

			//	if( dataCollector.TesselationActive )
			//	{
			//		UIUtils.ShowMessage( "Unable to use Vertex to Frag when Tessellation is active" );
			//		return m_outputPorts[ 0 ].ErrorValue;
			//	}


			//	string interpName = "data" + OutputId;
			//	dataCollector.AddToInput( UniqueId, interpName, m_inputPorts[ 0 ].DataType, m_currentPrecisionType );

			//	MasterNodePortCategory portCategory = dataCollector.PortCategory;
			//	dataCollector.PortCategory = MasterNodePortCategory.Vertex;

			//	bool dirtyVertexVarsBefore = dataCollector.DirtyVertexVariables;

			//	ContainerGraph.ResetNodesLocalVariablesIfNot( this, MasterNodePortCategory.Vertex );

			//	string vertexVarValue = m_inputPorts[ 0 ].GeneratePortInstructions( ref dataCollector );
			//	dataCollector.AddLocalVariable( UniqueId, Constants.VertexShaderOutputStr + "." + interpName, vertexVarValue + ";" );

			//	dataCollector.PortCategory = portCategory;

			//	if( !dirtyVertexVarsBefore && dataCollector.DirtyVertexVariables )
			//	{
			//		dataCollector.AddVertexInstruction( dataCollector.VertexLocalVariables, UniqueId, false );
			//		dataCollector.ClearVertexLocalVariables();
			//		ContainerGraph.ResetNodesLocalVariablesIfNot( this, MasterNodePortCategory.Vertex );
			//	}

			//	ContainerGraph.ResetNodesLocalVariablesIfNot( this, MasterNodePortCategory.Fragment );

			//	//return Constants.InputVarStr + "." + interpName;

			//	m_outputPorts[ 0 ].SetLocalValue( Constants.InputVarStr + "." + interpName );
			//	return m_outputPorts[ 0 ].LocalValue;
			//}
		}
		public override void ReadFromString( ref string[] nodeParams )
		{
			base.ReadFromString( ref nodeParams );
			if( UIUtils.CurrentShaderVersion() > 18707 )
				m_noInterpolation = Convert.ToBoolean( GetCurrentParam( ref nodeParams ) );

			if( UIUtils.CurrentShaderVersion() > 18808 )
				m_sample = Convert.ToBoolean( GetCurrentParam( ref nodeParams ) );
		}

		public override void WriteToString( ref string nodeInfo, ref string connectionsInfo )
		{
			base.WriteToString( ref nodeInfo, ref connectionsInfo );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_noInterpolation );
			IOUtils.AddFieldValueToString( ref nodeInfo , m_sample );
		}
	}
}
