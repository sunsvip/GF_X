// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using System;
using UnityEditor;
using UnityEngine;

namespace AmplifyShaderEditor
{
	[Serializable]
	[NodeAttributes( "Voronoi", "Miscellaneous", "Voronoi", Tags = "noise" )]
	public sealed class VoronoiNode : ParentNode
	{
		// Unity Voronoi
		private readonly string UnityVoronoiNoiseFunc = "UnityVoronoi({0},{1},{2},{3})";
		private readonly string[] UnityVoroniNoiseFunctionsBody =
		{
			"inline float2 UnityVoronoiRandomVector( float2 UV, float offset )\n",
			"{\n",
			"\tfloat2x2 m = float2x2( 15.27, 47.63, 99.41, 89.98 );\n",
			"\tUV = frac( sin(mul(UV, m) ) * 46839.32 );\n",
			"\treturn float2( sin(UV.y* +offset ) * 0.5 + 0.5, cos( UV.x* offset ) * 0.5 + 0.5 );\n",
			"}\n",
			"\n",
			"//x - Out y - Cells\n",
			"float3 UnityVoronoi( float2 UV, float AngleOffset, float CellDensity, inout float2 mr )\n",
			"{\n",
			"\tfloat2 g = floor( UV * CellDensity );\n",
			"\tfloat2 f = frac( UV * CellDensity );\n",
			"\tfloat t = 8.0;\n",
			"\tfloat3 res = float3( 8.0, 0.0, 0.0 );\n",
			"\n",
			"\tfor( int y = -1; y <= 1; y++ )\n",
			"\t{\n",
			"\t	for( int x = -1; x <= 1; x++ )\n",
			"\t	{\n",
			"\t\t\tfloat2 lattice = float2( x, y );\n",
			"\t\t\tfloat2 offset = UnityVoronoiRandomVector( lattice + g, AngleOffset );\n",
			"\t\t\tfloat d = distance( lattice + offset, f );\n",
			"\n",
			"\t\t\tif( d < res.x )\n",
			"\t\t\t{\n",
			"\t\t\t\tmr = f - lattice - offset;\n",
			"\t\t\t\tres = float3( d, offset.x, offset.y );\n",
			"\t\t\t}\n",
			"\t	}\n",
			"\t}\n",
			"\treturn res;\n",
			"}\n",
		};

		////////////

		private const string VoronoiHashHeader = "float2 voronoihash{0}( float2 p )";
		private readonly string[] VoronoiHashBody = { "p = p - 2 * floor( p / 2 );",
			"p = float2( dot( p, float2( 127.1, 311.7 ) ), dot( p, float2( 269.5, 183.3 ) ) );",
			"return frac( sin( p ) *43758.5453);" };


		private const string VoronoiHeader = "float voronoi{0}( float2 v, float time, inout float2 id, inout float2 mr, float smoothness, inout float2 smoothId )";
		private const string VoronoiFunc = "voronoi{0}( {1}, {2}, {3}, {4}, {5}, {6} )";
		private string[] VoronoiBody = 
		{
			"float2 n = floor( v );",
			"float2 f = frac( v );",
			"float F1 = 8.0;",
			"float F2 = 8.0; float2 mg = 0;",
			"for ( int j = -1; j <= 1; j++ )",
			"{",
			" \tfor ( int i = -1; i <= 1; i++ )",
			" \t{",
			" \t\tfloat2 g = float2( i, j );",
			" \t\tfloat2 o = voronoihash{0}( n + g );",
			" \t\tfloat2 r = f - g - ( sin( 0 + o * 6.2831 ) * 0.5 + 0.5 );",
			" \t\tfloat d = dot( r, r );",
			" \t\tif( d<F1 ) {",//12
			" \t\t\tF2 = F1;",//13
			" \t\t\tF1 = d; mg = g; mr = r; id = o;",//14
			" \t\t} else if( d<F2 ) {",//15
			" \t\t\tF2 = d;",//16
			"",//this is where we inject smooth Id
			" \t\t}" ,//18
			" \t}",
			"}",
			"return F1;"
		};
		
