// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using System;

namespace AmplifyShaderEditor
{
	[Serializable]
	[NodeAttributes( "Luminance", "Image Effects", "Calculates Luminance value from input")]
	public sealed class LuminanceNode : ParentNode
	{
		protected override void CommonInit( int uniqueId )
		{
			base.CommonInit( uniqueId );
			AddInputPort( WirePortDataType.FLOAT3, false, "RGB" );
			AddOutputPort( WirePortDataType.FLOAT, Constants.EmptyPortValue );
			m_previewShaderGUID = "81e1d8ffeec8a4b4cabb1094bc981048";
		}

		public override string GenerateShaderForOutput( int outputId, ref MasterNodeDataCollector dataCollector, bool ignoreLocalvar )
		{
			if( m_outputPorts[ 0 ].IsLocalValue( dataCollector.PortCategory ) )
				return m_outputPorts[ 0 ].LocalValue( dataCollector.PortCategory );

			string value = m_inputPorts[ 0 ].GeneratePortInstructions( ref dataCollector );
			string grayscale = "Luminance(" + value + ")";

			RegisterLocalVariable( 0, grayscale, ref dataCollector, "luminance" + OutputId );
			return m_outputPorts[ 0 ].LocalValue( dataCollector.PortCategory );
		}
	}
}
