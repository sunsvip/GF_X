// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using System;
using UnityEngine;
using UnityEditor;

namespace AmplifyShaderEditor
{
	[Serializable]
	[NodeAttributes( "Transform Position", "Object Transform", "Transforms a position value from one space to another" )]
	public sealed class TransformPositionNode : ParentNode
	{
		[SerializeField]
		private TransformSpaceFrom m_from = TransformSpaceFrom.Object;

		[SerializeField]
		private TransformSpaceTo m_to = TransformSpaceTo.World;

		[SerializeField]
		private bool m_perspectiveDivide = false;

		[SerializeField]
		private InverseTangentType m_inverseTangentType = InverseTangentType.Fast;

		[SerializeField]
		private bool m_absoluteWorldPos = true;

		private const string AbsoluteWorldPosStr = "Absolute";

		private string InverseTBNStr = "Inverse TBN";

		private const string AseObjectToWorldPosVarName = "objToWorld";
		private const string AseObjectToWorldPosFormat = "mul( unity_ObjectToWorld, float4( {0}, 1 ) ).xyz";
		private const string AseHDObjectToWorldPosFormat = "mul( GetObjectToWorldMatrix(), float4( {0}, 1 ) ).xyz";
		private const string ASEHDAbsoluteWordPos = "GetAbsolutePositionWS({0})";
		private const string ASEHDRelaviveCameraPos = "GetCameraRelativePositionWS({0})";
		private const string AseObjectToViewPosVarName = "objToView";
		private const string AseObjectToViewPosFormat = "mul( UNITY_MATRIX_MV, float4( {0}, 1 ) ).xyz";
		private const string AseHDObjectToViewPosFormat = "TransformWorldToView( TransformObjectToWorld({0}) )";

		private const string AseWorldToObjectPosVarName = "worldToObj";
		private const string AseWorldToObjectPosFormat = "mul( unity_WorldToObject, float4( {0}, 1 ) ).xyz";
		private const string AseSRPWorldToObjectPosFormat = "mul( GetWorldToObjectMatrix(), float4( {0}, 1 ) ).xyz";


		private const string AseWorldToViewPosVarName = "worldToView";
		private const string AseWorldToViewPosFormat = "mul( UNITY_MATRIX_V, float4( {0}, 1 ) ).xyz";

		private const string AseViewToObjectPosVarName = "viewToObj";
		private const string AseViewToObjectPosFormat = "mul( unity_WorldToObject, mul( UNITY_MATRIX_I_V , float4( {0}, 1 ) ) ).xyz";
		private const string AseHDViewToObjectPosFormat = "mul( GetWorldToObjectMatrix(), mul( UNITY_MATRIX_I_V , float4( {0}, 1 ) ) ).xyz";

		private const string AseViewToWorldPosVarName = "viewToWorld";
		private const string AseViewToWorldPosFormat = "mul( UNITY_MATRIX_I_V, float4( {0}, 1 ) ).xyz";

		///////////////////////////////////////////////////////////
		private const string AseObjectToClipPosVarName = "objectToClip";
		private const string AseObjectToClipPosFormat = "UnityObjectToClipPos({0})";
		//private const string AseSRPObjectToClipPosFormat = "TransformWorldToHClip(TransformObjectToWorld({0}))";
		private const string AseSRPObjectToClipPosFormat = "TransformObjectToHClip({0})";

		private const string AseWorldToClipPosVarName = "worldToClip";
		private const string AseWorldToClipPosFormat = "mul(UNITY_MATRIX_VP, float4({0}, 1.0))";
		private const string AseSRPWorldToClipPosFormat = "TransformWorldToHClip({0})";

		private const string AseViewToClipPosVarName = "viewToClip";
		private const string AseViewToClipPosFormat = "mul(UNITY_MATRIX_P, float4({0}, 1.0))";
		private const string AseSRPViewToClipPosFormat = "TransformWViewToHClip({0})";
		//

