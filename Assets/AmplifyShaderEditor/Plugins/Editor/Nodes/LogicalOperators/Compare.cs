// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using UnityEngine;
using UnityEditor;
using System;
using System.CodeDom;

namespace AmplifyShaderEditor
{
	[Serializable]
	[NodeAttributes( "Compare", "Logical Operators", "Compare A to B given the selected operator. If comparison is true return value of True else return value of False", tags: "If Ternary Compare Less Equal Not Greater" )]
	public sealed class Compare : ParentNode
	{
		private static readonly string[] LabelsSTR = { "Equal", "Not Equal", "Greater", "Greater Or Equal", "Less", "Less Or Equal" };

		enum Comparision
		{
			Equal,
			NotEqual,
			Greater,
			GreaterOrEqual,
			Less,
			LessOrEqual,
		}

		private WirePortDataType m_mainInputType = WirePortDataType.FLOAT;
		private WirePortDataType m_mainOutputType = WirePortDataType.FLOAT;

		private int m_cachedOperatorId = -1;

		[SerializeField]
		private Comparision m_comparision = Comparision.Equal;

		private UpperLeftWidgetHelper m_upperLeftWidget = new UpperLeftWidgetHelper();

		protected override void CommonInit( int uniqueId )
		{
			base.CommonInit( uniqueId );
			AddInputPort( WirePortDataType.FLOAT, false, "A" );
			AddInputPort( WirePortDataType.FLOAT, false, "B" );
			AddInputPort( WirePortDataType.FLOAT, false, "True" );
			AddInputPort( WirePortDataType.FLOAT, false, "False" );
			AddOutputPort( WirePortDataType.FLOAT, Constants.EmptyPortValue );
			m_inputPorts[ 0 ].AutoDrawInternalData = true;
			m_inputPorts[ 1 ].AutoDrawInternalData = true;
			m_inputPorts[ 2 ].AutoDrawInternalData = true;
			m_inputPorts[ 3 ].AutoDrawInternalData = true;
			m_textLabelWidth = 100;
			m_autoWrapProperties = true;
			m_hasLeftDropdown = true;
			m_previewShaderGUID = "381937898f0c15747af1da09a751890c";
			UpdateTitle();
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
			m_comparision = (Comparision)m_upperLeftWidget.DrawWidget( this, (int)m_comparision, LabelsSTR );
			if( EditorGUI.EndChangeCheck() )
			{
				UpdateTitle();
			}
		}

		public override void SetPreviewInputs()
		{
			base.SetPreviewInputs();

			if( m_cachedOperatorId == -1 )
				m_cachedOperatorId = Shader.PropertyToID( "_Operator" );

			PreviewMaterial.SetInt( m_cachedOperatorId, (int)m_comparision );
		}

		void UpdateTitle()
		{
			switch( m_comparision )
			{
				default:
				case Comparision.Equal:
				m_additionalContent.text = "( A = B )";
				break;
				case Comparision.NotEqual:
				m_additionalContent.text = "( A \u2260 B )";
				break;
				case Comparision.Greater:
				m_additionalContent.text = "( A > B )";
				break;
				case Comparision.GreaterOrEqual:
				m_additionalContent.text = "( A \u2265 B )";
				break;
				case Comparision.Less:
				m_additionalContent.text = "( A < B )";
				break;
				case Comparision.LessOrEqual:
				m_additionalContent.text = "( A \u2264 B )";
				break;
			}
			m_sizeIsDirty = true;
		}

		public override void DrawProperties()
		{
			base.DrawProperties();
			EditorGUI.BeginChangeCheck();
			m_comparision = (Comparision)EditorGUILayoutEnumPopup( "", m_comparision );
			if( EditorGUI.EndChangeCheck() )
			{
				UpdateTitle();
			}

			for( int i = 0; i < m_inputPorts.Count; i++ )
			{
				if( m_inputPorts[ i ].ValidInternalData && !m_inputPorts[ i ].IsConnected && m_inputPorts[ i ].Visible )
				{
					m_inputPorts[ i ].ShowInternalData( this );
				}
			}
		}

		public override void OnConnectedOutputNodeChanges( int inputPortId, int otherNodeId, int otherPortId, string name, WirePortDataType type )
		{
			base.OnConnectedOutputNodeChanges( inputPortId, otherNodeId, otherPortId, name, type );
			UpdateConnection( inputPortId );
		}

		public override void OnInputPortConnected( int portId, int otherNodeId, int otherPortId, bool activateNode = true )
		{
			base.OnInputPortConnected( portId, otherNodeId, otherPortId, activateNode );
			UpdateConnection( portId );
		}

		public override void OnInputPortDisconnected( int portId )
		{
			base.OnInputPortDisconnected( portId );

			int otherPortId = 0;
			if( portId < 2 )
			{
				otherPortId = ( portId == 0 ) ? 1 : 0;
				if( m_inputPorts[ otherPortId ].IsConnected )
				{
					m_mainInputType = m_inputPorts[ otherPortId ].DataType;
					m_inputPorts[ portId ].ChangeType( m_mainInputType, false );
				}
			}
			else
			{
				otherPortId = ( portId == 2 ) ? 3 : 2;
				if( m_inputPorts[ otherPortId ].IsConnected )
				{
					m_mainOutputType = m_inputPorts[ otherPortId ].DataType;
					m_inputPorts[ portId ].ChangeType( m_mainOutputType, false );
					m_outputPorts[ 0 ].ChangeType( m_mainOutputType, false );
				}
			}
		}

