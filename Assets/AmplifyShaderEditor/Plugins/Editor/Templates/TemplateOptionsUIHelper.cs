// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;

namespace AmplifyShaderEditor
{
	[Serializable]
	public class TemplateOptionsUIHelper
	{
		public struct ReadOptions
		{
			public string Name;
			public string Selection;
			public Int64 Timestamp;
		}
		private const string CustomOptionsLabel = " Custom Options";

		private bool m_isSubShader = false;

		[SerializeField]
		private bool m_passCustomOptionsFoldout = true;

		[SerializeField]
		private string m_passCustomOptionsLabel = CustomOptionsLabel;

		[SerializeField]
		private int m_passCustomOptionsSizeCheck = 0;

		[SerializeField]
		private List<TemplateOptionUIItem> m_passCustomOptionsUI = new List<TemplateOptionUIItem>();

		[NonSerialized]
		private Dictionary<string, TemplateOptionUIItem> m_passCustomOptionsUIDict = new Dictionary<string, TemplateOptionUIItem>();

		[NonSerialized]
		private TemplateMultiPassMasterNode m_owner;

		[NonSerialized]
		private List<ReadOptions> m_readOptions = null;

		[SerializeField]
		private List<TemplateOptionPortItem> m_passCustomOptionsPorts = new List<TemplateOptionPortItem>();

		public TemplateOptionsUIHelper( bool isSubShader )
		{
			m_isSubShader = isSubShader;
		}

		public void CopyOptionsValuesFrom( TemplateOptionsUIHelper origin )
		{
			for( int i = 0; i < origin.PassCustomOptionsUI.Count; i++ )
			{
				m_passCustomOptionsUI[ i ].CopyValuesFrom( origin.PassCustomOptionsUI[ i ] );
			}
		}

		public void Destroy()
		{
			for( int i = 0; i < m_passCustomOptionsUI.Count; i++ )
			{
				m_passCustomOptionsUI[ i ].Destroy();
			}

			m_passCustomOptionsUI.Clear();
			m_passCustomOptionsUI = null;

			m_passCustomOptionsUIDict.Clear();
			m_passCustomOptionsUIDict = null;

			m_passCustomOptionsPorts.Clear();
			m_passCustomOptionsPorts = null;
		}

		public void DrawCustomOptions( TemplateMultiPassMasterNode owner )
		{
			m_owner = owner;
			
			if( m_passCustomOptionsUI.Count > 0 )
			{
				NodeUtils.DrawNestedPropertyGroup( ref m_passCustomOptionsFoldout, m_passCustomOptionsLabel, DrawCustomOptionsBlock );
			}
		}

		public void DrawCustomOptionsBlock()
		{
			float currWidth = EditorGUIUtility.labelWidth;
			float size = Mathf.Max( UIUtils.CurrentWindow.ParametersWindow.TransformedArea.width * 0.385f, 0 );
			EditorGUIUtility.labelWidth = size;
			for( int i = 0; i < m_passCustomOptionsUI.Count; i++ )
			{
				m_passCustomOptionsUI[ i ].Draw( m_owner );
			}
			EditorGUILayout.Space();
			EditorGUIUtility.labelWidth = currWidth;
		}

