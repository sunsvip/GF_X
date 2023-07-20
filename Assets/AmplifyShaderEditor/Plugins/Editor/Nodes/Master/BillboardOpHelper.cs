// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

// Billboard based on:
// https://gist.github.com/renaudbedard/7a90ec4a5a7359712202
using System;
using UnityEngine;
using System.Collections.Generic;

namespace AmplifyShaderEditor
{
	public enum BillboardType
	{
		Cylindrical,
		Spherical
	}

	[Serializable]
	public class BillboardOpHelper
	{
		public static readonly string BillboardTitleStr = " Billboard";
		public static readonly string BillboardTypeStr = "Type";
		public static readonly string BillboardRotIndStr = "Ignore Rotation";
		public static readonly string BillboardAffectNormalTangentStr = "Affect Normal/Tangent";

		public static readonly string[] BillboardCylindricalInstructions = { "//Calculate new billboard vertex position and normal",
																			"float3 upCamVec = float3( 0, 1, 0 )"};

		public static readonly string[] BillboardSphericalInstructions = {   "//Calculate new billboard vertex position and normal",
																			"float3 upCamVec = normalize ( UNITY_MATRIX_V._m10_m11_m12 )"};


		public static readonly string[] BillboardCommonInstructions = { "float3 forwardCamVec = -normalize ( UNITY_MATRIX_V._m20_m21_m22 )",
																		"float3 rightCamVec = normalize( UNITY_MATRIX_V._m00_m01_m02 )",
																		"float4x4 rotationCamMatrix = float4x4( rightCamVec, 0, upCamVec, 0, forwardCamVec, 0, 0, 0, 0, 1 )",
																		"{0} = normalize( mul( float4( {0} , 0 ), rotationCamMatrix )).xyz"};

		public static readonly string[] BillboardRotDependent = {   "//This unfortunately must be made to take non-uniform scaling into account",
																	"//Transform to world coords, apply rotation and transform back to local",
																	"{0} = mul( {1} , unity_ObjectToWorld ){2}",
																	"{0} = mul( {1} , rotationCamMatrix ){2}",
																	"{0} = mul( {1} , unity_WorldToObject ){2}"};


		public static readonly string[] BillboardRotIndependent = { "{0}.x *= length( unity_ObjectToWorld._m00_m10_m20 )",
																	"{0}.y *= length( unity_ObjectToWorld._m01_m11_m21 )",
																	"{0}.z *= length( unity_ObjectToWorld._m02_m12_m22 )",
																	"{0} = mul( {0}, rotationCamMatrix )",
																	"{0}.xyz += unity_ObjectToWorld._m03_m13_m23",
																	"//Need to nullify rotation inserted by generated surface shader",
																	"{0} = mul( unity_WorldToObject, {0} )"};



		public static readonly string[] BillboardHDRotDependent = {   "//This unfortunately must be made to take non-uniform scaling into account",
																	"//Transform to world coords, apply rotation and transform back to local",
																	"{0} = mul( {1} , GetObjectToWorldMatrix() ){2}",
																	"{0} = mul( {1} , rotationCamMatrix ){2}",
																	"{0} = mul( {1} , GetWorldToObjectMatrix() ){2}"};


		public static readonly string[] BillboardHDRotIndependent = { "{0}.x *= length( GetObjectToWorldMatrix()._m00_m10_m20 )",
																	"{0}.y *= length( GetObjectToWorldMatrix()._m01_m11_m21 )",
																	"{0}.z *= length( GetObjectToWorldMatrix()._m02_m12_m22 )",
																	"{0} = mul( {0}, rotationCamMatrix )",
																	//Comment this next one out in HDRP since it was moving the vertices to incorrect locations
																	// Over HDRP the correct results are achievied without having to do this operation
																	//This is because the vertex position variable is a float3 and an implicit cast is done to float4
																	//with w set to 0, this makes the multiplication below only affects rotation and not translation
																	//thus no adding the world translation is needed to counter the GetObjectToWorldMatrix() operation
																	"{0}.xyz += GetObjectToWorldMatrix()._m03_m13_m23",
																	"//Need to nullify rotation inserted by generated surface shader",
																	"{0} = mul( GetWorldToObjectMatrix(), {0} )"};


		[SerializeField]
		private bool m_isBillboard = false;

		[SerializeField]
		private BillboardType m_billboardType = BillboardType.Cylindrical;

		[SerializeField]
		private bool m_rotationIndependent = false;

		[SerializeField]
		private bool m_affectNormalTangent = true;

