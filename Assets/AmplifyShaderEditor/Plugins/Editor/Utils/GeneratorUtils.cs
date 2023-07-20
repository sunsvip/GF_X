// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

namespace AmplifyShaderEditor
{
	public static class GeneratorUtils
	{
		public const string VertexBlendWeightsStr = "ase_blendWeights";
		public const string VertexBlendIndicesStr = "ase_blendIndices";
		public const string ObjectScaleStr = "ase_objectScale";
		public const string ParentObjectScaleStr = "ase_parentObjectScale";
		public const string ScreenDepthStr = "ase_screenDepth";
		public const string ViewPositionStr = "ase_viewPos";
		public const string WorldViewDirectionStr = "ase_worldViewDir";
		public const string TangentViewDirectionStr = "ase_tanViewDir";
		public const string NormalizedViewDirStr = "ase_normViewDir";
		public const string ClipPositionStr = "ase_clipPos";
		public const string VertexPosition3Str = "ase_vertex3Pos";
		public const string VertexPosition4Str = "ase_vertex4Pos";
		public const string VertexNormalStr = "ase_vertexNormal";
		public const string VertexTangentStr = "ase_vertexTangent";
		public const string VertexTangentSignStr = "ase_vertexTangentSign";
		public const string VertexBitangentStr = "ase_vertexBitangent";
		public const string ScreenPositionStr = "ase_screenPos";
		public const string NormalizedScreenPosFormat = "{0} / {0}.w";
		public const string ScreenPositionNormalizedStr = "ase_screenPosNorm";
		public const string GrabScreenPositionStr = "ase_grabScreenPos";
		public const string GrabScreenPositionNormalizedStr = "ase_grabScreenPosNorm";
		public const string WorldPositionStr = "ase_worldPos";
		public const string RelativeWorldPositionStr = "ase_relWorldPos";
		public const string VFaceStr = "ase_vface";
		public const string ShadowCoordsStr = "ase_shadowCoords";
		public const string WorldLightDirStr = "ase_worldlightDir";
		public const string ObjectLightDirStr = "ase_objectlightDir";
		public const string WorldNormalStr = "ase_worldNormal";
		public const string NormalizedWorldNormalStr = "ase_normWorldNormal";
		public const string WorldReflectionStr = "ase_worldReflection";
		public const string WorldTangentStr = "ase_worldTangent";
		public const string WorldBitangentStr = "ase_worldBitangent";
		public const string WorldToTangentStr = "ase_worldToTangent";
		public const string ObjectToTangentStr = "ase_objectToTangent";
		public const string TangentToWorldPreciseStr = "ase_tangentToWorldPrecise";
		public const string TangentToWorldFastStr = "ase_tangentToWorldFast";
		public const string TangentToObjectStr = "ase_tangentToObject";
		public const string TangentToObjectFastStr = "ase_tangentToObjectFast";
		private const string Float3Format = "float3 {0} = {1};";
		private const string Float4Format = "float4 {0} = {1};";
		private const string GrabFunctionHeader = "inline float4 ASE_ComputeGrabScreenPos( float4 pos )";
		private const string GrabFunctionCall = "ASE_ComputeGrabScreenPos( {0} )";
		private const string Identity4x4 = "ase_identity4x4";
		private const string FaceVertex = "ase_faceVertex";

		private static readonly string[] GrabFunctionBody = {
			"#if UNITY_UV_STARTS_AT_TOP",
			"float scale = -1.0;",
			"#else",
			"float scale = 1.0;",
			"#endif",
			"float4 o = pos;",
			"o.y = pos.w * 0.5f;",
			"o.y = ( pos.y - o.y ) * _ProjectionParams.x * scale + o.y;",
			"return o;"
		};

		// MATRIX IDENTITY
		static public string GenerateIdentity4x4( ref MasterNodeDataCollector dataCollector , int uniqueId )
		{
			dataCollector.AddLocalVariable( uniqueId , "float4x4 ase_identity4x4 = float4x4(1,0,0,0, 0,1,0,0, 0,0,1,0, 0,0,0,1);" );
			return Identity4x4;
		}


		// OBJECT SCALE
		static public string GenerateObjectScale( ref MasterNodeDataCollector dataCollector , int uniqueId )
		{
			if( dataCollector.IsTemplate )
				return dataCollector.TemplateDataCollectorInstance.GenerateObjectScale( ref dataCollector , uniqueId );

			//string value= "1/float3( length( unity_WorldToObject[ 0 ].xyz ), length( unity_WorldToObject[ 1 ].xyz ), length( unity_WorldToObject[ 2 ].xyz ) );";
			string value = "float3( length( unity_ObjectToWorld[ 0 ].xyz ), length( unity_ObjectToWorld[ 1 ].xyz ), length( unity_ObjectToWorld[ 2 ].xyz ) )";
			dataCollector.AddLocalVariable( uniqueId , PrecisionType.Float , WirePortDataType.FLOAT3 , ObjectScaleStr , value );
			return ObjectScaleStr;
		}

		static public string GenerateRotationIndependentObjectScale( ref MasterNodeDataCollector dataCollector , int uniqueId )
		{
			if( dataCollector.IsTemplate )
				return dataCollector.TemplateDataCollectorInstance.GenerateRotationIndependentObjectScale( ref dataCollector , uniqueId );

			string value = "(1.0/float3( length( unity_WorldToObject[ 0 ].xyz ), length( unity_WorldToObject[ 1 ].xyz ), length( unity_WorldToObject[ 2 ].xyz ) ))";
			dataCollector.AddLocalVariable( uniqueId , PrecisionType.Float , WirePortDataType.FLOAT3 , ParentObjectScaleStr , value );
			return ParentObjectScaleStr;
		}

		// WORLD POSITION
		static public string GenerateWorldPosition( ref MasterNodeDataCollector dataCollector , int uniqueId )
		{
			PrecisionType precision = PrecisionType.Float;
			if( dataCollector.IsTemplate )
				return dataCollector.TemplateDataCollectorInstance.GetWorldPos();

			dataCollector.AddToInput( -1 , SurfaceInputs.WORLD_POS , precision );

			string result = Constants.InputVarStr + ".worldPos";

			if( dataCollector.PortCategory == MasterNodePortCategory.Vertex || dataCollector.PortCategory == MasterNodePortCategory.Tessellation )
				result = "mul( unity_ObjectToWorld, " + Constants.VertexShaderInputStr + ".vertex )";

			//dataCollector.AddToLocalVariables( dataCollector.PortCategory, uniqueId, string.Format( Float3Format, WorldPositionStr, result ) );
			dataCollector.AddLocalVariable( uniqueId , precision , WirePortDataType.FLOAT3 , WorldPositionStr , result );

			return WorldPositionStr;
		}

		// WORLD REFLECTION
		static public string GenerateWorldReflection( ref MasterNodeDataCollector dataCollector , int uniqueId , bool normalize = false )
		{
			if( dataCollector.IsTemplate )
				return dataCollector.TemplateDataCollectorInstance.GetWorldReflection( UIUtils.CurrentWindow.CurrentGraph.CurrentPrecision , true , MasterNodePortCategory.Fragment , normalize );

			string precisionType = UIUtils.PrecisionWirePortToCgType( UIUtils.CurrentWindow.CurrentGraph.CurrentPrecision , WirePortDataType.FLOAT3 );
			string result = string.Empty;
			if( !dataCollector.DirtyNormal )
				result = Constants.InputVarStr + ".worldRefl";
			else
				result = "WorldReflectionVector( " + Constants.InputVarStr + ", " + precisionType + "( 0, 0, 1 ) )";

			if( dataCollector.PortCategory == MasterNodePortCategory.Vertex || dataCollector.PortCategory == MasterNodePortCategory.Tessellation )
				result = "UnityObjectToWorldNormal( " + Constants.VertexShaderInputStr + ".normal )";
			if( normalize )
			{
				result = string.Format( "normalize( {0} )" , result );
			}

			dataCollector.AddToLocalVariables( dataCollector.PortCategory , uniqueId , string.Concat( precisionType , " " , WorldReflectionStr , " = " , result , ";" ) );
			return WorldReflectionStr;
		}

