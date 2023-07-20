// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using System;
using UnityEngine;
using UnityEditor;

namespace AmplifyShaderEditor
{
	[Serializable]
	[NodeAttributes( "Reflection Probe" , "Miscellaneous" , "Provides access to the nearest Reflection Probe to the object. Only available on URP." )]
	public class ReflectionProbeNode : ParentNode
	{
		private const string ReflectionProbeStr = "SHADERGRAPH_REFLECTION_PROBE({0},{1},{2})";
		private const string InfoTransformSpace = "Both View Dir and Normal vectors are set in Object Space";
		public const string NodeErrorMsg = "Only valid on URP";
		public const string ErrorOnCompilationMsg = "Attempting to use URP specific node on incorrect SRP or Builtin RP.";
		protected override void CommonInit( int uniqueId )
		{
			base.CommonInit( uniqueId );
			AddInputPort( WirePortDataType.FLOAT3 , false , "View Dir" );
			AddInputPort( WirePortDataType.FLOAT3 , false , "Normal" );
			AddInputPort( WirePortDataType.FLOAT , false , "LOD" );
			AddOutputPort( WirePortDataType.FLOAT3 , "Out" );
			m_autoWrapProperties = true;
			m_errorMessageTooltip = NodeErrorMsg;
			m_errorMessageTypeIsError = NodeMessageType.Error;
		}

		public override void OnNodeLogicUpdate( DrawInfo drawInfo )
		{
			base.OnNodeLogicUpdate( drawInfo );
			m_showErrorMessage = ( ContainerGraph.CurrentCanvasMode == NodeAvailability.SurfaceShader ) ||
									( ContainerGraph.CurrentCanvasMode == NodeAvailability.TemplateShader && ContainerGraph.CurrentSRPType != TemplateSRPType.URP );
		}

		public override void DrawProperties()
		{
			base.DrawProperties();
			EditorGUILayout.HelpBox( InfoTransformSpace , MessageType.Info );
			if( m_showErrorMessage )
			{
				EditorGUILayout.HelpBox( NodeErrorMsg , MessageType.Error );
			}
		}

		public override string GenerateShaderForOutput( int outputId , ref MasterNodeDataCollector dataCollector , bool ignoreLocalvar )
		{
			if( !dataCollector.IsSRP || !dataCollector.TemplateDataCollectorInstance.IsLWRP )
			{
				UIUtils.ShowMessage( ErrorOnCompilationMsg , MessageSeverity.Error );
				return GenerateErrorValue();
			}

			if( m_outputPorts[ 0 ].IsLocalValue( dataCollector.PortCategory ) )
				return m_outputPorts[ 0 ].LocalValue( dataCollector.PortCategory );

			string viewDir = m_inputPorts[ 0 ].GeneratePortInstructions( ref dataCollector );
			string normal = m_inputPorts[ 1 ].GeneratePortInstructions( ref dataCollector );
			string lod = m_inputPorts[ 2 ].GeneratePortInstructions( ref dataCollector );

			RegisterLocalVariable( outputId , string.Format( ReflectionProbeStr , viewDir , normal , lod ), ref dataCollector );
			return m_outputPorts[ 0 ].LocalValue( dataCollector.PortCategory );
		}
	}
}