		private const string AseClipToObjectPosVarName = "clipToObject";
		private const string AseClipToObjectPosFormat = "mul( UNITY_MATRIX_IT_MV, mul( unity_CameraInvProjection,float4({0},1)) ).xyz";
		private const string AseHDClipToObjectPosFormat = "mul( UNITY_MATRIX_I_M, mul( UNITY_MATRIX_I_VP,float4({0},1)) ).xyz";

		private const string AseClipToWorldPosVarName = "clipToWorld";
		private const string AseClipToWorldPosFormat = "mul( UNITY_MATRIX_I_V, mul( unity_CameraInvProjection,float4({0},1)) ).xyz";
		private const string AseHDClipToWorldPosFormat = "mul( UNITY_MATRIX_I_VP, float4({0},1) ).xyz";

		private const string AseClipToViewPosVarName = "clipToView";
		private const string AseClipToViewPosFormat = " mul( unity_CameraInvProjection,float4({0},1)).xyz";
		private const string AseHDClipToViewPosFormat = " mul( UNITY_MATRIX_I_P,float4({0},1)).xyz";
		private const string AseClipToNDC = "{0}.xyz/{0}.w";
		/////////////////////////////////////////////////////
		private const string AseObjectToTangentPosVarName = "objectToTangentPos";
		private const string AseWorldToTangentPosVarName = "worldToTangentPos";
		private const string AseViewToTangentPosVarName = "viewToTangentPos";
		private const string AseClipToTangentPosVarName = "clipToTangentPos";
		private const string ASEWorldToTangentFormat = "mul( ase_worldToTangent, {0})";


		private const string AseTangentToObjectPosVarName = "tangentTobjectPos";
		private const string AseTangentToWorldPosVarName = "tangentToWorldPos";
		private const string AseTangentToViewPosVarName = "tangentToViewPos";
		private const string AseTangentToClipPosVarName = "tangentToClipPos";
		private const string ASEMulOpFormat = "mul( {0}, {1} )";


		///////////////////////////////////////////////////////////
		private const string FromStr = "From";
		private const string ToStr = "To";
		private const string PerpectiveDivideStr = "Perpective Divide";
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
			m_textLabelWidth = 120;
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
			if( EditorGUI.EndChangeCheck() )
			{
				UpdateSubtitle();
			}

			if( m_to == TransformSpaceTo.Clip )
			{
				m_perspectiveDivide = EditorGUILayoutToggle( PerpectiveDivideStr, m_perspectiveDivide );
			}

