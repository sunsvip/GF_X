// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using UnityEngine;
using UnityEditor;
using System;

namespace AmplifyShaderEditor
{
	[Serializable]
	[NodeAttributes( "Normalize", "Vector Operators", "Normalizes a vector", null, KeyCode.N )]
	public sealed class NormalizeNode : SingleInputOp
	{
		[SerializeField]
		private bool m_safeNormalize = false;

		protected override void CommonInit( int uniqueId )
		{
			base.CommonInit( uniqueId );
			m_selectedLocation = PreviewLocation.TopCenter;
			m_inputPorts[ 0 ].ChangeType( WirePortDataType.FLOAT4, false );
			m_inputPorts[ 0 ].CreatePortRestrictions( WirePortDataType.FLOAT, WirePortDataType.FLOAT2, WirePortDataType.FLOAT3, WirePortDataType.FLOAT4, WirePortDataType.COLOR, WirePortDataType.OBJECT, WirePortDataType.INT );

			m_outputPorts[ 0 ].ChangeType( WirePortDataType.FLOAT4 , false );

			m_previewShaderGUID = "a51b11dfb6b32884e930595e5f9defa8";
			m_autoWrapProperties = true;
		}

		public override void DrawProperties()
		{
			base.DrawProperties();
			m_safeNormalize = EditorGUILayoutToggle( "Safe Normalize" , m_safeNormalize );
			EditorGUILayout.HelpBox( Constants.SafeNormalizeInfoStr , MessageType.Info );
		}

		public override string GenerateShaderForOutput( int outputId, ref MasterNodeDataCollector dataCollector, bool ignoreLocalvar )
		{
			if ( m_outputPorts[ 0 ].IsLocalValue( dataCollector.PortCategory ) )
				return m_outputPorts[ 0 ].LocalValue( dataCollector.PortCategory );

			string result = string.Empty;
			switch ( m_inputPorts[ 0 ].DataType )
			{
				case WirePortDataType.FLOAT:
				case WirePortDataType.FLOAT2:
				case WirePortDataType.FLOAT3:
				case WirePortDataType.FLOAT4:
				case WirePortDataType.OBJECT:
				case WirePortDataType.COLOR:
				{
					string value = m_inputPorts[ 0 ].GeneratePortInstructions( ref dataCollector );
					result = GeneratorUtils.NormalizeValue( ref dataCollector , m_safeNormalize , m_inputPorts[ 0 ].DataType, value );
				}
				break;
				case WirePortDataType.INT:
				{
					return m_inputPorts[ 0 ].GeneratePortInstructions( ref dataCollector  );
				}
				case WirePortDataType.FLOAT3x3:
				case WirePortDataType.FLOAT4x4:
				{
					result = UIUtils.InvalidParameter( this );
				}
				break;
			}
			RegisterLocalVariable( 0, result, ref dataCollector, "normalizeResult" + OutputId );

			return m_outputPorts[ 0 ].LocalValue( dataCollector.PortCategory );
		}

		public override void ReadFromString( ref string[] nodeParams )
		{
			base.ReadFromString( ref nodeParams );
			if( UIUtils.CurrentShaderVersion() > 18814 )
			{
				m_safeNormalize = Convert.ToBoolean( GetCurrentParam( ref nodeParams ) );
			}
		}

		public override void WriteToString( ref string nodeInfo , ref string connectionsInfo )
		{
			base.WriteToString( ref nodeInfo , ref connectionsInfo );
			IOUtils.AddFieldValueToString( ref nodeInfo , m_safeNormalize );
		}
	}
}
