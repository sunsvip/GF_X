// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

namespace AmplifyShaderEditor
{
	[System.Serializable]
	public class ParentTransfNode : ParentNode
	{
		protected string m_matrixName;
		protected string m_matrixHDName;
		protected string m_matrixLWName;

		protected override void CommonInit( int uniqueId )
		{
			base.CommonInit( uniqueId );
			AddInputPort( WirePortDataType.FLOAT4, false, string.Empty );
			AddOutputVectorPorts( WirePortDataType.FLOAT4, "XYZW" );
			m_useInternalPortData = true;
			m_inputPorts[ 0 ].Vector4InternalData = new UnityEngine.Vector4( 0, 0, 0, 1 );
		}

		public override string GenerateShaderForOutput( int outputId, ref MasterNodeDataCollector dataCollector, bool ignoreLocalvar )
		{
			if ( m_outputPorts[ 0 ].IsLocalValue( dataCollector.PortCategory ) )
				return GetOutputVectorItem( 0, outputId, m_outputPorts[ 0 ].LocalValue( dataCollector.PortCategory ) );

			string value = m_inputPorts[ 0 ].GeneratePortInstructions( ref dataCollector );
			string matrixName = string.Empty;
			if( dataCollector.IsTemplate  )
			{
				if( dataCollector.TemplateDataCollectorInstance.CurrentSRPType == TemplateSRPType.HDRP && !string.IsNullOrEmpty( m_matrixHDName ) )
				{
					matrixName = m_matrixHDName;
				}
				else if( dataCollector.TemplateDataCollectorInstance.CurrentSRPType == TemplateSRPType.URP && !string.IsNullOrEmpty( m_matrixLWName ) )
				{
					matrixName = m_matrixLWName;
				}
				else
				{
					matrixName = m_matrixName;
				}
			}
			else
			{
				matrixName = m_matrixName;
			}

			RegisterLocalVariable( 0, string.Format( "mul({0},{1})", matrixName, value ),ref dataCollector,"transform"+ OutputId );
			return GetOutputVectorItem( 0, outputId, m_outputPorts[ 0 ].LocalValue( dataCollector.PortCategory ) );
		}
	}
}
