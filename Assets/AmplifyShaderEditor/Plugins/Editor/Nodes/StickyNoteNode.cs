// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using System;
using UnityEngine;
using UnityEditor;

namespace AmplifyShaderEditor
{
	[Serializable]
	[NodeAttributes( "Sticky Note", "Miscellaneous", "Allows adding notes into canvas" )]
	public sealed class StickyNoteNode : ParentNode, ISerializationCallbackReceiver
	{
		private const string WarningText = "Characters $ and @ are NOT allowed inside notes since they are internally used as delimiters over the node meta.\nThey will be automatically removed when saving the shader.";

		private const string CommentaryTitle = "Comment";
		private const string NoteTitle = "Note Title";
		private const float BORDER_SIZE_X = 50;
		private const float BORDER_SIZE_Y = 50;
		private const float MIN_SIZE_X = 100;
		private const float MIN_SIZE_Y = 100;
		private const float COMMENTARY_BOX_HEIGHT = 30;
		private const float NOTE_AREA_ADJUST = 5;
		
		private readonly Vector2 ResizeButtonPos = new Vector2( 1, 1 );

		[SerializeField]
		private string m_innerTitleText = "New Note";

		[SerializeField]
		private string m_titleText = string.Empty;

		[SerializeField]
		private string m_noteText = string.Empty;

		[SerializeField]
		private eResizeAxis m_resizeAxis = eResizeAxis.ALL;


		[SerializeField]
		private Rect m_resizeLeftIconCoords;

		[SerializeField]
		private Rect m_resizeRightIconCoords;

		[SerializeField]
		private Rect m_auxHeaderPos;

		[SerializeField]
		private Rect m_innerTitleArea;

		[SerializeField]
		private Rect m_noteTextArea;


		private Texture2D m_resizeIconTex;

		private bool m_isResizingRight = false;
		private bool m_isResizingLeft = false;

		private Vector2 m_resizeStartPoint = Vector2.zero;

		private string m_focusTitleName = "StickyNoteTitleText";
		private string m_focusNoteName = "StickyNoteInnerText";
		private bool m_focusOnTitle = false;
		
		private bool m_checkCommentText = true;
		private bool m_checkTitleText = true;

		public Color m_frameColor = Color.white;

		
		private bool m_isEditingInnerTitle;
		private bool m_stopEditingInnerTitle;
		private bool m_startEditingInnerTitle;

		private bool m_isEditingNoteText;
		private bool m_stopEditingNoteText;
		private bool m_startEditingNoteText;

		private double m_clickTimeTitle;
		private double m_doubleClickTimeTitle = 0.3;

		private double m_clickTimeNote;
		private double m_doubleClickTimeNote = 0.3;


		protected override void CommonInit( int uniqueId )
		{
			base.CommonInit( uniqueId );
			m_reorderLocked = true;
			m_rmbIgnore = true;
			m_defaultInteractionMode = InteractionMode.Both;
			m_connStatus = NodeConnectionStatus.Island;
			m_textLabelWidth = 90;
			m_position.width = 150;
			m_position.height = 100;
		}

		protected override void OnUniqueIDAssigned()
		{
			base.OnUniqueIDAssigned();
			m_focusTitleName = CommentaryTitle + OutputId;
			m_focusNoteName = NoteTitle + OutputId;
		}

		public override void DrawProperties()
		{
			base.DrawProperties();
			NodeUtils.DrawPropertyGroup( ref m_propertiesFoldout, Constants.ParameterLabelStr,()=>
			{
				EditorGUI.BeginChangeCheck();
				m_titleText = EditorGUILayoutTextField( "Frame Title", m_titleText );
				if ( EditorGUI.EndChangeCheck() )
				{
					m_checkTitleText = true;
				}
				EditorGUI.BeginChangeCheck();
				m_innerTitleText = EditorGUILayoutTextField( NoteTitle, m_innerTitleText );
				if ( EditorGUI.EndChangeCheck() )
				{
					m_checkCommentText = true;
				}

				m_noteText = EditorGUILayoutTextArea(  m_noteText , UIUtils.MainSkin.textArea );

				m_frameColor = EditorGUILayoutColorField( "Frame Color", m_frameColor );
			} );
			EditorGUILayout.HelpBox( WarningText, MessageType.Warning );
		}