		public void Draw( ParentNode owner )
		{
			bool visible = owner.ContainerGraph.ParentWindow.InnerWindowVariables.ExpandedVertexOptions;
			bool enabled = m_isBillboard;
			NodeUtils.DrawPropertyGroup( owner, ref visible, ref m_isBillboard, BillboardTitleStr, () =>
			{
				m_billboardType = (BillboardType)owner.EditorGUILayoutEnumPopup( BillboardTypeStr, m_billboardType );
				m_rotationIndependent = owner.EditorGUILayoutToggle( BillboardRotIndStr, m_rotationIndependent );
				m_affectNormalTangent = owner.EditorGUILayoutToggle( BillboardAffectNormalTangentStr , m_affectNormalTangent );
			} );

			owner.ContainerGraph.ParentWindow.InnerWindowVariables.ExpandedVertexOptions = visible;
			if( m_isBillboard != enabled )
			{
				UIUtils.RequestSave();
			}
		}
		public void FillDataCollectorWithInternalData( ref MasterNodeDataCollector dataCollector )
		{
			if( m_isBillboard )
			{
				FillDataCollector( ref dataCollector, m_billboardType, m_rotationIndependent, "v.vertex", "v.normal","v.tangent", false, m_affectNormalTangent );
			}
		}

		public static void CheckVertexPosition( ref string value , ref MasterNodeDataCollector dataCollector )
		{
			if( dataCollector.IsTemplate )
			{
				WirePortDataType vertexSize = dataCollector.TemplateDataCollectorInstance.GetVertexPositionDataType();
				if( vertexSize != WirePortDataType.FLOAT4 )
				{
					// the {0}.xyz += GetObjectToWorldMatrix()._m03_m13_m23 must only be done over float4 vertices for the reason stated above for HDRP
					// on all others can be commented out
					value = "//" + value;
				}
			}
		}

		// This should be called after the Vertex Offset and Vertex Normal ports are analised
		public static void FillDataCollector( ref MasterNodeDataCollector dataCollector, BillboardType billboardType, bool rotationIndependent, string vertexPosValue, string vertexNormalValue,string vertexTangentValue, bool vertexIsFloat3, bool affectNormalTangent )
		{
			vertexTangentValue = vertexTangentValue + ".xyz";
			switch( billboardType )
			{
				case BillboardType.Cylindrical:
				{
					for( int i = 0; i < BillboardCylindricalInstructions.Length; i++ )
					{
						dataCollector.AddVertexInstruction( BillboardCylindricalInstructions[ i ] + ( dataCollector.IsTemplate ? ";" : string.Empty ), -1, true );
					}
				}
				break;

				case BillboardType.Spherical:
				{
					for( int i = 0; i < BillboardCylindricalInstructions.Length; i++ )
					{
						dataCollector.AddVertexInstruction( BillboardSphericalInstructions[ i ] + ( dataCollector.IsTemplate ? ";" : string.Empty ), -1, true );
					}
				}
				break;
			}

			for( int i = 0; i < 3; i++ )
			{
				dataCollector.AddVertexInstruction( BillboardCommonInstructions[ i ] + ( dataCollector.IsTemplate ? ";" : string.Empty ), -1, true );
			}

			if( affectNormalTangent )
			{
				string normalValue = string.Format( BillboardCommonInstructions[ 3 ] , vertexNormalValue );
				dataCollector.AddVertexInstruction( normalValue + ( dataCollector.IsTemplate ? ";" : string.Empty ) , -1 , true );

				string tangentValue = string.Format( BillboardCommonInstructions[ 3 ] , vertexTangentValue );
				dataCollector.AddVertexInstruction( tangentValue + ( dataCollector.IsTemplate ? ";" : string.Empty ) , -1 , true );
			}

			if( rotationIndependent )
			{
				

				for( int i = 0; i < BillboardRotIndependent.Length; i++ )
				{
					string value = string.Empty;
					if( dataCollector.IsTemplate && dataCollector.TemplateDataCollectorInstance.CurrentSRPType != TemplateSRPType.BiRP )
					{
						value = ( i != 5 ) ? string.Format( BillboardHDRotIndependent[ i ], vertexPosValue ) : BillboardHDRotIndependent[ i ];
						if( i == 4 )
						{
							CheckVertexPosition( ref value , ref dataCollector );
						}

					}
					else
					{
						value = ( i != 5 ) ? string.Format( BillboardRotIndependent[ i ], vertexPosValue ) : BillboardRotIndependent[ i ];
						if( i == 4 )
						{
							CheckVertexPosition( ref value , ref dataCollector );
						}
					}
					dataCollector.AddVertexInstruction( value + ( dataCollector.IsTemplate ? ";" : string.Empty ), -1, true );
				}
			}
			else
			{
				string vertexPosConverted = vertexIsFloat3 ? string.Format( "float4({0},0)", vertexPosValue ) : vertexPosValue;
				for( int i = 0; i < BillboardRotDependent.Length; i++ )
				{
					string value = string.Empty;
					if( dataCollector.IsTemplate && dataCollector.TemplateDataCollectorInstance.CurrentSRPType == TemplateSRPType.HDRP )
					{
						value = ( i > 1 ) ? string.Format( BillboardHDRotDependent[ i ], vertexPosValue, vertexPosConverted, ( vertexIsFloat3 ? ".xyz" : string.Empty ) ) : BillboardHDRotDependent[ i ];
					}
					else
					{
						value = ( i > 1 ) ? string.Format( BillboardRotDependent[ i ], vertexPosValue, vertexPosConverted, ( vertexIsFloat3 ? ".xyz" : string.Empty ) ) : BillboardRotDependent[ i ];
					}
					dataCollector.AddVertexInstruction( value + ( dataCollector.IsTemplate ? ";" : string.Empty ), -1, true );
				}
			}
		}

