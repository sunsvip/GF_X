// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using System;

using UnityEngine;

namespace AmplifyShaderEditor
{
	[Serializable]
	[NodeAttributes( "Switch by Face", "Miscellaneous", "Switch which automaticaly uses a Face variable to select which input to use" )]
	public class SwitchByFaceNode : DynamicTypeNode
	{
		private const string SwitchOp = "((({0}>0)?({1}):({2})))";
		protected override void CommonInit( int uniqueId )
		{
			base.CommonInit( uniqueId );
			m_inputPorts[ 0 ].Name = "Front";
			m_inputPorts[ 1 ].Name = "Back";
			m_textLabelWidth = 50;
			m_previewShaderGUID = "f4edf6febb54dc743b25bd5b56facea8";
		}

		

		public override string GenerateShaderForOutput( int outputId, ref MasterNodeDataCollector dataCollector, bool ignoreLocalvar )
		{
			if ( dataCollector.PortCategory == MasterNodePortCategory.Tessellation )
			{
				UIUtils.ShowMessage( UniqueId, m_nodeAttribs.Name + " does not work on Tessellation port" );
				return GenerateErrorValue();
			}

			if ( dataCollector.PortCategory == MasterNodePortCategory.Vertex )
			{
				if ( dataCollector.TesselationActive )
				{
					UIUtils.ShowMessage( UniqueId, m_nodeAttribs.Name + " does not work properly on Vertex/Tessellation ports" );
					return GenerateErrorValue();
				}
				else
				{
					UIUtils.ShowMessage( UniqueId , FaceVariableNode.FaceOnVertexWarning , MessageSeverity.Warning );
					string faceVariable = GeneratorUtils.GenerateVertexFace( ref dataCollector , UniqueId );

					if( m_outputPorts[ 0 ].IsLocalValue( dataCollector.PortCategory ) )
						return m_outputPorts[ 0 ].LocalValue( dataCollector.PortCategory );

					string frontValue = m_inputPorts[ 0 ].GeneratePortInstructions( ref dataCollector );
					string backValue = m_inputPorts[ 1 ].GeneratePortInstructions( ref dataCollector );

					string finalResult = string.Format( SwitchOp , faceVariable , frontValue , backValue );
					RegisterLocalVariable( 0 , finalResult , ref dataCollector , "switchResult" + OutputId );
					return m_outputPorts[ 0 ].LocalValue( dataCollector.PortCategory );
				}
			}

			if ( m_outputPorts[ 0 ].IsLocalValue( dataCollector.PortCategory ) )
				return m_outputPorts[ 0 ].LocalValue( dataCollector.PortCategory );

			string front = m_inputPorts[ 0 ].GeneratePortInstructions( ref dataCollector );
			string back = m_inputPorts[ 1 ].GeneratePortInstructions( ref dataCollector );

			if ( dataCollector.CurrentCanvasMode == NodeAvailability.TemplateShader )
			{
				dataCollector.AddToInput( UniqueId, SurfaceInputs.FRONT_FACING );
			}
			else
			{
				dataCollector.AddToInput( UniqueId, SurfaceInputs.FRONT_FACING_VFACE );
			}

			string variable = string.Empty;
			if ( dataCollector.IsTemplate )
			{
				variable = dataCollector.TemplateDataCollectorInstance.GetVFace( UniqueId );
			}
			else
			{
				variable = ( ( dataCollector.PortCategory == MasterNodePortCategory.Vertex ) ? Constants.VertexShaderOutputStr : Constants.InputVarStr ) + "." + Constants.IsFrontFacingVariable;
			}

			string value = string.Format( SwitchOp, variable, front, back );
			RegisterLocalVariable( 0, value, ref dataCollector, "switchResult" + OutputId );
			return m_outputPorts[ 0 ].LocalValue( dataCollector.PortCategory );
		}
	}
}
