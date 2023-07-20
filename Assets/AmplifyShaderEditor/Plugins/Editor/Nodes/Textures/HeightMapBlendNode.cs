// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>
//
// Custom Node HeightMap Texture Masking
// Donated by Rea

using UnityEngine;
using UnityEditor;
using System;

namespace AmplifyShaderEditor
{
	[Serializable]
	[NodeAttributes( "HeightMap Texture Blend", "Textures", "Advanced Texture Blending by using heightMap and splatMask, usefull for texture layering ", null, KeyCode.None, true, false, null, null, "Rea" )]
	public sealed class HeightMapBlendNode : ParentNode
	{
		private const string PreventNaNLabel = "Prevent NaN";
		private const string PreventNaNInfo = "Prevent NaN clamps negative base numbers over the internal pow instruction to 0 since these originate NaN.";
		[SerializeField]
		private bool m_preventNaN = false;
		protected override void CommonInit( int uniqueId )
		{
			base.CommonInit( uniqueId );
			AddInputPort( WirePortDataType.FLOAT, false, "HeightMap" );
			AddInputPort( WirePortDataType.FLOAT, false, "SplatMask" );
			AddInputPort( WirePortDataType.FLOAT, false, "BlendStrength" );
			AddOutputVectorPorts( WirePortDataType.FLOAT, Constants.EmptyPortValue );
			m_textLabelWidth = 120;
			m_useInternalPortData = true;
			m_inputPorts[ 2 ].FloatInternalData = 1;
			m_autoWrapProperties = true;
			m_previewShaderGUID = "b2ac23d6d5dcb334982b6f31c2e7a734";
		}

		public override void DrawProperties()
		{
			base.DrawProperties();
			m_preventNaN = EditorGUILayoutToggle( PreventNaNLabel , m_preventNaN );
			EditorGUILayout.HelpBox( PreventNaNInfo , MessageType.Info );
		}

		public override string GenerateShaderForOutput( int outputId, ref MasterNodeDataCollector dataCollector, bool ignoreLocalvar )
		{
			if ( m_outputPorts[ 0 ].IsLocalValue( dataCollector.PortCategory ) )
				return m_outputPorts[ 0 ].LocalValue( dataCollector.PortCategory );

			string HeightMap = m_inputPorts[ 0 ].GeneratePortInstructions( ref dataCollector );
			string SplatMask = m_inputPorts[ 1 ].GeneratePortInstructions( ref dataCollector);
			string Blend = m_inputPorts[ 2 ].GeneratePortInstructions( ref dataCollector );
			string baseOp = "((" + HeightMap + "*" + SplatMask + ")*4)+(" + SplatMask + "*2)";
			if( m_preventNaN )
				baseOp = "max( (" + baseOp + "), 0 )";
			string HeightMask =  "saturate(pow("+baseOp+"," + Blend + "))";
			string varName = "HeightMask" + OutputId;

			RegisterLocalVariable( 0, HeightMask, ref dataCollector , varName );
			return m_outputPorts[ 0 ].LocalValue( dataCollector.PortCategory );
		}
		/*
         A = (heightMap * SplatMask)*4
         B = SplatMask*2
         C = pow(A+B,Blend)
         saturate(C)
         saturate(pow(((heightMap * SplatMask)*4)+(SplatMask*2),Blend));
         */

		public override void ReadFromString( ref string[] nodeParams )
		{
			base.ReadFromString( ref nodeParams );
			if( UIUtils.CurrentShaderVersion() > 18910 )
				m_preventNaN = Convert.ToBoolean( GetCurrentParam( ref nodeParams ) );
		}

		public override void WriteToString( ref string nodeInfo , ref string connectionsInfo )
		{
			base.WriteToString( ref nodeInfo , ref connectionsInfo );
			IOUtils.AddFieldValueToString( ref nodeInfo , m_preventNaN );
		}
	}
}
