using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;

namespace AmplifyShaderEditor
{
	[Serializable]
	public class PassVisibleOptionsItems
	{
		public bool Visible;
		public string Name;
		public int Idx = -1;
	}

	[Serializable]
	public class TemplatePassSelectorHelper
	{
		private const string Label = " Available Passes";

		[SerializeField]
		private bool m_foldout;

		[SerializeField]
		private PassVisibleOptionsItems[] m_currentPasses;

		[NonSerialized]
		private Dictionary<string, PassVisibleOptionsItems> m_currentPassesDict;

		[SerializeField]
		private int m_mainPassId;

		public void CopyFrom( TemplatePassSelectorHelper from )
		{
			for( int i = 0; i < from.AvailablePasses.Length; i++ )
			{
				SetPassVisible( from.AvailablePasses[ i ].Name, from.AvailablePasses[ i ].Visible );
			}
		}

		public void Setup( TemplateSubShader subShader )
		{
			if( m_currentPasses == null )
			{
				m_currentPassesDict = new Dictionary<string, PassVisibleOptionsItems>();
				m_currentPasses = new PassVisibleOptionsItems[ subShader.Passes.Count ];
				for( int i = 0; i < m_currentPasses.Length; i++ )
				{
					if( subShader.Passes[ i ].IsMainPass )
						m_mainPassId = i;

					m_currentPasses[ i ] = new PassVisibleOptionsItems() { Name = subShader.Passes[ i ].PassNameContainer.Data, Visible = true, Idx = i };
					m_currentPassesDict.Add( m_currentPasses[ i ].Name, m_currentPasses[ i ] );
				}
			}
		}

		public void Clear()
		{
			m_currentPasses = null;

			if( m_currentPassesDict != null )
				m_currentPassesDict.Clear();

			m_currentPassesDict = null;
		}

		public void Reset()
		{
			for ( int i = 0; i < m_currentPasses.Length; i++ )
			{
				m_currentPasses[ i ].Visible = true;
			}
		}

		public void Destroy()
		{
			m_currentPasses = null;

			if( m_currentPassesDict != null )
				m_currentPassesDict.Clear();

			m_currentPassesDict = null;
		}

		public void Draw( TemplateMultiPassMasterNode owner )
		{
			if( m_currentPasses.Length < 2 )
				return;

			NodeUtils.DrawNestedPropertyGroup( ref m_foldout, Label, () =>
			{
				for( int i = 0; i < m_currentPasses.Length; i++ )
				{
					EditorGUI.BeginChangeCheck();
					m_currentPasses[ i ].Visible = owner.EditorGUILayoutToggleLeft( m_currentPasses[ i ].Name, m_currentPasses[ i ].Visible );
					if( EditorGUI.EndChangeCheck() )
					{
						owner.ContainerGraph.GetMultiPassMasterNodes( owner.LODIndex)[ m_currentPasses[ i ].Idx ].IsInvisible = !m_currentPasses[ i ].Visible;
					}

				}
				EditorGUILayout.Space();
			} );
		}

		public void ReadFromString( ref uint index, ref string[] nodeParams )
		{
			int passAmount = Convert.ToInt32( nodeParams[ index++ ] );
			for( int i = 0; i < passAmount; i++ )
			{
				bool value = Convert.ToBoolean( nodeParams[ index++ ] );
				if( i < m_currentPasses.Length )
				{
					m_currentPasses[ i ].Visible = value;
				}
			}
		}

		public void WriteToString( ref string nodeInfo )
		{
			IOUtils.AddFieldValueToString( ref nodeInfo, m_currentPasses.Length );
			for( int i = 0; i < m_currentPasses.Length; i++ )
			{
				IOUtils.AddFieldValueToString( ref nodeInfo, m_currentPasses[ i ].Visible );
			}
		}

		public void SetPassVisible( string passName, bool visible )
		{
			bool refresh = false;
			if( m_currentPassesDict == null )
			{
				m_currentPassesDict = new Dictionary<string, PassVisibleOptionsItems>();
				refresh = true;
			}
			else if( m_currentPassesDict.Count != m_currentPasses.Length )
			{
				refresh = true;
			}

			if( refresh )
			{
				for( int i = 0; i < m_currentPasses.Length; i++ )
				{
					m_currentPassesDict.Add( m_currentPasses[ i ].Name, m_currentPasses[ i ] );
				}
			}

			if( m_currentPassesDict.ContainsKey( passName ) )
			{
				m_currentPassesDict[ passName ].Visible = visible;
			}
		}

		public int LastActivePass
		{
			get
			{
				if( m_currentPasses != null )
				{
					for( int i = m_currentPasses.Length - 1; i > -1; i-- )
					{
						if( m_currentPasses[ i ].Visible )
							return i;
					}
				}
				m_currentPasses[ m_mainPassId ].Visible = true;
				return m_mainPassId;
			}
		}
		public bool IsVisible( int passId ) { return m_currentPasses[ passId ].Visible; }
		private PassVisibleOptionsItems[] AvailablePasses { get { return m_currentPasses; } }
	}
}