		public void UpdateConnection( int portId )
		{
			m_inputPorts[ portId ].MatchPortToConnection();
			int otherPortId = 0;
			WirePortDataType otherPortType = WirePortDataType.FLOAT;
			if( portId < 2 )
			{
				otherPortId = ( portId == 0 ) ? 1 : 0;
				otherPortType = m_inputPorts[ otherPortId ].IsConnected ? m_inputPorts[ otherPortId ].DataType : WirePortDataType.FLOAT;
				m_mainInputType = UIUtils.GetPriority( m_inputPorts[ portId ].DataType ) > UIUtils.GetPriority( otherPortType ) ? m_inputPorts[ portId ].DataType : otherPortType;
				if( !m_inputPorts[ otherPortId ].IsConnected )
				{
					m_inputPorts[ otherPortId ].ChangeType( m_mainInputType, false );
				}
			}
			else
			{
				otherPortId = ( portId == 2 ) ? 3 : 2;
				otherPortType = m_inputPorts[ otherPortId ].IsConnected ? m_inputPorts[ otherPortId ].DataType : WirePortDataType.FLOAT;
				m_mainOutputType = UIUtils.GetPriority( m_inputPorts[ portId ].DataType ) > UIUtils.GetPriority( otherPortType ) ? m_inputPorts[ portId ].DataType : otherPortType;

				m_outputPorts[ 0 ].ChangeType( m_mainOutputType, false );

				if( !m_inputPorts[ otherPortId ].IsConnected )
				{
					m_inputPorts[ otherPortId ].ChangeType( m_mainOutputType, false );
				}
			}
		}

		public override string GenerateShaderForOutput( int outputId, ref MasterNodeDataCollector dataCollector, bool ignoreLocalvar )
		{
			//Conditional Operator ?: has some shenanigans
			//If the first operand is of type bool, one of the following must hold for the second and third operands:
			//Both operands have compatible structure types.
			//Both operands are scalars with numeric or bool type.
			//Both operands are vectors with numeric or bool type, where the two vectors are of the same size, which is less than or equal to four.
			//If the first operand is a packed vector of bool, then the conditional selection is performed on an elementwise basis.Both the second and third operands must be numeric vectors of the same size as the first operand.
			WirePortDataType compatibleInputType = m_mainInputType;
			if( m_mainInputType != WirePortDataType.FLOAT && m_mainInputType != WirePortDataType.INT && m_mainInputType != m_mainOutputType )
			{
				compatibleInputType = m_mainOutputType;
			}

			string a = m_inputPorts[ 0 ].GenerateShaderForOutput( ref dataCollector, compatibleInputType, ignoreLocalvar, true );
			string b = m_inputPorts[ 1 ].GenerateShaderForOutput( ref dataCollector, compatibleInputType, ignoreLocalvar, true );
			string op = string.Empty;
			switch( m_comparision )
			{
				default:
				case Comparision.Equal:
				op = "==";
				break;
				case Comparision.NotEqual:
				op = "!=";
				break;
				case Comparision.Greater:
				op = ">";
				break;
				case Comparision.GreaterOrEqual:
				op = ">=";
				break;
				case Comparision.Less:
				op = "<";
				break;
				case Comparision.LessOrEqual:
				op = "<=";
				break;
			}
			string T = m_inputPorts[ 2 ].GenerateShaderForOutput( ref dataCollector, m_mainOutputType, ignoreLocalvar, true );
			string F = m_inputPorts[ 3 ].GenerateShaderForOutput( ref dataCollector, m_mainOutputType, ignoreLocalvar, true );
			return CreateOutputLocalVariable( 0, string.Format( "( {0} {2} {1} ? {3} : {4} )", a, b, op, T, F ), ref dataCollector );
		}

		public override void ReadFromDeprecated( ref string[] nodeParams, Type oldType = null )
		{
			base.ReadFromDeprecated( ref nodeParams, oldType );

			if( oldType == typeof( TFHCCompareEqual ) )
			{
				m_comparision = Comparision.Equal;
			}
			else
			if( oldType == typeof( TFHCCompareNotEqual ) )
			{
				m_comparision = Comparision.NotEqual;
			}
			else
			if( oldType == typeof( TFHCCompareGreater ) )
			{
				m_comparision = Comparision.Greater;
			} 
			else
			if( oldType == typeof( TFHCCompareGreaterEqual ) )
			{
				m_comparision = Comparision.GreaterOrEqual;
			}
			else
			if( oldType == typeof( TFHCCompareLower ) )
			{
				m_comparision = Comparision.Less;
			}
			else
			if( oldType == typeof( TFHCCompareLowerEqual ) )
			{
				m_comparision = Comparision.LessOrEqual;
			}

			UpdateTitle();
		}

		public override void ReadFromString( ref string[] nodeParams )
		{
			base.ReadFromString( ref nodeParams );

			m_comparision = (Comparision)Convert.ToSingle( GetCurrentParam( ref nodeParams ) );
			UpdateTitle();
		}

		public override void WriteToString( ref string nodeInfo, ref string connectionsInfo )
		{
			base.WriteToString( ref nodeInfo, ref connectionsInfo );

			IOUtils.AddFieldValueToString( ref nodeInfo, (int)m_comparision );
		}
	}
}
