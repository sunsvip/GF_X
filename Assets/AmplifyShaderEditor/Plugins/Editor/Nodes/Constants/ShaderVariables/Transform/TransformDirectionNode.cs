// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using System;
using UnityEngine;
using UnityEditor;

namespace AmplifyShaderEditor
{
	public enum InverseTangentType
	{
		Fast,
		Precise
	}

	[Serializable]
	[NodeAttributes( "Transform Direction", "Vector Operators", "Transforms a direction vector from one space to another" )]
	public sealed class TransformDirectionNode : ParentNode
	{

		[SerializeField]
		private TransformSpaceFrom m_from = TransformSpaceFrom.Object;

		[SerializeField]
		private TransformSpaceTo m_to = TransformSpaceTo.World;

		[SerializeField]
		private bool m_normalize = false;

		[SerializeField]
		private bool m_safeNormalize = false;

		[SerializeField]
		private InverseTangentType m_inverseTangentType = InverseTangentType.Fast;

		private string InverseTBNStr = "Inverse TBN";

		private const string NormalizeOptionStr = "Normalize";
		private const string SafeNormalizeOptionStr = "Safe";

		private const string AseObjectToWorldDirVarName = "objToWorldDir";
		private const string AseObjectToWorldDirFormat = "mul( unity_ObjectToWorld, float4( {0}, 0 ) ).xyz";
		private const string AseSRPObjectToWorldDirFormat = "mul( GetObjectToWorldMatrix(), float4( {0}, 0 ) ).xyz";

		private const string AseObjectToViewDirVarName = "objToViewDir";
		private const string AseObjectToViewDirFormat = "mul( UNITY_MATRIX_IT_MV, float4( {0}, 0 ) ).xyz";
		private const string AseHDObjectToViewDirFormat = "TransformWorldToViewDir( TransformObjectToWorldDir( {0} ))";

		private const string AseWorldToObjectDirVarName = "worldToObjDir";
		private const string AseWorldToObjectDirFormat = "mul( unity_WorldToObject, float4( {0}, 0 ) ).xyz";
		private const string AseSRPWorldToObjectDirFormat = "mul( GetWorldToObjectMatrix(), float4( {0}, 0 ) ).xyz";


		private const string AseWorldToViewDirVarName = "worldToViewDir";
		private const string AseWorldToViewDirFormat = "mul( UNITY_MATRIX_V, float4( {0}, 0 ) ).xyz";

		private const string AseViewToObjectDirVarName = "viewToObjDir";
		private const string AseViewToObjectDirFormat = "mul( UNITY_MATRIX_T_MV, float4( {0}, 0 ) ).xyz";

		private const string AseViewToWorldDirVarName = "viewToWorldDir";
		private const string AseViewToWorldDirFormat = "mul( UNITY_MATRIX_I_V, float4( {0}, 0 ) ).xyz";

		///////////////////////////////////////////////////////////
		private const string AseObjectToClipDirVarName = "objectToClipDir";
		private const string AseObjectToClipDirFormat = "mul(UNITY_MATRIX_VP, mul(unity_ObjectToWorld, float4({0}, 0.0)))";
		private const string AseSRPObjectToClipDirFormat = "TransformWorldToHClipDir(TransformObjectToWorldDir({0}))";

		private const string AseWorldToClipDirVarName = "worldToClipDir";
		private const string AseWorldToClipDirFormat = "mul(UNITY_MATRIX_VP, float4({0}, 0.0))";
		private const string AseSRPWorldToClipDirFormat = "TransformWorldToHClipDir({0})";

		private const string AseViewToClipDirVarName = "viewToClipDir";
		private const string AseViewToClipDirFormat = "mul(UNITY_MATRIX_P, float4({0}, 0.0))";
		private const string AseSRPViewToClipDirFormat = "mul(GetViewToHClipMatrix(), float4({0}, 1.0))";
		//
		private const string AseClipToObjectDirVarName = "clipToObjectDir";

		private const string AseClipToObjectDirFormat = "mul( UNITY_MATRIX_IT_MV, mul( unity_CameraInvProjection,float4({0},0)) ).xyz";
		private const string AseClipToWorldDirFormat = "mul( UNITY_MATRIX_I_V, mul( unity_CameraInvProjection,float4({0},0)) ).xyz";
		private const string AseClipToViewDirFormat = " mul( unity_CameraInvProjection,float4({0},0)).xyz";
		private const string AseHDClipToObjectDirFormat = "mul( UNITY_MATRIX_I_M, mul( UNITY_MATRIX_I_VP,float4({0},0)) ).xyz";

