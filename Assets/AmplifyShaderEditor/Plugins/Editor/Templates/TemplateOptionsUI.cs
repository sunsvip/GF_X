// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace AmplifyShaderEditor
{
	// UI STRUCTURES
	[Serializable]
	public class TemplateOptionUIItem
	{
		public delegate void OnActionPerformed( bool actionFromUser, bool isRefreshing, bool invertAction, TemplateOptionUIItem uiItem, params TemplateActionItem[] validActions );
		public event OnActionPerformed OnActionPerformedEvt;

		[SerializeField]
		private bool m_isVisible = true;

		[SerializeField]
		private bool m_wasVisible = true;

		[SerializeField]
		private int m_currentOption = 0;

		[SerializeField]
		private TemplateOptionsItem m_options;

		[SerializeField]
		private bool m_checkOnExecute = false;

		[SerializeField]
		private bool m_invertActionOnDeselection = false;

		[SerializeField]
		private Int64 m_lastClickedTimestamp = 0;

		public TemplateOptionUIItem( TemplateOptionsItem options )
		{
			m_options = options;
			if( m_options.Type == AseOptionsType.Field )
			{
				m_options.FieldValue.FloatValue = m_options.DefaultFieldValue;
			}
			else
			{
				m_currentOption = m_options.DefaultOptionIndex;
			}
			m_invertActionOnDeselection = options.Setup == AseOptionItemSetup.InvertActionOnDeselection;
		}

		public void CopyValuesFrom( TemplateOptionUIItem origin )
		{
			m_isVisible = origin.IsVisible;
			m_wasVisible = origin.WasVisible;
			m_currentOption = origin.CurrentOption;
			m_options.FieldValue.FloatValue = origin.CurrentFieldValue;
			m_checkOnExecute = origin.CheckOnExecute;
			m_invertActionOnDeselection = origin.InvertActionOnDeselection;
		}
		

		public void Draw( UndoParentNode owner )
		{
			if( m_isVisible )
			{
				int lastOption = m_currentOption;
				EditorGUI.BeginChangeCheck();
				switch( m_options.UIWidget )
				{
					case AseOptionsUIWidget.Dropdown:
					{
						m_currentOption = owner.EditorGUILayoutPopup( m_options.Name, m_currentOption, m_options.DisplayOptions );
					}
					break;
					case AseOptionsUIWidget.Toggle:
					{
						m_currentOption = owner.EditorGUILayoutToggle( m_options.Name, m_currentOption == 1 ) ? 1 : 0;
					}
					break;
					case AseOptionsUIWidget.Float:
					{
						if( m_options.FieldInline )
						{
							m_options.FieldValue.FloatField( ref owner, m_options.Name );
							if( m_options.FieldValue.Active )
								m_currentOption = 1;
							else
								m_currentOption = 0;
						}
						else
						{
							m_options.FieldValue.FloatValue = owner.EditorGUILayoutFloatField( m_options.Name, m_options.FieldValue.FloatValue );
						}
					}
					break;
					case AseOptionsUIWidget.Int:
					{
						if( m_options.FieldInline )
						{
							m_options.FieldValue.IntField( ref owner, m_options.Name );
							if( m_options.FieldValue.Active )
								m_currentOption = 1;
							else
								m_currentOption = 0;
						}
						else
							m_options.FieldValue.FloatValue = owner.EditorGUILayoutIntField( m_options.Name, (int)m_options.FieldValue.FloatValue );
					}
					break;
					case AseOptionsUIWidget.FloatRange:
					{
						if( m_options.FieldInline )
						{
							m_options.FieldValue.SliderField( ref owner, m_options.Name, m_options.FieldMin, m_options.FieldMax );
							if( m_options.FieldValue.Active )
								m_currentOption = 1;
							else
								m_currentOption = 0;
						}
						else
							m_options.FieldValue.FloatValue = owner.EditorGUILayoutSlider( m_options.Name, m_options.FieldValue.FloatValue, m_options.FieldMin, m_options.FieldMax );
					}
					break;
					case AseOptionsUIWidget.IntRange:
					{
						if( m_options.FieldInline )
						{
							m_options.FieldValue.IntSlider( ref owner, m_options.Name, (int)m_options.FieldMin, (int)m_options.FieldMax );
							if( m_options.FieldValue.Active )
								m_currentOption = 1;
							else
								m_currentOption = 0;
						}
						else
							m_options.FieldValue.FloatValue = owner.EditorGUILayoutIntSlider( m_options.Name, (int)m_options.FieldValue.FloatValue, (int)m_options.FieldMin, (int)m_options.FieldMax );
					}
					break;
				}
				if( EditorGUI.EndChangeCheck() )
				{
					m_lastClickedTimestamp = DateTime.UtcNow.Ticks;
					if( OnActionPerformedEvt != null )
					{
						if( m_invertActionOnDeselection )
							OnActionPerformedEvt( true, false, lastOption != m_options.DisableIdx, this, m_options.ActionsPerOption[ lastOption ] );

						OnActionPerformedEvt( true, false, false, this, m_options.ActionsPerOption[ m_currentOption ] );
					}
				}
			}
		}

		public void CheckEnDisable( bool actionFromUser )
		{
			//string deb = string.Empty;// "-- Checked --" + m_options.Name+" "+ m_isVisible + " "+ m_wasVisible;
			if( m_isVisible )
			{
				if( !m_wasVisible )
				{
					//deb = "-- Enable --" + m_options.Name;
					//Debug.Log( deb );
					if( OnActionPerformedEvt != null )
					{
						if( m_invertActionOnDeselection )
						{
							for( int i = 0; i < m_options.Count; i++ )
							{
								if( i != m_currentOption && i != m_options.DisableIdx )
								{
									OnActionPerformedEvt( actionFromUser, false, true, this, m_options.ActionsPerOption[ i ] );
								}
							}
						}

						OnActionPerformedEvt( actionFromUser, false, false, this, m_options.ActionsPerOption[ m_currentOption ] );
						//if( !m_isVisible )
							//OnActionPerformedEvt( isRefreshing, false, this, m_options.ActionsPerOption[ m_options.DisableIdx ] );
					}
				}

				m_wasVisible = true;
			}
			else if( m_wasVisible )
			{
				//deb = "-- Disable --" + m_options.Name;
				//Debug.Log( deb );
				m_wasVisible = false;

				if( OnActionPerformedEvt != null )
				{
					OnActionPerformedEvt( actionFromUser, false, false, this, m_options.ActionsPerOption[ m_options.DisableIdx ] );
				}
			}
		}

		public void FillDataCollector( ref MasterNodeDataCollector dataCollector )
		{
			if( m_isVisible && m_checkOnExecute )
			{
				for( int i = 0; i < m_options.ActionsPerOption[ m_currentOption ].Length; i++ )
				{
					switch( m_options.ActionsPerOption[ m_currentOption ][ i ].ActionType )
					{
						case AseOptionsActionType.SetDefine:
						{
							dataCollector.AddToDefines( -1, m_options.ActionsPerOption[ m_currentOption ][ i ].ActionData );
						}
						break;
						case AseOptionsActionType.SetUndefine:
						{
							dataCollector.AddToDefines( -1, m_options.ActionsPerOption[ m_currentOption ][ i ].ActionData, false );
						}
						break;
					}
				}
			}
		}

		public void Update( bool isRefreshing = true )
		{
			if( OnActionPerformedEvt != null )
			{
				if( m_invertActionOnDeselection )
				{
					for( int i = 0; i < m_options.Count; i++ )
					{
						if( i != m_currentOption && i != m_options.DisableIdx )
						{
							OnActionPerformedEvt( false, isRefreshing, true, this, m_options.ActionsPerOption[ i ] );
						}
					}
				}

				OnActionPerformedEvt( false, isRefreshing, false, this, m_options.ActionsPerOption[ m_currentOption ] );
			}
		}

		public TemplateOptionsItem Options { get { return m_options; } }

		public void Destroy()
		{
			OnActionPerformedEvt = null;
		}

		public bool IsVisible
		{
			get { return m_isVisible; }
			set { m_isVisible = value; }
		}

		public bool WasVisible
		{
			get { return m_wasVisible; }
			set { m_wasVisible = value; }
		}

		public bool CheckOnExecute
		{
			get { return m_checkOnExecute; }
			set { m_checkOnExecute = value; }
		}

		public InlineProperty FieldValue
		{
			get { return m_options.FieldValue; }
			set { m_options.FieldValue = value; }
		}

		public float CurrentFieldValue
		{
			get { return m_options.FieldValue.FloatValue; }
			set { m_options.FieldValue.FloatValue = value; }
		}

		public int CurrentOption
		{
			get { return m_currentOption; }
			set
			{
				m_currentOption = Mathf.Clamp( value, 0, m_options.Options.Length - 1 );
				// why refreshing here?
				//Refresh();
			}
		}

		public int CurrentOptionIdx
		{
			set
			{
				m_currentOption = Mathf.Clamp( value, 0, m_options.Options.Length - 1 );
			}
		}
		public bool EmptyEvent { get { return OnActionPerformedEvt == null; } }
		public TemplateActionItemGrid.TemplateActionItemRow CurrentOptionActions
		{
			get
			{
				if( m_options.Type == AseOptionsType.Field &&
					m_currentOption == 1 )
				{
					if( m_options.FieldValue.NodeId < -1 )
					{
						//This is quite the hack. The bug is related to if a user chooses an inline property on the field option, then the behavior is to comment the original property set on the template
						// The problem is that, if the user sets an inline property and select its own internal property from there, then the behavior that will prevail without this hack is to call the actions associated with setting a new inline property
						// Which is on all templates to comment the original template internal property leaving the property commented on the final code (This was detected on URP PBR)
						PropertyNode node = UIUtils.CurrentWindow.OutsideGraph.GetInternalTemplateNode( m_options.FieldValue.NodeId ) as PropertyNode;
						if( node != null )
						{
							if( node.PropertyName.Equals( m_options.FieldInlineName ) )
							{
								return m_options.ActionsPerOption.Rows[ 0 ];
							}
						}
					}
					else if( m_options.FieldValue.NodeId == -1 )
					{
						// If node id is -1 then no node is selected on the inline dropdown then we should also fallback to using its internal property
						return m_options.ActionsPerOption.Rows[ 0 ];
					}
				}
				return m_options.ActionsPerOption.Rows[m_currentOption];
			}
		}
		public bool InvertActionOnDeselection { get { return m_invertActionOnDeselection; } }
		public Int64 LastClickedTimestamp { get { return m_lastClickedTimestamp; } set { m_lastClickedTimestamp = value; } }
	}
}
