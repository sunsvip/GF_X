// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

namespace AmplifyShaderEditor
{
	[System.Serializable]
	[NodeAttributes( "Object To World", "Object Transform", "Transforms input to World Space" )]
	public sealed class ObjectToWorldTransfNode : ParentTransfNode
	{
		protected override void CommonInit( int uniqueId )
		{
			base.CommonInit( uniqueId );
			m_matrixName = "unity_ObjectToWorld";
			m_matrixHDName = "GetObjectToWorldMatrix()";
			m_matrixLWName = "GetObjectToWorldMatrix()";
			m_previewShaderGUID = "a4044ee165813654486d0cecd0de478c";
		}

		public override string GenerateShaderForOutput( int outputId, ref MasterNodeDataCollector dataCollector, bool ignoreLocalvar )
		{
			string result = base.GenerateShaderForOutput( 0, ref dataCollector, ignoreLocalvar );
			if( dataCollector.IsTemplate && dataCollector.TemplateDataCollectorInstance.CurrentSRPType == TemplateSRPType.HDRP && !string.IsNullOrEmpty( m_matrixHDName ) )
			{
				dataCollector.AddLocalVariable( UniqueId, string.Format( "{0}.xyz", result ), string.Format( "GetAbsolutePositionWS(({0}).xyz);", result ) );
			}

			return GetOutputVectorItem( 0, outputId, result );
		}
	}
}