		private const string AseClipToWorldDirVarName = "clipToWorldDir";
		private const string AseHDClipToWorldDirFormat = "mul( UNITY_MATRIX_I_VP, float4({0},0) ).xyz";

		private const string AseClipToViewDirVarName = "clipToViewDir";
		private const string AseHDClipToViewDirFormat = " mul( UNITY_MATRIX_I_P,float4({0},0)).xyz";
		private const string AseClipToNDC = "{0}.xyz/{0}.w";

		/////////////////////////////////////////////////////
		private const string AseObjectToTangentDirVarName = "objectToTangentDir";
		private const string AseWorldToTangentDirVarName = "worldToTangentDir";
		private const string AseViewToTangentDirVarName = "viewToTangentDir";
		private const string AseClipToTangentDirVarName = "clipToTangentDir";
		private const string ASEWorldToTangentFormat = "mul( ase_worldToTangent, {0})";


		private const string AseTangentToObjectDirVarName = "tangentTobjectDir";
		private const string AseTangentToWorldDirVarName = "tangentToWorldDir";
		private const string AseTangentToViewDirVarName = "tangentToViewDir";
		private const string AseTangentToClipDirVarName = "tangentToClipDir";
		private const string ASEMulOpFormat = "mul( {0}, {1} )";



		///////////////////////////////////////////////////////////
		private const string FromStr = "From";
		private const string ToStr = "To";
		private const string SubtitleFormat = "{0} to {1}";

		private readonly string[] m_spaceOptionsFrom =
		{
			"Object",
			"World",
			"View",
			"Tangent"
		};

		private readonly string[] m_spaceOptionsTo =
		{
			"Object",
			"World",
			"View",
			"Tangent",
			"Clip"
		};


		protected override void CommonInit( int uniqueId )
		{
			base.CommonInit( uniqueId );
			AddInputPort( WirePortDataType.FLOAT3, false, Constants.EmptyPortValue );
			AddOutputVectorPorts( WirePortDataType.FLOAT3, "XYZ" );
			m_useInternalPortData = true;
			m_autoWrapProperties = true;
			m_previewShaderGUID = "74e4d859fbdb2c0468de3612145f4929";
			m_textLabelWidth = 100;
			UpdateSubtitle();
		}

		private void UpdateSubtitle()
		{
			SetAdditonalTitleText( string.Format( SubtitleFormat, m_from, m_to ) );
		}

		public override void DrawProperties()
		{
			base.DrawProperties();
			EditorGUI.BeginChangeCheck();
			m_from = (TransformSpaceFrom)EditorGUILayoutPopup( FromStr, (int)m_from, m_spaceOptionsFrom );
			m_to = (TransformSpaceTo)EditorGUILayoutPopup( ToStr, (int)m_to, m_spaceOptionsTo );
			if( m_from == TransformSpaceFrom.Tangent )
			{
				m_inverseTangentType = (InverseTangentType)EditorGUILayoutEnumPopup( InverseTBNStr, m_inverseTangentType );
			}

			m_normalize = EditorGUILayoutToggle( NormalizeOptionStr, m_normalize );
			if( EditorGUI.EndChangeCheck() )
			{
				UpdateSubtitle();
			}

			if( m_normalize )
			{
				EditorGUI.indentLevel++;
				m_safeNormalize = EditorGUILayoutToggle( SafeNormalizeOptionStr , m_safeNormalize );
				EditorGUILayout.HelpBox( Constants.SafeNormalizeInfoStr , MessageType.Info );
				EditorGUI.indentLevel--;
			}
		}

		public override void PropagateNodeData( NodeData nodeData, ref MasterNodeDataCollector dataCollector )
		{
			base.PropagateNodeData( nodeData, ref dataCollector );
			if( (int)m_from != (int)m_to && ( m_from == TransformSpaceFrom.Tangent || m_to == TransformSpaceTo.Tangent ) )
				dataCollector.DirtyNormal = true;
		}