		// WORLD NORMAL
		static public string GenerateWorldNormal( ref MasterNodeDataCollector dataCollector , int uniqueId , PrecisionType precisionType , string normal , string outputId )
		{
			if( dataCollector.IsTemplate )
				return dataCollector.TemplateDataCollectorInstance.GetWorldNormal( uniqueId , precisionType , normal , outputId );

			string tanToWorld = GenerateTangentToWorldMatrixFast( ref dataCollector , uniqueId , precisionType );
			return string.Format( "mul({0},{1})" , tanToWorld , normal );

		}
		static public string GenerateWorldNormal( ref MasterNodeDataCollector dataCollector , int uniqueId , bool normalize = false )
		{
			PrecisionType precision = UIUtils.CurrentWindow.CurrentGraph.CurrentPrecision;

			if( dataCollector.IsTemplate )
				return dataCollector.TemplateDataCollectorInstance.GetWorldNormal( precision , true , MasterNodePortCategory.Fragment , normalize );

			string precisionType = UIUtils.PrecisionWirePortToCgType( precision , WirePortDataType.FLOAT3 );
			string result = string.Empty;
			if( !dataCollector.DirtyNormal )
				result = Constants.InputVarStr + ".worldNormal";
			else
				result = "WorldNormalVector( " + Constants.InputVarStr + ", " + precisionType + "( 0, 0, 1 ) )";

			if( dataCollector.PortCategory == MasterNodePortCategory.Vertex || dataCollector.PortCategory == MasterNodePortCategory.Tessellation )
				result = "UnityObjectToWorldNormal( " + Constants.VertexShaderInputStr + ".normal )";

			dataCollector.AddLocalVariable( uniqueId , precision , WirePortDataType.FLOAT3 , WorldNormalStr , result );
			if( normalize )
			{
				dataCollector.AddLocalVariable( uniqueId , precision , WirePortDataType.FLOAT3 , NormalizedWorldNormalStr , "normalize( " + WorldNormalStr + " )" );
				return NormalizedWorldNormalStr;
			}
			return WorldNormalStr;
		}

		// WORLD TANGENT
		static public string GenerateWorldTangent( ref MasterNodeDataCollector dataCollector , int uniqueId )
		{
			if( dataCollector.IsTemplate )
				return dataCollector.TemplateDataCollectorInstance.GetWorldTangent( UIUtils.CurrentWindow.CurrentGraph.CurrentPrecision );

			string precisionType = UIUtils.PrecisionWirePortToCgType( UIUtils.CurrentWindow.CurrentGraph.CurrentPrecision , WirePortDataType.FLOAT3 );
			string result = "WorldNormalVector( " + Constants.InputVarStr + ", " + precisionType + "( 1, 0, 0 ) )";

			if( dataCollector.PortCategory == MasterNodePortCategory.Vertex || dataCollector.PortCategory == MasterNodePortCategory.Tessellation )
				result = "UnityObjectToWorldDir( " + Constants.VertexShaderInputStr + ".tangent.xyz )";
			dataCollector.AddLocalVariable( uniqueId , UIUtils.CurrentWindow.CurrentGraph.CurrentPrecision , WirePortDataType.FLOAT3 , WorldTangentStr , result );
			//dataCollector.AddToLocalVariables( dataCollector.PortCategory, uniqueId, string.Concat( precisionType, " ", WorldTangentStr, " = ", result, ";" ) );
			return WorldTangentStr;
		}

		// WORLD BITANGENT
		static public string GenerateWorldBitangent( ref MasterNodeDataCollector dataCollector , int uniqueId )
		{
			if( dataCollector.IsTemplate )
				return dataCollector.TemplateDataCollectorInstance.GetWorldBinormal( UIUtils.CurrentWindow.CurrentGraph.CurrentPrecision );

			string precisionType = UIUtils.PrecisionWirePortToCgType( UIUtils.CurrentWindow.CurrentGraph.CurrentPrecision , WirePortDataType.FLOAT3 );
			string result = "WorldNormalVector( " + Constants.InputVarStr + ", " + precisionType + "( 0, 1, 0 ) )";

			if( dataCollector.PortCategory == MasterNodePortCategory.Vertex || dataCollector.PortCategory == MasterNodePortCategory.Tessellation )
			{
				string worldNormal = GenerateWorldNormal( ref dataCollector , uniqueId );
				string worldTangent = GenerateWorldTangent( ref dataCollector , uniqueId );
				dataCollector.AddToVertexLocalVariables( uniqueId , string.Format( "half tangentSign = {0}.tangent.w * ( unity_WorldTransformParams.w >= 0.0 ? 1.0 : -1.0 );", Constants.VertexShaderInputStr ) );
				result = "cross( " + worldNormal + ", " + worldTangent + " ) * tangentSign";
			}

			dataCollector.AddToLocalVariables( dataCollector.PortCategory , uniqueId , string.Concat( precisionType , " " , WorldBitangentStr , " = " , result , ";" ) );
			return WorldBitangentStr;
		}

		// OBJECT TO TANGENT MATRIX
		static public string GenerateObjectToTangentMatrix( ref MasterNodeDataCollector dataCollector , int uniqueId , PrecisionType precision )
		{
			string normal = GenerateVertexNormal( ref dataCollector , uniqueId , precision );
			string tangent = GenerateVertexTangent( ref dataCollector , uniqueId , precision , WirePortDataType.FLOAT3 );
			string bitangen = GenerateVertexBitangent( ref dataCollector , uniqueId , precision );
			dataCollector.AddLocalVariable( uniqueId , precision , WirePortDataType.FLOAT3x3 , ObjectToTangentStr , "float3x3( " + tangent + ", " + bitangen + ", " + normal + " )" );
			return ObjectToTangentStr;
		}

		// TANGENT TO OBJECT
		//static public string GenerateTangentToObjectMatrixFast( ref MasterNodeDataCollector dataCollector, int uniqueId, PrecisionType precision )
		//{
		//	string normal = GenerateVertexNormal( ref dataCollector, uniqueId, precision );
		//	string tangent = GenerateVertexTangent( ref dataCollector, uniqueId, precision );
		//	string bitangent = GenerateVertexBitangent( ref dataCollector, uniqueId, precision );

		//	string result = string.Format( "float3x3({0}.x,{1}.x,{2}.x,{0}.y,{1}.y,{2}.y,{0}.z,{1}.z,{2}.z)",tangent,bitangent,normal );
		//	dataCollector.AddLocalVariable( uniqueId, precision, WirePortDataType.FLOAT3x3, TangentToObjectFastStr, result );
		//	return TangentToObjectFastStr;
		//}

		//static public string GenerateTangentToObjectMatrixPrecise( ref MasterNodeDataCollector dataCollector, int uniqueId, PrecisionType precision )
		//{
		//	string objectToTangent = GenerateObjectToTangentMatrix( ref dataCollector, uniqueId, precision );
		//	Add3x3InverseFunction( ref dataCollector, UIUtils.PrecisionWirePortToCgType( precision, WirePortDataType.FLOAT ) );
		//	dataCollector.AddLocalVariable( uniqueId, precision, WirePortDataType.FLOAT3x3, TangentToObjectStr, string.Format( Inverse3x3Header, objectToTangent ) );
		//	return TangentToObjectStr;
		//}

		// WORLD TO TANGENT MATRIX
		static public string GenerateWorldToTangentMatrix( ref MasterNodeDataCollector dataCollector , int uniqueId , PrecisionType precision )
		{
			if( dataCollector.IsTemplate )
				return dataCollector.TemplateDataCollectorInstance.GetWorldToTangentMatrix( precision );

			if( dataCollector.IsFragmentCategory )
			{
				dataCollector.ForceNormal = true;

				dataCollector.AddToInput( -1 , SurfaceInputs.WORLD_NORMAL , precision );
				dataCollector.AddToInput( -1 , SurfaceInputs.INTERNALDATA , addSemiColon: false );
			}

			string worldNormal = GenerateWorldNormal( ref dataCollector , uniqueId );
			string worldTangent = GenerateWorldTangent( ref dataCollector , uniqueId );
			string worldBitangent = GenerateWorldBitangent( ref dataCollector , uniqueId );

			dataCollector.AddLocalVariable( uniqueId , precision , WirePortDataType.FLOAT3x3 , WorldToTangentStr , "float3x3( " + worldTangent + ", " + worldBitangent + ", " + worldNormal + " )" );
			return WorldToTangentStr;
		}

