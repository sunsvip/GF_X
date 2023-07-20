// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using System;

namespace AmplifyShaderEditor
{
	[Serializable]
	[NodeAttributes( "Clip", "Miscellaneous", "Conditionally kill a pixel before output" )]
	public sealed class ClipNode : ParentNode
	{
		private const string ClipOpFormat = "clip( {0} );";
		private const string ClipSubOpFormat = "clip( {0} - {1});";
		protected override void CommonInit( int uniqueId )
		{
			base.CommonInit( uniqueId );
			AddInputPort( WirePortDataType.FLOAT, false, Constants.EmptyPortValue );
			AddInputPort( WirePortDataType.FLOAT, false, "Alpha" );
			AddInputPort( WirePortDataType.FLOAT, false, "Threshold" );
			AddOutputPort( WirePortDataType.FLOAT, Constants.EmptyPortValue );
			m_useInternalPortData = true;

			m_inputPorts[ 0 ].CreatePortRestrictions(	WirePortDataType.OBJECT,
														WirePortDataType.FLOAT ,
														WirePortDataType.FLOAT2,
														WirePortDataType.FLOAT3,
														WirePortDataType.FLOAT4,
														WirePortDataType.COLOR ,
														WirePortDataType.INT);

			m_previewShaderGUID = "1fca7774f364aee4d8c64e8634ef4be4";
		}

		public override void OnInputPortConnected( int portId, int otherNodeId, int otherPortId, bool activateNode = true )
		{
			base.OnInputPortConnected( portId, otherNodeId, otherPortId, activateNode );
			m_inputPorts[ portId ].MatchPortToConnection();
			UpdatePortConnection( portId );
		}

		public override void OnConnectedOutputNodeChanges( int outputPortId, int otherNodeId, int otherPortId, string name, WirePortDataType type )
		{
			base.OnConnectedOutputNodeChanges( outputPortId, otherNodeId, otherPortId, name, type );
			m_inputPorts[ outputPortId ].MatchPortToConnection();
			UpdatePortConnection( outputPortId );
		}

		void UpdatePortConnection( int portId )
		{
			if( portId == 0 )
			{
				m_outputPorts[ 0 ].ChangeType( m_inputPorts[ 0 ].DataType, false );
			}
			else
			{
				int otherPortId = portId == 1 ? 2 : 1;
				if( m_inputPorts[ otherPortId ].IsConnected )
				{
					WirePortDataType type1 = m_inputPorts[ portId ].DataType;
					WirePortDataType type2 = m_inputPorts[ otherPortId ].DataType;

					WirePortDataType mainType = UIUtils.GetPriority( type1 ) > UIUtils.GetPriority( type2 ) ? type1 : type2;

					m_inputPorts[ portId ].ChangeType( mainType, false );
					m_inputPorts[ otherPortId ].ChangeType( mainType , false );
				}
				else
				{
					m_inputPorts[ otherPortId ].ChangeType( m_inputPorts[ portId ].DataType,false );
				}
			}
		}

		public override void OnInputPortDisconnected( int portId )
		{
			base.OnInputPortDisconnected( portId );
			if( portId == 0 )
				return;
			int otherPortId = portId == 1 ? 2 : 1;
			if( m_inputPorts[ otherPortId ].IsConnected )
			{
				m_inputPorts[ portId ].ChangeType( m_inputPorts[ otherPortId ].DataType, false );
			}
			else
			{
				m_inputPorts[ portId ].ChangeType( WirePortDataType.FLOAT, false );
				m_inputPorts[ otherPortId ].ChangeType( WirePortDataType.FLOAT, false );
			}
		}

		public override string GenerateShaderForOutput( int outputId, ref MasterNodeDataCollector dataCollector, bool ignoreLocalvar )
		{
			if( dataCollector.PortCategory == MasterNodePortCategory.Vertex ||
				dataCollector.PortCategory == MasterNodePortCategory.Tessellation )
			{
				UIUtils.ShowMessage( UniqueId, "Clip can only be used in fragment functions", MessageSeverity.Warning );
				return GenerateErrorValue();
			}

			string value = m_inputPorts[ 0 ].GeneratePortInstructions( ref dataCollector );
			string alpha = m_inputPorts[ 1 ].GeneratePortInstructions( ref dataCollector );
			if( m_inputPorts[ 2 ].IsConnected )
			{
				string threshold = m_inputPorts[ 2 ].GeneratePortInstructions( ref dataCollector );
				dataCollector.AddLocalVariable( UniqueId, string.Format( ClipSubOpFormat, alpha , threshold) );
			}
			else
			{
				if( m_inputPorts[ 2 ].IsZeroInternalData )
				{
					dataCollector.AddLocalVariable( UniqueId, string.Format( ClipOpFormat, alpha ) );
				}
				else
				{
					string threshold = m_inputPorts[ 2 ].GeneratePortInstructions( ref dataCollector );
					dataCollector.AddLocalVariable( UniqueId, string.Format( ClipSubOpFormat, alpha, threshold ) );
				}
			}

			return value;
		}
	}
}