		public string[] GetInternalMultilineInstructions()
		{
			// This method is only used on Surface ... no HD variation is needed
			return GetMultilineInstructions( m_billboardType, m_rotationIndependent, "v.vertex", "v.normal", "v.tangent",m_affectNormalTangent );
		}

		public static string[] GetMultilineInstructions( BillboardType billboardType, bool rotationIndependent, string vertexPosValue, string vertexNormalValue, string vertexTangentValue, bool affectNormalTangent )
		{
			vertexTangentValue += ".xyz";
			// This method is only used on Surface ... no HD variation is needed
			List<string> body = new List<string>();
			switch( billboardType )
			{
				case BillboardType.Cylindrical:
				{
					for( int i = 0; i < BillboardCylindricalInstructions.Length; i++ )
					{
						body.Add( BillboardCylindricalInstructions[ i ] );
					}
				}
				break;

				case BillboardType.Spherical:
				{
					for( int i = 0; i < BillboardCylindricalInstructions.Length; i++ )
					{
						body.Add( BillboardSphericalInstructions[ i ] );
					}
				}
				break;
			}

			for( int i = 0; i < 3; i++ )
			{
				body.Add( BillboardCommonInstructions[ i ] );
			}

			if( affectNormalTangent )
			{
				string normalValue = string.Format( BillboardCommonInstructions[ 3 ] , vertexNormalValue );
				body.Add( normalValue );

				string tangentValue = string.Format( BillboardCommonInstructions[ 3 ] , vertexTangentValue );
				body.Add( tangentValue );
			}

			if( rotationIndependent )
			{
				for( int i = 0; i < BillboardRotIndependent.Length; i++ )
				{
					string value = ( i != 5 ) ? string.Format( BillboardRotIndependent[ i ], vertexPosValue ) : BillboardRotIndependent[ i ];
					body.Add( value );
				}
			}
			else
			{
				for( int i = 0; i < BillboardRotDependent.Length; i++ )
				{
					string value = ( i > 1 ) ? string.Format( BillboardRotDependent[ i ], vertexPosValue ) : BillboardRotDependent[ i ];
					body.Add( value );
				}
			}
			return body.ToArray();
		}

		public void ReadFromString( ref uint index, ref string[] nodeParams )
		{
			m_isBillboard = Convert.ToBoolean( nodeParams[ index++ ] );
			m_billboardType = (BillboardType)Enum.Parse( typeof( BillboardType ), nodeParams[ index++ ] );
			if( UIUtils.CurrentShaderVersion() > 11007 )
			{
				m_rotationIndependent = Convert.ToBoolean( nodeParams[ index++ ] );
			}

			if( UIUtils.CurrentShaderVersion() > 18918 )
			{
				m_affectNormalTangent = Convert.ToBoolean( nodeParams[ index++ ] );
			}
		}

		public void WriteToString( ref string nodeInfo )
		{
			IOUtils.AddFieldValueToString( ref nodeInfo, m_isBillboard );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_billboardType );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_rotationIndependent );
			IOUtils.AddFieldValueToString( ref nodeInfo , m_affectNormalTangent );
		}

		public bool IsBillboard { get { return m_isBillboard; } }
	}
}