		public override void OnNodeLayout( DrawInfo drawInfo )
		{
			//base.OnLayout( drawInfo );
			CalculatePositionAndVisibility( drawInfo );

			m_headerPosition = m_globalPosition;
			m_headerPosition.height = UIUtils.CurrentHeaderHeight;

			m_auxHeaderPos = m_position;
			m_auxHeaderPos.height = UIUtils.HeaderMaxHeight;

			m_innerTitleArea = m_globalPosition;
			m_innerTitleArea.height = COMMENTARY_BOX_HEIGHT * drawInfo.InvertedZoom;
			m_innerTitleArea.xMin += 10 * drawInfo.InvertedZoom;
			m_innerTitleArea.xMax -= 10 * drawInfo.InvertedZoom;


			m_noteTextArea = m_globalPosition;
			m_noteTextArea.height -= m_innerTitleArea.height;
			m_noteTextArea.yMin += m_innerTitleArea.height + NOTE_AREA_ADJUST * drawInfo.InvertedZoom;
			m_noteTextArea.yMax += 4*NOTE_AREA_ADJUST * drawInfo.InvertedZoom;
			m_noteTextArea.xMin += 10 * drawInfo.InvertedZoom;
			m_noteTextArea.xMax -= 10 * drawInfo.InvertedZoom;

			if ( m_resizeIconTex == null )
			{
				m_resizeIconTex = UIUtils.GetCustomStyle( CustomStyle.CommentaryResizeButton ).normal.background;
			}

			// LEFT RESIZE BUTTON
			m_resizeLeftIconCoords = m_globalPosition;
			m_resizeLeftIconCoords.x = m_globalPosition.x + 2;
			m_resizeLeftIconCoords.y = m_globalPosition.y + m_globalPosition.height - 2 - ( m_resizeIconTex.height + ResizeButtonPos.y ) * drawInfo.InvertedZoom;
			m_resizeLeftIconCoords.width = m_resizeIconTex.width * drawInfo.InvertedZoom;
			m_resizeLeftIconCoords.height = m_resizeIconTex.height * drawInfo.InvertedZoom;

			// RIGHT RESIZE BUTTON
			m_resizeRightIconCoords = m_globalPosition;
			m_resizeRightIconCoords.x = m_globalPosition.x + m_globalPosition.width - 1 - ( m_resizeIconTex.width + ResizeButtonPos.x ) * drawInfo.InvertedZoom;
			m_resizeRightIconCoords.y = m_globalPosition.y + m_globalPosition.height - 2 - ( m_resizeIconTex.height + ResizeButtonPos.y ) * drawInfo.InvertedZoom;
			m_resizeRightIconCoords.width = m_resizeIconTex.width * drawInfo.InvertedZoom;
			m_resizeRightIconCoords.height = m_resizeIconTex.height * drawInfo.InvertedZoom;			
		}

