// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using System;
namespace AmplifyShaderEditor
{
	[Serializable]
	[NodeAttributes( "Projection Params", "Camera And Screen", "Projection Near/Far parameters" )]
	public sealed class ProjectionParams : ConstVecShaderVariable
	{
		protected override void CommonInit( int uniqueId )
		{
			base.CommonInit( uniqueId );
			ChangeOutputName( 1, "Flipped" );
			ChangeOutputName( 2, "Near Plane" );
			ChangeOutputName( 3, "Far Plane" );
			ChangeOutputName( 4, "1/Far Plane" );
			m_value = "_ProjectionParams";
			m_previewShaderGUID = "97ae846cb0a6b044388fad3bc03bb4c2";
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
	}
}
