// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using UnityEngine;
using System;

namespace AmplifyShaderEditor
{
	[Serializable]
	public class TemplateRenderPlatformHelper : TemplateModuleData
	{
		[SerializeField]
		private string m_id;

		[SerializeField]
		private int m_index;

		[SerializeField]
		private bool[] m_renderingPlatforms = null;

		private void CommonInit( bool initialValue )
		{
			DataCheck = TemplateDataCheck.Valid;
			int renderPlatformLength = RenderingPlatformOpHelper.RenderingPlatformsInfo.Length;
			m_renderingPlatforms = new bool[ renderPlatformLength ];
			for( int i = 0 ; i < m_renderingPlatforms.Length ; i++ )
			{
				m_renderingPlatforms[ i ] = initialValue;
			}
		}

		public void InitByTag(int index)
		{
			m_id = TemplatesManager.TemplateRenderPlatformsTag;
			m_index = index;
			CommonInit( true );

		}

		public void InitByExcludeRenders( int index, string id )
		{
			m_id = id;
			m_index = index;
			CommonInit( true );
		}

		public void InitByOnlyRenders( int index , string id )
		{
			m_id = id;
			m_index = index;
			CommonInit( false );
		}

		public void SetupPlatform( string platformStr , bool value )
		{
			try
			{
				RenderPlatforms platform = (RenderPlatforms)Enum.Parse( typeof( RenderPlatforms ) , platformStr );
				int index = -1;
				if( RenderingPlatformOpHelper.PlatformToIndex.TryGetValue( platform , out index ) )
				{
					m_renderingPlatforms[ index ] = value;
				}
			}
			catch( Exception e )
			{
				Debug.LogException( e );
			}
		}

		public void Destroy()
		{
			m_renderingPlatforms = null;
		}

		public bool[] RenderingPlatforms { get { return m_renderingPlatforms; } }
		public string ID { get { return m_id; } }
		public int Index { get { return m_index; } set{ m_index = value; } }
	}
}
