// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>
using System;
using UnityEditor;
using UnityEngine;

namespace AmplifyShaderEditor
{
	[Serializable]
	[NodeAttributes( "Bone Blend Indices" , "Vertex Data" , "Bone indices for skinned Meshes" )]
	public sealed class BlendIndicesNode : VertexDataNode
	{
		private const string IncorrectUnityVersionMessage = "This info is only available on Unity 2019.1 or above.";
		private const string StandardSurfaceErrorMessage = "This info is not available on standard surface shaders.";

		protected override void CommonInit( int uniqueId )
		{
			base.CommonInit( uniqueId );
			m_outputPorts[ 1 ].ChangeType( WirePortDataType.UINT , false );
			m_outputPorts[ 2 ].ChangeType( WirePortDataType.UINT , false );
			m_outputPorts[ 3 ].ChangeType( WirePortDataType.UINT , false );
			m_outputPorts[ 4 ].ChangeType( WirePortDataType.UINT , false );
			m_currentVertexData = GeneratorUtils.VertexBlendIndicesStr;
			m_errorMessageTypeIsError = NodeMessageType.Error;
		}

		public override void OnNodeLogicUpdate( DrawInfo drawInfo )
		{
			base.OnNodeLogicUpdate( drawInfo );
			if( UIUtils.CurrentWindow.OutsideGraph.IsStandardSurface )
			{
				if( !m_showErrorMessage )
				{
					m_showErrorMessage = true;
					m_errorMessageTooltip = StandardSurfaceErrorMessage;
				}
			}
			else
			{
				m_showErrorMessage = false;
			}
		}

		public override string GenerateShaderForOutput( int outputId , ref MasterNodeDataCollector dataCollector , bool ignoreLocalVar )
		{
			string blendIndices = string.Empty;
			if( dataCollector.MasterNodeCategory == AvailableShaderTypes.Template )
			{
				blendIndices = dataCollector.TemplateDataCollectorInstance.GetBlendIndices();
				return GetOutputVectorItem( 0 , outputId , blendIndices );
			}

			return GenerateErrorValue( outputId );
		}
	}
}