		// TANGENT TO WORLD
		static public string GenerateTangentToWorldMatrixFast( ref MasterNodeDataCollector dataCollector , int uniqueId , PrecisionType precision )
		{
			if( dataCollector.IsTemplate )
				return dataCollector.TemplateDataCollectorInstance.GetTangentToWorldMatrixFast( precision );

			if( dataCollector.IsFragmentCategory )
			{
				dataCollector.ForceNormal = true;

				dataCollector.AddToInput( -1 , SurfaceInputs.WORLD_NORMAL , precision );
				dataCollector.AddToInput( -1 , SurfaceInputs.INTERNALDATA , addSemiColon: false );
			}

			string worldNormal = GenerateWorldNormal( ref dataCollector , uniqueId );
			string worldTangent = GenerateWorldTangent( ref dataCollector , uniqueId );
			string worldBitangent = GenerateWorldBitangent( ref dataCollector , uniqueId );

			string result = string.Format( "float3x3({0}.x,{1}.x,{2}.x,{0}.y,{1}.y,{2}.y,{0}.z,{1}.z,{2}.z)" , worldTangent , worldBitangent , worldNormal );
			dataCollector.AddLocalVariable( uniqueId , precision , WirePortDataType.FLOAT3x3 , TangentToWorldFastStr , result );
			return TangentToWorldFastStr;
		}

		static public string GenerateTangentToWorldMatrixPrecise( ref MasterNodeDataCollector dataCollector , int uniqueId , PrecisionType precision )
		{
			if( dataCollector.IsTemplate )
				return dataCollector.TemplateDataCollectorInstance.GetTangentToWorldMatrixPrecise( precision );

			if( dataCollector.IsFragmentCategory )
			{
				dataCollector.ForceNormal = true;

				dataCollector.AddToInput( -1 , SurfaceInputs.WORLD_NORMAL , precision );
				dataCollector.AddToInput( -1 , SurfaceInputs.INTERNALDATA , addSemiColon: false );
			}

			string worldToTangent = GenerateWorldToTangentMatrix( ref dataCollector , uniqueId , precision );
			Add3x3InverseFunction( ref dataCollector , UIUtils.PrecisionWirePortToCgType( precision , WirePortDataType.FLOAT ) );
			dataCollector.AddLocalVariable( uniqueId , precision , WirePortDataType.FLOAT3x3 , TangentToWorldPreciseStr , string.Format( Inverse3x3Header , worldToTangent ) );
			return TangentToWorldPreciseStr;
		}

		// SAMPLER STATES
		static public string GenerateSamplerState( ref MasterNodeDataCollector dataCollector , int uniqueId , string propertyName , VariableMode varMode , bool returnPropertyName = false )
		{

			string sampler = string.Format( Constants.SamplerFormat , propertyName );
			string samplerDecl = string.Empty;

			ParentGraph outsideGraph = UIUtils.CurrentWindow.OutsideGraph;
			if( outsideGraph.IsSRP )
				//if( dataCollector.IsSRP )
				samplerDecl = string.Format( Constants.SamplerDeclSRPFormat , sampler ) + ";";
			else
				samplerDecl = string.Format( Constants.SamplerDeclFormat , sampler ) + ";";
			if( varMode == VariableMode.Create )
				dataCollector.AddToUniforms( uniqueId , samplerDecl );

			if( returnPropertyName )
				return propertyName;
			else
				return sampler;
		}

		public static string GetPropertyFromSamplerState( string sampler )
		{
			if( sampler.StartsWith( "sampler" ) )
				return sampler.Remove( 0 , 7 );
			else
				return sampler;
		}

		public static string GetSamplerDeclaraction( string texture , WirePortDataType type , string termination = "" )
		{
			return GetSamplerDeclaraction( texture , Constants.WireToTexture[ type ] , termination );
		}

		public static string GetSamplerDeclaraction( string sampler , TextureType type , string termination = "" )
		{
			ParentGraph outsideGraph = UIUtils.CurrentWindow.OutsideGraph;
			if( outsideGraph.SamplingMacros || type == TextureType.Texture2DArray )
			{
				if( outsideGraph.IsSRP )
					return string.Format( Constants.SamplerDeclSRPFormat , sampler ) + termination;
				else
					return string.Format( Constants.SamplerDeclFormat , sampler ) + termination;
			}

			// we don't use sampler states when macros are not available
			return string.Empty;
		}

		// PROPERTY MACRO
		public static string GetPropertyDeclaraction( string texture , WirePortDataType type , string termination = "" )
		{
			return GetPropertyDeclaraction( texture , Constants.WireToTexture[ type ] , termination );
		}

		public static string GetPropertyDeclaraction( string texture , TextureType type , string termination = "" )
		{
			if( type == TextureType.Texture1D )
				return "sampler1D " + texture + termination;

			ParentGraph outsideGraph = UIUtils.CurrentWindow.OutsideGraph;
			if( outsideGraph.SamplingMacros || type == TextureType.Texture2DArray )
			{
				if( outsideGraph.IsSRP )
					return string.Format( Constants.TexDeclarationNoSamplerSRPMacros[ type ] , texture ) + termination;
				else
					return string.Format( Constants.TexDeclarationNoSamplerStandardMacros[ type ] , texture ) + termination;
			}

			return UIUtils.TextureTypeToCgType( type ) + " " + texture + termination;
		}

		// SAMPLING CALL
		public static string GenerateSamplingCall( ref MasterNodeDataCollector dataCollector , WirePortDataType type , string property , string samplerState , string uv , MipType mip = MipType.Auto , params string[] mipData )
		{
			ParentGraph ousideGraph = UIUtils.CurrentWindow.OutsideGraph;
			string result = string.Empty;
			string mipSuffix = string.Empty;

			//samplerState = GetPropertyFromSamplerState( samplerState );
			TextureType textureType = Constants.WireToTexture[ type ];

			bool usingMacro = false;
			if( ousideGraph.SamplingMacros || textureType == TextureType.Texture2DArray )
				usingMacro = true;

			switch( mip )
			{
				default:
				case MipType.Auto:
				break;
				case MipType.MipLevel:
				mipSuffix = usingMacro ? "_LOD" : "lod";
				break;
				case MipType.MipBias:
				mipSuffix = usingMacro ? "_BIAS" : "bias";
				break;
				case MipType.Derivative:
				mipSuffix = usingMacro ? "_GRAD" : "grad";
				break;
			}

			string mipParams = string.Empty;
			if( mip != MipType.Auto )
			{
				for( int i = 0 ; i < mipData.Length ; i++ )
				{
					mipParams += ", " + mipData[ i ];
				}
			}

			if( usingMacro )
			{
				if( ousideGraph.IsSRP )
				{
					if( textureType == TextureType.Texture3D && ( mip == MipType.MipBias || mip == MipType.Derivative ) )
						AddCustom3DSRPMacros( ref dataCollector );
					// srp macro
					result = string.Format( Constants.TexSampleSRPMacros[ textureType ] , mipSuffix , property , samplerState , uv + mipParams );
				}
				else
				{
					AddCustomStandardSamplingMacros( ref dataCollector , type , mip );
					result = string.Format( Constants.TexSampleSamplerStandardMacros[ textureType ] , mipSuffix , property , samplerState , uv + mipParams );

				}
			}
			else
			{
				//no macro : builtin and srp
				string uvs = uv + mipParams;
				string emptyParam = ", 0";
				if( textureType == TextureType.Texture3D || textureType == TextureType.Cube )
					emptyParam = string.Empty;

				if( ( mip == MipType.MipBias || mip == MipType.MipLevel ) )
					uvs = "float4(" + uv + emptyParam + mipParams + ")";

				result = string.Format( Constants.TexSampleStandard[ textureType ] , mipSuffix , property , uvs );
			}
			return result;
		}

