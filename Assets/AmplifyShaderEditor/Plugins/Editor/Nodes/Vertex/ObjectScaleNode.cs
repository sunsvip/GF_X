// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using System;
using UnityEngine;
using UnityEditor;
namespace AmplifyShaderEditor
{
	[Serializable]
	[NodeAttributes( "Object Scale", "Vertex Data", "Object Scale extracted directly from its transform matrix" )]
	public class ObjectScaleNode : ParentNode
	{
		private const string RotationIndependentScaleStr = "Rotation Independent Scale";

		[SerializeField]
		private bool m_rotationIndependentScale = false;
		
		protected override void CommonInit( int uniqueId )
		{
			base.CommonInit( uniqueId );
			AddOutputVectorPorts( WirePortDataType.FLOAT3, "XYZ" );
			m_drawPreviewAsSphere = true;
			m_previewShaderGUID = "5540033c6c52f51468938c1a42bd2730";
			m_textLabelWidth = 180;
			UpdateMaterialPass();
			m_autoWrapProperties = true;
		}

		public override void DrawProperties()
		{
			base.DrawProperties();
			EditorGUI.BeginChangeCheck();
			m_rotationIndependentScale = EditorGUILayoutToggle( RotationIndependentScaleStr, m_rotationIndependentScale );
			if( EditorGUI.EndChangeCheck() )
			{
				UpdateMaterialPass();
			}
		}

		public override string GenerateShaderForOutput( int outputId, ref MasterNodeDataCollector dataCollector, bool ignoreLocalvar )
		{
			string objectScale = m_rotationIndependentScale ?	GeneratorUtils.GenerateRotationIndependentObjectScale( ref dataCollector, UniqueId ):
														GeneratorUtils.GenerateObjectScale( ref dataCollector, UniqueId );

			return GetOutputVectorItem( 0, outputId, objectScale );
		}

		public override void ReadFromString( ref string[] nodeParams )
		{
			base.ReadFromString( ref nodeParams );
			if( UIUtils.CurrentShaderVersion() < 17402 )
			{
				m_rotationIndependentScale = false;
			}
			else
			{
				m_rotationIndependentScale = Convert.ToBoolean( GetCurrentParam( ref nodeParams ) );
			}
			UpdateMaterialPass();
		}

		public override void WriteToString( ref string nodeInfo, ref string connectionsInfo )
		{
			base.WriteToString( ref nodeInfo, ref connectionsInfo );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_rotationIndependentScale );
		}

		void UpdateMaterialPass()
		{
			m_previewMaterialPassId = m_rotationIndependentScale ? 1 : 0;
			PreviewIsDirty = true;
		}

	}
}
