// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>
using System;
using UnityEditor;
using UnityEngine;

namespace AmplifyShaderEditor
{
	[Serializable]
	[NodeAttributes( "Vertex Tangent", "Vertex Data", "Vertex tangent vector in object space, can be used in both local vertex offset and fragment outputs" )]
	public sealed class TangentVertexDataNode : VertexDataNode
	{
		private const string PropertyLabel = "Size";
		private readonly string[] SizeLabels = { "XYZ", "XYZW" };

		[SerializeField]
		private int m_sizeOption = 0;

		private UpperLeftWidgetHelper m_upperLeftWidget = new UpperLeftWidgetHelper();

		protected override void CommonInit( int uniqueId )
		{
			base.CommonInit( uniqueId );
			m_currentVertexData = "tangent";
			ChangeOutputProperties( 0, "XYZ", WirePortDataType.FLOAT3 );
			m_outputPorts[ 4 ].Visible = false;
			m_drawPreviewAsSphere = true;
			m_hasLeftDropdown = true;
			m_previewShaderGUID = "0a44bb521d06d6143a4acbc3602037f8";
		}

		public override void Destroy()
		{
			base.Destroy();
			m_upperLeftWidget = null;
		}

		public override void Draw( DrawInfo drawInfo )
		{
			base.Draw( drawInfo );
			EditorGUI.BeginChangeCheck();
			m_sizeOption = m_upperLeftWidget.DrawWidget( this, m_sizeOption, SizeLabels );
			if( EditorGUI.EndChangeCheck() )
			{
				UpdatePorts();
			}
		}

		public override void DrawProperties()
		{
			EditorGUI.BeginChangeCheck();
			m_sizeOption = EditorGUILayoutPopup( PropertyLabel, m_sizeOption, SizeLabels );
			if( EditorGUI.EndChangeCheck() )
			{
				UpdatePorts();
			}
		}

		void UpdatePorts()
		{
			if( m_sizeOption == 0 )
			{
				ChangeOutputProperties( 0, SizeLabels[ 0 ], WirePortDataType.FLOAT3, false );
				m_outputPorts[ 4 ].Visible = false;
			}
			else
			{
				ChangeOutputProperties( 0, SizeLabels[ 1 ], WirePortDataType.FLOAT4, false );
				m_outputPorts[ 4 ].Visible = true;
			}
		}

		public override void PropagateNodeData( NodeData nodeData, ref MasterNodeDataCollector dataCollector )
		{
			base.PropagateNodeData( nodeData, ref dataCollector );
			dataCollector.DirtyNormal = true;
		}

		public override string GenerateShaderForOutput( int outputId, ref MasterNodeDataCollector dataCollector, bool ignoreLocalVar )
		{
			string vertexTangent = string.Empty;
			if ( dataCollector.MasterNodeCategory == AvailableShaderTypes.Template )
			{
				vertexTangent = dataCollector.TemplateDataCollectorInstance.GetVertexTangent( WirePortDataType.FLOAT4, CurrentPrecisionType );
				if( m_sizeOption == 0 )
					vertexTangent += ".xyz";

				return GetOutputVectorItem( 0, outputId, vertexTangent );
			}

			if ( dataCollector.PortCategory == MasterNodePortCategory.Fragment || dataCollector.PortCategory == MasterNodePortCategory.Debug )
			{
				dataCollector.ForceNormal = true;
				dataCollector.AddToInput( UniqueId, SurfaceInputs.WORLD_NORMAL, CurrentPrecisionType );
				dataCollector.AddToInput( UniqueId, SurfaceInputs.INTERNALDATA, addSemiColon: false );
			}

			WirePortDataType sizeType = m_sizeOption == 0 ? WirePortDataType.FLOAT3 : WirePortDataType.FLOAT4;

			vertexTangent = GeneratorUtils.GenerateVertexTangent( ref dataCollector, UniqueId, CurrentPrecisionType, sizeType  );
			return GetOutputVectorItem( 0, outputId, vertexTangent );
		}

		public override void ReadFromString( ref string[] nodeParams )
		{
			base.ReadFromString( ref nodeParams );
			if( UIUtils.CurrentShaderVersion() > 16100 )
			{
				m_sizeOption = Convert.ToInt32( GetCurrentParam( ref nodeParams ) );
				UpdatePorts();
			}
		}

		public override void WriteToString( ref string nodeInfo, ref string connectionsInfo )
		{
			base.WriteToString( ref nodeInfo, ref connectionsInfo );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_sizeOption );
		}
	}
}