		public static string GenerateScaleOffsettedUV( TextureType texType , string uvName , string propertyName, bool addST )
		{
			if( addST )
				propertyName += "_ST";

			switch( texType )
			{
				case TextureType.Texture1D: return uvName + " * " + propertyName + ".x + " + propertyName + ".z";
				case TextureType.Texture2D: return uvName + " * " + propertyName + ".xy + " + propertyName + ".zw";
				case TextureType.Texture3D:
				case TextureType.Cube: return uvName + " * " + propertyName + ".xy + " + propertyName + ".zw";
				default:
				case TextureType.Texture2DArray:
				case TextureType.ProceduralTexture: return uvName;
			}
		}
		// AUTOMATIC UVS - SURFACE ONLY
		static public string GenerateAutoUVs( ref MasterNodeDataCollector dataCollector , int uniqueId , int index , string propertyName = null , WirePortDataType size = WirePortDataType.FLOAT2 , string scale = null , string offset = null , string outputId = null )
		{
			string result = string.Empty;
			string varName = string.Empty;
			string indexStr = index > 0 ? ( index + 1 ).ToString() : "";
			string sizeDif = string.Empty;
			WirePortDataType maxSize = dataCollector.GetMaxTextureChannelSize( index );

			//if( maxSize == WirePortDataType.FLOAT3 )
			//	sizeDif = "3";
			//else if( maxSize == WirePortDataType.FLOAT4 )
			//	sizeDif = "4";

			if( !dataCollector.IsTemplate && index > 3 )
			{
				string texCoordNameIn = TemplateHelperFunctions.BaseInterpolatorName + index;
				string texCoordNameOut = TemplateHelperFunctions.BaseInterpolatorName + ( index + 1 ).ToString();
				if( dataCollector.IsFragmentCategory )
				{
					GenerateValueInVertex( ref dataCollector , uniqueId , maxSize , PrecisionType.Float , Constants.VertexShaderInputStr + "." + texCoordNameIn , texCoordNameOut , true );
					result = Constants.InputVarStr + "." + texCoordNameOut;
				}
				else
				{
					result = Constants.VertexShaderInputStr + "." + texCoordNameIn;
				}

				if( !string.IsNullOrEmpty( propertyName ) )
				{

					varName = "uv" + indexStr + ( maxSize != WirePortDataType.FLOAT2 ? "s" + sizeDif : "" ) + propertyName;
					dataCollector.AddToUniforms( uniqueId , "uniform float4 " + propertyName + "_ST;" );
					if( maxSize > WirePortDataType.FLOAT2 )
					{
						dataCollector.UsingHigherSizeTexcoords = true;
						dataCollector.AddToLocalVariables( dataCollector.PortCategory , uniqueId , PrecisionType.Float , maxSize , varName , result );
						dataCollector.AddToLocalVariables( dataCollector.PortCategory , uniqueId , varName + ".xy = " + result + ".xy * " + propertyName + "_ST.xy + " + propertyName + "_ST.zw;" );
					}
					else
					{
						dataCollector.AddToLocalVariables( dataCollector.PortCategory , uniqueId , PrecisionType.Float , maxSize , varName , result + " * " + propertyName + "_ST.xy + " + propertyName + "_ST.zw" );
					}

					result = varName;
				}

				switch( maxSize )
				{
					default:
					case WirePortDataType.FLOAT2:
					{
						result += ".xy";
					}
					break;
					case WirePortDataType.FLOAT3:
					{
						result += ".xyz";
					}
					break;
					case WirePortDataType.FLOAT4: break;
				}

				if( size < maxSize )
				{
					switch( size )
					{
						case WirePortDataType.FLOAT2: result += ".xy"; break;
						case WirePortDataType.FLOAT3: result += ".xyz"; break;
					}
				}

				return result;
			}

			if( dataCollector.PortCategory == MasterNodePortCategory.Fragment || dataCollector.PortCategory == MasterNodePortCategory.Debug )
			{
				string dummyPropUV = "_tex" + sizeDif + "coord" + indexStr;
				string dummyUV = "uv" + indexStr + dummyPropUV;

				dataCollector.AddToProperties( uniqueId , "[HideInInspector] " + dummyPropUV + "( \"\", 2D ) = \"white\" {}" , 100 );
				dataCollector.AddToInput( uniqueId , dummyUV , maxSize );

				result = Constants.InputVarStr + "." + dummyUV;
			}
			else
			{
				result = Constants.VertexShaderInputStr + ".texcoord";
				if( index > 0 )
				{
					result += index.ToString();
				}

				switch( maxSize )
				{
					default:
					case WirePortDataType.FLOAT2:
					{
						result += ".xy";
					}
					break;
					case WirePortDataType.FLOAT3:
					{
						result += ".xyz";
					}
					break;
					case WirePortDataType.FLOAT4: break;
				}
			}

			varName = "uv" + indexStr + ( maxSize != WirePortDataType.FLOAT2 ? "s" + sizeDif : "" ) + "_TexCoord" + outputId;

			if( !string.IsNullOrEmpty( propertyName ) )
			{
				string finalVarName = "uv" + indexStr + ( maxSize != WirePortDataType.FLOAT2 ? "s" + sizeDif : "" ) + propertyName;

				dataCollector.AddToUniforms( uniqueId , "uniform float4 " + propertyName + "_ST;" );
				if( maxSize > WirePortDataType.FLOAT2 )
				{
					dataCollector.UsingHigherSizeTexcoords = true;
					dataCollector.AddToLocalVariables( dataCollector.PortCategory , uniqueId , PrecisionType.Float , maxSize , finalVarName , result );
					dataCollector.AddToLocalVariables( dataCollector.PortCategory , uniqueId , finalVarName + ".xy = " + result + ".xy * " + propertyName + "_ST.xy + " + propertyName + "_ST.zw;" );
				}
				else
				{
					dataCollector.AddToLocalVariables( dataCollector.PortCategory , uniqueId , PrecisionType.Float , size , finalVarName , result + " * " + propertyName + "_ST.xy + " + propertyName + "_ST.zw" );
				}

				result = finalVarName;
			}
			else if( !string.IsNullOrEmpty( scale ) || !string.IsNullOrEmpty( offset ) )
			{
				if( maxSize > WirePortDataType.FLOAT2 )
				{
					dataCollector.UsingHigherSizeTexcoords = true;
					dataCollector.AddToLocalVariables( dataCollector.PortCategory , uniqueId , PrecisionType.Float , size , varName , result );
					dataCollector.AddToLocalVariables( dataCollector.PortCategory , uniqueId , varName + ".xy = " + result + ".xy" + ( string.IsNullOrEmpty( scale ) ? "" : " * " + scale ) + ( string.IsNullOrEmpty( offset ) ? "" : " + " + offset ) + ";" );
				}
				else
				{
					dataCollector.AddToLocalVariables( dataCollector.PortCategory , uniqueId , PrecisionType.Float , size , varName , result + ( string.IsNullOrEmpty( scale ) ? "" : " * " + scale ) + ( string.IsNullOrEmpty( offset ) ? "" : " + " + offset ) );
				}

				result = varName;
			}
			else if( dataCollector.PortCategory == MasterNodePortCategory.Fragment )
			{
				if( maxSize > WirePortDataType.FLOAT2 )
					dataCollector.UsingHigherSizeTexcoords = true;
			}
			if( size < maxSize )
			{
				switch( size )
				{
					case WirePortDataType.FLOAT2:result += ".xy";break;
					case WirePortDataType.FLOAT3:result += ".xyz"; break;
				}
			}
			return result;
		}

		// SCREEN POSITION NORMALIZED
		static public string GenerateScreenPositionNormalizedForValue( string customVertexPos , string outputId , ref MasterNodeDataCollector dataCollector , int uniqueId , PrecisionType precision , bool addInput = true )
		{
			string stringPosVar = GenerateScreenPositionForValue( customVertexPos , outputId , ref dataCollector , uniqueId , precision , addInput );
			string varName = ScreenPositionNormalizedStr + uniqueId;

			// TODO: check later if precision can be half
			dataCollector.AddLocalVariable( uniqueId , precision , WirePortDataType.FLOAT4 , varName , string.Format( NormalizedScreenPosFormat , stringPosVar ) );
			dataCollector.AddLocalVariable( uniqueId , varName + ".z = ( UNITY_NEAR_CLIP_VALUE >= 0 ) ? " + varName + ".z : " + varName + ".z * 0.5 + 0.5;" );

			return varName;
		}

		static public string GenerateScreenPositionNormalized( ref MasterNodeDataCollector dataCollector , int uniqueId , PrecisionType precision , bool addInput = true , string customScreenPos = null )
		{
			string stringPosVar = string.IsNullOrEmpty( customScreenPos ) ? GenerateScreenPosition( ref dataCollector , uniqueId , precision , addInput ) : customScreenPos;

			// TODO: check later if precision can be half
			dataCollector.AddLocalVariable( uniqueId , precision , WirePortDataType.FLOAT4 , ScreenPositionNormalizedStr , string.Format( NormalizedScreenPosFormat , stringPosVar ) );
			dataCollector.AddLocalVariable( uniqueId , ScreenPositionNormalizedStr + ".z = ( UNITY_NEAR_CLIP_VALUE >= 0 ) ? " + ScreenPositionNormalizedStr + ".z : " + ScreenPositionNormalizedStr + ".z * 0.5 + 0.5;" );

			return ScreenPositionNormalizedStr;
		}

