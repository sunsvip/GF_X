// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>
using System;
using UnityEditor;
using UnityEngine;

namespace AmplifyShaderEditor
{
	[Serializable]
	[NodeAttributes( "Bone Blend Weights" , "Vertex Data" , "Bone blend weights for skinned Meshes" )]
	public sealed class BlendWeightsNode : VertexDataNode
	{
		private const string IncorrectUnityVersionMessage = "This info is only available on Unity 2019.1 or above.";
		private const string StandardSurfaceErrorMessage = "This info is not available on standard surface shaders.";

		protected override void CommonInit( int uniqueId )
		{
			base.CommonInit( uniqueId );
			m_currentVertexData = GeneratorUtils.VertexBlendWeightsStr;
			m_errorMessageTypeIsError = NodeMessageType.Error;
		}

		public override void OnNodeLogicUpdate( DrawInfo drawInfo )
		{
			base.OnNodeLogicUpdate( drawInfo );
			if( UIUtils.CurrentWindow.OutsideGraph.IsStandardSurface  )
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
			string blendWeights = string.Empty;
			if( dataCollector.MasterNodeCategory == AvailableShaderTypes.Template )
			{
				blendWeights = dataCollector.TemplateDataCollectorInstance.GetBlendWeights();
				return GetOutputVectorItem( 0 , outputId , blendWeights );
			}

			return GenerateErrorValue( outputId );
		}
	}
}
