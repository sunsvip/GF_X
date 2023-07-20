// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using System;
using System.Collections.Generic;
using UnityEngine;

namespace AmplifyShaderEditor
{
	// PORT CONTROLLERS
	[Serializable]
	public class TemplateOptionPortItem
	{
		[SerializeField]
		private int m_portId = -1;

		[SerializeField]
		private TemplateOptionsItem m_options;

		public TemplateOptionPortItem( TemplateMultiPassMasterNode owner, TemplateOptionsItem options )
		{
			m_options = options;
			InputPort port = owner.InputPorts.Find( x => x.Name.Equals( options.Name ) );
			if( port != null )
			{
				m_portId = port.PortId;
			}
		}

		public void FillDataCollector( TemplateMultiPassMasterNode owner, ref MasterNodeDataCollector dataCollector )
		{
			InputPort port = null;
			if( m_portId > -1 )
			{
				port = owner.GetInputPortByUniqueId( m_portId );
			}
			else
			{
				port = owner.InputPorts.Find( x => x.Name.Equals( m_options.Name ) );
			}

			if( port != null )
			{
				int optionId = port.HasOwnOrLinkConnection ? 0 : 1;
				for( int i = 0; i < m_options.ActionsPerOption[ optionId ].Length; i++ )
				{
					switch( m_options.ActionsPerOption[ optionId ][ i ].ActionType )
					{
						case AseOptionsActionType.SetDefine:
						{
							List<TemplateMultiPassMasterNode> nodes = owner.ContainerGraph.GetMultiPassMasterNodes( owner.LODIndex );
							int count = nodes.Count;
							for( int nodeIdx = 0; nodeIdx < count; nodeIdx++ )
							{
								string defineValue = string.Empty;
								bool isPragma = false;
								if( m_options.ActionsPerOption[ optionId ][ i ].ActionData.StartsWith( "pragma" ) )
								{
									defineValue = "#" + m_options.ActionsPerOption[ optionId ][ i ].ActionData;
									isPragma = true;
								}
								else
								{
									defineValue = "#define " + m_options.ActionsPerOption[ optionId ][ i ].ActionData;
								}

								nodes[ nodeIdx ].OptionsDefineContainer.AddDirective( defineValue ,false, isPragma );
							}
							//dataCollector.AddToDefines( -1, m_options.ActionsPerOption[ optionId ][ i ].ActionData );
						}
						break;
						case AseOptionsActionType.SetUndefine:
						{
							List<TemplateMultiPassMasterNode> nodes = owner.ContainerGraph.GetMultiPassMasterNodes( owner.LODIndex );
							int count = nodes.Count;
							for( int nodeIdx = 0; nodeIdx < count; nodeIdx++ )
							{
								nodes[ nodeIdx ].OptionsDefineContainer.AddDirective( "#undef " + m_options.ActionsPerOption[ optionId ][ i ].ActionData, false );
							}
							//dataCollector.AddToDefines( -1, m_options.ActionsPerOption[ optionId ][ i ].ActionData, false );
						}
						break;
						case AseOptionsActionType.SetShaderProperty:
						{
							TemplateShaderPropertyData data = owner.CurrentTemplate.GetShaderPropertyData( m_options.ActionsPerOption[ optionId ][ i ].ActionData );
							if( data != null )
							{
								string newPropertyValue = data.CreatePropertyForValue( m_options.ActionsPerOption[ optionId ][ i ].ActionBuffer );
								owner.CurrentTemplate.IdManager.SetReplacementText( data.FullValue, newPropertyValue );
							}
						}
						break;
					}
				}
			}
		}

		public void SubShaderFillDataCollector( TemplateMultiPassMasterNode owner, ref MasterNodeDataCollector dataCollector )
		{

			//TemplateMultiPassMasterNode targetNode = string.IsNullOrEmpty(m_options.Id) ? owner:owner.ContainerGraph.GetMasterNodeOfPass( m_options.Id , owner.LODIndex );
			TemplateMultiPassMasterNode targetNode = string.IsNullOrEmpty( m_options.Id ) ?
														owner.ContainerGraph.GetMainMasterNodeOfLOD( owner.LODIndex ) :
														owner.ContainerGraph.GetMasterNodeOfPass( m_options.Id , owner.LODIndex );

			InputPort port = null;
			if( m_portId > -1 )
			{
				port = targetNode.GetInputPortByUniqueId( m_portId );
			}
			else
			{
				port = targetNode.InputPorts.Find( x => x.Name.Equals( m_options.Name ) );
			}

			if( port != null )
			{
				int optionId = port.HasOwnOrLinkConnection ? 0 : 1;
				for( int i = 0; i < m_options.ActionsPerOption[ optionId ].Length; i++ )
				{
					if( string.IsNullOrEmpty( m_options.ActionsPerOption[ optionId ][ i ].PassName ) ||
						m_options.ActionsPerOption[ optionId ][ i ].PassName.Equals( owner.PassName ) )
					{
						switch( m_options.ActionsPerOption[ optionId ][ i ].ActionType )
						{
							case AseOptionsActionType.SetDefine:
							{
								string defineValue = string.Empty;
								bool isPragma = false;
								if( m_options.ActionsPerOption[ optionId ][ i ].ActionData.StartsWith( "pragma" ) )
								{
									defineValue = "#" + m_options.ActionsPerOption[ optionId ][ i ].ActionData;
									isPragma = true;
								}
								else
								{
									defineValue = "#define " + m_options.ActionsPerOption[ optionId ][ i ].ActionData;
								}

								owner.OptionsDefineContainer.AddDirective( defineValue ,true,  isPragma );
							}
							break;
							case AseOptionsActionType.SetUndefine:
							{
								owner.OptionsDefineContainer.AddDirective( "#undef " + m_options.ActionsPerOption[ optionId ][ i ].ActionData, true );
							}
							break;
							case AseOptionsActionType.SetShaderProperty:
							{
								TemplateShaderPropertyData data = owner.CurrentTemplate.GetShaderPropertyData( m_options.ActionsPerOption[ optionId ][ i ].ActionData );
								if( data != null )
								{
									string newPropertyValue = data.CreatePropertyForValue( m_options.ActionsPerOption[ optionId ][ i ].ActionBuffer );
									owner.CurrentTemplate.IdManager.SetReplacementText( data.FullValue, newPropertyValue );
								}
							}
							break;
						}
					}
				}
			}
		}

		public void CheckImediateActionsForPort( TemplateMultiPassMasterNode owner, int portId )
		{
			if( portId != m_portId )
				return;

			InputPort port = null;
			if( m_portId > -1 )
			{
				port = owner.GetInputPortByUniqueId( m_portId );
			}
			else
			{
				port = owner.InputPorts.Find( x => x.Name.Equals( m_options.Name ) );
			}

			if( port != null )
			{
				int optionId = port.HasOwnOrLinkConnection ? 0 : 1;
				for( int i = 0; i < m_options.ActionsPerOption[ optionId ].Length; i++ )
				{
					switch( m_options.ActionsPerOption[ optionId ][ i ].ActionType )
					{
						case AseOptionsActionType.SetPortName:
						{
							port.Name = m_options.ActionsPerOption[ optionId ][ i ].ActionData;
							owner.SizeIsDirty = true;
						}
						break;
					}
				}
			}
		}
		public TemplateOptionsItem Options { get { return m_options; } }
	}
}