		// SCREEN POSITION
		static public string GenerateScreenPositionForValue( string customVertexPosition , string outputId , ref MasterNodeDataCollector dataCollector , int uniqueId , PrecisionType precision , bool addInput = true )
		{
			// overriding precision
			precision = PrecisionType.Float;

			if( dataCollector.IsTemplate )
				return dataCollector.TemplateDataCollectorInstance.GetScreenPosForValue( precision , customVertexPosition , outputId );


			string value = GenerateVertexScreenPositionForValue( customVertexPosition , outputId , ref dataCollector , uniqueId , precision );

			if( !dataCollector.IsFragmentCategory )
			{
				return value;
			}

			string screenPosVarName = "screenPosition" + outputId;
			dataCollector.AddToInput( uniqueId , screenPosVarName , WirePortDataType.FLOAT4 , precision );
			dataCollector.AddToVertexLocalVariables( uniqueId , Constants.VertexShaderOutputStr + "." + screenPosVarName + " = " + value + ";" );



			string screenPosVarNameOnFrag = ScreenPositionStr + outputId;
			string globalResult = Constants.InputVarStr + "." + screenPosVarName;
			dataCollector.AddLocalVariable( uniqueId , precision , WirePortDataType.FLOAT4 , screenPosVarNameOnFrag , globalResult );
			return screenPosVarNameOnFrag;

		}

		static public string GenerateScreenPosition( ref MasterNodeDataCollector dataCollector , int uniqueId , PrecisionType precision , bool addInput = true )
		{
			// overriding precision
			precision = PrecisionType.Float;

			if( dataCollector.UsingCustomScreenPos && dataCollector.IsFragmentCategory )
			{
				string value = GenerateVertexScreenPosition( ref dataCollector , uniqueId , precision );
				dataCollector.AddToInput( uniqueId , "screenPosition" , WirePortDataType.FLOAT4 , precision );
				dataCollector.AddToVertexLocalVariables( uniqueId , Constants.VertexShaderOutputStr + ".screenPosition = " + value + ";" );

				string globalResult = Constants.InputVarStr + ".screenPosition";
				dataCollector.AddLocalVariable( uniqueId , precision , WirePortDataType.FLOAT4 , ScreenPositionStr , globalResult );
				return ScreenPositionStr;
			}
			else
			{
				if( !dataCollector.IsFragmentCategory )
					return GenerateVertexScreenPosition( ref dataCollector , uniqueId , precision );

				if( dataCollector.IsTemplate )
					return dataCollector.TemplateDataCollectorInstance.GetScreenPos( precision );
			}


			if( addInput )
				dataCollector.AddToInput( uniqueId , SurfaceInputs.SCREEN_POS , precision );

			string result = Constants.InputVarStr + ".screenPos";
			dataCollector.AddLocalVariable( uniqueId , string.Format( "float4 {0} = float4( {1}.xyz , {1}.w + 0.00000000001 );" , ScreenPositionStr , result ) );

			return ScreenPositionStr;
		}

		// GRAB SCREEN POSITION
		static public string GenerateGrabScreenPosition( ref MasterNodeDataCollector dataCollector , int uniqueId , PrecisionType precision , bool addInput = true , string customScreenPos = null )
		{
			// overriding precision
			precision = PrecisionType.Float;

			string screenPos = string.Empty;
			if( string.IsNullOrEmpty( customScreenPos ) )
				screenPos = GenerateScreenPosition( ref dataCollector , uniqueId , precision , addInput );
			else
				screenPos = customScreenPos;

			string computeBody = string.Empty;
			IOUtils.AddFunctionHeader( ref computeBody , GrabFunctionHeader );
			foreach( string line in GrabFunctionBody )
				IOUtils.AddFunctionLine( ref computeBody , line );
			IOUtils.CloseFunctionBody( ref computeBody );
			string functionResult = dataCollector.AddFunctions( GrabFunctionCall , computeBody , screenPos );

			dataCollector.AddLocalVariable( uniqueId , precision , WirePortDataType.FLOAT4 , GrabScreenPositionStr , functionResult );
			return GrabScreenPositionStr;
		}

		// GRAB SCREEN POSITION NORMALIZED
		static public string GenerateGrabScreenPositionNormalized( ref MasterNodeDataCollector dataCollector , int uniqueId , PrecisionType precision , bool addInput = true , string customScreenPos = null )
		{
			string stringPosVar = GenerateGrabScreenPosition( ref dataCollector , uniqueId , precision , addInput , customScreenPos );

			dataCollector.AddLocalVariable( uniqueId , precision , WirePortDataType.FLOAT4 , GrabScreenPositionNormalizedStr , string.Format( NormalizedScreenPosFormat , stringPosVar ) );
			return GrabScreenPositionNormalizedStr;
		}

		// SCREEN POSITION ON VERT
		static public string GenerateVertexScreenPositionForValue( string customVertexPosition , string outputId , ref MasterNodeDataCollector dataCollector , int uniqueId , PrecisionType precision )
		{
			// overriding precision
			precision = PrecisionType.Float;

			if( dataCollector.IsTemplate )
				return dataCollector.TemplateDataCollectorInstance.GetScreenPosForValue( precision , customVertexPosition , outputId );

			string screenPosVarName = ScreenPositionStr + outputId;
			string value = string.Format( "ComputeScreenPos( UnityObjectToClipPos( {0} ) )" , customVertexPosition );
			dataCollector.AddToVertexLocalVariables( uniqueId , precision , WirePortDataType.FLOAT4 , screenPosVarName , value );
			return screenPosVarName;
		}

		static public string GenerateVertexScreenPosition( ref MasterNodeDataCollector dataCollector , int uniqueId , PrecisionType precision )
		{
			// overriding precision
			precision = PrecisionType.Float;

			if( dataCollector.IsTemplate )
				return dataCollector.TemplateDataCollectorInstance.GetScreenPos( precision );

			string value = string.Format( "ComputeScreenPos( UnityObjectToClipPos( {0}.vertex ) )" , Constants.VertexShaderInputStr );
			dataCollector.AddToVertexLocalVariables( uniqueId , precision , WirePortDataType.FLOAT4 , ScreenPositionStr , value );
			return ScreenPositionStr;
		}

		// VERTEX POSITION
		static public string GenerateVertexPosition( ref MasterNodeDataCollector dataCollector , int uniqueId , WirePortDataType size )
		{
			// overriding precision
			var precision = PrecisionType.Float;

			if( dataCollector.IsTemplate )
				return dataCollector.TemplateDataCollectorInstance.GetVertexPosition( size , precision );

			string value = Constants.VertexShaderInputStr + ".vertex";
			if( size == WirePortDataType.FLOAT3 )
				value += ".xyz";

			if( dataCollector.PortCategory == MasterNodePortCategory.Fragment || dataCollector.PortCategory == MasterNodePortCategory.Debug )
			{
				dataCollector.AddToInput( uniqueId , SurfaceInputs.WORLD_POS );
				dataCollector.AddToIncludes( uniqueId , Constants.UnityShaderVariables );

				value = "mul( unity_WorldToObject, float4( " + Constants.InputVarStr + ".worldPos , 1 ) )";
			}
			string varName = VertexPosition4Str;
			if( size == WirePortDataType.FLOAT3 )
				varName = VertexPosition3Str;

			dataCollector.AddLocalVariable( uniqueId , precision , size , varName , value );
			return varName;
		}

		// VERTEX NORMAL
		static public string GenerateVertexNormal( ref MasterNodeDataCollector dataCollector , int uniqueId , PrecisionType precision )
		{
			if( dataCollector.MasterNodeCategory == AvailableShaderTypes.Template )
			{
				return dataCollector.TemplateDataCollectorInstance.GetVertexNormal( UIUtils.CurrentWindow.CurrentGraph.CurrentPrecision );
			}

			string value = Constants.VertexShaderInputStr + ".normal.xyz";
			if( dataCollector.PortCategory == MasterNodePortCategory.Fragment || dataCollector.PortCategory == MasterNodePortCategory.Debug )
			{
				GenerateWorldNormal( ref dataCollector , uniqueId );
				dataCollector.AddLocalVariable( uniqueId , precision , WirePortDataType.FLOAT3 , VertexNormalStr , "mul( unity_WorldToObject, float4( " + WorldNormalStr + ", 0 ) )" );
				dataCollector.AddLocalVariable( uniqueId , VertexNormalStr + " = normalize( " + VertexNormalStr + " );" );
				//dataCollector.AddToLocalVariables( uniqueId, precision, WirePortDataType.FLOAT3, VertexNormalStr, "mul( unity_WorldToObject, float4( " + WorldNormalStr + ", 0 ) )" );
			}
			else
			{
				dataCollector.AddLocalVariable( uniqueId , precision , WirePortDataType.FLOAT3 , VertexNormalStr , value );
			}
			return VertexNormalStr;
		}

