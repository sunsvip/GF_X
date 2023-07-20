using UnityEngine;
using System;
using System.Collections.Generic;

namespace AmplifyShaderEditor
{
	[Serializable]
	public class TemplateOptionsDefinesContainer
	{
		[SerializeField]
		private List<PropertyDataCollector> m_directivesList = new List<PropertyDataCollector>();

		[NonSerialized]
		private Dictionary<string, PropertyDataCollector> m_directivesDict = new Dictionary<string, PropertyDataCollector>();

		void Refresh()
		{
			if( m_directivesDict.Count != m_directivesList.Count )
			{
				m_directivesDict.Clear();
				for( int i = 0; i < m_directivesList.Count; i++ )
				{
					m_directivesDict.Add( m_directivesList[ i ].PropertyName, m_directivesList[ i ] );
				}
			}
		}

		public void RemoveTemporaries()
		{
			List<PropertyDataCollector> temporaries = m_directivesList.FindAll( ( x ) => ( x.NodeId == 1 ) );
			for( int i = 0; i < temporaries.Count; i++ )
			{
				m_directivesList.Remove( temporaries[ i ] );
				m_directivesDict.Remove( temporaries[ i ].PropertyName );
			}
		}

		public void AddDirective( string directive , bool temporary , bool isPragma = false )
		{
			Refresh();
			if( !m_directivesDict.ContainsKey( directive ) )
			{
				int nodeId = temporary ? 1 : 0;
				PropertyDataCollector data = new PropertyDataCollector( nodeId, directive,-1, isPragma );
				m_directivesDict.Add( directive, data );
				m_directivesList.Add( data );
			}
		}

		public void RemoveDirective( string directive )
		{
			Refresh();
			if( m_directivesDict.ContainsKey( directive ) )
			{
				m_directivesList.Remove( m_directivesDict[directive] );
				m_directivesDict.Remove( directive );
			}
		}

		public void Destroy()
		{
			m_directivesDict.Clear();
			m_directivesDict = null;
			m_directivesList.Clear();
			m_directivesList = null;
		}
		public List<PropertyDataCollector> DefinesList { get { return m_directivesList; } }
	}
}