		public void OnCustomOptionSelected( bool actionFromUser, bool isRefreshing, bool invertAction, TemplateMultiPassMasterNode owner, TemplateOptionUIItem uiItem, params TemplateActionItem[] validActions )
		{
			uiItem.CheckOnExecute = false;
			for( int i = 0; i < validActions.Length; i++ )
			{
				AseOptionsActionType actionType = validActions[ i ].ActionType;
				if( invertAction )
				{
					if( !TemplateOptionsToolsHelper.InvertAction( validActions[ i ].ActionType, ref actionType ) )
					{
						continue;
					}
				}


				switch( actionType )
				{
					case AseOptionsActionType.ShowOption:
					{
						TemplateOptionUIItem item = m_passCustomOptionsUI.Find( x => ( x.Options.Name.Equals( validActions[ i ].ActionData ) ) );
						if( item != null )
						{
							if( isRefreshing )
							{
								string optionId = validActions[ i ].PassName + validActions[ i ].ActionData + "Option";
								owner.ContainerGraph.ParentWindow.TemplatesManagerInstance.SetOptionsValue( optionId, true );
							}

							// this prevents options from showing up when loading by checking if they were hidden by another option
							// it works on the assumption that an option that may possible hide this one is checked first
							if( !isRefreshing )
								item.IsVisible = true;
							else if( item.WasVisible )
								item.IsVisible = true;

							if( !invertAction && validActions[ i ].ActionDataIdx > -1 )
								item.CurrentOption = validActions[ i ].ActionDataIdx;

							item.CheckEnDisable( actionFromUser );
						}
						else
						{
							Debug.LogFormat( "Could not find Option {0} for action '{1}' on template {2}", validActions[ i ].ActionData, validActions[ i ].ActionType, owner.CurrentTemplate.DefaultShaderName );
						}
					}
					break;
					case AseOptionsActionType.HideOption:
					{
						TemplateOptionUIItem item = m_passCustomOptionsUI.Find( x => ( x.Options.Name.Equals( validActions[ i ].ActionData ) ) );
						if( item != null )
						{
							bool flag = false;
							if( isRefreshing )
							{
								string optionId = validActions[ i ].PassName + validActions[ i ].ActionData + "Option";
								flag = owner.ContainerGraph.ParentWindow.TemplatesManagerInstance.SetOptionsValue( optionId, false );
							}

							item.IsVisible = false || flag;
							if( !invertAction && validActions[ i ].ActionDataIdx > -1 )
								item.CurrentOption = validActions[ i ].ActionDataIdx;

							item.CheckEnDisable( actionFromUser );
						}
						else
						{
							Debug.LogFormat( "Could not find Option {0} for action '{1}' on template {2}", validActions[ i ].ActionData, validActions[ i ].ActionType, owner.CurrentTemplate.DefaultShaderName );
						}
					}
					break;
					case AseOptionsActionType.SetOption:
					{
						if( !uiItem.IsVisible )
							break;

						TemplateOptionUIItem item = m_passCustomOptionsUI.Find( x => ( x.Options.Name.Equals( validActions[ i ].ActionData ) ) );
						if( item != null )
						{
							item.CurrentOption = validActions[ i ].ActionDataIdx;
							item.Update( isRefreshing );
						}
						else
						{
							Debug.LogFormat( "Could not find Option {0} for action '{1}' on template {2}", validActions[ i ].ActionData, validActions[ i ].ActionType, owner.CurrentTemplate.DefaultShaderName );
						}
					}
					break;
					case AseOptionsActionType.HidePort:
					{
						TemplateMultiPassMasterNode passMasterNode = owner;
						if( !string.IsNullOrEmpty( validActions[ i ].PassName ) )
						{
							passMasterNode = owner.ContainerGraph.GetMasterNodeOfPass( validActions[ i ].PassName,owner.LODIndex );
						}

						if( passMasterNode != null )
						{
							InputPort port = validActions[ i ].ActionDataIdx > -1 ?
								passMasterNode.GetInputPortByUniqueId( validActions[ i ].ActionDataIdx ) :
								passMasterNode.InputPorts.Find( x => x.Name.Equals( validActions[ i ].ActionData ) );
							if( port != null )
							{
								if( isRefreshing )
								{
									string optionId = validActions[ i ].PassName + port.Name;
									owner.ContainerGraph.ParentWindow.TemplatesManagerInstance.SetOptionsValue( optionId, port.IsConnected );
									port.Visible = port.IsConnected;
								}
								else
								{
									port.Visible = false;
								}
								passMasterNode.SizeIsDirty = true;
							}
							else
							{
								Debug.LogFormat( "Could not find port {0},{1} for action '{2}' on template {3}", validActions[ i ].ActionDataIdx, validActions[ i ].ActionData, validActions[ i ].ActionType, owner.CurrentTemplate.DefaultShaderName );
							}
						}
						else
						{
							Debug.LogFormat( "Could not find pass {0} for action {1} '{2}' on template {3}", validActions[ i ].PassName, validActions[ i ].ActionType, validActions[ i ].ActionData, owner.CurrentTemplate.DefaultShaderName );
						}
					}
					break;
					case AseOptionsActionType.ShowPort:
					{
						if( !uiItem.IsVisible )
							break;

						TemplateMultiPassMasterNode passMasterNode = owner;
						if( !string.IsNullOrEmpty( validActions[ i ].PassName ) )
						{
							passMasterNode = owner.ContainerGraph.GetMasterNodeOfPass( validActions[ i ].PassName, owner.LODIndex );
						}

						if( passMasterNode != null )
						{
							InputPort port = validActions[ i ].ActionDataIdx > -1 ?
								passMasterNode.GetInputPortByUniqueId( validActions[ i ].ActionDataIdx ) :
								passMasterNode.InputPorts.Find( x => x.Name.Equals( validActions[ i ].ActionData ) );
							if( port != null )
							{
								if( isRefreshing )
								{
									string optionId = validActions[ i ].PassName + port.Name;
									owner.ContainerGraph.ParentWindow.TemplatesManagerInstance.SetOptionsValue( optionId, true );
								}

								port.Visible = true;
								passMasterNode.SizeIsDirty = true;
							}
							else
							{
								Debug.LogFormat( "Could not find port {0},{1} for action '{2}' on template {3}", validActions[ i ].ActionDataIdx, validActions[ i ].ActionData, validActions[ i ].ActionType, owner.CurrentTemplate.DefaultShaderName );
							}
						}
						else
						{
							Debug.LogFormat( "Could not find pass {0} for action {1} '{2}' on template {3}", validActions[ i ].PassName, validActions[ i ].ActionType, validActions[ i ].ActionData, owner.CurrentTemplate.DefaultShaderName );
						}
					}
					break;
					case AseOptionsActionType.SetPortName:
					{
						if( !uiItem.IsVisible )
							break;

						TemplateMultiPassMasterNode passMasterNode = owner;
						if( !string.IsNullOrEmpty( validActions[ i ].PassName ) )
						{
							passMasterNode = owner.ContainerGraph.GetMasterNodeOfPass( validActions[ i ].PassName, owner.LODIndex );
						}

						if( passMasterNode != null )
						{
							InputPort port = passMasterNode.GetInputPortByUniqueId( validActions[ i ].ActionDataIdx );
							if( port != null )
							{
								port.Name = validActions[ i ].ActionData;
								passMasterNode.SizeIsDirty = true;
							}
							else
							{
								Debug.LogFormat( "Could not find port {0},{1} for action '{2}' on template {3}", validActions[ i ].ActionDataIdx, validActions[ i ].ActionType, validActions[ i ].ActionData, owner.CurrentTemplate.DefaultShaderName );
							}
						}
						else
						{
							Debug.LogFormat( "Could not find pass {0}, {1} for action '{2}' on template {3}", validActions[ i ].PassName, validActions[ i ].ActionType, validActions[ i ].ActionData, owner.CurrentTemplate.DefaultShaderName );
						}
					}
					break;
					case AseOptionsActionType.SetDefine:
					{
						if( !uiItem.IsVisible )
						{
							uiItem.CheckOnExecute = true;
							break;
						}

						//Debug.Log( "DEFINE " + validActions[ i ].ActionData );
						if( validActions[ i ].AllPasses )
						{
							string actionData = validActions[ i ].ActionData;
							string defineValue = string.Empty;
							bool isPragma = false;
							if( actionData.StartsWith( "pragma" ) )
							{
								defineValue = "#" + actionData;
								isPragma = true;
							}
							else
							{
								defineValue = "#define " + validActions[ i ].ActionData;
							}
							if( isRefreshing )
							{
								owner.ContainerGraph.ParentWindow.TemplatesManagerInstance.SetOptionsValue( defineValue, true );
							}
							List<TemplateMultiPassMasterNode> nodes = owner.ContainerGraph.GetMultiPassMasterNodes( owner.LODIndex );
							int count = nodes.Count;
							for( int nodeIdx = 0; nodeIdx < count; nodeIdx++ )
							{
								nodes[ nodeIdx ].OptionsDefineContainer.AddDirective( defineValue, false, isPragma );
							}
						}
						else if( !string.IsNullOrEmpty( validActions[ i ].PassName ) )
						{
							TemplateMultiPassMasterNode passMasterNode = owner.ContainerGraph.GetMasterNodeOfPass( validActions[ i ].PassName, owner.LODIndex );
							if( passMasterNode != null )
							{
								string actionData = validActions[ i ].ActionData;
								string defineValue = string.Empty;
								bool isPragma = false;
								if( actionData.StartsWith( "pragma" ) )
								{
									defineValue = "#" + actionData;
									isPragma = true;
								}
								else
								{
									defineValue = "#define " + validActions[ i ].ActionData;
								}
								if( isRefreshing )
								{
									string optionsId = validActions[ i ].PassName + defineValue;
									owner.ContainerGraph.ParentWindow.TemplatesManagerInstance.SetOptionsValue( optionsId, true );
								}
								passMasterNode.OptionsDefineContainer.AddDirective( defineValue, false, isPragma );
							}
							else
							{
								Debug.LogFormat( "Could not find pass {0} for action {1} '{2}' on template {3}", validActions[ i ].PassName, validActions[ i ].ActionType, validActions[ i ].ActionData, owner.CurrentTemplate.DefaultShaderName );
							}
						}
						else
						{
							uiItem.CheckOnExecute = true;
						}
					}
					break;
					case AseOptionsActionType.RemoveDefine:
					{
						//Debug.Log( "UNDEFINE " + validActions[ i ].ActionData );
						if( validActions[ i ].AllPasses )
						{
							string actionData = validActions[ i ].ActionData;
							string defineValue = string.Empty;
							if( actionData.StartsWith( "pragma" ) )
							{
								defineValue = "#" + actionData;
							}
							else
							{
								defineValue = "#define " + validActions[ i ].ActionData;
							}

							bool flag = false;
							if( isRefreshing )
							{
								flag = owner.ContainerGraph.ParentWindow.TemplatesManagerInstance.SetOptionsValue( defineValue, false );
							}

							if( !flag )
							{
								List<TemplateMultiPassMasterNode> nodes = owner.ContainerGraph.GetMultiPassMasterNodes( owner.LODIndex );
								int count = nodes.Count;
								for( int nodeIdx = 0; nodeIdx < count; nodeIdx++ )
								{
									nodes[ nodeIdx ].OptionsDefineContainer.RemoveDirective( defineValue );
								}
							}
						}
						else if( !string.IsNullOrEmpty( validActions[ i ].PassName ) )
						{
							TemplateMultiPassMasterNode passMasterNode = owner.ContainerGraph.GetMasterNodeOfPass( validActions[ i ].PassName, owner.LODIndex );
							if( passMasterNode != null )
							{
								string actionData = validActions[ i ].ActionData;
								string defineValue = string.Empty;
								if( actionData.StartsWith( "pragma" ) )
								{
									defineValue = "#" + actionData;
								}
								else
								{
									defineValue = "#define " + validActions[ i ].ActionData;
								}
								bool flag = false;
								if( isRefreshing )
								{
									string optionId = validActions[ i ].PassName + defineValue;
									flag = owner.ContainerGraph.ParentWindow.TemplatesManagerInstance.SetOptionsValue( optionId, false );
								}
								if( !flag )
								{
									passMasterNode.OptionsDefineContainer.RemoveDirective( defineValue );
								}
							}
							else
							{
								Debug.LogFormat( "Could not find pass {0} for action {1} '{2}' on template {3}", validActions[ i ].PassName, validActions[ i ].ActionType, validActions[ i ].ActionData, owner.CurrentTemplate.DefaultShaderName );
							}
						}
						else
						{
							uiItem.CheckOnExecute = false;
						}
					}
					break;
					case AseOptionsActionType.SetUndefine:
					{
						if( !uiItem.IsVisible )
						{
							uiItem.CheckOnExecute = true;
							break;
						}

						if( validActions[ i ].AllPasses )
						{
							string defineValue = "#undef " + validActions[ i ].ActionData;
							if( isRefreshing )
							{
								owner.ContainerGraph.ParentWindow.TemplatesManagerInstance.SetOptionsValue( defineValue, true );
							}
							List<TemplateMultiPassMasterNode> nodes = owner.ContainerGraph.GetMultiPassMasterNodes(owner.LODIndex);
							int count = nodes.Count;
							for( int nodeIdx = 0; nodeIdx < count; nodeIdx++ )
							{
								nodes[ nodeIdx ].OptionsDefineContainer.AddDirective( defineValue, false );
							}
						}
						else if( !string.IsNullOrEmpty( validActions[ i ].PassName ) )
						{
							TemplateMultiPassMasterNode passMasterNode = owner.ContainerGraph.GetMasterNodeOfPass( validActions[ i ].PassName, owner.LODIndex );
							if( passMasterNode != null )
							{
								string defineValue = "#undef " + validActions[ i ].ActionData;
								if( isRefreshing )
								{
									string optionsId = validActions[ i ].PassName + defineValue;
									owner.ContainerGraph.ParentWindow.TemplatesManagerInstance.SetOptionsValue( optionsId, true );
								}
								passMasterNode.OptionsDefineContainer.AddDirective( defineValue, false );
							}
							else
							{
								Debug.LogFormat( "Could not find pass {0} for action {1} '{2}' on template {3}", validActions[ i ].PassName, validActions[ i ].ActionType, validActions[ i ].ActionData, owner.CurrentTemplate.DefaultShaderName );
							}
						}
						else
						{
							uiItem.CheckOnExecute = true;
						}
					}
					break;
					case AseOptionsActionType.RemoveUndefine:
					{
						if( validActions[ i ].AllPasses )
						{
							string defineValue = "#undef " + validActions[ i ].ActionData;
							bool flag = false;
							if( isRefreshing )
							{
								flag = owner.ContainerGraph.ParentWindow.TemplatesManagerInstance.SetOptionsValue( defineValue, false );
							}

							if( !flag )
							{
								List<TemplateMultiPassMasterNode> nodes = owner.ContainerGraph.GetMultiPassMasterNodes( owner.LODIndex );
								int count = nodes.Count;
								for( int nodeIdx = 0; nodeIdx < count; nodeIdx++ )
								{
									nodes[ nodeIdx ].OptionsDefineContainer.RemoveDirective( defineValue );
								}
							}
						}
						else if( !string.IsNullOrEmpty( validActions[ i ].PassName ) )
						{
							TemplateMultiPassMasterNode passMasterNode = owner.ContainerGraph.GetMasterNodeOfPass( validActions[ i ].PassName, owner.LODIndex );
							if( passMasterNode != null )
							{
								bool flag = false;
								string defineValue = "#undef " + validActions[ i ].ActionData;
								if( isRefreshing )
								{
									string optionId = validActions[ i ].PassName + defineValue;
									flag = owner.ContainerGraph.ParentWindow.TemplatesManagerInstance.SetOptionsValue( optionId, false );
								}

								if( !flag )
								{
									passMasterNode.OptionsDefineContainer.RemoveDirective( defineValue );
								}
							}
							else
							{
								Debug.LogFormat( "Could not find pass {0} for action {1} '{2}' on template {3}", validActions[ i ].PassName, validActions[ i ].ActionType, validActions[ i ].ActionData, owner.CurrentTemplate.DefaultShaderName );
							}
						}
						else
						{
							uiItem.CheckOnExecute = false;
						}
					}
					break;
					case AseOptionsActionType.ExcludePass:
					{
						string optionId = validActions[ i ].ActionData + "Pass";
						//bool flag = isRefreshing ? owner.ContainerGraph.ParentWindow.TemplatesManagerInstance.SetOptionsValue( optionId, false ) : false;
						//if( !flag )
						//	owner.SetPassVisible( validActions[ i ].ActionData, false );
						owner.ContainerGraph.ParentWindow.TemplatesManagerInstance.SetOptionsValue( optionId , false ) ;
						owner.SetPassVisible( validActions[ i ].ActionData , false );
					}
					break;


					case AseOptionsActionType.IncludePass:
					{
						if( !uiItem.IsVisible )
							break;

						string optionId = validActions[ i ].ActionData + "Pass";
						owner.ContainerGraph.ParentWindow.TemplatesManagerInstance.SetOptionsValue( optionId, true );
						owner.SetPassVisible( validActions[ i ].ActionData, true );
					}
					break;
					case AseOptionsActionType.SetPropertyOnPass:
					{
						//Debug.Log( "PASSPROP " + validActions[ i ].ActionData );
						//Refresh happens on hotcode reload and shader load and in those situation
						// The property own serialization handles its setup
						if( isRefreshing )
							continue;

						if( !string.IsNullOrEmpty( validActions[ i ].PassName ) )
						{
							TemplateMultiPassMasterNode passMasterNode = owner.ContainerGraph.GetMasterNodeOfPass( validActions[ i ].PassName, owner.LODIndex );
							if( passMasterNode != null )
							{
								passMasterNode.SetPropertyActionFromItem( actionFromUser, passMasterNode.PassModule, validActions[ i ] );
							}
							else
							{
								Debug.LogFormat( "Could not find pass {0} for action {1} '{2}' on template {3}", validActions[ i ].PassName, validActions[ i ].ActionType, validActions[ i ].ActionData, owner.CurrentTemplate.DefaultShaderName );
							}
						}
						else
						{
							owner.SetPropertyActionFromItem( actionFromUser, owner.PassModule, validActions[ i ] );
						}
					}
					break;
					case AseOptionsActionType.SetPropertyOnSubShader:
					{
						//Refresh happens on hotcode reload and shader load and in those situation
						// The property own serialization handles its setup
						if( isRefreshing )
							continue;

						owner.SetPropertyActionFromItem( actionFromUser, owner.SubShaderModule, validActions[ i ] );
					}
					break;
					case AseOptionsActionType.SetShaderProperty:
					{
						//This action is only check when shader is compiled over 
						//the TemplateMultiPassMasterNode via the on CheckPropertyChangesOnOptions() method
					}
					break;
					case AseOptionsActionType.ExcludeAllPassesBut:
					{
						//This action is only check when shader is compiled over 
						//the TemplateMultiPassMasterNode via the on CheckExcludeAllPassOptions() method
					}
					break;
					case AseOptionsActionType.SetMaterialProperty:
					{
						if( isRefreshing )
							continue;

						if( !uiItem.IsVisible )
							break;

						if( owner.ContainerGraph.CurrentMaterial != null )
						{
							string prop = validActions[ i ].ActionData;
							if( owner.ContainerGraph.CurrentMaterial.HasProperty( prop ) )
							{
								if( uiItem.Options.UIWidget == AseOptionsUIWidget.Float || uiItem.Options.UIWidget == AseOptionsUIWidget.FloatRange )
									owner.ContainerGraph.CurrentMaterial.SetFloat( prop, uiItem.CurrentFieldValue );
								else
									owner.ContainerGraph.CurrentMaterial.SetInt( prop, (int)uiItem.CurrentFieldValue );

								if( ASEMaterialInspector.Instance != null )
									ASEMaterialInspector.Instance.Repaint();
							}
						}
					}
					break;
				}
			}
		}