		// VERTEX TANGENT
		static public string GenerateVertexTangent( ref MasterNodeDataCollector dataCollector , int uniqueId , PrecisionType precision , WirePortDataType size )
		{
			if( dataCollector.MasterNodeCategory == AvailableShaderTypes.Template )
			{
				return dataCollector.TemplateDataCollectorInstance.GetVertexTangent( size , UIUtils.CurrentWindow.CurrentGraph.CurrentPrecision );
			}

			if( dataCollector.PortCategory == MasterNodePortCategory.Fragment || dataCollector.PortCategory == MasterNodePortCategory.Debug )
			{
				GenerateWorldTangent( ref dataCollector , uniqueId );
				dataCollector.AddLocalVariable( uniqueId , precision , WirePortDataType.FLOAT4 , VertexTangentStr , "mul( unity_WorldToObject, float4( " + WorldTangentStr + ", 0 ) )" );
				dataCollector.AddLocalVariable( uniqueId , VertexTangentStr +" = normalize( " + VertexTangentStr + " );");
			}
			else
			{
				string value = Constants.VertexShaderInputStr + ".tangent";
				dataCollector.AddLocalVariable( uniqueId , precision , WirePortDataType.FLOAT4 , VertexTangentStr , value );
			}

			return ( size == WirePortDataType.FLOAT4 ) ? VertexTangentStr : VertexTangentStr + ".xyz";
		}

		// VERTEX TANGENT SIGN
		static public string GenerateVertexTangentSign( ref MasterNodeDataCollector dataCollector , int uniqueId , PrecisionType precision )
		{
			if( dataCollector.MasterNodeCategory == AvailableShaderTypes.Template )
			{
				return dataCollector.TemplateDataCollectorInstance.GetTangentSign( UIUtils.CurrentWindow.CurrentGraph.CurrentPrecision );
			}

			string value = Constants.VertexShaderInputStr + ".tangent.w";
			if( dataCollector.IsFragmentCategory )
			{
				dataCollector.AddToInput( uniqueId , VertexTangentSignStr , WirePortDataType.FLOAT , PrecisionType.Half );
				dataCollector.AddToVertexLocalVariables( uniqueId , Constants.VertexShaderOutputStr + "." + VertexTangentSignStr + " = " + Constants.VertexShaderInputStr + ".tangent.w;" );
				return Constants.InputVarStr + "." + VertexTangentSignStr;
			}
			else
			{
				dataCollector.AddLocalVariable( uniqueId , precision , WirePortDataType.FLOAT , VertexTangentSignStr , value );
			}
			return VertexTangentSignStr;
		}

		// VERTEX BITANGENT
		static public string GenerateVertexBitangent( ref MasterNodeDataCollector dataCollector , int uniqueId , PrecisionType precision )
		{
			if( dataCollector.MasterNodeCategory == AvailableShaderTypes.Template )
			{
				return dataCollector.TemplateDataCollectorInstance.GetVertexBitangent( UIUtils.CurrentWindow.CurrentGraph.CurrentPrecision );
			}

			if( dataCollector.PortCategory == MasterNodePortCategory.Fragment || dataCollector.PortCategory == MasterNodePortCategory.Debug )
			{
				GenerateWorldBitangent( ref dataCollector , uniqueId );
				dataCollector.AddLocalVariable( uniqueId , precision , WirePortDataType.FLOAT3 , VertexBitangentStr , "mul( unity_WorldToObject, float4( " + WorldBitangentStr + ", 0 ) )" );
				dataCollector.AddLocalVariable( uniqueId , VertexBitangentStr + " = normalize( " + VertexBitangentStr + " );" );
			}
			else
			{
				GenerateVertexNormal( ref dataCollector , uniqueId , precision );
				GenerateVertexTangent( ref dataCollector , uniqueId , precision , WirePortDataType.FLOAT3 );
				dataCollector.AddLocalVariable( uniqueId , precision , WirePortDataType.FLOAT3 , VertexBitangentStr , "cross( " + VertexNormalStr + ", " + VertexTangentStr + ") * " + Constants.VertexShaderInputStr + ".tangent.w * ( unity_WorldTransformParams.w >= 0.0 ? 1.0 : -1.0 )" );
			}
			return VertexBitangentStr;
		}

		// VERTEX POSITION ON FRAG
		static public string GenerateVertexPositionOnFrag( ref MasterNodeDataCollector dataCollector , int uniqueId , PrecisionType precision )
		{
			dataCollector.AddToInput( uniqueId , SurfaceInputs.WORLD_POS );
			dataCollector.AddToIncludes( uniqueId , Constants.UnityShaderVariables );

			string value = "mul( unity_WorldToObject, float4( " + Constants.InputVarStr + ".worldPos , 1 ) )";

			dataCollector.AddToLocalVariables( uniqueId , precision , WirePortDataType.FLOAT4 , VertexPosition4Str , value );
			return VertexPosition4Str;
		}

		// CLIP POSITION ON FRAG
		static public string GenerateClipPositionOnFrag( ref MasterNodeDataCollector dataCollector , int uniqueId , PrecisionType precision )
		{
			if( dataCollector.IsTemplate )
				return dataCollector.TemplateDataCollectorInstance.GetClipPos();

			string vertexName = GenerateVertexPositionOnFrag( ref dataCollector , uniqueId , precision );
			string value = string.Format( "ComputeScreenPos( UnityObjectToClipPos( {0} ) )" , vertexName );
			dataCollector.AddToLocalVariables( uniqueId , precision , WirePortDataType.FLOAT4 , ClipPositionStr , value );
			return ClipPositionStr;
		}

		// VIEW DIRECTION
		static public string GenerateViewDirection( ref MasterNodeDataCollector dataCollector , int uniqueId , ViewSpace space = ViewSpace.World )
		{
			PrecisionType precision = UIUtils.CurrentWindow.CurrentGraph.CurrentPrecision;
			if( dataCollector.IsTemplate )
				return ( space == ViewSpace.Tangent ) ? dataCollector.TemplateDataCollectorInstance.GetTangentViewDir( precision ) : dataCollector.TemplateDataCollectorInstance.GetViewDir();

			string worldPos = GenerateWorldPosition( ref dataCollector , uniqueId );
			string safeNormalizeInstruction = string.Empty;
			if( dataCollector.SafeNormalizeViewDir )
			{
				if( dataCollector.IsTemplate && dataCollector.IsSRP )
				{
					safeNormalizeInstruction = "SafeNormalize";
				}
				else
				{
					if( dataCollector.IsTemplate )
						dataCollector.AddToIncludes( -1 , Constants.UnityBRDFLib );
					safeNormalizeInstruction = "Unity_SafeNormalize";
				}
			}
			dataCollector.AddLocalVariable( uniqueId , precision , WirePortDataType.FLOAT3 , WorldViewDirectionStr , ( dataCollector.SafeNormalizeViewDir ? safeNormalizeInstruction : "normalize" ) + "( UnityWorldSpaceViewDir( " + worldPos + " ) )" );

			if( space == ViewSpace.Tangent )
			{
				string worldToTangent = GenerateWorldToTangentMatrix( ref dataCollector , uniqueId , precision );
				dataCollector.AddLocalVariable( uniqueId , precision , WirePortDataType.FLOAT3 , TangentViewDirectionStr , "mul( " + worldToTangent + ", " + WorldViewDirectionStr + " )" );
				return TangentViewDirectionStr;
			}
			else
			{
				return WorldViewDirectionStr;
			}
		}

		const string FaceVertexInstr = "(dot({0},float3(0,0,1)))";
		static public string GenerateVertexFace( ref MasterNodeDataCollector dataCollector , int uniqueId )
		{
			string viewDir = GenerateViewDirection( ref dataCollector , uniqueId , ViewSpace.Tangent );
			dataCollector.AddLocalVariable( -1 , PrecisionType.Float , WirePortDataType.FLOAT , FaceVertex , string.Format( FaceVertexInstr , viewDir ));
			return FaceVertex;

		}
		// VIEW POS
		static public string GenerateViewPositionOnFrag( ref MasterNodeDataCollector dataCollector, int uniqueId, PrecisionType precision )
		{
			// overriding precision
			precision = PrecisionType.Float;

			if( dataCollector.IsTemplate )
				UnityEngine.Debug.LogWarning( "View Pos not implemented on Templates" );

			string vertexName = GenerateVertexPositionOnFrag( ref dataCollector, uniqueId, precision );
			string value = string.Format( "UnityObjectToViewPos( {0} )", vertexName );
			dataCollector.AddToLocalVariables( uniqueId, precision, WirePortDataType.FLOAT3, ViewPositionStr, value );
			return ViewPositionStr;
		}