		public override void OnNodeRepaint( DrawInfo drawInfo )
		{
			if ( !m_isVisible )
				return;

			m_colorBuffer = GUI.color;
			// Background
			GUI.color = Constants.NodeBodyColor * m_frameColor;
			GUI.Label( m_globalPosition, string.Empty, UIUtils.GetCustomStyle( CustomStyle.CommentaryBackground ) );
			
			// Header
			GUI.color = m_headerColor * m_headerColorModifier * m_frameColor;
			GUI.Label( m_headerPosition, string.Empty, UIUtils.GetCustomStyle( CustomStyle.NodeHeader ) );
			GUI.color = m_colorBuffer;

			// Fixed Title ( only renders when not editing )
			if ( !m_isEditingInnerTitle && !m_startEditingInnerTitle && ContainerGraph.LodLevel <= ParentGraph.NodeLOD.LOD3 )
			{
				GUI.Label( m_innerTitleArea, m_innerTitleText, UIUtils.CommentaryTitle );
			}


			// Note Text ( only renders when not editing )
			if( !m_isEditingNoteText && !m_startEditingNoteText && ContainerGraph.LodLevel <= ParentGraph.NodeLOD.LOD3 )
			{
				GUI.Label( m_noteTextArea, m_noteText, UIUtils.StickyNoteText );
			}

			// Buttons
			GUI.Label( m_resizeLeftIconCoords, string.Empty, UIUtils.GetCustomStyle( CustomStyle.CommentaryResizeButtonInv ) );
			GUI.Label( m_resizeRightIconCoords, string.Empty, UIUtils.GetCustomStyle( CustomStyle.CommentaryResizeButton ) );

			// Selection Box
			if ( m_selected )
			{
				GUI.color = Constants.NodeSelectedColor;
				RectOffset cache = UIUtils.GetCustomStyle( CustomStyle.NodeWindowOn ).border;
				UIUtils.GetCustomStyle( CustomStyle.NodeWindowOn ).border = UIUtils.RectOffsetSix;
				GUI.Label( m_globalPosition, string.Empty, UIUtils.GetCustomStyle( CustomStyle.NodeWindowOn ) );
				UIUtils.GetCustomStyle( CustomStyle.NodeWindowOn ).border = cache;
				GUI.color = m_colorBuffer;
			}

			if ( !string.IsNullOrEmpty( m_titleText ) )
			{
				Rect titleRect = m_globalPosition;
				titleRect.y -= 24;
				titleRect.height = 24;
				GUI.Label( titleRect, m_titleText, UIUtils.GetCustomStyle( CustomStyle.CommentarySuperTitle ) );
			}
		}

		public override void Move( Vector2 delta, bool snap )
		{
			if( m_isResizingRight || m_isResizingLeft )
				return;

			base.Move( delta, snap );
		}

