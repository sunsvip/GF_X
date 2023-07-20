using System;
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

namespace AmplifyShaderEditor
{
	[Serializable]
	[NodeAttributes( "Gradient Sample", "Miscellaneous", "Samples a gradient" )]
	public sealed class GradientSampleNode : ParentNode
	{
		private string m_functionHeader = "SampleGradient( {0}, {1} )";
		private string m_functionBody = string.Empty;

		private GradientNode m_gradientNode = null;
		private InputPort m_gradPort;

		private Gradient m_blankGrandient = new Gradient();

		private int m_cachedTimeId = -1;
		private int m_cachedTypeId = -1;
		private int m_cachedColorNumId = -1;
		private int m_cachedAlphaNumId = -1;

		private Vector4 m_auxVec4 = Vector4.zero;

		private string m_functionHeaderStruct = "Gradient( {0} )";
		private string m_functionBodyStruct = string.Empty;

		protected override void CommonInit( int uniqueId )
		{
			base.CommonInit( uniqueId );
			AddInputPort( WirePortDataType.OBJECT, true, Constants.EmptyPortValue );
			AddInputPort( WirePortDataType.FLOAT, false, "Time" );
			AddOutputColorPorts( "RGBA", true );
			m_gradPort = m_inputPorts[ 0 ];
			m_useInternalPortData = true;
			m_autoDrawInternalPortData = true;
			m_drawPreviewExpander = false;
			m_drawPreview = true;
			m_showPreview = true;
			m_selectedLocation = PreviewLocation.TopCenter;
			m_previewShaderGUID = "8a09124cd6e4aa54a996e7487ec16b90";
		}

		public override void SetPreviewInputs()
		{
			base.SetPreviewInputs();

			if( m_cachedTypeId == -1 )
				m_cachedTypeId = Shader.PropertyToID( "_GType" );

			if( m_cachedTimeId == -1 )
				m_cachedTimeId = Shader.PropertyToID( "_GTime" );

			if( m_cachedColorNumId == -1 )
				m_cachedColorNumId = Shader.PropertyToID( "_GColorNum" );

			if( m_cachedAlphaNumId == -1 )
				m_cachedAlphaNumId = Shader.PropertyToID( "_GAlphaNum" );

			PreviewMaterial.SetTexture( m_cachedTimeId, m_inputPorts[ 1 ].InputPreviewTexture( ContainerGraph ) );

			Gradient curGrad = m_blankGrandient;
			if( m_gradientNode != null )
				curGrad = m_gradientNode.Gradient;

			PreviewMaterial.SetInt( m_cachedTypeId, (int)curGrad.mode );
			PreviewMaterial.SetInt( m_cachedColorNumId, curGrad.colorKeys.Length );
			PreviewMaterial.SetInt( m_cachedAlphaNumId, curGrad.alphaKeys.Length );

			for( int i = 0; i < 8; i++ )
			{
				if( i < curGrad.colorKeys.Length )
				{
					m_auxVec4.x = curGrad.colorKeys[ i ].color.r;
					m_auxVec4.y = curGrad.colorKeys[ i ].color.g;
					m_auxVec4.z = curGrad.colorKeys[ i ].color.b;
					m_auxVec4.w = curGrad.colorKeys[ i ].time;
					PreviewMaterial.SetVector( "_Col" + i, m_auxVec4 );
				}
				else
				{
					PreviewMaterial.SetVector( "_Col" + i, Vector4.zero );
				}
			}

			for( int i = 0; i < 8; i++ )
			{
				if( i < curGrad.alphaKeys.Length )
				{
					m_auxVec4.x = curGrad.alphaKeys[ i ].alpha;
					m_auxVec4.y = curGrad.alphaKeys[ i ].time;
					PreviewMaterial.SetVector( "_Alp" + i, m_auxVec4 );
				}
				else
				{
					PreviewMaterial.SetVector( "_Alp" + i, Vector4.zero );
				}
			}
		}

		public override void OnInputPortConnected( int portId , int otherNodeId , int otherPortId , bool activateNode = true )
		{
			if( portId == 0 )
				m_gradientNode = RecursiveBackCheck( m_gradPort.GetOutputNode( 0 ) );

			base.OnInputPortConnected( portId , otherNodeId , otherPortId , activateNode );
		}

		public override void OnInputPortDisconnected( int portId )
		{
			if( portId == 0 )
			{
				m_gradientNode = null;
			}

			base.OnInputPortDisconnected( portId );
		}


		//public override void OnNodeLogicUpdate( DrawInfo drawInfo )
		//{
		//	base.OnNodeLogicUpdate( drawInfo );

		//	if( m_gradPort.IsConnected )
		//	{
		//		if( m_gradientNode == null )
		//		{
		//			m_gradientNode = RecursiveBackCheck( m_gradPort.GetOutputNode( 0 ) );
		//			PreviewIsDirty = true;
		//		}
		//	}
		//	else
		//	{
		//		m_gradientNode = null;
		//	}
		//}