		// SCREEN DEPTH 
		static public string GenerateScreenDepthOnFrag( ref MasterNodeDataCollector dataCollector, int uniqueId, PrecisionType precision )
		{
			// overriding precision
			precision = PrecisionType.Float;

			if( dataCollector.IsTemplate )
				UnityEngine.Debug.LogWarning( "Screen Depth not implemented on Templates" );

			string viewPos = GenerateViewPositionOnFrag( ref dataCollector, uniqueId, precision );
			string value = string.Format( "-{0}.z", viewPos );
			dataCollector.AddToLocalVariables( uniqueId, precision, WirePortDataType.FLOAT, ScreenDepthStr, value );
			return ScreenDepthStr;
		}

		// LIGHT DIRECTION WORLD
		static public string GenerateWorldLightDirection( ref MasterNodeDataCollector dataCollector, int uniqueId, PrecisionType precision )
		{
			dataCollector.AddToIncludes( uniqueId, Constants.UnityCgLibFuncs );
			string worldPos = GeneratorUtils.GenerateWorldPosition( ref dataCollector, uniqueId );
			dataCollector.AddLocalVariable( uniqueId, "#if defined(LIGHTMAP_ON) && UNITY_VERSION < 560 //aseld" );
			dataCollector.AddLocalVariable( uniqueId, precision, WirePortDataType.FLOAT3, WorldLightDirStr, "0" );
			dataCollector.AddLocalVariable( uniqueId, "#else //aseld" );
			dataCollector.AddLocalVariable( uniqueId, precision, WirePortDataType.FLOAT3, WorldLightDirStr, ( dataCollector.SafeNormalizeLightDir ? "Unity_SafeNormalize" : "normalize" ) + "( UnityWorldSpaceLightDir( " + worldPos + " ) )" );
			dataCollector.AddLocalVariable( uniqueId, "#endif //aseld" );
			return WorldLightDirStr;
		}

		private static readonly string[] SafeNormalizeBuiltin = 
		{
			"inline float{0} ASESafeNormalize(float{0} inVec)\n",
			"{\n",
			"\tfloat dp3 = max( 0.001f , dot( inVec , inVec ) );\n",
			"\treturn inVec* rsqrt( dp3);\n",
			"}\n"
		};

		private static readonly string[] SafeNormalizeSRP =
		{
			"real{0} ASESafeNormalize(float{0} inVec)\n",
			"{\n",
			"\treal dp3 = max(FLT_MIN, dot(inVec, inVec));\n",
			"\treturn inVec* rsqrt( dp3);\n",
			"}\n",
		};

		private static readonly string ASEUnpackNormalRGBCall = "ASEUnpackNormalRGB({0},{1})";
		private static readonly string[] ASEUnpackNormalRGB =
		{
			"float3 ASEUnpackNormalRGB(float4 PackedNormal, float Scale = 1.0 )\n",
			"{\n",
			"\tfloat3 normal;\n",
			"\tnormal.xyz = PackedNormal.rgb * 2.0 - 1.0;\n",
			"\tnormal.xy *= Scale;\n",
			"\treturn normal;\n",
			"}\n"
		};

		static public string NormalizeValue( ref MasterNodeDataCollector dataCollector , bool safeNormalize , WirePortDataType dataType, string value )
		{
			string normalizeInstruction = string.Empty;
			if( safeNormalize )
			{
				string[] finalFunction = null;
				string[] funcVersion = dataCollector.IsSRP ? SafeNormalizeSRP : SafeNormalizeBuiltin;

				finalFunction = new string[ funcVersion.Length ];

				switch( dataType )
				{
					case WirePortDataType.FLOAT:
					finalFunction[0] = string.Format( funcVersion[ 0 ] , string.Empty );
					break;
					case WirePortDataType.FLOAT2:
					finalFunction[ 0 ] = string.Format( funcVersion[ 0 ] , "2" );
					break;
					case WirePortDataType.FLOAT3:
					finalFunction[ 0 ] = string.Format( funcVersion[ 0 ] , "3" );
					break;
					case WirePortDataType.FLOAT4:
					case WirePortDataType.COLOR:
					finalFunction[ 0 ] = string.Format( funcVersion[ 0 ] , "4" );
					break;
					default:
					case WirePortDataType.FLOAT3x3:
					case WirePortDataType.FLOAT4x4:
					case WirePortDataType.INT:
					case WirePortDataType.OBJECT:
					case WirePortDataType.SAMPLER1D:
					case WirePortDataType.SAMPLER2D:
					case WirePortDataType.SAMPLER3D:
					case WirePortDataType.SAMPLERCUBE:
					case WirePortDataType.UINT:
					case WirePortDataType.UINT4:
					case WirePortDataType.SAMPLER2DARRAY:
					case WirePortDataType.SAMPLERSTATE:return value;
				}

				for( int i = 1 ; i < funcVersion.Length ; i++ )
				{
					finalFunction[ i ] = funcVersion[ i ];
				}
				dataCollector.AddFunction( finalFunction[ 0 ] , finalFunction , false );
				normalizeInstruction = "ASESafeNormalize";
			}
			else
			{
				normalizeInstruction = "normalize";
			}

			return normalizeInstruction = normalizeInstruction + "( " + value + " )";

		}
		// LIGHT DIRECTION Object
		static public string GenerateObjectLightDirection( ref MasterNodeDataCollector dataCollector, int uniqueId, PrecisionType precision, string vertexPos )
		{
			dataCollector.AddToIncludes( uniqueId, Constants.UnityCgLibFuncs );
			dataCollector.AddLocalVariable( uniqueId, precision, WirePortDataType.FLOAT3, ObjectLightDirStr, "normalize( ObjSpaceLightDir( " + vertexPos + " ) )" );
			return ObjectLightDirStr;
		}

		// UNPACK NORMALS
		public static string GenerateUnpackNormalStr( ref MasterNodeDataCollector dataCollector, PrecisionType precision, int uniqueId, string outputId, string src, bool applyScale, string scale, UnpackInputMode inputMode )
		{
			string funcName;

			if( inputMode == UnpackInputMode.Object )
			{
				dataCollector.AddFunction( ASEUnpackNormalRGB[ 0 ] , ASEUnpackNormalRGB , false );
				return string.Format( ASEUnpackNormalRGBCall, src , scale );
			}

			if( dataCollector.IsTemplate && dataCollector.IsSRP )
			{
				if( applyScale )
				{
					dataCollector.AddLocalVariable( uniqueId, precision, WirePortDataType.FLOAT3, "unpack" + outputId, "UnpackNormalScale( " + src + ", " + scale + " )" );
					dataCollector.AddLocalVariable( uniqueId, "unpack" + outputId + ".z = lerp( 1, unpack" + outputId + ".z, saturate(" + scale + ") );" );
					funcName = "unpack" + outputId;
				}
				else
				{
					funcName = "UnpackNormalScale( " + src + ", " + scale + " )";
				}
			}
			else
			{
				funcName = applyScale ? "UnpackScaleNormal( " + src + ", " + scale + " )" : "UnpackNormal( " + src + " )";
			}
			return funcName;
		}

		//MATRIX INVERSE
		// 3x3
		public static string Inverse3x3Header = "Inverse3x3( {0} )";
		public static string[] Inverse3x3Function =
		{
			"{0}3x3 Inverse3x3({0}3x3 input)\n",
			"{\n",
			"\t{0}3 a = input._11_21_31;\n",
			"\t{0}3 b = input._12_22_32;\n",
			"\t{0}3 c = input._13_23_33;\n",
			"\treturn {0}3x3(cross(b,c), cross(c,a), cross(a,b)) * (1.0 / dot(a,cross(b,c)));\n",
			"}\n"
		};

		public static bool[] Inverse3x3FunctionFlags =
		{
			true,
			false,
			true,
			true,
			true,
			true,
			false
		};

		public static void Add3x3InverseFunction( ref MasterNodeDataCollector dataCollector, string precisionString )
		{
			if( !dataCollector.HasFunction( Inverse3x3Header ) )
			{
				//Hack to be used util indent is properly used
				int currIndent = UIUtils.ShaderIndentLevel;
				if( dataCollector.IsTemplate )
				{
					UIUtils.ShaderIndentLevel = 0;
				}
				else
				{
					UIUtils.ShaderIndentLevel = 1;
					UIUtils.ShaderIndentLevel++;
				}
				string finalFunction = string.Empty;
				for( int i = 0; i < Inverse3x3Function.Length; i++ )
				{
					finalFunction += UIUtils.ShaderIndentTabs + ( Inverse3x3FunctionFlags[ i ] ? string.Format( Inverse3x3Function[ i ], precisionString ) : Inverse3x3Function[ i ] );
				}


				UIUtils.ShaderIndentLevel = currIndent;

				dataCollector.AddFunction( Inverse3x3Header, finalFunction );
			}
		}