		public void SetupCustomOptionsFromTemplate( TemplateMultiPassMasterNode owner, bool newTemplate )
		{
			TemplateOptionsContainer customOptionsContainer = m_isSubShader ? owner.SubShader.CustomOptionsContainer : owner.Pass.CustomOptionsContainer;

			if( !newTemplate && customOptionsContainer.Body.Length == m_passCustomOptionsSizeCheck )
			{
				for( int i = 0; i < m_passCustomOptionsUI.Count; i++ )
				{
					if( m_passCustomOptionsUI[ i ].EmptyEvent )
					{
						if( m_isSubShader )
						{
							m_passCustomOptionsUI[ i ].OnActionPerformedEvt += owner.OnCustomSubShaderOptionSelected;
						}
						else
						{
							m_passCustomOptionsUI[ i ].OnActionPerformedEvt += owner.OnCustomPassOptionSelected;
						}
					}
				}
				return;
			}

			m_passCustomOptionsLabel = string.IsNullOrEmpty( customOptionsContainer.Name ) ? CustomOptionsLabel : " " + customOptionsContainer.Name;

			for( int i = 0; i < m_passCustomOptionsUI.Count; i++ )
			{
				m_passCustomOptionsUI[ i ].Destroy();
			}

			m_passCustomOptionsUI.Clear();
			m_passCustomOptionsUIDict.Clear();
			m_passCustomOptionsPorts.Clear();

			if( customOptionsContainer.Enabled )
			{
				m_passCustomOptionsSizeCheck = customOptionsContainer.Body.Length;
				for( int i = 0; i < customOptionsContainer.Options.Length; i++ )
				{
					switch( customOptionsContainer.Options[ i ].Type )
					{
						case AseOptionsType.Option:
						{
							TemplateOptionUIItem item = new TemplateOptionUIItem( customOptionsContainer.Options[ i ] );
							if( m_isSubShader )
							{
								item.OnActionPerformedEvt += owner.OnCustomSubShaderOptionSelected;
							}
							else
							{
								item.OnActionPerformedEvt += owner.OnCustomPassOptionSelected;
							}

							m_passCustomOptionsUI.Add( item );
							m_passCustomOptionsUIDict.Add( customOptionsContainer.Options[ i ].Id, item );
						}
						break;
						case AseOptionsType.Port:
						{
							TemplateOptionPortItem item = new TemplateOptionPortItem( owner, customOptionsContainer.Options[ i ] );
							m_passCustomOptionsPorts.Add( item );
							//if( m_isSubShader )
							//{
							//	if( string.IsNullOrEmpty( customOptionsContainer.Options[ i ].Id ) )
							//	{
							//		//No pass name selected. inject on all passes
							//		TemplateOptionPortItem item = new TemplateOptionPortItem( owner, customOptionsContainer.Options[ i ] );
							//		m_passCustomOptionsPorts.Add( item );
							//	}
							//	else if( customOptionsContainer.Options[ i ].Id.Equals( owner.PassName ) )
							//	{
							//		TemplateOptionPortItem item = new TemplateOptionPortItem( owner, customOptionsContainer.Options[ i ] );
							//		m_passCustomOptionsPorts.Add( item );
							//	}
							//}
							//else
							//{
							//	TemplateOptionPortItem item = new TemplateOptionPortItem( owner, customOptionsContainer.Options[ i ] );
							//	m_passCustomOptionsPorts.Add( item );
							//}
						}
						break;
						case AseOptionsType.Field:
						{
							TemplateOptionUIItem item = new TemplateOptionUIItem( customOptionsContainer.Options[ i ] );
							if( m_isSubShader )
							{
								item.OnActionPerformedEvt += owner.OnCustomSubShaderOptionSelected;
							}
							else
							{
								item.OnActionPerformedEvt += owner.OnCustomPassOptionSelected;
							}

							m_passCustomOptionsUI.Add( item );
							m_passCustomOptionsUIDict.Add( customOptionsContainer.Options[ i ].Id, item );
						}
						break;
					}
				}
			}
			else
			{
				m_passCustomOptionsSizeCheck = 0;
			}
		}

