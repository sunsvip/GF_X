// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using System;

namespace AmplifyShaderEditor
{
	[Serializable]
	[NodeAttributes( "Instance ID", "Vertex Data", "Indicates the per-instance identifier" )]
	public class InstanceIdNode : ParentNode
	{
		private readonly string[] InstancingVariableAttrib =
		{   "uint currInstanceId = 0;",
			"#ifdef UNITY_INSTANCING_ENABLED",
			"currInstanceId = unity_InstanceID;",
			"#endif"
		};

		private const string TemplateSVInstanceIdVar = "instanceID";
		private const string InstancingInnerVariable = "currInstanceId";
		private bool m_useSVSemantic = false;

		protected override void CommonInit( int uniqueId )
		{
			base.CommonInit( uniqueId );
			AddOutputPort( WirePortDataType.INT, "Out" );
			m_previewShaderGUID = "03febce56a8cf354b90e7d5180c1dbd7";
		}

		public override void OnNodeLogicUpdate( DrawInfo drawInfo )
		{
			base.OnNodeLogicUpdate( drawInfo );
			m_autoWrapProperties = !m_containerGraph.IsStandardSurface;
		}

		public override void DrawProperties()
		{
			base.DrawProperties();

			if( !m_containerGraph.IsStandardSurface )
			{
				m_useSVSemantic = EditorGUILayoutToggle( "Use SV semantic" , m_useSVSemantic );
			}
			
		}

		public override string GenerateShaderForOutput( int outputId, ref MasterNodeDataCollector dataCollector, bool ignoreLocalvar )
		{
			if( dataCollector.IsTemplate )
			{
				dataCollector.TemplateDataCollectorInstance.SetupInstancing();
				if( m_useSVSemantic )
				{
					return dataCollector.TemplateDataCollectorInstance.GetSVInstanceId( ref dataCollector );
				}
			}

			if( !dataCollector.HasLocalVariable( InstancingVariableAttrib[ 0 ] ) )
			{
				dataCollector.AddLocalVariable( UniqueId, InstancingVariableAttrib[ 0 ] ,true );
				dataCollector.AddLocalVariable( UniqueId, InstancingVariableAttrib[ 1 ] ,true );
				dataCollector.AddLocalVariable( UniqueId, InstancingVariableAttrib[ 2 ] ,true );
				dataCollector.AddLocalVariable( UniqueId, InstancingVariableAttrib[ 3 ] ,true );
			}
			return InstancingInnerVariable;
		}

		public override void ReadFromString( ref string[] nodeParams )
		{
			base.ReadFromString( ref nodeParams );
			if( UIUtils.CurrentShaderVersion() > 18915 )
			{
				m_useSVSemantic = Convert.ToBoolean( GetCurrentParam( ref nodeParams ) );
			}
		}

		public override void WriteToString( ref string nodeInfo , ref string connectionsInfo )
		{
			base.WriteToString( ref nodeInfo , ref connectionsInfo );
			IOUtils.AddFieldValueToString( ref nodeInfo , m_useSVSemantic );
		}
	}
}
