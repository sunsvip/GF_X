// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using System;
namespace AmplifyShaderEditor
{
	[Serializable]
	[NodeAttributes( "Cos Time", "Time", "Cosine of time" )]
	public sealed class CosTime : ConstVecShaderVariable
	{
		private readonly string[] SRPTime =
		{
			"cos( _TimeParameters.x * 0.125 )",
			"cos( _TimeParameters.x * 0.25 )",
			"cos( _TimeParameters.x * 0.5 )",
			"_TimeParameters.z",
		};

		protected override void CommonInit( int uniqueId )
		{
			base.CommonInit( uniqueId );
			ChangeOutputName( 1, "t/8" );
			ChangeOutputName( 2, "t/4" );
			ChangeOutputName( 3, "t/2" );
			ChangeOutputName( 4, "t" );
			m_value = "_CosTime";
			m_previewShaderGUID = "3093999b42c3c0940a71799511d7781c";
			m_continuousPreviewRefresh = true;
		}

		public override void RefreshExternalReferences()
		{
			base.RefreshExternalReferences();
			if( !m_outputPorts[ 0 ].IsConnected )
			{
				m_outputPorts[ 0 ].Visible = false;
				m_sizeIsDirty = true;
			}
		}

		public override string GenerateShaderForOutput( int outputId, ref MasterNodeDataCollector dataCollector, bool ignoreLocalvar )
		{
			if( outputId > 0 && dataCollector.IsTemplate )
			{
				if( dataCollector.TemplateDataCollectorInstance.IsHDRP || dataCollector.TemplateDataCollectorInstance.IsLWRP )
					return SRPTime[ outputId - 1 ];
			}
			return base.GenerateShaderForOutput( outputId, ref dataCollector, ignoreLocalvar );
		}
	}
}
