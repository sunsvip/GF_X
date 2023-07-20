// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using System;
using UnityEngine;
using UnityEditor;
namespace AmplifyShaderEditor
{
	[Serializable]
	[NodeAttributes( "Billboard", "Miscellaneous", "Calculates new Vertex positions and normals to achieve a billboard effect." )]
	public sealed class BillboardNode : ParentNode
	{
		private const string ErrorMessage = "Billboard node should only be connected to vertex ports.";
		private const string WarningMessage = "This node is a bit different from all others as it injects the necessary code into the vertex body and writes directly on the vertex position and normal.\nIt outputs a value of 0 so it can be connected directly to a vertex port.\n[Only if that port is a relative vertex offset].";

		[SerializeField]
		private BillboardType m_billboardType = BillboardType.Cylindrical;

		[SerializeField]
		private bool m_rotationIndependent = false;

		[SerializeField]
		private bool m_affectNormalTangent = true;

		private UpperLeftWidgetHelper m_upperLeftWidget = new UpperLeftWidgetHelper();

		protected override void CommonInit( int uniqueId )
		{
			base.CommonInit( uniqueId );
			AddOutputPort( WirePortDataType.FLOAT3, "Out" );
			m_textLabelWidth = 115;
			m_hasLeftDropdown = true;
			SetAdditonalTitleText( string.Format( Constants.SubTitleTypeFormatStr, m_billboardType ) );
		}

		public override void Destroy()
		{
			base.Destroy();
			m_upperLeftWidget = null;
		}

		public override void AfterCommonInit()
		{
			base.AfterCommonInit();

			if( PaddingTitleLeft == 0 )
			{
				PaddingTitleLeft = Constants.PropertyPickerWidth + Constants.IconsLeftRightMargin;
				if( PaddingTitleRight == 0 )
					PaddingTitleRight = Constants.PropertyPickerWidth + Constants.IconsLeftRightMargin;
			}
		}

		public override void Draw( DrawInfo drawInfo )
		{
			base.Draw( drawInfo );
			m_upperLeftWidget.DrawWidget<BillboardType>( ref m_billboardType, this, OnWidgetUpdate );
		}

		private readonly Action<ParentNode> OnWidgetUpdate = ( x ) =>
		{
			x.SetAdditonalTitleText( string.Format( Constants.SubTitleTypeFormatStr, ( x as BillboardNode ).Type ) );
		};

		public override void DrawProperties()
		{
			base.DrawProperties();
			NodeUtils.DrawPropertyGroup( ref m_propertiesFoldout, Constants.ParameterLabelStr, () =>
			 {
				 EditorGUI.BeginChangeCheck();
				 m_billboardType = (BillboardType)EditorGUILayoutEnumPopup( BillboardOpHelper.BillboardTypeStr, m_billboardType );
				 if( EditorGUI.EndChangeCheck() )
				 {
					 SetAdditonalTitleText( string.Format( Constants.SubTitleTypeFormatStr, m_billboardType ) );
				 }
				 m_rotationIndependent = EditorGUILayoutToggle( BillboardOpHelper.BillboardRotIndStr, m_rotationIndependent );
				 m_affectNormalTangent = EditorGUILayoutToggle( BillboardOpHelper.BillboardAffectNormalTangentStr , m_affectNormalTangent );
			 } );
			EditorGUILayout.HelpBox( WarningMessage, MessageType.Warning );
		}

		public override string GenerateShaderForOutput( int outputId, ref MasterNodeDataCollector dataCollector, bool ignoreLocalvar )
		{
			if( dataCollector.IsFragmentCategory )
			{
				UIUtils.ShowMessage( UniqueId, ErrorMessage,MessageSeverity.Error );
				return m_outputPorts[0].ErrorValue;
			}
			if( m_outputPorts[ 0 ].IsLocalValue( dataCollector.PortCategory ) )
				return m_outputPorts[ 0 ].ErrorValue;

			m_outputPorts[ 0 ].SetLocalValue( "0", dataCollector.PortCategory );
			string vertexPosValue = dataCollector.IsTemplate ? dataCollector.TemplateDataCollectorInstance.GetVertexPosition( WirePortDataType.OBJECT, CurrentPrecisionType ) : "v.vertex";
			string vertexNormalValue = dataCollector.IsTemplate ? dataCollector.TemplateDataCollectorInstance.GetVertexNormal( CurrentPrecisionType ) : "v.normal";
			string vertexTangentValue = dataCollector.IsTemplate ? dataCollector.TemplateDataCollectorInstance.GetVertexTangent( WirePortDataType.FLOAT4, CurrentPrecisionType ) : "v.tangent";

			bool vertexIsFloat3 = false;
			if( dataCollector.IsTemplate )
			{
				InterpDataHelper info = dataCollector.TemplateDataCollectorInstance.GetInfo( TemplateInfoOnSematics.POSITION );
				if( info != null )
				{
					vertexIsFloat3 = info.VarType == WirePortDataType.FLOAT3;
				}
			}

			BillboardOpHelper.FillDataCollector( ref dataCollector, m_billboardType, m_rotationIndependent, vertexPosValue, vertexNormalValue, vertexTangentValue, vertexIsFloat3, m_affectNormalTangent );

			return "0";
		}

		public override void ReadFromString( ref string[] nodeParams )
		{
			base.ReadFromString( ref nodeParams );
			m_billboardType = (BillboardType)Enum.Parse( typeof( BillboardType ), GetCurrentParam( ref nodeParams ) );
			SetAdditonalTitleText( string.Format( Constants.SubTitleTypeFormatStr , m_billboardType ) );
			m_rotationIndependent = Convert.ToBoolean( GetCurrentParam( ref nodeParams ) );
			if( UIUtils.CurrentShaderVersion() > 18918 )
			{
				m_affectNormalTangent = Convert.ToBoolean( GetCurrentParam( ref nodeParams ) );
			}
		}

		public override void WriteToString( ref string nodeInfo, ref string connectionsInfo )
		{
			base.WriteToString( ref nodeInfo, ref connectionsInfo );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_billboardType );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_rotationIndependent );
			IOUtils.AddFieldValueToString( ref nodeInfo , m_affectNormalTangent );
			SetAdditonalTitleText( string.Format( Constants.SubTitleTypeFormatStr, m_billboardType ) );
		}

		public BillboardType Type { get { return m_billboardType; } }
	}
}