		void CalculateTransform( TransformSpaceFrom from, TransformSpaceTo to, ref MasterNodeDataCollector dataCollector, ref string varName, ref string result )
		{
			switch( from )
			{
				case TransformSpaceFrom.Object:
				{
					switch( to )
					{
						default: case TransformSpaceTo.Object: break;
						case TransformSpaceTo.World:
						{
							if( dataCollector.IsTemplate && dataCollector.TemplateDataCollectorInstance.CurrentSRPType != TemplateSRPType.BiRP )
								result = string.Format( AseSRPObjectToWorldDirFormat, result );
							else
								result = string.Format( AseObjectToWorldDirFormat, result );
							varName = AseObjectToWorldDirVarName + OutputId;
						}
						break;
						case TransformSpaceTo.View:
						{
							if( dataCollector.IsTemplate && dataCollector.TemplateDataCollectorInstance.CurrentSRPType == TemplateSRPType.HDRP )
								result = string.Format( AseHDObjectToViewDirFormat, result );
							else
								result = string.Format( AseObjectToViewDirFormat, result );
							varName = AseObjectToViewDirVarName + OutputId;
						}
						break;
						case TransformSpaceTo.Clip:
						{
							if( dataCollector.IsTemplate && dataCollector.TemplateDataCollectorInstance.CurrentSRPType != TemplateSRPType.BiRP )
							{
								result = string.Format( AseSRPObjectToClipDirFormat, result );
							}
							else
							{
								result = string.Format( AseObjectToClipDirFormat, result );
							}
							varName = AseObjectToClipDirVarName + OutputId;
						}
						break;
					}
				}
				break;
				case TransformSpaceFrom.World:
				{
					switch( to )
					{
						case TransformSpaceTo.Object:
						{
							if( dataCollector.IsTemplate && dataCollector.TemplateDataCollectorInstance.CurrentSRPType != TemplateSRPType.BiRP )
								result = string.Format( AseSRPWorldToObjectDirFormat, result );
							else
								result = string.Format( AseWorldToObjectDirFormat, result );
							varName = AseWorldToObjectDirVarName + OutputId;
						}
						break;
						default:
						case TransformSpaceTo.World: break;
						case TransformSpaceTo.View:
						{
							result = string.Format( AseWorldToViewDirFormat, result );
							varName = AseWorldToViewDirVarName + OutputId;
						}
						break;
						case TransformSpaceTo.Clip:
						{
							if( dataCollector.IsTemplate && dataCollector.TemplateDataCollectorInstance.CurrentSRPType != TemplateSRPType.BiRP )
							{
								result = string.Format( AseSRPWorldToClipDirFormat, result );
							}
							else
							{
								result = string.Format( AseWorldToClipDirFormat, result );
							}
							varName = AseWorldToClipDirVarName + OutputId;
						}
						break;
					}
				}
				break;
				case TransformSpaceFrom.View:
				{
					switch( to )
					{
						case TransformSpaceTo.Object:
						{
							result = string.Format( AseViewToObjectDirFormat, result );
							varName = AseViewToObjectDirVarName + OutputId;
						}
						break;
						case TransformSpaceTo.World:
						{
							result = string.Format( AseViewToWorldDirFormat, result );
							varName = AseViewToWorldDirVarName + OutputId;
						}
						break;
						default: case TransformSpaceTo.View: break;
						case TransformSpaceTo.Clip:
						{
							if( dataCollector.IsTemplate && dataCollector.TemplateDataCollectorInstance.CurrentSRPType != TemplateSRPType.BiRP )
							{
								result = string.Format( AseSRPViewToClipDirFormat, result );
							}
							else
							{
								result = string.Format( AseViewToClipDirFormat, result );
							}
							varName = AseViewToClipDirVarName + OutputId;
						}
						break;
					}
				}
				break;
				//case TransformSpace.Clip:
				//{
				//	switch( to )
				//	{
				//		case TransformSpace.Object:
				//		{
				//			if( dataCollector.IsTemplate && dataCollector.TemplateDataCollectorInstance.CurrentSRPType == TemplateSRPType.HDRP )
				//			{
				//				result = string.Format( AseHDClipToObjectDirFormat, result );
				//			}
				//			else
				//			{
				//				result = string.Format( AseClipToObjectDirFormat, result );
				//			}
				//			varName = AseClipToObjectDirVarName + OutputId;
				//		}
				//		break;
				//		case TransformSpace.World:
				//		{
				//			if( dataCollector.IsTemplate && dataCollector.TemplateDataCollectorInstance.CurrentSRPType == TemplateSRPType.HDRP )
				//			{
				//				result = string.Format( AseHDClipToWorldDirFormat, result );
				//			}
				//			else
				//			{
				//				result = string.Format( AseClipToWorldDirFormat, result );
				//			}
				//			varName = AseClipToWorldDirVarName + OutputId;
				//		}
				//		break;
				//		case TransformSpace.View:
				//		{
				//			if( dataCollector.IsTemplate && dataCollector.TemplateDataCollectorInstance.CurrentSRPType == TemplateSRPType.HDRP )
				//			{
				//				result = string.Format( AseHDClipToViewDirFormat, result );
				//			}
				//			else
				//			{
				//				result = string.Format( AseClipToViewDirFormat, result );
				//			}
				//			varName = AseClipToViewDirVarName + OutputId;
				//		}
				//		break;
				//		case TransformSpace.Clip: break;
				//		default:
				//		break;
				//	}
				//}
				//break;
				default: break;
			}
		}