		public void SetCustomOptionsInfo( TemplateMultiPassMasterNode masterNode, ref MasterNodeDataCollector dataCollector )
		{
			if( masterNode == null )
				return;

			for( int i = 0; i < m_passCustomOptionsUI.Count; i++ )
			{
				m_passCustomOptionsUI[ i ].FillDataCollector( ref dataCollector );
			}

			for( int i = 0; i < m_passCustomOptionsPorts.Count; i++ )
			{
				m_passCustomOptionsPorts[ i ].FillDataCollector( masterNode, ref dataCollector );
			}
		}

		public void CheckImediateActionsForPort( TemplateMultiPassMasterNode masterNode , int portId )
		{
			for( int i = 0; i < m_passCustomOptionsPorts.Count; i++ )
			{
				m_passCustomOptionsPorts[ i ].CheckImediateActionsForPort( masterNode, portId );
			}
		}

		public void SetSubShaderCustomOptionsPortsInfo( TemplateMultiPassMasterNode masterNode, ref MasterNodeDataCollector dataCollector  )
		{
			if( masterNode == null )
				return;

			
			//for( int i = 0; i < m_passCustomOptionsPorts.Count; i++ )
			//{
			//	if( string.IsNullOrEmpty( m_passCustomOptionsPorts[ i ].Options.Id ) ||
			//		masterNode.PassUniqueName.Equals( m_passCustomOptionsPorts[ i ].Options.Id ) )
			//	{
			//		m_passCustomOptionsPorts[ i ].FillDataCollector( masterNode, ref dataCollector );
			//	}
			//}
			
			for( int i = 0; i < m_passCustomOptionsPorts.Count; i++ )
			{	
				m_passCustomOptionsPorts[ i ].SubShaderFillDataCollector( masterNode, ref dataCollector );	
			}
		}

