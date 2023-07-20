// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using System;
using UnityEngine;
using UnityEditor;

namespace AmplifyShaderEditor
{
	[Serializable]
	[NodeAttributes( "Linear Depth", "Miscellaneous", "Converts depth values given on logarithmic space to linear" )]
	public sealed class LinearDepthNode : ParentNode
	{
		private readonly string[] LinearModeLabels = { "Eye Space", "0-1 Space" };

		private const string LinearEyeFuncFormat = "LinearEyeDepth({0})";
		private const string Linear01FuncFormat = "Linear01Depth({0})";

		private const string LinearEyeFuncSRPFormat = "LinearEyeDepth({0},_ZBufferParams)";
		private const string Linear01FuncSRPFormat = "Linear01Depth({0},_ZBufferParams)";

		private const string LinerValName = "depthToLinear";
		private const string ViewSpaceLabel = "View Space";

		
		private UpperLeftWidgetHelper m_upperLeftWidget = new UpperLeftWidgetHelper();

		[SerializeField]
		private int m_currentOption = 0;

		protected override void CommonInit( int uniqueId )
		{
			base.CommonInit( uniqueId );
			AddInputPort( WirePortDataType.FLOAT, false, Constants.EmptyPortValue );
			AddOutputPort( WirePortDataType.FLOAT, Constants.EmptyPortValue );
			m_autoWrapProperties = true;
			m_hasLeftDropdown = true;
			m_previewShaderGUID = "2b0785cc8b854974ab4e45419072705a";
			UpdateFromOption();
		}

		public override void Destroy()
		{
			base.Destroy();
			m_upperLeftWidget = null;
		}

		void UpdateFromOption()
		{
			m_previewMaterialPassId = m_currentOption;
			SetAdditonalTitleText( string.Format( Constants.SubTitleSpaceFormatStr, LinearModeLabels[ m_currentOption ] ) );
		}

		public override void Draw( DrawInfo drawInfo )
		{
			base.Draw( drawInfo );
			EditorGUI.BeginChangeCheck();
			m_currentOption = m_upperLeftWidget.DrawWidget( this, m_currentOption, LinearModeLabels );
			if( EditorGUI.EndChangeCheck() )
			{
				UpdateFromOption();
			}
		}

		public override void DrawProperties()
		{
			base.DrawProperties();
			EditorGUI.BeginChangeCheck();
			m_currentOption = EditorGUILayoutPopup( ViewSpaceLabel, m_currentOption, LinearModeLabels );
			if( EditorGUI.EndChangeCheck() )
			{
				SetAdditonalTitleText( string.Format( Constants.SubTitleSpaceFormatStr, LinearModeLabels[ m_currentOption ] ) );
			}
		}

		public override string GenerateShaderForOutput( int outputId, ref MasterNodeDataCollector dataCollector, bool ignoreLocalvar )
		{
			if( m_outputPorts[ 0 ].IsLocalValue( dataCollector.PortCategory ) )
				return m_outputPorts[ 0 ].LocalValue( dataCollector.PortCategory );

			base.GenerateShaderForOutput( outputId, ref dataCollector, ignoreLocalvar );

			string value =  m_inputPorts[ 0 ].GeneratePortInstructions( ref dataCollector );
			if( dataCollector.IsSRP )
			{
				value = string.Format( ( ( m_currentOption == 0 ) ? LinearEyeFuncSRPFormat : Linear01FuncSRPFormat ), value );
			}
			else
			{
				value = string.Format( ( ( m_currentOption == 0 ) ? LinearEyeFuncFormat : Linear01FuncFormat ), value );
			}
			RegisterLocalVariable( 0, value, ref dataCollector, LinerValName + OutputId );
			return m_outputPorts[ 0 ].LocalValue( dataCollector.PortCategory );
		}

		public override void WriteToString( ref string nodeInfo, ref string connectionsInfo )
		{
			base.WriteToString( ref nodeInfo, ref connectionsInfo );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_currentOption );
		}

		public override void ReadFromString( ref string[] nodeParams )
		{
			base.ReadFromString( ref nodeParams );
			int.TryParse( GetCurrentParam( ref nodeParams ), out m_currentOption );
			UpdateFromOption();
		}
	}
}