		public override void Draw( DrawInfo drawInfo )
		{
			base.Draw( drawInfo );

			// Custom Editable Title
			if ( ContainerGraph.LodLevel <= ParentGraph.NodeLOD.LOD3 )
			{
				if( !m_startEditingNoteText && !m_isEditingNoteText )
				{
					if( !m_isEditingInnerTitle && ( ( !ContainerGraph.ParentWindow.MouseInteracted && drawInfo.CurrentEventType == EventType.MouseDown && m_innerTitleArea.Contains( drawInfo.MousePosition ) ) ) )
					{
						if( ( EditorApplication.timeSinceStartup - m_clickTimeTitle ) < m_doubleClickTimeTitle )
							m_startEditingInnerTitle = true;
						else
							GUI.FocusControl( null );
						m_clickTimeTitle = EditorApplication.timeSinceStartup;
					}
					else if( m_isEditingInnerTitle && ( ( drawInfo.CurrentEventType == EventType.MouseDown && !m_innerTitleArea.Contains( drawInfo.MousePosition ) ) || !EditorGUIUtility.editingTextField ) )
					{
						m_stopEditingInnerTitle = true;
					}

					if( m_isEditingInnerTitle || m_startEditingInnerTitle )
					{
						EditorGUI.BeginChangeCheck();
						GUI.SetNextControlName( m_focusTitleName );
						m_innerTitleText = EditorGUITextField( m_innerTitleArea, string.Empty, m_innerTitleText, UIUtils.CommentaryTitle );
						if( EditorGUI.EndChangeCheck() )
						{
							m_checkCommentText = true;
						}

						if( m_startEditingInnerTitle )
							EditorGUI.FocusTextInControl( m_focusTitleName );
					}

					if( drawInfo.CurrentEventType == EventType.Repaint )
					{
						if( m_startEditingInnerTitle )
						{
							m_startEditingInnerTitle = false;
							m_isEditingInnerTitle = true;
						}

						if( m_stopEditingInnerTitle )
						{
							m_stopEditingInnerTitle = false;
							m_isEditingInnerTitle = false;
							GUI.FocusControl( null );
						}
					}
				}

				////////////////////////
				if( !m_startEditingInnerTitle  && !m_isEditingInnerTitle )
				{
					if( !m_isEditingNoteText && ( ( !ContainerGraph.ParentWindow.MouseInteracted && drawInfo.CurrentEventType == EventType.MouseDown && m_noteTextArea.Contains( drawInfo.MousePosition ) ) ) )
					{
						if( ( EditorApplication.timeSinceStartup - m_clickTimeNote ) < m_doubleClickTimeNote )
							m_startEditingNoteText = true;
						else
							GUI.FocusControl( null );
						m_clickTimeNote = EditorApplication.timeSinceStartup;
					}
					else if( m_isEditingNoteText && ( ( drawInfo.CurrentEventType == EventType.MouseDown && !m_noteTextArea.Contains( drawInfo.MousePosition ) ) || !EditorGUIUtility.editingTextField ) )
					{
						m_stopEditingInnerTitle = true;
					}

					if( m_isEditingNoteText || m_startEditingNoteText )
					{
						GUI.SetNextControlName( m_focusNoteName );
						m_noteText = EditorGUITextArea( m_noteTextArea, m_noteText, UIUtils.StickyNoteText );
						if( m_startEditingNoteText )
							EditorGUI.FocusTextInControl( m_focusNoteName );
					}

					if( drawInfo.CurrentEventType == EventType.Repaint )
					{
						if( m_startEditingNoteText )
						{
							m_startEditingNoteText = false;
							m_isEditingNoteText = true;
						}

						if( m_stopEditingInnerTitle )
						{
							m_stopEditingInnerTitle = false;
							m_isEditingNoteText = false;
							GUI.FocusControl( null );
						}
					}
				}
				////////////////////////
			}

			if ( drawInfo.CurrentEventType == EventType.MouseDown && drawInfo.LeftMouseButtonPressed )
			{
				// Left Button
				if( m_resizeLeftIconCoords.Contains( drawInfo.MousePosition ) && ContainerGraph.ParentWindow.CurrentEvent.modifiers != EventModifiers.Shift )
				{
					if ( !m_isResizingLeft )
					{
						m_isResizingLeft = true;
						ContainerGraph.ParentWindow.ForceAutoPanDir = true;
						m_resizeStartPoint = drawInfo.TransformedMousePos;
					}
				}

				// Right Button
				if ( m_resizeRightIconCoords.Contains( drawInfo.MousePosition ) && ContainerGraph.ParentWindow.CurrentEvent.modifiers != EventModifiers.Shift )
				{
					if ( !m_isResizingRight )
					{
						m_isResizingRight = true;
						ContainerGraph.ParentWindow.ForceAutoPanDir = true;
						m_resizeStartPoint = drawInfo.TransformedMousePos;
					}
				}
			}

			if ( drawInfo.CurrentEventType == EventType.Repaint || drawInfo.CurrentEventType == EventType.MouseUp )
			{
				// Left Button
				EditorGUIUtility.AddCursorRect( m_resizeLeftIconCoords, MouseCursor.ResizeUpRight );
				if ( m_isResizingLeft )
				{
					if ( drawInfo.CurrentEventType == EventType.MouseUp )
					{
						m_isResizingLeft = false;
						ContainerGraph.ParentWindow.ForceAutoPanDir = false;
						FireStoppedMovingEvent( false, InteractionMode.Target );
					}
					else
					{
						Vector2 currSize = ( drawInfo.TransformedMousePos - m_resizeStartPoint ) /*/ drawInfo.InvertedZoom*/;
						m_resizeStartPoint = drawInfo.TransformedMousePos;
						if ( m_resizeAxis != eResizeAxis.Y_AXIS )
						{
							m_position.x += currSize.x;
							m_position.width -= currSize.x;
							if ( m_position.width < MIN_SIZE_X )
							{
								m_position.x -= ( MIN_SIZE_X - m_position.width );
								m_position.width = MIN_SIZE_X;
							}
						}

						if ( m_resizeAxis != eResizeAxis.X_AXIS )
						{
							m_position.height += currSize.y;
							if ( m_position.height < MIN_SIZE_Y )
							{
								m_position.height = MIN_SIZE_Y;
							}
						}
					}
				}

				// Right Button
				EditorGUIUtility.AddCursorRect( m_resizeRightIconCoords, MouseCursor.ResizeUpLeft );
				if ( m_isResizingRight )
				{
					if ( drawInfo.CurrentEventType == EventType.MouseUp )
					{
						m_isResizingRight = false;
						ContainerGraph.ParentWindow.ForceAutoPanDir = false;
						FireStoppedMovingEvent( false, InteractionMode.Target );
					}
					else
					{
						Vector2 currSize = ( drawInfo.TransformedMousePos - m_resizeStartPoint ) /*/ drawInfo.InvertedZoom*/;
						m_resizeStartPoint = drawInfo.TransformedMousePos;
						if ( m_resizeAxis != eResizeAxis.Y_AXIS )
						{
							m_position.width += currSize.x;
							if ( m_position.width < MIN_SIZE_X )
							{
								m_position.width = MIN_SIZE_X;
							}
						}

						if ( m_resizeAxis != eResizeAxis.X_AXIS )
						{
							m_position.height += currSize.y;
							if ( m_position.height < MIN_SIZE_Y )
							{
								m_position.height = MIN_SIZE_Y;
							}
						}
					}
				}
			}

			if ( m_checkCommentText )
			{
				m_checkCommentText = false;
				m_innerTitleText = m_innerTitleText.Replace( IOUtils.FIELD_SEPARATOR, ' ' );
			}

			if ( m_checkTitleText )
			{
				m_checkTitleText = false;
				m_titleText = m_titleText.Replace( IOUtils.FIELD_SEPARATOR, ' ' );
			}

			if ( m_focusOnTitle && drawInfo.CurrentEventType == EventType.KeyUp )
			{
				m_focusOnTitle = false;
				m_startEditingInnerTitle = true;
			}
		}