		public void RefreshCustomOptionsDict()
		{
			if( m_passCustomOptionsUIDict.Count != m_passCustomOptionsUI.Count )
			{
				m_passCustomOptionsUIDict.Clear();
				int count = m_passCustomOptionsUI.Count;
				for( int i = 0; i < count; i++ )
				{
					m_passCustomOptionsUIDict.Add( m_passCustomOptionsUI[ i ].Options.Id, m_passCustomOptionsUI[ i ] );
				}
			}
		}

		public void ReadFromString( ref uint index, ref string[] nodeParams )
		{
			RefreshCustomOptionsDict();
			int savedOptions = Convert.ToInt32( nodeParams[ index++ ] );
			m_readOptions = new List<ReadOptions>();
			for( int i = 0; i < savedOptions; i++ )
			{
				string optionName = nodeParams[ index++ ];
				string optionSelection = nodeParams[ index++ ];
				Int64 optionTimestamp = ( UIUtils.CurrentShaderVersion() > 18929 ) ? Convert.ToInt64( nodeParams[ index++ ] ):0;
				m_readOptions.Add( new ReadOptions() { Name = optionName , Selection = optionSelection , Timestamp = optionTimestamp });

			}
		}

		public void WriteToString( ref string nodeInfo )
		{
			int optionsCount = m_passCustomOptionsUI.Count;
			IOUtils.AddFieldValueToString( ref nodeInfo , optionsCount );
			for( int i = 0 ; i < optionsCount ; i++ )
			{
				IOUtils.AddFieldValueToString( ref nodeInfo , m_passCustomOptionsUI[ i ].Options.Id );
				if( m_passCustomOptionsUI[ i ].Options.Type == AseOptionsType.Field )
					IOUtils.AddFieldValueToString( ref nodeInfo , m_passCustomOptionsUI[ i ].FieldValue.WriteToSingle() );
				else
					IOUtils.AddFieldValueToString( ref nodeInfo , m_passCustomOptionsUI[ i ].CurrentOption );

				IOUtils.AddFieldValueToString( ref nodeInfo , m_passCustomOptionsUI[ i ].LastClickedTimestamp );
			}

		}

