// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using UnityEngine;
using UnityEditor;
using System;

namespace AmplifyShaderEditor
{
	[Serializable]
	[NodeAttributes( "Switch by Pipeline", "Functions", "Executes branch according to current pipeline", NodeAvailabilityFlags = (int)NodeAvailability.ShaderFunction )]
	public sealed class FunctionSwitchByPipeline : ParentNode
	{
		protected override void CommonInit( int uniqueId )
		{
			base.CommonInit( uniqueId );
			AddInputPort( WirePortDataType.FLOAT, false, "BiRP Surface", -1, MasterNodePortCategory.Fragment, 0 );
			AddInputPort( WirePortDataType.FLOAT, false, "BiRP VertFrag", -1, MasterNodePortCategory.Fragment, 3 );
			AddInputPort( WirePortDataType.FLOAT, false, "URP", -1, MasterNodePortCategory.Fragment, 1 );
			AddInputPort( WirePortDataType.FLOAT, false, "HDRP", -1, MasterNodePortCategory.Fragment, 2 );
			AddOutputPort( WirePortDataType.FLOAT, Constants.EmptyPortValue );

		}

		public override void OnInputPortConnected( int portId, int otherNodeId, int otherPortId, bool activateNode = true )
		{
			base.OnInputPortConnected( portId, otherNodeId, otherPortId, activateNode );
			GetInputPortByUniqueId( portId ).MatchPortToConnection();
			UpdateOutputPort();
		}

		public override void OnConnectedOutputNodeChanges( int outputPortId, int otherNodeId, int otherPortId, string name, WirePortDataType type )
		{
			base.OnConnectedOutputNodeChanges( outputPortId, otherNodeId, otherPortId, name, type );
			GetInputPortByUniqueId( outputPortId ).MatchPortToConnection();
			UpdateOutputPort();
		}

		void UpdateOutputPort()
		{
			switch( UIUtils.CurrentWindow.OutsideGraph.CurrentSRPType )
			{
				case TemplateSRPType.BiRP:
				{
					InputPort port = UIUtils.CurrentWindow.OutsideGraph.IsStandardSurface ? GetInputPortByUniqueId( 0 ) : GetInputPortByUniqueId( 3 );
					m_outputPorts[ 0 ].ChangeType( port.DataType, false );
				}
				break;
				case TemplateSRPType.URP:
				{
					InputPort port = GetInputPortByUniqueId( 1 );
					m_outputPorts[ 0 ].ChangeType( port.DataType, false );
				}
				break;
				case TemplateSRPType.HDRP:
				{
					InputPort port = GetInputPortByUniqueId( 2 );
					m_outputPorts[ 0 ].ChangeType( port.DataType, false );
				}
				break;
			}
		}

		public override string GenerateShaderForOutput( int outputId, ref MasterNodeDataCollector dataCollector, bool ignoreLocalvar )
		{
			base.GenerateShaderForOutput( outputId, ref dataCollector, ignoreLocalvar );
			switch( dataCollector.CurrentSRPType )
			{
				case TemplateSRPType.BiRP:
				{
					InputPort port = UIUtils.CurrentWindow.OutsideGraph.IsStandardSurface ? GetInputPortByUniqueId( 0 ) : GetInputPortByUniqueId( 3 );
					return port.GeneratePortInstructions( ref dataCollector );
				}
				case TemplateSRPType.URP:
				{
					InputPort port = GetInputPortByUniqueId( 1 );
					return port.GeneratePortInstructions( ref dataCollector );
				}
				case TemplateSRPType.HDRP:
				{
					InputPort port = GetInputPortByUniqueId( 2 );
					return port.GeneratePortInstructions( ref dataCollector );
				}
			}

			return "0";
		}

		public override void RefreshExternalReferences()
		{
			base.RefreshExternalReferences();
			if( UIUtils.CurrentShaderVersion() < 16303 )
			{
				InputPort standardPort = GetInputPortByUniqueId( 0 );
				if( standardPort.IsConnected )
				{
					UIUtils.SetConnection( UniqueId, 3, standardPort.GetConnection().NodeId, standardPort.GetConnection().PortId );
				}
			}
		}
	}
}