		public static string GenerateValueInVertex( ref MasterNodeDataCollector dataCollector, int uniqueId, WirePortDataType dataType, PrecisionType currentPrecisionType, string dataValue, string dataName, bool createInterpolator )
		{
			if( !dataCollector.IsFragmentCategory )
				return dataValue;

			//TEMPLATES
			if( dataCollector.IsTemplate )
			{
				if( createInterpolator && dataCollector.TemplateDataCollectorInstance.HasCustomInterpolatedData( dataName ) )
					return dataName;

				MasterNodePortCategory category = dataCollector.PortCategory;
				dataCollector.PortCategory = MasterNodePortCategory.Vertex;

				dataCollector.PortCategory = category;

				if( createInterpolator )
				{
					dataCollector.TemplateDataCollectorInstance.RegisterCustomInterpolatedData( dataName, dataType, currentPrecisionType, dataValue );
				}
				else
				{
					dataCollector.AddToVertexLocalVariables( -1, currentPrecisionType, dataType, dataName, dataValue );
				}

				return dataName;
			}

			//SURFACE 
			{
				if( dataCollector.TesselationActive )
				{
					UIUtils.ShowMessage( "Unable to use Vertex to Frag when Tessellation is active" );
					switch( dataType )
					{
						case WirePortDataType.FLOAT2:
						{
							return "(0).xx";
						}
						case WirePortDataType.FLOAT3:
						{
							return "(0).xxx";
						}
						case WirePortDataType.FLOAT4:
						case WirePortDataType.COLOR:
						{
							return "(0).xxxx";
						}
					}
					return "0";
				}

				if( createInterpolator )
					dataCollector.AddToInput( uniqueId, dataName, dataType, currentPrecisionType );

				MasterNodePortCategory portCategory = dataCollector.PortCategory;
				dataCollector.PortCategory = MasterNodePortCategory.Vertex;
				if( createInterpolator )
				{
					dataCollector.AddLocalVariable( uniqueId, Constants.VertexShaderOutputStr + "." + dataName, dataValue + ";" );
				}
				else
				{
					dataCollector.AddLocalVariable( uniqueId, currentPrecisionType, dataType, dataName, dataValue );
				}
				dataCollector.PortCategory = portCategory;
				return createInterpolator ? Constants.InputVarStr + "." + dataName : dataName;
			}
		}

		public static void AddCustomStandardSamplingMacros( ref MasterNodeDataCollector dataCollector, TextureType type, MipType mip )
		{
			AddCustomStandardSamplingMacros( ref dataCollector, Constants.TextureToWire[ type ], mip );
		}

		public static void AddCustomStandardSamplingMacros( ref MasterNodeDataCollector dataCollector, WirePortDataType type, MipType mip )
		{
			MacrosMask result = MacrosMask.NONE;
			switch( mip )
			{
				default:
				case MipType.Auto:
				result |= MacrosMask.AUTO;
				break;
				case MipType.MipLevel:
				result |= MacrosMask.LOD;
				break;
				case MipType.MipBias:
				result |= MacrosMask.BIAS;
				break;
				case MipType.Derivative:
				result |= MacrosMask.GRAD;
				break;
			}

			switch( type )
			{
				default:
				case WirePortDataType.SAMPLER2D:
				dataCollector.Using2DMacrosMask |= result;
				break;
				case WirePortDataType.SAMPLER3D:
				dataCollector.Using3DMacrosMask |= result;
				break;
				case WirePortDataType.SAMPLERCUBE:
				dataCollector.UsingCUBEMacrosMask |= result;
				break;
				case WirePortDataType.SAMPLER2DARRAY:
				dataCollector.Using2DArrayMacrosMask |= result;
				break;
			}
		}

		public static void AddCustom3DSRPMacros( ref MasterNodeDataCollector dataCollector )
		{
			// add just once
			if( dataCollector.UsingExtra3DSRPMacros )
				return;
			
			dataCollector.UsingExtra3DSRPMacros = true;
			for( int i = 0; i < Constants.CustomSRPSamplingMacros.Length; i++ )
				dataCollector.AddToDirectives( Constants.CustomSRPSamplingMacros[ i ] );
		}

		public static void AddCustomArraySamplingMacros( ref MasterNodeDataCollector dataCollector )
		{
			// add just once
			if( dataCollector.UsingArrayDerivatives )
				return;

			dataCollector.UsingArrayDerivatives = true;
			for( int i = 0; i < Constants.CustomArraySamplingMacros.Length; i++ )
				dataCollector.AddToDirectives( Constants.CustomArraySamplingMacros[ i ] );
		}

		/*public static void AddCustomASEMacros( ref MasterNodeDataCollector dataCollector )
		{
			string varPrefix = dataCollector.IsSRP ? varPrefix = "TEXTURE" : "UNITY_DECLARE_TEX";

			if( dataCollector.IsSRP )
			{
				for( int i = 0; i < Constants.CustomASESRPArgsMacros.Length; i++ )
				{
					dataCollector.AddToDirectives( Constants.CustomASESRPArgsMacros[ i ] );
				}

				for( int i = 0; i < Constants.CustomSRPSamplingMacros.Length; i++ )
				{
					dataCollector.AddToDirectives( Constants.CustomSRPSamplingMacros[ i ] );
				}
			}
			else
			{

				for( int i = 0; i < Constants.CustomASEStandardArgsMacros.Length; i++ )
				{
					dataCollector.AddToDirectives( Constants.CustomASEStandardArgsMacros[ i ] );
				}

				for( int i = 0; i < Constants.CustomStandardSamplingMacros.Length; i++ )
				{
					dataCollector.AddToDirectives( Constants.CustomStandardSamplingMacros[ i ] );
				}
			}

			for( int i = 0; i < Constants.CustomASEDeclararionMacros.Length; i++ )
			{
				string value = string.Format( Constants.CustomASEDeclararionMacros[ i ], varPrefix );
				dataCollector.AddToDirectives( value );
			}

			string samplePrefix = string.Empty;
			string samplerArgs = string.Empty;
			string samplerDecl = string.Empty;

			if( dataCollector.IsSRP )
			{
				samplePrefix = "SAMPLE_TEXTURE";
				samplerArgs = "samplerName,";

				for( int i = 0; i < Constants.CustomASESamplingMacros.Length; i++ )
				{
					string value = string.Format( Constants.CustomASESamplingMacros[ i ], samplerArgs, samplePrefix, samplerDecl );
					dataCollector.AddToDirectives( value );
				}
			}
			else
			{
				samplePrefix = "UNITY_SAMPLE_TEX";
				samplerArgs = "samplerName,";
				samplerDecl = "_SAMPLER";
				dataCollector.AddToDirectives( Constants.CustomASEStandarSamplingMacrosHelper[ 0 ] );
				for( int i = 0; i < Constants.CustomASESamplingMacros.Length; i++ )
				{
					string value = string.Format( Constants.CustomASESamplingMacros[ i ], samplerArgs, samplePrefix, samplerDecl );
					dataCollector.AddToDirectives( value );
				}
				dataCollector.AddToDirectives( Constants.CustomASEStandarSamplingMacrosHelper[ 1 ] );
				samplerArgs = string.Empty;
				samplerDecl = string.Empty;
				for( int i = 0; i < Constants.CustomASESamplingMacros.Length; i++ )
				{
					string value = string.Format( Constants.CustomASESamplingMacros[ i ], samplerArgs, samplePrefix, samplerDecl );
					dataCollector.AddToDirectives( value );
				}
				dataCollector.AddToDirectives( Constants.CustomASEStandarSamplingMacrosHelper[ 2 ] );
			}
		}*/

		public static void RegisterUnity2019MatrixDefines( ref MasterNodeDataCollector dataCollector )
		{
			if( dataCollector.IsSRP && dataCollector.TemplateDataCollectorInstance.IsHDRP )
			{
				//dataCollector.AddToDefines( -1, "unity_CameraProjection UNITY_MATRIX_P" );
				//dataCollector.AddToDefines( -1, "unity_CameraInvProjection UNITY_MATRIX_I_P" );
				//dataCollector.AddToDefines( -1, "unity_WorldToCamera UNITY_MATRIX_V" );
				//dataCollector.AddToDefines( -1, "unity_CameraToWorld UNITY_MATRIX_I_V" );

				dataCollector.AddToUniforms( -1, "float4x4 unity_CameraProjection;" );
				dataCollector.AddToUniforms( -1, "float4x4 unity_CameraInvProjection;" );
				dataCollector.AddToUniforms( -1, "float4x4 unity_WorldToCamera;" );
				dataCollector.AddToUniforms( -1, "float4x4 unity_CameraToWorld;" );
			}
		}
	}
}