		public void SetReadOptions()
		{
			if( m_readOptions != null )
			{
				for( int i = 0 ; i < m_readOptions.Count ; i++ )
				{
					if( m_passCustomOptionsUIDict.ContainsKey( m_readOptions[ i ].Name ) )
					{
						m_passCustomOptionsUIDict[ m_readOptions[ i ].Name ].LastClickedTimestamp = m_readOptions[ i ].Timestamp;
						if( m_passCustomOptionsUIDict[ m_readOptions[ i ].Name ].Options.Type == AseOptionsType.Field )
						{
							m_passCustomOptionsUIDict[ m_readOptions[ i ].Name ].FieldValue.ReadFromSingle( m_readOptions[ i ].Selection );
							foreach( var item in m_passCustomOptionsUIDict[ m_readOptions[ i ].Name ].Options.ActionsPerOption.Rows )
							{
								if( item.Columns.Length > 0 && item.Columns[ 0 ].ActionType == AseOptionsActionType.SetMaterialProperty )
								{
									if( UIUtils.CurrentWindow.CurrentGraph.CurrentMaterial != null )
									{
										if( UIUtils.CurrentWindow.CurrentGraph.CurrentMaterial.HasProperty( item.Columns[ 0 ].ActionData ) )
										{
											m_passCustomOptionsUIDict[ m_readOptions[ i ].Name ].CurrentFieldValue = UIUtils.CurrentWindow.CurrentGraph.CurrentMaterial.GetFloat( item.Columns[ 0 ].ActionData );
										}
									}
								}
							}
						}
						else
							m_passCustomOptionsUIDict[ m_readOptions[ i ].Name ].CurrentOptionIdx = Convert.ToInt32( m_readOptions[ i ].Selection );
					}
				}
			}
		}

		public void Refresh()
		{
			//int count = m_passCustomOptionsUI.Count;
			//for( int i = 0; i < count; i++ )
			//{
			//	m_passCustomOptionsUI[ i ].Refresh();
			//}
			List<TemplateOptionUIItem> sortedList = m_passCustomOptionsUI.OrderBy( item => item.LastClickedTimestamp ).ToList();
			int count = sortedList.Count;
			for( int i = 0 ; i < count ; i++ )
			{
				sortedList[ i ].Update();
			}
		}

		public void CheckDisable()
		{
			int count = m_passCustomOptionsUI.Count;
			for( int i = 0; i < count; i++ )
			{
				m_passCustomOptionsUI[ i ].CheckEnDisable(false);
			}
		}
	
		public List<TemplateOptionUIItem> PassCustomOptionsUI { get { return m_passCustomOptionsUI; } }
	}
}