		public override string GenerateShaderForOutput( int outputId, ref MasterNodeDataCollector dataCollector, bool ignoreLocalvar )
		{
			if( m_outputPorts[ 0 ].IsLocalValue( dataCollector.PortCategory ) )
				return GetOutputVectorItem( 0, outputId, m_outputPorts[ 0 ].LocalValue( dataCollector.PortCategory ) );

			GeneratorUtils.RegisterUnity2019MatrixDefines( ref dataCollector );

			string result = m_inputPorts[ 0 ].GeneratePortInstructions( ref dataCollector );
			string varName = string.Empty;

			if( (int)m_from == (int)m_to )
			{
				RegisterLocalVariable( 0, result, ref dataCollector );
				return GetOutputVectorItem( 0, outputId, m_outputPorts[ 0 ].LocalValue( dataCollector.PortCategory ) );
			}

			switch( m_from )
			{
				case TransformSpaceFrom.Object:
				{
					switch( m_to )
					{
						default: case TransformSpaceTo.Object: break;
						case TransformSpaceTo.World:
						{
							CalculateTransform( m_from, m_to, ref dataCollector, ref varName, ref result );
						}
						break;
						case TransformSpaceTo.View:
						{
							CalculateTransform( m_from, m_to, ref dataCollector, ref varName, ref result );
						}
						break;
						case TransformSpaceTo.Clip:
						{
							CalculateTransform( m_from, m_to, ref dataCollector, ref varName, ref result );
						}
						break;
						case TransformSpaceTo.Tangent:
						{
							GeneratorUtils.GenerateWorldToTangentMatrix( ref dataCollector, UniqueId, CurrentPrecisionType );
							CalculateTransform( m_from, TransformSpaceTo.World, ref dataCollector, ref varName, ref result );
							result = string.Format( ASEWorldToTangentFormat, result );
							varName = AseObjectToTangentDirVarName + OutputId;
						}
						break;
					}
				}
				break;
				case TransformSpaceFrom.World:
				{
					switch( m_to )
					{
						case TransformSpaceTo.Object:
						{
							CalculateTransform( m_from, m_to, ref dataCollector, ref varName, ref result );
						}
						break;
						default:
						case TransformSpaceTo.World: break;
						case TransformSpaceTo.View:
						{
							CalculateTransform( m_from, m_to, ref dataCollector, ref varName, ref result );
						}
						break;
						case TransformSpaceTo.Clip:
						{
							CalculateTransform( m_from, m_to, ref dataCollector, ref varName, ref result );
						}
						break;
						case TransformSpaceTo.Tangent:
						{
							GeneratorUtils.GenerateWorldToTangentMatrix( ref dataCollector, UniqueId, CurrentPrecisionType );
							result = string.Format( ASEWorldToTangentFormat, result );
							varName = AseWorldToTangentDirVarName + OutputId;
						}
						break;
					}
				}
				break;
				case TransformSpaceFrom.View:
				{
					switch( m_to )
					{
						case TransformSpaceTo.Object:
						{
							CalculateTransform( m_from, m_to, ref dataCollector, ref varName, ref result );
						}
						break;
						case TransformSpaceTo.World:
						{
							CalculateTransform( m_from, m_to, ref dataCollector, ref varName, ref result );
						}
						break;
						default: case TransformSpaceTo.View: break;
						case TransformSpaceTo.Clip:
						{
							CalculateTransform( m_from, m_to, ref dataCollector, ref varName, ref result );
						}
						break;
						case TransformSpaceTo.Tangent:
						{
							GeneratorUtils.GenerateWorldToTangentMatrix( ref dataCollector, UniqueId, CurrentPrecisionType );
							CalculateTransform( m_from, TransformSpaceTo.World, ref dataCollector, ref varName, ref result );
							result = string.Format( ASEWorldToTangentFormat, result );
							varName = AseViewToTangentDirVarName + OutputId;
						}
						break;
					}
				}
				break;
				//case TransformSpace.Clip:
				//{
				//	switch( m_to )
				//	{
				//		case TransformSpace.Object:
				//		{
				//			CalculateTransform( m_from, m_to, ref dataCollector, ref varName, ref result );
				//		}
				//		break;
				//		case TransformSpace.World:
				//		{
				//			CalculateTransform( m_from, m_to, ref dataCollector, ref varName, ref result );
				//		}
				//		break;
				//		case TransformSpace.View:
				//		{
				//			CalculateTransform( m_from, m_to, ref dataCollector, ref varName, ref result );
				//		}
				//		break;
				//		case TransformSpace.Clip: break;
				//		case TransformSpace.Tangent:
				//		{
				//			GeneratorUtils.GenerateWorldToTangentMatrix( ref dataCollector, UniqueId, CurrentPrecisionType );
				//			CalculateTransform( m_from, TransformSpace.World, ref dataCollector, ref varName, ref result );
				//			result = string.Format( ASEWorldToTangentFormat, result );
				//			varName = AseClipToTangentDirVarName + OutputId;
				//		}
				//		break;
				//		default:
				//		break;
				//	}
				//}break;
				case TransformSpaceFrom.Tangent:
				{
					string matrixVal = string.Empty;
					if( m_inverseTangentType == InverseTangentType.Fast )
						matrixVal = GeneratorUtils.GenerateTangentToWorldMatrixFast( ref dataCollector, UniqueId, CurrentPrecisionType );
					else
						matrixVal = GeneratorUtils.GenerateTangentToWorldMatrixPrecise( ref dataCollector, UniqueId, CurrentPrecisionType );

					switch( m_to )
					{
						case TransformSpaceTo.Object:
						{
							result = string.Format( ASEMulOpFormat, matrixVal, result );
							CalculateTransform( TransformSpaceFrom.World, m_to, ref dataCollector, ref varName, ref result );
							varName = AseTangentToObjectDirVarName + OutputId;
						}
						break;
						case TransformSpaceTo.World:
						{
							result = string.Format( ASEMulOpFormat, matrixVal, result );
							varName = AseTangentToWorldDirVarName + OutputId;
						}
						break;
						case TransformSpaceTo.View:
						{
							result = string.Format( ASEMulOpFormat, matrixVal, result );
							CalculateTransform( TransformSpaceFrom.World, m_to, ref dataCollector, ref varName, ref result );
							varName = AseTangentToViewDirVarName + OutputId;
						}
						break;
						case TransformSpaceTo.Clip:
						{
							result = string.Format( ASEMulOpFormat, matrixVal, result );
							CalculateTransform( TransformSpaceFrom.World, m_to, ref dataCollector, ref varName, ref result );
							varName = AseTangentToClipDirVarName + OutputId;
						}
						break;
						case TransformSpaceTo.Tangent:
						default:
						break;
					}
				}
				break;
				default: break;
			}

			if( m_normalize )
			{
				result = GeneratorUtils.NormalizeValue( ref dataCollector , m_safeNormalize , m_inputPorts[ 0 ].DataType , result );
			}

			RegisterLocalVariable( 0, result, ref dataCollector, varName );
			return GetOutputVectorItem( 0, outputId, m_outputPorts[ 0 ].LocalValue( dataCollector.PortCategory ) );
		}

