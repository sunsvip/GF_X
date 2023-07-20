// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using System;
using UnityEditor;

namespace AmplifyShaderEditor
{
	using UnityEngine;

	[Serializable]
	[NodeAttributes( "HD Emission", "Miscellaneous", "Get emission HDR Color. Only available on HDRP." )]
	public sealed class HDEmissionNode : ParentNode
	{
		public enum HDEmissionIntensityUnit
		{
			Luminance,
			EV100
		};

		public readonly string[] EmissionFunctionWithNormalize =
		{
			"float3 ASEGetEmissionHDRColorNormalize(float3 ldrColor, float luminanceIntensity, float exposureWeight, float inverseCurrentExposureMultiplier)\n",
			"{\n",
			"\tldrColor = ldrColor * rcp(max(Luminance(ldrColor), 1e-6));\n",
			"\tfloat3 hdrColor = ldrColor * luminanceIntensity;\n",
			"\thdrColor = lerp( hdrColor* inverseCurrentExposureMultiplier, hdrColor, exposureWeight);\n",
			"\treturn hdrColor;\n",
			"}\n",
		};

		public readonly string[] EmissionFunction =
		{
			"float3 ASEGetEmissionHDRColor(float3 ldrColor, float luminanceIntensity, float exposureWeight, float inverseCurrentExposureMultiplier)\n",
			"{\n",
			"\tfloat3 hdrColor = ldrColor * luminanceIntensity;\n",
			"\thdrColor = lerp( hdrColor* inverseCurrentExposureMultiplier, hdrColor, exposureWeight);\n",
			"\treturn hdrColor;\n",
			"}\n",
		};

		public const string CommonLightingLib = "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonLighting.hlsl";
		public const string EmissionHeaderLuminance = "ASEGetEmissionHDRColor{0}({1},{2},{3},GetInverseCurrentExposureMultiplier())";
		public const string EmissionHeaderEV100 = "ASEGetEmissionHDRColor{0}({1},ConvertEvToLuminance({2}),{3},GetInverseCurrentExposureMultiplier())";

		public const string IntensityUnityLabel = "Intensity Unit";

		public const string NormalizeColorLabel = "Normalize Color";
		public const string ErrorOnCompilationMsg = "Attempting to use HDRP specific node on incorrect SRP or Builtin RP.";
		public const string MinorVersionMsg = "This node require at least Unity 2019.1/HDRP v5 to properly work.";
		public const string NodeErrorMsg = "Only valid on HDRP";
		public const string MinorNodeErrorMsg = "Invalid Unity/HDRP version";

		[SerializeField]
		private HDEmissionIntensityUnit m_intensityUnit = HDEmissionIntensityUnit.Luminance;

		[SerializeField]
		private bool m_normalizeColor = false;

		protected override void CommonInit( int uniqueId )
		{
			base.CommonInit( uniqueId );
			AddInputPort( WirePortDataType.FLOAT3, false, "Color" );
			AddInputPort( WirePortDataType.FLOAT, false, "Intensity" );
			AddInputPort( WirePortDataType.FLOAT, false, "Exposition Weight" );
			AddOutputPort( WirePortDataType.FLOAT3, Constants.EmptyPortValue );

			m_errorMessageTooltip = NodeErrorMsg;
			m_errorMessageTypeIsError = NodeMessageType.Error;
			m_autoWrapProperties = true;
		}

		public override void DrawProperties()
		{
			base.DrawProperties();
			m_intensityUnit = (HDEmissionIntensityUnit)EditorGUILayoutEnumPopup( IntensityUnityLabel, m_intensityUnit );
			m_normalizeColor = EditorGUILayoutToggle( NormalizeColorLabel, m_normalizeColor );
			if( m_showErrorMessage )
			{
				EditorGUILayout.HelpBox( NodeErrorMsg , MessageType.Error );
			}
		}

		public override string GenerateShaderForOutput( int outputId, ref MasterNodeDataCollector dataCollector, bool ignoreLocalvar )
		{
			if( !dataCollector.IsSRP || !dataCollector.TemplateDataCollectorInstance.IsHDRP )
			{
				UIUtils.ShowMessage( ErrorOnCompilationMsg , MessageSeverity.Error );
				return GenerateErrorValue();
			}

			if( m_outputPorts[ 0 ].IsLocalValue( dataCollector.PortCategory ) )
				return m_outputPorts[ 0 ].LocalValue( dataCollector.PortCategory );

#if UNITY_2020_3_OR_NEWER
			dataCollector.AddToIncludes( UniqueId , CommonLightingLib );
#endif
			string colorValue = m_inputPorts[ 0 ].GeneratePortInstructions( ref dataCollector );
			string intensityValue = m_inputPorts[ 1 ].GeneratePortInstructions( ref dataCollector );
			string expositionWeightValue = m_inputPorts[ 2 ].GeneratePortInstructions( ref dataCollector );
			string functionPrefix = string.Empty;
			if( m_normalizeColor )
			{
				functionPrefix = "Normalize";
				dataCollector.AddFunction( EmissionFunctionWithNormalize[ 0 ], EmissionFunctionWithNormalize , false);
			}
			else
			{
				dataCollector.AddFunction( EmissionFunction[ 0 ], EmissionFunction, false );
			}

			string varName = "hdEmission" + OutputId;
			string varValue = string.Empty;
			switch( m_intensityUnit )
			{
				default:
				case HDEmissionIntensityUnit.Luminance:
				varValue = string.Format( EmissionHeaderLuminance, functionPrefix, colorValue, intensityValue, expositionWeightValue );
				break;
				case HDEmissionIntensityUnit.EV100:
				varValue = string.Format( EmissionHeaderEV100, functionPrefix, colorValue, intensityValue, expositionWeightValue );
				break;
			}

			dataCollector.AddLocalVariable( UniqueId, CurrentPrecisionType, m_outputPorts[ 0 ].DataType, varName, varValue );
			m_outputPorts[ 0 ].SetLocalValue( varName, dataCollector.PortCategory );
			return varName;
		}

		public override void OnNodeLogicUpdate( DrawInfo drawInfo )
		{
			base.OnNodeLogicUpdate( drawInfo );
			m_showErrorMessage = ( ContainerGraph.CurrentCanvasMode == NodeAvailability.SurfaceShader ) ||
									( ContainerGraph.CurrentCanvasMode == NodeAvailability.TemplateShader && ContainerGraph.CurrentSRPType != TemplateSRPType.HDRP );
		}
		
		public override void ReadFromString( ref string[] nodeParams )
		{
			base.ReadFromString( ref nodeParams );
			Enum.TryParse<HDEmissionIntensityUnit>( GetCurrentParam( ref nodeParams ), out m_intensityUnit );
			m_normalizeColor =  Convert.ToBoolean( GetCurrentParam( ref nodeParams ) );
		}

		public override void WriteToString( ref string nodeInfo, ref string connectionsInfo )
		{
			base.WriteToString( ref nodeInfo, ref connectionsInfo );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_intensityUnit );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_normalizeColor );
		}
	}
}
//#endif