		private string VoronoiDistanceBody =
			"\nF1 = 8.0;" +
			"\nfor ( int j = -2; j <= 2; j++ )" +
			"\n{{" +
			"\nfor ( int i = -2; i <= 2; i++ )" +
			"\n{{" +
			"\nfloat2 g = mg + float2( i, j );" +
			"\nfloat2 o = voronoihash{1}( n + g );" +
			"\n{0}" +
			"\nfloat d = dot( 0.5 * ( r + mr ), normalize( r - mr ) );" +
			"\nF1 = min( F1, d );" +
			"\n}}" +
			"\n}}" +
			"\nreturn F1;";

		[SerializeField]
		private int m_distanceFunction = 0;

		[SerializeField]
		private float m_minkowskiPower = 1;

		[SerializeField]
		private int m_functionType = 0;

		[SerializeField]
		private int m_octaves = 1;

		[SerializeField]
		private bool m_tileable = false;

		[SerializeField]
		private int m_tileScale = 1;

		[SerializeField]
		private bool m_useUnity = false;

		[SerializeField]
		private bool m_calculateSmoothValue = false;

		[SerializeField]
		private bool m_applySmoothToIds = false;

		private const string FunctionTypeStr = "Method";//"Function Type";
		private readonly string[] m_functionTypeStr = { "Cells", "Crystal", "Glass", "Caustic", "Distance" };

		private const string DistanceFunctionLabelStr = "Distance Function";
		private readonly string[] m_distanceFunctionStr = { "Euclidean\u00B2", "Euclidean", "Manhattan", "Chebyshev", "Minkowski" };

		[SerializeField]
		private int m_searchQuality = 0;
		private const string SearchQualityLabelStr = "Search Quality";
		private readonly string[] m_searchQualityStr = { "9 Cells", "25 Cells", "49 Cells" };


		private const string UseTileScaleStr = "_UseTileScale";
		private const string TileScaleStr = "_TileScale";
		private const string MinkowskiPowerStr = "_MinkowskiPower";
		private const string DistFuncStr = "_DistFunc";
		private const string MethodTypeStr = "_MethodType";
		private const string SearchQualityStr = "_SearchQuality";
		private const string OctavesStr = "_Octaves";
		private const string UseSmoothnessStr = "_UseSmoothness";

		private int m_UseTileScaleId;
		private int m_TileScaleId;
		private int m_MinkowskiPowerId;
		private int m_DistFuncId;
		private int m_MethodTypeId;
		private int m_SearchQualityId;
		private int m_OctavesId;
		private int m_UseSmoothnessId;

		private int m_cachedUvsId = -1;

		protected override void CommonInit( int uniqueId )
		{
			base.CommonInit( uniqueId );
			AddInputPort( WirePortDataType.FLOAT2, false, "UV" );
			AddInputPort( WirePortDataType.FLOAT, false, "Angle" );
			AddInputPort( WirePortDataType.FLOAT, false, "Scale" );
			AddInputPort( WirePortDataType.FLOAT, false, "Smoothness" );

			m_inputPorts[ 1 ].AutoDrawInternalData = true;
			m_inputPorts[ 2 ].AutoDrawInternalData = true;
			m_inputPorts[ 2 ].FloatInternalData = 1;

			AddOutputPort( WirePortDataType.FLOAT, "Out" );
			AddOutputPort( WirePortDataType.FLOAT2, "ID" );
			AddOutputPort( WirePortDataType.FLOAT2, "UV" );
			m_textLabelWidth = 120;
			//m_useInternalPortData = true;
			m_autoWrapProperties = true;
			m_previewShaderGUID = "bc1498ccdade442479038b24982fc946";
			ChangePorts();
			ChechSmoothPorts();
		}

		void ChechSmoothPorts()
		{
			m_inputPorts[ 3 ].Visible = !m_useUnity && m_calculateSmoothValue && (m_functionType == 0) ;
			m_sizeIsDirty = true;
		}

		void ChangePorts()
		{
			m_previewMaterialPassId = 0;
		}

