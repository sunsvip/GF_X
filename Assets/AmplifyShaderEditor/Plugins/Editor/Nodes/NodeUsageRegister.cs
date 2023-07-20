using System;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace AmplifyShaderEditor
{
	[Serializable] public class UsageListSamplerNodes : NodeUsageRegister<SamplerNode> { }
	[Serializable] public class UsageListFloatIntNodes : NodeUsageRegister<PropertyNode> { }
	[Serializable] public class UsageListTexturePropertyNodes : NodeUsageRegister<TexturePropertyNode> { }
	[Serializable] public class UsageListTextureArrayNodes : NodeUsageRegister<TextureArrayNode> { }
	[Serializable] public class UsageListPropertyNodes : NodeUsageRegister<PropertyNode> { }
	[Serializable] public class UsageListScreenColorNodes : NodeUsageRegister<ScreenColorNode> { }
	[Serializable] public class UsageListRegisterLocalVarNodes : NodeUsageRegister<RegisterLocalVarNode> { }
	[Serializable] public class UsageListFunctionInputNodes : NodeUsageRegister<FunctionInput> { }
	[Serializable] public class UsageListFunctionNodes : NodeUsageRegister<FunctionNode> { }
	[Serializable] public class UsageListFunctionOutputNodes : NodeUsageRegister<FunctionOutput> { }
	[Serializable] public class UsageListFunctionSwitchNodes : NodeUsageRegister<FunctionSwitch> { }
	[Serializable] public class UsageListFunctionSwitchCopyNodes : NodeUsageRegister<FunctionSwitch> { }
	[Serializable] public class UsageListTemplateMultiPassMasterNodes : NodeUsageRegister<TemplateMultiPassMasterNode> { }
	[Serializable] public class UsageListCustomExpressionsOnFunctionMode : NodeUsageRegister<CustomExpressionNode> { }
	[Serializable] public class UsageListGlobalArrayNodes : NodeUsageRegister<GlobalArrayNode> { }
	[Serializable] public class UsageListStaticSwitchNodes : NodeUsageRegister<StaticSwitch> { }

	[Serializable]
	public class NodeUsageRegister<T> where T : ParentNode
	{
		public delegate void ReorderEvent();
		public event ReorderEvent OnReorderEventComplete;

		[SerializeField]
		public bool ReorderOnChange = false;

		// Sampler Nodes registry
		[SerializeField]
		private List<T> m_nodes;

		[SerializeField]
		private string[] m_nodesArr;

		[SerializeField]
		private int[] m_nodeIDs;

		[SerializeField]
		ParentGraph m_containerGraph;

		public NodeUsageRegister()
		{
			m_nodesArr = new string[ 0 ];
			m_nodeIDs = new int[ 0 ];
			m_nodes = new List<T>();
		}

		public void Destroy()
		{
			m_nodes.Clear();
			m_nodes = null;
			m_nodesArr = null;
			m_nodeIDs = null;
		}

		public void Clear()
		{
			m_nodes.Clear();
		}

		public int AddNode( T node )
		{
			if( node == null )
				return -1;

			if( !m_nodes.Contains( node ) )
			{
				if( m_containerGraph != null )
				{
					UndoUtils.RegisterCompleteObjectUndo( m_containerGraph.ParentWindow, Constants.UndoRegisterNodeId );
					UndoUtils.RegisterCompleteObjectUndo( m_containerGraph, Constants.UndoRegisterNodeId );
				}
				m_nodes.Add( node );
				ReorderNodes();
				UpdateNodeArr();
				return m_nodes.Count - 1;
			}
			else if( node.UniqueId > -1 )
			{
				UpdateNodeArr();
			}
			
			return -1;
		}

		public bool HasNode( int uniqueId )
		{
			return m_nodes.FindIndex( x => x.UniqueId == uniqueId ) > -1 ? true : false;
		}

		public void RemoveNode( T node )
		{
			if( node == null )
				return;

			if( m_nodes.Contains( node ) )
			{
				if( m_containerGraph != null )
				{
					UndoUtils.RegisterCompleteObjectUndo( m_containerGraph.ParentWindow, Constants.UndoUnregisterNodeId );
					UndoUtils.RegisterCompleteObjectUndo( m_containerGraph, Constants.UndoUnregisterNodeId );
				}

				m_nodes.Remove( node );
				ReorderNodes();
				UpdateNodeArr();
			}
		}

		public void ReorderNodes()
		{
			if( ReorderOnChange )
			{
				UnityEngine.Profiling.Profiler.BeginSample( "ReorderNodes_Sort" );
				m_nodes.Sort( ( x, y ) => ( x.DataToArray.CompareTo( y.DataToArray ) ) );
				UnityEngine.Profiling.Profiler.EndSample();

				if( OnReorderEventComplete != null )
				{
					UnityEngine.Profiling.Profiler.BeginSample( "ReorderNodes_CallEvent" );
					OnReorderEventComplete();
					UnityEngine.Profiling.Profiler.EndSample();
				}
			}
		}

		public void UpdateNodeArr()
		{
			int nodeCount = m_nodes.Count;
			if( nodeCount != m_nodesArr.Length )
			{
				m_nodesArr = new string[ nodeCount ];
				m_nodeIDs = new int[ nodeCount ];
			}
			
			for( int i = 0; i < nodeCount; i++ )
			{
				m_nodesArr[ i ] = m_nodes[ i ].DataToArray;
				m_nodeIDs[ i ] = m_nodes[ i ].UniqueId;
			}
		}

		public T GetNode( int idx )
		{
			if( idx > -1 && idx < m_nodes.Count )
			{
				return m_nodes[ idx ];
			}
			return null;
		}

		public T GetNodeByUniqueId( int uniqueId )
		{
			return m_nodes.Find( x => x.UniqueId == uniqueId );
		}

		public T GetNodeByDataToArray( string data )
		{
			return m_nodes.Find( x => x.DataToArray.Equals( data ));
		}

		public int GetNodeRegisterIdx( int uniqueId )
		{
			return m_nodes.FindIndex( x => x.UniqueId == uniqueId );
		}

		public void UpdateDataOnNode( int uniqueId, string data )
		{
			if( ReorderOnChange )
			{
				ReorderNodes();
				UpdateNodeArr();
			}
			else
			{
				int index = m_nodes.FindIndex( x => x.UniqueId == uniqueId );
				if( index > -1 )
				{
					m_nodesArr[ index ] = data;
					m_nodeIDs[ index ] = uniqueId;
				}
			}
		}

		public void Dump()
		{
			string data = string.Empty;

			for( int i = 0; i < m_nodesArr.Length; i++ )
			{
				data += m_nodesArr[ i ] + " " + m_nodeIDs[ i ] + '\n';
			}
			Debug.Log( data );
		}

		public string[] NodesArr { get { return m_nodesArr; } }
		public int[] NodeIds { get { return m_nodeIDs; } }
		public List<T> NodesList { get { return m_nodes; } }
		public int Count { get { return m_nodes.Count; } }
		public ParentGraph ContainerGraph { get { return m_containerGraph; } set { m_containerGraph = value; } }
	}
}
