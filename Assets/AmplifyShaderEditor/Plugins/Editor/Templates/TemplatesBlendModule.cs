// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using System;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace AmplifyShaderEditor
{
	[Serializable]
	public sealed class TemplatesBlendModule : TemplateModuleParent
	{
		private const string AlphaToMaskStr = "Alpha To Coverage"; 
		private const string BlendModeStr = " Blend Mode";

		private const string BlendModesRGBStr = "Blend RGB";
		private const string BlendModesAlphaStr = "Blend Alpha";

		private const string BlendOpsRGBStr = "Blend Op RGB";
		private const string BlendOpsAlphaStr = "Blend Op Alpha";

		private const string SourceFactorStr = "Src";
		private const string DstFactorStr = "Dst";

		private const string AlphaToMaskFormat = "AlphaToMask {0}";
		private const string BlendFactorOff = "Blend Off";
		private const string SingleBlendFactorStr = "Blend{0} {1} {2}";
		private const string SeparateBlendFactorStr = "Blend{0} {1} {2}, {3} {4}";

		private const string SingleBlendOpStr = "BlendOp{0} {1}";
		private const string SeparateBlendOpStr = "BlendOp{0} {1}, {2}";
		private const string BlendOpOffStr = "BlendOp Off";


		private string[] m_commonBlendTypesArr;
		private List<CommonBlendTypes> m_commonBlendTypes = new List<CommonBlendTypes>
		{
			new CommonBlendTypes("<OFF>",               AvailableBlendFactor.Zero,              AvailableBlendFactor.Zero ),
			new CommonBlendTypes("Custom",              AvailableBlendFactor.Zero,              AvailableBlendFactor.Zero ) ,
			new CommonBlendTypes("Alpha Blend",         AvailableBlendFactor.SrcAlpha,          AvailableBlendFactor.OneMinusSrcAlpha ) ,
			new CommonBlendTypes("Premultiplied",		AvailableBlendFactor.One,               AvailableBlendFactor.OneMinusSrcAlpha ),
			new CommonBlendTypes("Additive",            AvailableBlendFactor.One,               AvailableBlendFactor.One ),
			new CommonBlendTypes("Soft Additive",       AvailableBlendFactor.OneMinusDstColor,  AvailableBlendFactor.One ),
			new CommonBlendTypes("Multiplicative",      AvailableBlendFactor.DstColor,          AvailableBlendFactor.Zero ),
			new CommonBlendTypes("2x Multiplicative",   AvailableBlendFactor.DstColor,          AvailableBlendFactor.SrcColor ),
			new CommonBlendTypes("Particle Additive",   AvailableBlendFactor.SrcAlpha,          AvailableBlendFactor.One )
		};

		[SerializeField]
		private string m_target = string.Empty;

		[SerializeField]
		private bool m_validBlendMode = false;

		[SerializeField]
		private bool m_validBlendOp = false;

		[SerializeField]
		private bool m_blendModeEnabled = false;

		// Blend Factor
		// RGB
		[SerializeField]
		private int m_currentRGBIndex = 0;

		[SerializeField]
		private AvailableBlendFactor m_sourceFactorRGB = AvailableBlendFactor.Zero;
		[SerializeField]
		private InlineProperty m_sourceFactorRGBInline = new InlineProperty();

		[SerializeField]
		private AvailableBlendFactor m_destFactorRGB = AvailableBlendFactor.Zero;
		[SerializeField]
		private InlineProperty m_destFactorRGBInline = new InlineProperty();

		//Alpha
		[SerializeField]
		private int m_currentAlphaIndex = 0;

		[SerializeField]
		private AvailableBlendFactor m_sourceFactorAlpha = AvailableBlendFactor.Zero;
		[SerializeField]
		private InlineProperty m_sourceFactorAlphaInline = new InlineProperty();

		[SerializeField]
		private AvailableBlendFactor m_destFactorAlpha = AvailableBlendFactor.Zero;
		[SerializeField]
		private InlineProperty m_destFactorAlphaInline = new InlineProperty();

		//Blend Ops
		[SerializeField]
		private bool m_blendOpEnabled = false;

		[SerializeField]
		private AvailableBlendOps m_blendOpRGB = AvailableBlendOps.OFF;

		[SerializeField]
		private InlineProperty m_blendOpRGBInline = new InlineProperty();

		[SerializeField]
		private AvailableBlendOps m_blendOpAlpha = AvailableBlendOps.OFF;

		[SerializeField]
		private InlineProperty m_blendOpAlphaInline = new InlineProperty();

		public TemplatesBlendModule() : base( "Blend Mode and Ops" )
		{
			m_commonBlendTypesArr = new string[ m_commonBlendTypes.Count ];
			for( int i = 0; i < m_commonBlendTypesArr.Length; i++ )
			{
				m_commonBlendTypesArr[ i ] = m_commonBlendTypes[ i ].Name;
			}
		}

		public void CopyFrom( TemplatesBlendModule other, bool allData )
		{
			if( allData )
			{
				m_independentModule = other.IndependentModule;
				m_validBlendMode = other.ValidBlendMode;
				m_target = other.Target;
				m_validBlendOp = other.ValidBlendOp;
			}
			m_blendModeEnabled = other.BlendModeEnabled;
			m_currentRGBIndex = other.CurrentRGBIndex;
			m_sourceFactorRGB = other.SourceFactorRGB;
			m_destFactorRGB = other.DestFactorRGB;
			m_currentAlphaIndex = other.CurrentAlphaIndex;
			m_sourceFactorAlpha = other.SourceFactorAlpha;
			m_destFactorAlpha = other.DestFactorAlpha;
			m_blendOpEnabled = other.BlendOpEnabled;
			m_blendOpRGB = other.BlendOpRGB;
			m_blendOpAlpha = other.BlendOpAlpha;
			m_sourceFactorRGBInline = other.SourceFactorRGBInline;
			m_destFactorRGBInline = other.DestFactorRGBInline;
			m_sourceFactorAlphaInline = other.SourceFactorAlphaInline;
			m_destFactorAlphaInline = other.DestFactorAlphaInline;
			m_blendOpRGBInline = other.BlendOpRGBInline;
			m_blendOpAlphaInline = other.BlendOpAlphaInline;
		}

		public void ConfigureFromTemplateData( TemplateBlendData blendData )
		{
			if( blendData.ValidBlendMode )
			{
				if( m_validBlendMode != blendData.ValidBlendMode )
				{
					m_blendModeEnabled = true;
					m_independentModule = blendData.IndependentModule;
					if( string.IsNullOrEmpty( blendData.SourceFactorRGBInline ) )
					{
						m_sourceFactorRGB = blendData.SourceFactorRGB;
						m_sourceFactorRGBInline.ResetProperty();
					}
					else
					{
						m_sourceFactorRGBInline.SetInlineByName( blendData.SourceFactorRGBInline );
					}

					if( string.IsNullOrEmpty( blendData.DestFactorRGBInline ) )
					{
						m_destFactorRGB = blendData.DestFactorRGB;
						m_destFactorRGBInline.ResetProperty();
					}
					else
					{
						m_destFactorRGBInline.SetInlineByName( blendData.DestFactorRGBInline );
					}

					if( string.IsNullOrEmpty( blendData.SourceFactorAlphaInline ) )
					{
						m_sourceFactorAlpha = blendData.SourceFactorAlpha;
						m_sourceFactorAlphaInline.ResetProperty();
					}
					else
					{
						m_sourceFactorAlphaInline.SetInlineByName( blendData.SourceFactorAlphaInline );
					}
					if( string.IsNullOrEmpty( blendData.DestFactorAlphaInline ) )
					{
						m_destFactorAlpha = blendData.DestFactorAlpha;
						m_destFactorAlphaInline.ResetProperty();
					}
					else
					{
						m_destFactorAlphaInline.SetInlineByName( blendData.DestFactorAlphaInline );
					}

					if( blendData.SeparateBlendFactors )
					{
						if( blendData.BlendModeOff )
						{
							m_currentRGBIndex = 0;
						}
						else
						{
							CheckRGBIndex();
						}
						CheckAlphaIndex();
					}
					else
					{
						if( blendData.BlendModeOff )
						{
							m_currentRGBIndex = 0;
						}
						else
						{
							CheckRGBIndex();
						}
						m_currentAlphaIndex = 0;
					}
				}
			}
			else
			{
				m_blendModeEnabled = false;
			}

			if( blendData.ValidBlendOp )
			{
				if( m_validBlendOp != blendData.ValidBlendOp )
				{
					m_blendOpEnabled = true;
					if( string.IsNullOrEmpty( blendData.BlendOpRGBInline ) )
					{
						m_blendOpRGB = blendData.BlendOpRGB;
						m_blendOpRGBInline.ResetProperty();
					}
					else
					{
						m_blendOpRGBInline.SetInlineByName( blendData.BlendOpRGBInline );
					}

					if( string.IsNullOrEmpty( blendData.BlendOpAlphaInline ) )
					{
						m_blendOpAlpha = blendData.BlendOpAlpha;
						m_blendOpAlphaInline.ResetProperty();
					}
					else
					{
						m_blendOpAlphaInline.SetInlineByName( blendData.BlendOpAlphaInline );
					}
				}
			}
			else
			{
				m_blendOpEnabled = false;
			}

			m_target = blendData.Target;
			m_validBlendMode = blendData.ValidBlendMode;
			m_validBlendOp = blendData.ValidBlendOp;
			m_validData = m_validBlendMode || m_validBlendOp;
		}

		public override void ShowUnreadableDataMessage( ParentNode owner )
		{
			bool foldout = owner.ContainerGraph.ParentWindow.InnerWindowVariables.ExpandedBlendModeModule;
			NodeUtils.DrawPropertyGroup( ref foldout, BlendModeStr, base.ShowUnreadableDataMessage );
			owner.ContainerGraph.ParentWindow.InnerWindowVariables.ExpandedBlendModeModule = foldout;
		}

		public override void Draw( UndoParentNode owner, bool style = true )
		{
			bool foldout = owner.ContainerGraph.ParentWindow.InnerWindowVariables.ExpandedBlendModeModule;
			if( style )
			{
				NodeUtils.DrawPropertyGroup( ref foldout, BlendModeStr + Target, () =>
				{
					DrawBlock( owner, style );
				} );
			}
			else
			{
				NodeUtils.DrawNestedPropertyGroup( ref foldout, BlendModeStr + Target, () =>
				{
					DrawBlock( owner, style );
				} );
			}
			owner.ContainerGraph.ParentWindow.InnerWindowVariables.ExpandedBlendModeModule = foldout;
		}

		void DrawBlock( UndoParentNode owner, bool style )
		{
			EditorGUI.BeginChangeCheck();
			{
				var cache = EditorGUIUtility.labelWidth;
				EditorGUIUtility.labelWidth = EditorGUIUtility.labelWidth - 20;

				if( m_blendModeEnabled )
				{
					// RGB
					EditorGUI.BeginChangeCheck();
					m_currentRGBIndex = owner.EditorGUILayoutPopup( BlendModesRGBStr, m_currentRGBIndex, m_commonBlendTypesArr );
					if( EditorGUI.EndChangeCheck() )
					{
						if( m_currentRGBIndex > 1 )
						{
							m_sourceFactorRGB = m_commonBlendTypes[ m_currentRGBIndex ].SourceFactor;
							m_sourceFactorRGBInline.IntValue = (int)m_sourceFactorRGB;
							m_sourceFactorRGBInline.SetInlineNodeValue();

							m_destFactorRGB = m_commonBlendTypes[ m_currentRGBIndex ].DestFactor;
							m_destFactorRGBInline.IntValue = (int)m_destFactorRGB;
							m_destFactorRGBInline.SetInlineNodeValue();
						}
					}
					EditorGUI.BeginDisabledGroup( m_currentRGBIndex == 0 );

					EditorGUI.BeginChangeCheck();
					float cached = EditorGUIUtility.labelWidth;
					if( style )
					{
						EditorGUIUtility.labelWidth = 40;
					}
					else
					{
						EditorGUIUtility.labelWidth = 25;
					}

					EditorGUILayout.BeginHorizontal();
					//m_sourceFactorRGB = (AvailableBlendFactor)owner.EditorGUILayoutEnumPopup( SourceFactorStr, m_sourceFactorRGB );
					m_sourceFactorRGBInline.CustomDrawer( ref owner, ( x ) => { m_sourceFactorRGB = (AvailableBlendFactor)x.EditorGUILayoutEnumPopup( SourceFactorStr, m_sourceFactorRGB ); }, SourceFactorStr );
					if( style )
					{
						EditorGUI.indentLevel--;
						EditorGUIUtility.labelWidth = 25;
					}
					//m_destFactorRGB = (AvailableBlendFactor)owner.EditorGUILayoutEnumPopup( DstFactorStr, m_destFactorRGB );
					m_destFactorRGBInline.CustomDrawer( ref owner, ( x ) => { m_destFactorRGB = (AvailableBlendFactor)x.EditorGUILayoutEnumPopup( DstFactorStr, m_destFactorRGB ); }, DstFactorStr );
					if( style )
						EditorGUI.indentLevel++;

					EditorGUILayout.EndHorizontal();

					EditorGUIUtility.labelWidth = cached;
					if( EditorGUI.EndChangeCheck() )
					{
						CheckRGBIndex();
					}
					EditorGUI.EndDisabledGroup();
				}

				if( m_blendOpEnabled )
				{
					// Both these tests should be removed on a later stage
					// ASE v154dev004 changed AvailableBlendOps.OFF value from -1 to 0
					// If importing the new package into an already opened ASE window makes 
					// hotcode to preserve the -1 value on these variables
					if( (int)m_blendOpRGB == -1 )
						m_blendOpRGB = AvailableBlendOps.OFF;

					//m_blendOpRGB = (AvailableBlendOps)owner.EditorGUILayoutEnumPopup( BlendOpsRGBStr, m_blendOpRGB );
					m_blendOpRGBInline.CustomDrawer( ref owner, ( x ) => { m_blendOpRGB = (AvailableBlendOps)x.EditorGUILayoutPopup( BlendOpsRGBStr, (int)m_blendOpRGB, BlendOpsHelper.BlendOpsLabels ); }, BlendOpsRGBStr );
				}
				
				if( m_blendModeEnabled )
				{
					// Alpha
					EditorGUILayout.Separator();

					EditorGUI.BeginChangeCheck();
					m_currentAlphaIndex = owner.EditorGUILayoutPopup( BlendModesAlphaStr, m_currentAlphaIndex, m_commonBlendTypesArr );
					if( EditorGUI.EndChangeCheck() )
					{
						if( m_currentAlphaIndex > 0 )
						{
							m_sourceFactorAlpha = m_commonBlendTypes[ m_currentAlphaIndex ].SourceFactor;
							m_sourceFactorAlphaInline.IntValue = (int)m_sourceFactorAlpha;
							m_sourceFactorAlphaInline.SetInlineNodeValue();

							m_destFactorAlpha = m_commonBlendTypes[ m_currentAlphaIndex ].DestFactor;
							m_destFactorAlphaInline.IntValue = (int)m_destFactorAlpha;
							m_destFactorAlphaInline.SetInlineNodeValue();
						}
					}
					EditorGUI.BeginDisabledGroup( m_currentAlphaIndex == 0 );

					EditorGUI.BeginChangeCheck();
					float cached = EditorGUIUtility.labelWidth;
					if( style )
					{
						EditorGUIUtility.labelWidth = 40;
					}
					else
					{
						EditorGUIUtility.labelWidth = 25;
					}
					EditorGUILayout.BeginHorizontal();
					//m_sourceFactorAlpha = (AvailableBlendFactor)owner.EditorGUILayoutEnumPopup( SourceFactorStr, m_sourceFactorAlpha );
					m_sourceFactorAlphaInline.CustomDrawer( ref owner, ( x ) => { m_sourceFactorAlpha = (AvailableBlendFactor)x.EditorGUILayoutEnumPopup( SourceFactorStr, m_sourceFactorAlpha ); }, SourceFactorStr );
					if( style )
					{
						EditorGUI.indentLevel--;
						EditorGUIUtility.labelWidth = 25;
					}
					//m_destFactorAlpha = (AvailableBlendFactor)owner.EditorGUILayoutEnumPopup( DstFactorStr, m_destFactorAlpha );
					m_destFactorAlphaInline.CustomDrawer( ref owner, ( x ) => { m_destFactorAlpha = (AvailableBlendFactor)x.EditorGUILayoutEnumPopup( DstFactorStr, m_destFactorAlpha ); }, DstFactorStr );
					if( style )
						EditorGUI.indentLevel++;
					EditorGUILayout.EndHorizontal();
					EditorGUIUtility.labelWidth = cached;

					if( EditorGUI.EndChangeCheck() )
					{
						CheckAlphaIndex();
					}

					EditorGUI.EndDisabledGroup();
					//EditorGUILayout.Separator();
				}

				if( m_blendOpEnabled )
				{
					if( (int)m_blendOpAlpha == -1 )
						m_blendOpAlpha = AvailableBlendOps.OFF;

					//m_blendOpAlpha = (AvailableBlendOps)owner.EditorGUILayoutEnumPopup( BlendOpsAlphaStr, m_blendOpAlpha );
					m_blendOpAlphaInline.CustomDrawer( ref owner, ( x ) => { m_blendOpAlpha = (AvailableBlendOps)x.EditorGUILayoutPopup( BlendOpsAlphaStr, (int)m_blendOpAlpha, BlendOpsHelper.BlendOpsLabels ); }, BlendOpsAlphaStr );
				}

				EditorGUIUtility.labelWidth = cache;
			}

			if( EditorGUI.EndChangeCheck() )
			{
				m_isDirty = true;
				CustomEdited = true;
			}
		}

		void CheckRGBIndex()
		{
			int count = m_commonBlendTypes.Count;
			m_currentRGBIndex = 1;
			for( int i = 1; i < count; i++ )
			{
				if( m_commonBlendTypes[ i ].SourceFactor == m_sourceFactorRGB && m_commonBlendTypes[ i ].DestFactor == m_destFactorRGB )
				{
					m_currentRGBIndex = i;
					return;
				}
			}

		}

		void CheckAlphaIndex()
		{
			int count = m_commonBlendTypes.Count;
			m_currentAlphaIndex = 1;
			for( int i = 1; i < count; i++ )
			{
				if( m_commonBlendTypes[ i ].SourceFactor == m_sourceFactorAlpha && m_commonBlendTypes[ i ].DestFactor == m_destFactorAlpha )
				{
					m_currentAlphaIndex = i;
					if( m_currentAlphaIndex > 0 && m_currentRGBIndex == 0 )
						m_currentRGBIndex = 1;
					return;
				}
			}

			if( m_currentAlphaIndex > 0 && m_currentRGBIndex == 0 )
				m_currentRGBIndex = 1;
		}

		public void ReadAlphaToMaskFromString( ref uint index, ref string[] nodeParams )
		{
			//TODO: we should send this data to the alpha to mask module instead
			if( UIUtils.CurrentShaderVersion() > 16102 && UIUtils.CurrentShaderVersion() <= 18103)
			{
				bool validAlphaToMask = Convert.ToBoolean( nodeParams[ index++ ] );
				if( validAlphaToMask )
				{
					/*bool alphaToMaskValue = */Convert.ToBoolean( nodeParams[ index++ ] );
				}
			}
		}

		public void ReadBlendModeFromString( ref uint index, ref string[] nodeParams )
		{
			bool validDataOnMeta = m_validBlendMode;
			if( UIUtils.CurrentShaderVersion() > TemplatesManager.MPShaderVersion )
			{
				validDataOnMeta = Convert.ToBoolean( nodeParams[ index++ ] );
			}

			if( validDataOnMeta )
			{
				if( UIUtils.CurrentShaderVersion() < 15304 )
				{
					m_currentRGBIndex = Convert.ToInt32( nodeParams[ index++ ] );
					m_sourceFactorRGB = (AvailableBlendFactor)Enum.Parse( typeof( AvailableBlendFactor ), nodeParams[ index++ ] );
					m_destFactorRGB = (AvailableBlendFactor)Enum.Parse( typeof( AvailableBlendFactor ), nodeParams[ index++ ] );

					m_currentAlphaIndex = Convert.ToInt32( nodeParams[ index++ ] );
					m_sourceFactorAlpha = (AvailableBlendFactor)Enum.Parse( typeof( AvailableBlendFactor ), nodeParams[ index++ ] );
					m_destFactorAlpha = (AvailableBlendFactor)Enum.Parse( typeof( AvailableBlendFactor ), nodeParams[ index++ ] );
				}
				else
				{
					m_currentRGBIndex = Convert.ToInt32( nodeParams[ index++ ] );
					m_sourceFactorRGBInline.ReadFromString( ref index, ref nodeParams );
					m_sourceFactorRGB = (AvailableBlendFactor)m_sourceFactorRGBInline.IntValue;
					m_destFactorRGBInline.ReadFromString( ref index, ref nodeParams );
					m_destFactorRGB = (AvailableBlendFactor)m_destFactorRGBInline.IntValue;

					m_currentAlphaIndex = Convert.ToInt32( nodeParams[ index++ ] );
					m_sourceFactorAlphaInline.ReadFromString( ref index, ref nodeParams );
					m_sourceFactorAlpha = (AvailableBlendFactor)m_sourceFactorAlphaInline.IntValue;
					m_destFactorAlphaInline.ReadFromString( ref index, ref nodeParams );
					m_destFactorAlpha = (AvailableBlendFactor)m_destFactorAlphaInline.IntValue;
				}
			}
		}

		public void ReadBlendOpFromString( ref uint index, ref string[] nodeParams )
		{
			bool validDataOnMeta = m_validBlendOp;
			if( UIUtils.CurrentShaderVersion() > TemplatesManager.MPShaderVersion )
			{
				validDataOnMeta = Convert.ToBoolean( nodeParams[ index++ ] );
			}

			if( validDataOnMeta )
			{
				if( UIUtils.CurrentShaderVersion() < 15304 )
				{
					m_blendOpRGB = (AvailableBlendOps)Enum.Parse( typeof( AvailableBlendOps ), nodeParams[ index++ ] );
					m_blendOpAlpha = (AvailableBlendOps)Enum.Parse( typeof( AvailableBlendOps ), nodeParams[ index++ ] );
				}
				else
				{
					m_blendOpRGBInline.ReadFromString( ref index, ref nodeParams );
					m_blendOpAlphaInline.ReadFromString( ref index, ref nodeParams );

					if( UIUtils.CurrentShaderVersion() < 15404 )
					{
						// Now BlendOps enum starts at 0 and not -1
						m_blendOpRGBInline.FloatValue += 1;
						m_blendOpAlphaInline.FloatValue += 1;
					}

					m_blendOpRGB = (AvailableBlendOps)m_blendOpRGBInline.IntValue;
					m_blendOpAlpha = (AvailableBlendOps)m_blendOpAlphaInline.IntValue;
				}
				//m_blendOpEnabled = ( m_blendOpRGB != AvailableBlendOps.OFF );
			}
		}

		public void WriteBlendModeToString( ref string nodeInfo )
		{
			IOUtils.AddFieldValueToString( ref nodeInfo, m_validBlendMode );
			if( m_validBlendMode )
			{
				IOUtils.AddFieldValueToString( ref nodeInfo, m_currentRGBIndex );
				if( !m_sourceFactorRGBInline.IsValid ) m_sourceFactorRGBInline.IntValue = (int)m_sourceFactorRGB;
				m_sourceFactorRGBInline.WriteToString( ref nodeInfo );

				if( !m_destFactorRGBInline.IsValid ) m_destFactorRGBInline.IntValue = (int)m_destFactorRGB;
				m_destFactorRGBInline.WriteToString( ref nodeInfo );

				IOUtils.AddFieldValueToString( ref nodeInfo, m_currentAlphaIndex );
				if( !m_sourceFactorAlphaInline.IsValid ) m_sourceFactorAlphaInline.IntValue = (int)m_sourceFactorAlpha;
				m_sourceFactorAlphaInline.WriteToString( ref nodeInfo );

				if( !m_destFactorAlphaInline.IsValid ) m_destFactorAlphaInline.IntValue = (int)m_destFactorAlpha;
				m_destFactorAlphaInline.WriteToString( ref nodeInfo );
			}
		}

		public void WriteBlendOpToString( ref string nodeInfo )
		{
			IOUtils.AddFieldValueToString( ref nodeInfo, m_validBlendOp );
			if( m_validBlendOp )
			{
				if( !m_blendOpRGBInline.IsValid ) m_blendOpRGBInline.IntValue = (int)m_blendOpRGB;
				m_blendOpRGBInline.WriteToString( ref nodeInfo );

				if( !m_blendOpAlphaInline.IsValid ) m_blendOpAlphaInline.IntValue = (int)m_blendOpAlpha;
				m_blendOpAlphaInline.WriteToString( ref nodeInfo );
			}
		}

		public override void ReadFromString( ref uint index, ref string[] nodeParams )
		{
			base.ReadFromString( ref index, ref nodeParams );
			ReadBlendModeFromString( ref index, ref nodeParams );
			ReadBlendOpFromString( ref index, ref nodeParams );
			ReadAlphaToMaskFromString( ref index, ref nodeParams );
		}

		public override void WriteToString( ref string nodeInfo )
		{
			base.WriteToString( ref nodeInfo );
			WriteBlendModeToString( ref nodeInfo );
			WriteBlendOpToString( ref nodeInfo );
		}

		public override void Destroy()
		{
			base.Destroy();
			m_sourceFactorRGBInline = null;
			m_destFactorRGBInline = null;
			m_sourceFactorAlphaInline = null;
			m_destFactorAlphaInline = null;
			m_blendOpRGBInline = null;
			m_blendOpAlphaInline = null;
		}

		public string CurrentBlendFactorSingle
		{
			get
			{
				return ( m_currentRGBIndex > 0 ) ? string.Format( SingleBlendFactorStr, m_target, m_sourceFactorRGBInline.GetValueOrProperty( m_sourceFactorRGB.ToString() ), m_destFactorRGBInline.GetValueOrProperty( m_destFactorRGB.ToString() ) ) : BlendFactorOff;
			}
		}

		public string CurrentBlendFactorSeparate
		{
			get
			{
				return string.Format( SeparateBlendFactorStr, m_target,
				m_sourceFactorRGBInline.GetValueOrProperty( ( m_currentRGBIndex > 0 ? m_sourceFactorRGB.ToString() : AvailableBlendFactor.One.ToString() ) ),
				m_destFactorRGBInline.GetValueOrProperty( m_currentRGBIndex > 0 ? m_destFactorRGB.ToString() : AvailableBlendFactor.Zero.ToString() ),
				m_sourceFactorAlphaInline.GetValueOrProperty( m_sourceFactorAlpha.ToString() ),
				m_destFactorAlphaInline.GetValueOrProperty( m_destFactorAlpha.ToString() ) );
			}
		}

		public string CurrentBlendFactor
		{
			get
			{
				return ( ( m_currentAlphaIndex > 0 ) ? CurrentBlendFactorSeparate : CurrentBlendFactorSingle );
			}
		}


		public string CurrentBlendOpSingle
		{
			get
			{
				return ( m_blendOpRGB != AvailableBlendOps.OFF || m_blendOpRGBInline.IsValid ) ? string.Format( SingleBlendOpStr, Target, m_blendOpRGBInline.GetValueOrProperty( m_blendOpRGB.ToString() ) ) : string.Empty;
			}
		}

		public string CurrentBlendOpSeparate
		{
			get
			{
				return string.Format( SeparateBlendOpStr, Target, m_blendOpRGBInline.GetValueOrProperty( ( m_currentRGBIndex > 0 && m_blendOpRGB != AvailableBlendOps.OFF ) ? m_blendOpRGB.ToString() : AvailableBlendOps.Add.ToString() ), m_blendOpAlphaInline.GetValueOrProperty( m_blendOpAlpha.ToString() ) );
			}
		}

		public string CurrentBlendOp { get { return ( ( m_blendOpAlpha != AvailableBlendOps.OFF || m_blendOpAlphaInline.IsValid ) ? CurrentBlendOpSeparate : CurrentBlendOpSingle ); } }
		public bool Active { get { return m_blendModeEnabled && ( m_currentRGBIndex > 0 || m_currentAlphaIndex > 0 ); } }
		public bool BlendOpActive
		{
			get
			{
				return m_blendOpEnabled &&
					(
					m_blendOpRGBInline.Active ||
					m_blendOpAlphaInline.Active ||
					( !m_blendOpRGBInline.Active && m_blendOpRGB != AvailableBlendOps.OFF ) ||
					( !m_blendOpAlphaInline.Active && m_blendOpAlpha != AvailableBlendOps.OFF ) );
			}
		}

		public string Target { get { return m_target; } }
		public bool ValidBlendMode { get { return m_validBlendMode; } }
		public bool ValidBlendOp { get { return m_validBlendOp; } }
		public int CurrentRGBIndex { get { return m_currentRGBIndex; } }

		public AvailableBlendFactor SourceFactorRGB
		{
			get { return m_sourceFactorRGB; }
			set
			{
				m_sourceFactorRGB = value;
				m_sourceFactorRGBInline.IntValue = (int)value;
				m_sourceFactorRGBInline.Active = false;
			}

		}
		public AvailableBlendFactor DestFactorRGB
		{
			get { return m_destFactorRGB; }
			set
			{
				m_destFactorRGB = value;
				m_destFactorRGBInline.IntValue = (int)value;
				m_destFactorRGBInline.Active = false;
			}
		}

		public int CurrentAlphaIndex { get { return m_currentAlphaIndex; } set { m_currentAlphaIndex = value; } }

		public AvailableBlendFactor SourceFactorAlpha
		{
			get { return m_sourceFactorAlpha; }
			set
			{
				m_sourceFactorAlpha = value;
				m_sourceFactorAlphaInline.IntValue = (int)value;
				m_sourceFactorAlphaInline.Active = false;
			}
		}

		public AvailableBlendFactor DestFactorAlpha
		{
			get { return m_destFactorAlpha; }
			set
			{
				m_destFactorAlpha = value;
				m_destFactorAlphaInline.IntValue = (int)value;
				m_destFactorAlphaInline.Active = false;

			}
		}

		public bool BlendModeEnabled { get { return m_blendModeEnabled; } }
		public bool BlendOpEnabled { get { return m_blendOpEnabled; } }
		public AvailableBlendOps BlendOpRGB
		{
			get { return m_blendOpRGB; }
			set
			{
				m_blendOpRGB = value;
				m_blendOpRGBInline.IntValue = (int)value;
				m_blendOpRGBInline.Active = false;
			}
		}

		public AvailableBlendOps BlendOpAlpha
		{
			get { return m_blendOpAlpha; }
			set
			{
				m_blendOpAlpha = value;
				m_blendOpAlphaInline.IntValue = (int)value;
				m_blendOpAlphaInline.Active = false;
			}
		}

		public InlineProperty SourceFactorRGBInline { get { return m_sourceFactorRGBInline; } }
		public InlineProperty DestFactorRGBInline { get { return m_destFactorRGBInline; } }
		public InlineProperty SourceFactorAlphaInline { get { return m_sourceFactorAlphaInline; } }
		public InlineProperty DestFactorAlphaInline { get { return m_destFactorAlphaInline; } }
		public InlineProperty BlendOpRGBInline { get { return m_blendOpRGBInline; } }
		public InlineProperty BlendOpAlphaInline { get { return m_blendOpAlphaInline; } }
		public bool IsAdditiveRGB { get { return m_validBlendMode && m_blendModeEnabled && ( m_currentRGBIndex > 0 ) && ( m_sourceFactorRGB == AvailableBlendFactor.One ) && ( m_destFactorRGB == AvailableBlendFactor.One ); } }
		public bool IsAlphaBlendRGB { get { return m_validBlendMode && m_blendModeEnabled && ( m_currentRGBIndex > 0 ) && ( m_sourceFactorRGB == AvailableBlendFactor.SrcAlpha ) && ( m_destFactorRGB == AvailableBlendFactor.OneMinusSrcAlpha ); } }
	}
}