		public override void OnEnable()
		{
			base.OnEnable();
			m_UseTileScaleId = Shader.PropertyToID( UseTileScaleStr );
			m_TileScaleId = Shader.PropertyToID( TileScaleStr );
			m_MinkowskiPowerId = Shader.PropertyToID( MinkowskiPowerStr );
			m_DistFuncId = Shader.PropertyToID( DistFuncStr );
			m_MethodTypeId = Shader.PropertyToID( MethodTypeStr );
			m_SearchQualityId = Shader.PropertyToID( SearchQualityStr );
			m_OctavesId = Shader.PropertyToID( OctavesStr );
			m_UseSmoothnessId = Shader.PropertyToID( UseSmoothnessStr );
		}

		public override void SetPreviewInputs()
		{
			base.SetPreviewInputs();

			if( m_cachedUvsId == -1 )
				m_cachedUvsId = Shader.PropertyToID( "_CustomUVs" );

			PreviewMaterial.SetInt( m_cachedUvsId, ( m_inputPorts[ 0 ].IsConnected ? 1 : 0 ) );

			m_previewMaterialPassId = m_useUnity ? 0 : 1;
			if( !m_useUnity )
			{
				PreviewMaterial.SetInt( m_SearchQualityId, m_searchQuality + 1 );

				if( m_functionType == 4)
				{
					PreviewMaterial.SetInt( m_DistFuncId, 0 );
				} else
				{
					PreviewMaterial.SetInt( m_DistFuncId, m_distanceFunction );
				}

				if( m_distanceFunction == 4 )
				{
					PreviewMaterial.SetFloat( m_MinkowskiPowerId, m_minkowskiPower );
				}

				PreviewMaterial.SetInt( m_MethodTypeId, m_functionType );
				int smoothnessValue = m_calculateSmoothValue ? 1 : 0;
				PreviewMaterial.SetInt( m_UseSmoothnessId, smoothnessValue );

				PreviewMaterial.SetFloat( m_UseTileScaleId, m_tileable ? 1.0f : 0.0f );

				if( m_tileable )
					PreviewMaterial.SetInt( m_TileScaleId, m_tileScale );

				PreviewMaterial.SetInt( m_OctavesId, m_octaves );
			}
		}

		public override void RenderNodePreview()
		{
			//Runs at least one time
			if( !m_initialized )
			{
				// nodes with no preview don't update at all
				PreviewIsDirty = false;
				return;
			}

			if( !PreviewIsDirty )
				return;

			SetPreviewInputs();

			if( !Preferences.GlobalDisablePreviews )
			{
				RenderTexture temp = RenderTexture.active;

				RenderTexture.active = m_outputPorts[ 0 ].OutputPreviewTexture;
				PreviewMaterial.SetInt( "_PreviewID" , 0 );
				Graphics.Blit( null , m_outputPorts[ 0 ].OutputPreviewTexture , PreviewMaterial , m_previewMaterialPassId );

				RenderTexture.active = m_outputPorts[ 1 ].OutputPreviewTexture;
				PreviewMaterial.SetInt( "_PreviewID" , 1 );
				Graphics.Blit( null , m_outputPorts[ 1 ].OutputPreviewTexture , PreviewMaterial , m_previewMaterialPassId );

				RenderTexture.active = m_outputPorts[ 2 ].OutputPreviewTexture;
				PreviewMaterial.SetInt( "_PreviewID" , 2 );
				Graphics.Blit( null , m_outputPorts[ 2 ].OutputPreviewTexture , PreviewMaterial , m_previewMaterialPassId );
				RenderTexture.active = temp;
			}
			PreviewIsDirty = m_continuousPreviewRefresh;

			FinishPreviewRender = true;
		}