		public void Focus()
		{
			m_focusOnTitle = true;
		}

		public override void ReadFromString( ref string[] nodeParams )
		{
			base.ReadFromString( ref nodeParams );
			m_position.width = Convert.ToSingle( GetCurrentParam( ref nodeParams ) );
			m_position.height = Convert.ToSingle( GetCurrentParam( ref nodeParams ) );
			m_innerTitleText = GetCurrentParam( ref nodeParams );
			m_titleText = GetCurrentParam( ref nodeParams );
			string[] colorChannels = GetCurrentParam( ref nodeParams ).Split( IOUtils.VECTOR_SEPARATOR );
			if ( colorChannels.Length == 4 )
			{
				m_frameColor.r = Convert.ToSingle( colorChannels[ 0 ] );
				m_frameColor.g = Convert.ToSingle( colorChannels[ 1 ] );
				m_frameColor.b = Convert.ToSingle( colorChannels[ 2 ] );
				m_frameColor.a = Convert.ToSingle( colorChannels[ 3 ] );
			}
			else
			{
				UIUtils.ShowMessage( UniqueId, "Incorrect number of color values", MessageSeverity.Error );
			}
			m_noteText = GetCurrentParam( ref nodeParams );
			m_noteText = m_noteText.Replace( Constants.LineFeedSeparator, '\n' );
			m_noteText = m_noteText.Replace( Constants.SemiColonSeparator, ';' );
		}

		public override void WriteToString( ref string nodeInfo, ref string connectionsInfo )
		{
			base.WriteToString( ref nodeInfo, ref connectionsInfo );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_position.width );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_position.height );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_innerTitleText );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_titleText );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_frameColor.r.ToString() + IOUtils.VECTOR_SEPARATOR + m_frameColor.g.ToString() + IOUtils.VECTOR_SEPARATOR + m_frameColor.b.ToString() + IOUtils.VECTOR_SEPARATOR + m_frameColor.a.ToString() );

			m_noteText = m_noteText.Replace( Constants.LineFeedSeparator.ToString(), string.Empty);
			m_noteText = m_noteText.Replace( Constants.SemiColonSeparator.ToString(), string.Empty );
			m_noteText = UIUtils.ForceLFLineEnding( m_noteText );

			string parsedText = m_noteText.Replace( '\n', Constants.LineFeedSeparator );
			parsedText = parsedText.Replace( ';', Constants.SemiColonSeparator );
			IOUtils.AddFieldValueToString( ref nodeInfo, parsedText );
		}

		public override Rect Position { get { return Event.current.alt ? m_position : m_auxHeaderPos; } }
		public override bool Contains( Vector3 pos )
		{
			return Event.current.alt ? m_globalPosition.Contains( pos ) : ( m_headerPosition.Contains( pos ) || m_resizeRightIconCoords.Contains( pos ) || m_resizeLeftIconCoords.Contains( pos ) );
		}
	}
}