		GradientNode RecursiveBackCheck( ParentNode node )
		{
			if( node is GradientNode )
			{
				return node as GradientNode;
			}
			else
			{
				if( node is RelayNode || node is WireNode || node is RegisterLocalVarNode )
				{
					return RecursiveBackCheck( node.InputPorts[ 0 ].GetOutputNode( 0 ) );
				}
				else if( node is GetLocalVarNode)
				{
					var gnode = node as GetLocalVarNode;
					if( gnode.CurrentSelected != null )
						return RecursiveBackCheck( gnode.CurrentSelected );
					else
						return null;
				}
				else
				{
					return null;
				}
			}
			
		}

		public override string GenerateShaderForOutput( int outputId, ref MasterNodeDataCollector dataCollector, bool ignoreLocalvar )
		{
			m_functionBodyStruct = string.Empty;
			if( !dataCollector.IsSRP )
			{
				GradientNode.GenerateGradientStruct( ref m_functionBodyStruct );
				dataCollector.AddFunctions( m_functionHeaderStruct, m_functionBodyStruct, "0" );
			}
			else
			{
				dataCollector.AddToIncludes( UniqueId, "Packages/com.unity.shadergraph/ShaderGraphLibrary/Functions.hlsl" );
			}

			GenerateGradientSampler( dataCollector.IsSRP );

			string gradient = "(Gradient)0";
			if( m_inputPorts[ 0 ].IsConnected && m_gradientNode != null )
				gradient = m_inputPorts[ 0 ].GeneratePortInstructions( ref dataCollector );

			string time = m_inputPorts[ 1 ].GeneratePortInstructions( ref dataCollector );

			string functionResult =	dataCollector.AddFunctions( m_functionHeader, m_functionBody, gradient, time );
			return GetOutputVectorItem( 0, outputId, functionResult );
		}

		void GenerateGradientSampler( bool isSrp )
		{
			m_functionBody = string.Empty;
			IOUtils.AddFunctionHeader( ref m_functionBody, "float4 SampleGradient( Gradient gradient, float time )" );
			IOUtils.AddFunctionLine( ref m_functionBody, "float3 color = gradient.colors[0].rgb;" );
			IOUtils.AddFunctionLine( ref m_functionBody, "UNITY_UNROLL" );
			IOUtils.AddFunctionLine( ref m_functionBody, "for (int c = 1; c < 8; c++)" );
			IOUtils.AddFunctionLine( ref m_functionBody, "{" );
			if( isSrp )
				IOUtils.AddFunctionLine( ref m_functionBody, "float colorPos = saturate((time - gradient.colors[c-1].w) / ( 0.00001 + (gradient.colors[c].w - gradient.colors[c-1].w)) * step(c, gradient.colorsLength-1));" );
			else
				IOUtils.AddFunctionLine( ref m_functionBody, "float colorPos = saturate((time - gradient.colors[c-1].w) / ( 0.00001 + (gradient.colors[c].w - gradient.colors[c-1].w)) * step(c, (float)gradient.colorsLength-1));" );
			IOUtils.AddFunctionLine( ref m_functionBody, "color = lerp(color, gradient.colors[c].rgb, lerp(colorPos, step(0.01, colorPos), gradient.type));" );
			IOUtils.AddFunctionLine( ref m_functionBody, "}" );

			IOUtils.AddFunctionLine( ref m_functionBody, "#ifndef UNITY_COLORSPACE_GAMMA" );
			if( isSrp )
				IOUtils.AddFunctionLine( ref m_functionBody, "color = SRGBToLinear(color);" );
			else
				IOUtils.AddFunctionLine( ref m_functionBody, "color = half3(GammaToLinearSpaceExact(color.r), GammaToLinearSpaceExact(color.g), GammaToLinearSpaceExact(color.b));" );
			IOUtils.AddFunctionLine( ref m_functionBody, "#endif" );

			IOUtils.AddFunctionLine( ref m_functionBody, "float alpha = gradient.alphas[0].x;" );
			IOUtils.AddFunctionLine( ref m_functionBody, "UNITY_UNROLL" );
			IOUtils.AddFunctionLine( ref m_functionBody, "for (int a = 1; a < 8; a++)" );
			IOUtils.AddFunctionLine( ref m_functionBody, "{" );
			if( isSrp )
				IOUtils.AddFunctionLine( ref m_functionBody, "float alphaPos = saturate((time - gradient.alphas[a-1].y) / ( 0.00001 + (gradient.alphas[a].y - gradient.alphas[a-1].y)) * step(a, gradient.alphasLength-1));" );
			else
				IOUtils.AddFunctionLine( ref m_functionBody, "float alphaPos = saturate((time - gradient.alphas[a-1].y) / ( 0.00001 + (gradient.alphas[a].y - gradient.alphas[a-1].y)) * step(a, (float)gradient.alphasLength-1));" );
			IOUtils.AddFunctionLine( ref m_functionBody, "alpha = lerp(alpha, gradient.alphas[a].x, lerp(alphaPos, step(0.01, alphaPos), gradient.type));" );
			IOUtils.AddFunctionLine( ref m_functionBody, "}" );
			IOUtils.AddFunctionLine( ref m_functionBody, "return float4(color, alpha);" );
			IOUtils.CloseFunctionBody( ref m_functionBody );
		}

	}
}
