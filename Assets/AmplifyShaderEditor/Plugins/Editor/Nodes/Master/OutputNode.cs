using System;
using UnityEngine;

namespace AmplifyShaderEditor
{
	public class OutputNode : SignalGeneratorNode
	{
		public static int LOD_SUBSHADER_VERSION = 17200;
		[SerializeField]
		protected bool m_isMainOutputNode = false;

		[SerializeField]
		protected int m_lodIndex = -1;

		public OutputNode() : base() { }
		public OutputNode( int uniqueId, float x, float y, float width, float height ) : base( uniqueId, x, y, width, height ) { }

		public override void ResetNodeData()
		{
			base.ResetNodeData();
			m_graphDepth = -1;
		}

		public virtual void SetupNodeCategories()
		{
			ContainerGraph.ResetNodesData();
			//int count = m_inputPorts.Count;
			//for( int i = 0; i < count; i++ )
			//{
			//	if( m_inputPorts[ i ].IsConnected )
			//	{
			//		NodeData nodeData = new NodeData( m_inputPorts[ i ].Category );
			//		ParentNode node = m_inputPorts[ i ].GetOutputNode();
			//		node.PropagateNodeData( nodeData, ref collector );
			//	}
			//}
		}

		public override void WriteToString( ref string nodeInfo, ref string connectionsInfo )
		{
			base.WriteToString( ref nodeInfo, ref connectionsInfo );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_isMainOutputNode );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_lodIndex );
		}

		public override void ReadFromString( ref string[] nodeParams )
		{
			base.ReadFromString( ref nodeParams );
			m_isMainOutputNode = Convert.ToBoolean( GetCurrentParam( ref nodeParams ) );
			if( UIUtils.CurrentShaderVersion() > LOD_SUBSHADER_VERSION )
			{
				m_lodIndex = Convert.ToInt32( GetCurrentParam( ref nodeParams ) );
			}

			if( IsLODMainMasterNode && !ContainerGraph.IsDuplicating )
			{
				ContainerGraph.AssignMasterNode( this, true );
			}
		}

		public override void AfterDuplication()
		{
			base.AfterDuplication();
			m_isMainOutputNode = false;
		}
		
		public bool IsMainOutputNode
		{
			get { return m_isMainOutputNode; }
			set
			{
				if( value != m_isMainOutputNode )
				{
					m_isMainOutputNode = value;
					if( m_isMainOutputNode )
					{
						GenerateSignalPropagation();
					}
					else
					{
						GenerateSignalInibitor();
					}
				}
			}
		}

		public int LODIndex { get { return m_lodIndex; } set { m_lodIndex = value; } }
		public bool IsLODMainMasterNode { get { return m_isMainOutputNode && m_lodIndex == -1; } }
	}
}