		public override void DrawProperties()
		{
			base.DrawProperties();
			EditorGUI.BeginChangeCheck();
			{
				EditorGUI.BeginDisabledGroup( m_useUnity );
				{
					EditorGUI.BeginChangeCheck();
					m_functionType = EditorGUILayoutPopup( FunctionTypeStr, m_functionType, m_functionTypeStr );
					if( EditorGUI.EndChangeCheck() )
					{
						ChechSmoothPorts();
					}

					EditorGUI.BeginDisabledGroup( m_functionType == 4 );
					m_distanceFunction = EditorGUILayoutPopup( DistanceFunctionLabelStr, m_distanceFunction, m_distanceFunctionStr );
					if( m_distanceFunction == 4 )
					{
						m_minkowskiPower = EditorGUILayoutSlider( "Minkowski Power", m_minkowskiPower, 1, 5 );
					}
					EditorGUI.EndDisabledGroup();

					m_searchQuality = EditorGUILayoutPopup( SearchQualityLabelStr, m_searchQuality, m_searchQualityStr );
					m_octaves = EditorGUILayoutIntSlider( "Octaves", m_octaves, 1, 8 );
					m_tileable = EditorGUILayoutToggle( "Tileable", m_tileable );
					EditorGUI.BeginDisabledGroup( !m_tileable );
					m_tileScale = EditorGUILayoutIntField( "Tile Scale", m_tileScale );
					EditorGUI.EndDisabledGroup();

					//Only smoothing cells type for now
					if( m_functionType == 0 )
					{
						EditorGUI.BeginChangeCheck();
						m_calculateSmoothValue = EditorGUILayoutToggle( "Smooth", m_calculateSmoothValue );
						if( EditorGUI.EndChangeCheck() )
						{
							ChechSmoothPorts();
						}

						if( m_calculateSmoothValue )
						{
							m_applySmoothToIds = EditorGUILayoutToggle( "  Apply To ID" , m_applySmoothToIds );
						}
					}
				}
				EditorGUI.EndDisabledGroup();

				EditorGUI.BeginChangeCheck();
				m_useUnity = EditorGUILayoutToggle( "Unity's Voronoi", m_useUnity );
				if( EditorGUI.EndChangeCheck() )
				{
					ChangePorts();
					ChechSmoothPorts();
				}
			}
			if( EditorGUI.EndChangeCheck() )
			{
				PreviewIsDirty = true;
			}

			NodeUtils.DrawPropertyGroup( ref m_internalDataFoldout, Constants.InternalDataLabelStr, () =>
			{
				for( int i = 0; i < m_inputPorts.Count; i++ )
				{
					if( m_inputPorts[ i ].ValidInternalData && !m_inputPorts[ i ].IsConnected && m_inputPorts[ i ].Visible && m_inputPorts[ i ].AutoDrawInternalData )
					{
						m_inputPorts[ i ].ShowInternalData( this );
					}
				}
			} );
		}

