// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using System;
namespace AmplifyShaderEditor
{
	[Serializable]
	[NodeAttributes( "Time Parameters", "Time", "Time since level load" )]
	public sealed class TimeNode : ConstVecShaderVariable
	{
		private readonly string[] SRPTime =
		{
			"( _TimeParameters.x * 0.05 )",
			"( _TimeParameters.x )",
			"( _TimeParameters.x * 2 )",
			"( _TimeParameters.x * 3 )",
		};

		protected override void CommonInit( int uniqueId )
		{
			base.CommonInit( uniqueId );
			ChangeOutputName( 1, "t/20" );
			ChangeOutputName( 2, "t" );
			ChangeOutputName( 3, "t*2" );
			ChangeOutputName( 4, "t*3" );
			m_value = "_Time";
			m_previewShaderGUID = "73abc10c8d1399444827a7eeb9c24c2a";
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
				if(	dataCollector.TemplateDataCollectorInstance.IsHDRP || dataCollector.TemplateDataCollectorInstance.IsLWRP )
					return SRPTime[ outputId - 1 ];
			}

			return base.GenerateShaderForOutput( outputId, ref dataCollector, ignoreLocalvar );
		}
	}
}
