// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using System;
using UnityEngine;
using UnityEditor;

namespace AmplifyShaderEditor
{
	[Serializable]
	public class TemplateModuleParent
	{
		private const string UnreadableDataMessagePrefix = "Unreadable data on Module ";
		protected string m_unreadableMessage;

		[SerializeField]
		protected bool m_validData = false;

		[SerializeField]
		protected bool m_isDirty = false;

		[SerializeField]
		protected string m_moduleName = string.Empty;

		//[SerializeField]
		//protected bool m_foldoutValue = false;

		[SerializeField]
		protected bool m_independentModule = true;

		[SerializeField]
		private bool m_customEdited = false;

		public TemplateModuleParent( string moduleName ) { m_moduleName = moduleName; m_unreadableMessage = UnreadableDataMessagePrefix + moduleName; }
		public virtual void Draw( UndoParentNode owner , bool style = true) { }
		public virtual void ReadFromString( ref uint index, ref string[] nodeParams )
		{
			if( UIUtils.CurrentShaderVersion() > 18805 )
			{
				CustomEdited = Convert.ToBoolean( nodeParams[ index++ ] );
			}
		}

		public virtual void WriteToString( ref string nodeInfo )
		{
			IOUtils.AddFieldValueToString( ref nodeInfo, m_customEdited );
		}

		public virtual string GenerateShaderData( bool isSubShader ) { return string.Empty; }
		public virtual void Destroy() { }
		public bool ValidData { get { return m_validData; } }
		public bool ValidAndIndependent { get { return m_validData && m_independentModule; } }

		public virtual void ShowUnreadableDataMessage( ParentNode owner )
		{
			ShowUnreadableDataMessage();
		}

		public virtual void ShowUnreadableDataMessage()
		{
			EditorGUILayout.HelpBox( m_unreadableMessage, MessageType.Info );
		}

		public bool IsDirty
		{
			get { return m_isDirty; }
			set { m_isDirty = value; }
		}

		public bool IndependentModule
		{
			get { return m_independentModule; }
			set { m_independentModule = value; }
		}

		public bool CustomEdited
		{
			get { return m_customEdited; }
			set	{ m_customEdited = value; }
		}
	}
}
