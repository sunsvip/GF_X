// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using System;
using UnityEngine;

namespace AmplifyShaderEditor
{
	[Serializable]
	[NodeAttributes( "Decode Lightmap", "Miscellaneous", "Decodes color from Unity lightmap (RGBM or dLDR depending on platform)" )]
	public sealed class DecodeLightmapHlpNode : ParentNode
	{
		private const string m_funcStandard = "DecodeLightmap({0})";
		private string m_funcSRP = "DecodeLightmap({0},{1})";

		private const string DecodeInstructionsLWValueStr = "half4 decodeLightmapInstructions = half4(LIGHTMAP_HDR_MULTIPLIER, LIGHTMAP_HDR_EXPONENT, 0.0h, 0.0h);";
		private const string DecodeInstructionsNameStr = "decodeLightmapInstructions";
		private readonly string[] DecodeInstructionsHDValueStr =
		{
			"#ifdef UNITY_LIGHTMAP_FULL_HDR//ase_decode_lightmap_0",
			"\tbool useRGBMLightmap = false;//ase_decode_lightmap_1",
			"\tfloat4 decodeLightmapInstructions = float4( 0.0, 0.0, 0.0, 0.0 );//ase_decode_lightmap_2",
			"#else//ase_decode_lightmap//ase_decode_lightmap_3",
			"\tbool useRGBMLightmap = true;//ase_decode_lightmap_4",
			"#if defined(UNITY_LIGHTMAP_RGBM_ENCODING)//ase_decode_lightmap_5",
			"\tfloat4 decodeLightmapInstructions = float4(34.493242, 2.2, 0.0, 0.0);//ase_decode_lightmap_6",
			"#else//ase_decode_lightmap_7",
			"\tfloat4 decodeLightmapInstructions = float4( 2.0, 2.2, 0.0, 0.0 );//ase_decode_lightmap_8",
			"#endif//ase_decode_lightmap_9",
			"#endif//ase_decode_lightmap_10"
		};
		private string m_localVarName = null;

		protected override void CommonInit( int uniqueId )
		{
			base.CommonInit( uniqueId );
			AddInputPort( WirePortDataType.FLOAT4, false, "Value" );
			AddInputPort( WirePortDataType.FLOAT4, false, "Instructions" );

			AddOutputPort( WirePortDataType.FLOAT3, Constants.EmptyPortValue );

			m_previewShaderGUID = "c2d3bee1aee183343b31b9208cb402e9";
			m_useInternalPortData = true;
		}

		public override string GetIncludes()
		{
			return Constants.UnityCgLibFuncs;
		}

		protected override void OnUniqueIDAssigned()
		{
			base.OnUniqueIDAssigned();
			m_localVarName = "decodeLightMap" + OutputId;
		}

		public override void OnNodeLogicUpdate( DrawInfo drawInfo )
		{
			base.OnNodeLogicUpdate( drawInfo );
			m_inputPorts[ 1 ].Visible = m_containerGraph.ParentWindow.IsShaderFunctionWindow || m_containerGraph.IsSRP;
		}

		public override string GenerateShaderForOutput( int outputId, ref MasterNodeDataCollector dataCollector, bool ignoreLocalvar )
		{
			if( m_outputPorts[ 0 ].IsLocalValue( dataCollector.PortCategory ) )
				return GetOutputVectorItem( 0, outputId, m_outputPorts[ 0 ].LocalValue( dataCollector.PortCategory ) );

			base.GenerateShaderForOutput( outputId, ref dataCollector, ignoreLocalvar );

			string value = m_inputPorts[ 0 ].GeneratePortInstructions( ref dataCollector );
			string finalResult = string.Empty;
			if( dataCollector.IsTemplate && dataCollector.IsSRP )
			{
				string instructions = string.Empty;
				if( m_inputPorts[ 1 ].IsConnected )
				{
					instructions = m_inputPorts[ 1 ].GeneratePortInstructions( ref dataCollector );
				}
				else
				{
					if( dataCollector.TemplateDataCollectorInstance.IsHDRP )
					{
						for( int i = 0; i < DecodeInstructionsHDValueStr.Length; i++ )
						{
							dataCollector.AddLocalVariable( UniqueId, DecodeInstructionsHDValueStr[ i ] );
						}
					}
					else
					{
						dataCollector.AddLocalVariable( UniqueId, DecodeInstructionsLWValueStr );
					}
						instructions = DecodeInstructionsNameStr;

				}

				finalResult = string.Format( m_funcSRP, value , instructions );

			}
			else
			{
				dataCollector.AddToIncludes( UniqueId, Constants.UnityCgLibFuncs );
				finalResult = string.Format( m_funcStandard, value );
			}
			
			RegisterLocalVariable( 0, finalResult, ref dataCollector, m_localVarName );
			return GetOutputVectorItem( 0, outputId, m_outputPorts[ 0 ].LocalValue( dataCollector.PortCategory ) );
		}
	}
}