		public override void ReadFromString( ref string[] nodeParams )
		{
			base.ReadFromString( ref nodeParams );
			string from = GetCurrentParam( ref nodeParams );
			if( UIUtils.CurrentShaderVersion() < 17500 && from.Equals( "Clip" ) )
			{
				UIUtils.ShowMessage( UniqueId, "Clip Space no longer supported on From field over Transform Direction node" );
			}
			else
			{
				m_from = (TransformSpaceFrom)Enum.Parse( typeof( TransformSpaceFrom ), from );
			}
			m_to = (TransformSpaceTo)Enum.Parse( typeof( TransformSpaceTo ), GetCurrentParam( ref nodeParams ) );
			m_normalize = Convert.ToBoolean( GetCurrentParam( ref nodeParams ) );
			if( UIUtils.CurrentShaderVersion() > 15800 )
			{
				m_inverseTangentType = (InverseTangentType)Enum.Parse( typeof( InverseTangentType ), GetCurrentParam( ref nodeParams ) );
			}
			if( UIUtils.CurrentShaderVersion() > 18814 )
			{
				m_safeNormalize = Convert.ToBoolean( GetCurrentParam( ref nodeParams ) );
			}

			UpdateSubtitle();
		}

		public override void WriteToString( ref string nodeInfo, ref string connectionsInfo )
		{
			base.WriteToString( ref nodeInfo, ref connectionsInfo );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_from );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_to );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_normalize );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_inverseTangentType );
			IOUtils.AddFieldValueToString( ref nodeInfo , m_safeNormalize );
		}
	}
}