		void ChangeFunction( string scale )
		{
			VoronoiBody[ 10 ] = "\t\to = ( sin( time + o * 6.2831 ) * 0.5 + 0.5 ); float2 r = f - g - o;";
			int q = m_searchQuality + 1;
			VoronoiBody[ 4 ] = "for ( int j = -" + q + "; j <= " + q + "; j++ )";
			VoronoiBody[ 6 ] = "\tfor ( int i = -" + q + "; i <= " + q + "; i++ )";
			int dFunction = m_distanceFunction;
			if( m_functionType == 4 )
				dFunction = 0;
			switch( dFunction )
			{
				default:
				case 0:
				VoronoiBody[ 11 ] = "\t\tfloat d = 0.5 * dot( r, r );";
				break;
				case 1:
				VoronoiBody[ 11 ] = "\t\tfloat d = 0.707 * sqrt(dot( r, r ));";
				break;
				case 2:
				VoronoiBody[ 11 ] = "\t\tfloat d = 0.5 * ( abs(r.x) + abs(r.y) );";
				break;
				case 3:
				VoronoiBody[ 11 ] = "\t\tfloat d = max(abs(r.x), abs(r.y));";
				break;
				case 4:
				VoronoiBody[ 11 ] = "\t\tfloat d = " + ( 1 / Mathf.Pow( 2, 1 / m_minkowskiPower ) ).ToString( "n3" ) + " * pow( ( pow( abs( r.x ), " + m_minkowskiPower + " ) + pow( abs( r.y ), " + m_minkowskiPower + " ) ), " + ( 1 / m_minkowskiPower ).ToString( "n3" ) + " );";
				break;
			}

			if( m_functionType == 0 )
			{
				if( m_calculateSmoothValue )
				{
					VoronoiBody[ 12 ] = " //\t\tif( d<F1 ) {";
					VoronoiBody[ 13 ] = " //\t\t\tF2 = F1;";
					VoronoiBody[ 14 ] = " \t\t\tfloat h = smoothstep(0.0, 1.0, 0.5 + 0.5 * (F1 - d) / smoothness); F1 = lerp(F1, d, h) - smoothness * h * (1.0 - h);mg = g; mr = r; id = o;";
					VoronoiBody[ 15 ] = " //\t\t} else if( d<F2 ) {";
					VoronoiBody[ 16 ] = " //\t\t\tF2 = d;";
					VoronoiBody[ 17 ] = m_applySmoothToIds? "\t\t\tfloat correctionFactor = smoothness * h * (1.0f - h) / ( 1.0f + 3.0f * smoothness ); smoothId = lerp( smoothId,id, h ) - correctionFactor;":string.Empty;
					VoronoiBody[ 18 ] = " //\t\t}";
				}
				else
				{
					VoronoiBody[ 12 ] = " \t\tif( d<F1 ) {";
					VoronoiBody[ 13 ] = " \t\t\tF2 = F1;";
					VoronoiBody[ 14 ] = " \t\t\tF1 = d; mg = g; mr = r; id = o;";
					VoronoiBody[ 15 ] = " \t\t} else if( d<F2 ) {";
					VoronoiBody[ 16 ] = " \t\t\tF2 = d;";
					VoronoiBody[ 17 ] = string.Empty;
					VoronoiBody[ 18 ] = " \t\t}";
				}
				
				
			}
			else
			{
				VoronoiBody[ 12 ] = " \t\tif( d<F1 ) {";
				VoronoiBody[ 13 ] = " \t\t\tF2 = F1;";
				VoronoiBody[ 14 ] = " \t\t\tF1 = d; mg = g; mr = r; id = o;";
				VoronoiBody[ 15 ] = " \t\t} else if( d<F2 ) {";
				VoronoiBody[ 16 ] = " \t\t\tF2 = d;";
				VoronoiBody[ 17 ] = string.Empty;
				VoronoiBody[ 18 ] = " \t\t}";
			}

			switch( m_functionType )
			{
				default:
				case 0:
				VoronoiBody[ 21 ] = "return F1;";
				break;
				case 1:
				VoronoiBody[ 21 ] = "return F2;";
				break;
				case 2:
				VoronoiBody[ 21 ] = "return F2 - F1;";
				break;
				case 3:
				VoronoiBody[ 21 ] = "return (F2 + F1) * 0.5;";
				break;
				case 4:
				VoronoiBody[ 21 ] = string.Format( VoronoiDistanceBody , VoronoiBody[ 10 ], OutputId );
				break;
			}

			if( m_tileable )
			{

				VoronoiHashBody[ 0 ] = "p = p - " + m_tileScale + " * floor( p / " + m_tileScale + " );";
			}
			else
			{
				VoronoiHashBody[ 0 ] = "";
			}
		}

