// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using System;
using UnityEditor;
using UnityEngine;
namespace AmplifyShaderEditor
{
	[Serializable]
	[NodeAttributes( "Sin Time", "Time", "Unity sin time" )]
	public sealed class SinTimeNode : ConstVecShaderVariable
	{
		//double m_time;
		private readonly string[] SRPTime =
		{
			"sin( _TimeParameters.x * 0.125 )",
			"sin( _TimeParameters.x * 0.25 )",
			"sin( _TimeParameters.x * 0.5 )",
			"_TimeParameters.y",
		};

		protected override void CommonInit( int uniqueId )
		{
			base.CommonInit( uniqueId );
			ChangeOutputName( 1, "t/8" );
			ChangeOutputName( 2, "t/4" );
			ChangeOutputName( 3, "t/2" );
			ChangeOutputName( 4, "t" );
			m_value = "_SinTime";
			m_previewShaderGUID = "e4ba809e0badeb94994170b2cbbbba10";
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
