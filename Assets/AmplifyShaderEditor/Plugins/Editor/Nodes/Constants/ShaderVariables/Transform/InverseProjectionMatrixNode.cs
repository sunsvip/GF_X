// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

namespace AmplifyShaderEditor
{
	[System.Serializable]
	[NodeAttributes( "Inverse Projection Matrix", "Matrix Transform", "Current inverse projection matrix", NodeAvailabilityFlags = (int)( NodeAvailability.TemplateShader ) )]
	public sealed class InverseProjectionMatrixNode : ConstantShaderVariable
	{
		protected override void CommonInit( int uniqueId )
		{
			base.CommonInit( uniqueId );
			ChangeOutputProperties( 0, "Out", WirePortDataType.FLOAT4x4 );
			m_value = "UNITY_MATRIX_I_P";
			m_drawPreview = false;
			m_matrixId = 1;
		}

		public override string GenerateShaderForOutput( int outputId, ref MasterNodeDataCollector dataCollector, bool ignoreLocalvar )
		{
			if( dataCollector.IsTemplate && dataCollector.IsSRP )
			{
				return base.GenerateShaderForOutput( outputId, ref dataCollector, ignoreLocalvar );
			}
			else
			{
				return GeneratorUtils.GenerateIdentity4x4( ref dataCollector, UniqueId );
			}
		}

		public override void Draw( DrawInfo drawInfo )
		{
			base.Draw( drawInfo );
			if( ContainerGraph.IsSRP )
			{
				m_showErrorMessage = false;
			}
			else
			{
				m_showErrorMessage = true;
				m_errorMessageTypeIsError = NodeMessageType.Warning;
				m_errorMessageTooltip = "This node only works for Scriptable Render Pipeline (LWRP, HDRP, URP)";
			}
		}
	}
}