		public override string GenerateShaderForOutput( int outputId, ref MasterNodeDataCollector dataCollector, bool ignoreLocalvar )
		{
			if( m_outputPorts[ outputId ].IsLocalValue( dataCollector.PortCategory ) )
			{
				return m_outputPorts[ outputId ].LocalValue( dataCollector.PortCategory );
			}

			if( m_useUnity )
			{
				string uvValue = string.Empty;
				if( m_inputPorts[ 0 ].IsConnected )
					uvValue = m_inputPorts[ 0 ].GeneratePortInstructions( ref dataCollector );
				else
				{
					if( dataCollector.IsTemplate )
						uvValue = dataCollector.TemplateDataCollectorInstance.GenerateAutoUVs( 0 );
					else
						uvValue = GeneratorUtils.GenerateAutoUVs( ref dataCollector, UniqueId, 0 );
				}
				string angleOffset = m_inputPorts[ 1 ].GeneratePortInstructions( ref dataCollector );
				string cellDensity = m_inputPorts[ 2 ].GeneratePortInstructions( ref dataCollector );
				dataCollector.AddLocalVariable( UniqueId, string.Format( "float2 uv{0} = 0;", OutputId ) );
				dataCollector.AddFunction( UnityVoroniNoiseFunctionsBody[ 0 ], UnityVoroniNoiseFunctionsBody, false );
				string varName = "unityVoronoy" + OutputId;
				string varValue = string.Format( UnityVoronoiNoiseFunc, uvValue, angleOffset, cellDensity, "uv" + OutputId );
				dataCollector.AddLocalVariable( UniqueId, CurrentPrecisionType, WirePortDataType.FLOAT3, varName, varValue );
				m_outputPorts[ 0 ].SetLocalValue( varName + ".x", dataCollector.PortCategory );
				m_outputPorts[ 1 ].SetLocalValue( varName + ".yz", dataCollector.PortCategory );
				m_outputPorts[ 2 ].SetLocalValue( "uv" + OutputId, dataCollector.PortCategory );
				return m_outputPorts[ outputId ].LocalValue( dataCollector.PortCategory );
			}
			else
			{

				string scaleValue = m_inputPorts[ 2 ].GeneratePortInstructions( ref dataCollector );

				string timeVarValue = m_inputPorts[ 1 ].GeneratePortInstructions( ref dataCollector );
				string timeVarName = "time" + OutputId;
				dataCollector.AddLocalVariable( UniqueId, CurrentPrecisionType, WirePortDataType.FLOAT, timeVarName, timeVarValue );

				ChangeFunction( scaleValue );

				string voronoiHashFunc = string.Empty;
				string VoronoiHashHeaderFormatted = string.Format( VoronoiHashHeader, OutputId );
				IOUtils.AddFunctionHeader( ref voronoiHashFunc, VoronoiHashHeaderFormatted );
				for( int i = 0; i < VoronoiHashBody.Length; i++ )
				{
					IOUtils.AddFunctionLine( ref voronoiHashFunc, VoronoiHashBody[ i ] );
				}
				IOUtils.CloseFunctionBody( ref voronoiHashFunc );
				dataCollector.AddFunction( VoronoiHashHeaderFormatted, voronoiHashFunc );

				string smoothnessName = "0";
				//need to add a local value to send to function since its an inout and sending a numeric value to it generates a compilation error
				string smoothIdName = "voronoiSmoothId" + OutputId;
				dataCollector.AddLocalVariable( UniqueId , CurrentPrecisionType , WirePortDataType.FLOAT2 , smoothIdName , "0" );

				if( m_calculateSmoothValue )
				{
					smoothnessName = "voronoiSmooth" + OutputId;
					string smoothnessValue = m_inputPorts[ 3 ].GeneratePortInstructions( ref dataCollector );
					dataCollector.AddLocalVariable( UniqueId, CurrentPrecisionType, WirePortDataType.FLOAT, smoothnessName, smoothnessValue );
				}

				string voronoiFunc = string.Empty;
				IOUtils.AddFunctionHeader( ref voronoiFunc, string.Format( VoronoiHeader, OutputId ) );
				for( int i = 0; i < VoronoiBody.Length; i++ )
				{
					if( i == 9 )
					{
						IOUtils.AddFunctionLine( ref voronoiFunc, string.Format( VoronoiBody[ i ],OutputId ) );
					}
					else
					{
						IOUtils.AddFunctionLine( ref voronoiFunc, VoronoiBody[ i ] );
					}
				}
				IOUtils.CloseFunctionBody( ref voronoiFunc );
				dataCollector.AddFunction( string.Format( VoronoiHeader, OutputId ), voronoiFunc );

				string uvs = string.Empty;
				if( m_inputPorts[ 0 ].IsConnected )
					uvs = m_inputPorts[ 0 ].GeneratePortInstructions( ref dataCollector );
				else
				{
					if( dataCollector.IsTemplate )
					{
						uvs = dataCollector.TemplateDataCollectorInstance.GenerateAutoUVs( 0 );
					}
					else
					{
						uvs = GeneratorUtils.GenerateAutoUVs( ref dataCollector, UniqueId, 0 );
					}
				}

				dataCollector.AddLocalVariable( UniqueId, string.Format( "float2 coords{0} = {1} * {2};", OutputId, uvs, scaleValue ) );
				dataCollector.AddLocalVariable( UniqueId, string.Format( "float2 id{0} = 0;", OutputId ) );
				dataCollector.AddLocalVariable( UniqueId, string.Format( "float2 uv{0} = 0;", OutputId ) );

				if( m_octaves == 1 )
				{
					dataCollector.AddLocalVariable( UniqueId, string.Format( "float voroi{0} = {1};", OutputId, string.Format( VoronoiFunc, OutputId, "coords" + OutputId,timeVarName, "id"+ OutputId, "uv" + OutputId, smoothnessName , smoothIdName ) ) );
				}
				else
				{
					dataCollector.AddLocalVariable( UniqueId, string.Format( "float fade{0} = 0.5;", OutputId ) );
					dataCollector.AddLocalVariable( UniqueId, string.Format( "float voroi{0} = 0;", OutputId ) );
					dataCollector.AddLocalVariable( UniqueId, string.Format( "float rest{0} = 0;", OutputId ) );
					dataCollector.AddLocalVariable( UniqueId, string.Format( "for( int it{0} = 0; it{0} <" + m_octaves + "; it{0}++ ){{", OutputId)  );
					dataCollector.AddLocalVariable( UniqueId, string.Format( "voroi{0} += fade{0} * voronoi{0}( coords{0}, time{0}, id{0}, uv{0}, {1},{2} );", OutputId, smoothnessName, smoothIdName ) );
					dataCollector.AddLocalVariable( UniqueId, string.Format( "rest{0} += fade{0};", OutputId ) );
					dataCollector.AddLocalVariable( UniqueId, string.Format( "coords{0} *= 2;", OutputId ) );
					dataCollector.AddLocalVariable( UniqueId, string.Format( "fade{0} *= 0.5;", OutputId ) );
					dataCollector.AddLocalVariable( UniqueId, "}" + "//Voronoi" + OutputId );
					dataCollector.AddLocalVariable( UniqueId, string.Format( "voroi{0} /= rest{0};", OutputId ) );
				}
				m_outputPorts[ 0 ].SetLocalValue( "voroi" + OutputId, dataCollector.PortCategory );
				
				m_outputPorts[ 1 ].SetLocalValue( ( m_functionType == 0 && m_calculateSmoothValue && m_applySmoothToIds ) ? smoothIdName : ( "id" + OutputId ), dataCollector.PortCategory );
				m_outputPorts[ 2 ].SetLocalValue( "uv" + OutputId, dataCollector.PortCategory );
				return m_outputPorts[ outputId ].LocalValue( dataCollector.PortCategory );
			}
		}

