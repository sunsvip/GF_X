// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using UnityEngine;
using UnityEditor;
using System;

namespace AmplifyShaderEditor
{
	[Serializable]
	public sealed class TemplateAlphaToMaskModule : TemplateModuleParent
	{
		private const string AlphaToMaskFormatStr = "AlphaToMask ";
		private const string AlphaToMaskFormat = "AlphaToMask {0}";

		public TemplateAlphaToMaskModule() : base( "Alpha To Coverage" ) { }

        private static readonly string AlphaToMaskStr = "Alpha To Coverage";

		[SerializeField]
		private bool m_alphaToMask = false;

		[SerializeField]
		private InlineProperty m_inlineAlphaToMask = new InlineProperty();
		
		public void CopyFrom( TemplateAlphaToMaskModule other , bool allData )
		{
			if( allData )
				m_independentModule = other.IndependentModule;

			m_alphaToMask = other.AlphaToMask;
			m_inlineAlphaToMask.CopyFrom( other.AlphaToMaskInlineProperty );
		}

		public void ConfigureFromTemplateData( TemplateAlphaToMaskData data )
		{
			bool newValidData = ( data.DataCheck == TemplateDataCheck.Valid );

			if( newValidData && m_validData != newValidData )
			{
				m_independentModule = data.IndependentModule;
				if( string.IsNullOrEmpty( data.InlineData ) )
				{
					m_alphaToMask = data.AlphaToMaskData;
					m_inlineAlphaToMask.IntValue = m_alphaToMask ? 1 : 0;
					m_inlineAlphaToMask.ResetProperty();
				}
				else
				{
					m_inlineAlphaToMask.SetInlineByName( data.InlineData );
				}
			}

			m_validData = newValidData;
		}

		public override void Draw( UndoParentNode owner, bool style = true )
		{
			EditorGUI.BeginChangeCheck();
			m_inlineAlphaToMask.CustomDrawer( ref owner, ( x ) => { m_alphaToMask = owner.EditorGUILayoutToggle( AlphaToMaskStr, m_alphaToMask ); }, AlphaToMaskStr );
			if( EditorGUI.EndChangeCheck() )
			{
				m_inlineAlphaToMask.IntValue = m_alphaToMask ? 1 : 0;
				m_isDirty = true;
			}
		}

		public override void ReadFromString( ref uint index, ref string[] nodeParams )
		{
			bool validDataOnMeta = m_validData;
			if( UIUtils.CurrentShaderVersion() > TemplatesManager.MPShaderVersion )
			{
				validDataOnMeta = Convert.ToBoolean( nodeParams[ index++ ] );
			}

			if( validDataOnMeta )
			{
				if( UIUtils.CurrentShaderVersion() < 15304 )
				{
					m_alphaToMask = Convert.ToBoolean( nodeParams[ index++ ] );
					m_inlineAlphaToMask.IntValue = m_alphaToMask ? 1 : 0;
				}
				else
				{
					m_inlineAlphaToMask.ReadFromString( ref index, ref nodeParams );
					m_alphaToMask = m_inlineAlphaToMask.IntValue == 1;
				}
			}
		}

		public override void WriteToString( ref string nodeInfo )
		{
			IOUtils.AddFieldValueToString( ref nodeInfo, m_validData );
			if( m_validData )
			{
				m_inlineAlphaToMask.WriteToString( ref nodeInfo );
			}
		}

		public override string GenerateShaderData( bool isSubShader )
		{
			return AlphaToMaskFormatStr + m_inlineAlphaToMask.GetValueOrProperty( m_alphaToMask ? "On" : "Off");
		}
		
		public override void Destroy()
		{
			base.Destroy();
			m_inlineAlphaToMask = null;
		}

		//public string CurrentAlphaToMask
		//{
		//	get { return string.Format( AlphaToMaskFormat, m_alphaToMask ? "On" : "Off" ); }
		//}

		public bool AlphaToMask
		{
			get { return m_alphaToMask; }
			set
			{
				m_alphaToMask = value;
				m_inlineAlphaToMask.IntValue = value ? 1 : 0;
				m_inlineAlphaToMask.Active = false;
			}
		}
		public InlineProperty AlphaToMaskInlineProperty { get { return m_inlineAlphaToMask; } }
	}
}