			//if( m_containerGraph.IsHDRP && ( m_from == TransformSpace.Object && m_to == TransformSpace.World ) ||
			//	( m_from == TransformSpace.World && m_to == TransformSpace.Object ) )
			//{
			//	m_absoluteWorldPos = EditorGUILayoutToggle( AbsoluteWorldPosStr, m_absoluteWorldPos );
			//}
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
						default:
						case TransformSpaceTo.Object: break;
						case TransformSpaceTo.World:
						{
							if( dataCollector.IsTemplate && dataCollector.IsSRP )
							{
								if( dataCollector.TemplateDataCollectorInstance.CurrentSRPType == TemplateSRPType.HDRP )
								{
									result = string.Format( AseHDObjectToWorldPosFormat, result );
									if( m_absoluteWorldPos )
									{
										result = string.Format( ASEHDAbsoluteWordPos, result );
									}
								}
								else if( dataCollector.TemplateDataCollectorInstance.CurrentSRPType == TemplateSRPType.URP )
								{
									result = string.Format( AseHDObjectToWorldPosFormat, result );
								}
							}
							else
								result = string.Format( AseObjectToWorldPosFormat, result );


							varName = AseObjectToWorldPosVarName + OutputId;
						}
						break;
						case TransformSpaceTo.View:
						{
							if( dataCollector.IsTemplate && dataCollector.TemplateDataCollectorInstance.CurrentSRPType == TemplateSRPType.HDRP )
								result = string.Format( AseHDObjectToViewPosFormat, result );
							else
								result = string.Format( AseObjectToViewPosFormat, result );
							varName = AseObjectToViewPosVarName + OutputId;
						}
						break;
						case TransformSpaceTo.Clip:
						{
							if( dataCollector.IsTemplate && dataCollector.TemplateDataCollectorInstance.CurrentSRPType != TemplateSRPType.BiRP )
							{
								result = string.Format( AseSRPObjectToClipPosFormat, result );
							}
							else
							{
								result = string.Format( AseObjectToClipPosFormat, result );
							}
							varName = AseObjectToClipPosVarName + OutputId;
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
							if( dataCollector.IsTemplate && dataCollector.IsSRP )
							{
								if( dataCollector.TemplateDataCollectorInstance.CurrentSRPType == TemplateSRPType.HDRP )
								{
									if( m_absoluteWorldPos )
									{
										result = string.Format( ASEHDRelaviveCameraPos, result );
									}
									result = string.Format( AseSRPWorldToObjectPosFormat, result );
								}
								else if( dataCollector.TemplateDataCollectorInstance.CurrentSRPType == TemplateSRPType.URP )
								{
									result = string.Format( AseSRPWorldToObjectPosFormat, result );
								}

							}
							else
								result = string.Format( AseWorldToObjectPosFormat, result );
							varName = AseWorldToObjectPosVarName + OutputId;
						}
						break;
						default:
						case TransformSpaceTo.World: break;
						case TransformSpaceTo.View:
						{
							result = string.Format( AseWorldToViewPosFormat, result );
							varName = AseWorldToViewPosVarName + OutputId;
						}
						break;
						case TransformSpaceTo.Clip:
						{
							if( dataCollector.IsTemplate && dataCollector.TemplateDataCollectorInstance.CurrentSRPType != TemplateSRPType.BiRP )
							{
								result = string.Format( AseSRPWorldToClipPosFormat, result );
							}
							else
							{
								result = string.Format( AseWorldToClipPosFormat, result );
							}
							varName = AseWorldToClipPosVarName + OutputId;
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
							if( dataCollector.IsTemplate && dataCollector.TemplateDataCollectorInstance.CurrentSRPType == TemplateSRPType.HDRP )
								result = string.Format( AseHDViewToObjectPosFormat, result );
							else
								result = string.Format( AseViewToObjectPosFormat, result );
							varName = AseViewToObjectPosVarName + OutputId;
						}
						break;
						case TransformSpaceTo.World:
						{
							result = string.Format( AseViewToWorldPosFormat, result ); 
							if( dataCollector.IsTemplate && 
								dataCollector.TemplateDataCollectorInstance.CurrentSRPType == TemplateSRPType.HDRP &&
								m_absoluteWorldPos )
							{
								result = string.Format( ASEHDAbsoluteWordPos , result );
							}
							varName = AseViewToWorldPosVarName + OutputId;
						}
						break;
						default:
						case TransformSpaceTo.View: break;
						case TransformSpaceTo.Clip:
						{
							if( dataCollector.IsTemplate && dataCollector.TemplateDataCollectorInstance.CurrentSRPType != TemplateSRPType.BiRP )
							{
								result = string.Format( AseSRPViewToClipPosFormat, result );
							}
							else
							{
								result = string.Format( AseViewToClipPosFormat, result );
							}
							varName = AseViewToClipPosVarName + OutputId;
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
				//				result = string.Format( AseHDClipToObjectPosFormat, result );
				//			}
				//			else
				//			{
				//				result = string.Format( AseClipToObjectPosFormat, result );
				//			}
				//			varName = AseClipToObjectPosVarName + OutputId;
				//		}
				//		break;
				//		case TransformSpace.World:
				//		{
				//			if( dataCollector.IsTemplate && dataCollector.TemplateDataCollectorInstance.CurrentSRPType == TemplateSRPType.HDRP )
				//			{
				//				result = string.Format( AseHDClipToWorldPosFormat, result );
				//			}
				//			else
				//			{
				//				result = string.Format( AseClipToWorldPosFormat, result );
				//			}
				//			varName = AseClipToWorldPosVarName + OutputId;
				//		}
				//		break;
				//		case TransformSpace.View:
				//		{
				//			if( dataCollector.IsTemplate && dataCollector.TemplateDataCollectorInstance.CurrentSRPType == TemplateSRPType.HDRP )
				//			{
				//				result = string.Format( AseHDClipToViewPosFormat, result );
				//			}
				//			else
				//			{
				//				result = string.Format( AseClipToViewPosFormat, result );
				//			}
				//			varName = AseClipToViewPosVarName + OutputId;
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
						default:
						case TransformSpaceTo.Object: break;
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
							varName = AseObjectToTangentPosVarName + OutputId;
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
							varName = AseWorldToTangentPosVarName + OutputId;
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
							CalculateTransform( m_from, m_to, ref dataCollector, ref varName, ref result ); ;
						}
						break;
						default:
						case TransformSpaceTo.View: break;
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
							varName = AseViewToTangentPosVarName + OutputId;
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
				//			varName = AseClipToTangentPosVarName + OutputId;
				//		}
				//		break;
				//		default:
				//		break;
				//	}
				//}
				//break;
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
							varName = AseTangentToObjectPosVarName + OutputId;
						}
						break;
						case TransformSpaceTo.World:
						{
							result = string.Format( ASEMulOpFormat, matrixVal, result );
							varName = AseTangentToWorldPosVarName + OutputId;
						}
						break;
						case TransformSpaceTo.View:
						{
							result = string.Format( ASEMulOpFormat, matrixVal, result );
							CalculateTransform( TransformSpaceFrom.World, m_to, ref dataCollector, ref varName, ref result );
							varName = AseTangentToViewPosVarName + OutputId;
						}
						break;
						case TransformSpaceTo.Clip:
						{
							result = string.Format( ASEMulOpFormat, matrixVal, result );
							CalculateTransform( TransformSpaceFrom.World, m_to, ref dataCollector, ref varName, ref result );
							varName = AseTangentToClipPosVarName + OutputId;
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

			if( m_to == TransformSpaceTo.Clip )
			{
				if( m_perspectiveDivide )
				{
					dataCollector.AddLocalVariable( UniqueId, CurrentPrecisionType, WirePortDataType.FLOAT4, varName, result );
					result = string.Format( AseClipToNDC, varName );
					varName += "NDC";
				}
				else
				{
					result += ".xyz";
				}
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
				UIUtils.ShowMessage( UniqueId, "Clip Space no longer supported on From field over Transform Position node" );
			}
			else
			{
				m_from = (TransformSpaceFrom)Enum.Parse( typeof( TransformSpaceFrom ), from );
			}
			m_to = (TransformSpaceTo)Enum.Parse( typeof( TransformSpaceTo ), GetCurrentParam( ref nodeParams ) );
			if( UIUtils.CurrentShaderVersion() > 15701 )
			{
				m_perspectiveDivide = Convert.ToBoolean( GetCurrentParam( ref nodeParams ) );
			}
			if( UIUtils.CurrentShaderVersion() > 15800 )
			{
				m_inverseTangentType = (InverseTangentType)Enum.Parse( typeof( InverseTangentType ), GetCurrentParam( ref nodeParams ) );
			}
			if( UIUtils.CurrentShaderVersion() > 16103 )
			{
				m_absoluteWorldPos = Convert.ToBoolean( GetCurrentParam( ref nodeParams ) );
			}
			UpdateSubtitle();
		}

		public override void WriteToString( ref string nodeInfo, ref string connectionsInfo )
		{
			base.WriteToString( ref nodeInfo, ref connectionsInfo );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_from );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_to );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_perspectiveDivide );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_inverseTangentType );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_absoluteWorldPos );
		}
	}
}