		public override void ReadFromString( ref string[] nodeParams )
		{
			base.ReadFromString( ref nodeParams );
			m_searchQuality = Convert.ToInt32( GetCurrentParam( ref nodeParams ) );
			m_distanceFunction = Convert.ToInt32( GetCurrentParam( ref nodeParams ) );
			m_minkowskiPower = Convert.ToSingle( GetCurrentParam( ref nodeParams ) );
			m_functionType = Convert.ToInt32( GetCurrentParam( ref nodeParams ) );
			m_octaves = Convert.ToInt32( GetCurrentParam( ref nodeParams ) );
			m_tileable = Convert.ToBoolean( GetCurrentParam( ref nodeParams ) );
			m_tileScale = Convert.ToInt32( GetCurrentParam( ref nodeParams ) );
			m_useUnity = Convert.ToBoolean( GetCurrentParam( ref nodeParams ) );
			if( UIUtils.CurrentShaderVersion() > 17402 )
			{
				m_calculateSmoothValue = Convert.ToBoolean( GetCurrentParam( ref nodeParams ) );
			}

			if( UIUtils.CurrentShaderVersion() > 18902 )
			{
				m_applySmoothToIds = Convert.ToBoolean( GetCurrentParam( ref nodeParams ) );
			}

			ChangePorts();
			ChechSmoothPorts();
		}

		public override void WriteToString( ref string nodeInfo, ref string connectionsInfo )
		{
			base.WriteToString( ref nodeInfo, ref connectionsInfo );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_searchQuality );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_distanceFunction );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_minkowskiPower );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_functionType );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_octaves );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_tileable.ToString() );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_tileScale );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_useUnity );
			IOUtils.AddFieldValueToString( ref nodeInfo, m_calculateSmoothValue );
			IOUtils.AddFieldValueToString( ref nodeInfo , m_applySmoothToIds );
		}
	}
}
