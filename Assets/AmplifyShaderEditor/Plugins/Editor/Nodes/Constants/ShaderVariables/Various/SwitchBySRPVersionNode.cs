// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using System;
using System.Collections.Generic;
using UnityEngine;

namespace AmplifyShaderEditor
{
	[Serializable]
	[NodeAttributes( "Switch by SRP Version", "Miscellaneous", "Switch between different inputs based on the currently installed SRP version" )]
	public class SwitchBySRPVersionNode : ParentNode
	{
		private static readonly Tuple<string, int>[] SRPVersionList = new Tuple<string, int>[]
		{
			new Tuple<string, int>( "None", -1 ),
			new Tuple<string, int>( "10.x", 100000 ),
			new Tuple<string, int>( "11.x", 110000 ),
			new Tuple<string, int>( "12.x", 120000 ),
			new Tuple<string, int>( "13.x", 130000 ),
			new Tuple<string, int>( "14.x", 140000 ),
			new Tuple<string, int>( "15.x", 150000 )
		};

		private static readonly string[] SRPTypeNames =
		{
			"Built-in",
			"High Definition",
			"Universal"
		};

		private static readonly int m_conditionId = Shader.PropertyToID( "_Condition" );

		protected override void CommonInit( int uniqueId )
		{
			base.CommonInit( uniqueId );

			foreach ( var item in SRPVersionList )
			{
				AddInputPort( WirePortDataType.FLOAT, false, item.Item1 );
			}

			AddOutputPort( WirePortDataType.FLOAT, Constants.EmptyPortValue );

			m_textLabelWidth = 50;
			m_previewShaderGUID = "63c0b9ddc2c9d0c4b871af8347b2d5c9";

			UpdateConnections();
		}

		public override void OnInputPortConnected( int portId, int otherNodeId, int otherPortId, bool activateNode = true )
		{
			base.OnInputPortConnected( portId, otherNodeId, otherPortId, activateNode );
			GetInputPortByUniqueId( portId ).MatchPortToConnection();
			UpdateConnections();
		}

		public override void OnInputPortDisconnected( int portId )
		{
			base.OnInputPortDisconnected( portId );
			GetInputPortByUniqueId( portId ).MatchPortToConnection();
			UpdateConnections();
		}

		public override void OnConnectedOutputNodeChanges( int outputPortId, int otherNodeId, int otherPortId, string name, WirePortDataType type )
		{
			base.OnConnectedOutputNodeChanges( outputPortId, otherNodeId, otherPortId, name, type );
			GetInputPortByUniqueId( outputPortId ).MatchPortToConnection();
			UpdateConnections();
		}

		public override void OnMasterNodeReplaced( MasterNode newMasterNode )
		{
			base.OnMasterNodeReplaced( newMasterNode );
			UpdateConnections();
		}

		public override void RefreshExternalReferences()
		{
			base.RefreshExternalReferences();
			UpdateConnections();
		}
		
		private int GetActivePortArrayId()
		{
			if ( ContainerGraph != null )
			{
				int srpVersion = ASEPackageManagerHelper.CurrentSRPVersion;
				for ( int i = SRPVersionList.Length - 1; i >= 0; i-- )
				{
					if ( srpVersion > SRPVersionList[ i ].Item2 )
					{
						return i;
					}
				}

			}
			return 0;
		}		

		private void UpdateConnections()
		{
			int activePortIndex = GetActivePortArrayId();
			InputPort activePort = GetInputPortByArrayId( activePortIndex );
			m_outputPorts[ 0 ].ChangeTypeWithRestrictions( activePort.DataType, FunctionInput.PortCreateRestriction( activePort.DataType ) );

			string srpTypeName = ( ContainerGraph != null ) ? SRPTypeNames[ ( int )ContainerGraph.CurrentSRPType ] : "Unknown";
			SetAdditonalTitleText( string.Format( Constants.SubTitleCurrentFormatStr, srpTypeName + ", " + SRPVersionList[ activePortIndex ].Item1 ) );
		}		

		public override string GenerateShaderForOutput( int outputId, ref MasterNodeDataCollector dataCollector, bool ignoreLocalvar )
		{
			base.GenerateShaderForOutput( outputId, ref dataCollector, ignoreLocalvar );
			InputPort port = GetInputPortByArrayId( GetActivePortArrayId() );
			m_outputPorts[ 0 ].ChangeType( port.DataType, false );
			return port.GeneratePortInstructions( ref dataCollector );
		}

		public override void SetPreviewInputs()
		{
			base.SetPreviewInputs();
			PreviewMaterial.SetInt( m_conditionId, GetActivePortArrayId() );
		}
	}
}
