using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;

namespace AmplifyShaderEditor
{
	public class InlinePropertyTable
	{
		// @diogo: Used to keep track of inline properties during Graph loading process, in order to resolve
		//         dependencies AFTER the meta data is parsed, not during the process, making it order agnostic.

		static List<InlineProperty> m_pool = new List<InlineProperty>( 32 );
		static List<InlineProperty> m_trackingTable = null;

		public static void Initialize()
		{
			m_trackingTable = m_pool; // keep memory allocated, despite empty list
		}

		public static void Register( InlineProperty prop )
		{
			if ( m_trackingTable != null )
			{
				m_trackingTable.Add( prop );
			}
		}

		public static void ResolveDependencies()
		{
			if ( m_trackingTable != null )
			{
				foreach ( var prop in m_trackingTable )
				{
					prop.TryResolveDependency();
				}

				m_trackingTable.Clear();
				m_trackingTable = null;
			}
		}
	}
}
